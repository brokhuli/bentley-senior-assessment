# Bentley Senior Assessment — Forms API

A .NET 10 Web API for managing form records (`FormData`), built with ASP.NET Core controllers and an in-memory repository.

## Running the API

```bash
dotnet run --project src/FormsApi
```

## Running the tests

```bash
dotnet test
```

## API endpoints

All endpoints are rooted at `api/forms`.

| Method | Route             | Description        |
| ------ | ----------------- | ------------------ |
| POST   | `/api/forms`      | Create a form      |
| GET    | `/api/forms/{id}` | Get a form by id   |
| GET    | `/api/forms`      | List forms (paged) |
| PUT    | `/api/forms/{id}` | Update a form      |
| DELETE | `/api/forms/{id}` | Delete a form      |

## Further Improvements

### Comments

- I wouldn't normally include all the comments in code but seemed appropriate for an exercise, my preference is self documenting code for production and only add comments for complex or unusual constructs.

### Separation of Concerns

- I would move the business logic in FormsController out to a Service so the logic in FormsController only receives, validates auth, send request to service, report result. Keeps the logic in the controller within its scope. Also allows you to test FormsController and the Service separately. Also, the FormsController file is getting large at this point, so moving a good portion of the code over to a service helps for readablility.

- Possibly use a different FormData object for entity and response objects to decouple controller layer, service layer, and db layer.

### Validation criteria for Data Annotations

- The same validation values are replicated across contract records and model data. Would make these static consts in production. Although its nice to see them with the data visually.

### Hardening for Scalability

- Depending on requirements, harden the api for expected user base / load / SLAs:
  - Rate limiting for single client request pressure
  - Caching for hot reads
  - Indexing on db if reads are significantly greater than writes

### Observability

- Add /health or /metrics endpoints for observability scraping
