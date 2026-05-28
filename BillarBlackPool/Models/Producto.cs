using BillarBlackPool.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillarBlackPool.Models
{
    public class Producto
    {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdProducto { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Nombre del Producto")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Precio { get; set; }

    [Required]
    public int IdCategoria { get; set; }

    [ForeignKey("IdCategoria")]
    public CategoriaProducto? CategoriaProducto { get; set; }

  
    public ICollection<ConsumoDetalle> Detalles { get; set; } = new List<ConsumoDetalle>();

    [StringLength(250)]
    public string? ImagenUrl { get; set; }
    }
}
