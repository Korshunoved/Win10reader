using System.Threading.Tasks;
using LitRes.Models;

namespace LitRes.Services
{
	public interface IInAppPurchaseService
	{
		Task<Purchase> BuyBook( Book book );
        Task<Purchase> AddToDeposit(DepositType dt);
		void CheckProductIsUsed( string productId );
        void CheckProductIsUsed(DepositType dt);
	}
}
