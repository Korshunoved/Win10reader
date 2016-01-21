using System;
using System.Collections;
using System.ComponentModel;
using Athenaeum.Styles;

namespace Athenaeum.Formatter
{
	#region public class TableBlock
	public class TableBlock : ContainerBlock
	{
		private int m_borderWidth;
		private int m_rowCount;
		private int m_columnCount;

		#region Constructors/Disposer
		public TableBlock( DocumentFormatter formatter, FictionBook.Element element, int id )
			: base( formatter, element, id )
		{
		}
		#endregion

		#region Public Properties
		public int BorderWidth
		{
			get { return m_borderWidth; }
			set { m_borderWidth = value; }
		}
		#endregion

		#region BuildChildBlocks
        public override void BuildChildrenBlocks(object additionalRoot = null)
		{
			base.BuildChildrenBlocks();

			// Calculating number of rows and columns

			for (int i = 0; i < Children.Count; ++i)
			{
				TableRowBlock rowBlock = (TableRowBlock) Children[i];

				m_columnCount = Math.Max( m_columnCount, rowBlock.Children.Count );
			}

			m_rowCount = Children.Count;
		}
		#endregion
	}
	#endregion

	#region public class TableRowBlock
	public class TableRowBlock : ContainerBlock
	{
		#region Constructors/Disposer
		public TableRowBlock( DocumentFormatter formatter, FictionBook.Element element, int id )
			: base( formatter, element, id )
		{
		}
		#endregion

		#region Format
		public override int Format( XRect bounds )
		{
			int columnCount = Children.Count;

			if (columnCount == 0)
				return 0;

			Athenaeum.Styles.Style style = Formatter.GetCurrentStyle();
			int y = (int)bounds.Top;
			int x = (int)bounds.Left;
			int columnWidth = (int)bounds.Width / columnCount;
			int columnWidthRest = (int)bounds.Width % columnCount;
			int rowHeight = 0;

			for (int i = 0; i < columnCount; ++i)
			{
				TableCellBlock cellBlock = (TableCellBlock) Children[i];
				int cellWidth = columnWidth;

				if (i < columnWidthRest)
					++cellWidth;

				int cellHeight = cellBlock.Format( new XRect( x, y, cellWidth, bounds.Height ) );

				rowHeight = Math.Max( rowHeight, cellHeight );
				x += cellWidth;
			}

			Bounds = new XRect( bounds.Left, bounds.Top, bounds.Width, rowHeight );

			return rowHeight;
		}
		#endregion
	}
	#endregion

	#region public class TableCellBlock
	public class TableCellBlock : ParagraphBlock
	{
		private int m_borderWidth;

		#region Constructors/Disposer
		public TableCellBlock( DocumentFormatter formatter, FictionBook.Element element, int id )
			: base( formatter, element, id )
		{
		}
		#endregion

		#region Public Properties
		public int BorderWidth
		{
			get { return m_borderWidth; }
			set { m_borderWidth = value; }
		}
		#endregion
	}
	#endregion
}
