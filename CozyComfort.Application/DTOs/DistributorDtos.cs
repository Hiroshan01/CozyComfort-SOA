namespace CozyComfort.Application.DTOs;

public sealed class AssignedSellerDto
{
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string SellerEmail { get; set; } = string.Empty;
}
