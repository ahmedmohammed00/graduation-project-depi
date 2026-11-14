using System.ComponentModel.DataAnnotations;

namespace FastDrop.MVC.ViewModels;

public class MerchantCreateViewModel : MerchantEditViewModel
{
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}
