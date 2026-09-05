using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class MaintenanceLocationConfiguration : IEntityTypeConfiguration<MaintenanceLocation>
{
    public void Configure(EntityTypeBuilder<MaintenanceLocation> builder)
    {
        builder.ConfigureOperational("MaintenanceLocations", "maintenance");
        builder.Property(x => x.Code).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne<OperatingCity>().WithMany().HasForeignKey(x => x.OperatingCityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.OperatingCityId, x.Status });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_MaintenanceLocations_Type", "[LocationType] BETWEEN 1 AND 3");
            table.HasCheckConstraint("CK_MaintenanceLocations_Status", "[Status] BETWEEN 1 AND 3");
            table.HasCheckConstraint("CK_MaintenanceLocations_Latitude", "[Latitude] IS NULL OR [Latitude] BETWEEN -90 AND 90");
            table.HasCheckConstraint("CK_MaintenanceLocations_Longitude", "[Longitude] IS NULL OR [Longitude] BETWEEN -180 AND 180");
        });

        var seededAt = new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new MaintenanceLocation
            {
                Id = MaintenanceLocation.JeddahWarehouseId,
                Code = "JEDDAH_WAREHOUSE",
                NameAr = "مستودع جدة",
                NameEn = "Jeddah Warehouse",
                OperatingCityId = OperatingCity.JeddahId,
                LocationType = MaintenanceLocationType.WarehouseAndWorkshop,
                AllowsCompanyVehicles = true,
                AllowsExternalVehicles = false,
                AllowsSparePartSales = false,
                AllowsPaidExternalRepairs = false,
                InventoryEnabled = true,
                Status = CatalogStatus.Active,
                CreatedAtUtc = seededAt
            },
            new MaintenanceLocation
            {
                Id = MaintenanceLocation.RiyadhWorkshopId,
                Code = "RIYADH_WORKSHOP",
                NameAr = "ورشة الرياض",
                NameEn = "Riyadh Workshop",
                OperatingCityId = OperatingCity.RiyadhId,
                LocationType = MaintenanceLocationType.Workshop,
                AllowsCompanyVehicles = true,
                AllowsExternalVehicles = true,
                AllowsSparePartSales = true,
                AllowsPaidExternalRepairs = true,
                InventoryEnabled = true,
                Status = CatalogStatus.Active,
                CreatedAtUtc = seededAt
            });
    }
}

internal sealed class InventoryLocationConfiguration : IEntityTypeConfiguration<InventoryLocation>
{
    public void Configure(EntityTypeBuilder<InventoryLocation> builder)
    {
        builder.ConfigureOperational("InventoryLocations", "maintenance");
        builder.Property(x => x.Code).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.HasOne<MaintenanceLocation>().WithMany().HasForeignKey(x => x.MaintenanceLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.MaintenanceLocationId, x.Status });
        builder.ToTable(table => table.HasCheckConstraint("CK_InventoryLocations_Status", "[Status] BETWEEN 1 AND 3"));

        var seededAt = new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new InventoryLocation
            {
                Id = InventoryLocation.JeddahWarehouseInventoryId,
                Code = "JEDDAH_WAREHOUSE_STOCK",
                NameAr = "مخزون مستودع جدة",
                NameEn = "Jeddah Warehouse Stock",
                MaintenanceLocationId = MaintenanceLocation.JeddahWarehouseId,
                Status = CatalogStatus.Active,
                CreatedAtUtc = seededAt
            },
            new InventoryLocation
            {
                Id = InventoryLocation.RiyadhWorkshopInventoryId,
                Code = "RIYADH_WORKSHOP_STOCK",
                NameAr = "مخزون ورشة الرياض",
                NameEn = "Riyadh Workshop Stock",
                MaintenanceLocationId = MaintenanceLocation.RiyadhWorkshopId,
                Status = CatalogStatus.Active,
                CreatedAtUtc = seededAt
            });
    }
}

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ConfigureOperational("InventoryItems", "maintenance");
        builder.Property(x => x.Sku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedSku).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.Barcode).HasMaxLength(100);
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(2000);
        builder.Property(x => x.DescriptionEn).HasMaxLength(2000);
        builder.Property(x => x.DefaultPackageQuantity).HasPrecision(18, 3);
        builder.Property(x => x.MinimumStockLevel).HasPrecision(18, 3);
        builder.Property(x => x.ReorderQuantity).HasPrecision(18, 3);
        builder.HasIndex(x => x.NormalizedSku).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.Barcode).HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_InventoryItems_ItemType", "[ItemType] BETWEEN 1 AND 4");
            table.HasCheckConstraint("CK_InventoryItems_Units", "[BaseUnitOfMeasure] BETWEEN 1 AND 5 AND [PurchaseUnitOfMeasure] BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_InventoryItems_OilUnit", "[ItemType] <> 3 OR [BaseUnitOfMeasure] = 2");
            table.HasCheckConstraint("CK_InventoryItems_Quantities", "([DefaultPackageQuantity] IS NULL OR [DefaultPackageQuantity] > 0) AND [MinimumStockLevel] >= 0 AND [ReorderQuantity] >= 0");
            table.HasCheckConstraint("CK_InventoryItems_Status", "[Status] BETWEEN 1 AND 3");
        });
    }
}

internal sealed class MaintenanceSupplierConfiguration : IEntityTypeConfiguration<MaintenanceSupplier>
{
    public void Configure(EntityTypeBuilder<MaintenanceSupplier> builder)
    {
        builder.ConfigureOperational("Suppliers", "maintenance");
        builder.Property(x => x.SupplierNumber).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.LegalNameAr).HasMaxLength(250).IsRequired();
        builder.Property(x => x.LegalNameEn).HasMaxLength(250).IsRequired();
        builder.Property(x => x.VatNumber).HasMaxLength(32);
        builder.Property(x => x.CommercialRegistrationNumber).HasMaxLength(32);
        builder.Property(x => x.ContactName).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => x.SupplierNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.VatNumber).HasFilter("[VatNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_MaintenanceSuppliers_Status", "[Status] BETWEEN 1 AND 3");
            table.HasCheckConstraint("CK_MaintenanceSuppliers_PaymentTerms", "[PaymentTermsDays] IS NULL OR [PaymentTermsDays] >= 0");
        });
    }
}

internal sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ConfigureOperational("StockBalances", "maintenance");
        builder.Property(x => x.QuantityOnHand).HasPrecision(18, 3);
        builder.Property(x => x.QuantityReserved).HasPrecision(18, 3);
        builder.Property(x => x.ReportingAverageUnitCost).HasPrecision(18, 6);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.InventoryItemId, x.InventoryLocationId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint("CK_StockBalances_Quantities", "[QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand] AND [ReportingAverageUnitCost] >= 0"));
    }
}

internal sealed class StockCostLayerConfiguration : IEntityTypeConfiguration<StockCostLayer>
{
    public void Configure(EntityTypeBuilder<StockCostLayer> builder)
    {
        builder.ConfigureOperational("StockCostLayers", "maintenance");
        builder.Property(x => x.OriginalQuantity).HasPrecision(18, 3);
        builder.Property(x => x.RemainingQuantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Property(x => x.OriginalTotalCost).HasPrecision(18, 2);
        builder.Property(x => x.LotNumber).HasMaxLength(100);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseReceiptLine>().WithMany().HasForeignKey(x => x.SourceReceiptLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovementLine>().WithMany().HasForeignKey(x => x.SourceMovementLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockCostLayer>().WithMany().HasForeignKey(x => x.SourceCostLayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.InventoryItemId, x.InventoryLocationId, x.ReceivedAtUtc, x.OriginalSequence, x.Id });
        builder.ToTable(table => table.HasCheckConstraint("CK_StockCostLayers_Values", "[OriginalQuantity] > 0 AND [RemainingQuantity] >= 0 AND [RemainingQuantity] <= [OriginalQuantity] AND [UnitCost] >= 0 AND [OriginalTotalCost] >= 0"));
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ConfigureHistory("StockMovements", "maintenance");
        builder.Property(x => x.MovementNumber).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.SourceDocumentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.SourceLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.DestinationLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.ReversalOfMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.MovementNumber).IsUnique();
        builder.HasIndex(x => new { x.SourceLocationId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.DestinationLocationId, x.OccurredAtUtc });
        builder.ToTable(table => table.HasCheckConstraint("CK_StockMovements_Type", "[MovementType] BETWEEN 1 AND 9"));
    }
}

internal sealed class StockMovementLineConfiguration : IEntityTypeConfiguration<StockMovementLine>
{
    public void Configure(EntityTypeBuilder<StockMovementLine> builder)
    {
        builder.ConfigureHistory("StockMovementLines", "maintenance");
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.LotNumber).HasMaxLength(100);
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.StockMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockCostLayer>().WithMany().HasForeignKey(x => x.CostLayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.StockMovementId, x.InventoryItemId });
        builder.ToTable(table => table.HasCheckConstraint("CK_StockMovementLines_Values", "[Quantity] > 0 AND [UnitCost] >= 0 AND [TotalCost] >= 0"));
    }
}

internal sealed class StockCostAllocationConfiguration : IEntityTypeConfiguration<StockCostAllocation>
{
    public void Configure(EntityTypeBuilder<StockCostAllocation> builder)
    {
        builder.ConfigureHistory("StockCostAllocations", "maintenance");
        builder.Property(x => x.AllocatedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Property(x => x.AllocatedCost).HasPrecision(18, 2);
        builder.HasOne<StockMovementLine>().WithMany().HasForeignKey(x => x.StockMovementLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceMaterialUsage>().WithMany().HasForeignKey(x => x.MaintenanceMaterialUsageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderInventoryIssueLine>().WithMany().HasForeignKey(x => x.RiderInventoryIssueLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockCostLayer>().WithMany().HasForeignKey(x => x.StockCostLayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.StockMovementLineId, x.StockCostLayerId }).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint("CK_StockCostAllocations_Values", "[AllocatedQuantity] > 0 AND [UnitCost] >= 0 AND [AllocatedCost] >= 0"));
    }
}

internal sealed class PurchaseReceiptConfiguration : IEntityTypeConfiguration<PurchaseReceipt>
{
    public void Configure(EntityTypeBuilder<PurchaseReceipt> builder)
    {
        builder.ConfigureOperational("PurchaseReceipts", "maintenance");
        builder.Property(x => x.ReceiptNumber).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.SupplierInvoiceNumber).HasMaxLength(100);
        ConfigureMoney(builder.Property(x => x.Subtotal));
        ConfigureMoney(builder.Property(x => x.DiscountAmount));
        ConfigureMoney(builder.Property(x => x.TaxAmount));
        ConfigureMoney(builder.Property(x => x.InventoryValuationAmount));
        ConfigureMoney(builder.Property(x => x.TotalAmount));
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired();
        builder.HasOne<MaintenanceSupplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.PostedMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ReceiptNumber).IsUnique();
        builder.HasIndex(x => new { x.SupplierId, x.SupplierInvoiceNumber }).IsUnique().HasFilter("[SupplierInvoiceNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint("CK_PurchaseReceipts_Amounts", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [DiscountAmount] <= [Subtotal] AND [TaxAmount] >= 0 AND [InventoryValuationAmount] >= 0 AND [TotalAmount] >= 0"));
    }

    private static void ConfigureMoney(PropertyBuilder<decimal> property) => property.HasPrecision(18, 2);
}

internal sealed class PurchaseReceiptLineConfiguration : IEntityTypeConfiguration<PurchaseReceiptLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptLine> builder)
    {
        builder.ConfigureHistory("PurchaseReceiptLines", "maintenance");
        builder.Property(x => x.PackageCount).HasPrecision(18, 3);
        builder.Property(x => x.DeclaredQuantityPerPackage).HasPrecision(18, 3);
        builder.Property(x => x.ReceivedBaseQuantity).HasPrecision(18, 3);
        builder.Property(x => x.GrossWeightKg).HasPrecision(18, 3);
        builder.Property(x => x.NetWeightKg).HasPrecision(18, 3);
        builder.Property(x => x.PackageUnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.InventoryValuationAmount).HasPrecision(18, 2);
        builder.Property(x => x.BaseUnitCost).HasPrecision(18, 6);
        builder.Property(x => x.LotNumber).HasMaxLength(100);
        builder.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovementLine>().WithMany().HasForeignKey(x => x.StockMovementLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockCostLayer>().WithMany().HasForeignKey(x => x.StockCostLayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PurchaseReceiptId, x.InventoryItemId });
        builder.ToTable(table => table.HasCheckConstraint("CK_PurchaseReceiptLines_Values", "[PackageCount] > 0 AND [DeclaredQuantityPerPackage] > 0 AND [ReceivedBaseQuantity] > 0 AND ([GrossWeightKg] IS NULL OR [GrossWeightKg] > 0) AND ([NetWeightKg] IS NULL OR [NetWeightKg] > 0) AND [PackageUnitPrice] >= 0 AND [LineSubtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [InventoryValuationAmount] >= 0 AND [BaseUnitCost] >= 0"));
    }
}

internal sealed class PurchaseReceiptAttachmentConfiguration : IEntityTypeConfiguration<PurchaseReceiptAttachment>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptAttachment> builder)
    {
        builder.ConfigureHistory("PurchaseReceiptAttachments", "maintenance");
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256Checksum).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PurchaseReceiptId).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint("CK_PurchaseReceiptAttachments_Size", "[FileSizeBytes] > 0"));
    }
}

internal sealed class OilBarrelConfiguration : IEntityTypeConfiguration<OilBarrel>
{
    public void Configure(EntityTypeBuilder<OilBarrel> builder)
    {
        builder.ConfigureOperational("OilBarrels", "maintenance");
        builder.Property(x => x.BarrelNumber).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.NominalCapacityLiters).HasPrecision(18, 3);
        builder.Property(x => x.RemainingLiters).HasPrecision(18, 3);
        builder.Property(x => x.UnitCostPerLiter).HasPrecision(18, 6);
        builder.Property(x => x.MaximumAllowedLossLiters).HasPrecision(18, 3);
        builder.Property(x => x.RecordedLossLiters).HasPrecision(18, 3);
        builder.HasOne<PurchaseReceiptLine>().WithMany().HasForeignKey(x => x.PurchaseReceiptLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockCostLayer>().WithMany().HasForeignKey(x => x.StockCostLayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.BarrelNumber).IsUnique();
        builder.HasIndex(x => new { x.InventoryLocationId, x.InventoryItemId, x.Status, x.OpenedAtUtc });
        builder.HasIndex(x => new { x.InventoryLocationId, x.InventoryItemId }).IsUnique().HasFilter("[Status] = 2 AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.PurchaseReceiptLineId, x.PackageSequence }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_OilBarrels_Quantities", "[NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [UnitCostPerLiter] >= 0 AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.02, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters]");
            table.HasCheckConstraint("CK_OilBarrels_Status", "([Status] = 1 AND [OpenedAtUtc] IS NULL AND [RemainingLiters] > 0) OR ([Status] = 2 AND [OpenedAtUtc] IS NOT NULL AND [RemainingLiters] > 0) OR ([Status] = 3 AND [OpenedAtUtc] IS NOT NULL AND [RemainingLiters] = 0) OR [Status] = 4");
        });
    }
}

internal sealed class OilBarrelUsageAllocationConfiguration : IEntityTypeConfiguration<OilBarrelUsageAllocation>
{
    public void Configure(EntityTypeBuilder<OilBarrelUsageAllocation> builder)
    {
        builder.ConfigureHistory("OilBarrelUsageAllocations", "maintenance");
        builder.Property(x => x.QuantityLiters).HasPrecision(18, 3);
        builder.HasOne<MaintenanceMaterialUsage>().WithMany().HasForeignKey(x => x.MaintenanceMaterialUsageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OilBarrel>().WithMany().HasForeignKey(x => x.OilBarrelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OilBarrelUsageAllocation>().WithMany().HasForeignKey(x => x.ReversalOfAllocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.MaintenanceMaterialUsageId, x.OilBarrelId, x.Direction });
        builder.ToTable(table => table.HasCheckConstraint("CK_OilBarrelUsageAllocations_Values", "[QuantityLiters] > 0 AND ([Direction] = 1 AND [ReversalOfAllocationId] IS NULL OR [Direction] = 2 AND [ReversalOfAllocationId] IS NOT NULL)"));
    }
}

internal sealed class OilBarrelLossConfiguration : IEntityTypeConfiguration<OilBarrelLoss>
{
    public void Configure(EntityTypeBuilder<OilBarrelLoss> builder)
    {
        builder.ConfigureHistory("OilBarrelLosses", "maintenance");
        builder.Property(x => x.QuantityLiters).HasPrecision(18, 3);
        builder.Property(x => x.CostAmount).HasPrecision(18, 2);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<OilBarrel>().WithMany().HasForeignKey(x => x.OilBarrelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.StockMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovementLine>().WithMany().HasForeignKey(x => x.StockMovementLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.OilBarrelId, x.OccurredAtUtc });
        builder.ToTable(table => table.HasCheckConstraint("CK_OilBarrelLosses_Values", "[QuantityLiters] > 0 AND [CostAmount] >= 0"));
    }
}

internal sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ConfigureOperational("StockTransfers", "maintenance");
        builder.Property(x => x.TransferNumber).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.SourceLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.DestinationLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.SourceMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.DestinationMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TransferNumber).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint("CK_StockTransfers_Locations", "[SourceLocationId] <> [DestinationLocationId]"));
    }
}

internal sealed class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> builder)
    {
        builder.ConfigureHistory("StockTransferLines", "maintenance");
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.HasOne<StockTransfer>().WithMany().HasForeignKey(x => x.StockTransferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint("CK_StockTransferLines_Values", "[Quantity] > 0 AND [TotalCost] >= 0"));
    }
}

internal sealed class SupplierReturnConfiguration : IEntityTypeConfiguration<SupplierReturn>
{
    public void Configure(EntityTypeBuilder<SupplierReturn> builder)
    {
        builder.ConfigureOperational("SupplierReturns", "maintenance");
        builder.Property(x => x.ReturnNumber).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<MaintenanceSupplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseReceipt>().WithMany().HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.PostedMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ReturnNumber).IsUnique();
    }
}

internal sealed class SupplierReturnLineConfiguration : IEntityTypeConfiguration<SupplierReturnLine>
{
    public void Configure(EntityTypeBuilder<SupplierReturnLine> builder)
    {
        builder.ConfigureHistory("SupplierReturnLines", "maintenance");
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<SupplierReturn>().WithMany().HasForeignKey(x => x.SupplierReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockCostLayer>().WithMany().HasForeignKey(x => x.StockCostLayerId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint("CK_SupplierReturnLines_Values", "[Quantity] > 0 AND [UnitCost] >= 0 AND [TotalCost] >= 0"));
    }
}

internal sealed class RiderInventoryIssueConfiguration : IEntityTypeConfiguration<RiderInventoryIssue>
{
    public void Configure(EntityTypeBuilder<RiderInventoryIssue> builder)
    {
        builder.ConfigureOperational("RiderInventoryIssues", "maintenance");
        builder.Property(x => x.IssueNumber).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.IssuedFromLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RelatedAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.PostedMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IssueNumber).IsUnique();
        builder.HasIndex(x => new { x.RiderProfileId, x.IssuedAtUtc });
    }
}

internal sealed class RiderInventoryIssueLineConfiguration : IEntityTypeConfiguration<RiderInventoryIssueLine>
{
    public void Configure(EntityTypeBuilder<RiderInventoryIssueLine> builder)
    {
        builder.ConfigureHistory("RiderInventoryIssueLines", "maintenance");
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.ReturnedQuantity).HasPrecision(18, 3);
        builder.HasOne<RiderInventoryIssue>().WithMany().HasForeignKey(x => x.RiderInventoryIssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovementLine>().WithMany().HasForeignKey(x => x.StockMovementLineId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint("CK_RiderInventoryIssueLines_Values", "[Quantity] > 0 AND [TotalCost] >= 0 AND [ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]"));
    }
}

internal sealed class MaintenanceWorkOrderConfiguration : IEntityTypeConfiguration<MaintenanceWorkOrder>
{
    public void Configure(EntityTypeBuilder<MaintenanceWorkOrder> builder)
    {
        builder.ConfigureOperational("WorkOrders", "maintenance");
        builder.Property(x => x.WorkOrderNumber).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.Diagnosis).HasMaxLength(4000);
        builder.Property(x => x.WorkPerformed).HasMaxLength(4000);
        builder.Property(x => x.QualityCheckNotes).HasMaxLength(4000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        ConfigureMoney(builder.Property(x => x.EstimatedCost));
        ConfigureMoney(builder.Property(x => x.ActualMaterialCost));
        ConfigureMoney(builder.Property(x => x.ActualLaborCost));
        ConfigureMoney(builder.Property(x => x.ActualOtherCost));
        ConfigureMoney(builder.Property(x => x.ActualTotalCost));
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleIssue>().WithMany().HasForeignKey(x => x.VehicleIssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceLocation>().WithMany().HasForeignKey(x => x.MaintenanceLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RiderVehicleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.AttributedRiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.WorkOrderNumber).IsUnique();
        builder.HasIndex(x => new { x.MaintenanceLocationId, x.Status, x.OpenedAtUtc });
        builder.HasIndex(x => new { x.VehicleId, x.OpenedAtUtc });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_MaintenanceWorkOrders_Subject", "([ServiceSubjectType] = 1 AND [VehicleId] IS NOT NULL) OR ([ServiceSubjectType] = 2 AND [VehicleId] IS NULL AND [VehicleIssueId] IS NULL)");
            table.HasCheckConstraint("CK_MaintenanceWorkOrders_Odometers", "([OdometerAtOpen] IS NULL OR [OdometerAtOpen] >= 0) AND ([OdometerAtCompletion] IS NULL OR [OdometerAtCompletion] >= 0)");
            table.HasCheckConstraint("CK_MaintenanceWorkOrders_Costs", "[EstimatedCost] >= 0 AND [ActualMaterialCost] >= 0 AND [ActualLaborCost] >= 0 AND [ActualOtherCost] >= 0 AND [ActualTotalCost] >= 0");
        });
    }

    private static void ConfigureMoney(PropertyBuilder<decimal> property) => property.HasPrecision(18, 2);
}

internal sealed class ExternalVehicleSnapshotConfiguration : IEntityTypeConfiguration<ExternalVehicleSnapshot>
{
    public void Configure(EntityTypeBuilder<ExternalVehicleSnapshot> builder)
    {
        builder.ConfigureHistory("ExternalVehicleSnapshots", "maintenance");
        builder.Property(x => x.PlateOrReference).HasMaxLength(100);
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.CustomerPhone).HasMaxLength(32);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne<MaintenanceWorkOrder>().WithMany().HasForeignKey(x => x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.MaintenanceWorkOrderId).IsUnique();
    }
}

internal sealed class MaintenanceMaterialUsageConfiguration : IEntityTypeConfiguration<MaintenanceMaterialUsage>
{
    public void Configure(EntityTypeBuilder<MaintenanceMaterialUsage> builder)
    {
        builder.ConfigureHistory("MaterialUsages", "maintenance");
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne<MaintenanceWorkOrder>().WithMany().HasForeignKey(x => x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLocation>().WithMany().HasForeignKey(x => x.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(x => x.StockMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovementLine>().WithMany().HasForeignKey(x => x.StockMovementLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RiderVehicleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceMaterialUsage>().WithMany().HasForeignKey(x => x.ReversalOfUsageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.UsedAtUtc });
        builder.HasIndex(x => new { x.RiderProfileId, x.UsedAtUtc });
        builder.ToTable(table => table.HasCheckConstraint("CK_MaterialUsages_Values", "[Quantity] > 0 AND [TotalCost] >= 0"));
    }
}

internal sealed class MaintenanceLaborEntryConfiguration : IEntityTypeConfiguration<MaintenanceLaborEntry>
{
    public void Configure(EntityTypeBuilder<MaintenanceLaborEntry> builder)
    {
        builder.ConfigureHistory("LaborEntries", "maintenance");
        builder.Property(x => x.ExternalTechnicianName).HasMaxLength(200);
        builder.Property(x => x.Hours).HasPrecision(18, 2);
        builder.Property(x => x.HourlyRate).HasPrecision(18, 2);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.HasOne<MaintenanceWorkOrder>().WithMany().HasForeignKey(x => x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_MaintenanceLaborEntries_Actor", "([TechnicianUserId] IS NOT NULL AND [ExternalTechnicianName] IS NULL) OR ([TechnicianUserId] IS NULL AND [ExternalTechnicianName] IS NOT NULL)");
            table.HasCheckConstraint("CK_MaintenanceLaborEntries_Values", "[EndedAtUtc] >= [StartedAtUtc] AND [Hours] >= 0 AND [HourlyRate] >= 0 AND [TotalCost] >= 0");
        });
    }
}

internal sealed class MaintenancePlanConfiguration : IEntityTypeConfiguration<MaintenancePlan>
{
    public void Configure(EntityTypeBuilder<MaintenancePlan> builder)
    {
        builder.ConfigureOperational("Plans", "maintenance");
        builder.Property(x => x.Code).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DefaultOilQuantityLiters).HasPrecision(9, 3);
        builder.Property(x => x.ChecklistJson).HasColumnType("nvarchar(max)");
        builder.HasOne<VehicleModel>().WithMany().HasForeignKey(x => x.VehicleModelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint("CK_MaintenancePlans_Intervals", "([TriggerType] <> 3) OR ([ReminderAfterKilometers] > 0 AND [MaximumAfterKilometers] > [ReminderAfterKilometers])"));

        var seededAt = new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new MaintenancePlan
            {
                Id = MaintenancePlan.CarOilPlanId,
                Code = "OIL_CHANGE_CAR",
                NameAr = "تغيير زيت السيارة",
                NameEn = "Car oil change",
                VehicleType = VehicleType.Car,
                TriggerType = MaintenanceTriggerType.OdometerWindow,
                ReminderAfterKilometers = 4_000,
                MaximumAfterKilometers = 5_000,
                Status = CatalogStatus.Active,
                CreatedAtUtc = seededAt
            },
            new MaintenancePlan
            {
                Id = MaintenancePlan.MotorcycleOilPlanId,
                Code = "OIL_CHANGE_MOTORCYCLE",
                NameAr = "تغيير زيت الدراجة النارية",
                NameEn = "Motorcycle oil change",
                VehicleType = VehicleType.Motorcycle,
                TriggerType = MaintenanceTriggerType.OdometerWindow,
                ReminderAfterKilometers = 800,
                MaximumAfterKilometers = 1_000,
                Status = CatalogStatus.Active,
                CreatedAtUtc = seededAt
            });
    }
}

internal sealed class VehicleMaintenanceScheduleConfiguration : IEntityTypeConfiguration<VehicleMaintenanceSchedule>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenanceSchedule> builder)
    {
        builder.ConfigureOperational("VehicleSchedules", "maintenance");
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenancePlan>().WithMany().HasForeignKey(x => x.MaintenancePlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceWorkOrder>().WithMany().HasForeignKey(x => x.LastCompletedWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.MaintenancePlanId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class OilChangeOperationConfiguration : IEntityTypeConfiguration<OilChangeOperation>
{
    public void Configure(EntityTypeBuilder<OilChangeOperation> builder)
    {
        builder.ConfigureHistory("OilChangeOperations", "maintenance");
        builder.Property(x => x.OilQuantityLiters).HasPrecision(9, 3);
        builder.Property(x => x.OilCost).HasPrecision(18, 2);
        builder.Property(x => x.OilFilterCost).HasPrecision(18, 2);
        builder.Property(x => x.LaborCost).HasPrecision(18, 2);
        builder.Property(x => x.OtherCost).HasPrecision(18, 2);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne<MaintenanceWorkOrder>().WithMany().HasForeignKey(x => x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.OilInventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.OilFilterInventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceMaterialUsage>().WithMany().HasForeignKey(x => x.OilMaterialUsageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceMaterialUsage>().WithMany().HasForeignKey(x => x.OilFilterMaterialUsageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.MaintenanceWorkOrderId).IsUnique();
        builder.HasIndex(x => new { x.VehicleTypeSnapshot, x.OdometerAtChange });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_OilChangeOperations_Values", "[OdometerAtChange] >= 0 AND [OilQuantityLiters] > 0 AND [OilCost] >= 0 AND [OilFilterCost] >= 0 AND [LaborCost] >= 0 AND [OtherCost] >= 0 AND [TotalCost] >= 0");
            table.HasCheckConstraint("CK_OilChangeOperations_Filter", "([OilFilterChanged] = 0 AND [OilFilterInventoryItemId] IS NULL AND [OilFilterMaterialUsageId] IS NULL AND [OilFilterCost] = 0) OR ([OilFilterChanged] = 1 AND [OilFilterInventoryItemId] IS NOT NULL AND [OilFilterMaterialUsageId] IS NOT NULL)");
            table.HasCheckConstraint("CK_OilChangeOperations_CarQuantity", "[VehicleTypeSnapshot] <> 2 OR ([OilFilterChanged] = 0 AND [OilQuantityLiters] = 3.500) OR ([OilFilterChanged] = 1 AND [OilQuantityLiters] = 4.000)");
        });
    }
}

internal sealed class VehicleExpenseConfiguration : IEntityTypeConfiguration<VehicleExpense>
{
    public void Configure(EntityTypeBuilder<VehicleExpense> builder)
    {
        builder.ConfigureHistory("VehicleExpenses", "maintenance");
        builder.Property(x => x.ExpenseType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceEntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AmountBeforeTax).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RiderVehicleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleExpense>().WithMany().HasForeignKey(x => x.ReversalOfExpenseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.OccurredOn });
        builder.HasIndex(x => new { x.RiderProfileId, x.OccurredOn });
    }
}

internal sealed class ExternalPartSaleLineConfiguration : IEntityTypeConfiguration<ExternalPartSaleLine>
{
    public void Configure(EntityTypeBuilder<ExternalPartSaleLine> builder)
    {
        builder.ConfigureHistory("ExternalPartSaleLines", "maintenance");
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.SellingUnitPriceBeforeTax).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.InventoryCost).HasPrecision(18, 2);
        builder.Property(x => x.PartsGrossProfit).HasPrecision(18, 2);
        builder.HasOne<MaintenanceWorkOrder>().WithMany().HasForeignKey(x => x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceMaterialUsage>().WithMany().HasForeignKey(x => x.MaintenanceMaterialUsageId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint("CK_ExternalPartSaleLines_Values", "[Quantity] > 0 AND [SellingUnitPriceBeforeTax] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [InventoryCost] >= 0"));
    }
}

internal sealed class ExternalMaintenanceFinancialEntryConfiguration : IEntityTypeConfiguration<ExternalMaintenanceFinancialEntry>
{
    public void Configure(EntityTypeBuilder<ExternalMaintenanceFinancialEntry> builder)
    {
        builder.ConfigureHistory("ExternalFinancialEntries", "maintenance");
        builder.Property(x => x.AmountBeforeTax).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ExternalMechanicName).HasMaxLength(200);
        builder.HasOne<MaintenanceWorkOrder>().WithMany().HasForeignKey(x => x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.MechanicEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalMaintenanceFinancialEntry>().WithMany().HasForeignKey(x => x.ReversalOfEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.MaintenanceWorkOrderId, x.SourceType });
        builder.HasIndex(x => new { x.OccurredAtUtc, x.EntryType });
        builder.ToTable(table => table.HasCheckConstraint("CK_ExternalFinancialEntries_Reversal", "[ReversalOfEntryId] IS NOT NULL OR ([AmountBeforeTax] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0)"));
    }
}

internal sealed class ExternalCustomerPaymentConfiguration : IEntityTypeConfiguration<ExternalCustomerPayment>
{
    public void Configure(EntityTypeBuilder<ExternalCustomerPayment> builder)
    {
        builder.ConfigureHistory("ExternalCustomerPayments", "maintenance");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Reference).HasMaxLength(200);
        builder.HasOne<MaintenanceWorkOrder>().WithMany().HasForeignKey(x => x.MaintenanceWorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalCustomerPayment>().WithMany().HasForeignKey(x => x.ReversalOfPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.MaintenanceWorkOrderId, x.PaidAtUtc });
        builder.ToTable(table => table.HasCheckConstraint("CK_ExternalCustomerPayments_Amount", "[ReversalOfPaymentId] IS NOT NULL OR [Amount] > 0"));
    }
}
