using System.ComponentModel.DataAnnotations;
namespace FormsApi.Contracts;

// This record represents the query parameters for listing form 
// data entries.
public record FormListQuery
(
    [Range(1, int.MaxValue)] // > 1
    int Page = 1,

    [Range(1, 50)] // > 1, cap for performance reasons
    int PageSize = 20,

    [StringLength(200, MinimumLength = 1)] // consistent for Subject Length
    string? SubjectFilter = null
);
