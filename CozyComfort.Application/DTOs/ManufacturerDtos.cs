using System.ComponentModel.DataAnnotations;

namespace CozyComfort.Application.DTOs;

public sealed class ProductionCapacityResponse
{
    public Guid BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ProductionCapacity { get; set; }
    public int RequestedQuantity { get; set; }
    public bool CanFulfillFromStock { get; set; }
    public int EstimatedLeadTimeDays { get; set; }
}

public sealed class UpdateProductionStatusRequest
{
    [Range(0, 100000)]
    public int CurrentStock { get; set; }

    [Range(0, 100000)]
    public int ProductionCapacity { get; set; }
}

public sealed class LeadTimeResponse
{
    public Guid BlanketId { get; set; }
    public int RequestedQuantity { get; set; }
    public int EstimatedLeadTimeDays { get; set; }
}
