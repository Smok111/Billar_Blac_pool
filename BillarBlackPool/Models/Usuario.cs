using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillarBlackPool.Models
{
    public class Usuario
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Nombre")]
        public string NomUsuario { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Apellido")]
        public string ApeUsuario { get; set; }

        [Required]
        [StringLength(50)]
        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string Correo { get; set; }

        [Required]
        [StringLength(8)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        
        [Required]
        [Display(Name = "Rol")]
        public int IdRol { get; set; }

        [ForeignKey("IdRol")]
        public Rol? Rol { get; set; }

        public ICollection<Consumo>? Consumos { get; set; }
    }
}