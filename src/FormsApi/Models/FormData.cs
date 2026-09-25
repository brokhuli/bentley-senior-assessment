namespace FormsApi.Models;

public class FormData
{
    // Set internally
    public Guid Id { get; set; }

    public required string Subject { get; set; }

    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public int? Priority { get; set; }

    public bool? Critical { get; set; }

    // Set internally
    public DateTime CreatedAt { get; set; }

    // Set internally
    public DateTime? UpdatedAt { get; set; }

    // Assuming this is set via an auth service and delivered when 
    // creating a form rather than something FormsApi would set itself.
    public required string CreatedBy { get; set; }

    // Added to handle soft deletes
    // Set internally
    public bool IsDeleted { get; set; }

    // Added to handle soft deletes
    // Set internally
    public DateTime? DeletedAt { get; set; }
}
