using System;
using Windows.UI.Xaml;

namespace LitRes.ValueConverters
{
	public class EnumVisibilityConverter : ConverterBase<Enum, Visibility>
	{
		public override object Convert(Enum value, object parameter, string language)
        {
			return value.ToString() == (string) parameter ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
