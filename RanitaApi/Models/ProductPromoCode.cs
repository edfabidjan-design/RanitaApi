using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanitaApi.Models;

public class ProductPromoCode
{
    public int Id { get; set; }
    [Required] public string Code { get; set; } = "";
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Discount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}