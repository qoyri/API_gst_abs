using System;
using System.ComponentModel.DataAnnotations;

namespace gest_abs.DTO
{
    public class StudentDTO
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateOnly? Birthdate { get; set; }
    }
}

