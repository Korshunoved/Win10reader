using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

using FictionBook;

namespace Athenaeum.Formatter
{

	public enum Themes { Day, Night, Sepia, }
    public enum Scales { xxLeastest = -2, xLeaster = -1, Least = 0, Small, Normal, Middle, Big, Biggest }
	public enum Faces { Sans, Serif, Mono }
	public enum Indents { Fit = 0, Small = 1, Normal = 2, Big = 4 }
	public enum Dencities { Low = -1, Medium = 0 }
	public enum LineSpacings { None = 0, Increased = 2, Big = 5, Extra = 10, }
    public enum ParagraphSpacing { Low = -5, Normal = 0, Big = 15, }
	public class PaintingContext : Bounded
	{

		internal IDrawingContext Context;
		internal object Target;
		//private static ICollection<Athenaeum.Styles.Stylesheet> _styles;

		public IList<LinkRegion> Links { get; private set; }
		internal Pointer Pointer { get; set; }
		/*internal static ICollection<Athenaeum.Styles.Stylesheet> Styles
		{
			get
			{
				if (_styles == null)
					_styles = Athenaeum.Styles.Stylesheet.LoadDefaultStyles();
				return _styles;
			}
		}*/

		#region Constructors/Disposer
		public PaintingContext(IDrawingContext context, XRect bounds)
			: base(bounds)
		{
			//Buffer = buffer;
			Context = context;
			Target = context.CreateTarget(bounds.Width, bounds.Height);
			//Buffer = (WriteableBitmap) ( Target = DrawingContext.CreateTarget( bounds.Width, bounds.Height ) );
		}
		#endregion

		public static class Settings
		{

			private const float _minBrightness = 0.2f;
			private const float _maxBrightness = 1f;

			private static float _brightness = _maxBrightness;
			private static Themes _theme;
			private static Scales _scale;
			private static Faces _face;
			private static Indents _indent = Indents.Normal;
			private static LineSpacings _lineSpacing;
			private static Dencities _density = Formatter.Dencities.Medium;
			private static bool _hyphenate;
			private static bool _eliminateEmptyLines = false;
			private static bool _useKerning = false;
			private static bool _justify = true;
            private static ParagraphSpacing _paragraphSpacing = ParagraphSpacing.Normal;

			public static Scales Scale
			{
				get { return _scale; }
				set
				{
					if(value != _scale)
					{
						_scale = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

			public static Faces Face
			{
				get { return _face; }
				set
				{
					if(value != _face)
					{
						_face = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

			public static Indents Indent
			{
				get { return _indent; }
				set
				{
					if(value != _indent)
					{
						_indent = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

			public static Dencities Dencity
			{
				get { return _density; }
				set
				{
					if(value != _density)
					{
						_density = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

			public static LineSpacings LineSpacing
			{
				get { return _lineSpacing; }
				set
				{
					if(value != _lineSpacing)
					{
						_lineSpacing = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

			public static bool Hyphenate
			{
				get { return _hyphenate; }
				set
				{
					if(value != _hyphenate)
					{
						_hyphenate = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

			public static bool EliminateEmptyLines
			{
				get { return _eliminateEmptyLines; }
				set
				{
					if(value != _eliminateEmptyLines)
					{
						_eliminateEmptyLines = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

			public static bool UseKerning
			{
				get { return _useKerning; }
				set
				{
					if(value != _useKerning)
					{
						_useKerning = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

			public static bool Justify
			{
				get { return _justify; }
				set
				{
					if(value != _justify)
					{
						_justify = value;
						if(FormattingChanged != null)
							FormattingChanged(null, new EventArgs());
					}
				}
			}

            public static ParagraphSpacing ParagraphSpacing
            {
                get { return _paragraphSpacing; }
                set
                {
                    if (value != _paragraphSpacing)
                    {
                        _paragraphSpacing = value;
                        if (FormattingChanged != null)
                            FormattingChanged(null, new EventArgs());
                    }
                }
            }

			internal static void ApplySetting(Athenaeum.Styles.Style style)
			{
				style.Alignment = PaintingContext.Settings.Justify ? Athenaeum.Styles.HAlignment.Justify : Athenaeum.Styles.HAlignment.Left;
				style.LeftIndent = style.RightIndent = (int)PaintingContext.Settings.Indent;                
			}

			public static float Brightness
			{
				get { return _brightness; }
				set
				{
					value = Math.Max(_minBrightness, Math.Min(_maxBrightness, value));
					if(_brightness != value)
					{
						_brightness = value;
						if(PaintChanged != null)
							PaintChanged(null, new EventArgs());
					}
				}
			}

			public static Themes Theme
			{
				get { return _theme; }
				set
				{
					if(value != _theme)
					{
						_theme = value;
						if(PaintChanged != null)
							PaintChanged(null, new EventArgs());
					}
				}
			}

			public delegate void FormattingChangedHandler(object sender, EventArgs args);
			public static event FormattingChangedHandler FormattingChanged;

			public delegate void PaintChangedHandler(object sender, EventArgs args);
			public static event PaintChangedHandler PaintChanged;

			public static IList<Themes> Themes = new List<Themes>(new Themes[] { Athenaeum.Formatter.Themes.Day, Athenaeum.Formatter.Themes.Night, Athenaeum.Formatter.Themes.Sepia, });
            public static IList<Scales> Scales = new List<Scales>(new Scales[] { Athenaeum.Formatter.Scales.Least, Athenaeum.Formatter.Scales.Small, Athenaeum.Formatter.Scales.Normal, Athenaeum.Formatter.Scales.Middle, Athenaeum.Formatter.Scales.Big, Athenaeum.Formatter.Scales.Biggest, });
			public static IList<Faces> Faces = new List<Faces>(new Faces[] { Athenaeum.Formatter.Faces.Sans, Athenaeum.Formatter.Faces.Serif, Athenaeum.Formatter.Faces.Mono, });
			public static IList<Dencities> Dencities = new List<Dencities>(new Dencities[] { Athenaeum.Formatter.Dencities.Low, Athenaeum.Formatter.Dencities.Medium, });
            //public static IList<int> Sizes = new List<int>(new int[] {  4, 6, 8, 10, 12, 
            //                                                           22, 24, 26, 28, 30 });
            public static IList<int> Sizes = new List<int>(new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 36, 44, });

            //public static IList<int> Sizes = new List<int>(new int[] { 0, 4, 8, 12, 16, 20, 24, 28, });
			public static IList<string> Backgrounds = new List<string>(new string[] { "Assets/BackgroundDay.png", "Assets/BackgroundNight.png", "Assets/BackgroundSepia.png", });
		}
	}

#if false
	/// <summary>
	/// Internal fonts helper
	/// </summary>
	public static class FontsHelper
	{

		public static SpriteFont Default
		{
			get { return SpriteFont.Find( PaintingContext.Settings.Sizes[(int) PaintingContext.Settings.Scale + 1] ); }
		}

		public static SpriteFont System
		{
			get { return SpriteFont.Find( PaintingContext.Settings.Sizes[(int) PaintingContext.Settings.Scale + 0] ); }
		}

		public static SpriteFont Find( Faces face, int size, bool bold = false, bool italic = false )
		{
			switch (PaintingContext.Settings.Scale)
			{
				case Athenaeum.Formatter.Scales.Normal:
					size = PaintingContext.Settings.Sizes[PaintingContext.Settings.Sizes.IndexOf( size ) + 1];
					break;
				case Athenaeum.Formatter.Scales.Big:
					size = PaintingContext.Settings.Sizes[PaintingContext.Settings.Sizes.IndexOf( size ) + 2];
					break;
			}
			SpriteFont font = SpriteFont.TryFind( face, size, bold, italic );
			if (font != null)
				return font;
			foreach (Faces f in PaintingContext.Settings.Faces.Where( p => p != face ))
			{
				font = SpriteFont.TryFind( f, size, bold, italic );
				if (font != null)
					return font;
			}
			return Default;
		}
	}
#endif

	/// <summary>
	/// Internal colors helper
	/// </summary>
	internal static class ColorsHelper
	{

		public static XColor Ink
		{
			get
			{
				switch(PaintingContext.Settings.Theme)
				{
					case Athenaeum.Formatter.Themes.Day: return XColors.InkDay * PaintingContext.Settings.Brightness;
					case Athenaeum.Formatter.Themes.Night: return XColors.InkNight * PaintingContext.Settings.Brightness;
					case Athenaeum.Formatter.Themes.Sepia: return XColors.InkSepia * PaintingContext.Settings.Brightness;
					default: throw new IndexOutOfRangeException();
				}
			}
		}

		public static XColor Link
		{
			get
			{
				switch(PaintingContext.Settings.Theme)
				{
					case Athenaeum.Formatter.Themes.Day: return XColors.LinkDay * PaintingContext.Settings.Brightness;
					case Athenaeum.Formatter.Themes.Night: return XColors.LinkNight * PaintingContext.Settings.Brightness;
					case Athenaeum.Formatter.Themes.Sepia: return XColors.LinkSepia * PaintingContext.Settings.Brightness;
					default: throw new IndexOutOfRangeException();
				}
			}
		}

		public static XColor Paper
		{
			get
			{
				switch(PaintingContext.Settings.Theme)
				{
					case Athenaeum.Formatter.Themes.Day: return XColors.White * PaintingContext.Settings.Brightness;
					case Athenaeum.Formatter.Themes.Night: return XColors.Black * PaintingContext.Settings.Brightness;
					case Athenaeum.Formatter.Themes.Sepia: return XColors.LightSepia * PaintingContext.Settings.Brightness;
					default: throw new IndexOutOfRangeException();
				}
			}
		}

		public static string Background
		{
			get { return PaintingContext.Settings.Backgrounds[(int)PaintingContext.Settings.Theme]; }
		}
	}
}
