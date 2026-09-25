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
        // Implement
        throw new NotImplementedException();
    }

    [HttpGet("{id:guid}")] // GET /api/forms/{id}
    public async Task<IActionResult> GetById(Guid id)
    {
        // Implement
        throw new NotImplementedException();
    }

    [HttpGet] // GET /api/forms
    public async Task<IActionResult> List([FromQuery] FormListQuery query)
    {
        // Implement
        throw new NotImplementedException();
    }

    [HttpPut("{id:guid}")] // PUT /api/forms/{id}
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFormRequest request)
    {
        // Implement
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")] // DELETE /api/forms/{id}
    public async Task<IActionResult> Delete(Guid id)
    {
        // Implement
        throw new NotImplementedException();
    }
}
