using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace LitRes.Controls
{
	public class LitresSettingsSlider : Slider
	{
		private Rectangle _rect1;
		private Rectangle _rect2;
		private Rectangle _rect3;
        private Rectangle _rect5;

        protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_rect1 = GetTemplateChild( "Rect1" ) as Rectangle;
			_rect2 = GetTemplateChild( "Rect2" ) as Rectangle;
			_rect3 = GetTemplateChild( "Rect3" ) as Rectangle;
            _rect5 = GetTemplateChild( "Rect4" ) as Rectangle;

			SetRectangles( (int) Value );
		}

		protected override void OnValueChanged( double oldValue, double newValue )
		{
			base.OnValueChanged( oldValue, newValue );

			SetRectangles( (int) Math.Round( newValue ) );
		}

		private void SetRectangles( int value )
		{
			switch( value )
			{
				case 0:
					if( _rect1 != null )
					{
						_rect1.Visibility = Visibility.Collapsed;
					}
					if( _rect2 != null )
					{
						_rect2.Visibility = Visibility.Collapsed;
					}
					if( _rect3 != null )
					{
						_rect3.Visibility = Visibility.Collapsed;
					}
                    if (_rect5 != null)
                    {
                        _rect5.Visibility = Visibility.Collapsed;
                    }
					break;
				case 1:
					if( _rect1 != null )
					{
						_rect1.Visibility = Visibility.Visible;
					}
					if( _rect2 != null )
					{
						_rect2.Visibility = Visibility.Collapsed;
					}
					if( _rect3 != null )
					{
						_rect3.Visibility = Visibility.Collapsed;
					}
                    if (_rect5 != null)
                    {
                        _rect5.Visibility = Visibility.Collapsed;
                    }
					break;
				case 2:
					if( _rect1 != null )
					{
						_rect1.Visibility = Visibility.Visible;
					}
					if( _rect2 != null )
					{
						_rect2.Visibility = Visibility.Visible;
					}
					if( _rect3 != null )
					{
						_rect3.Visibility = Visibility.Collapsed;
					}
                    if (_rect5 != null)
                    {
                        _rect5.Visibility = Visibility.Collapsed;
                    }
					break;
                case 3:
                    if (_rect1 != null)
                    {
                        _rect1.Visibility = Visibility.Visible;
                    }
                    if (_rect2 != null)
                    {
                        _rect2.Visibility = Visibility.Visible;
                    }
                    if (_rect3 != null)
                    {
                        _rect3.Visibility = Visibility.Visible;
                    }
                    if (_rect5 != null)
                    {
                        _rect5.Visibility = Visibility.Collapsed;
                    }
                    break;
                case 4:
                    if (_rect1 != null)
                    {
                        _rect1.Visibility = Visibility.Visible;
                    }
                    if (_rect2 != null)
                    {
                        _rect2.Visibility = Visibility.Visible;
                    }
                    if (_rect3 != null)
                    {
                        _rect3.Visibility = Visibility.Visible;
                    }
                    if (_rect5 != null)
                    {
                        _rect5.Visibility = Visibility.Visible;
                    }                   
                    break;
               
			}
		}
	}
}
