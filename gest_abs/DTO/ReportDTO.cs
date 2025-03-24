using System;
using System.Collections.Generic;

namespace gest_abs.DTO
{
    public class ReportFilterDTO
    {
        public int? ClassId { get; set; }
        public int? StudentId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Format { get; set; } = "json"; // json, pdf, excel
        public bool IncludeAllStatuses { get; set; } = true;
    }

    public class AbsenceReportDTO
    {
        public DateTime GeneratedAt { get; set; }
        public string ReportPeriod { get; set; }
        public int TotalAbsences { get; set; }
        public int JustifiedAbsences { get; set; }
        public int UnjustifiedAbsences { get; set; }
        public int PendingAbsences { get; set; }
        public List<AbsenceDTO> Absences { get; set; }
        public Dictionary<string, int> AbsencesByClass { get; set; }
        public Dictionary<string, int> AbsencesByStudent { get; set; }
        public Dictionary<string, int> AbsencesByMonth { get; set; }
        public Dictionary<string, int> AbsencesByDay { get; set; }
    }

    public class ClassStatisticsDTO
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int StudentCount { get; set; }
        public int TotalAbsences { get; set; }
        public int JustifiedAbsences { get; set; }
        public int UnjustifiedAbsences { get; set; }
        public int PendingAbsences { get; set; }
        public double AbsenceRate { get; set; }
        public List<StudentAbsenceStatDTO> StudentStats { get; set; }
        public Dictionary<string, int> AbsencesByMonth { get; set; }
        public Dictionary<string, int> AbsencesByDay { get; set; }
    }

    public class StudentStatisticsDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int TotalAbsences { get; set; }
        public int JustifiedAbsences { get; set; }
        public int UnjustifiedAbsences { get; set; }
        public int PendingAbsences { get; set; }
        public double AbsenceRate { get; set; }
        public List<AbsenceDTO> RecentAbsences { get; set; }
        public Dictionary<string, int> AbsencesByMonth { get; set; }
    }

    public class StudentAbsenceStatDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int TotalAbsences { get; set; }
        public int JustifiedAbsences { get; set; }
        public int UnjustifiedAbsences { get; set; }
        public int PendingAbsences { get; set; }
        public double AbsenceRate { get; set; }
    }
}

