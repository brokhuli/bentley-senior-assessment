using FormsApi.Data;
using FormsApi.Exceptions;
using FormsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FormsApi.Repositories;

// Relational database implementation of repository.
// Uses EF Core and SQLite for storage.
// Can be set with DI in Program.cs.
public class RelationalFormDataRepository : IFormDataRepository
{
    private readonly FormsApiDbContext _dbContext;

    public RelationalFormDataRepository(FormsApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Create a new form data entry
    public async Task<FormData> CreateAsync(FormData form)
    {
        // Add new FormData to database
        _dbContext.Forms.Add(form);
        await _dbContext.SaveChangesAsync();

        return form;
    }

    // Get a form data entry by its unique identifier
    public async Task<FormData?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Forms.SingleOrDefaultAsync(f => f.Id == id);
    }

    // List form data entries with pagination and optional filtering by subject
    public async Task<(IReadOnlyList<FormData> Items, int TotalCount)> ListAsync(int page, int pageSize, string? subjectFilter)
    {
        var query = _dbContext.Forms.Where(f => !f.IsDeleted);

        // Filter the query based on subjectFilter if provided
        if (!string.IsNullOrEmpty(subjectFilter))
        {
            query = query.Where(f => f.Subject.Contains(subjectFilter));
        }

        var totalCount = await query.CountAsync();

        // Apply pagination (skip and take)
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // Update an existing form data entry by its unique identifier
    public async Task<FormData?> UpdateAsync(Guid id, FormData form, Guid expectedVersionKey)
    {
        // Check if the form exists in the db
        var existingForm = await _dbContext.Forms.SingleOrDefaultAsync(f => f.Id == id);
        if (existingForm is null)
        {
            return null;
        }

        // If found, update the existing FormData record with new values
        _dbContext.Entry(existingForm).CurrentValues.SetValues(form);

        // Bump the concurrency token, and set the expected version key
        existingForm.VersionKey = Guid.NewGuid();
        _dbContext.Entry(existingForm).Property(f => f.VersionKey).OriginalValue = expectedVersionKey;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new FormPreconditionFailedException(id);
        }

        return existingForm;
    }

    // Delete a form data entry by its unique identifier
    // Note that this is a soft delete,
    // meaning the entry will be marked as deleted but not removed from storage
    public async Task<bool> DeleteAsync(Guid id, Guid expectedVersionKey)
    {
        // Check if the form exists in the db
        var existingForm = await _dbContext.Forms.SingleOrDefaultAsync(f => f.Id == id);
        if (existingForm is null)
        {
            return false;
        }

        // If found, mark the FormData entry as deleted (soft delete)
        if (!existingForm.IsDeleted)
        {
            existingForm.IsDeleted = true;
            existingForm.DeletedAt = DateTime.UtcNow;

            // Bump the concurrency token, and set the expected version key
            existingForm.VersionKey = Guid.NewGuid();
            _dbContext.Entry(existingForm).Property(f => f.VersionKey).OriginalValue = expectedVersionKey;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new FormPreconditionFailedException(id);
            }
        }

        return true;
    }
}
