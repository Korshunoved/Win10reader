using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.Models;
using LitRes.ViewModels;

namespace LitRes.Views
{
	[View( "Settings" )]
	public partial class Settings : SettingsFitting
	{
		private bool _loaded;

	    private Reader _readerPage;

	    private int CurrentFontSize = 0;

	    public static Settings Instance;

		#region Constructors/Disposer
		public Settings()
		{
			InitializeComponent();

			Loaded += Settings_Loaded;		   
		}
		#endregion

	    protected override Session CreateDataSession(DataLoadReason reason)
	    {
            ViewModel.IsDefaultSettings = !ViewParameters.GetValue<bool>("PDFBook");        
            return base.CreateDataSession(reason);
	    }

		void Settings_Loaded( object sender, RoutedEventArgs e )
		{
			_loaded = true;
		    Instance = this;
		    CurrentFontSize = (int)FontSizeSlider.Value;
            Analytics.Instance.sendMessage(Analytics.ViewSettingsReader);
            if (_readerPage == null)
                _readerPage = Reader.Instance;
		    try
		    {
		        var font = _readerPage.ViewModel.ReaderSettings.Font;
		        switch (font)
		        {
                    case 1:
		                SansFont1.IsChecked = true;
		                break;
                    case 2:
                        SerifFont2.IsChecked = true;
                        break;
                    case 3:
                        MonoFont3.IsChecked = true;
                        break;
                }
		    }
		    catch (Exception)
		    {
		       //
		    }
		}

	    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
	    {
            await ViewModel.Save();
	        _readerPage.ViewModel.SaveSettings();
            base.OnNavigatingFrom(e);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //await ViewModel.Load();
            base.OnNavigatedTo(e);
        }

        private void SliderManipulationCompleted( object sender, ManipulationCompletedEventArgs e )
		{
			( ( Slider ) sender ).Value = Math.Round( ( ( Slider ) sender ).Value );
		}

        private void LightSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;            
            var slider = sender as Slider;
            if (slider == null) return;
            _readerPage.ViewModel.ReaderSettings.Brightness = 1 - (float) slider.Value/100;
            
        }

        private void AnimationSwither_Toggled(object sender, RoutedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var toggle = sender as ToggleSwitch;
            if (toggle == null) return;
            _readerPage.CurrentFlipView.UseTouchAnimationsForAllNavigation = toggle.IsOn;
        }

        private void FontSizeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (CurrentFontSize == (int) FontSizeSlider.Value) return;
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = sender as Slider;
            if (slider == null) return;
            _readerPage.ViewModel.ReaderSettings.FontSize = (int) slider.Value + 20;
            CurrentFontSize = (int)FontSizeSlider.Value;
            _readerPage.ChangeFontSize();
        }

        private void FontRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton?.IsChecked == true)
            {
                radioButton.Style = (Style)Application.Current.Resources["LitResRadioButtonStyle1"];
                _readerPage.ViewModel.ReaderSettings.Font = int.Parse(Regex.Match(radioButton.Name, @"\d+").Value);
                _readerPage.ChangeFont();
            }
        }

        private void Font_Unchecked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null) radioButton.Style = null;
        }
	}

    public class SettingsFitting : ViewModelPage<ReaderSettingsViewModel>
	{
	}
}