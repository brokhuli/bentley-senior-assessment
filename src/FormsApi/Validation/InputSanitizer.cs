namespace FormsApi.Validation;

// This static class provides methods for sanitizing input strings to 
// ensure consistent formatting and prevent issues with line endings 
// or whitespace.
public static class InputSanitizer
{
    // Sanitize a string by normalizing line endings and trimming whitespace
    public static string Sanitize(string value) =>
        value.Replace("\r\n", "\n")
             .Replace("\r", "\n")
             .Trim();

    // Sanitize an optional string, returning null if the input is null
    public static string? SanitizeOptional(string? value) =>
        value is null ? null : Sanitize(value);
}
