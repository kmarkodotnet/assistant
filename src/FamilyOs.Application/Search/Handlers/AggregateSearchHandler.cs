using System.Globalization;
using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Search.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Search.Handlers;

/// <summary>
/// Deterministic SQL-aggregation path for summation-style Q&amp;A questions
/// (e.g. "mennyi villanyszámlát fizettünk az elmúlt 6 hónapban"). Routed from
/// <see cref="QaHandler"/> before any LLM/RAG call, so the reported number is
/// always a real SUM() over <see cref="FamilyOs.Domain.Entities.FinancialRecord"/>
/// rows, never LLM-hallucinated.
/// </summary>
public sealed class AggregateSearchHandler
{
    private readonly IFamilyOsDbContext _db;
    private readonly IClock _clock;

    public AggregateSearchHandler(IFamilyOsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest req, Guid? userId, CancellationToken ct)
    {
        var range = ResolveDateRange(req, _clock.Today);
        var hint = ExtractVendorHint(req.Query);

        var query = _db.FinancialRecords.AsNoTracking()
            .Where(r => r.Document != null
                && (r.Document.IsPrivate == false || r.Document.CreatedByUserAccountId == userId)
                && r.IssueDate.HasValue
                && r.IssueDate.Value >= range.from
                && r.IssueDate.Value <= range.to);

        if (hint is not null)
            query = query.Where(r =>
                (r.Vendor != null && r.Vendor.Contains(hint)) ||
                (r.Document != null && r.Document.Title.Contains(hint)));

        var records = await query
            .Select(r => new { r.Id, r.DocumentId, r.Vendor, r.Amount, r.Currency })
            .ToListAsync(ct);

        if (records.Count == 0)
        {
            return new SearchResponse
            {
                ModeUsed = SearchMode.Qa,
                Answer = "Nincs erre vonatkozó adat a rögzített pénzügyi tételek között.",
                Confidence = 0.0,
            };
        }

        var currency = records.GroupBy(r => r.Currency ?? "HUF")
            .OrderByDescending(g => g.Count())
            .First().Key;
        var total = records.Where(r => (r.Currency ?? "HUF") == currency).Sum(r => r.Amount ?? 0m);
        var countInCurrency = records.Count(r => (r.Currency ?? "HUF") == currency);

        var answer = FormatAnswer(total, currency, countInCurrency, range, hint);

        return new SearchResponse
        {
            ModeUsed = SearchMode.Qa,
            Answer = answer,
            Confidence = 1.0,
            AnswerSources = records.Select(r => r.Id.ToString()).ToArray(),
            TotalCount = records.Count,
            Hits = records.Select(r => new SearchHit
            {
                EntityType = "document",
                EntityId = r.DocumentId,
                Title = r.Vendor ?? "Pénzügyi tétel",
                Score = 1.0,
            }).ToList(),
        };
    }

    internal static (DateOnly from, DateOnly to) ResolveDateRange(SearchRequest req, DateOnly today)
    {
        var to = req.To ?? today;
        var from = req.From ?? today.AddMonths(-6);
        return (from, to);
    }

    internal static string? ExtractVendorHint(string query)
    {
        var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mennyi", "összesen", "átlagosan", "hányszor", "az", "a", "elmúlt", "hónapban",
            "hónap", "hónapja", "napban", "napja", "fizettünk", "fizetünk", "fizettem",
            "volt", "van", "ft", "forint", "huf",
        };

        var tokens = query
            .Split([' ', '?', '.', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 5 && !stopwords.Contains(t) && !t.All(char.IsDigit))
            .OrderByDescending(t => t.Length)
            .ToList();

        if (tokens.Count == 0) return null;

        var token = tokens[0];
        return token.Length > 6 ? token[..6] : token;
    }

    internal static string FormatAnswer(decimal total, string currency, int count, (DateOnly from, DateOnly to) range, string? vendorHint)
    {
        var subject = vendorHint is not null
            ? string.Create(CultureInfo.InvariantCulture, $" (a(z) \"{vendorHint}\" tételekre)")
            : string.Empty;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Az elmúlt időszakban ({range.from:yyyy.MM.dd} – {range.to:yyyy.MM.dd}) összesen {total:N0} {currency}-ot fizettünk{subject}, {count} tétel alapján.");
    }
}
