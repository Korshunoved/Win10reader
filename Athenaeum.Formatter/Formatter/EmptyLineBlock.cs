using System;
using System.Collections;
using System.ComponentModel;

using Athenaeum.Styles;

namespace Athenaeum.Formatter
{
	public class EmptyLineBlock : Block, IDrawable
	{
		#region Constructors/Disposer
		public EmptyLineBlock( DocumentFormatter formatter, FictionBook.Element element, int id )
			: base( formatter, element, id )
		{
		}
		#endregion

		#region BuildChildBlocks
		public override void UpdateStyles()
		{
			base.UpdateStyles();
			Bounds = new XRect( 0, 0, 0, (int) PaintObject.Font.Height );
		}
		#endregion

		#region Format
		public override int Format( XRect bounds )
		{
			int y = (int) bounds.Top;

			Formatter.OnBlockFormatting( y );

			int bt = ( (int) bounds.Bottom / (int) bounds.Height ) * (int) bounds.Height;

			if (y + PaintObject.Font.Space.Height > (int) bt)
			{
				y += bt - y;
			}

			if (!Formatter.RectangleFitsOnPage( new XRect( 0, y, bounds.Width, Bounds.Height ) ))
				y = Formatter.AdjustToPage( y );

			Bounds = new XRect( bounds.Left, y, bounds.Width, Bounds.Height );

			//if (y % (int) Formatter.PageSize.Height == 0)
			//	Formatter.Drawables.Add( this ); ;

			Formatter.OnBlockFormatted( this );

			return PaintingContext.Settings.EliminateEmptyLines ? y - (int) bounds.Top : (int) Bounds.Height;
		}
		#endregion

		int IDrawable.Y
		{
			get { return (int) Bounds.Y; }
		}
		Pointer IDrawable.GetPointer()
		{
			return new Pointer( Id );
		}
	}
}
