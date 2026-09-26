using FormsApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Register controller infrastructure
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register local form data repository
builder.Services.AddSingleton<IFormDataRepository, LocalFormDataRepository>();

// Note: Did not make nor register a Logger service in DI

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
