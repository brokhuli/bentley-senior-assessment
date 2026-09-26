using System.ComponentModel.DataAnnotations;
using FormsApi.Validation;

namespace FormsApi.Contracts;

// This record represents the request for creating a new form data entry.
// Should be a subset of the FormData object
public record CreateFormRequest
(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Subject,

    [StringLength(5000)]
    string? Description,

    [FutureDate] // a due date must be in the future
    DateTime? DueDate,

    [Range(1, 10)]
    int? Priority,

    bool? Critical,

    [StringLength(100, MinimumLength = 1)]
    string CreatedBy
);
