namespace gest_abs.DTO
{
    public class StudentUpdateDTO
    {
        public int ClassId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateOnly? Birthdate { get; set; }
    }
}