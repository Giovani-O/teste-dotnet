using System.Net;
using System.Net.Http.Json;
using Contacts.API.Data;
using Contacts.API.DTOs;
using Contacts.API.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Contacts.Tests.Controllers;

[Trait("Category", "E2E")]
public class ContactsControllerTests : IClassFixture<ContactsApiFactory>, IDisposable
{
    private readonly ContactsApiFactory _factory;
    private readonly HttpClient _client;
    private readonly string _connectionString;

    public ContactsControllerTests(ContactsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _connectionString = factory.ConnectionString;
        ClearDatabase();
    }

    private void ClearDatabase()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand("DELETE FROM dbo.Contacts", connection);
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        ClearDatabase();
    }

    [Fact]
    public async Task Create_Returns201AndLocationHeader()
    {
        var dto = new CreateContactDto
        {
            Name = "John Doe",
            Birthdate = new DateOnly(1990, 5, 15),
            Gender = Gender.Male
        };

        var response = await _client.PostAsJsonAsync("/api/contacts", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dto = new CreateContactDto
        {
            Name = "Jane Doe",
            Birthdate = new DateOnly(1992, 8, 20),
            Gender = Gender.Female
        };
        await _client.PostAsJsonAsync("/api/contacts", dto);

        var response = await _client.GetAsync("/api/contacts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contacts = await response.Content.ReadFromJsonAsync<List<ContactResponseDto>>();
        Assert.NotNull(contacts);
        Assert.NotEmpty(contacts);
    }

    [Fact]
    public async Task GetById_Returns200ForActive()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", new CreateContactDto
        {
            Name = "Active Contact",
            Birthdate = new DateOnly(1985, 1, 1),
            Gender = Gender.Male
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var response = await _client.GetAsync($"/api/contacts/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contact = await response.Content.ReadFromJsonAsync<ContactResponseDto>();
        Assert.NotNull(contact);
        Assert.True(contact.IsActive);
    }

    [Fact]
    public async Task GetById_Returns404ForInactive()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", new CreateContactDto
        {
            Name = "Inactive Contact",
            Birthdate = new DateOnly(1985, 1, 1),
            Gender = Gender.Male
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        await _client.DeleteAsync($"/api/contacts/{created!.Id}");

        var response = await _client.GetAsync($"/api/contacts/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", new CreateContactDto
        {
            Name = "Original Name",
            Birthdate = new DateOnly(1990, 1, 1),
            Gender = Gender.Male
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var response = await _client.PatchAsJsonAsync($"/api/contacts/{created!.Id}", new UpdateContactDto
        {
            Name = "Updated Name"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ContactResponseDto>();
        Assert.Equal("Updated Name", updated?.Name);
    }

    [Fact]
    public async Task Activate_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", new CreateContactDto
        {
            Name = "To Activate",
            Birthdate = new DateOnly(1990, 1, 1),
            Gender = Gender.Male
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        await _client.PatchAsync($"/api/contacts/{created!.Id}/deactivate", null);
        var response = await _client.PatchAsync($"/api/contacts/{created.Id}/activate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Activate_Returns409IfAlreadyActive()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", new CreateContactDto
        {
            Name = "Already Active",
            Birthdate = new DateOnly(1990, 1, 1),
            Gender = Gender.Male
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        await _client.PatchAsync($"/api/contacts/{created!.Id}/activate", null);
        var response = await _client.PatchAsync($"/api/contacts/{created.Id}/activate", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", new CreateContactDto
        {
            Name = "To Deactivate",
            Birthdate = new DateOnly(1990, 1, 1),
            Gender = Gender.Male
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var response = await _client.PatchAsync($"/api/contacts/{created!.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_Returns409IfAlreadyInactive()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", new CreateContactDto
        {
            Name = "To Deactivate",
            Birthdate = new DateOnly(1990, 1, 1),
            Gender = Gender.Male
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        await _client.PatchAsync($"/api/contacts/{created!.Id}/deactivate", null);
        var response = await _client.PatchAsync($"/api/contacts/{created.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", new CreateContactDto
        {
            Name = "To Delete",
            Birthdate = new DateOnly(1990, 1, 1),
            Gender = Gender.Male
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ContactResponseDto>();

        var response = await _client.DeleteAsync($"/api/contacts/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}

public class ContactsApiFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; } = "Server=localhost,1434;Database=ContactsDb_Test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True";

    private static readonly object _lock = new();
    private static bool _initialized = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);

        if (!_initialized)
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    EnsureDatabaseCreated();
                    _initialized = true;
                }
            }
        }
    }

    private void EnsureDatabaseCreated()
    {
        var masterConnectionString = "Server=localhost,1434;Database=master;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True";
        
        using var masterConnection = new SqlConnection(masterConnectionString);
        masterConnection.Open();
        
        using var createDbCommand = new SqlCommand("IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ContactsDb_Test') CREATE DATABASE ContactsDb_Test", masterConnection);
        createDbCommand.ExecuteNonQuery();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(ConnectionString);
        
        using var context = new AppDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
    }
}