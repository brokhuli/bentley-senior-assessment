namespace FormsApi.Contracts;

// This record represents the request for updating an existing form data entry.
// Should be a subset of the FormData object
public record UpdateFormRequest
(
    Guid Id,
    string? Subject,
    string? Description,
    DateTime? DueDate,
    int? Priority,
    bool? Critical
);
