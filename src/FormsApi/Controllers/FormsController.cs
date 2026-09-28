using System.Security.Claims;
using FormsApi.Contracts;
using FormsApi.Exceptions;
using FormsApi.Models;
using FormsApi.Repositories;
using FormsApi.Validation;
using Microsoft.AspNetCore.Mvc;

namespace FormsApi.Controllers;

// This controller handles CRUD operations for form data entries. 
// It uses a repository for storage and supports basic validation and logging.
[ApiController]
[Route("api/[controller]")]
public class FormsController : ControllerBase
{
    private readonly IFormDataRepository _repository;
    private readonly ILogger<FormsController> _logger;

    public FormsController(IFormDataRepository repository, ILogger<FormsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // Placeholder methods for user authorization checks. 
    // Actual implementation out of scope.
    // Assuming here that users have full access to view or modify regardless
    // of the exact record ownsership.
    private bool UserCanCreate(ClaimsPrincipal user) => true;
    private bool UserCanView(ClaimsPrincipal user) => true;
    private bool UserCanModify(ClaimsPrincipal user) => true;
    private bool UserCanDelete(ClaimsPrincipal user) => true;

    [HttpPost] // POST /api/forms
    public async Task<IActionResult> Create([FromBody] CreateFormRequest request)
    {
        // Authorize user for creation request
        if (!UserCanCreate(User))
        {
            _logger.LogWarning("Unauthorized attempt to create form data entry by user: {User}", User.Identity?.Name);
            return Forbid(); // could be an exception, this seems cleaner as its an expected case
        }

        // Validation handled via DataAnnotations on CreateFormRequest

        // Create FormData object from request
        var formData = new FormData
        {
            Id = Guid.NewGuid(),
            Subject = InputSanitizer.Sanitize(request.Subject),
            Description = InputSanitizer.SanitizeOptional(request.Description),
            DueDate = request.DueDate,
            Priority = request.Priority,
            Critical = request.Critical,
            CreatedBy = InputSanitizer.Sanitize(request.CreatedBy),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            DeletedAt = null
        };

        // Store in repository, await to not block thread
        await _repository.CreateAsync(formData);

        _logger.LogInformation("Created new form data entry with ID: {FormId}", formData.Id);

        // Return 201 Created with the new form location and data
        return CreatedAtAction(nameof(GetById), new { id = formData.Id }, formData);
    }

    [HttpGet("{id:guid}")] // GET /api/forms/{id}
    public async Task<IActionResult> GetById(Guid id)
    {
        // Authorize user for viewing request
        if (!UserCanView(User))
        {
            _logger.LogWarning("Unauthorized attempt to view form data entry with ID: {FormId} by user: {User}", id, User.Identity?.Name);
            return Forbid();
        }

        // Query repository for form data by ID
        var formData = await _repository.GetByIdAsync(id);

        // If not found, throw, handled centrally by ApiExceptionHandler
        if (formData == null)
        {
            _logger.LogWarning("Form data entry with ID: {FormId} not found", id);
            throw new FormNotFoundException(id); // tradeoff of exception overhead vs clean handling in comparison to 'return NotFound()'
        }

        // If found, return 200 OK with form data
        return Ok(formData);
    }

    [HttpGet] // GET /api/forms
    public async Task<IActionResult> List([FromQuery] FormListQuery query)
    {
        // Authorize user for list viewing request
        if (!UserCanView(User))
        {
            _logger.LogWarning("Unauthorized attempt to list form data entries by user: {User}", User.Identity?.Name);
            return Forbid();
        }

        // Validation handled via DataAnnotations on FormListQuery

        // Query repository for list of FormData (Page, PageSize, and filter)
        var (items, totalCount) = await _repository.ListAsync(query.Page, query.PageSize, query.SubjectFilter);

        // Either results found or empty set returned, 
        // return 200 OK with list of FormData and total count
        return Ok(new { Items = items, TotalCount = totalCount });
    }

    // PUT method to support full updates of form data entries
    // PUT takes the null params from UpdateFormRequest and replaces the existing values with nulls,
    // while PATCH takes the null params from PatchFormRequest and ignores them, keeping the existing values.
    [HttpPut("{id:guid}")] // PUT /api/forms/{id}
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFormRequest request)
    {
        // Authorize user for modification request
        if (!UserCanModify(User))
        {
            _logger.LogWarning("Unauthorized attempt to update form data entry with ID: {FormId} by user: {User}", id, User.Identity?.Name);
            return Forbid();
        }

        // Validation handled via DataAnnotations on UpdateFormRequest

        // retrieve existing form data from repository
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Attempted to update non-existent form data entry with ID: {FormId}", id);
            throw new FormNotFoundException(id);
        }

        if (existing.IsDeleted) // soft deleted records can't be updated
        {
            _logger.LogWarning("Attempted to update deleted form data entry with ID: {FormId}", id);
            throw new FormConflictException($"Form '{id}' has been deleted and cannot be updated.");
        }

        // Merge existing data with new data from request
        var updatedFormData = new FormData
        {
            Id = id,
            Subject = InputSanitizer.Sanitize(request.Subject),
            Description = InputSanitizer.SanitizeOptional(request.Description),
            DueDate = request.DueDate,
            Priority = request.Priority,
            Critical = request.Critical,
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = existing.IsDeleted,
            DeletedAt = existing.DeletedAt
        };

        // Call Update on repository with ID, updated data, and concurrency token
        var formData = await _repository.UpdateAsync(id, updatedFormData, request.VersionKey);

        // If form not found, throw
        if (formData == null)
        {
            _logger.LogWarning("Failed to update form data entry with ID: {FormId} - not found", id);
            throw new FormNotFoundException(id);
        }

        // If update successful, return 200 OK with updated form data
        return Ok(formData);
    }

    // Added Patch method to support partial updates of form data entries
    // as Put is intended for full replacement of the resource.
    [HttpPatch("{id:guid}")] // PATCH /api/forms/{id}
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchFormRequest request)
    {
        // Authorize user for modification request
        if (!UserCanModify(User))
        {
            _logger.LogWarning("Unauthorized attempt to patch form data entry with ID: {FormId} by user: {User}", id, User.Identity?.Name);
            return Forbid();
        }

        // Validation handled via DataAnnotations on PatchFormRequest

        // retrieve existing form data from repository
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Attempted to patch non-existent form data entry with ID: {FormId}", id);
            throw new FormNotFoundException(id);
        }

        if (existing.IsDeleted) // soft deleted records can't be updated
        {
            _logger.LogWarning("Attempted to patch deleted form data entry with ID: {FormId}", id);
            throw new FormConflictException($"Form '{id}' has been deleted and cannot be patched.");
        }

        // Merge existing data with new data from request
        var updatedFormData = new FormData
        {
            Id = id,
            Subject = request.Subject is null ? existing.Subject : InputSanitizer.Sanitize(request.Subject),
            Description = request.Description is null ? existing.Description : InputSanitizer.SanitizeOptional(request.Description),
            DueDate = request.DueDate ?? existing.DueDate,
            Priority = request.Priority ?? existing.Priority,
            Critical = request.Critical ?? existing.Critical,
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = existing.IsDeleted,
            DeletedAt = existing.DeletedAt
        };

        // Call Update on repository with ID, updated data, and concurrency token
        var formData = await _repository.UpdateAsync(id, updatedFormData, request.VersionKey);

        // If form not found, throw
        if (formData == null)
        {
            _logger.LogWarning("Failed to patch form data entry with ID: {FormId} - not found", id);
            throw new FormNotFoundException(id);
        }

        // If patch successful, return 200 OK with updated form data
        return Ok(formData);
    }

    [HttpDelete("{id:guid}")] // DELETE /api/forms/{id}
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid expectedVersionKey)
    {
        // Authorize user for deletion request
        if (!UserCanDelete(User))
        {
            _logger.LogWarning("Unauthorized attempt to delete form data entry with ID: {FormId} by user: {User}", id, User.Identity?.Name);
            return Forbid();
        }

        // Validate Delete request
        if (id == Guid.Empty)
        {
            return BadRequest("Invalid ID");
        }

        // Check if the form data entry exists before attempting deletion
        var formData = await _repository.GetByIdAsync(id);
        if (formData == null)
        {
            _logger.LogWarning("Attempted to delete non-existent form data entry with ID: {FormId}", id);
            throw new FormNotFoundException(id);
        }

        // If form data entry is already soft deleted, return 204 No Content
        // This is idempotent behavior, as the resource is already in the desired state (deleted)
        if (formData.IsDeleted)
        {
            return NoContent();
        }

        // Call Delete on repository with ID and concurrency token
        bool success = await _repository.DeleteAsync(id, expectedVersionKey);

        // If form not found, throw - handled centrally by ApiExceptionHandler
        if (!success)
        {
            _logger.LogWarning("Attempted to delete non-existent form data entry with ID: {FormId}", id);
            throw new FormNotFoundException(id);
        }

        // If delete successful, return 204 No Content
        return NoContent();
    }
}
