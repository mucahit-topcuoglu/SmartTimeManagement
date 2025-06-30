using SmartTimeManagement.Core.Entities;

namespace SmartTimeManagement.Core.Interfaces;

public interface ICategoryService
{
    Task<Category> CreateAsync(Category category);
    Task<Category?> GetByIdAsync(int id);
    Task<IEnumerable<Category>> GetAllAsync();
    Task<IEnumerable<Category>> GetActiveAsync();
    Task<Category> UpdateAsync(Category category);
    Task<bool> DeleteAsync(int id);
    Task<bool> CategoryExistsAsync(string name);
}
