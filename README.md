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

| Method | Route            | Description          |
|--------|-------------------|-----------------------|
| POST   | `/api/forms`      | Create a form         |
| GET    | `/api/forms/{id}` | Get a form by id      |
| GET    | `/api/forms`      | List forms (paged)    |
| PUT    | `/api/forms/{id}` | Update a form         |
| DELETE | `/api/forms/{id}` | Delete a form         |
