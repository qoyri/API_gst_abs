namespace gest_abs.DTO;

public class AbsenceDTO
{
    public int Id { get; set; }
    public string StudentName { get; set; }
    public DateTime AbsenceDate { get; set; }
    public string Reason { get; set; }
    public string Status { get; set; }
}
