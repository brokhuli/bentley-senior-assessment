namespace FormsApi.Contracts;

// This record represents the request for creating a new form data entry.
// Should be a subset of the FormData object
public record CreateFormRequest
(
    string Subject,
    string? Description,
    DateTime? DueDate,
    int? Priority,
    bool? Critical,
    string CreatedBy
);
