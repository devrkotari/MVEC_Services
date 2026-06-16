using Mvec.BuildingBlocks.Persistence;

namespace Mvec.Review.Api.Domain;

// TODO: flesh out per Guide for Review service.
public class Review : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
