using System;
using System.Collections.Generic;

namespace gest_abs.Models;

public partial class Absence
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public DateOnly AbsenceDate { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public string? Document { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Student Student { get; set; } = null!;
}
