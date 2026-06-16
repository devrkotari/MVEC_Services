using Mvec.BuildingBlocks.Persistence;

namespace Mvec.Payment.Api.Domain;

// TODO: flesh out per Guide for Payment service.
public class Payment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
