namespace FormsApi.Models;

public class FormData
{
    public Guid Id { get; set; }

    public string Subject { get; set; }

    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public int? Priority { get; set; } // Must be between 1 and 10

    public bool? Critical { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string CreatedBy { get; set; }
}
