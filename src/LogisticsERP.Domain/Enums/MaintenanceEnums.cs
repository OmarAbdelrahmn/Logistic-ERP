namespace LogisticsERP.Domain.Enums;

public enum MaintenanceLocationType { Warehouse = 1, Workshop = 2, WarehouseAndWorkshop = 3 }
public enum MaintenanceServiceSubjectType { CompanyVehicle = 1, ExternalVehicle = 2 }
public enum MaintenanceType { Preventive = 1, Corrective = 2, Inspection = 3, AccidentRepair = 4, OilChange = 5, PartSaleOnly = 6 }
public enum MaintenanceWorkOrderStatus { Open = 1, InProgress = 2, Completed = 3, Closed = 4, Cancelled = 5 }
public enum InventoryItemType { SparePart = 1, RiderAccessory = 2, Oil = 3, Consumable = 4 }
public enum InventoryUnitOfMeasure { Piece = 1, Liter = 2, Barrel = 3, Box = 4, Set = 5 }
public enum StockMovementType
{
    PurchaseReceipt = 1,
    TransferOut = 2,
    TransferIn = 3,
    MaintenanceUsage = 4,
    RiderIssue = 5,
    SupplierReturn = 6,
    Reversal = 7,
    ExternalPartSale = 8,
    OilLoss = 9
}
public enum InventoryDocumentStatus { Posted = 1, Reversed = 2 }
public enum MaintenanceUsageType { SparePart = 1, Oil = 2, OilFilter = 3, Consumable = 4, ExternalPartSale = 5 }
public enum MaintenanceUsageDirection { Issue = 1, Reversal = 2 }
public enum InventoryAttributionStatus { AssignedRider = 1, Unassigned = 2, ExternalVehicle = 3 }
public enum MaintenanceTriggerType { Days = 1, Odometer = 2, OdometerWindow = 3, WhicheverComesFirst = 4 }
public enum MaintenanceDueStatus { Ok = 1, Due = 2, Overdue = 3, NeverDone = 4, OdometerMissing = 5 }
public enum ExternalFinancialEntryType { Income = 1, Expense = 2 }
public enum ExternalFinancialSourceType
{
    PartSaleRevenue = 1,
    CustomerLaborCharge = 2,
    InventoryCost = 3,
    MechanicLaborPayment = 4,
    OtherIncome = 5,
    OtherExpense = 6
}
public enum WorkshopPaymentMethod { Cash = 1, Card = 2, BankTransfer = 3, Other = 4 }
public enum WorkshopPaymentStatus { Unpaid = 1, PartiallyPaid = 2, Paid = 3, Refunded = 4 }
public enum OilBarrelStatus { Sealed = 1, Open = 2, Depleted = 3, Returned = 4 }
