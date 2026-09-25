namespace FormsApi.Models;

public class FormData
{
    // Set by FormsApi
    public Guid Id { get; set; }

    // Set by CreateFormRequest
    public string Subject { get; set; }

    // Set by CreateFormRequest
    public string? Description { get; set; }

    // Set by CreateFormRequest
    public DateTime? DueDate { get; set; }

    // Set by CreateFormRequest
    public int? Priority { get; set; }

    // Set by CreateFormRequest
    public bool? Critical { get; set; }

    // Set by FormsApi
    public DateTime CreatedAt { get; set; }

    // Set by FormsApi
    public DateTime? UpdatedAt { get; set; }

    // Set by CreateFormRequest
    // Assuming this is set via an auth service and delivered when 
    // creating a form rather than something FormsApi would set itself.
    public string CreatedBy { get; set; }
}
