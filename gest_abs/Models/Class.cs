using System;
using System.Collections.Generic;

namespace gest_abs.Models;

public partial class Class
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? TeacherId { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual Teacher? Teacher { get; set; }
}
