using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StockFlowPro.Models;

namespace StockFlowPro.ViewModels
{
    public class ScannerViewModel
    {
        [Required(ErrorMessage = "Order ID is required")]
        [Display(Name = "Order ID")]
        public int? OrderId { get; set; }

        public Order CurrentOrder { get; set; }

        [Display(Name = "Select Employee")]
        public int? SelectedEmployeeId { get; set; }
        public List<Employee> Employees { get; set; } = new List<Employee>();

        public string ErrorMessage { get; set; }
    }
}
