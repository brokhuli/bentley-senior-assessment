using System.ComponentModel.DataAnnotations;
using FormsApi.Validation;

namespace FormsApi.Contracts;

// This record represents the request for updating (put) an existing form data entry.
// Should be a subset of the FormData object
// Validation included with DataAnnotation attributes
public sealed record UpdateFormRequest
(
    // changed from nullable to non-nullable to ensure that Subject 
    // is always provided in the update request, its a required field in
    // the FormData object, and we want to avoid null values in the 
    // database. Cleanest solution.
    [StringLength(200, MinimumLength = 1)]
    string Subject, 

    [StringLength(5000)]
    string? Description,

    [FutureDate] // a due date must be in the future (custom attribute)
    DateTime? DueDate,

    [Range(1, 10)]
    int? Priority,

    bool? Critical
);
