using System.Collections.Concurrent;
using FormsApi.Models;

namespace FormsApi.Repositories;

// In-process in-memory implementation of repository.
// Simple but fully allows testing of the API without needing a database connection.
// Can be set with DI in Program.cs.
public class InProcessFormDataRepository : IFormDataRepository
{
    // Chose ConcurrentDictionary for thread-safety and quick lookups by Guid
    private readonly ConcurrentDictionary<Guid, FormData> _forms = new();

    // Create a new form data entry
    public async Task<FormData> CreateAsync(FormData form)
    {
        // Add new FormData to dictionary
        if (!_forms.TryAdd(form.Id, form))
        {
            throw new InvalidOperationException($"A form with id {form.Id} already exists.");
        }

        return form;
    }

    // Get a form data entry by its unique identifier
    public async Task<FormData?> GetByIdAsync(Guid id)
    {
        // Get the FormData from the ConcurrentDictionary by ID
        if (_forms.TryGetValue(id, out var formData))
        {
            return formData;
        }

        // If not found, return null
        return null;
    }

    // List form data entries with pagination and optional filtering by subject
    public async Task<(IReadOnlyList<FormData> Items, int TotalCount)> ListAsync(int page, int pageSize, string? subjectFilter)
    {
        // Query the ConcurrentDictionary for valid FormData entries
        var query = _forms.Values.Where(f => !f.IsDeleted);

        // Filter the ConcurrentDictionary based on subjectFilter if provided
        if (!string.IsNullOrEmpty(subjectFilter))
        {
            query = query.Where(f => f.Subject.Contains(subjectFilter));
        }

        var totalCount = query.Count();

        // Apply pagination (skip and take)
        var items = query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Return the paginated list of FormData and the total count
        return (items, totalCount);
    }

    // Update an existing form data record by its unique identifier
    public async Task<FormData?> UpdateAsync(Guid id, FormData form)
    {
        // Check if the form exists in the ConcurrentDictionary
        if (!_forms.TryGetValue(id, out var existingForm))
        {
            return null; // If not found, return null
        }

        // If found, update the existing FormData record with new values
        _forms[id] = form;

        return form;
    }

    // Delete a form data record by its unique identifier
    // Note that this is a soft delete, 
    // meaning the record will be marked as deleted but not removed from storage
    public async Task<bool> DeleteAsync(Guid id)
    {
        // Check if the form exists in the ConcurrentDictionary
        if (!_forms.TryGetValue(id, out var existingForm))
        {
            return false; // If not found, return false
        }

        // If found, mark the FormData entry as deleted (soft delete)
        if (!existingForm.IsDeleted)
        {
            existingForm.IsDeleted = true;
            existingForm.DeletedAt = DateTime.UtcNow;
        }

        // Return true to indicate successful deletion
        return true;
    }
}