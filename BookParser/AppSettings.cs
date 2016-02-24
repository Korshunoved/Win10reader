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
using BookParser.Models;

namespace BookParser
{
    public class AppSettings
    {
        public const int WORDS_PER_PAGE = 200;
        private const bool DEFAULT_LOCK_SCREEN = false;
        private const Orientation DEFAULT_ORIENTATION = Windows.UI.Xaml.Controls.Orientation.Vertical;
        private const bool DEFAULT_HIDE_MENU = false;
        private const SupportedMargins DEFAULT_MARGIN = SupportedMargins.Medium;
        private const ColorSchemes DEFAULT_COLOR_SCHEME = ColorSchemes.Day;
        private const string DEFAULT_TRANSLATE_LANGUAGE = "en";
        private const bool DEFAULT_USE_CSS_FONTSIZE = false;
        private const bool DEFAULT_HYPHENATION = true;
        private const FlippingMode DEFAULT_FLIPPING_MODE = FlippingMode.TouchOrSlide;
        private const FlippingStyle DEFAULT_FLIPPING_STYLE = FlippingStyle.Overlap;
        private readonly string DEFAULT_LANGUAGE;

        private readonly SettingsStorage _settingsStorage = new SettingsStorage();
        private readonly FontSettings _fontSettings;

        private static readonly Dictionary<SupportedMargins, Thickness> Margins = new Dictionary
            <SupportedMargins, Thickness>
        {
            {SupportedMargins.None, new Thickness(2)},
            {SupportedMargins.Small, new Thickness(8)},
            {SupportedMargins.Medium, new Thickness(12)},
            {SupportedMargins.Big, new Thickness(18)}
        };

        private readonly List<CultureInfo> _uiLanguages = new List<string> {"ru", "en"}
            .Select(l => new CultureInfo(l)).ToList();

        private readonly List<string> _defaultTranslateLanguages = new List<string> {"ru", "en"};

        private static readonly Lazy<AppSettings> LazyInstance = new Lazy<AppSettings>(() => new AppSettings());

        public static AppSettings Default
        {
            get { return LazyInstance.Value; }
        }

        private AppSettings()
        {
            _fontSettings = new FontSettings(_settingsStorage);

            var defaultLang =
                UILanguages.SingleOrDefault(
                    l => l.TwoLetterISOLanguageName == CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            DEFAULT_LANGUAGE = defaultLang == null ? "en" : defaultLang.TwoLetterISOLanguageName;

        }

        public BookModel CurrentBook { get; set; }

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

        public FontSettings FontSettings
        {
            get { return _fontSettings; }
        }

        public bool HideMenu
        {
            get { return _settingsStorage.GetValueWithDefault("HideMenu", DEFAULT_HIDE_MENU); }
            set { _settingsStorage.SetValue("HideMenu", value); }
        }

        public SupportedMargins MarginKey
        {
            get { return _settingsStorage.GetValueWithDefault("MarginKey", DEFAULT_MARGIN); }
            set { _settingsStorage.SetValue("MarginKey", value); }
        }

        public Thickness Margin
        {
            get { return Margins[MarginKey]; }
        }

        public ColorSchemes ColorSchemeKey
        {
            get { return _settingsStorage.GetValueWithDefault("ColorSchemeKey", DEFAULT_COLOR_SCHEME); }
            set { _settingsStorage.SetValue("ColorSchemeKey", value); }
        }

        public Scheme ColorScheme
        {
            get { return BookThemes.Default[ColorSchemeKey]; }
        }

        public List<Scheme> Schemes
        {
            get { return BookThemes.Default.ToList(); }
        }

        public List<CultureInfo> UILanguages
        {
            get { return _uiLanguages; }
        }

        public CultureInfo CurrentUILanguage
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

        public bool UseCSSFontSize
        {
            get { return _settingsStorage.GetValueWithDefault("UseCSSFontSize", DEFAULT_USE_CSS_FONTSIZE); }
            set { _settingsStorage.SetValue("UseCSSFontSize", value); }
        }

        public bool Hyphenation
        {
            get { return _settingsStorage.GetValueWithDefault("Hyphenation", DEFAULT_HYPHENATION); }
            set { _settingsStorage.SetValue("Hyphenation", value); }
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

        public bool DontShowUploadToSkyDriveMessage
        {
            get { return _settingsStorage.GetValueWithDefault("DontShowUploadToSkyDriveMessage", false); }
            set { _settingsStorage.SetValue("DontShowUploadToSkyDriveMessage", value); }
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
        Day,
        Night,
        GrayOne,
        GrayTwo,
        Sepia,
        Coffee,
        Sky,
        Asphalt
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
        private const decimal DEFAULT_FONT_SIZE = 24;
        private const decimal DEFAULT_FONT_INTERVAL = 1.0M;
        public const string DEFAULT_FONT_FAMILY = "Segoe WP";

        private readonly SettingsStorage _settingsStorage;

        private readonly List<FontFamily> _fonts = new List<string>
        {
            "Arial Black",
            "Arial Bold",
            "Arial Italic",
            "Arial",
            "Calibri Bold",
            "Calibri Italic",
            "Calibri",
            "Comic Sans MS Bold",
            "Comic Sans MS",
            "Courier New Bold",
            "Courier New Italic",
            "Courier New",
            "Georgia Bold",
            "Georgia Italic",
            "Georgia",
            "Lucida Sans Unicode",
            "Malgun Gothic",
            "Meiryo UI",
            "Microsoft YaHei",
            "Segoe UI Bold",
            "Segoe UI",
            "Segoe WP Black",
            "Segoe WP Bold",
            "Segoe WP Light",
            "Segoe WP Semibold",
            "Segoe WP SemiLight",
            "Segoe WP",
            "Tahoma Bold",
            "Tahoma",
            "Times New Roman Bold",
            "Times New Roman Italic",
            "Times New Roman",
            "Trebuchet MS Bold",
            "Trebuchet MS Italic",
            "Trebuchet MS",
            "Verdana Bold",
            "Verdana Italic",
            "Verdana",
        }.Select(f => new FontFamily(f)).ToList();

        private readonly List<decimal> _sizes = new List<decimal>
        {
            10,
            11,
            12,
            14,
            16,
            18,
            20,
            22,
            24,
            26,
            28,
            32,
            36,
            40,
            42
        };

        private readonly List<decimal> _intervals = Enumerable.Range(8, 13).Select(n => (decimal) n/10).ToList();

        public FontSettings(SettingsStorage settingsStorage)
        {
            _settingsStorage = settingsStorage;
        }

        public List<FontFamily> Fonts
        {
            get { return _fonts; }
        }

        public List<decimal> Sizes
        {
            get { return _sizes; }
        }

        public List<decimal> Intervals
        {
            get { return _intervals; }
        }

        public decimal FontSize
        {
            get { return _settingsStorage.GetValueWithDefault("FontSize", DEFAULT_FONT_SIZE); }
            set { _settingsStorage.SetValue("FontSize", value); }
        }

        public decimal FontInterval
        {
            get { return _settingsStorage.GetValueWithDefault("FontInterval", DEFAULT_FONT_INTERVAL); }
            set { _settingsStorage.SetValue("FontInterval", value); }
        }

        public FontFamily FontFamily
        {
            get { return new FontFamily( DEFAULT_FONT_FAMILY); }
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

        public static BookThemes Default
        {
            get { return LazyInstance.Value; }
        }

        public Scheme this[ColorSchemes scheme]
        {
            get
            {
                if (!_schemes.ContainsKey(scheme))
                    throw new NotSupportedException();

                return _schemes[scheme];
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
                ColorSchemes.Day,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Day,
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
                ColorSchemes.Night,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Night,
                    backgroundBrush: Colors.Black,
                    titleForegroundBrush: Color.FromArgb(0xFF, 0xA8, 0xA8, 0xA8),
                    textForegroundBrush: Colors.White,
                    linkForegroundBrush: Color.FromArgb(0xFF, 0xF0, 0x96, 0x09),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x2C, 0x2C, 0x2C),
                    progressBarBrush: Color.FromArgb(0xFF, 0x71, 0x71, 0x71),
                    systemTrayForegroundColor: Colors.Black
                    ));

            _schemes.Add(
                ColorSchemes.GrayOne,
                new Scheme
                    (
                    colorScheme: ColorSchemes.GrayOne,
                    backgroundBrush: Color.FromArgb(0xFF, 0xCF, 0xCF, 0xCF),
                    titleForegroundBrush: Color.FromArgb(0xFF, 0x60, 0x60, 0x60),
                    textForegroundBrush: Colors.Black,
                    linkForegroundBrush: Color.FromArgb(0xFF, 0x16, 0x78, 0xCA),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x3E, 0x3E, 0x3E),
                    progressBarBrush: Color.FromArgb(0xFF, 0x3C, 0x3C, 0x3C),
                    systemTrayForegroundColor: Colors.Black
                    ));

            _schemes.Add(
                ColorSchemes.GrayTwo,
                new Scheme
                    (
                    colorScheme: ColorSchemes.GrayTwo,
                    backgroundBrush: Color.FromArgb(0xFF, 0x32, 0x32, 0x32),
                    titleForegroundBrush: Color.FromArgb(0xFF, 0xE3, 0xE3, 0xE3),
                    textForegroundBrush: Color.FromArgb(0xFF, 0xB4, 0xB4, 0xB4),
                    linkForegroundBrush: Color.FromArgb(0xFF, 0xF0, 0x96, 0x09),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x18, 0x18, 0x18),
                    progressBarBrush: Color.FromArgb(0xFF, 0x71, 0x71, 0x71),
                    systemTrayForegroundColor: Color.FromArgb(0xFF, 0xB4, 0xB4, 0xB4)
                    ));

            _schemes.Add(
                ColorSchemes.Sepia,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Sepia,
                    backgroundBrush: Color.FromArgb(0xFF, 0xF3, 0xF1, 0xCF),
                    titleForegroundBrush: Color.FromArgb(0xFF, 0x77, 0x70, 0x52),
                    textForegroundBrush: Colors.Black,
                    linkForegroundBrush: Color.FromArgb(0xFF, 0xD7, 0x83, 0x00),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x34, 0x2E, 0x2B),
                    progressBarBrush: Color.FromArgb(0xFF, 0xF0, 0x96, 0x09),
                    systemTrayForegroundColor: Colors.Black
                    ));

            _schemes.Add(
                ColorSchemes.Coffee,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Coffee,
                    backgroundBrush: Color.FromArgb(0xFF, 0x36, 0x34, 0x2B),
                    titleForegroundBrush: Color.FromArgb(0xFF, 0xF3, 0xD3, 0xA0),
                    textForegroundBrush: Color.FromArgb(0xFF, 0xE6, 0xE4, 0xC8),
                    linkForegroundBrush: Color.FromArgb(0xFF, 0xD7, 0x83, 0x00),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x23, 0x21, 0x19),
                    progressBarBrush: Color.FromArgb(0xFF, 0xF0, 0x96, 0x09),
                    systemTrayForegroundColor: Color.FromArgb(0xFF, 0xE6, 0xE4, 0xC8)
                    ));

            _schemes.Add(
                ColorSchemes.Sky,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Sky,
                    backgroundBrush: Color.FromArgb(0xFF, 0xCF, 0xE2, 0xE6),
                    titleForegroundBrush: Color.FromArgb(0xFF, 0x54, 0x6D, 0x81),
                    textForegroundBrush: Color.FromArgb(0xFF, 0x28, 0x2D, 0x2E),
                    linkForegroundBrush: Color.FromArgb(0xFF, 0x1C, 0x6D, 0xB9),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x25, 0x34, 0x41),
                    progressBarBrush: Color.FromArgb(0xFF, 0x90, 0xB0, 0xB7),
                    systemTrayForegroundColor: Colors.Black
                    ));

            _schemes.Add(
                ColorSchemes.Asphalt,
                new Scheme
                    (
                    colorScheme: ColorSchemes.Asphalt,
                    backgroundBrush: Color.FromArgb(0xFF, 0x6D, 0x75, 0x80),
                    titleForegroundBrush: Color.FromArgb(0xFF, 0xD1, 0xED, 0xFF),
                    textForegroundBrush: Color.FromArgb(0xFF, 0xE7, 0xE8, 0xE9),
                    linkForegroundBrush: Color.FromArgb(0xFF, 0x75, 0xCD, 0xDD),
                    selectionBrush: Color.FromArgb(0x26, 0x00, 0x00, 0x00),
                    applicationBarBackgroundBrush: Color.FromArgb(0xF2, 0x2F, 0x35, 0x3E),
                    progressBarBrush: Color.FromArgb(0xFF, 0x69, 0x7B, 0x95),
                    systemTrayForegroundColor: Colors.White
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