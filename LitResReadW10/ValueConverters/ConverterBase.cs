using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public abstract class ConverterBase<TFrom, TTo> : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if( value is TFrom && targetType == typeof( TTo ) )
				return Convert( (TFrom) value, parameter, language );

			return value;
		}

		public abstract object Convert( TFrom value, object parameter, string language);
		
		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			if( value is TTo && targetType == typeof( TFrom ) )
				return ConvertBack( (TTo) value, parameter, language );

			throw new NotImplementedException();
		}

		public virtual object ConvertBack( TTo value, object parameter, string language)
		{
			throw new NotImplementedException();
		}

		
	}
}
