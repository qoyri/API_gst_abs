using System;
using System.ComponentModel.DataAnnotations;

namespace gest_abs.DTO
{
    public class AbsenceDTO
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public DateOnly AbsenceDate { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Document { get; set; }
    }

    public class AbsenceDetailDTO : AbsenceDTO
    {
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AbsenceCreateDTO
    {
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public DateTime AbsenceDate { get; set; }
        
        public string Status { get; set; }
        
        public string Reason { get; set; }
        
        public string Document { get; set; }
    }

    public class AbsenceUpdateDTO
    {
        [Required]
        public DateTime AbsenceDate { get; set; }
        
        public string Status { get; set; }
        
        public string Reason { get; set; }
        
        public string Document { get; set; }
    }

    public class AbsenceFilterDTO
    {
        public int? ClassId { get; set; }
        public int? StudentId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
    }
}