using System;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class ToUpperConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
        {
		    if (parameter is string)
		    {
		        if(((string)parameter).Equals("FirstCharacter"))
		        {
		            var str = (string) value;
		            if (!string.IsNullOrEmpty(str) && !str.Equals(String.Empty))
		            {
                       return char.ToUpper(str[0]) + str.Substring(1);
                    }
		        }
		    }
		    if (value is string)
		    {
		        return ((string) value).ToUpper();
		    }
			return value.ToString().ToUpper();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
