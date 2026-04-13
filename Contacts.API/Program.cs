using Contacts.API.Data;
using Contacts.API.Middleware;
using Contacts.API.Repositories;
using Contacts.API.Repositories.Interfaces;
using Contacts.API.Services;
using Contacts.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load connection string from environment variable, fallback to appsettings
var connectionString = Environment.GetEnvironmentVariable("CONTACTS_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No database connection string configured. Set CONTACTS_DB_CONNECTION environment variable or ConnectionStrings:DefaultConnection in appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IContactService, ContactService>();

var app = builder.Build();

// ErrorHandlingMiddleware must be first — catches exceptions from the entire pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
