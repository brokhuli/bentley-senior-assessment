using FormsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FormsApi.Data;

// EF Core DbContext for RelationalFormDataRepository implementation of IFormDataRepository.
// Simple schema, one db table for FormData.
public class FormsApiDbContext : DbContext
{
    public FormsApiDbContext(DbContextOptions<FormsApiDbContext> options) : base(options)
    {
    }

    public DbSet<FormData> Forms => Set<FormData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormData>(entity =>
        {
            // Mapping do not need specified, EF Core maps record properties to 
            // columns by convention w/ DataAnnotation attributes.
        });
    }
}
