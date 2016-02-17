using System;
using System.Text.RegularExpressions;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;

// ReSharper disable CheckNamespace
namespace LitRes.Views
    // ReSharper restore CheckNamespace
{
	[View( "Settings" )]
	public partial class Settings
	{

	    private Reader _readerPage;

	    private int _currentFontSize;

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
		    Instance = this;
		    _currentFontSize = (int)FontSizeSlider.Value;
            Analytics.Instance.sendMessage(Analytics.ViewSettingsReader);
		    var deviceFamily = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
		    if (deviceFamily.Contains("Desktop"))
		    {
		        AutorotateTextBlock.Visibility = Visibility.Collapsed;
                AutorotateSwither.Visibility = Visibility.Collapsed;		        
		    }
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

            LightSlider.ValueChanged -= LightSlider_ValueChanged;
            LightSlider.ValueChanged += LightSlider_ValueChanged;

            AnimationSwither.Toggled -= AnimationSwither_Toggled;
            AnimationSwither.Toggled += AnimationSwither_Toggled;

		    FontSizeSlider.ValueChanged -= FontSizeSlider_ValueChanged;
            FontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;		  

		    JustificationSwither.Toggled -= JustificationSwither_Toggled;
            JustificationSwither.Toggled += JustificationSwither_Toggled;

		    SideIndentSlider.ValueChanged -= SideIndentSlider_ValueChanged;
            SideIndentSlider.ValueChanged += SideIndentSlider_ValueChanged;

		    LineSpacingSlider.ValueChanged -= LineSpacingSlider_ValueChanged;
            LineSpacingSlider.ValueChanged += LineSpacingSlider_ValueChanged;
        }

	    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
	    {
            await ViewModel.Save();	      
            base.OnNavigatingFrom(e);
        }

        private void LightSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;            
            var slider = sender as Slider;
            if (slider == null) return;
            if (ViewModel != null)
            ViewModel.Brightness = 1 - (float)slider.Value / 100;
            _readerPage.ViewModel.ReaderSettings.Brightness = 1 - (float) slider.Value/100;
            _readerPage.ViewModel.SaveSettings();
        }

        private void AnimationSwither_Toggled(object sender, RoutedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var toggle = sender as ToggleSwitch;
            if (toggle == null) return;
            if (ViewModel != null)
                ViewModel.AnimationMoveToPage = toggle.IsOn;
            _readerPage.CurrentFlipView.UseTouchAnimationsForAllNavigation = toggle.IsOn;            
            _readerPage.ViewModel.SaveSettings();
        }

        private void FontSizeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_currentFontSize == (int) FontSizeSlider.Value) return;
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = sender as Slider;
            if (slider == null) return;
            _readerPage.ViewModel.ReaderSettings.FontSize = (int) slider.Value;
            _currentFontSize = (int)FontSizeSlider.Value;
            if (ViewModel != null)
                ViewModel.FontSize = (int)slider.Value;
            _readerPage.ChangeFontSize();
            _readerPage.ViewModel.SaveSettings();
        }

        private void FontRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton?.IsChecked == true)
            {
                radioButton.Style = (Style)Application.Current.Resources["LitResRadioButtonStyle1"];
                ViewModel.Font = int.Parse(Regex.Match(radioButton.Name, @"\d+").Value);
                _readerPage.ViewModel.ReaderSettings.Font = int.Parse(Regex.Match(radioButton.Name, @"\d+").Value);
                _readerPage.ViewModel.SaveSettings();
                _readerPage.ChangeFont();
            }
        }

        private void Font_Unchecked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null) radioButton.Style = null;
        }

        private void JustificationSwither_Toggled(object sender, RoutedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var toggle = sender as ToggleSwitch;
            if (toggle == null) return;
            if (ViewModel != null)
                ViewModel.FitWidth = toggle.IsOn;
            _readerPage.ViewModel.ReaderSettings.FitWidth = toggle.IsOn;
            _readerPage.ViewModel.SaveSettings();
            _readerPage.ChangeJustification();
        }

        private void LightSlider_Loaded(object sender, RoutedEventArgs e)
        {
            var slider = sender as Slider;
            if (slider == null) return;
            if (ViewModel != null)
                slider.Value = (1 - ViewModel.Brightness)*100;
        }

        private void RadioButton_Loaded(object sender, RoutedEventArgs e)
        {
            var fontValue = 0;
            if (ViewModel != null)
                fontValue = ViewModel.Font;
            var radioButton = sender as RadioButton;
            if (radioButton == null) return;
            radioButton.Checked -= FontRadioButton_Checked;
            radioButton.Checked += FontRadioButton_Checked;
            radioButton.Unchecked -= Font_Unchecked;
            radioButton.Unchecked += Font_Unchecked;
            var settingsFont = int.Parse(Regex.Match(radioButton.Name, @"\d+").Value);
            if (settingsFont != fontValue) return;
            radioButton.Style = (Style)Application.Current.Resources["LitResRadioButtonStyle1"];
            radioButton.IsChecked = true;
            _readerPage.ViewModel.ReaderSettings.Font = int.Parse(Regex.Match(radioButton.Name, @"\d+").Value);
            _readerPage.ViewModel.SaveSettings();
            _readerPage.ChangeFont();
        }

        private void ThemeRadioButton_Loaded(object sender, RoutedEventArgs e)
        {
            int theme;
            var settingsTheme = 0;
            if (ViewModel != null)
                settingsTheme = ViewModel.Theme;
            var radiobutton = sender as RadioButton;
            if (radiobutton == null) return;
            radiobutton.Checked -= ThemeRadioButton_Checked;
            radiobutton.Checked += ThemeRadioButton_Checked;
            radiobutton.Unchecked -= ThemeRadioButton_Unchecked;
            radiobutton.Unchecked += ThemeRadioButton_Unchecked;
            var name = radiobutton.Name;
            if (name.Contains("Light"))
                theme = 1;
            else if (name.Contains("Dark"))
                theme = 3;
            else
                theme = 2;
            if (settingsTheme != theme) return;
            radiobutton.Style = (Style)Application.Current.Resources["LitResRadioButtonStyle1"];
            radiobutton.IsChecked = true;
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton?.IsChecked != true) return;
            radioButton.Style = (Style)Application.Current.Resources["LitResRadioButtonStyle1"];
            int theme;
            var name = radioButton.Name;
            if (name.Contains("Light"))
                theme = 1;
            else if (name.Contains("Dark"))
                theme = 3;
            else
                theme = 2;
            ViewModel.Theme = theme;
            if (_readerPage == null) return;
            _readerPage.ViewModel.ReaderSettings.Theme = theme;
            _readerPage.ChangeTheme();
        }

        private void ThemeRadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null) radioButton.Style = null;
        }

        private void SideIndentSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {            
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = sender as Slider;
            if (slider == null) return;
            _readerPage.ViewModel.ReaderSettings.Margin = (int)slider.Value;            
            if (ViewModel != null)
                ViewModel.Margins = (int)slider.Value;
            _readerPage.ChangeMargins();
            _readerPage.ViewModel.SaveSettings();
        }

        private void LineSpacingSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = sender as Slider;
            if (slider == null) return;
            _readerPage.ViewModel.ReaderSettings.CharacterSpacing = (int)slider.Value;
            if (ViewModel != null)
                ViewModel.Interlineage = (int)slider.Value;
            _readerPage.ChangeCharacterSpacing();
            _readerPage.ViewModel.SaveSettings();
        }
    }

    public class SettingsFitting : ViewModelPage<ReaderSettingsViewModel>
	{
	}
}