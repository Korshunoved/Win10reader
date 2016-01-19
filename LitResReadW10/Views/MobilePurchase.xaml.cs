using System;
using System.Collections.Generic;
using System.Linq;
using Digillect.Mvvm.UI;
using LitRes;
using LitRes.Models;
using LitRes.ViewModels;
using System.Windows;
using Autofac;
using System.ComponentModel;
using Digillect.Mvvm;
using System.Threading.Tasks;

namespace LitRes.Views
{
    [View("MobilePurchase")]
	public partial class MobilePurchase : MobilePurchaseFitting
	{
		#region Constructors/Disposer
        public MobilePurchase()
		{
			InitializeComponent();
		}
		#endregion			

        #region CreateDataSession
        protected override Session CreateDataSession(DataLoadReason reason)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            return base.CreateDataSession(reason);
        }
        #endregion

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "WithoutPhoneBlockEnable")
            {
                var sum = ViewModel.Entity.Price;
                if (sum < 10) sum = 10;
                priceTitle.Text = string.Format("Покупка книги за {0} руб.", sum);

                WithoutNumberBlock.Visibility = Visibility.Visible;
            }
            else if (e.PropertyName == "WithPhoneBlockEnable")
            {
                var sum = ViewModel.Entity.Price;
                if (sum < 10) sum = 10;
                priceTitle.Text = string.Format("Покупка книги за {0} руб.", sum);

                WithNumberBlock.Visibility = Visibility.Visible;
                tbPhone2.Text = ViewModel.UserInformation.Phone;
            }
        }

        private void MobileCommerceOk(object sender, System.Windows.RoutedEventArgs e)
        {
            string caption = "Внимание";
            if (tbPhone.Text.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(string.Concat("+7", tbPhone.Text), App.PhoneRegexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                System.Windows.MessageBox.Show("Номер телефона должен быть в формате +7xxxxxxxxxx", caption, System.Windows.MessageBoxButton.OK);
                tbPhone.Focus();
                return;
            }
            if (tbPhone.Text.Length == 0)
            {
                System.Windows.MessageBox.Show("Пожалуйста введите номер телефона в формате +7xxxxxxxxxx", caption, System.Windows.MessageBoxButton.OK);
                tbPhone.Text = string.Empty;
                tbPhone.Focus();
                return;
            }
            else
            {
               ViewModel.RunMobileCommerceSaveNubmerAndStart(string.Concat("+7", tbPhone.Text), (bool)saveCheckBox.IsChecked);
            }
        }
    }

    public class MobilePurchaseFitting : EntityPage<int, LitRes.Models.Book, MobilePurchaseViewModel>
	{
	}
}