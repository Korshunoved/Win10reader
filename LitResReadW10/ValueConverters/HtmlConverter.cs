using System;
using System.Text;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class HtmlConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			//remove from begin and replace br new line + remove html
			string formatLines = Regex.Replace( System.Convert.ToString( value ), @"^<br/>", "" ).Replace( "<br/>", Environment.NewLine ).Replace( "&quot;", "" );
			formatLines = Regex.Replace( formatLines, @"<\s*\/\s*p\s*.*?>", Environment.NewLine );//space after close </p>
			var result = Regex.Replace(formatLines, @"<[^>]*>", string.Empty ).Trim();

		    var s = parameter as string;
		    if (s != null)
		    {
		        if (s.Equals("Uppercase"))
		        {
		            return result.ToUpper();
		        }
		    }
		    return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
