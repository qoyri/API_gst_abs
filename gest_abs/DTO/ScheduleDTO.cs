using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace gest_abs.DTO
{
    public class ScheduleDTO
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
    }

    public class ScheduleCreateDTO
    {
        [Required]
        public int ClassId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        public TimeSpan EndTime { get; set; }
        
        [StringLength(100)]
        public string Subject { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
    }

    public class ScheduleUpdateDTO
    {
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public TimeSpan StartTime { get; set; }
        
        [Required]
        public TimeSpan EndTime { get; set; }
        
        [StringLength(100)]
        public string Subject { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
    }
}

