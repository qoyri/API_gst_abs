using System;
using System.Collections.Generic;

namespace gest_abs.Models;

public partial class Teacher
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Subject { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual User User { get; set; } = null!;
}

