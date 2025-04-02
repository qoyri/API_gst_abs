namespace gest_abs.Models
{
    public class PointsConfig
    {
        public int Id { get; set; }
        public int PointsPerJustifiedAbsence { get; set; }
        public int PointsPerUnjustifiedAbsence { get; set; }
        public int PointsPerLateArrival { get; set; }
        public int BonusPointsForPerfectAttendance { get; set; }
        public int BonusPointsPerMonth { get; set; }
    }
}

