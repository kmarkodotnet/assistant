namespace FamilyOs.Domain.Entities;

public sealed class Warranty
{
    private Warranty() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public decimal? PurchasePrice { get; private set; }
    public string? Currency { get; private set; }
    public int? WarrantyMonths { get; private set; }
    public DateOnly? WarrantyEndDate { get; private set; }
    public string? Seller { get; private set; }
    public Guid? RelatedFamilyMemberId { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public DateTime? DeletedUtc { get; private set; }

    public Document? Document { get; private set; }

    public static Warranty Create(Guid documentId, string productName)
        => new()
        {
            Id = Guid.CreateVersion7(),
            DocumentId = documentId,
            ProductName = productName,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
        };

    public void Patch(
        string? productName,
        string? brand,
        string? model,
        string? serialNumber,
        DateOnly? purchaseDate,
        decimal? purchasePrice,
        string? currency,
        int? warrantyMonths,
        DateOnly? warrantyEndDate,
        string? seller,
        Guid? relatedFamilyMemberId)
    {
        if (productName is not null) ProductName = productName;
        Brand = brand ?? Brand;
        Model = model ?? Model;
        SerialNumber = serialNumber ?? SerialNumber;
        PurchaseDate = purchaseDate ?? PurchaseDate;
        PurchasePrice = purchasePrice ?? PurchasePrice;
        Currency = currency ?? Currency;
        WarrantyMonths = warrantyMonths ?? WarrantyMonths;
        WarrantyEndDate = warrantyEndDate ?? WarrantyEndDate;
        Seller = seller ?? Seller;
        RelatedFamilyMemberId = relatedFamilyMemberId ?? RelatedFamilyMemberId;
        UpdatedUtc = DateTime.UtcNow;
    }
}
