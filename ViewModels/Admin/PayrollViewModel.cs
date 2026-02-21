using System;
using System.Collections.Generic;

namespace StockFlowPro.ViewModels.Admin
{
    public class PayrollViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RatePerOrder { get; set; } = 0.50m;
        public List<EmployeePayrollItem> Items { get; set; } = new List<EmployeePayrollItem>();
    }

    public class EmployeePayrollItem
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
