using System.ComponentModel.DataAnnotations;
using FormsApi.Validation;

namespace FormsApi.Contracts;

// This record represents the request for patching an existing form data entry.
// Should be a subset of the FormData object
public record PatchFormRequest
(
    // nullable properties allow for partial updates, where only the 
    // provided fields will be updated.
    [StringLength(200, MinimumLength = 1)]
    string? Subject, 

    [StringLength(5000)]
    string? Description,

    [FutureDate] // a due date must be in the future
    DateTime? DueDate,

    [Range(1, 10)]
    int? Priority,

    bool? Critical
);
