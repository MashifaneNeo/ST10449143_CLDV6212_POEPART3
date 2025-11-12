using System.ComponentModel.DataAnnotations;

namespace ST10449143_CLDV6212_POEPART1.Models
{
    public class FileUploadModel
    {
        [Required]
        [Display(Name = "Proof of Payment")]
        public IFormFile? ProofOfPayment { get; set; }

        [Display(Name = "Order ID")]
        public string? OrderId { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }
    }
}