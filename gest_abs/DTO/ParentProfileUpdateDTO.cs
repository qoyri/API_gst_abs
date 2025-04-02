using System.ComponentModel.DataAnnotations;

namespace gest_abs.DTO
{
    public class ParentProfileUpdateDTO
    {
        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        public string? Email { get; set; }
        
        public string? CurrentPassword { get; set; }
        
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères.")]
        public string? NewPassword { get; set; }
    }
}

