using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StockFlowPro.Models
{
    public class Facility
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Area { get; set; }
        public string RepresentativeName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Order> Orders { get; set; }
    }
}
