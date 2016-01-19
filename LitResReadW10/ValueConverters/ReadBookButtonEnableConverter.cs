using System;
using System.Globalization;
using Windows.UI.Xaml.Data;
using LitRes.Models;

namespace LitRes.ValueConverters
{
	public class ReadBookButtonEnableConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
        {
			return value is Models.Book && (((Models.Book)value).IsMyBook || ((Models.Book)value).HasTrial != "0");
		}

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
