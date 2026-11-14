using System.ComponentModel.DataAnnotations;

namespace FastDrop.MVC.ViewModels
{
    public class SignupViewModel
    {
        [Required]
        [AllowedValues("merchant", ErrorMessage = "Public signup is available for merchants only.")]
        public string Role { get; set; } = "merchant";

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = null!;

        [Required]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;

        [Required]
        [Display(Name = "Shop Name")]
        public string ShopName { get; set; } = null!;

        [Display(Name = "Shop Address")]
        public string? ShopAddress { get; set; }
    }
}
