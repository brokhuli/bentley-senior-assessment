using FormsApi.Data;
using FormsApi.Exceptions;
using FormsApi.Models;
using FormsApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FormsApi.Tests.Repositories;

public class RelationalFormDataRepositoryTests
{
    private static FormsApiDbContext NewDbContext()
    {
        var options = new DbContextOptionsBuilder<FormsApiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FormsApiDbContext(options);
    }

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
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        var form = NewForm();

        var result = await repository.CreateAsync(form);

        Assert.Equal(form.Id, result.Id);
        Assert.NotNull(await repository.GetByIdAsync(form.Id));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ExcludesSoftDeletedForms()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        var active = NewForm("Active Subject");
        var deleted = NewForm("Deleted Subject", isDeleted: true);
        await repository.CreateAsync(active);
        await repository.CreateAsync(deleted);

        var (items, totalCount) = await repository.ListAsync(1, 20, null);

        Assert.Equal(1, totalCount);
        Assert.Contains(items, f => f.Id == active.Id);
        Assert.DoesNotContain(items, f => f.Id == deleted.Id);
    }

    [Fact]
    public async Task ListAsync_FiltersBySubjectSubstring()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        await repository.CreateAsync(NewForm("Widget Request"));
        await repository.CreateAsync(NewForm("Gadget Request"));

        var (items, totalCount) = await repository.ListAsync(1, 20, "Widget");

        Assert.Equal(1, totalCount);
        Assert.Equal("Widget Request", Assert.Single(items).Subject);
    }

    [Fact]
    public async Task ListAsync_OrdersByCreatedAtDescending_AndPaginates()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        var now = DateTime.UtcNow;
        var oldest = NewForm("Oldest", now.AddMinutes(-2));
        var middle = NewForm("Middle", now.AddMinutes(-1));
        var newest = NewForm("Newest", now);
        await repository.CreateAsync(oldest);
        await repository.CreateAsync(middle);
        await repository.CreateAsync(newest);

        var (page1, totalCount) = await repository.ListAsync(1, 2, null);
        var (page2, _) = await repository.ListAsync(2, 2, null);

        Assert.Equal(3, totalCount);
        Assert.Equal([newest.Id, middle.Id], page1.Select(f => f.Id));
        Assert.Equal([oldest.Id], page2.Select(f => f.Id));
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenFormNotFound()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);

        var result = await repository.UpdateAsync(Guid.NewGuid(), NewForm(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_AndBumpsVersionKey_WhenVersionKeyMatches()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        var existing = NewForm();
        await repository.CreateAsync(existing);
        var originalVersionKey = existing.VersionKey;

        var updated = new FormData
        {
            Id = existing.Id,
            Subject = "Updated Subject",
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt
        };

        var result = await repository.UpdateAsync(existing.Id, updated, originalVersionKey);

        Assert.NotNull(result);
        Assert.Equal("Updated Subject", result!.Subject);
        Assert.NotEqual(originalVersionKey, result.VersionKey);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenVersionKeyIsStale()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        var existing = NewForm();
        await repository.CreateAsync(existing);
        var staleVersionKey = Guid.NewGuid();

        var updated = new FormData
        {
            Id = existing.Id,
            Subject = "Updated Subject",
            CreatedBy = existing.CreatedBy,
            CreatedAt = existing.CreatedAt
        };

        await Assert.ThrowsAsync<FormConflictException>(() => repository.UpdateAsync(existing.Id, updated, staleVersionKey));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenFormNotFound()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);

        var result = await repository.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_WhenVersionKeyMatches()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        var existing = NewForm();
        await repository.CreateAsync(existing);
        var originalVersionKey = existing.VersionKey;

        var result = await repository.DeleteAsync(existing.Id, originalVersionKey);

        Assert.True(result);
        var stored = await repository.GetByIdAsync(existing.Id);
        Assert.True(stored!.IsDeleted);
        Assert.NotNull(stored.DeletedAt);
        Assert.NotEqual(originalVersionKey, stored.VersionKey);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenVersionKeyIsStale()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        var existing = NewForm();
        await repository.CreateAsync(existing);
        var staleVersionKey = Guid.NewGuid();

        await Assert.ThrowsAsync<FormConflictException>(() => repository.DeleteAsync(existing.Id, staleVersionKey));
    }

    [Fact]
    public async Task DeleteAsync_IsIdempotent_WhenAlreadySoftDeleted()
    {
        await using var dbContext = NewDbContext();
        var repository = new RelationalFormDataRepository(dbContext);
        var existing = NewForm();
        await repository.CreateAsync(existing);
        await repository.DeleteAsync(existing.Id, existing.VersionKey);

        // Passing a mismatched version key should not matter for an already-deleted record.
        var result = await repository.DeleteAsync(existing.Id, Guid.NewGuid());

        Assert.True(result);
    }
}
