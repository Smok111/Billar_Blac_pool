using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BillarBlackPool.Models
{
    public class Reserva
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReserva { get; set; }

        
        [Required(ErrorMessage = "La fecha de reserva es obligatoria")]
        [Display(Name = "Fecha de Reserva")]
        [DataType(DataType.Date)]
        public DateTime FechaReserva { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Display(Name = "Hora de Inicio Programada")]
        [DataType(DataType.Time)]
        public TimeSpan HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        [Display(Name = "Hora de Fin Programada")]
        [DataType(DataType.Time)]
        public TimeSpan HoraFin { get; set; }

    
        [Display(Name = "Fecha y Hora de Inicio del Juego")]
        [DataType(DataType.DateTime)]
        public DateTime? FechaHoraInicioJuego { get; set; }

        [Display(Name = "Fecha y Hora de Fin del Juego")]
        [DataType(DataType.DateTime)]
        public DateTime? FechaHoraFinJuego { get; set; }

        
        [Display(Name = "Mesa")]
        public int IdMesa { get; set; }

        [Required]
        [Display(Name = "Cliente")]
        public int IdCliente { get; set; }


        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Abierto"; 

        [StringLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [Required]
        [Display(Name = "Número de Personas")]
        [Range(1, 20, ErrorMessage = "El número de personas debe estar entre 1 y 20")]
        public int NumeroPersonas { get; set; } = 1;


        [ForeignKey("IdMesa")]
        public Mesa? Mesa { get; set; }

        [ForeignKey("IdCliente")]
        public Cliente? Cliente { get; set; }
    }
}