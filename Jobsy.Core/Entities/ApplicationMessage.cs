using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class ApplicationMessage
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public Guid SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
    public Guid? AttachmentId { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public ApplicationMessageBlockReason BlockReason { get; set; }
    public bool SentDespiteWarning { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
