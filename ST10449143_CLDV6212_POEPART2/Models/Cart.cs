// Models/Cart.cs
using System.ComponentModel.DataAnnotations;

namespace ST10449143_CLDV6212_POEPART1.Models
{
    public class Cart
    {
        [Display(Name = "Cart ID")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Customer ID")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation property
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public double TotalAmount => Items.Sum(item => item.TotalPrice);

        [Display(Name = "Total Items")]
        public int TotalItems => Items.Sum(item => item.Quantity);
    }

    public class CartItem
    {
        [Display(Name = "Cart Item ID")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Cart ID")]
        public string CartId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product ID")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public double UnitPrice { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public double TotalPrice => UnitPrice * Quantity;

        // Make Product nullable to avoid serialization issues
        public Product? Product { get; set; }
    }
}