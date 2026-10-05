using System.ComponentModel.DataAnnotations;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Dtos;

public record RegisterRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, MinLength(8), StringLength(128)] string Password);

public record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public record AuthResponse(string Token, DateTime ExpiresAt);

public record CreateTaskRequest(
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    DateTime? DueDate);

public record UpdateTaskRequest(
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    TaskItemStatus Status,
    DateTime? DueDate);

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
