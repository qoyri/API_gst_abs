using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace gest_abs.DTO
{
    public class TeacherDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Subject { get; set; }
        public string Email { get; set; }
    }

    public class TeacherProfileDTO
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class TeacherUpdateProfileDTO
    {
        public string Subject { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}