﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace LitRes.Controls
{
	public class HDLitresSettingsSlider : Slider
	{
		private Rectangle _rect1;
		private Rectangle _rect2;
		private Rectangle _rect3;
        private Rectangle _rect4;
        private Rectangle _rect7;

        protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_rect1 = GetTemplateChild( "Rect1" ) as Rectangle;
			_rect2 = GetTemplateChild( "Rect2" ) as Rectangle;
			_rect3 = GetTemplateChild( "Rect3" ) as Rectangle;
            _rect4 = GetTemplateChild( "Rect4" ) as Rectangle;
            _rect7 = GetTemplateChild( "Rect5" ) as Rectangle;

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
				case -2:
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
                    if (_rect4 != null)
                    {
                        _rect4.Visibility = Visibility.Collapsed;
                    }
                    if (_rect7 != null)
                    {
                        _rect7.Visibility = Visibility.Collapsed;
                    }
					break;
				case -1:
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
                    if (_rect4 != null)
                    {
                        _rect4.Visibility = Visibility.Collapsed;
                    }
                    if (_rect7 != null)
                    {
                        _rect7.Visibility = Visibility.Collapsed;
                    }
					break;
				case 0:
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
                    if (_rect4 != null)
                    {
                        _rect4.Visibility = Visibility.Collapsed;
                    }
                    if (_rect7 != null)
                    {
                        _rect7.Visibility = Visibility.Collapsed;
                    }
					break;
                case 1:
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
                    if (_rect4 != null)
                    {
                        _rect4.Visibility = Visibility.Collapsed;
                    }
                    if (_rect7 != null)
                    {
                        _rect7.Visibility = Visibility.Collapsed;
                    }
                    break;
                case 2:
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
                    if (_rect4 != null)
                    {
                        _rect4.Visibility = Visibility.Visible;
                    }
                    if (_rect7 != null)
                    {
                        _rect7.Visibility = Visibility.Collapsed;
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
                    if (_rect4 != null)
                    {
                        _rect4.Visibility = Visibility.Visible;
                    }
                    if (_rect7 != null)
                    {
                        _rect7.Visibility = Visibility.Visible;
                    }
                    break;
			}
		}
	}
}
