using System.Collections.Generic;
using StockFlowPro.Models;

namespace StockFlowPro.ViewModels
{
    public class ScannerViewModel
    {
        public StockFlowPro.Models.Order? CurrentOrder { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
