using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace LitRes.ValueConverters
{
	public class BookOpacityConverter : IValueConverter
	{
		private int _entityId;

		public int EntityId
		{
			get
			{
				return _entityId;
			}
			set
			{
				_entityId = value;
			}
		}

		public object Convert( object value, Type targetType, object parameter, string language)
		{
		    if (parameter != null)
		    {
		        if (parameter is string)
		        {
		            if (parameter.Equals("visibility"))
		            {
		                if ((value is int && (int) value == _entityId)) return Visibility.Visible;
		                return Visibility.Collapsed;
		            }
		            if (parameter.Equals("rating"))
		            {
                        if ((value is int && (int)value == _entityId)) return "/assets/RatingDisabled.png";
                        return "/assets/RatingDisabled.png"; 
		            }
		            if (parameter.Equals("ratingColor"))
		            {
                        if ((value is int && (int)value == _entityId)) return new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));
                        return new SolidColorBrush(Color.FromArgb(255, 255, 77, 21));
		            }
		        }
                else if (parameter is SolidColorBrush)
                {
                    if ((value is int && (int)value == _entityId)) return new SolidColorBrush(Color.FromArgb(255, 182, 182, 182));
                    return parameter;
                }
		    }
		    return ((value is int && (int) value == _entityId))?0.5:1.0;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
		
	}
}
