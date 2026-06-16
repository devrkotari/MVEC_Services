using Mvec.BuildingBlocks.Persistence;

namespace Mvec.Product.Api.Domain;

// TODO: flesh out per Guide for Product service.
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
