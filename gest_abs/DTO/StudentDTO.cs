namespace gest_abs.DTO
{
    public class StudentDTO
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public DateOnly? Birthdate { get; set; }
        // Vous pourrez ajouter d'autres propriétés si nécessaire
    }
}