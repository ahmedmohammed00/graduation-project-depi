using System.ComponentModel.DataAnnotations;

namespace FastDrop.MVC.ViewModels;

public class ProfileEditViewModel
{
    [Required, StringLength(150)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Display(Name = "Contact name"), StringLength(150)]
    public string? ContactName { get; set; }

    [Display(Name = "Phone number"), Phone, StringLength(30)]
    public string? Phone { get; set; }

    [Display(Name = "Current password"), StringLength(100), DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, StringLength(100), Display(Name = "Current password"), DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8), Display(Name = "New password"), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required, Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match."),
     Display(Name = "Confirm new password"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class ProfileViewModel
{
    public ProfileEditViewModel Details { get; set; } = new();
    public ChangePasswordViewModel Password { get; set; } = new();
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsMerchant { get; set; }
    public bool IsDriver { get; set; }
}
