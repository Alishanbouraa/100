using System;

namespace QuickTechSystems.Domain.Entities
{
    public class InventoryHistory
    {
        public int InventoryHistoryId { get; set; }
        public int ProductId { get; set; }
        public decimal QuantityChange { get; set; }
        public decimal NewQuantity { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual Product Product { get; set; } = null!;
    }
}