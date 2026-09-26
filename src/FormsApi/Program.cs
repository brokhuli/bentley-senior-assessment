using FormsApi.Exceptions;
using FormsApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Register controller infrastructure
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register local form data repository
builder.Services.AddSingleton<IFormDataRepository, LocalFormDataRepository>();

// Global exception handling. Maps domain exceptions to consistent
// ProblemDetails responses and prevents unhandled exceptions from
// leaking internal details to the client.
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

// Note: Did not make nor register a custom Logger service in DI

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
