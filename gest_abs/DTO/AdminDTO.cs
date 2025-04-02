namespace gest_abs.DTO;

public class AdminDTO
{
    // DTO pour la lecture d'un utilisateur
    public class UserDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }

    // DTO pour la création d'un utilisateur
    public class UserCreateDto
    {
        public string Role { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    // DTO pour la mise à jour d'un utilisateur
    public class UserUpdateDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Password { get; set; } // Optionnel: ne sera modifié que si non nul
    }

    // DTO pour réinitialiser le mot de passe d'un utilisateur
    public class ResetPasswordDto
    {
        public string NewPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}

