namespace VendingService.API.Models;

public sealed class NotificationTemplate
{
    public int NotificationTemplateId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

