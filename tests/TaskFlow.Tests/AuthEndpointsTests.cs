using System.Net;
using System.Net.Http.Json;
using TaskFlow.Application.Dtos;

namespace TaskFlow.Tests;

public class AuthEndpointsTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task Register_returns_201_and_a_token()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("nuevo@test.com", "Password123!"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(TestApiFactory.Json);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_409()
    {
        var client = factory.CreateClient();
        var request = new RegisterRequest("duplicado@test.com", "Password123!");

        await client.PostAsJsonAsync("/api/auth/register", request);
        var second = await client.PostAsJsonAsync("/api/auth/register", request with { Email = "DUPLICADO@test.com" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("no-es-un-correo", "Password123!")]
    [InlineData("ok@test.com", "corta")]
    public async Task Register_with_invalid_data_returns_400(string email, string password)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("login@test.com", "Password123!"));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("login@test.com", "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("wrong@test.com", "Password123!"));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("wrong@test.com", "OtraClave999"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tasks_endpoint_requires_authentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
