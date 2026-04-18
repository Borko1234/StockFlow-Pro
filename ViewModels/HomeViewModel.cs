using System;
using System.Collections.Generic;
using StockFlowPro.Models;

namespace StockFlowPro.ViewModels
{
    public class HomeViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public List<StockFlowPro.Models.Order> Orders { get; set; } = new List<StockFlowPro.Models.Order>();
        public List<StockFlowPro.Models.Order> TodaysOrders { get; set; } = new List<StockFlowPro.Models.Order>();
    }

    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int FacilityCount { get; set; }
        public int ActiveOrdersCount { get; set; }
        public List<StockFlowPro.Models.Order> RecentOrders { get; set; } = new List<StockFlowPro.Models.Order>();
        public List<MonthlyMovementDto> WeeklyMovements { get; set; } = new List<MonthlyMovementDto>();
    }

    public class MonthlyMovementDto
    {
        public string Day { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
    }
}
