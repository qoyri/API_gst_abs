namespace gest_abs.DTO;

public class TeacherDTO
{
    // DTO pour la lecture d'un Teacher
    public class TeacherDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Subject { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    // DTO pour la création d'un Teacher
    public class TeacherCreateDto
    {
        public int UserId { get; set; }
        public string Subject { get; set; } = null!;
    }

    // DTO pour la mise à jour d'un Teacher
    public class TeacherUpdateDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Subject { get; set; } = null!;
    }
}