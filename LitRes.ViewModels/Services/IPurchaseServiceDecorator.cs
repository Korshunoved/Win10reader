using System.Threading.Tasks;
using LitRes.Models;

namespace LitRes.Services
{
	public interface IPurchaseServiceDecorator
	{
		void UpdateBook( Book currentBook, bool withDownload = false );
        void UpdateBookFailed(Book art);
        void UpdateAccountDeposit();
        void UpdateAccountDepositFailed();
	    Task RefreshPages(Book book);
        Task RefreshAccountPage();
	}
}
