using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillarBlackPool.Models
{
    public class Consumo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdConsumo { get; set; }

        [Required]
        [Display(Name = "Mesa")]
        public int? IdMesa { get; set; }
        [ForeignKey("IdMesa")]
        public Mesa? Mesa { get; set; }

        [Required]
        [Display(Name = "Cliente")]
        public int? IdCliente { get; set; }
        [ForeignKey("IdCliente")]
        public Cliente? Cliente { get; set; }

        [Required]
        [Display(Name = "Fecha de Inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Fin")]
        public DateTime? FechaFin { get; set; }

        [Required]
        public string Estado { get; set; } = "Abierto";

        [Required]
        public int IdUsuario { get; set; }
        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        [Required]
        [Display(Name = "Tipo de Cobro")]
        [StringLength(20)]
        public string TipoCobro { get; set; } = "Libre";

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio por Hora")]
        public decimal PrecioHora { get; set; } = 6m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio por Media Hora")]
        public decimal PrecioMediaHora { get; set; } = 3m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio por Minuto")]
        public decimal PrecioLibrePorMinuto { get; set; } = 0.10m;

        [Display(Name = "Minutos Jugados")]
        public int? MinutosJugados { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Costo de Mesa")]
        public decimal CostoMesa { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Productos")]
        public decimal TotalProductos { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total")]
        public decimal Total { get; set; }

        public ICollection<ConsumoDetalle>? Detalles { get; set; } = new List<ConsumoDetalle>();
    }
}
