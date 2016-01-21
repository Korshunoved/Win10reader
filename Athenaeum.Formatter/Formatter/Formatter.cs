using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Athenaeum.Styles;

namespace Athenaeum.Formatter
{
	public sealed class DocumentFormatter : IDisposable//, ISectionBlockContainer
	{

		public struct TimingStruct
		{
			public TimeSpan BuildBlocks, UpdateStyles, Format;
		}

		public TimingStruct Timing;

		private static IDictionary<Type, Type> ElementToBlockMap { get; set; }

		public FictionBook.Document Document { get; private set; }
		public FictionBook.Element RootElement { get; private set; }
		public Stylesheet Stylesheet { get; private set; }
		public Block RootBlock { get; set; }
        public Block RootAnnotationBlock { get; set; }
		public XRect PageSize { get; set; }
		public List<Pointer> Pages = new List<Pointer>();
		internal IDrawingContext Context;

		private int LastBlockId { get; set; }

		private IDictionary<Athenaeum.Styles.Style, PaintObject> PaintCache { get; set; }

		private List<Athenaeum.Styles.Style> StyleStack { get; set; }
		private Athenaeum.Styles.Style CurrentStyle { get; set; }

		#region Constructors/Disposer
		static DocumentFormatter()
		{
			ElementToBlockMap = new Dictionary<Type, Type>();

			ElementToBlockMap.Add(typeof(FictionBook.Document), typeof(DocumentBlock));
			ElementToBlockMap.Add(typeof(FictionBook.Body), typeof(BodyBlock));
			ElementToBlockMap.Add(typeof(FictionBook.SectionElement), typeof(SectionBlock));
			ElementToBlockMap.Add(typeof(FictionBook.ImageElement), typeof(ImageBlock));
			ElementToBlockMap.Add(typeof(FictionBook.TitleElement), typeof(TitleBlock));
			ElementToBlockMap.Add(typeof(FictionBook.AnnotationElement), typeof(AnnotationBlock));
			ElementToBlockMap.Add(typeof(FictionBook.SubtitleElement), typeof(SubtitleBlock));
			ElementToBlockMap.Add(typeof(FictionBook.EpigraphElement), typeof(EpigraphBlock));
			ElementToBlockMap.Add(typeof(FictionBook.CiteElement), typeof(CiteBlock));
			ElementToBlockMap.Add(typeof(FictionBook.PoemElement), typeof(PoemBlock));
			ElementToBlockMap.Add(typeof(FictionBook.StanzaElement), typeof(StanzaBlock));
			ElementToBlockMap.Add(typeof(FictionBook.VerseElement), typeof(VerseBlock));
			ElementToBlockMap.Add(typeof(FictionBook.ParagraphElement), typeof(ParagraphBlock));
			ElementToBlockMap.Add(typeof(FictionBook.EmptyLineElement), typeof(EmptyLineBlock));
			ElementToBlockMap.Add(typeof(FictionBook.TableElement), typeof(TableBlock));
			ElementToBlockMap.Add(typeof(FictionBook.TableRowElement), typeof(TableRowBlock));
			ElementToBlockMap.Add(typeof(FictionBook.TableCellElement), typeof(TableCellBlock));
			ElementToBlockMap.Add(typeof(FictionBook.TextAuthorElement), typeof(TextAuthorBlock));
		}

		public DocumentFormatter(FictionBook.Document document, FictionBook.Element rootElement, Stylesheet stylesheet)
		{
			//GraphicsDevice = graphicsDevice;
			Document = document;
			RootElement = rootElement;
			Stylesheet = stylesheet;
			StyleStack = new List<Styles.Style>();
			PaintCache = new Dictionary<Athenaeum.Styles.Style, PaintObject>();
			/*

			 m_paintCache = new Hashtable();
			 m_styleStack = new ArrayList();

			 m_buffer = new Bitmap(100, 100);
			 m_graphics = Graphics.FromImage(m_buffer);
			 m_graphics.TextRenderingHint = m_textRenderingHint;

			 m_stringFormat = new StringFormat(StringFormat.GenericTypographic);
			 m_stringFormat.Alignment = StringAlignment.Near;
			 m_stringFormat.LineAlignment = StringAlignment.Near;
			 m_stringFormat.Trimming = StringTrimming.None;
			 m_stringFormat.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
*/
		}

		public void Initialize()
		{
			DateTime current = DateTime.Now;
			BuildBlocks();
			Timing.BuildBlocks = DateTime.Now - current;
			current = DateTime.Now;
			UpdateStyles();
			Timing.UpdateStyles = DateTime.Now - current;
		}

		public void Dispose()
		{
			foreach(IDisposable paintObject in PaintCache.Values)
				paintObject.Dispose();
			PaintCache.Clear();
		}
		#endregion

		#region Public Properties
		/*public FictionBook.Document Document
		{
			get { return m_document; }
		}*/

		/*public FictionBook.Element RootElement
		{
			get { return m_rootElement; }
		}*/

		/* public Stylesheet Stylesheet
		 {
			 get { return m_stylesheet; }
			 set { m_stylesheet = value; }
		 }*/

		/*public Block RootBlock
		{
			get { return m_rootBlock; }
		}*/

		/*public Graphics Graphics
		{
			get { return m_graphics; }
		}*/

		/*public StringFormat StringFormat
		{
			get { return m_stringFormat; }
		}*/

		/*public TextRenderingHint TextRenderingHint
		{
			get { return m_textRenderingHint; }
			set { m_textRenderingHint = value; }
		}*/

		/*public Size PageSize
		{
			get { return m_pageSize; }
		}*/
		#endregion

		#region Style Stack Processing
		public void PushStyle(string styleName)
		{
			PushStyle(Stylesheet[styleName]);
		}

		public void PushStyle(Athenaeum.Styles.Style style)
		{
			if(style == null)
				throw new ArgumentNullException("style");

			StyleStack.Add(style);
			CurrentStyle = null;
		}

		public void PopStyle()
		{
			if(StyleStack.Count > 0)
			{
				StyleStack.RemoveAt(StyleStack.Count - 1);
				CurrentStyle = null;
			}
		}


		int styleCloneCount, styleMergeCount;

		public Athenaeum.Styles.Style GetCurrentStyle()
		{
			if(CurrentStyle == null)
			{
				if(StyleStack.Count == 0)
				{
					CurrentStyle = Stylesheet[KnownStyles.Normal];
					PaintingContext.Settings.ApplySetting(CurrentStyle);
				}
				else
				{
					CurrentStyle = Stylesheet[KnownStyles.Normal].Clone();
					PaintingContext.Settings.ApplySetting(CurrentStyle);

					styleCloneCount++;

					foreach(Athenaeum.Styles.Style style in StyleStack)
					{
						try
						{
							Athenaeum.Styles.Style.Merge(style, CurrentStyle);
							styleMergeCount++;
						}
						catch(Exception ex)
						{
							throw ex;
						}
					}
				}
			}

			return CurrentStyle;
		}

		#endregion

		#region Paint Cache Processing
		internal PaintObject GetCurrentPaintObject()
		{
			return GetPaintObject(GetCurrentStyle());
		}

		internal PaintObject GetPaintObject(Athenaeum.Styles.Style style)
		{
			PaintObject paintObject = null;
			if(!PaintCache.TryGetValue(style, out paintObject))
			{
				IXFont font = this.Context.GetFont(PaintingContext.Settings.Face, style.FontSize.Value, style.Bold == Flag.On, style.Italic == Flag.On);
				paintObject = new PaintObject(font, style);
				//paintObject.font = PaintingContext.DrawingContext.GetFont( PaintingContext.Settings.Face, style.FontSize.Value, style.Bold == Flag.On, style.Italic == Flag.On );
				//paintObject.Style = style;
				PaintCache.Add(style, paintObject);
			}
			return paintObject;
		}
		#endregion

		#region Paging Support
		internal bool RectangleFitsOnPage(XRect rc)
		{
			if(PageSize.Height <= 0)
				return true;

			if(rc.Top % PageSize.Height == 0)
				return true;

			int pageNumber = (int)rc.Top / (int)PageSize.Height;
			XRect pageBounds = new XRect(0, pageNumber * PageSize.Height, PageSize.Width, PageSize.Height);

			if(rc.Bottom < pageBounds.Bottom)
				return true;

			//if (rc.Bottom == pageBounds.Bottom)
			//	return true;

			return false;
		}

		internal int AdjustToPage(int top)
		{

			if(PageSize.Height <= 0)
				return top;

			int a = top + (int)PageSize.Height - top % (int)PageSize.Height;

			int pageNumber = top / (int)PageSize.Height;

			int b = (int)PageSize.Height * (pageNumber + 1);

			if(a != b)
			{
			}

			return (int)PageSize.Height * (pageNumber + 1);
		}
		#endregion

		#region Units Conversion
		internal int UnitsToPixels(double units)
		{
			return (int)(units * 5);
		}
		#endregion

		#region Blocks Building
		public void BuildBlocks()
		{            
			RootBlock = CreateBlockForElement(RootElement);
            if (RootAnnotationBlock == null && RootBlock.Element.Children.Count > 1)
		    {
		        var saveChild = RootBlock.Element.Children[1];
                (RootBlock as ContainerBlock).Children.RemoveAt(1);
		        RootBlock.Element.Children.RemoveItemCustom(1);

                RootAnnotationBlock = CreateBlockForElement(RootElement.Clone());
                RootAnnotationBlock.Element.Children.Clear();
                RootAnnotationBlock.Element.Children.Add(saveChild);
		    }
		}

		public Block CreateBlockForElement(FictionBook.Element element)
		{
			Type blockType = ElementToBlockMap[element.GetType()];
			Block block = null;

			try
			{
				block = (Block)Activator.CreateInstance(blockType, new object[] { this, element, ++LastBlockId });
                block.BuildChildrenBlocks(RootAnnotationBlock);
			}
			catch(Exception)
			{
				//
			}

			return block;
		}
		#endregion

		#region Formatting

		private List<IDrawable> EnumerateDrawables(Block block, List<IDrawable> buffer)
		{
			if(buffer == null)
				buffer = new List<IDrawable>();
			if(block is ContainerBlock)
				foreach(Block child in ((ContainerBlock)block).Children)
					EnumerateDrawables(child, buffer);
			if(block is ParagraphBlock)
				foreach(TextLine line in ((ParagraphBlock)block).Lines)
					if(((ParagraphBlock)block).Lines.Count > 0)
						buffer.Add(line.Items[0]);
			if(block is IDrawable)
				buffer.Add((IDrawable)block);
			return buffer;
		}

		private void Paginate2()
		{
			List<IDrawable> drawables = EnumerateDrawables(RootBlock, null);
			drawables = drawables.OrderBy(p => p.Y).ToList();
			Pages.Clear();
			Pages.AddRange(new Pointer[PagesCount]);
			for(int pos = 0, y = 0, pageNumber = 0; y < RootBlock.Bounds.Height; y += PageSize.Height, pageNumber++)
			{
				for(; pos < drawables.Count; pos++)
				{
					if(drawables[pos].Y >= y)
					{
						Pages[pageNumber] = drawables[pos].GetPointer();
						break;
					}
				}
			}
		}

		private IList<IDrawable> Paginate(Block block, IList<IDrawable> collection)
		{
			if(collection == null)
				collection = new List<IDrawable>(new IDrawable[PagesCount]);

			if(block is ContainerBlock)
				foreach(Block b in ((ContainerBlock)block).Children)
				{
					collection = Paginate(b, collection);
				}
			else if(block is IDrawable)
			{
				//int pageIndex = GetPageIndex( block.Bounds.Y );
				int top = ((int)block.Bounds.Top / (int)PageSize.Height) * (int)PageSize.Height;
				int bottom = ((int)block.Bounds.Bottom / (int)PageSize.Height) * (int)PageSize.Height;

				if(block.Bounds.Y % (int)PageSize.Height == 0)
				{
					collection[GetPageIndex(block.Bounds.Y)] = (IDrawable)block;
					//collection.Add( ( (IDrawable) block ) );
				}
				else if(block is ParagraphBlock && top != bottom)
				{
					foreach(TextLine line in ((ParagraphBlock)block).Lines)
					{
						if(line.Bounds.Y % (int)PageSize.Height == 0)
						{
							collection[GetPageIndex(line.Bounds.Y)] = ((TextLineWord)line.Items[0]);
							break;
						}
					}
				}
			}
			return collection;
		}

		private int PagesCount
		{
			get { return (int)RootBlock.Bounds.Height / (int)PageSize.Height + 1; }
		}

		private int GetPageIndex(double y)
		{
			return (int)y / (int)PageSize.Height;
		}

		private int GetPageNumber(double y)
		{
			return GetPageIndex(y) + 1;
		}

		private void Paginate(int i)
		{
			int y = (int)PageSize.Height * i;
			Block block = RootBlock.FindFirst(p => p is IDrawable && p.Bounds.Y == y);
			if(block == null)
			{
				block = RootBlock.FindDepth(b => b is ParagraphBlock && b.Bounds.Top <= y && b.Bounds.Bottom >= y);
				if(block != null)
				{
					ParagraphBlock p = (ParagraphBlock)block;
					TextLineWord word = null;
					foreach(TextLine line in p.Lines)
					{
						word = (TextLineWord)line.Items.FirstOrDefault(l => l.Bounds.Top >= y);
						if(word != null)
							break;
					}
					if(word != null)
					{
						Pages[i] = new ParagraphPointer(p.Id, word.Index);
					}
				}
			}
			else
				Pages[i] = new Pointer(block.Id);
		}

		private void Paginate()
		{
			IList<IDrawable> drawables = Paginate(RootBlock, null);
			Pages.Clear();

			Pages.AddRange(new Pointer[drawables.Count]);

			for(int i = 0; i < drawables.Count; i++)
			{
				if(drawables[i] == null)
					Paginate(i);
				else
					Pages[i] = drawables[i].GetPointer();
			}
		}

		public IList<int> GetInvalidPageNumbers()
		{
			IList<int> buffer = new List<int>();
			for(int index = 0; index < Pages.Count; index++)
				if(Pages[index] == null)
					buffer.Add(index);
			return buffer;
		}

		public int Format(XRect pageSize)
		{

			DateTime current = DateTime.Now;

			PageSize = pageSize;

			int rc = RootBlock.Format(new XRect(0, 0, PageSize.Width, PageSize.Height));

			Paginate2();

			if(Formatted != null)
				Formatted(this, new FormattedEventArgs());

			Timing.Format = DateTime.Now - current;

			return rc;
		}

		public void ClearFormatting()
		{
			RootBlock.ClearFormatting();
		}

		public void UpdateStyles()
		{
			foreach(IDisposable po in PaintCache.Values)
				po.Dispose();

			PaintCache.Clear();

			StyleStack.Clear();
			CurrentStyle = null;

			if(RootBlock != null)
				RootBlock.UpdateStyles();
		}
		#endregion

		#region FindBlock
		public Block FindBlockAtPoint(int x, int y)
		{
			return FindBlockAtPoint(new XPoint(x, y));
		}

		public Block FindBlockAtPoint(XPoint pt)
		{
			return FindBlockAtPoint(RootBlock, pt);
		}

		private Block FindBlockAtPoint(Block block, XPoint pt)
		{
			if(block is ContainerBlock)
			{
				Block found = null;

				foreach(Block child in ((ContainerBlock)block).Children)
				{
					if((found = FindBlockAtPoint(child, pt)) != null)
						return found;
				}

				return null;
			}
			else
			{
				if(block.Bounds.Contains(pt))
					return block;
			}

			return null;
		}

		public Block FindBlockForElement(FictionBook.Element element)
		{
			return FindBlockForElement(RootBlock, element);
		}

		private Block FindBlockForElement(Block block, FictionBook.Element element)
		{
			if(block.Element == element)
				return block;

			if(block is ContainerBlock)
			{
				Block found = null;

				foreach(Block child in ((ContainerBlock)block).Children)
					if((found = FindBlockForElement(child, element)) != null)
						return found;
			}

			return null;
		}

		public Block FindBlockById(int id)
		{
			return FindBlockById(RootBlock, id);
		}

		private Block FindBlockById(Block block, int id)
		{
			if(block.Id == id)
				return block;

			if(block is ContainerBlock)
			{
				Block found = null;

				foreach(Block child in ((ContainerBlock)block).Children)
					if((found = FindBlockById(child, id)) != null)
						return found;
			}

			return null;
		}
		#endregion

		#region Events

		public class BlockFormattedEventArgs : EventArgs
		{
			public Block Block { get; private set; }
			public BlockFormattedEventArgs(Block block)
			{
				Block = block;
			}
		}
		public delegate void BlockFormattedEventHandler(object sender, BlockFormattedEventArgs args);
		public event BlockFormattedEventHandler BlockFormatted;
		internal void OnBlockFormatted(Block block)
		{
			if(BlockFormatted != null)
				BlockFormatted(this, new BlockFormattedEventArgs(block));
		}

		public class BlockFormattingEventArgs : EventArgs
		{
			public double Y { get; private set; }
			public BlockFormattingEventArgs(double y)
			{
				Y = y;
			}
		}
		public delegate void BlockFormattingEventHandler(object sender, BlockFormattingEventArgs args);
		public event BlockFormattingEventHandler BlockFormatting;
		internal void OnBlockFormatting(double y)
		{
			if(BlockFormatting != null)
				BlockFormatting(this, new BlockFormattingEventArgs(y));
		}


		public class FormattedEventArgs : EventArgs { }
		public delegate void FormattedEventHandler(object sender, FormattedEventArgs args);
		public event FormattedEventHandler Formatted;

		#endregion

		#region XPointer
		internal string GetXPointer(Pointer pointer)
		{
		    if (pointer == null) return null;

			StringBuilder buffer = new StringBuilder();
		    var pointer1 = pointer;
		    Block block = RootBlock.FindFirst(p => p.Id == pointer1.BlockId);
			bool hasPoint = false;
			if(block != null)
			{

				BodyBlock parentBody = (BodyBlock)block.FindUp(p => p is BodyBlock);
				if(parentBody.Index > 0)
					return null;

				while(block != null && !(block is BodyBlock))
				{
					if(block is ParagraphBlock && pointer is ParagraphPointer)
					{
						int? position = ((ParagraphBlock)block).GetMarkupPosition(((ParagraphPointer)pointer).ItemId);
						if(position != null && position > 0)
						{
							buffer.Insert(0, (position + 1).ToString());
							buffer.Insert(0, ".");
							hasPoint = true;
						}
					}
					buffer.Insert(0, (block.Index + 1).ToString());
					buffer.Insert(0, "/");
					block = block.Parent;
					pointer = null;
				}
			}
			else
			{
				return null;
			}
			if(!hasPoint)
				buffer.Append(".1");
			return string.Format(xpointerFormatString, buffer);
		}

		internal string GetXPointer(int pageNumber)
		{
			return GetXPointer(Pages[pageNumber - 1]);
		}

		const string xpointerStartMagicWord = "fb2#xpointer(point(/1/2";
		const string xpointerEndMagicWord = "))";
		static readonly string xpointerFormatString = xpointerStartMagicWord + "{0}" + xpointerEndMagicWord;

		internal Pointer ResolveXPointer(string xpointer)
		{
			if(xpointer != null)
				xpointer = xpointer.Trim();
			if(string.IsNullOrEmpty(xpointer))
				throw new ArgumentNullException();
			if(xpointer.IndexOf(xpointerStartMagicWord) != 0)
				throw new ArgumentException();
			if(xpointer.Substring(xpointer.Length - xpointerEndMagicWord.Length, xpointerEndMagicWord.Length) != xpointerEndMagicWord)
				throw new ArgumentException();
			xpointer = xpointer.Substring(xpointerStartMagicWord.Length);
			xpointer = xpointer.Substring(0, xpointer.Length - xpointerEndMagicWord.Length);
			if(string.IsNullOrEmpty(xpointer))
				throw new ArgumentException();
			if(xpointer[0] != '/')
				throw new ArgumentException();
			xpointer = xpointer.Substring(1);
			if(string.IsNullOrEmpty(xpointer))
				throw new ArgumentException();
			string[] blocks = xpointer.Split('/');
			Block block = RootBlock.FindFirst(b => b is BodyBlock);
			int pos = 0;
			int blockIndex = -1;
			int itemIndex = -1;
			while(block != null)
			{
				if(block is ContainerBlock)
				{
					if(pos >= blocks.Length)
						break;
					ContainerBlock container = (ContainerBlock)block;
					itemIndex = -1;
					string[] b = blocks[pos].Split('.');
					if(b.Length == 0 || b.Length > 2)
						throw new Exception();
					if(b.Length > 0)
						blockIndex = int.Parse(b[0]);
					if(b.Length == 2)
						itemIndex = int.Parse(b[1]);
					//if(blockIndex < 0 || blockIndex >= container.Children.Count)
					//	return null;
					//block = container.Children[blockIndex - 1];
					block = container.FindBlockByNotZeroBasedPosition(blockIndex);
					pos++;
				}
				else if(block is ParagraphBlock)
				{
					ParagraphBlock paragraph = (ParagraphBlock)block;
					//if(itemIndex > 0)
					//{
					int? wordIndex = paragraph.FindMarkupIndexByNotZeroBasedPosition(itemIndex);
					return wordIndex != null ? paragraph.Markup[(int)wordIndex].GetPointer() : paragraph.GetPointer();
					//}
					//else
					//{
					//	return paragraph.GetPointer();
					//}
				}
				else if(block is IDrawable)
				{
					return ((IDrawable)block).GetPointer();
				}
				else
					throw new Exception();
			}
			return null;
		}
		#endregion
	}

}
