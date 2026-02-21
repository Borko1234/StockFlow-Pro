using System;
using System.Collections.Generic;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; } 
        public int PendingOrders { get; set; }
        public List<ProductPerformanceDto> TopProducts { get; set; } = new List<ProductPerformanceDto>();
        public List<MonthlyProfitDto> MonthlyProfits { get; set; } = new List<MonthlyProfitDto>();
    }

    public class ProductPerformanceDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal ProfitMargin { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    public class MonthlyProfitDto
    {
        public string Month { get; set; }
        public decimal Profit { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminOrderListViewModel
    {
        public List<StockFlowPro.Models.Order> Orders { get; set; } = new List<StockFlowPro.Models.Order>();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public OrderStatus? Status { get; set; }
        public int? FacilityId { get; set; }
    }
}
