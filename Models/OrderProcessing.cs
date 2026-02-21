using System;

namespace StockFlowPro.Models
{
    public class OrderProcessing
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public DateTime ProcessDate { get; set; }
        public int? CreatedByEmployeeId { get; set; }
        public Employee? CreatedByEmployee { get; set; }

        public int? PreparedByEmployeeId { get; set; }
        public Employee? PreparedByEmployee { get; set; }

        public int? ScannedByEmployeeId { get; set; }
        public Employee? ScannedByEmployee { get; set; }
    }
}
