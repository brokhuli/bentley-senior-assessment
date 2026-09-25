using System.Collections.Concurrent;
using FormsApi.Models;

namespace FormsApi.Repositories;

public class LocalFormDataRepository : IFormDataRepository
{
    // Chose ConcurrentDictionary for thread-safety and quick lookups by Guid
    private readonly ConcurrentDictionary<Guid, FormData> _forms = new();

    // Create a new form data entry
    public async Task<FormData> CreateAsync(FormData form)
    {
        // Create a new FormData entry

        // Add to the ConcurrentDictionary

        // Return the created FormData object

        throw new NotImplementedException();
    }

    // Get a form data entry by its unique identifier
    public async Task<FormData?> GetByIdAsync(Guid id)
    {
        // Get the FormData from the ConcurrentDictionary by ID

        // If not found, return null

        // If found, return the FormData object

        throw new NotImplementedException();
    }

    // List form data entries with pagination and optional filtering by subject
    public async Task<(IReadOnlyList<FormData> Items, int TotalCount)> ListAsync(int page, int pageSize, string? subjectFilter)
    {
        // Validate page and pageSize parameters

        // Filter the ConcurrentDictionary based on subjectFilter if provided

        // Apply pagination (skip and take)

        // Return the paginated list of FormData and the total count

        throw new NotImplementedException();
    }

    // Update an existing form data entry by its unique identifier
    public async Task<FormData?> UpdateAsync(Guid id, FormData form)
    {
        // Check if the form exists in the ConcurrentDictionary

        // If not found, return null

        // If found, update the existing FormData entry with new values

        // Return the updated FormData object

        throw new NotImplementedException();
    }

    // Delete a form data entry by its unique identifier
    // Note that this is a soft delete, 
    // meaning the entry will be marked as deleted but not removed from storage
    public async Task<bool> DeleteAsync(Guid id)
    {
        // Check if the form exists in the ConcurrentDictionary

        // If not found, return false

        // If found, mark the FormData entry as deleted (soft delete)

        // Return true to indicate successful deletion

        throw new NotImplementedException();
    }
}