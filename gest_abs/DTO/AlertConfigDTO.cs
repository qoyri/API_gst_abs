namespace gest_abs.DTO
{
    public class AlertConfigDTO
    {
        public int Id { get; set; }
        public int MaxAbsencesBeforeAlert { get; set; }
        public bool NotifyParents { get; set; }
        public bool NotifyTeachers { get; set; }
        public bool NotifyAdmin { get; set; }
        public string AlertMessage { get; set; }
    }
}

