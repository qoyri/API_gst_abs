using System;
using System.Collections.Generic;

namespace gest_abs.Models;

public partial class Reservation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoomId { get; set; }

    public DateOnly ReservationDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

