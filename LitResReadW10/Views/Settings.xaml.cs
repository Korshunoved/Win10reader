using System;
using System.Text.RegularExpressions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using BookParser;
using Digillect.Mvvm;
using Digillect.Mvvm.UI;
using LitRes.ViewModels;
using LitResReadW10.Helpers;

// ReSharper disable CheckNamespace
namespace LitRes.Views
    // ReSharper restore CheckNamespace
{
	[View( "Settings" )]
	public partial class Settings
	{

	    private Reader _readerPage;

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
            Analytics.Instance.sendMessage(Analytics.ViewSettingsReader);

		    var deviceFamily = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
		    if (deviceFamily.Contains("Desktop"))
		    {
		        AutorotateTextBlock.Visibility = Visibility.Collapsed;
                AutorotateSwither.Visibility = Visibility.Collapsed;		        
		    }
            if (_readerPage == null)
                _readerPage = Reader.Instance;

		    SideIndentSlider.Value = AppSettings.Default.MarginIndex;

		    LineSpacingSlider.Value = AppSettings.Default.FontSettings.FontInterval == 1.0f ? 1 : 2;

            HyphenationSwither.IsOn = AppSettings.Default.Hyphenation;

		    SetFontSliderValue();

            LightSlider.ValueChanged -= LightSlider_ValueChanged;
            LightSlider.ValueChanged += LightSlider_ValueChanged;

            AnimationSwither.Toggled -= AnimationSwither_Toggled;
            AnimationSwither.Toggled += AnimationSwither_Toggled;

            HyphenationSwither.Toggled -= HyphenationSwitherOnToggled;
            HyphenationSwither.Toggled += HyphenationSwitherOnToggled;

            FontSizeSlider.ManipulationCompleted -= FontSizeSliderOnManipulationCompleted;
            FontSizeSlider.ManipulationCompleted += FontSizeSliderOnManipulationCompleted;                        

            JustificationSwither.Toggled -= JustificationSwither_Toggled;
            JustificationSwither.Toggled += JustificationSwither_Toggled;

		    SideIndentSlider.ValueChanged -= SideIndentSlider_ValueChanged;
            SideIndentSlider.ValueChanged += SideIndentSlider_ValueChanged;

		    LineSpacingSlider.ValueChanged -= LineSpacingSlider_ValueChanged;
            LineSpacingSlider.ValueChanged += LineSpacingSlider_ValueChanged;
        }

	    private void FontSizeSliderOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
	    {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = sender as Slider;
            if (slider == null) return;

            var value = (int)slider.Value;
            switch (value)
            {
                case 1:
                    AppSettings.Default.FontSettings.FontSize = SystemInfoHelper.IsDesktop() ? 16 : 14;
                    break;
                case 2:
                    AppSettings.Default.FontSettings.FontSize = SystemInfoHelper.IsDesktop() ? 18 : 16;
                    break;
                case 3:
                    AppSettings.Default.FontSettings.FontSize = SystemInfoHelper.IsDesktop() ? 20 : 18;
                    break;
                case 4:
                    AppSettings.Default.FontSettings.FontSize = SystemInfoHelper.IsDesktop() ? 22 : 20;
                    break;
                case 5:
                    AppSettings.Default.FontSettings.FontSize = SystemInfoHelper.IsDesktop() ? 24 : 22;
                    break;
            }
            if (SystemInfoHelper.IsDesktop())
                _readerPage.ChangeFontSize();
        }

	    private void SetFontSliderValue()
	    {
	        var value = AppSettings.Default.FontSettings.FontSize;
            switch (value)
            {
                case 14:
                    FontSizeSlider.Value = 1;
                    break;
                case 16:
                    FontSizeSlider.Value = SystemInfoHelper.IsDesktop() ? 1 : 2;
                    break;
                case 18:
                    FontSizeSlider.Value = SystemInfoHelper.IsDesktop() ? 2 : 3;
                    break;
                case 20:
                    FontSizeSlider.Value = SystemInfoHelper.IsDesktop() ? 3 : 4;
                    break;
                case 22:
                    FontSizeSlider.Value = SystemInfoHelper.IsDesktop() ? 4 : 5;
                    break;
                case 24:
                    FontSizeSlider.Value = 5;
                    break;
            }
        }

	    private void HyphenationSwitherOnToggled(object sender, RoutedEventArgs routedEventArgs)
	    {
            var toggle = sender as ToggleSwitch;
            if (toggle == null) return;
	        AppSettings.Default.Hyphenation = toggle.IsOn;
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            if (SystemInfoHelper.IsDesktop())
                _readerPage.GoToChapter();
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
        }

        private void AnimationSwither_Toggled(object sender, RoutedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var toggle = sender as ToggleSwitch;
            if (toggle == null) return;
            if (ViewModel != null)
                ViewModel.AnimationMoveToPage = toggle.IsOn;
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
            try
            {
                var font = AppSettings.Default.FontSettings.FontFamily;
                if (font.Source.Contains("PT Sans"))
                {
                    fontValue = 1;
                }
                else if (font.Source.Contains("PT Serif"))
                {
                    fontValue = 2;
                }
                else if (font.Source.Contains("PT Mono"))
                {
                    fontValue = 3;
                }
            }
            catch (Exception)
            {
                //
            }
            var radioButton = sender as RadioButton;
            if (radioButton == null) return;
            radioButton.Click -= RadioButtonOnClick;
            radioButton.Click += RadioButtonOnClick;
            radioButton.Unchecked -= Font_Unchecked;
            radioButton.Unchecked += Font_Unchecked;
            var settingsFont = int.Parse(Regex.Match(radioButton.Name, @"\d+").Value);
            if (settingsFont != fontValue) return;
            radioButton.Style = (Style)Application.Current.Resources["LitResRadioButtonStyle1"];
            radioButton.IsChecked = true;         
        }

	    private void RadioButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
	    {
            var radioButton = sender as RadioButton;
            if (radioButton?.IsChecked == true)
            {
                radioButton.Style = (Style)Application.Current.Resources["LitResRadioButtonStyle1"];
                var fontNum = int.Parse(Regex.Match(radioButton.Name, @"\d+").Value);
                switch (fontNum)
                {
                    case 0:
                    case 1:
                        AppSettings.Default.FontSettings.FontFamily = new FontFamily("/Fonts/PT Sans.ttf#PT Sans");
                        if (SystemInfoHelper.IsDesktop())
                            _readerPage.ChangeFont();
                        break;
                    case 2:
                        AppSettings.Default.FontSettings.FontFamily = new FontFamily("PT Serif");
                        if (SystemInfoHelper.IsDesktop())
                            _readerPage.ChangeFont();
                        break;
                    case 3:
                        AppSettings.Default.FontSettings.FontFamily = new FontFamily("/Fonts/PT Mono.ttf#PT Mono");
                        if (SystemInfoHelper.IsDesktop())
                            _readerPage.ChangeFont();
                        break;
                }
            }
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
            if (SystemInfoHelper.IsDesktop())
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
            var value = (int)slider.Value;
            AppSettings.Default.MarginIndex = value;
            var margin = AppSettings.Default.MarginIndex;
            switch (margin)
            {
                case 1:
                    AppSettings.Default.Margin = new Thickness(200,0,200,0);
                    break;
                case 2:
                    AppSettings.Default.Margin = new Thickness(100,0,100,0);
                    break;
                case 3:
                    AppSettings.Default.Margin = new Thickness(50,0,50,0);
                    break;
            }
            if (SystemInfoHelper.IsDesktop())
                _readerPage.ChangeMargins();
        }

        private void LineSpacingSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = sender as Slider;
            if (slider == null) return;
            var value = (int)slider.Value;
            switch (value)
            {
                case 1:
                    AppSettings.Default.FontSettings.FontInterval = 1f;
                    break;
                case 2:
                    AppSettings.Default.FontSettings.FontInterval = 0.7f;
                    break;
            }
            if (SystemInfoHelper.IsDesktop())
                _readerPage.ChangeCharacterSpacing();
        }
    }

    public class SettingsFitting : ViewModelPage<ReaderSettingsViewModel>
	{
	}
}