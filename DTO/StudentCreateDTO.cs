namespace gest_abs.DTO
{
    public class StudentCreateDTO
    {
        public int UserId { get; set; }
        public int ClassId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? Birthdate { get; set; }  // Conserver DateOnly pour le DTO
        public int? ParentId { get; set; }  // Le parent est optionnel
    }
}
 