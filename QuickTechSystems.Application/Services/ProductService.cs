using System.Diagnostics;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Events;
using QuickTechSystems.Application.Interfaces;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Interfaces.Repositories;

namespace QuickTechSystems.Application.Services
{
    public class ProductService : BaseService<Product, ProductDTO>, IProductService
    {
        public ProductService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IEventAggregator eventAggregator,
            IDbContextScopeService dbContextScopeService)
            : base(unitOfWork, mapper, eventAggregator, dbContextScopeService)
        {
        }

        public async Task<ProductDTO?> GetByBarcodeAsync(string barcode)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var product = await _repository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);
                return _mapper.Map<ProductDTO>(product);
            });
        }

        public async Task<IEnumerable<ProductDTO>> GetByCategoryAsync(int categoryId)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var products = await _repository.Query()
                    .Where(p => p.CategoryId == categoryId)
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                return _mapper.Map<IEnumerable<ProductDTO>>(products);
            });
        }

        public async Task<IEnumerable<ProductDTO>> GetBySupplierAsync(int supplierId)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var products = await _repository.Query()
                    .Where(p => p.SupplierId == supplierId)
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                return _mapper.Map<IEnumerable<ProductDTO>>(products);
            });
        }

        public async Task<IEnumerable<ProductDTO>> GetActiveAsync()
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var products = await _repository.Query()
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                return _mapper.Map<IEnumerable<ProductDTO>>(products);
            });
        }

        public async Task<IEnumerable<ProductDTO>> GetLowStockAsync()
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var products = await _repository.Query()
                    .Where(p => p.CurrentStock <= p.MinimumStock && p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                return _mapper.Map<IEnumerable<ProductDTO>>(products);
            });
        }

        public async Task<IEnumerable<ProductDTO>> SearchByNameAsync(string name)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var products = await _repository.Query()
                    .Where(p => p.Name.Contains(name) && p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                return _mapper.Map<IEnumerable<ProductDTO>>(products);
            });
        }

        public async Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeId = null)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var query = _repository.Query().Where(p => p.Barcode == barcode);
                if (excludeId.HasValue)
                {
                    query = query.Where(p => p.ProductId != excludeId.Value);
                }
                return !await query.AnyAsync();
            });
        }

        public async Task<bool> TransferFromStorehouseAsync(int productId, decimal quantity)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var product = await _repository.GetByIdAsync(productId);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product with ID {productId} not found");
                    }

                    if (product.Storehouse < quantity)
                    {
                        throw new InvalidOperationException("Insufficient quantity in storehouse");
                    }

                    if (quantity <= 0)
                    {
                        throw new InvalidOperationException("Transfer quantity must be greater than zero");
                    }

                    product.Storehouse -= quantity;
                    product.CurrentStock += quantity;
                    product.UpdatedAt = DateTime.Now;

                    await _repository.UpdateAsync(product);

                    var inventoryHistory = new InventoryHistory
                    {
                        ProductId = productId,
                        QuantityChange = quantity,
                        NewQuantity = product.CurrentStock,
                        Type = "Transfer",
                        Notes = $"Transferred {quantity} items from storehouse to stock",
                        Timestamp = DateTime.Now
                    };

                    await _unitOfWork.Context.Set<InventoryHistory>().AddAsync(inventoryHistory);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var productDto = _mapper.Map<ProductDTO>(product);
                    _eventAggregator.Publish(new EntityChangedEvent<ProductDTO>("Update", productDto));

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Debug.WriteLine($"Error transferring from storehouse: {ex}");
                    throw;
                }
            });
        }

        public async Task<bool> TransferBoxesFromStorehouseAsync(int productId, int boxQuantity)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var product = await _repository.GetByIdAsync(productId);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product with ID {productId} not found");
                    }

                    if (product.ItemsPerBox <= 0)
                    {
                        throw new InvalidOperationException("Items per box must be configured for box transfers");
                    }

                    var totalItemsToTransfer = boxQuantity * product.ItemsPerBox;

                    if (product.Storehouse < totalItemsToTransfer)
                    {
                        throw new InvalidOperationException("Insufficient quantity in storehouse for box transfer");
                    }

                    if (boxQuantity <= 0)
                    {
                        throw new InvalidOperationException("Box quantity must be greater than zero");
                    }

                    decimal availableBoxes = Math.Floor(product.Storehouse / product.ItemsPerBox);
                    if (boxQuantity > availableBoxes)
                    {
                        throw new InvalidOperationException($"Insufficient complete boxes in storehouse. Available: {availableBoxes} boxes");
                    }

                    product.Storehouse -= totalItemsToTransfer;
                    product.CurrentStock += totalItemsToTransfer;
                    product.NumberOfBoxes -= boxQuantity;
                    product.UpdatedAt = DateTime.Now;

                    await _repository.UpdateAsync(product);

                    var inventoryHistory = new InventoryHistory
                    {
                        ProductId = productId,
                        QuantityChange = totalItemsToTransfer,
                        NewQuantity = product.CurrentStock,
                        Type = "BoxTransfer",
                        Notes = $"Transferred {boxQuantity} boxes ({totalItemsToTransfer} items) from storehouse to stock",
                        Timestamp = DateTime.Now
                    };

                    await _unitOfWork.Context.Set<InventoryHistory>().AddAsync(inventoryHistory);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var productDto = _mapper.Map<ProductDTO>(product);
                    _eventAggregator.Publish(new EntityChangedEvent<ProductDTO>("Update", productDto));

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Debug.WriteLine($"Error transferring boxes from storehouse: {ex}");
                    throw;
                }
            });
        }

        public async Task<ProductDTO> GenerateBarcodeAsync(ProductDTO product)
        {
            if (string.IsNullOrEmpty(product.Barcode))
            {
                product.Barcode = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
            }
            return product;
        }

        public override async Task<IEnumerable<ProductDTO>> GetAllAsync()
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var products = await _repository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                return _mapper.Map<IEnumerable<ProductDTO>>(products);
            });
        }

        public override async Task<ProductDTO?> GetByIdAsync(int id)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                var product = await _repository.Query()
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductId == id);
                return _mapper.Map<ProductDTO>(product);
            });
        }

        public override async Task<ProductDTO> CreateAsync(ProductDTO dto)
        {
            return await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                try
                {
                    var isUnique = await IsBarcodeUniqueAsync(dto.Barcode);
                    if (!isUnique)
                    {
                        throw new InvalidOperationException($"A product with barcode '{dto.Barcode}' already exists.");
                    }

                    var entity = _mapper.Map<Product>(dto);
                    entity.CreatedAt = DateTime.Now;

                    var result = await _repository.AddAsync(entity);
                    await _unitOfWork.SaveChangesAsync();

                    var resultDto = _mapper.Map<ProductDTO>(result);
                    _eventAggregator.Publish(new EntityChangedEvent<ProductDTO>("Create", resultDto));

                    return resultDto;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating product: {ex.Message}");
                    throw;
                }
            });
        }

        public override async Task UpdateAsync(ProductDTO dto)
        {
            await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                try
                {
                    var isUnique = await IsBarcodeUniqueAsync(dto.Barcode, dto.ProductId);
                    if (!isUnique)
                    {
                        throw new InvalidOperationException($"Another product with barcode '{dto.Barcode}' already exists.");
                    }

                    var product = await _repository.GetByIdAsync(dto.ProductId);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product with ID {dto.ProductId} not found");
                    }

                    _mapper.Map(dto, product);
                    product.UpdatedAt = DateTime.Now;

                    await _repository.UpdateAsync(product);
                    await _unitOfWork.SaveChangesAsync();

                    _eventAggregator.Publish(new EntityChangedEvent<ProductDTO>("Update", dto));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating product: {ex}");
                    throw;
                }
            });
        }

        public override async Task DeleteAsync(int id)
        {
            await _dbContextScopeService.ExecuteInScopeAsync(async context =>
            {
                try
                {
                    var product = await _unitOfWork.Context.Set<Product>()
                        .Include(p => p.TransactionDetails)
                        .FirstOrDefaultAsync(p => p.ProductId == id);

                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product with ID {id} not found");
                    }

                    if (product.TransactionDetails.Any())
                    {
                        throw new InvalidOperationException(
                            "Cannot delete product because it has associated transactions. " +
                            "Please mark it as inactive instead.");
                    }

                    await _repository.DeleteAsync(product);
                    await _unitOfWork.SaveChangesAsync();

                    var productDto = _mapper.Map<ProductDTO>(product);
                    _eventAggregator.Publish(new EntityChangedEvent<ProductDTO>("Delete", productDto));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting product: {ex}");
                    throw;
                }
            });
        }
    }
}