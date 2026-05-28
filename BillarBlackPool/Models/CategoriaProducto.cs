using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillarBlackPool.Models
{
    public class CategoriaProducto
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCategoria { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        public string? Nombre { get; set; }
        public ICollection<Producto>? Productos { get; set; }
    }
}