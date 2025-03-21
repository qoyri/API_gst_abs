namespace gest_abs.DTO
{
    public class AbsenceDTO
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateOnly AbsenceDate { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public string? Document { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
