using System;
using System.Linq;
using FictionBook;

using Athenaeum.Formatter;

namespace Athenaeum
{
	public class BookReader : Bounded
	{

		#region Fields
		private readonly Document _document;
		//private WriteableBitmap _buffer;
		private int _position;
		#endregion

		#region Constructors/Disposer
		public BookReader(IDrawingContext context, Document document, XSize bounds)
			: this(context, document, bounds, Pointer.Start)
		{
		}

		public BookReader(IDrawingContext context, Document document, XSize bounds, Pointer pointer)
			: this(context, document, new XRect(0, 0, (int)bounds.Width, (int)bounds.Height), pointer)
		{
		}

		public BookReader(IDrawingContext context, Document document, XRect bounds, Pointer pointer)
			: base(bounds)
		{
			DateTime d = DateTime.Now;
			_document = document;
			Formatter = new DocumentFormatter(_document, _document, context.GetStyles().First(p => p.Name == Athenaeum.Styles.KnownStylesheets.Text));
			Formatter.Context = context;
			Formatter.Initialize();
			Formatter.Format(Bounds);
			TimeSpan t = DateTime.Now - d;
			MoveTo(pointer);
		}
		#endregion

	    public delegate void onBookReaderMoveTo(object sender);

		#region Properties
		public Pointer Pointer { get; internal set; }
		public DocumentFormatter Formatter { get; internal set; }
	    public onBookReaderMoveTo OnMoveTo { get; set; }
		public int CurrentPage
		{
			get { return _position / (int)Formatter.PageSize.Height + 1; }
		}
		#endregion

		#region Methods

		public object Paint()
		{
			PaintingContext paintingContext = new PaintingContext(Formatter.Context, new XRect(Bounds.X, Bounds.X + _position, Bounds.Width, Bounds.Height));
			//paintingContext.Context.Clear( paintingContext.Target, ColorsHelper.Paper );
			paintingContext.Context.Clear(paintingContext.Target, ColorsHelper.Background, PaintingContext.Settings.Brightness);
			Formatter.RootBlock.Paint(paintingContext);
			return paintingContext.Target;
		}

		public void Release( object o )
		{
			Formatter.Context.ReleaseTarget( o );
		}

		public void MoveTo(Pointer pointer)
		{
			if(pointer == null)
			{
				throw new ArgumentNullException();
			}

			Pointer = pointer;

			_position = Pointer.ResolvePointerTop(Formatter);
		}

		public enum ValidatePageNumberResult { OK, EmptyBook, OutOfRange, NullPointer, }

		public ValidatePageNumberResult ValidatePageNumber(int pageNumber)
		{
			pageNumber--;
			if(Formatter.Pages.Count == 0)
				return ValidatePageNumberResult.EmptyBook;
			if(pageNumber < 0 || pageNumber >= Formatter.Pages.Count)
				return ValidatePageNumberResult.OutOfRange;
			if(Formatter.Pages[pageNumber] == null)
				return ValidatePageNumberResult.NullPointer;
			return ValidatePageNumberResult.OK;
		}

		public void MoveTo(int pageNumber)
		{
			pageNumber--;

			if(pageNumber <= 0)
			{
				Pointer = Pointer.Start;
			}
			else if(pageNumber >= Formatter.Pages.Count)
			{
				Pointer = Formatter.Pages.Last();
			}
			else
			{
				Pointer = Formatter.Pages[pageNumber];
			}

			_position = Pointer.ResolvePointerTop(Formatter);
		}

		public object GetLinkPointer(XPoint point, bool isHD = false)
		{
			Block block = Formatter.RootBlock.FindDepth(p => p.Bounds.Contains(point.X, point.Y + _position));
           
			if(block is ParagraphBlock)
			{
				foreach(var line in ((ParagraphBlock)block).Lines)
				{
					foreach(var item in line.Items)
					{
						if(item is TextLineWord)
						{
							var word = (TextLineWord)item;
						    var specialBounds = word.Bounds;
                            specialBounds.Y -= 70;
                            specialBounds.Width += 40;
						    if (isHD) specialBounds.Height += 100;
						    else specialBounds.Height += 50;
                            if (specialBounds.Contains((int)point.X, (int)point.Y + _position) && word.LinkTarget != null)
							{
								var linkElement = _document.Find(b => b.Id == word.LinkTarget.IdReference);

							    if(linkElement != null)
								{
									var linkBlock = Formatter.RootBlock.FindFirst(b => b.Element.Id == linkElement.Id);                                    
                                    if (linkBlock != null) return new Pointer(linkBlock.Id);
								}
							    else
							    {
                                    var lnkElem = Formatter.RootAnnotationBlock.Element.Children[0].Find(b => b.Id == word.LinkTarget.IdReference);
                                    if (lnkElem != null) return lnkElem;
							    }
							    return word.LinkTarget.LinkReference;
							}
						}
					}
				}
			}

			return null;
		}

		public void Reformat(XRect bounds, bool updateStyles = true)
		{
			if(!bounds.IsEmpty)
			{
				Bounds = bounds;
			}

			Formatter.ClearFormatting();
			if(updateStyles)
				Formatter.UpdateStyles();
			Formatter.Format(Bounds);

			_position = Pointer.ResolvePointerTop(Formatter);
		}

		public void Reformat(XSize size, bool updateStyles = true)
		{
			if(size.IsEmpty)
			{
				Reformat(XRect.Empty, updateStyles);
			}
			else
			{
				Reformat(new XRect(0, 0, (int)size.Width, (int)size.Height), updateStyles);
			}
		}

		private void BuildTableOfContent(ContainerBlock blocks, ITableOfContentNodeContainer container, TableOfContentBuildMode buildMode)
		{
			foreach(ContainerBlock block in blocks.Children.Where(p => p is BodyBlock || p is SectionBlock))
			{
				TableOfContentNode node = new TableOfContentNode();

				node.PageNumber = (int)block.Bounds.Y / (int)Formatter.PageSize.Height + 1;
				node.Pointer = new Pointer(block.Id);

				var title = block.Element.Children.Where(p => p is TitleElement).FirstOrDefault();

				node.Text = title == null ? string.Empty : title.ToString();

				container.Nodes.Add(node);

				BuildTableOfContent(block, buildMode == TableOfContentBuildMode.Tree ? node : container, buildMode);
			}
		}

		public TableOfContent GetTableOfContent(TableOfContentBuildMode buildMode = TableOfContentBuildMode.Tree)
		{
			TableOfContent toc = new TableOfContent();

			if(Formatter.RootBlock is ContainerBlock)
			{
				BuildTableOfContent((ContainerBlock)Formatter.RootBlock, toc, buildMode);
			}

			return toc;
		}

		public SectionBlock GetSectionBlock(int pageNumber)
		{
			if(pageNumber < 0 || pageNumber >= Formatter.Pages.Count)
			{
				throw new ArgumentOutOfRangeException();
			}

			Block block = Formatter.FindBlockById(Formatter.Pages[pageNumber].BlockId);

			if(block != null)
			{
				return (SectionBlock)block.FindUp(p => p is SectionBlock);
			}

			return null;
		}

		/// <summary>
		/// Returns xpointer by page number.
		/// </summary>
		/// <param name="pageNumber"></param>
		/// <returns></returns>
		public string GetXPointer(int pageNumber)
		{
			return Formatter.GetXPointer(pageNumber);
		}

		/// <summary>
		/// Returns xpointer by current page.
		/// </summary>
		/// <returns></returns>
		public string GetXPointer()
		{
			return Formatter.GetXPointer(CurrentPage);
		}

		public string GetXPointer(Pointer pointer)
		{
			return Formatter.GetXPointer(pointer);
		}

		public Pointer ResolveXPointer(string xpointer)
		{
			return Formatter.ResolveXPointer(xpointer);
		}

		#endregion

	}
}