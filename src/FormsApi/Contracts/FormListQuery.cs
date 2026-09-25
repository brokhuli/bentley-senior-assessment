namespace FormsApi.Contracts;

public record FormListQuery
(
    int Page = 1,
    int PageSize = 20,
    string? SubjectFilter = null
);
