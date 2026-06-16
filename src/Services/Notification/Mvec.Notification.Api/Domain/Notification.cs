using Mvec.BuildingBlocks.Persistence;

namespace Mvec.Notification.Api.Domain;

// TODO: flesh out per Guide for Notification service.
public class Notification : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
