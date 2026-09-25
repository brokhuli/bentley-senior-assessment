using FormsApi.Contracts;
using FormsApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FormsApi.Controllers;

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

    [HttpPost] // POST /api/forms
    public async Task<IActionResult> Create([FromBody] CreateFormRequest request)
    {
        // Validate CreateFormRequest params

        // Create FormData object from request

        // Store in repository

        // Return appropriate response (201 Created with the new form's ID)

        throw new NotImplementedException();
    }

    [HttpGet("{id:guid}")] // GET /api/forms/{id}
    public async Task<IActionResult> GetById(Guid id)
    {
        // Query repository for form data by ID

        // If found, return 200 OK with form data

        // If not found, return 404 Not Found

        throw new NotImplementedException();
    }

    [HttpGet] // GET /api/forms
    public async Task<IActionResult> List([FromQuery] FormListQuery query)
    {
        // Validate FormListQuery params (Page, PageSize, SubjectFilter)

        // Query repository for list of FormData (Page, PageSize, and filter)

        // If empty set returned, return 200 OK with empty list and total count of 0

        // If results found, return 200 OK with list of FormData and total count

        throw new NotImplementedException();
    }

    [HttpPut("{id:guid}")] // PUT /api/forms/{id}
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFormRequest request)
    {
        // Validate UpdateFormRequest params

        // Call Update on repository with ID and updated data

        // If form not found, return 404 Not Found

        // If update successful, return 200 OK with updated form data
        
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")] // DELETE /api/forms/{id}
    public async Task<IActionResult> Delete(Guid id)
    {
        // Validate Delete request

        // Call Delete on repository with ID

        // If form not found, return 404 Not Found

        // If delete successful, return 204 No Content
        
        throw new NotImplementedException();
    }
}
