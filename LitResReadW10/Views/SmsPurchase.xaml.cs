using System;
using System.Collections.Generic;
using System.Linq;
using Digillect.Mvvm.UI;
using LitRes;
using LitRes.Models;
using LitRes.ViewModels;
using System.Windows;
using Autofac;
using System.Windows.Controls;
using System.ComponentModel;
using Digillect.Mvvm;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;

namespace LitRes.Views
{
    [View("SmsPurchase")]
	public partial class SmsPurchase : SmsPurchaseFitting
	{
		#region Constructors/Disposer
        public SmsPurchase()
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
            if (e.PropertyName == "SmsLoaded")
            {
                var sum = ViewModel.Entity.Price;
                if (sum < 10) sum = 10;
                priceTitle.Text = string.Format("Покупка книги за {0} руб.", sum);
                CountriesPicker.ItemsSource = ViewModel.Countries;
            }
        }

        private void List_Picker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Summ.Text = string.Empty;
            FullSumm.Text = string.Empty;

            var picker = sender as Microsoft.Phone.Controls.ListPicker;
            if (picker.SelectedItem != null) OperatorPicker.ItemsSource = (picker.SelectedItem as LitRes.Models.Country).Operators;
        }

        private void Operator_List_Picker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var picker = sender as Microsoft.Phone.Controls.ListPicker;
            if (picker.SelectedItem != null)
            {
                var oprator = picker.SelectedItem as LitRes.Models.Operator;

                var num = getSmsNumber();

                if (num != null)
                {
                    string money = "";
                    foreach (var country in ViewModel.Countries)
                    {
                        if (country.Operators.Contains(oprator))
                        {
                            money = country.Money;
                            break;
                        }
                    }
                    Summ.Text = string.Format("Счет будет пополнен на {0} руб.", num.Summ);
                    FullSumm.Text = string.Format("Полная стоймость sms - {0} {1}", num.Cost, money);
                    BuyButton.Visibility = Visibility.Visible;
                }
                else
                {
                    Summ.Text = string.Empty;
                    FullSumm.Text = string.Empty;
                    Summ.Text = "Невозможно произвести оплату на такую сумму у данного мобильного оператора.";
                    BuyButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                BuyButton.Visibility = Visibility.Collapsed;
            }
        }

        private Number getSmsNumber()
        {
            Number num = null;

            if (OperatorPicker.SelectedItem != null)
            {
                var sum = ViewModel.Entity.Price;
                if (sum < 10) sum = 10;
                var oprator = OperatorPicker.SelectedItem as LitRes.Models.Operator;
                foreach (var number in oprator.Numbers)
                {
                    if (number.Summ >= sum)
                    {
                        num = number;
                        break;
                    }
                }
            }
            return num;
        }

        private void SendSmsTap(object sender, System.Windows.RoutedEventArgs e)
        {
            var number = getSmsNumber();
            if (number != null)
            {
                new Task(async () =>
                {
                    try
                    {
                        await ViewModel.RunSmsLauncher(number);
                    }
                    catch { }
                }).Start();
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (ViewModel.Entity != null && e.IsNavigationInitiator == false && e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                NavigationService.StopLoading();
                ViewModel.ShowSmsProcessView();               
            }
        }
    }

    public class SmsPurchaseFitting : EntityPage<int, LitRes.Models.Book, SmsPurchaseViewModel>
	{       
	}
}