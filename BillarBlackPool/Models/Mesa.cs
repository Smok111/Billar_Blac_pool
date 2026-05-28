using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillarBlackPool.Models
{
    public class Mesa
    {
        [Key]
        public int IdMesa { get; set; }

        [Required(ErrorMessage = "El número de mesa es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El número debe ser mayor a 0")]
        public int NumeroMesa { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Disponible";

        // Navegación
        public virtual ICollection<Consumo>? Consumos { get; set; }
        public virtual ICollection<Reserva>? Reservas { get; set; }
    }
}