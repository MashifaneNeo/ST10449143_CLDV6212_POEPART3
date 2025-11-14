using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10449143_CLDV6212_POEPART1.Models
{
    public class Cart
    {
        [Key]
        [Display(Name = "Cart ID")]
        public Guid CartId { get; set; } = Guid.NewGuid();

        [Required]
        [Display(Name = "User ID")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual List<CartItem> Items { get; set; } = new List<CartItem>();

        [NotMapped]
        [Display(Name = "Total Amount")]
        public double TotalAmount => Items?.Sum(item => (double)item.TotalPrice) ?? 0;

        [NotMapped]
        [Display(Name = "Total Items")]
        public int TotalItems => Items?.Sum(item => item.Quantity) ?? 0;

        [NotMapped]
        [Display(Name = "Subtotal")]
        public double Subtotal => Items?.Sum(item => (double)item.TotalPrice) ?? 0;

        [NotMapped]
        [Display(Name = "Tax Amount")]
        public double TaxAmount => Subtotal * 0.15;

        [NotMapped]
        [Display(Name = "Grand Total")]
        public double GrandTotal => Subtotal + TaxAmount;

        public Cart() { }

        public Cart(string userId, string username)
        {
            CartId = Guid.NewGuid(); 
            UserId = userId;
            Username = username;
            CreatedDate = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            IsActive = true;
            Items = new List<CartItem>();
        }
    }

    public class CartItem
    {
        [Key]
        [Display(Name = "Cart Item ID")]
        public Guid CartItemId { get; set; } = Guid.NewGuid(); 

        [Required]
        [Display(Name = "Cart ID")]
        public Guid CartId { get; set; } 

        [Required]
        [Display(Name = "Product ID")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Unit Price")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; }

        [NotMapped]
        [Display(Name = "Total Price")]
        public decimal TotalPrice => UnitPrice * Quantity;

        // Double properties for display
        [NotMapped]
        public double UnitPriceDouble => (double)UnitPrice;

        [NotMapped]
        public double TotalPriceDouble => (double)TotalPrice;

        [NotMapped]
        public Product? Product { get; set; }

        public CartItem() { }

        public CartItem(Guid cartId, string productId, string productName, decimal unitPrice, int quantity) 
        {
            CartItemId = Guid.NewGuid();
            CartId = cartId;
            ProductId = productId;
            ProductName = productName;
            UnitPrice = unitPrice;
            Quantity = quantity;
            CreatedDate = DateTime.UtcNow;
        }

        
        public CartItem(Guid cartId, string productId, string productName, double unitPrice, int quantity) 
        {
            CartItemId = Guid.NewGuid();
            CartId = cartId;
            ProductId = productId;
            ProductName = productName;
            UnitPrice = (decimal)unitPrice;
            Quantity = quantity;
            CreatedDate = DateTime.UtcNow;
        }
    }
}