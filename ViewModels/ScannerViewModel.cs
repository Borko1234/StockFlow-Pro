using System.Collections.Generic;
using StockFlowPro.Models;

namespace StockFlowPro.ViewModels
{
    public class ScannerViewModel
    {
        public StockFlowPro.Models.Order? CurrentOrder { get; set; }
        public List<ScannerItemViewModel> Items { get; set; } = new List<ScannerItemViewModel>();
        public List<int> RemainingProductIds { get; set; } = new List<int>();
        public string RemainingProductIdsCsv { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int RemainingItems { get; set; }
        public bool AllItemsScanned { get; set; }
        public List<ScannerPackerViewModel> Packers { get; set; } = new List<ScannerPackerViewModel>();
        public int? SelectedPackerId { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }

    public class ScannerItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsScanned { get; set; }
    }

    public class ScannerPackerViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}
