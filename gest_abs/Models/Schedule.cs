namespace gest_abs.Models;

public partial class Schedule
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Subject { get; set; } = null!;
    public string? Description { get; set; }

    public virtual Class Class { get; set; } = null!;
}

