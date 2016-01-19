using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LitRes.Models;

namespace LitRes.Services
{
	public interface ILitresPurchaseService
	{
        Task Deposit(DepositType deposit, CancellationToken cancellationToken);
		Task BuyBook( Book book, CancellationToken cancellationToken );
        Task BuyBookFromLitres(Book book, CancellationToken cancellationToken);
        Task MobileCommerceInit(double sum, string phone,Book book, CancellationToken cancellationToken);
        Task<SmsResponse> GetSmsPaymentInfo(CancellationToken cancellationToken);
        Task StartSmsPaymentListener(Book book, CancellationToken cancellationToken);
        Task<CreditCardInitResponse> CreditCardInit(double sum, bool isAuth, bool preventrebil, CancellationToken cancellationToken);
        Task<bool> CreditCardPayment(Book book, double sum, bool isAuth, bool preventrebil, IDictionary<string, object> parameters, CancellationToken cancellationToken);
        Task TakeBookFromLitres(Book book, CancellationToken cancellationToken);
	    Task Process3ds(CancellationToken cancellationToken);
	}
    public enum DepositType
    {
        Poor, Normal, Rich
    }
}
