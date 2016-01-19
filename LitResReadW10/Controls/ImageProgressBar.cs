using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace LitRes.Controls
{
	[TemplatePart(Name = "CLIPRECTANGLE", Type = typeof(RectangleGeometry))]
	public class ImageProgressBar : ProgressBar
	{
		public ImageProgressBar()
		{
			this.DefaultStyleKey = typeof(ImageProgressBar);
		}

        public ImageSource Source
		{
			get { return (ImageSource)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register("Source", typeof(ImageSource), typeof(ImageProgressBar), new PropertyMetadata(null));

        public ImageSource SourceMaskUri
        {
            get { return (ImageSource)GetValue(SourceMaskUriProperty); }
            set { SetValue(SourceMaskUriProperty, value); }
        }

        public static readonly DependencyProperty SourceMaskUriProperty =
            DependencyProperty.Register("SourceMaskUri", typeof(ImageSource), typeof(ImageProgressBar), new PropertyMetadata(null));

        public Brush Fill
		{
			get { return (Brush)GetValue(FillProperty); }
			set { SetValue(FillProperty, value); }
		}

		public static readonly DependencyProperty FillProperty =
			DependencyProperty.Register("Fill", typeof(Brush), typeof(ImageProgressBar), new PropertyMetadata(null));

		private RectangleGeometry _clip;

	    protected override void OnApplyTemplate()
	    {
            base.OnApplyTemplate();
            
            _clip = this.GetTemplateChild("CLIPRECTANGLE") as RectangleGeometry;
            this.ValueChanged += ImageProgressBar_ValueChanged;
            this.SizeChanged += ImageProgressBar_SizeChanged;
        }

        void ImageProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateClip();
		}

        private void ImageProgressBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateClip();
        }

		private void UpdateClip()
		{
			if (_clip != null)
			{
				_clip.Rect = new Rect(0, 0, this.ActualWidth * ((this.Value - this.Minimum) / (this.Maximum - this.Minimum)), this.ActualHeight);
			}
		}
	}
}
