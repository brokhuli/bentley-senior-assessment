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
