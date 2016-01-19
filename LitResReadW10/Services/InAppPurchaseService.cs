using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Threading.Tasks;

using Digillect.Mvvm.Services;

using LitRes.Exceptions;
using LitRes.Models;

using Windows.ApplicationModel.Store;

namespace LitRes.Services
{
	public class InAppPurchaseService : IInAppPurchaseService
	{
		private readonly INavigationService _navigationService;
		private ListingInformation _listings;
		private readonly Dictionary<double, ProductListing> _priceToListing = new Dictionary<double, ProductListing>();
        public readonly string[] depositIds = { "ru.litres.wp8.refillbalance_14900", "ru.litres.wp8.refillbalance_26900", "ru.litres.wp8.refillbalance_43400" };
		#region Constructors/Disposer
		/// <summary>
		/// Initializes a new instance of the InAppPurchaseService class.
		/// </summary>
		/// <param name="navigationService"></param>
		public InAppPurchaseService( INavigationService navigationService )
		{
			_navigationService = navigationService;
		}
		#endregion

		private async Task GetInformationFromStore()
		{
			try
			{
				_listings = await CurrentApp.LoadListingInformationAsync();
			}
			catch( Exception ex )
			{
				throw;
			}
		    if (_listings.ProductListings != null)
		    {
		        foreach (var productListing in _listings.ProductListings.Values)
		        {
		            int price = 0;

		            if (int.TryParse(productListing.Tag, out price))
		            {
		                _priceToListing.Add(((double) price)/100, productListing);
		            }
		        }
		    }
		}

		private async Task<ProductListing> FindProductForBook( Book book )
		{
			if( string.IsNullOrWhiteSpace( book.InappName ) )
			{
				throw new CatalitPurchaseException( "Книга не содержит описания продукта", 100 );
			}

			if( _listings == null )
			{
				await GetInformationFromStore();
			}
           
		    var res = _listings.ProductListings.Count;
            Debug.WriteLine(res);

            if (_listings != null && _listings.ProductListings.Any( x => x.Key == book.InappName ) )
			{
				return _listings.ProductListings[book.InappName];
			}
			else
			{
				throw new CatalitPurchaseException( "Продукт не найден", 100 );
			}
		}

        private async Task<ProductListing> FindProductForId(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new CatalitPurchaseException("Покупка не содержит описания продукта", 100);
            }

            if (_listings == null)
            {
                await GetInformationFromStore();
            }

            if (_listings != null && _listings.ProductListings.Any(x => x.Key == productId))
            {
                return _listings.ProductListings[productId];
            }
            else
            {
                throw new CatalitPurchaseException("Продукт не найден", 100);
            }
        }

		public void CheckProductIsUsed( string productId )
		{
			if( CurrentApp.LicenseInformation.ProductLicenses[productId].IsActive )
			{
				CurrentApp.ReportProductFulfillment( productId );
			}
		}

	    public void CheckProductIsUsed(DepositType dt)
	    {
	        CheckProductIsUsed(depositIds[(int) dt]);
	    }

		public async Task<Purchase> BuyBook( Book book )
		{
			ProductListing product = await FindProductForBook( book );

			string receiptString;
			try
			{
				if( CurrentApp.LicenseInformation.ProductLicenses[product.ProductId].IsActive )
				{
					receiptString = await CurrentApp.GetProductReceiptAsync( product.ProductId );
				}
				else
				{
					receiptString = await CurrentApp.RequestProductPurchaseAsync( product.ProductId, true );
				}
			}
			catch( COMException e)
			{
				return null;
			}
	
			try
			{
				var document = XDocument.Parse( receiptString );
				var receipt = document.DescendantNodes().OfType<XElement>().FirstOrDefault(x => x.Name.LocalName == "ProductReceipt");

				if( receipt != null )
				{
					var productIdAttribute = receipt.Attribute( "ProductId" );

					if( productIdAttribute == null || productIdAttribute.Value != product.ProductId )
					{
						throw new InvalidOperationException( "Windows Store purchase failed." );
					}
				}
				else
				{
					throw new InvalidOperationException( "Windows Store purchase failed." );
				}
			}
			catch( InvalidOperationException )
			{
				throw;
			}
			catch( Exception ex )
			{
				throw new InvalidOperationException( "Windows Store purchase failed.", ex );
			}

			return new Purchase { Art = book.Id, Win8Inapp = receiptString, ProductId = product.ProductId };
		}

        public async Task<Purchase> AddToDeposit(DepositType dt)
	    {
            ProductListing product = await FindProductForId(depositIds[(int)dt]);

            string receiptString;
            try
            {
                if (CurrentApp.LicenseInformation.ProductLicenses[product.ProductId].IsActive)
                {
                    receiptString = await CurrentApp.GetProductReceiptAsync(product.ProductId);
                }
                else
                {
                    receiptString = await CurrentApp.RequestProductPurchaseAsync(product.ProductId, true);
                }
            }
            catch (COMException e)
            {
                return null;
            }

            try
            {
                var document = XDocument.Parse(receiptString);
                var receipt = document.DescendantNodes().OfType<XElement>().FirstOrDefault(x => x.Name.LocalName == "ProductReceipt");

                if (receipt != null)
                {
                    var productIdAttribute = receipt.Attribute("ProductId");

                    if (productIdAttribute == null || productIdAttribute.Value != product.ProductId)
                    {
                        throw new InvalidOperationException("Windows Store purchase failed.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Windows Store purchase failed.");
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Windows Store purchase failed.", ex);
            }

            return new Purchase { Art = 0, Win8Inapp = receiptString, ProductId = product.ProductId };
	    }
	}
}
