namespace gest_abs.DTO
{
    public class StudentRankingDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int TotalAbsences { get; set; }
        public int JustifiedAbsences { get; set; }
        public int UnjustifiedAbsences { get; set; }
        public int Rank { get; set; }
        public bool IsCurrentStudent { get; set; }
    }
}

