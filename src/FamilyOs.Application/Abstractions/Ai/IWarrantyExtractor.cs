namespace FamilyOs.Application.Abstractions.Ai;

public record WarrantyExtraction(string? ProductName, DateOnly? PurchaseDate, DateOnly? ExpiryDate, int? WarrantyMonths, string? Notes);

public interface IWarrantyExtractor
{
    Task<WarrantyExtraction?> ExtractAsync(string text, CancellationToken ct = default);
}
