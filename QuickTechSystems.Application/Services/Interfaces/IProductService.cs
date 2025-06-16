using QuickTechSystems.Application.DTOs;

namespace QuickTechSystems.Application.Services.Interfaces
{
    public interface IProductService : IBaseService<ProductDTO>
    {
        Task<ProductDTO?> GetByBarcodeAsync(string barcode);
        Task<IEnumerable<ProductDTO>> GetByCategoryAsync(int categoryId);
        Task<IEnumerable<ProductDTO>> GetBySupplierAsync(int supplierId);
        Task<IEnumerable<ProductDTO>> GetActiveAsync();
        Task<IEnumerable<ProductDTO>> GetLowStockAsync();
        Task<IEnumerable<ProductDTO>> SearchByNameAsync(string name);
        Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeId = null);
        Task<bool> TransferFromStorehouseAsync(int productId, decimal quantity);
        Task<bool> TransferBoxesFromStorehouseAsync(int productId, int boxQuantity);
        Task<ProductDTO> GenerateBarcodeAsync(ProductDTO product);
    }
}