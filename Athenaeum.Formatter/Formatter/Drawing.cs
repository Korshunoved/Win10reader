using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Athenaeum.Formatter
{

	internal interface IDrawable
	{
		int Y { get; }
		Pointer GetPointer();
	}

	public interface IXFont : IEquatable<IXFont>
	{
		XSize MeasureString( string text, int pos = 0, int length = 0 );
		void DrawString( object target, XPoint destination, XColor color, string text, int pos = 0, int length = 0 );
		XSize Space { get; }
		XSize Hyphen { get; }
		String HyphenString { get; }
		string Hash { get; }
		int Height { get; }
	}

	public interface IDrawingContext
	{
		IXFont GetFont( Faces face, int size, bool bold = false, bool italic = false );
		object CreateTarget( int width, int height );
		void ReleaseTarget( object target );
		void Clear( object target, XColor color );
		void Clear( object target, string uri, double brightness );
		XSize MeasureImage( byte[] source );
		void DrawImage( object target, XRect destination, byte[] source );
		void FillRectangle( object target, XRect destination, XColor color );
		ICollection<Athenaeum.Styles.Stylesheet> GetStyles();
	}

	public struct XRect : IEquatable<XRect>
	{
		public int X, Y, Width, Height;
		public int Left { get { return Math.Min( X, X + Width ); } }
		public int Top { get { return Math.Min( Y, Y + Height ); } }
		public int Right { get { return Math.Max( X, X + Width ); } }
		public int Bottom { get { return Math.Max( Y, Y + Height ); } }
		public static readonly XRect Empty = default( XRect );
		public bool IsEmpty { get { return this == Empty; } }

		#region ctor
		public XRect( int x, int y, int width, int height )
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}
		public XRect( XPoint p, XSize s )
		{
			X = p.X;
			Y = p.Y;
			Width = s.Width;
			Height = s.Height;
		}
		#endregion

		#region operators
		public static bool operator ==( XRect a, XRect b )
		{
			return a.Equals( b );
		}
		public static bool operator !=( XRect a, XRect b )
		{
			return a.X != b.X || a.Y != b.Y || a.Width != b.Width || a.Height != b.Height;
		}
		public static bool operator &( XRect a, XRect b )
		{
			return a.Intersected( b );
		}
		#endregion

		#region overridables/implementations
		public override bool Equals( object obj )
		{
			if (obj is XRect)
				return this.Equals( (XRect) obj );
			return base.Equals( obj );
		}
		public override int GetHashCode()
		{
			int i1 = 5381;
			int i2 = (int) X;
			i1 = ( ( i1 << 5 ) + i1 ) ^ i2;
			i2 = (int) Y;
			i1 = ( ( i1 << 5 ) + i1 ) ^ i2;
			i2 = (int) Width;
			i1 = ( ( i1 << 5 ) + i1 ) ^ i2;
			i2 = (int) Height;
			i1 = ( ( i1 << 5 ) + i1 ) ^ i2;
			return i1;
		}
		public override string ToString()
		{
			return string.Format( "{0},{1},{2},{3}", X, Y, Width, Height );
		}
		public bool Equals( XRect b )
		{
			return X == b.X && Y == b.Y && Width == b.Width && Height == b.Height;
		}
		#endregion

	}

	public struct XPoint : IEquatable<XPoint>
	{

		public int X, Y;

		#region ctor
		public XPoint( int x, int y )
		{
			X = x;
			Y = y;
		}
		public XPoint( XPoint b )
		{
			X = b.X;
			Y = b.Y;
		}
		#endregion

		#region operators
		public static bool operator ==( XPoint a, XPoint b )
		{
			return a.Equals( b );
		}
		public static bool operator !=( XPoint a, XPoint b )
		{
			return a.X != b.X || a.Y != b.Y;
		}
		#endregion

		#region overridables
		public override string ToString()
		{
			return string.Format( "{0},{1}", X, Y );
		}
		public override bool Equals( object obj )
		{
			if (obj is XPoint)
				return this.Equals( (XPoint) obj );
			return base.Equals( obj );
		}
		public override int GetHashCode()
		{
			int i1 = 5381;
			int i2 = (int) X;
			i1 = ( ( i1 << 5 ) + i1 ) ^ i2;
			i2 = (int) Y;
			i1 = ( ( i1 << 5 ) + i1 ) ^ i2;
			return i1;
		}
		public bool Equals( XPoint b )
		{
			return X == b.X && Y == b.Y;
		}
		#endregion

	}

	public struct XSize : IEquatable<XSize>
	{

		public int Width, Height;
		public static readonly XSize Empty = default( XSize );
		public bool IsEmpty { get { return this == Empty; } }

		#region ctor
		public XSize( int width, int height )
		{
			Width = width;
			Height = height;
		}
		public XSize( XSize b )
		{
			Width = b.Width;
			Height = b.Height;
		}
		#endregion

		#region operators
		public static bool operator ==( XSize a, XSize b )
		{
			return a.Equals( b );
		}
		public static bool operator !=( XSize a, XSize b )
		{
			return a.Width != b.Width || a.Height != b.Height;
		}
		public static XSize operator *( XSize a, double b )
		{
			return new XSize( (int) ( a.Width * b ), (int) ( a.Height * b ) );
		}
		#endregion

		#region overridables
		public override bool Equals( object obj )
		{
			if (obj is XSize)
				return this.Equals( (XSize) obj );
			return base.Equals( obj );
		}
		public override int GetHashCode()
		{
			int i1 = 5381;
			int i2 = (int) Width;
			i1 = ( ( i1 << 5 ) + i1 ) ^ i2;
			i2 = (int) Height;
			i1 = ( ( i1 << 5 ) + i1 ) ^ i2;
			return i1;
		}
		public override string ToString()
		{
			return string.Format( "{0},{1}", Width, Height );
		}
		public bool Equals( XSize b )
		{
			return Width == b.Width && Height == b.Height;
		}
		#endregion

	}

	public static class XColors
	{
		public static readonly XColor InkDay = new XColor( 0x3, 0x3, 0x3 );
		public static readonly XColor InkNight = new XColor( 0xf2, 0xf0, 0xe3 );
		public static readonly XColor InkSepia = new XColor( 0x1c, 0x0e, 0x00 );
		public static readonly XColor Transparent = new XColor( 255, 255, 255, 0 );
		public static readonly XColor Black = new XColor( 0, 0, 0 );
		public static readonly XColor White = new XColor( 255, 255, 255 );
		public static readonly XColor LightSepia = new XColor( 255, 218, 185 );
		public static readonly XColor DarkSepia = new XColor( 94, 38, 18 );
		public static readonly XColor Magenta = new XColor( 255, 0, 255 );
		public static readonly XColor Purple = new XColor( 128, 0, 128 );
		public static readonly XColor LinkDay = new XColor( 0, 0, 255 );
		public static readonly XColor LinkNight = new XColor( 0, 0, 255 );
		public static readonly XColor LinkSepia = new XColor( 0, 0, 255 );
	}

	public struct XColor : IEquatable<XColor>
	{
		public byte A, R, G, B;

		#region ctor
		public XColor( byte r, byte g, byte b, byte a = 255 )
		{
			A = a;
			R = r;
			G = g;
			B = b;
		}
		#endregion

		#region operators
		public static bool operator ==( XColor a, XColor b )
		{
			return ( (IEquatable<XColor>) a ).Equals( b );
		}
		public static bool operator !=( XColor a, XColor b )
		{
			return a.A != b.A || a.R != b.R || a.G != b.G || a.B != b.B;
		}
		public static XColor operator *( XColor a, double b )
		{
			return new XColor( (byte) Math.Max( Math.Min( a.R * b, 255 ), 0 ), (byte) Math.Max( Math.Min( a.G * b, 255 ), 0 ), (byte) Math.Max( Math.Min( a.B * b, 255 ), 0 ), a.A );
		}
		#endregion

		#region overridables
		public override string ToString()
		{
			return string.Format( "{0},{1},{2}:{3}", R, G, B, A );
		}
		public override int GetHashCode()
		{
			return (int) A << 24 | (int) R << 16 | (int) G << 8 | (int) B;
		}
		public override bool Equals( object obj )
		{
			if (obj is XColor)
				return this.Equals( (XColor) obj );
			return base.Equals( obj );
		}
		public bool Equals( XColor b )
		{
			return A == b.A && R == b.R && G == b.G && B == b.B;
		}
		#endregion

	}

	public static class D2X
	{
		public static bool Intersected( this XRect a, XRect b )
		{
			return a.Left <= b.Right && b.Left <= a.Right && a.Top <= b.Bottom && b.Top <= a.Bottom;
		}
		public static XRect Intersect( this XRect a, XRect b )
		{
			int x = Math.Max( a.Left, b.Left );
			int y = Math.Max( a.Top, b.Top );
			int w = Math.Min( a.Right, b.Right ) - x;
			int h = Math.Min( a.Bottom, b.Bottom ) - y;
			return w < 0 || h < 0 ? XRect.Empty : new XRect( x, y, w, h );
		}
		public static XRect Union( this XRect a, XRect b )
		{
			int x = Math.Min( a.Left, b.Left );
			int y = Math.Min( a.Top, b.Top );
			int w = Math.Max( a.Right, b.Right ) - x;
			int h = Math.Max( a.Bottom, b.Bottom ) - y;
			return new XRect( x, y, w, h );
		}
		public static bool Contains( this XRect r, XPoint p )
		{
			return r.Left <= p.X && p.X <= r.Right && r.Top <= p.Y && p.Y <= r.Bottom;
		}
		public static bool Contains( this XRect r, int x, int y )
		{
			return r.Left <= x && x <= r.Right && r.Top <= y && y <= r.Bottom;
		}
	}

}
