using System;
using System.Collections.Generic;
using StockFlowPro.Models;

namespace StockFlowPro.ViewModels
{
    public class HomeViewModel
    {
        public string UserName { get; set; }
        public List<Order> Orders { get; set; }
    }
}
