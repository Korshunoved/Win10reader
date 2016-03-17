﻿using System;
using System.Text.RegularExpressions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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

        public Visibility DesktopVisibility { get; set; }

        #region Constructors/Disposer
        public Settings()
		{
			InitializeComponent();

            MobileVisibility = SystemInfoHelper.IsDesktop() ? Visibility.Collapsed : Visibility.Visible;
            DesktopVisibility = SystemInfoHelper.IsDesktop() ? Visibility.Visible : Visibility.Collapsed;

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

		    _readerPage.FromSettings = true;

		    GetLineSpacingValue();

		    GetMarginValue();

            HyphenationSwither.IsOn = AppSettings.Default.Hyphenation;

		    StatusBarSwitcher.IsOn = AppSettings.Default.HideStatusBar;

		    TwoColumnsSwitcher.IsOn = AppSettings.Default.TwoColumns;

		    AutorotateSwitch.IsOn = AppSettings.Default.Autorotate;

            StatusBarSwitcher.Toggled -= StatusBarSwitcherOnToggled;
            StatusBarSwitcher.Toggled += StatusBarSwitcherOnToggled;

            AnimationSwither.Toggled -= AnimationSwither_Toggled;
            AnimationSwither.Toggled += AnimationSwither_Toggled;

            HyphenationSwither.Toggled -= HyphenationSwitherOnToggled;
            HyphenationSwither.Toggled += HyphenationSwitherOnToggled;

            AutorotateSwitch.Toggled -= AutorotateSwitchOnToggled;
            AutorotateSwitch.Toggled += AutorotateSwitchOnToggled;

            FontSizeSlider.ManipulationCompleted -= FontSizeSliderOnManipulationCompleted;
            FontSizeSlider.ManipulationCompleted += FontSizeSliderOnManipulationCompleted;
		    FontSizeSlider.Minimum = SystemInfoHelper.IsDesktop() ? 16 : 14;
		    FontSizeSlider.Maximum = SystemInfoHelper.IsDesktop() ? 24 : 22;
		    FontSizeSlider.StepFrequency = 2;
            FontSizeSlider.Value = AppSettings.Default.FontSettings.FontSize;
            FontSizeSlider.ValueChanged -= FontSizeSliderOnValueChanged;
            FontSizeSlider.ValueChanged += FontSizeSliderOnValueChanged;

            JustificationSwither.Toggled -= JustificationSwither_Toggled;
            JustificationSwither.Toggled += JustificationSwither_Toggled;

            _marginSliderValueChanging = false;
            _marginSliderValue = MarginsSlider.Value;
            MarginsSlider.Tapped -= MarginsSliderOnTapped;
            MarginsSlider.Tapped += MarginsSliderOnTapped;
            MarginsSlider.ManipulationDelta -= MarginsSliderOnManipulationDelta; 
            MarginsSlider.ManipulationDelta += MarginsSliderOnManipulationDelta;
            MarginsSlider.ManipulationCompleted -= MarginsSliderOnManipulationCompleted;
            MarginsSlider.ManipulationCompleted += MarginsSliderOnManipulationCompleted;
            MarginsSlider.ValueChanged -= MarginsSliderOnValueChanged;
            MarginsSlider.ValueChanged += MarginsSliderOnValueChanged;

            LineSpacingSlider.ValueChanged -= LineSpacingSlider_ValueChanged;
            LineSpacingSlider.ValueChanged += LineSpacingSlider_ValueChanged;            

            GetTheme();		    
		}

	    private int CalculateMargin(int value)
	    {
	        var deviceWidth = Window.Current.CoreWindow.Bounds.Width;
	        switch (value)
	        {
                case 1:
                    return (int)(deviceWidth * 0.03f);
                case 2:
	                return (int)(deviceWidth * 0.06f);
                case 3:
                    return (int)(deviceWidth * 0.09f);
                case 4:
                    return (int)(deviceWidth * 0.12f);
                case 5:
                    return (int)(deviceWidth * 0.15f);
            }
            return 0;
        }

	    private void MarginsSliderOnValueChanged(object sender, RangeBaseValueChangedEventArgs rangeBaseValueChangedEventArgs)
	    {
            if (SystemInfoHelper.IsDesktop()) return;
            var slider = MarginsSlider;
            if (slider == null) return;
            var value = (int)slider.Value;
            var margin = CalculateMargin(value);
            AppSettings.Default.MarginValue = margin;
        }

	    private void FontSizeSliderOnValueChanged(object sender, RangeBaseValueChangedEventArgs rangeBaseValueChangedEventArgs)
	    {
	        if (SystemInfoHelper.IsDesktop()) return;
            var slider = sender as Slider;
            if (slider == null) return;
            var value = (int)slider.Value;
            AppSettings.Default.FontSettings.FontSize = value;
        }

	    private void GetMarginValue()
	    {
	        var value = AppSettings.Default.MarginValue;
            var deviceWidth = Window.Current.CoreWindow.Bounds.Width;
	        var percent = value/deviceWidth;            
            if (Math.Abs(percent - 0.15) <= 0.001)
            {
                MarginsSlider.Value = 5;
                return;
            }
            if (Math.Abs(percent - 0.12) <= 0.001)
            {
                MarginsSlider.Value = 4;
                return;
            }
            if (Math.Abs(percent - 0.09) <= 0.001)
            {
                MarginsSlider.Value = 3;
                return;
            }
            if (Math.Abs(percent - 0.06) <= 0.001)
            {
                MarginsSlider.Value = 2;
                return;
            }
            MarginsSlider.Value = 1;            
        }

	    private void GetLineSpacingValue()
	    {
	        var value = AppSettings.Default.FontSettings.FontInterval;
	        if (value == 0.7f)
	        {
	            LineSpacingSlider.Value = 1;
	            return;
	        }
            if (value == 0.85f)
            {
                LineSpacingSlider.Value = 2;
                return;
            }
            if (value == 1f)
            {
                LineSpacingSlider.Value = 3;
                return;
            }
            if (value == 1.15f)
            {
                LineSpacingSlider.Value = 4;
                return;
            }
            if (value == 1.5f)
            {
                LineSpacingSlider.Value = 5;
            }
        }

	    private void StatusBarSwitcherOnToggled(object sender, RoutedEventArgs routedEventArgs)
	    {
            var toggle = sender as ToggleSwitch;
            if (toggle == null) return;
            AppSettings.Default.HideStatusBar = toggle.IsOn;
        }

	    private void AutorotateSwitchOnToggled(object sender, RoutedEventArgs routedEventArgs)
	    {
            var toggle = sender as ToggleSwitch;
            if (toggle == null) return;
	        AppSettings.Default.Autorotate = toggle.IsOn;
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
	        var margin = CalculateMargin(value);
            AppSettings.Default.MarginValue = margin;
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

        private void LineSpacingSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_readerPage == null)
                _readerPage = Reader.Instance;
            var slider = sender as Slider;
            if (slider == null) return;
            var value = (int)slider.Value;
            switch (value)
            {
                case 1:
                    AppSettings.Default.FontSettings.FontInterval = 0.7f;
                    break;
                case 2:
                    AppSettings.Default.FontSettings.FontInterval = 0.85f;
                    break;
                case 3:
                    AppSettings.Default.FontSettings.FontInterval = 1f;
                    break;
                case 4:
                    AppSettings.Default.FontSettings.FontInterval = 1.15f;
                    break;
                case 5:
                    AppSettings.Default.FontSettings.FontInterval = 1.5f;
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