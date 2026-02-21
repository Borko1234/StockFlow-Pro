using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockFlowPro.ViewModels.Order
{
    public class CreateOrderViewModel
    {
        public int FacilityId { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
        
        // For Dropdowns
        public SelectList? Facilities { get; set; }
        public SelectList? Products { get; set; }
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
