using System;
using System.ComponentModel.DataAnnotations;

namespace gest_abs.DTO
{
    public class RoomDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Capacity { get; set; }
        public string Location { get; set; }
    }

    public class RoomCreateDTO
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        public int? Capacity { get; set; }
        
        [StringLength(100)]
        public string Location { get; set; }
    }

    public class RoomUpdateDTO
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        public int? Capacity { get; set; }
        
        [StringLength(100)]
        public string Location { get; set; }
    }
}