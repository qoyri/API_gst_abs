namespace gest_abs.DTO
{
    public class ReportExportDTO
    {
        public string ReportType { get; set; } // "Student", "Class", "Teacher", "Global"
        public string Format { get; set; } // "PDF", "Excel", "CSV"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? StudentId { get; set; }
        public int? ClassId { get; set; }
        public int? TeacherId { get; set; }
        public bool IncludePoints { get; set; }
        public bool IncludeAbsences { get; set; }
        public bool IncludeJustifications { get; set; }
        public bool IncludeStatistics { get; set; }
    }
}

