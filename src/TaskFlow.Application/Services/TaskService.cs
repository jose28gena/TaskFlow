using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Dtos;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class TaskService(IAppDbContext db)
{
    private const int MaxPageSize = 100;

    public async Task<PagedResponse<TaskResponse>> ListAsync(
        Guid ownerId, TaskItemStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Tasks.AsNoTracking().Where(t => t.OwnerId == ownerId);
        if (status is not null)
            query = query.Where(t => t.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<TaskResponse>(items.Select(ToResponse).ToList(), page, pageSize, total);
    }

    public async Task<TaskResponse> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default)
        => ToResponse(await FindAsync(ownerId, id, ct));

    public async Task<TaskResponse> CreateAsync(Guid ownerId, CreateTaskRequest request, CancellationToken ct = default)
    {
        var task = new TaskItem
        {
            OwnerId = ownerId,
            Title = request.Title.Trim(),
            Description = request.Description,
            DueDate = request.DueDate
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid ownerId, Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(ownerId, id, ct);

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return ToResponse(task);
    }

    public async Task DeleteAsync(Guid ownerId, Guid id, CancellationToken ct = default)
    {
        var task = await FindAsync(ownerId, id, ct);
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }

    // Filtrar por OwnerId evita que un usuario vea tareas ajenas: responde 404, no 403.
    private async Task<TaskItem> FindAsync(Guid ownerId, Guid id, CancellationToken ct)
        => await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId, ct)
           ?? throw new NotFoundException("Tarea no encontrada.");

    private static TaskResponse ToResponse(TaskItem t)
        => new(t.Id, t.Title, t.Description, t.Status, t.DueDate, t.CreatedAt, t.UpdatedAt);
}
