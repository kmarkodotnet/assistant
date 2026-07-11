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

        // IssueDate is AI-extracted and frequently missing (e.g. insurance documents that state
        // a policy period rather than a clean "kiállítás dátuma"). Records without it fall back
        // to CreatedUtc (when the record entered the system) so they still count in aggregates
        // instead of silently vanishing.
        var query = _db.FinancialRecords.AsNoTracking()
            .Where(r => r.Document != null
                && (r.Document.IsPrivate == false || r.Document.CreatedByUserAccountId == userId));

        var records = await query
            .Select(r => new { r.Id, r.DocumentId, r.Vendor, r.Amount, r.Currency, r.IssueDate, r.CreatedUtc, DocumentTitle = r.Document!.Title })
            .ToListAsync(ct);

        records = records
            .Where(r =>
            {
                var effectiveDate = r.IssueDate ?? DateOnly.FromDateTime(r.CreatedUtc);
                return effectiveDate >= range.from && effectiveDate <= range.to;
            })
            .ToList();

        if (hint is not null)
            records = records
                .Where(r =>
                    (r.Vendor is not null && r.Vendor.Contains(hint, StringComparison.OrdinalIgnoreCase)) ||
                    r.DocumentTitle.Contains(hint, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (records.Count == 0)
        {
            return new SearchResponse
            {
                ModeUsed = SearchMode.Qa,
                Answer = "Nincs erre vonatkozó adat a rögzített pénzügyi tételek között.",
                Confidence = 0.0,
            };
        }

        // Report every currency present, not just the numerically dominant one — otherwise a
        // mixed-currency result set (e.g. 2 HUF invoices + 1 EUR invoice) silently drops the
        // minority currency from the answer while still listing its document among the sources,
        // which reads as an internal contradiction ("3 tételt találtam" vs. a sum that only
        // covers 2 of them).
        var currencyTotals = records
            .GroupBy(r => r.Currency ?? "HUF")
            .Select(g => (Currency: g.Key, Total: g.Sum(r => r.Amount ?? 0m), Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();

        var answer = FormatAnswer(currencyTotals, range, hint);

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

    // Stem-prefix matching instead of an exact-word stopword set: Hungarian is agglutinative
    // (kiadás/kiadásom/kiadásaim, fizet/fizettünk/fizetünk, hónap/hónapban/hónapja all share a
    // stem with different suffixes attached), so a handful of stems catches every inflected form
    // instead of an ever-growing word list that silently misses one ("kiadásom" was previously
    // misread as a vendor-name hint, since only the bare word "kiadás" would ever appear in the
    // aggregate-intent trigger list, not its inflected forms).
    private static readonly string[] _stopStems =
    [
        "mennyi", "összesen", "átlagosan", "hányszor", "elmúlt", "hónap", "nap",
        "fizet", "kiadás", "kolt", "költ", "bevétel", "volt", "van", "forint", "huf",
    ];

    internal static string? ExtractVendorHint(string query)
    {
        var tokens = query
            .Split([' ', '?', '.', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 5
                && !t.All(char.IsDigit)
                && !_stopStems.Any(stem => t.StartsWith(stem, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(t => t.Length)
            .ToList();

        if (tokens.Count == 0) return null;

        var token = tokens[0];
        return token.Length > 6 ? token[..6] : token;
    }

    internal static string FormatAnswer(
        IReadOnlyList<(string Currency, decimal Total, int Count)> currencyTotals,
        (DateOnly from, DateOnly to) range,
        string? vendorHint)
    {
        var subject = vendorHint is not null
            ? string.Create(CultureInfo.InvariantCulture, $" (a(z) \"{vendorHint}\" tételekre)")
            : string.Empty;

        var amountsText = string.Join(" és ", currencyTotals.Select(c =>
            string.Create(CultureInfo.InvariantCulture, $"{c.Total:N0} {c.Currency} ({c.Count} tétel)")));

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Az elmúlt időszakban ({range.from:yyyy.MM.dd} – {range.to:yyyy.MM.dd}) összesen {amountsText} fizettünk{subject}.");
    }
}
