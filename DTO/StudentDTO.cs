namespace gest_abs.DTO
{
    public class StudentDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ClassId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? Birthdate { get; set; }
        public int? ParentId { get; set; } // ParentId est optionnel
    }
}
