/*
 * Author: CactusSoft (http://cactussoft.biz/), 2013
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA
 * 02110-1301, USA.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using BookParser.Fonts;
using BookParser.Models;

namespace BookParser
{
    public class AppSettings
    {
        public const int WORDS_PER_PAGE = 200;
        private const bool DEFAULT_LOCK_SCREEN = false;
        private const Orientation DEFAULT_ORIENTATION = Orientation.Vertical;
        private const bool DEFAULT_HIDE_MENU = false;
        private const SupportedMargins DEFAULT_MARGIN = SupportedMargins.Medium;
        private const int DEFAULT_COLOR_SCHEME = 1;
        private const string DEFAULT_TRANSLATE_LANGUAGE = "en";
        private const bool DEFAULT_USE_CSS_FONTSIZE = false;
        private const bool DEFAULT_HYPHENATION = true;
        private const FlippingMode DEFAULT_FLIPPING_MODE = FlippingMode.TouchOrSlide;
        private const FlippingStyle DEFAULT_FLIPPING_STYLE = FlippingStyle.Overlap;
        private readonly string DEFAULT_LANGUAGE;        

        private readonly SettingsStorage _settingsStorage = new SettingsStorage();

        private readonly List<string> _defaultTranslateLanguages = new List<string> {"ru", "en"};

        private static readonly Lazy<AppSettings> LazyInstance = new Lazy<AppSettings>(() => new AppSettings());

        public static AppSettings Default => LazyInstance.Value;

        private AppSettings()
        {
            FontSettings = new FontSettings(_settingsStorage);

            var defaultLang =
                UiLanguages.SingleOrDefault(
                    l => l.TwoLetterISOLanguageName == CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            DEFAULT_LANGUAGE = defaultLang == null ? "en" : defaultLang.TwoLetterISOLanguageName;

        }

        public BookModel CurrentBook { get; set; }

        public int CurrentPage
        {
            get { return _settingsStorage.GetValueWithDefault("CurrentPage", 1); }
            set { _settingsStorage.SetValue("CurrentPage", value); }
        }

        public IEnumerable<ChapterModel> Chapters { get; set; } 

        public bool LockScreen
        {
            get { return _settingsStorage.GetValueWithDefault("LockScreen", DEFAULT_LOCK_SCREEN); }
            set { _settingsStorage.SetValue("LockScreen", value); }
        }

        public Orientation Orientation
        {
            get { return _settingsStorage.GetValueWithDefault("Orientation", DEFAULT_ORIENTATION); }
            set { _settingsStorage.SetValue("Orientation", value); }
        }

        public bool Autorotate
        {
            get { return _settingsStorage.GetValueWithDefault("Autorotate", false); }
            set { _settingsStorage.SetValue("Autorotate", value); }
        }

        public Visibility MobileVisibility { get; set; }

        public FontSettings FontSettings { get; }

        public bool HideMenu
        {
            get { return _settingsStorage.GetValueWithDefault("HideMenu", DEFAULT_HIDE_MENU); }
            set { _settingsStorage.SetValue("HideMenu", value); }
        }

        public Thickness Margin => new Thickness(MarginValue,0, MarginValue,0);

        public int MarginValue
        {
            get { return _settingsStorage.GetValueWithDefault("MarginValue", 25); }
            set { _settingsStorage.SetValue("MarginValue", value); }
        }

        public int ColorSchemeKey
        {
            get { return _settingsStorage.GetValueWithDefault("ColorSchemeKey", DEFAULT_COLOR_SCHEME); }
            set { _settingsStorage.SetValue("ColorSchemeKey", value); }
        }

        public Scheme ColorScheme
        {
            get { return BookThemes.Default[ColorSchemeKey]; }
        }

        public List<Scheme> Schemes => BookThemes.Default.ToList();

        public List<CultureInfo> UiLanguages { get; } = new List<string> {"ru", "en"}
            .Select(l => new CultureInfo(l)).ToList();

        public CultureInfo CurrentUiLanguage
        {
            get { return new CultureInfo(_settingsStorage.GetValueWithDefault("CurrentUILanguage", DEFAULT_LANGUAGE)); }
            set { _settingsStorage.SetValue("CurrentUILanguage", value.TwoLetterISOLanguageName); }
        }

        public bool AreTranslateLanguagesSet
        {
            get
            {
                var langNamesList = _settingsStorage.GetValueWithDefault("TranslateLanguages",
                    _defaultTranslateLanguages);

                return !langNamesList.SequenceEqual(_defaultTranslateLanguages);
            }
        }

        public List<CultureInfo> TranslateLanguages
        {
            get
            {
                var langNamesList = _settingsStorage.GetValueWithDefault("TranslateLanguages",
                    _defaultTranslateLanguages);

                return langNamesList.Select(l => new CultureInfo(l)).ToList();
            }
            set
            {
                _settingsStorage.SetValue("TranslateLanguages", value.Select(ci => ci.TwoLetterISOLanguageName).ToList());
            }
        }

        public CultureInfo CurrentTranslateLanguage
        {
            get
            {
                return
                    new CultureInfo(_settingsStorage.GetValueWithDefault("CurrentTranslateLanguage",
                        DEFAULT_TRANSLATE_LANGUAGE));
            }
            set { _settingsStorage.SetValue("CurrentTranslateLanguage", value.TwoLetterISOLanguageName); }
        }

        public bool UseCssFontSize
        {
            get { return _settingsStorage.GetValueWithDefault("UseCSSFontSize", DEFAULT_USE_CSS_FONTSIZE); }
            set { _settingsStorage.SetValue("UseCSSFontSize", value); }
        }

        public bool Hyphenation
        {
            get { return _settingsStorage.GetValueWithDefault("Hyphenation", DEFAULT_HYPHENATION); }
            set { _settingsStorage.SetValue("Hyphenation", value); }
        }

        public bool HideStatusBar
        {
            get { return _settingsStorage.GetValueWithDefault("HideStatusBar", false); }
            set { _settingsStorage.SetValue("HideStatusBar", value); }
        }

        public bool TwoColumns
        {
            get { return _settingsStorage.GetValueWithDefault("TwoColumns", false); }
            set { _settingsStorage.SetValue("TwoColumns", value); }
        }

        public FlippingMode FlippingMode
        {
            get { return _settingsStorage.GetValueWithDefault("FlippingMode", DEFAULT_FLIPPING_MODE); }
            set { _settingsStorage.SetValue("FlippingMode", value); }
        }

        public FlippingStyle FlippingStyle
        {
            get { return _settingsStorage.GetValueWithDefault("FlippingStyle", DEFAULT_FLIPPING_STYLE); }
            set { _settingsStorage.SetValue("FlippingStyle", value); }
        }

        public bool IsFirstTimeRunning
        {
            get { return _settingsStorage.GetValueWithDefault("IsFirstTimeRunning", true); }
            set { _settingsStorage.SetValue("IsFirstTimeRunning", value); }
        }

        public int Brightness
        {
            get { return _settingsStorage.GetValueWithDefault("Brightness", 100); }
            set { _settingsStorage.SetValue("Brightness", value); }
        }
    }

    public enum SupportedMargins
    {
        None,
        Small,
        Medium,
        Big
    }

    public enum ColorSchemes
    {
        Light,
        Sepia,
        Dark
    }

    public enum FlippingStyle
    {
        None,
        Shift,
        Overlap
    }

    [Flags]
    public enum FlippingMode
    {
        Touch = 1,
        Slide = 2,
        TouchOrSlide = Touch | Slide
    }


    public class FontSettings
    {
        private const int DefaultFontSize = 20;
        private const float DefaultFontInterval = 1f;
        public const string DefaultFontFamily = "/Fonts/PT Sans.ttf#PT Sans";

        public IFontHelper FontHelper { get; set; }

        private readonly SettingsStorage _settingsStorage;

        public FontSettings(SettingsStorage settingsStorage)
        {
            _settingsStorage = settingsStorage;
        }

        public int FontSize
        {
            get { return _settingsStorage.GetValueWithDefault("FontSize", DefaultFontSize); }
            set { _settingsStorage.SetValue("FontSize", value); }
        }

        public float FontInterval
        {
            get
            {
                var val = _settingsStorage.GetValueWithDefault("FontInterval", DefaultFontInterval);
                return val;
            }
            set { _settingsStorage.SetValue("FontInterval", value); }
        }

        public FontFamily FontFamily
        {
            get { return new FontFamily(_settingsStorage.GetValueWithDefault("FontFamily", DefaultFontFamily)); }
            set { _settingsStorage.SetValue("FontFamily", value.Source); }
        }
    }

    public class Scheme
    {
        public Scheme(
            ColorSchemes colorScheme,
            Color backgroundBrush,
            Color titleForegroundBrush,
            Color textForegroundBrush,
            Color linkForegroundBrush,
            Color selectionBrush,
            Color applicationBarBackgroundBrush,
            Color progressBarBrush,
            Color systemTrayForegroundColor)
        {
            ColorScheme = colorScheme;
            BackgroundBrush = new SolidColorBrush(backgroundBrush);
            TitleForegroundBrush = new SolidColorBrush(titleForegroundBrush);
            TextForegroundBrush = new SolidColorBrush(textForegroundBrush);
            LinkForegroundBrush = new SolidColorBrush(linkForegroundBrush);
            SelectionBrush = new SolidColorBrush(selectionBrush);
            ApplicationBarBackgroundBrush = new SolidColorBrush(applicationBarBackgroundBrush);
            ProgressBarBrush = new SolidColorBrush(progressBarBrush);
            SystemTrayForegroundColor = systemTrayForegroundColor;
        }

        public ColorSchemes ColorScheme { get; set; }

        public SolidColorBrush BackgroundBrush { get; set; }

        public SolidColorBrush TitleForegroundBrush { get; set; }

        public SolidColorBrush TextForegroundBrush { get; set; }

        public SolidColorBrush LinkForegroundBrush { get; set; }

        public SolidColorBrush ApplicationBarBackgroundBrush { get; set; }

        public SolidColorBrush ProgressBarBrush { get; set; }

        public SolidColorBrush SelectionBrush { get; set; }

        public Color SystemTrayForegroundColor { get; set; }
    }

    public class BookThemes : IEnumerable<Scheme>
    {
        private static readonly Lazy<BookThemes> LazyInstance = new Lazy<BookThemes>(() => new BookThemes());

        private readonly Dictionary<ColorSchemes, Scheme> _schemes = new Dictionary<ColorSchemes, Scheme>(8);

        private BookThemes()
        {
            InitSchemes();
        }

        public static BookThemes Default => LazyInstance.Value;

        public Scheme this[int scheme]
        {
            get
            {
                switch (scheme)
                {
                    case 1:
                        return _schemes[ColorSchemes.Light];
                    case 2:
                        return _schemes[ColorSchemes.Sepia];
                    case 3:
                        return _schemes[ColorSchemes.Dark];
                    default:
                        throw new Exception("Not supported index");
                }
            }
        }

        public IEnumerator<Scheme> GetEnumerator()
        {
            return _schemes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void InitSchemes()
        {
            _schemes.Add(
                ColorSchemes.Light,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Light,
                    backgroundBrush: Colors.White,
                    titleForegroundBrush: Color.FromArgb(0xFF, 0x7D, 0x7D, 0x7D),
                    textForegroundBrush: Colors.Black,
                    linkForegroundBrush: Color.FromArgb(0xFF, 0x2D, 0x8E, 0xCC),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x34, 0x2E, 0x2B),
                    progressBarBrush: Color.FromArgb(0xFF, 0xF0, 0x96, 0x09),
                    systemTrayForegroundColor: Colors.Black
                    ));

            _schemes.Add(
                ColorSchemes.Dark,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Dark,
                    backgroundBrush: Color.FromArgb(0xFF, 0x3b, 0x39, 0x3f), //background-color: #3b393f;
                    titleForegroundBrush: Color.FromArgb(0xFF, 0xA8, 0xA8, 0xA8),
                    textForegroundBrush: Colors.White,
                    linkForegroundBrush: Color.FromArgb(0xFF, 0xF0, 0x96, 0x09),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x2C, 0x2C, 0x2C),
                    progressBarBrush: Color.FromArgb(0xFF, 0x71, 0x71, 0x71),
                    systemTrayForegroundColor: Colors.Black
                    ));

            _schemes.Add(
                ColorSchemes.Sepia,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Sepia,
                    backgroundBrush: Color.FromArgb(0xFF, 0xEB, 0xEA, 0xE4),
                    titleForegroundBrush: Color.FromArgb(0xFF, 0x77, 0x70, 0x52),
                    textForegroundBrush: Colors.Black,
                    linkForegroundBrush: Color.FromArgb(0xFF, 0xD7, 0x83, 0x00),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x34, 0x2E, 0x2B),
                    progressBarBrush: Color.FromArgb(0xFF, 0xF0, 0x96, 0x09),
                    systemTrayForegroundColor: Colors.Black
                    ));
        }
    }

    public class SettingsStorage
    {
        private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

        public T GetValue<T>(string key)
        {
            object value;
            bool exists = _settings.Values.TryGetValue(key, out value);

            if (!exists)
                throw new KeyNotFoundException();
            return (T)value;
        }

        public T GetValueWithDefault<T>(string key, T defaultValue)
        {
            object value;
            bool exists = _settings.Values.TryGetValue(key, out value);

            if (exists)
            {
                try
                {                   
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
            }
            else
            {
                return defaultValue;
            }
        }

        public void SetValue<T>(string key, T newValue)
        {
            _settings.Values[key] = newValue;
        }
    }
}