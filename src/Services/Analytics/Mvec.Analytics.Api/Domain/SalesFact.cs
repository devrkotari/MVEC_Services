using Mvec.BuildingBlocks.Persistence;

namespace Mvec.Analytics.Api.Domain;

// TODO: flesh out per Guide for Analytics service.
public class SalesFact : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
