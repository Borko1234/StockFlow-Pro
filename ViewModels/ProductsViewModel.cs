using System.Collections.Generic;
using StockFlowPro.Models;

namespace StockFlowPro.ViewModels
{
    public class ProductsViewModel
    {
        public int OrderId { get; set; }
        public Order CurrentOrder { get; set; }
        
        // Using a dictionary to track scanned quantities or a separate list?
        // For simplicity, we can just use the OrderItems and maybe a client-side or session-based tracking of "scanned" status if not persisting immediately.
        // However, the prompt implies "Real-time inventory checking and availability status updates".
        // Let's stick to the Order object which has OrderItems.
        
        public string SearchText { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
    }
}
