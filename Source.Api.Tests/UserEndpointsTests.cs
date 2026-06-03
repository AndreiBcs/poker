using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Source.Features.User;
using Xunit;

namespace Source.Api.Tests;

public class UserEndpointsTests : ApiTestBase
{
    public UserEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateUser_ThenGetMe_ReturnsUser()
    {
        await ResetDatabaseAsync();
        var client = CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/users",
            new UserCreateRequest("alice", "alice@example.com", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(created);

        var request = new HttpRequestMessage(HttpMethod.Get, "/users/me");
        request.Headers.Add(TestAuthHandler.UserIdHeader, created!.Id.ToString());
        request.Headers.Add(TestAuthHandler.UserNameHeader, created.Username);
        request.Headers.Add(TestAuthHandler.EmailHeader, created.Email);

        var meResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var me = await meResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(me);
        Assert.Equal(created.Id, me!.Id);
    }

    [Fact]
    public async Task GetUser_WithDifferentUser_ReturnsForbidden()
    {
        await ResetDatabaseAsync();
        var userA = await SeedUserAsync("alice", "alice@example.com");
        var userB = await SeedUserAsync("bob", "bob@example.com");
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/users/{userA.Id}");
        AddAuthHeaders(request, userB);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
