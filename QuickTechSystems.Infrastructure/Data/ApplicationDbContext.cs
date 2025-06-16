using Microsoft.EntityFrameworkCore;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Infrastructure.Data.Configurations;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace QuickTechSystems.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionDetail> TransactionDetails => Set<TransactionDetail>();
        public DbSet<InventoryHistory> InventoryHistories { get; set; }
        public DbSet<BusinessSetting> BusinessSettings => Set<BusinessSetting>();
        public DbSet<SystemPreference> SystemPreferences => Set<SystemPreference>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<SupplierTransaction> SupplierTransactions => Set<SupplierTransaction>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Drawer> Drawers => Set<Drawer>();
        public DbSet<DrawerHistoryEntry> DrawerHistory => Set<DrawerHistoryEntry>();
        public DbSet<DrawerTransaction> DrawerTransactions => Set<DrawerTransaction>();
        public DbSet<EmployeeSalaryTransaction> EmployeeSalaryTransactions => Set<EmployeeSalaryTransaction>();
        public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
        public DbSet<SupplierInvoice> SupplierInvoices { get; set; }
        public DbSet<SupplierInvoiceDetail> SupplierInvoiceDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            modelBuilder.ApplyConfiguration(new SupplierInvoiceConfiguration());
            modelBuilder.ApplyConfiguration(new SupplierInvoiceDetailConfiguration());
            modelBuilder.ApplyConfiguration(new InventoryHistoryConfiguration());
        }

        public DbContextOptions<ApplicationDbContext> GetConfiguration()
        {
            return (DbContextOptions<ApplicationDbContext>)this.GetType()
                .GetProperty("ContextOptions")
                .GetValue(this);
        }
    }

    public static class DatabaseInitializationExtensions
    {
        private static readonly Dictionary<string, bool> InitializationCache = new Dictionary<string, bool>();
        private static readonly object InitializationLock = new object();

        public static async Task InitializeDatabaseAsync(this ApplicationDbContext context)
        {
            var connectionString = context.Database.GetConnectionString();
            var cacheKey = $"init_{connectionString?.GetHashCode()}";

            lock (InitializationLock)
            {
                if (InitializationCache.ContainsKey(cacheKey) && InitializationCache[cacheKey])
                {
                    return;
                }
            }

            await context.Database.EnsureCreatedAsync();

            lock (InitializationLock)
            {
                InitializationCache[cacheKey] = true;
            }
        }

        public static async Task SeedDefaultAdministratorAsync(this ApplicationDbContext context)
        {
            if (!await context.Employees.AnyAsync())
            {
                var administratorEntity = new Employee
                {
                    Username = "admin",
                    PasswordHash = HashSecurePassword("admin123"),
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = "Manager",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await context.Employees.AddAsync(administratorEntity);
                await context.SaveChangesAsync();
            }
        }

        private static string HashSecurePassword(string password)
        {
            using var hashAlgorithm = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hashedBytes = hashAlgorithm.ComputeHash(passwordBytes);
            return Convert.ToBase64String(hashedBytes);
        }
    }
}