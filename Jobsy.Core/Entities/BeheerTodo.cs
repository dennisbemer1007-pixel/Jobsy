using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Admin-only task list item (beheer takenlijst), not the dashboard "te doen" inbox.</summary>
public class BeheerTodo
{
    public Guid Id { get; set; }

    /// <summary>Stable key for idempotent migration seed (unique when set).</summary>
    public string? SeedKey { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateOnly? DueDate { get; set; }

    public BeheerTodoPriority Priority { get; set; } = BeheerTodoPriority.Normaal;

    public BeheerTodoStatus Status { get; set; } = BeheerTodoStatus.Open;

    public string Category { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
}
