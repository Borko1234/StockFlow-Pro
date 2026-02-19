using System;
using System.ComponentModel.DataAnnotations;

namespace StockFlowPro.Models
{
    public class Employee
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; }
        public string Position { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
