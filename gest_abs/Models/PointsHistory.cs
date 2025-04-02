namespace gest_abs.Models
{
    public class PointsHistory
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime Date { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; }
        public string Type { get; set; } // "Bonus", "Malus", "Regular"

        public virtual Student Student { get; set; }
    }
}

