using System.ComponentModel.DataAnnotations;
using ErpMetronic.Domain.Common;

namespace ErpMetronic.Domain.Entities;

/// <summary>Satuan ukur (pcs, box, kg, dll).</summary>
public class UnitOfMeasure : BaseEntity
{
    [Required, StringLength(15)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
