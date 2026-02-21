using System;
using System.ComponentModel.DataAnnotations;

namespace StockFlowPro.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UserId { get; set; }
        public Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }
    }
}
