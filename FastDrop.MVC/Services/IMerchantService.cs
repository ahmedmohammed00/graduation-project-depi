using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using FastDrop.MVC.ViewModels;

namespace FastDrop.MVC.Services
{
    public interface IMerchantService
    {
        Task<IEnumerable<Merchant>> GetMerchantsAsync();
        Task<Merchant?> GetMerchantByIdAsync(int id);
        Task<Merchant?> GetMerchantForDeleteAsync(int id);
        Task CreateMerchantAsync(MerchantCreateViewModel model);
        Task<bool> UpdateMerchantAsync(MerchantEditViewModel model);
        Task DeleteMerchantAsync(int id);
        bool MerchantExists(int id);

        Task<IEnumerable<Address>> GetAddressesAsync();
    }
}
