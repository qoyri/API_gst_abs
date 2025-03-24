namespace gest_abs.DTO
{
    public class StatsDTO
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public int TotalAbsences { get; set; }
        public int JustifiedAbsences { get; set; }
        public int UnjustifiedAbsences { get; set; } // Correction ici
        public Dictionary<string, int> AbsencesByMonth { get; set; }
    }
}