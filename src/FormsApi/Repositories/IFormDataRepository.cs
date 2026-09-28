using System.Runtime.InteropServices;
using FormsApi.Models;

namespace FormsApi.Repositories;

public interface IFormDataRepository
{
    // Create a new form data entry
    Task<FormData> CreateAsync(FormData form);

    // Get a form data entry by its unique identifier
    Task<FormData?> GetByIdAsync(Guid id);

    // List form data entries with pagination and optional filtering by subject
    Task<(IReadOnlyList<FormData> Items, int TotalCount)> ListAsync(int page, int pageSize, string? subjectFilter);
    
    // Update an existing form data entry by its unique identifier.
    // expectedVersionKey is the VersionKey the caller last read for concurrency checks
    Task<FormData?> UpdateAsync(Guid id, FormData form, Guid expectedVersionKey);

    // Delete a form data entry by its unique identifier
    // Note that this is a soft delete,
    // meaning the entry will be marked as deleted but not removed from storage
    Task<bool> DeleteAsync(Guid id, Guid expectedVersionKey);
}
