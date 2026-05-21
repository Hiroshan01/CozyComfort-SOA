using System.ComponentModel.DataAnnotations;

namespace CozyComfort.Application.DTOs;

public sealed class UpsertBlanketRequest
{
    [Required, StringLength(100)]
    public string ModelName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Material { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Size { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Color { get; set; } = string.Empty;

    [Range(0.01, 999999)]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int ProductionCapacity { get; set; }

    [Range(0, 100000)]
    public int CurrentStock { get; set; }
}

public sealed class BlanketDto
{
    public Guid Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int ProductionCapacity { get; set; }
    public int CurrentStock { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
