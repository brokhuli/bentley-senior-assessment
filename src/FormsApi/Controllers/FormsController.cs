using FormsApi.Contracts;
using FormsApi.Models;
using FormsApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FormsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormsController : ControllerBase
{
    private readonly IFormDataRepository _repository;

    //TODO: Logger implementation and usage
    private readonly ILogger<FormsController> _logger;

    public FormsController(IFormDataRepository repository, ILogger<FormsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost] // POST /api/forms
    public async Task<IActionResult> Create([FromBody] CreateFormRequest request)
    {
        // Validation handled via DataAnnotations on CreateFormRequest

        // Create FormData object from request
        var formData = new FormData
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject,
            Description = request.Description,
            DueDate = request.DueDate,
            Priority = request.Priority,
            Critical = request.Critical,
            CreatedBy = request.CreatedBy
        };

        // Store in repository
        // TODO: handle exception?
        await _repository.CreateAsync(formData);

        // Return 201 Created with the new form's ID
        return CreatedAtAction(nameof(GetById), new { id = formData.Id }, formData);
    }

    [HttpGet("{id:guid}")] // GET /api/forms/{id}
    public async Task<IActionResult> GetById(Guid id)
    {
        // Query repository for form data by ID
        var formData = await _repository.GetByIdAsync(id);

        // If not found, return 404 Not Found
        if (formData == null)
        {
            return NotFound();
        }

        // If found, return 200 OK with form data
        return Ok(formData);
    }

    [HttpGet] // GET /api/forms
    public async Task<IActionResult> List([FromQuery] FormListQuery query)
    {
        // Validation handled via DataAnnotations on FormListQuery

        // Query repository for list of FormData (Page, PageSize, and filter)
        var (items, totalCount) = await _repository.ListAsync(query.Page, query.PageSize, query.SubjectFilter);

        // Either results found or empty set returned, 
        // return 200 OK with list of FormData and total count
        return Ok(new { Items = items, TotalCount = totalCount });
    }

    [HttpPut("{id:guid}")] // PUT /api/forms/{id}
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFormRequest request)
    {
        // Validation handled via DataAnnotations on UpdateFormRequest

        // Call Update on repository with ID and updated data
        //TODO: handle null subject issue
        var formData = await _repository.UpdateAsync(id, new FormData
        {
            Id = request.Id,
            Subject = request.Subject,
            Description = request.Description,
            DueDate = request.DueDate,
            Priority = request.Priority,
            Critical = request.Critical
        });

        // If form not found, return 404 Not Found
        if (formData == null)
        {
            return NotFound();
        }

        // If update successful, return 200 OK with updated form data
        return Ok(formData);
    }

    [HttpDelete("{id:guid}")] // DELETE /api/forms/{id}
    public async Task<IActionResult> Delete(Guid id)
    {
        // Validate Delete request
        if (id == Guid.Empty)
        {
            return BadRequest("Invalid ID");
        }

        // Call Delete on repository with ID
        bool success = await _repository.DeleteAsync(id);

        // If form not found, return 404 Not Found
        if (!success)
        {
            return NotFound();
        }

        // If delete successful, return 204 No Content
        return NoContent();
    }
}
