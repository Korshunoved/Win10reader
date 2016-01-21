using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace Athenaeum.Formatter
{

	#region public class Block
	public abstract class Block : Bounded
	{
		public int Id { get; private set; }
		public int Index { get; internal set; }
		public FictionBook.Element Element { get; private set; }
		internal virtual PaintObject PaintObject { get; set; }
		public DocumentFormatter Formatter { get; private set; }
		internal Block Parent { get; set; }

		#region Constructors/Disposer
		public Block(DocumentFormatter formatter, FictionBook.Element element, int id)
		{
			Formatter = formatter;
			Element = element;
			Id = id;
		}
		#endregion

		#region Public Properties
		//public Point Location
		/*private Point Location
		{
			get { return m_bounds.; }
			set { m_bounds.Location = value; }
		}*/

		/*public Size Size
		{
			get { return m_bounds.Size; }
			set { m_bounds.Size = value; }
		}*/

		//public XRect Bounds { get; set; }
		#endregion

		#region BuildChildBlocks
		public virtual void BuildChildrenBlocks(object additionalRoot = null)
		{
		}
		#endregion

		#region UpdateStyles
		public virtual void UpdateStyles()
		{
			PaintObject = Formatter.GetCurrentPaintObject();
		}
		#endregion

		#region Format
		public abstract int Format(/*int left, int top, int width*/XRect bounds);
		#endregion

		#region Paint
		public virtual void Paint(PaintingContext context)
		{
			/*if (PaintObject != null)
			{
				context.Graphics.FillRectangle(PaintObject.BackBrush, Bounds);
			}*/
		}
		#endregion

		#region Methods
		internal void SetCommonHeight(int height)
		{
			Bounds.Height = height;
			if(Parent != null)
				Parent.SetCommonHeight(height);
		}

		public Pointer FindPointer(XPoint point, XRect bounds)
		{
			Block block = FindDepth(b => b.Bounds.Contains(point));
			if(block != null)
				return block.GetPointer(point, bounds);
			return CreatePointer();
		}

		protected virtual Pointer CreatePointer()
		{
			return new Pointer();
		}

		protected virtual Pointer GetPointer(XPoint point, XRect bounds)
		{
			Pointer pointer = CreatePointer();
			pointer.BlockId = Id;
			return pointer;
		}

		#endregion

		#region ToString
		public override string ToString()
		{
			return GetType().Name;
		}
		#endregion

		#region Find helpers
		internal Block FindUp(Predicate<Block> match)
		{
			if(match(this))
				return this;
			if(Parent != null)
				return Parent.FindUp(match);
			return null;
		}

		internal Block FindFirst(Predicate<Block> match)
		{
			if(match(this))
				return this;
			Block found = null;
			if(this is ContainerBlock)
				foreach(Block child in ((ContainerBlock)this).Children)
					if((found = child.FindFirst(match)) != null)
						return found;
			return found;
		}

		internal Block FindDepth(Predicate<Block> match, Func<Block, bool> where)
		{
			Block found = null;
			if(this is ContainerBlock)
			{
				foreach(Block child in ((ContainerBlock)this).Children.Where(where))
					if((found = child.FindDepth(match, where)) != null)
						break;
				if(found == null)
					if(match(this))
						return this;
			}
			else
			{
				if(match(this))
					return this;
			}
			return found;
		}

		internal Block FindDepth(Predicate<Block> match)
		{
			Block found = null;
			if(this is ContainerBlock)
			{
				foreach(Block child in ((ContainerBlock)this).Children)
					if((found = child.FindDepth(match)) != null)
						break;
			}
			else if(match(this))
				return this;
			return found;
		}
		#endregion

	}
	#endregion

	#region public class BlockCollection
	public class BlockCollection : List<Block>
	{
		internal Block Owner { get; set; }
		public BlockCollection(Block owner) { Owner = owner; }
		public new void Add(Block item)
		{
			base.Add(item);
			item.Parent = Owner;
		}
		public new void Insert(int pos, Block item)
		{
			base.Insert(pos, item);
			item.Parent = Owner;
		}
	}
	#endregion

	/*internal static class Utils
	{
		public static bool IsIntersect( this XRect r1, XRect r2 )
		{
			XRect rc = new XRect( r1.X, r1.Y, r1.Width, r1.Height );
			rc.Intersect( r2 );
			return rc != XRect.Empty;
		}
		public static bool Contains( this XRect r, double x, double y )
		{
			return r.Contains( new XPoint( x, y ) );
		}
	}*/

	#region public class ContainerBlock
	public class ContainerBlock : Block
	{
		public BlockCollection Children { get; private set; }

		#region Constructors/Disposer
		public ContainerBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
			: base(formatter, element, id)
		{
			Children = new BlockCollection(this);
		}
		#endregion

		#region Public Properties
		/* public IList<Block> Children
        {
            get { return m_children; }
        }*/
		#endregion

		#region BuildChildrenBlocks
        public override void BuildChildrenBlocks(object additionalRoot = null)
		{          
			base.BuildChildrenBlocks();

			FictionBook.ElementCollection children = Element.Children;
			int childsCount = children.Count;

			for(int index = 0; index < childsCount; ++index)
			{
                Block block = Formatter.CreateBlockForElement(children[index]);
                block.Index = index;
                //if (block is BodyBlock && Children.Count > 0)
                //{
                //    if(additionalRoot != null) (additionalRoot as DocumentBlock).Children.Add(block);
                //    block.BuildChildrenBlocks();
                //}
                //else Children.Add(block);
                Children.Add(block);
			}
		}
		#endregion

		#region UpdateStyles
		public override void UpdateStyles()
		{
			base.UpdateStyles();
			foreach(Block child in Children)
				child.UpdateStyles();
		}
		#endregion

		#region Format
		public override int Format(XRect bounds)
		{
			int y = (int)bounds.Top;

			//y += PreFormat(new XRect(bounds.Left, y, bounds.Width, bounds.Height));
			Bounds = bounds;

			foreach(Block child in Children)
				y += child.Format(new XRect(bounds.Left, y, bounds.Width, bounds.Height));

			//y += PostFormat(new XRect(bounds.Left, y, bounds.Width, bounds.Height));

			Bounds = new XRect(bounds.Left, bounds.Top, bounds.Width, y - bounds.Top);

			return y - (int)bounds.Top;
		}
		#endregion

		#region Paint
		public override void Paint(PaintingContext context)
		{
			if(Bounds.Intersected(context.Bounds))
			{
				foreach(Block child in Children)
					if(child.Bounds.Intersected(context.Bounds))
						child.Paint(context);
			}
			else
				return;
		}
		#endregion

		#region Protected Methods
		protected virtual int PreFormat(XRect bounds)
		{
			return 0;
		}

		protected virtual int PostFormat(XRect bounds)
		{
			return 0;
		}
		#endregion

		#region Public Methods
		public override void Offset(int dx, int dy)
		{
			base.Offset(dx, dy);
			OffsetChildren(dx, dy);
		}

		public virtual void OffsetChildren(int dx, int dy)
		{
			foreach(Block child in Children)
				child.Offset(dx, dy);
		}
		#endregion

		protected internal override void ClearFormatting()
		{
			base.ClearFormatting();
			foreach(Bounded item in Children)
				item.ClearFormatting();
		}

		internal Block FindBlockByNotZeroBasedPosition(int position)
		{
			position--;
			if(position < 0 || position >= Children.Count)
				return null;
			return Children[position];
		}

	}
	#endregion
}
