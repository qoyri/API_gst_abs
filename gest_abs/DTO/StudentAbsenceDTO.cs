namespace gest_abs.DTO
{
    public class StudentAbsenceDTO
    {
        public int Id { get; set; }
        public DateOnly AbsenceDate { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Document { get; set; }
    }

    public class StudentAbsenceDetailDTO : StudentAbsenceDTO
    {
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ClassName { get; set; }
    }
}

