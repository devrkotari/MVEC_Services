using Mvec.BuildingBlocks.Persistence;

namespace Mvec.Order.Api.Domain;

// TODO: flesh out per Guide for Order service.
public class Order : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
