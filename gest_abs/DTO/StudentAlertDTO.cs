namespace gest_abs.DTO
{
    public class StudentAlertDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int TotalAbsences { get; set; }
        public int UnjustifiedAbsences { get; set; }
        public int PendingAbsences { get; set; }
        public string AlertMessage { get; set; }
    }
}

