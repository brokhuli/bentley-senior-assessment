using FormsApi.Contracts;
using FormsApi.Controllers;
using FormsApi.Exceptions;
using FormsApi.Repositories;
using FormsApi.Models;
using Microsoft.AspNetCore.Http;
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
        _controller = new FormsController(_repository.Object, _logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // Sets the If-Match request header the controller reads as the caller's expected VersionKey.
    private void SetIfMatchHeader(Guid versionKey) =>
        _controller.ControllerContext.HttpContext.Request.Headers.IfMatch = $"\"{versionKey}\"";

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
    public async Task Test_Create_SanitizesInput()
    {
        var request = new CreateFormRequest
        (
            "  Test Subject  ",
            "Line1\r\nLine2\rLine3",
            DateTime.UtcNow.AddDays(1),
            5,
            false,
            "  TestUser  "
        );

        var result = await _controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var formData = Assert.IsType<FormData>(createdResult.Value);

        Assert.Equal("Test Subject", formData.Subject);
        Assert.Equal("Line1\nLine2\nLine3", formData.Description);
        Assert.Equal("TestUser", formData.CreatedBy);
    }

    [Fact]
    public async Task Test_Create_ReturnsInstance_WhenOptionalFieldsAreNull()
    {
        var request = new CreateFormRequest
        (
            "Test Subject",
            null,
            null,
            null,
            null,
            "TestUser"
        );

        var result = await _controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var formData = Assert.IsType<FormData>(createdResult.Value);

        Assert.Null(formData.Description);
        Assert.Null(formData.DueDate);
        Assert.Null(formData.Priority);
        Assert.Null(formData.Critical);
        Assert.False(formData.IsDeleted);
        Assert.Null(formData.DeletedAt);
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
    public async Task Test_GetById_Throws_WhenFormDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((FormData?)null);

        await Assert.ThrowsAsync<FormNotFoundException>(() => _controller.GetById(id));
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
    public async Task Test_List_PassesQueryParametersToRepository()
    {
        var query = new FormListQuery(Page: 2, PageSize: 10, SubjectFilter: "Test");
        var items = new List<FormData>
        {
            new() { Id = Guid.NewGuid(), Subject = "Test Subject", CreatedBy = "TestUser", CreatedAt = DateTime.UtcNow }
        };

        _repository.Setup(r => r.ListAsync(2, 10, "Test")).ReturnsAsync((items, items.Count));

        var result = await _controller.List(query);

        Assert.IsType<OkObjectResult>(result);
        _repository.Verify(r => r.ListAsync(2, 10, "Test"), Times.Once);
    }

    [Fact]
    public async Task Test_List_ReturnsOk_WithEmptyResults_WhenNoFormsMatch()
    {
        var query = new FormListQuery(Page: 1, PageSize: 20, SubjectFilter: "NoMatch");

        _repository.Setup(r => r.ListAsync(query.Page, query.PageSize, query.SubjectFilter))
            .ReturnsAsync((new List<FormData>(), 0));

        var result = await _controller.List(query);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var itemsProperty = okResult.Value!.GetType().GetProperty("Items");
        var totalCountProperty = okResult.Value!.GetType().GetProperty("TotalCount");

        var itemsValue = Assert.IsType<IEnumerable<FormData>>(itemsProperty!.GetValue(okResult.Value), exactMatch: false);
        Assert.Empty(itemsValue);
        Assert.Equal(0, totalCountProperty!.GetValue(okResult.Value));
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
        _repository.Setup(r => r.UpdateAsync(id, It.IsAny<FormData>(), existing.VersionKey)).ReturnsAsync(updated);

        SetIfMatchHeader(existing.VersionKey);
        var result = await _controller.Update(id, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(updated, okResult.Value);
    }

    [Fact]
    public async Task Test_Update_ReturnsBadRequest_WhenIfMatchHeaderIsMissing()
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

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

        var result = await _controller.Update(id, request);

        Assert.IsType<BadRequestObjectResult>(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<FormData>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Update_Throws_WhenFormDoesNotExist()
    {
        var id = Guid.NewGuid();
        var request = new UpdateFormRequest("Updated Subject", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((FormData?)null);

        await Assert.ThrowsAsync<FormNotFoundException>(() => _controller.Update(id, request));
    }

    [Fact]
    public async Task Test_Update_Throws_WhenFormIsSoftDeleted()
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

        await Assert.ThrowsAsync<FormConflictException>(() => _controller.Update(id, request));

        _repository.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<FormData>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Update_Throws_WhenRowVersionIsStale()
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
        var staleRowVersion = Guid.NewGuid();
        var request = new UpdateFormRequest("Updated Subject", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repository.Setup(r => r.UpdateAsync(id, It.IsAny<FormData>(), staleRowVersion))
            .ThrowsAsync(new FormPreconditionFailedException(id));

        SetIfMatchHeader(staleRowVersion);
        await Assert.ThrowsAsync<FormPreconditionFailedException>(() => _controller.Update(id, request));
    }

    [Fact]
    public async Task Test_Patch_ReturnsOk_WhenFormExistsAndNotDeleted()
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
        var request = new PatchFormRequest("Patched Subject", null, null, null, null);
        var patched = new FormData
        {
            Id = id,
            Subject = "Patched Subject",
            CreatedBy = "TestUser",
            CreatedAt = existing.CreatedAt
        };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repository.Setup(r => r.UpdateAsync(id, It.IsAny<FormData>(), existing.VersionKey)).ReturnsAsync(patched);

        SetIfMatchHeader(existing.VersionKey);
        var result = await _controller.Patch(id, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(patched, okResult.Value);
    }

    [Fact]
    public async Task Test_Patch_PreservesExistingFields_WhenNotProvidedInRequest()
    {
        var id = Guid.NewGuid();
        var dueDate = DateTime.UtcNow.AddDays(5);
        var existing = new FormData
        {
            Id = id,
            Subject = "Original Subject",
            Description = "Original Description",
            DueDate = dueDate,
            Priority = 3,
            Critical = true,
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        var request = new PatchFormRequest(null, null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repository.Setup(r => r.UpdateAsync(id, It.IsAny<FormData>(), existing.VersionKey))
            .ReturnsAsync((Guid _, FormData f, Guid _) => f);

        SetIfMatchHeader(existing.VersionKey);
        var result = await _controller.Patch(id, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var patched = Assert.IsType<FormData>(okResult.Value);
        Assert.Equal(existing.Subject, patched.Subject);
        Assert.Equal(existing.Description, patched.Description);
        Assert.Equal(existing.DueDate, patched.DueDate);
        Assert.Equal(existing.Priority, patched.Priority);
        Assert.Equal(existing.Critical, patched.Critical);
    }

    [Fact]
    public async Task Test_Patch_ReturnsBadRequest_WhenIfMatchHeaderIsMissing()
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
        var request = new PatchFormRequest("Patched Subject", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

        var result = await _controller.Patch(id, request);

        Assert.IsType<BadRequestObjectResult>(result);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<FormData>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Patch_Throws_WhenFormDoesNotExist()
    {
        var id = Guid.NewGuid();
        var request = new PatchFormRequest("Patched Subject", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((FormData?)null);

        await Assert.ThrowsAsync<FormNotFoundException>(() => _controller.Patch(id, request));
    }

    [Fact]
    public async Task Test_Patch_Throws_WhenFormIsSoftDeleted()
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
        var request = new PatchFormRequest("Patched Subject", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<FormConflictException>(() => _controller.Patch(id, request));

        _repository.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<FormData>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Delete_ReturnsBadRequest_WhenIdIsEmpty()
    {
        var result = await _controller.Delete(Guid.Empty);

        Assert.IsType<BadRequestObjectResult>(result);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Delete_ReturnsNoContent_WhenDeleteSucceeds()
    {
        var id = Guid.NewGuid();
        var existing = new FormData
        {
            Id = id,
            Subject = "Test Subject",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repository.Setup(r => r.DeleteAsync(id, existing.VersionKey)).ReturnsAsync(true);

        SetIfMatchHeader(existing.VersionKey);
        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Test_Delete_ReturnsNoContent_WhenAlreadySoftDeleted()
    {
        var id = Guid.NewGuid();
        var existing = new FormData
        {
            Id = id,
            Subject = "Test Subject",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = true
        };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Delete_Throws_WhenFormDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((FormData?)null);

        await Assert.ThrowsAsync<FormNotFoundException>(() => _controller.Delete(id));

        _repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Delete_ReturnsBadRequest_WhenIfMatchHeaderIsMissing()
    {
        var id = Guid.NewGuid();
        var existing = new FormData
        {
            Id = id,
            Subject = "Test Subject",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

        var result = await _controller.Delete(id);

        Assert.IsType<BadRequestObjectResult>(result);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Test_Delete_Throws_WhenDeleteFailsConcurrently()
    {
        var id = Guid.NewGuid();
        var existing = new FormData
        {
            Id = id,
            Subject = "Test Subject",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repository.Setup(r => r.DeleteAsync(id, existing.VersionKey)).ReturnsAsync(false);

        SetIfMatchHeader(existing.VersionKey);
        await Assert.ThrowsAsync<FormNotFoundException>(() => _controller.Delete(id));
    }

    [Fact]
    public async Task Test_Delete_Throws_WhenRowVersionIsStale()
    {
        var id = Guid.NewGuid();
        var existing = new FormData
        {
            Id = id,
            Subject = "Test Subject",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        var staleRowVersion = Guid.NewGuid();

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repository.Setup(r => r.DeleteAsync(id, staleRowVersion))
            .ThrowsAsync(new FormPreconditionFailedException(id));

        SetIfMatchHeader(staleRowVersion);
        await Assert.ThrowsAsync<FormPreconditionFailedException>(() => _controller.Delete(id));
    }
}
