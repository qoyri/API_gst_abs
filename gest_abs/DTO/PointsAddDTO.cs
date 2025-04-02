namespace gest_abs.DTO
{
    public class PointsAddDTO
    {
        public int Points { get; set; }
        public string Reason { get; set; }
        public string Type { get; set; } // "Bonus", "Malus", "Regular"
    }
}

