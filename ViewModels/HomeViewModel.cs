using System;
using System.Collections.Generic;
using StockFlowPro.Models;

namespace StockFlowPro.ViewModels
{
    public class HomeViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public List<StockFlowPro.Models.Order> TodaysOrders { get; set; } = new List<StockFlowPro.Models.Order>();
    }
}
