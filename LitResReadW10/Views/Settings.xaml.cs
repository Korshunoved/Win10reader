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

	    private bool _marginSliderValueChanging;
	    private double  _marginSliderValue;

	    public Visibility MobileVisibility { get; set; }

        #region Constructors/Disposer
        public Settings()
		{
			InitializeComponent();

            MobileVisibility = SystemInfoHelper.IsDesktop() ? Visibility.Collapsed : Visibility.Visible;

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

            if (_readerPage == null)
                _readerPage = Reader.Instance;

		    LineSpacingSlider.Value = AppSettings.Default.FontSettings.FontInterval == 1.0f ? 1 : 2;

            HyphenationSwither.IsOn = AppSettings.Default.Hyphenation;

            LightSlider.ValueChanged -= LightSlider_ValueChanged;
            LightSlider.ValueChanged += LightSlider_ValueChanged;

            AnimationSwither.Toggled -= AnimationSwither_Toggled;
            AnimationSwither.Toggled += AnimationSwither_Toggled;

            HyphenationSwither.Toggled -= HyphenationSwitherOnToggled;
            HyphenationSwither.Toggled += HyphenationSwitherOnToggled;

            FontSizeSlider.ManipulationCompleted -= FontSizeSliderOnManipulationCompleted;
            FontSizeSlider.ManipulationCompleted += FontSizeSliderOnManipulationCompleted;
		    FontSizeSlider.Minimum = SystemInfoHelper.IsDesktop() ? 16 : 14;
		    FontSizeSlider.Maximum = SystemInfoHelper.IsDesktop() ? 24 : 22;
		    FontSizeSlider.StepFrequency = 2;
            FontSizeSlider.Value = AppSettings.Default.FontSettings.FontSize;

            JustificationSwither.Toggled -= JustificationSwither_Toggled;
            JustificationSwither.Toggled += JustificationSwither_Toggled;

            _marginSliderValueChanging = false;
            _marginSliderValue = MarginsSlider.Value = AppSettings.Default.MarginIndex;
            MarginsSlider.Tapped -= MarginsSliderOnTapped;
            MarginsSlider.Tapped += MarginsSliderOnTapped;
            MarginsSlider.ManipulationDelta -= MarginsSliderOnManipulationDelta; 
            MarginsSlider.ManipulationDelta += MarginsSliderOnManipulationDelta;
            MarginsSlider.ManipulationCompleted -= MarginsSliderOnManipulationCompleted;
            MarginsSlider.ManipulationCompleted += MarginsSliderOnManipulationCompleted;

		    LineSpacingSlider.ValueChanged -= LineSpacingSlider_ValueChanged;
            LineSpacingSlider.ValueChanged += LineSpacingSlider_ValueChanged;

            GetTheme();		    
		}

	    private void GetTheme()
	    {
	        var theme = AppSettings.Default.ColorSchemeKey;
            LightImage.Tapped += ThemeImageOnTapped;
            SepiaImage.Tapped += ThemeImageOnTapped;
            DarkImage.Tapped += ThemeImageOnTapped;
            switch (theme)
	        {
                case 1:
	                SepiaEllipse.Visibility = Visibility.Collapsed;
                    DarkEllipse.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    LightEllipse.Visibility = Visibility.Collapsed;
                    DarkEllipse.Visibility = Visibility.Collapsed;
                    break;
                case 3:
                    SepiaEllipse.Visibility = Visibility.Collapsed;
                    LightEllipse.Visibility = Visibility.Collapsed;
                    break;
            }
	    }

	    private void ThemeImageOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
	    {
	        var image = sender as Image;
	        var themeIndex = 0;
	        if (image == null) return;
	        switch (image.Name)
	        {
                case "LightImage":
                    LightEllipse.Visibility = Visibility.Visible;
                    SepiaEllipse.Visibility = Visibility.Collapsed;
                    DarkEllipse.Visibility = Visibility.Collapsed;
                    themeIndex = 1;
                    break;
                case "SepiaImage":
                    LightEllipse.Visibility = Visibility.Collapsed;
                    SepiaEllipse.Visibility = Visibility.Visible;
                    DarkEllipse.Visibility = Visibility.Collapsed;
                    themeIndex = 2;
                    break;
                case "DarkImage":
                    SepiaEllipse.Visibility = Visibility.Collapsed;
                    LightEllipse.Visibility = Visibility.Collapsed;
                    DarkEllipse.Visibility = Visibility.Visible;
                    themeIndex = 3;
                    break;
            }
	        AppSettings.Default.ColorSchemeKey = themeIndex;
            if (SystemInfoHelper.IsDesktop())
            {
                _readerPage?.UpdateSettings();
            }
	    }

	    private void MarginsSliderOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
	    {
	        if (_marginSliderValue == MarginsSlider.Value) return;
	        OnMarginSliderValueChanged();
	    }

	    private void MarginsSliderOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs manipulationDeltaRoutedEventArgs)
	    {
            _marginSliderValueChanging = true;
	    }

	    private void MarginsSliderOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
	    {
            _marginSliderValueChanging = false;
            OnMarginSliderValueChanged();
        }

	    private void OnMarginSliderValueChanged()
	    {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = MarginsSlider;
            if (slider == null) return;
            var value = (int)slider.Value;
            AppSettings.Default.MarginIndex = value;
            if (SystemInfoHelper.IsDesktop() && !_marginSliderValueChanging)
                _readerPage.UpdateSettings();
        }

	    private void FontSizeSliderOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
	    {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = sender as Slider;
            if (slider == null) return;

            var value = (int)slider.Value;
	        AppSettings.Default.FontSettings.FontSize = value;
            if (SystemInfoHelper.IsDesktop())
                _readerPage.UpdateSettings();
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
                _readerPage.UpdateSettings();
        }
    }

    public class SettingsFitting : ViewModelPage<ReaderSettingsViewModel>
	{
	}
}