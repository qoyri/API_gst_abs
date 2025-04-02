using System.ComponentModel.DataAnnotations;

namespace gest_abs.DTO
{
    public class JustifyAbsenceDTO
    {
        [Required(ErrorMessage = "La raison est requise.")]
        public string Reason { get; set; }

        public string Document { get; set; }
    }
}

