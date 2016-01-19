using System;
using System.Collections;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
	public class ObjectToVisibilityConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language)
        {
			bool visible = false;
			string type = "(null)";
			string annotation = string.Empty;

			if( value != null )
			{
				Type valueType = value.GetType();

				type = valueType.Name;

				if( valueType.GetTypeInfo().IsValueType )
				{
					if( valueType == typeof( int ) )
						visible = ((int) value) != 0;
					else
						if( valueType == typeof( bool ) )
							visible = (bool) value;
						else
							if( valueType == typeof( float ) )
								visible = ((float) value) != 0;
							else
								if( valueType == typeof( DateTime ) )
									visible = ((DateTime) value).Ticks > 0;

					type += "/" + value.ToString();
				}
				else
				{
					if( valueType == typeof( string ) )
						visible = !string.IsNullOrEmpty( (string) value );
					else
						visible = true;
				}

				if( value is IList )
				{
					visible = ( ( IList ) value ).Count > 0;
				}

			    if (value is Visibility)
			    {
			        visible = ((Visibility)value) == Visibility.Visible;
			    }
            }


		    var s = parameter as string;
		    if ( s != null )
			{
				annotation = s;

				if( annotation == "inverse" )
					visible = !visible;
			}
			
			//Debug.WriteLine( "O2V: {0} ({1}) -> {2}", type, annotation, visible ? "Visible" : "Collapsed" );
			return visible ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
        {
			throw new NotImplementedException();
		}
	}
}
