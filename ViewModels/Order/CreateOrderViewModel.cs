using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockFlowPro.ViewModels.Order
{
    public class CreateOrderViewModel
    {
        [Required]
        [Display(Name = "Select Facility")]
        public int FacilityId { get; set; }
        
        public SelectList Facilities { get; set; }

        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        // For selecting products in the view
        public SelectList Products { get; set; }
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
