using System.ComponentModel.DataAnnotations;
using FormsApi.Validation;

namespace FormsApi.Contracts;

// This record represents the request for updating an existing form data entry.
// Should be a subset of the FormData object
public record UpdateFormRequest
(
    [Required]
    Guid Id,

    [StringLength(200, MinimumLength = 1)]
    string? Subject,

    string? Description,

    [FutureDate] // a due date must be in the future
    DateTime? DueDate,

    [Range(1, 10)]
    int? Priority,

    bool? Critical
);
