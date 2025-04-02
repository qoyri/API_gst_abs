namespace gest_abs.DTO
{
    public class PointsSystemDTO
    {
        public int Id { get; set; }
        public int PointsPerJustifiedAbsence { get; set; }
        public int PointsPerUnjustifiedAbsence { get; set; }
        public int PointsPerLateArrival { get; set; }
        public int BonusPointsForPerfectAttendance { get; set; }
        public int BonusPointsPerMonth { get; set; }
    }

    public class StudentPointsDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int TotalPoints { get; set; }
        public int CurrentMonthPoints { get; set; }
        public int Rank { get; set; }
        public List<PointsHistoryDTO> PointsHistory { get; set; }
    }

    public class PointsHistoryDTO
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime Date { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; }
        public string Type { get; set; } // "Bonus", "Malus", "Regular"
    }

    public class ClassPointsDTO
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int TotalPoints { get; set; }
        public int AveragePoints { get; set; }
        public List<StudentPointsDTO> TopStudents { get; set; }
    }
}

