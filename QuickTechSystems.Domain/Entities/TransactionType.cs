namespace QuickTechSystems.Domain.Entities
{
    public enum TransactionType
    {
        Sale,
        Purchase,
        Adjustment
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Cancelled
    }
}