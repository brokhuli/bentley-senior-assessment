using FormsApi.Exceptions;
using FormsApi.Models;
using FormsApi.Repositories;

namespace FormsApi.Tests.Repositories;

public class InProcessFormDataRepositoryTests
{
    private readonly InProcessFormDataRepository _repository = new();

    private static FormData NewForm(string subject = "Test Subject", DateTime? createdAt = null, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        Subject = subject,
        CreatedBy = "TestUser",
        CreatedAt = createdAt ?? DateTime.UtcNow,
        IsDeleted = isDeleted
    };

    [Fact]
    public async Task CreateAsync_StoresAndReturnsForm()
    {
        var form = NewForm();

        var result = await _repository.CreateAsync(form);

        Assert.Same(form, result);
        Assert.Same(form, await _repository.GetByIdAsync(form.Id));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenIdAlreadyExists()
    {
        var form = NewForm();
        await _repository.CreateAsync(form);

        var duplicate = new FormData { Id = form.Id, Subject = "Other", CreatedBy = "TestUser", CreatedAt = DateTime.UtcNow };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.CreateAsync(duplicate));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ExcludesSoftDeletedForms()
    {
        var active = NewForm("Active Subject");
        var deleted = NewForm("Deleted Subject", isDeleted: true);
        await _repository.CreateAsync(active);
        await _repository.CreateAsync(deleted);

        var (items, totalCount) = await _repository.ListAsync(1, 20, null);

        Assert.Equal(1, totalCount);
        Assert.Contains(items, f => f.Id == active.Id);
        Assert.DoesNotContain(items, f => f.Id == deleted.Id);
    }

    [Fact]
    public async Task ListAsync_FiltersBySubjectSubstring()
    {
        await _repository.CreateAsync(NewForm("Widget Request"));
        await _repository.CreateAsync(NewForm("Gadget Request"));

        var (items, totalCount) = await _repository.ListAsync(1, 20, "Widget");

        Assert.Equal(1, totalCount);
        Assert.Equal("Widget Request", Assert.Single(items).Subject);
    }

    [Fact]
    public async Task ListAsync_OrdersByCreatedAtDescending_AndPaginates()
    {
        var now = DateTime.UtcNow;
        var oldest = NewForm("Oldest", now.AddMinutes(-2));
        var middle = NewForm("Middle", now.AddMinutes(-1));
        var newest = NewForm("Newest", now);
        await _repository.CreateAsync(oldest);
        await _repository.CreateAsync(middle);
        await _repository.CreateAsync(newest);

        var (page1, totalCount) = await _repository.ListAsync(1, 2, null);
        var (page2, _) = await _repository.ListAsync(2, 2, null);

        Assert.Equal(3, totalCount);
        Assert.Equal([newest.Id, middle.Id], page1.Select(f => f.Id));
        Assert.Equal([oldest.Id], page2.Select(f => f.Id));
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenFormNotFound()
    {
        var result = await _repository.UpdateAsync(Guid.NewGuid(), NewForm(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_AndBumpsVersionKey_WhenVersionKeyMatches()
    {
        var existing = NewForm();
        await _repository.CreateAsync(existing);
        var originalVersionKey = existing.VersionKey;

        var updated = new FormData
        {
            Id = existing.Id,
            Subject = "Updated Subject",
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt
        };

        var result = await _repository.UpdateAsync(existing.Id, updated, originalVersionKey);

        Assert.NotNull(result);
        Assert.Equal("Updated Subject", result!.Subject);
        Assert.NotEqual(originalVersionKey, result.VersionKey);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenVersionKeyIsStale()
    {
        var existing = NewForm();
        await _repository.CreateAsync(existing);
        var staleVersionKey = Guid.NewGuid();

        var updated = new FormData
        {
            Id = existing.Id,
            Subject = "Updated Subject",
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt
        };

        await Assert.ThrowsAsync<FormPreconditionFailedException>(() => _repository.UpdateAsync(existing.Id, updated, staleVersionKey));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenFormNotFound()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_WhenVersionKeyMatches()
    {
        var existing = NewForm();
        await _repository.CreateAsync(existing);
        var originalVersionKey = existing.VersionKey;

        var result = await _repository.DeleteAsync(existing.Id, originalVersionKey);

        Assert.True(result);
        var stored = await _repository.GetByIdAsync(existing.Id);
        Assert.True(stored!.IsDeleted);
        Assert.NotNull(stored.DeletedAt);
        Assert.NotEqual(originalVersionKey, stored.VersionKey);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenVersionKeyIsStale()
    {
        var existing = NewForm();
        await _repository.CreateAsync(existing);
        var staleVersionKey = Guid.NewGuid();

        await Assert.ThrowsAsync<FormPreconditionFailedException>(() => _repository.DeleteAsync(existing.Id, staleVersionKey));
    }

    [Fact]
    public async Task DeleteAsync_IsIdempotent_WhenAlreadySoftDeleted()
    {
        var existing = NewForm();
        await _repository.CreateAsync(existing);
        await _repository.DeleteAsync(existing.Id, existing.VersionKey);

        // Passing a mismatched version key should not matter for an already-deleted record.
        var result = await _repository.DeleteAsync(existing.Id, Guid.NewGuid());

        Assert.True(result);
    }
}
