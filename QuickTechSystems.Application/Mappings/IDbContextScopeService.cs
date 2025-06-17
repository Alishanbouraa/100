// QuickTechSystems.Application/Interfaces/IDbContextScopeService.cs
using System;
using System.Threading.Tasks;

namespace QuickTechSystems.Application.Mappings
{
    public interface IDbContextScopeService
    {
        Task<T> ExecuteInScopeAsync<T>(Func<object, Task<T>> operation);
        Task ExecuteInScopeAsync(Func<object, Task> operation);
    }
}