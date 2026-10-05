using System.Net;
using System.Net.Http.Json;
using TaskFlow.Application.Dtos;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Tests;

public class TaskEndpointsTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    private static async Task<TaskResponse> CreateTaskAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(title, null, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(TestApiFactory.Json))!;
    }

    [Fact]
    public async Task Create_then_get_returns_the_task()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var created = await CreateTaskAsync(client, "Preparar entrevista");
        var fetched = await client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{created.Id}", TestApiFactory.Json);

        Assert.Equal("Preparar entrevista", fetched!.Title);
        Assert.Equal(TaskItemStatus.Todo, fetched.Status);
    }

    [Fact]
    public async Task Create_with_empty_title_returns_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_filters_by_status_and_paginates()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        for (var i = 1; i <= 5; i++) await CreateTaskAsync(client, $"Tarea {i}");

        var first = await CreateTaskAsync(client, "Terminada");
        await client.PutAsJsonAsync($"/api/tasks/{first.Id}",
            new UpdateTaskRequest("Terminada", null, TaskItemStatus.Done, null), TestApiFactory.Json);

        var done = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks?status=Done", TestApiFactory.Json);
        var page = await client.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks?page=2&pageSize=4", TestApiFactory.Json);

        Assert.Equal(1, done!.TotalCount);
        Assert.Equal(6, page!.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.Page);
    }

    [Fact]
    public async Task Update_changes_status_and_title()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(client, "Original");

        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Editada", "con detalle", TaskItemStatus.InProgress, null), TestApiFactory.Json);
        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>(TestApiFactory.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Editada", updated!.Title);
        Assert.Equal(TaskItemStatus.InProgress, updated.Status);
    }

    [Fact]
    public async Task Delete_returns_204_and_then_404()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(client, "Borrar");

        var delete = await client.DeleteAsync($"/api/tasks/{created.Id}");
        var get = await client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Users_cannot_see_or_modify_other_users_tasks()
    {
        var owner = await factory.CreateAuthenticatedClientAsync();
        var intruder = await factory.CreateAuthenticatedClientAsync();
        var task = await CreateTaskAsync(owner, "Privada");

        var get = await intruder.GetAsync($"/api/tasks/{task.Id}");
        var delete = await intruder.DeleteAsync($"/api/tasks/{task.Id}");
        var list = await intruder.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks", TestApiFactory.Json);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(list!.Items);
    }
}
