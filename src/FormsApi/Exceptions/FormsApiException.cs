namespace FormsApi.Exceptions;

public abstract class FormsApiException : Exception
{
    protected FormsApiException(string message) : base(message) { }
}

public sealed class FormNotFoundException : FormsApiException
{
    public FormNotFoundException(Guid id) : base($"Form '{id}' was not found.") { }
}

public sealed class FormConflictException : FormsApiException
{
    public FormConflictException(string message) : base(message) { }
}

// Thrown when a caller's If-Match value no longer matches the stored
// VersionKey.
public sealed class FormPreconditionFailedException : FormsApiException
{
    public FormPreconditionFailedException(Guid id)
        : base($"Form '{id}' was modified by another request. Please reload and try again.") { }
}
