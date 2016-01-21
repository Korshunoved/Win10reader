using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Athenaeum.Formatter
{

	public class Bounded
	{
		public XRect Bounds;

		#region Constructors
		public Bounded()
		{
		}

		public Bounded( XRect bounds )
		{
			Bounds = bounds;
		}
		#endregion

		#region Methods
		protected internal virtual void ClearFormatting()
		{
			Bounds.X = 0;
			Bounds.Y = 0;
			Bounds.Height = 0;
			Bounds.Width = 0;
		}

		public virtual void Offset( int dx, int dy )
		{
			Bounds = new XRect( Bounds.X + dx, Bounds.Y + dy, Bounds.Width, Bounds.Height );
		}
		#endregion

	}

	/*public class TextLineItem : Bounded
	{
		private const int IndexMask = 0x0000FFFF;
		protected int Flags { get; set; }
		public TextLineItem( int flags ) { Flags = flags; }
		public int Index { get { return Flags & IndexMask; } }
	}*/

	[Flags]
	public enum TextLineType { Inside = 0x0, First = 0x1, Last = 0x2, }

	public class TextLine : Bounded
	{

		#region Constructors
		internal TextLine( PaintObject paintObject )
		{
			PaintObject = paintObject; Items = new List<TextLineWord>();
		}
		#endregion

		#region Properties
		internal PaintObject PaintObject { get; private set; }

		internal TextLineType Type { get; set; }

		internal bool IsFirst
		{
			get { return ( Type & TextLineType.First ) != TextLineType.Inside; }
		}

		internal bool IsLast
		{
			get { return ( Type & TextLineType.Last ) != TextLineType.Inside; }
		}

		public IList<TextLineWord> Items { get; private set; }
		#endregion

		#region Methods
		public override void Offset( int dx, int dy )
		{
			base.Offset( dx, dy );
			foreach (Bounded item in Items)
				item.Offset( dx, dy );
		}

		public override string ToString()
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			foreach (TextLineWord item in Items)
				buffer.Append( buffer.Length == 0 ? "" : " " ).Append( item );
			return buffer.ToString();
		}

		protected internal override void ClearFormatting()
		{
			base.ClearFormatting();
			foreach (Bounded item in Items)
				item.ClearFormatting();
		}

		internal void ArrangeWords()
		{
			int count = Items.Count;
			for (int index = 0; index < count; index++)
			{
				if (index > 0)
				{
					Bounded prevWord = Items[index - 1];
					Items[index].Bounds.X = prevWord.Bounds.X + prevWord.Bounds.Width;
				}
			}
		}

		internal int GetWordsWidth()
		{
			int sumWidth = 0;
			for (int i = Items.Count - 1; i >= 0; i--)
				sumWidth += ( (int) Items[i].Bounds.Width + 0 );
			return sumWidth;
		}
		#endregion

	}

	public class TextLineWord : Bounded, IDrawable
	{

		internal ParagraphBlock Parent;
		internal FictionBook.TextElement Markup;
		internal int Position, Length, Offset;
		internal bool Hyphenated;
		public int Index;
		public int Subindex;
		internal bool[] Hyph { get; set; }

		public int Top { get { return Position + Length; } }
		public int Absolute { get { return Position + Offset; } }

		#region Constructors
		internal TextLineWord( int index, int subindex, int width, int height, PaintObject paintObject, FictionBook.LinkElement linkTarget )
			: this( index, width, height, paintObject, linkTarget )
		{
			Subindex = subindex;
		}
		internal TextLineWord( int index, int width, int height, PaintObject paintObject, FictionBook.LinkElement linkTarget )
			: base()
		{
			Index = index;
			PaintObject = paintObject;
			LinkTarget = linkTarget;
			Bounds = new XRect( Bounds.X, Bounds.Y, width, height );
		}
		#endregion

		#region Properties
		internal XColor Color
		{
			get { return HasLink ? ColorsHelper.Link : ColorsHelper.Ink; }
		}

		internal PaintObject PaintObject { get; private set; }

		internal FictionBook.LinkElement LinkTarget { get; private set; }

		internal bool HasLink
		{
			get { return LinkTarget != null; }
		}

		#endregion

		#region Methods
		protected internal override void ClearFormatting()
		{
			Bounds.X = 0;
			Bounds.Y = 0;
			//Bounds.Width = (int) Size.Width;
			//Bounds.Height = (int) Size.Height;
		}

		public override string ToString()
		{
			return Markup.Text.Substring( Position, Length );
		}

		#endregion

		int IDrawable.Y
		{
			get { return (int) Bounds.Y; }
		}
		public Pointer GetPointer()
		{
			return new ParagraphPointer( Parent.Id, Index );
		}

	}
}
