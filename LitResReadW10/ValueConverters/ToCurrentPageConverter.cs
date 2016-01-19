using System;
using Windows.UI.Xaml.Data;
using LitRes.Models;

namespace LitRes.ValueConverters
{
	public class ToCurrentPageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
            if (value is Book)
            {
                var book = (Book)value;

                return System.Convert.ToString( (int)(book.ReadedPercent / 100.0f * book.Pages));
            }
            return "0";
        }

		public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
