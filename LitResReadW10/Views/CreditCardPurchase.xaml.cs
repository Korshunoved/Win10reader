using System;
using System.Collections.Generic;
using System.Linq;
using Digillect.Mvvm.UI;
using LitRes;
using LitRes.Models;
using LitRes.ViewModels;
using System.ComponentModel;
using Digillect.Mvvm;
using System.Text;
using System.Text.RegularExpressions;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using LitResReadW10;

namespace LitRes.Views
{
    [View("CreditCardPurchase")]
	public partial class CreditCardPurchase : CreditCardPurchaseFitting
	{
		#region Constructors/Disposer
        public CreditCardPurchase()
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
            if (e.PropertyName == "CreditEmailBlockShow")
            {
                CreditEmailBlock.Visibility = Visibility.Visible;
            }
            else if (e.PropertyName == "PriceShow")
            {
                var sum = ViewModel.Entity.Price;
                if (sum < 10) sum = 10;
                priceTitle.Text = string.Format("Покупка книги за {0} руб.", sum);
            }
        }

        private async void ValidateAndBuy(object sender, RoutedEventArgs e)
        {
            string caption = "Внимание";

            if (CreditEmailBlock.Visibility != Visibility.Collapsed)
            {
                if (creditEmail.Text == string.Empty)
                {

                    await new MessageDialog("Поле Email обязательно для заполнения", caption).ShowAsync();
                    creditEmail.Focus(FocusState.Programmatic);
                    return;
                }

                if (!Regex.IsMatch(creditEmail.Text, App.EmailRegexPattern, RegexOptions.IgnoreCase))
                {
                    await new MessageDialog("Неверный формат Email", caption).ShowAsync();
                    creditEmail.Focus(FocusState.Programmatic);
                    return;
                }
                ViewModel.UserInformation.Email = creditEmail.Text;
            }

            var creditCardNumber = new StringBuilder();
            creditCardNumber.Append(card1.Text);
            creditCardNumber.Append(card2.Text);
            creditCardNumber.Append(card3.Text);
            creditCardNumber.Append(card4.Text);
            var creditCardNumberString = creditCardNumber.ToString();

            if (creditCardNumberString == string.Empty)
            {
                await new MessageDialog("Поле Номер карты обязательно для заполнения", caption).ShowAsync();
                card1.Focus(FocusState.Programmatic);
                return;
            }

            if (!Regex.IsMatch(creditCardNumberString, @"^\d+$"))
            {
                await new MessageDialog("Неверный формат номера карты", caption).ShowAsync();
                card1.Text = string.Empty;
                card2.Text = string.Empty;
                card3.Text = string.Empty;
                card4.Text = string.Empty;
                card1.Focus(FocusState.Programmatic);
                return;
            }

            if (cardHolder.Text == string.Empty)
            {
                await new MessageDialog("Поле Держатель карты обязательно для заполнения", caption).ShowAsync();
                cardHolder.Focus(FocusState.Programmatic);
                return;
            }

            if (Regex.IsMatch(cardHolder.Text, @"\d"))
            {
                await new MessageDialog("Неверный формат Держатель карты", caption).ShowAsync();
                cardHolder.Text = string.Empty;
                cardHolder.Focus(FocusState.Programmatic);
                return;
            }

            if (cvv.Text == string.Empty)
            {
                await new MessageDialog("Поле Код CVV2/CVC2 обязательно для заполнения", caption).ShowAsync();
                cvv.Focus(FocusState.Programmatic);
                return;
            }

            if (!Regex.IsMatch(cvv.Text, @"^\d+$"))
            {
                await new MessageDialog("Неверный формат Код CVV2/CVC2", caption).ShowAsync();
                cvv.Text = string.Empty;
                cvv.Focus(FocusState.Programmatic);
                return;
            }

            if (datePickerLeft.Text == string.Empty || datePickerRight.Text == string.Empty)
            {
                await new MessageDialog("Поле Cрок действия карты обязательно для заполнения", caption).ShowAsync();
                datePickerLeft.Text = string.Empty;
                datePickerRight.Text = string.Empty;
                datePickerLeft.Focus(FocusState.Programmatic);
                return;
            }

            string expires = string.Concat(datePickerLeft.Text, datePickerRight.Text);

            if (!Regex.IsMatch(expires, @"^\d+$") || expires.Length != 4)
            {
                await new MessageDialog("Неверный срок действия карты", caption).ShowAsync();
                datePickerLeft.Focus(FocusState.Programmatic);
                return;
            }

            var parameters = new Dictionary<string, object>{
                                                            {"isSave", saveCheckBox.IsChecked != null && (bool)saveCheckBox.IsChecked},
                                                            {"isAuth", true},                                                            
                                                            {"mail",ViewModel.UserInformation.Email},
                                                            {"name",cardHolder.Text},
                                                            {"number",creditCardNumberString},
                                                            {"expires",expires},
                                                            {"cvv",cvv.Text},
                                                            {"phone",ViewModel.UserInformation.Phone}
                                                            };
            ViewModel.ShowProcessView(parameters);
            
        }

        private void card1_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = sender as TextBox;
            if (text.Text.Length >= text.MaxLength) card2.Focus(FocusState.Programmatic);            
        }

        private void card2_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = sender as TextBox;
            if (text.Text.Length >= text.MaxLength) card3.Focus(FocusState.Programmatic);
        }

        private void card3_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = sender as TextBox;
            if (text.Text.Length >= text.MaxLength) card4.Focus(FocusState.Programmatic);
        }

        private void card4_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = sender as TextBox;
            if (text.Text.Length >= text.MaxLength) datePickerLeft.Focus(FocusState.Programmatic);
        }

        private void dataPickerLeft_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = sender as TextBox;
            if (text.Text.Length >= text.MaxLength) datePickerRight.Focus(FocusState.Programmatic);
        }

        private void dataPickerRight_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = sender as TextBox;
            if (text.Text.Length >= text.MaxLength) cvv.Focus(FocusState.Programmatic);
        }

        private void CCV2_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = sender as TextBox;
            if (text.Text.Length >= text.MaxLength) cardHolder.Focus(FocusState.Programmatic);
        }

        private void Email_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                saveCheckBox.Focus(FocusState.Programmatic);
            }
        }

        private void CardHolder_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                saveCheckBox.Focus(FocusState.Programmatic);
            }
        }        
    }

    public class CreditCardPurchaseFitting : EntityPage<LitRes.Models.Book, CreditCardPurchaseViewModel>
	{
	}
}