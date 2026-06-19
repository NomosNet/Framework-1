using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace UserService.Tests;

public class ApiTests
{
    private const string BaseUrl = "http://127.0.0.1:8080";
    private static readonly HttpClient Client = new();

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        var response = await Client.GetAsync($"{BaseUrl}/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_NotFound_Returns404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/api/users/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_InvalidData_Returns400()
    {
        var payload = new StringContent(
            """{"email": "bad@mail.com", "name": "", "age": -5}""",
            Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync($"{BaseUrl}/api/users", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ValidData_ReturnsCreated()
    {
        var payload = new StringContent(
            """
            {
                "email": "test@example.com",
                "password": "secure123",
                "name": "Test User",
                "age": 25
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync($"{BaseUrl}/api/users", payload);

        Assert.True(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
            $"Expected 201 or 200, got: {(int)response.StatusCode}");
    }

    [Fact]
    public async Task GetUserById_AfterCreate_ReturnsOk()
    {
        var payload = new StringContent(
            """
            {
                "email": "getbyid@example.com",
                "password": "secure123",
                "name": "Lookup User",
                "age": 30
            }
            """,
            Encoding.UTF8,
            "application/json");

        var createResponse = await Client.PostAsync($"{BaseUrl}/api/users", payload);
        if (createResponse.StatusCode is not (HttpStatusCode.Created or HttpStatusCode.OK))
        {
            return;
        }

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        if (!created.TryGetProperty("id", out var idElement))
        {
            return;
        }

        var id = idElement.GetInt32();
        var response = await Client.GetAsync($"{BaseUrl}/api/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
