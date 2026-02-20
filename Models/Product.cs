using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockFlowPro.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Brand { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Selling Price

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; } // Cost Price for Profit Calc
        
        public int QuantityInStock { get; set; }
        public int MinimumStockLevel { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
