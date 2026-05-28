using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillarBlackPool.Models
{
    public class Rol
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRol { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Nombre del Rol")]
        public string NomRol { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Descripción")]
        public string DesRol { get; set; }

        
        public ICollection<Usuario>? Usuarios { get; set; }
    }
}