using FormsApi.Contracts;
using FormsApi.Controllers;
using FormsApi.Repositories;
using FormsApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Moq;
using Microsoft.AspNetCore.Mvc;

namespace FormsApi.Tests.Controllers;

public class FormsControllerTests
{
    private readonly Mock<IFormDataRepository> _repository = new();
    private readonly Mock<ILogger<FormsController>> _logger = new();
    private readonly FormsController _controller;

    public FormsControllerTests()
    {
        _controller = new FormsController(_repository.Object, _logger.Object);
    }

    [Fact]
    public async Task Test_CreateFormsController_ReturnsInstance()
    {
        var request = new CreateFormRequest
        (
            "Test Subject",
            "Test Description",
            DateTime.UtcNow.AddDays(1),
            5,
            false,
            "TestUser"
        );

        var result = await _controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(FormsController.GetById), createdResult.ActionName);

        var formData = Assert.IsType<FormData>(createdResult.Value);
        Assert.Equal(request.Subject, formData.Subject);
        Assert.Equal(request.Description, formData.Description);
        Assert.Equal(request.DueDate, formData.DueDate);
        Assert.Equal(request.Priority, formData.Priority);
        Assert.Equal(request.Critical, formData.Critical);
        Assert.Equal(request.CreatedBy, formData.CreatedBy);
        Assert.NotEqual(Guid.Empty, formData.Id);

        _repository.Verify(r => r.CreateAsync(It.Is<FormData>(f => f.Subject == request.Subject)), Times.Once);
    }

    [Fact]
    public async Task Test_GetById_ReturnsOk_WhenFormExists()
    {
        var id = Guid.NewGuid();
        var formData = new FormData
        {
            Id = id,
            Subject = "Test Subject",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow
        };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(formData);

        var result = await _controller.GetById(id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(formData, okResult.Value);
    }

    [Fact]
    public async Task Test_GetById_ReturnsNotFound_WhenFormDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((FormData?)null);

        var result = await _controller.GetById(id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Test_List_ReturnsOk_WithItemsAndTotalCount()
    {
        var query = new FormListQuery(Page: 1, PageSize: 20, SubjectFilter: null);
        var items = new List<FormData>
        {
            new() { Id = Guid.NewGuid(), Subject = "Test Subject", CreatedBy = "TestUser", CreatedAt = DateTime.UtcNow }
        };

        _repository.Setup(r => r.ListAsync(query.Page, query.PageSize, query.SubjectFilter))
            .ReturnsAsync((items, items.Count));

        var result = await _controller.List(query);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Test_Update_ReturnsOk_WhenFormExistsAndNotDeleted()
    {
        var id = Guid.NewGuid();
        var existing = new FormData
        {
            Id = id,
            Subject = "Original Subject",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        var request = new UpdateFormRequest("Updated Subject", null, null, null, null);
        var updated = new FormData
        {
            Id = id,
            Subject = "Updated Subject",
            CreatedBy = "TestUser",
            CreatedAt = existing.CreatedAt
        };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repository.Setup(r => r.UpdateAsync(id, It.IsAny<FormData>())).ReturnsAsync(updated);

        var result = await _controller.Update(id, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(updated, okResult.Value);
    }

    [Fact]
    public async Task Test_Update_ReturnsNotFound_WhenFormDoesNotExist()
    {
        var id = Guid.NewGuid();
        var request = new UpdateFormRequest("Updated Subject", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((FormData?)null);

        var result = await _controller.Update(id, request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Test_Update_ReturnsNotFound_WhenFormIsSoftDeleted()
    {
        var id = Guid.NewGuid();
        var existing = new FormData
        {
            Id = id,
            Subject = "Original Subject",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = true
        };
        var request = new UpdateFormRequest("Updated Subject", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

        var result = await _controller.Update(id, request);

        Assert.IsType<NotFoundResult>(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<FormData>()), Times.Never);
    }

    [Fact]
    public async Task Test_Delete_ReturnsBadRequest_WhenIdIsEmpty()
    {
        var result = await _controller.Delete(Guid.Empty);

        Assert.IsType<BadRequestObjectResult>(result);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Delete_ReturnsNoContent_WhenDeleteSucceeds()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Test_Delete_ReturnsNotFound_WhenFormDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        var result = await _controller.Delete(id);

        Assert.IsType<NotFoundResult>(result);
    }
}
