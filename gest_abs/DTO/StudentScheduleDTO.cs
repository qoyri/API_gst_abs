namespace gest_abs.DTO
{
    public class StudentScheduleDTO
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
    }
}

