using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Athenaeum.Styles;

namespace Athenaeum.Formatter
{
	public class ParagraphBlock : Block, IDrawable
	{
		//private readonly static Hyphenator hyphenator = new Hyphenator();
		private Athenaeum.Styles.Style _paragraphStyle;

		#region Constructors/Disposer
		public ParagraphBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
			: base(formatter, element, id)
		{
			Lines = new List<TextLine>();
			Markup = new List<TextLineWord>();
		}
		#endregion

		#region Public Properties
		public new FictionBook.ParagraphElement Element
		{
			get { return (FictionBook.ParagraphElement)base.Element; }
		}

		public IList<TextLine> Lines { get; private set; }
		public IList<TextLineWord> Markup { get; private set; }
		#endregion

		#region UpdateStyles
		public override void UpdateStyles()
		{
			Markup.Clear();

			BuildParagraphMarkup(Element, null, 0);
		}
		#endregion

		#region BuildParagraphMarkup
		private int BuildParagraphMarkup(FictionBook.Element element, FictionBook.LinkElement linkTarget, int offset)
		{            
			_paragraphStyle = Formatter.GetCurrentStyle();
			PaintObject = Formatter.GetPaintObject(_paragraphStyle);

			Style style = null;
		    if (element != null)
		    {
		        if (element is FictionBook.TextElement)
		        {
		            offset = BuildTextMarkup((FictionBook.TextElement) element, linkTarget, offset);
		        }
		        else if (element is FictionBook.SubElement)
		        {
		            style = Formatter.Stylesheet[KnownStyles.Sub];
		        }
		        else if (element is FictionBook.StrikethroughElement)
		        {
		            style = Formatter.Stylesheet[KnownStyles.Strikethrough];
		        }
		        else if (element is FictionBook.SupElement)
		        {
		            style = Formatter.Stylesheet[KnownStyles.Sup];
		        }
		        else if (element is FictionBook.StrongElement)
		        {
		            style = Formatter.Stylesheet[KnownStyles.Strong];
		        }
		        else if (element is FictionBook.EmphasisElement)
		        {
		            style = Formatter.Stylesheet[KnownStyles.Emphasis];
		        }
		        else if (element is FictionBook.LinkElement)
		        {
		            style = Formatter.Stylesheet[KnownStyles.Link];
		            linkTarget = (FictionBook.LinkElement) element;
		        }

		        if (style != null)
		        {
		            Formatter.PushStyle(style);
		        }
                
		        foreach (var childElement in element.Children)
		        {
		            offset = BuildParagraphMarkup(childElement, linkTarget, offset);
		        }
		    }
		    if(style != null)
			{
				Formatter.PopStyle();
			}

			return offset;
		}

		private int BuildTextMarkup(FictionBook.TextElement element, FictionBook.LinkElement linkTarget, int offset)
		{
			PaintObject paintObject = PaintObject;
			int markupTextLength = element.Text.Length;
			int length = 0, position = -1;
			for(int pos = 0; pos < markupTextLength; pos++)
			{
				char c = element.Text[pos];

				if(c != (char)160 && char.IsWhiteSpace(c))
				{
					if(length > 0)
					{
						var size = paintObject.Font.MeasureString(element.Text, position, length);
						var word = new TextLineWord(Markup.Count, size.Width, size.Height, paintObject, linkTarget);
						word.Offset = offset;
						word.Parent = this;
						word.Markup = element;
						word.Position = position;
						word.Length = length;
						Markup.Add(word);
						length = 0;
						position = -1;
					}
				}
				else
				{
					length++;
					if(position < 0)
						position = pos;
				}
			}
			if(length > 0)
			{
				var size = paintObject.Font.MeasureString(element.Text, position, length);
				var word = new TextLineWord(Markup.Count, size.Width, size.Height, paintObject, linkTarget);
				word.Offset = offset;
				word.Parent = this;
				word.Markup = element;
				word.Position = position;
				word.Length = length;
				Markup.Add(word);
			}
			return offset + markupTextLength;
		}

		/*private void AddMarkup( StringBuilder wordBuilder, PaintObject paintObject, FictionBook.LinkElement linkTarget )
		{
			if (wordBuilder.Length > 0)
			{
				var buffer = wordBuilder.ToString();
				var size = paintObject.Font.MeasureString( buffer );
				var word = new TextLineWord( Markup.Count, buffer, (int) size.Width, (int) size.Height, paintObject, linkTarget );
				word.Parent = this;
				Markup.Add( word );

				wordBuilder.Clear();
			}
		}*/
		#endregion

		#region Formatting

		private bool TestHyphPermit(string s, int count)
		{
			if(string.IsNullOrEmpty(s))
				return false;
			int pos = 0;
			for(; pos < s.Length; pos++)
			{
				char c = s[pos];
				if(char.IsLetterOrDigit(c))
					break;
			}
			int rc = 0;
			for(; pos < s.Length; pos++)
			{
				char c = s[pos];
				if(!char.IsLetterOrDigit(c))
					break;
				rc++;
				if(rc > count)
					return true;
			}
			return false;
		}

		private bool TestHyphPermit(string s, int position, int length, int count)
		{
			if(string.IsNullOrEmpty(s))
				return false;
			int pos = position, l = position + length;
			for(; pos < l; pos++)
			{
				char c = s[pos];
				if(char.IsLetterOrDigit(c))
					break;
			}
			int rc = 0;
			for(; pos < l; pos++)
			{
				char c = s[pos];
				if(!char.IsLetterOrDigit(c))
					break;
				rc++;
				if(rc > count)
					return true;
			}
			return false;
		}

		const int hyphOffset = 6;

		public override int Format(XRect bounds)
		{

			PaintObject paintObject = PaintObject;
			int lineHeight = 0;
			bool isFirstLine = true;
			TextLine line = new TextLine(paintObject);
			int markupLength = Markup.Count;
            
			//PaintingContext.Settings.ApplySetting( _paragraphStyle );

			Lines.Clear();
			Bounds = XRect.Empty;
			double indentScale = bounds.Width / 50.0;
			int firstLineIndent = _paragraphStyle.FirstLineIndent != 0 ? (int)(PaintObject.Font.Hyphen.Width * 4) : 0;
			int indentedLeft = (int)bounds.Left + (int)(_paragraphStyle.LeftIndent * indentScale);
			int indentedWidth = (int)bounds.Width - (int)((_paragraphStyle.LeftIndent + _paragraphStyle.RightIndent) * indentScale);
			int x = indentedLeft + firstLineIndent;
			int y = (int)bounds.Top;
			int availableWidth = indentedWidth - firstLineIndent;
			bool spaceBefore = false;

			int bt = ((int)bounds.Bottom / (int)bounds.Height) * (int)bounds.Height;

			if(y + PaintObject.Font.Height > (int)bt)
			{
				y += bt - y;
			}

			Formatter.OnBlockFormatting(y);

			for(int i = 0; i < markupLength; ++i)
			{
				TextLineWord word = Markup[i];

				lineHeight = Math.Max(lineHeight, (int)word.Bounds.Height);

				if(isFirstLine)
				{
					line.Type |= TextLineType.First;
				}

				if(!spaceBefore && line.IsFirst && PaintObject.Style.SpacingBefore > 0)
				{
					if(y % bounds.Height != 0)
					{
						//y += PaintObject.Style.SpacingBefore;
                        y += (int)PaintingContext.Settings.ParagraphSpacing;
					}

					spaceBefore = true;
				}

				if(word.Bounds.Width > availableWidth)
				{
                    if (PaintingContext.Settings.Hyphenate && word.Length<20)
					{
						if(word.Hyph == null)
						{
							word.Hyph = Hyphenator.Hyphenate(word.Markup.Text, word.Position, word.Length);
						}

						for(int j = word.Hyph.Length - 1; j >= 0; j--)
						{
							if(word.Hyph[j])
							{

								if(TestHyphPermit(word.Markup.Text, word.Position, j, 1))
								{

									int l = word.Length;

									int subWordWidth = (int)PaintObject.Font.MeasureString(word.Markup.Text, word.Position, j).Width + PaintObject.Font.Hyphen.Width + PaintObject.Font.Hyphen.Width / hyphOffset;

									if(subWordWidth <= availableWidth)
									{
										TextLineWord subWord = new TextLineWord(word.Index, word.Subindex + 1, subWordWidth, (int)word.Bounds.Height, word.PaintObject, word.LinkTarget);
										subWord.Parent = word.Parent;
										subWord.Markup = word.Markup;
										subWord.Position = word.Position;
										subWord.Length = j;
										if(subWord.Markup.Text[subWord.Position + subWord.Length - 1] != '-')
											subWord.Hyphenated = true;

										if(TestHyphPermit(word.Markup.Text, subWord.Position + subWord.Length, l - subWord.Length, 1))
										{
											AddWord(line, subWord, ref x, ref availableWidth, ref lineHeight);
											subWordWidth = (int)PaintObject.Font.MeasureString(word.Markup.Text, subWord.Position + subWord.Length, l - subWord.Length).Width;
											word = new TextLineWord(subWord.Index, subWord.Subindex + 1, subWordWidth, (int)word.Bounds.Height, word.PaintObject, word.LinkTarget);
											word.Parent = subWord.Parent;
											word.Markup = subWord.Markup;
											word.Position = subWord.Position + subWord.Length;
											word.Length = l - subWord.Length;
											break;
										}
									}
								}
							}
						}
					}

					if(!Formatter.RectangleFitsOnPage(new XRect(0, y, bounds.Width, lineHeight)))
					{
						y = Formatter.AdjustToPage(y);
					}

					if(line.Items.Count > 0)
					{
						AlignTextLine(line, indentedLeft, y, indentedWidth, firstLineIndent);
						Lines.Add(line);
						y += (int)((1 + (int)PaintingContext.Settings.LineSpacing / 10.0) * line.Bounds.Height);
					}

					isFirstLine = false;
					x = indentedLeft;
					availableWidth = indentedWidth;

					lineHeight = 0;

					line = new TextLine(paintObject);

				}

				AddWord(line, word, ref x, ref availableWidth, ref lineHeight);

			}

			if(line.Items.Count > 0)
			{
				// Добавляем последнюю (висящую) строку
				if(!Formatter.RectangleFitsOnPage(new XRect(0, y, bounds.Width, lineHeight)))
				{
					y = Formatter.AdjustToPage(y);
				}

				line.Type |= TextLineType.Last;

				if(line.Items.Count > 0)
				{
					AlignTextLine(line, indentedLeft, y, indentedWidth, firstLineIndent);
					Lines.Add(line);
					y += (int)((1 + (int)PaintingContext.Settings.LineSpacing / 10.0) * line.Bounds.Height);
				}
			}

			Bounds = new XRect(bounds.Left, bounds.Top, bounds.Width, y - bounds.Top);

			if(Parent != null)
			{
				Parent.SetCommonHeight((int)Bounds.Y);
			}

			Formatter.OnBlockFormatted(this);

			return y - (int)bounds.Top;
		}

		private static void AddWord(TextLine line, TextLineWord word, ref int x, ref int availableWidth, ref int lineHeight)
		{
			word.Bounds.X = x;

			line.Items.Add(word);

			x += (int)word.Bounds.Width;
			availableWidth -= (int)word.Bounds.Width;

			x += (int)line.PaintObject.Font.Space.Width;
			availableWidth -= (int)line.PaintObject.Font.Space.Width;

			lineHeight = Math.Max(lineHeight, (int)word.Bounds.Height);
		}

		private void AlignTextLine(TextLine line, int left, int top, int width, int firstLineIndent)
		{
			line.Bounds = new XRect(left, top, width, AlignTextLine(line, left, top, width, _paragraphStyle.Alignment));
		}

		private int AlignTextLine(TextLine line, int left, int top, int width, HAlignment alignment)
		{
			int height = 0;
			int wordCount = line.Items.Count;
			int spaceWidth = (int)line.PaintObject.Font.Space.Width;

			line.Bounds.X = left;
			line.Bounds.Y = top;

			if(wordCount > 0)
			{
				if(!line.IsLast && wordCount > 1 && alignment == HAlignment.Justify)
				{
					int spacesCount = wordCount - 1;
					int sumWidth = line.GetWordsWidth();
					int residue = width - (line.IsFirst ? ((int)line.Items[0].Bounds.X - left) : 0) - sumWidth + 1;
					int n = residue / spacesCount;
					int m = residue % spacesCount;

					if(line.IsFirst)
						left = (int)line.Items[0].Bounds.X;

					for(int i = 0; i < wordCount; ++i)
					{
						TextLineWord word = line.Items[i];
						word.Bounds.Y = top;
						if(i > 0)
						{
							left += n;
							if(i < m)
								left++;
						}
						word.Bounds.X = left;
						left += (int)word.Bounds.Width;
						height = Math.Max(height, (int)word.Bounds.Height);
					}
				}
				else
				{

					if(alignment == HAlignment.Left || alignment == HAlignment.Justify)
					{
						if(line.IsFirst)
							left = (int)line.Items[0].Bounds.X;
					}
					else if(alignment == HAlignment.Center)
					{
						int sumWidth = line.GetWordsWidth();
						left += (width - (int)sumWidth - wordCount * spaceWidth) / 2;
					}
					else if(alignment == HAlignment.Right)
					{
						int sumWidth = line.GetWordsWidth();
						left += (width - (int)sumWidth - wordCount * spaceWidth);
					}

					for(int i = 0; i < wordCount; ++i)
					{
						TextLineWord word = line.Items[i];
						word.Bounds.Y = top;
						if(i > 0)
							left += spaceWidth;
						word.Bounds.X = left;
						left += (int)word.Bounds.Width;
						height = Math.Max(height, (int)word.Bounds.Height);
					}

				}
			}

			return height == 0 ? (int)line.PaintObject.Font.Space.Height : height;
		}
		#endregion

		#region Painting
		public override void Paint(PaintingContext context)
		{
			foreach(TextLine line in Lines)
			{
				if(line.Bounds.Intersected(context.Bounds))
				{
					if(line.Bounds.Top < context.Bounds.Top || line.Bounds.Bottom > context.Bounds.Bottom)
					{
						continue;
					}

					foreach(TextLineWord item in line.Items)
					{
						if(context.Pointer == null)
						{
							context.Pointer = new ParagraphPointer(Id, item.Index);
						}

						TextLineWord word = (TextLineWord)item;
						XPoint point = new XPoint(word.Bounds.X - context.Bounds.X, word.Bounds.Y - context.Bounds.Y);
						XColor color = word.HasLink ? ColorsHelper.Link : ColorsHelper.Ink;

						//word.PaintObject.Font.DrawString( context.Buffer, point, System.Windows.Media.Color.FromArgb( color.A, color.R, color.G, color.B ), word.Markup.Text, word.Position, word.Length );
						word.PaintObject.Font.DrawString(context.Target, point, color, word.Markup.Text, word.Position, word.Length);

						if(word.PaintObject.Style.Strikethrough == Flag.On)
						{
							int h = (int)Math.Max(1, word.Bounds.Height / 15.0);
							int y = point.Y + (int)((word.Bounds.Height - h) * 0.67);
							context.Context.FillRectangle(context.Target, new XRect(point.X, y, word.Bounds.Width, (int)(h + 0.5)), color);
							//context.Buffer.FillRectangle( (int) point.X, (int) y, (int) ( point.X + word.Bounds.Width ), (int) ( h + 0.5 ), System.Windows.Media.Color.FromArgb( color.A, color.R, color.G, color.B ) );
						}

						if(word.Hyphenated)
						{
							point.X += word.Bounds.Width - PaintObject.Font.Hyphen.Width + PaintObject.Font.Hyphen.Width / hyphOffset;
							word.PaintObject.Font.DrawString(context.Target, point, color, /*"-"*/word.PaintObject.Font.HyphenString);
						}
					}
				}
			}
		}
		#endregion

		#region Overrides
		public override void Offset(int dx, int dy)
		{
			base.Offset(dx, dy);

			foreach(TextLine line in Lines)
				line.Offset(dx, dy);
		}

		protected override Pointer CreatePointer()
		{
			return new ParagraphPointer();
		}

		protected override Pointer GetPointer(XPoint point, XRect bounds)
		{
			ParagraphPointer pointer = (ParagraphPointer)base.GetPointer(point, bounds);

			foreach(TextLine line in Lines)
			{
				if(line.Bounds.Intersected(bounds))
				{
					if(line.Bounds.Top < bounds.Top || line.Bounds.Bottom > bounds.Bottom)
					{
						continue;
					}

					foreach(TextLineWord word in line.Items)
					{
						pointer.ItemId = word.Index;

						return pointer;
					}
				}
			}

			return pointer;
		}

		protected internal override void ClearFormatting()
		{
			base.ClearFormatting();

			foreach(Bounded item in Markup)
			{
				item.ClearFormatting();
			}
		}

		public override string ToString()
		{
			StringBuilder buffer = new StringBuilder();
			foreach(TextLineWord word in Markup)
			{
				if(buffer.Length != 0)
					buffer.Append(" ");
				buffer.Append(word);
			}
			return buffer.ToString();
		}

		#endregion

		#region IDrawable implementation
		int IDrawable.Y
		{
			get { return (int)Bounds.Y; }
		}
		public Pointer GetPointer()
		{
			return new Pointer(Id);
		}
		#endregion

		internal int? FindMarkupIndexByNotZeroBasedPosition(int position)
		{
			if(position == 1)
				return Markup.Count > 0 ? (int?)0 : null;
			position--;
			for(int index = 0; index < Markup.Count; index++)
			{
				TextLineWord word = Markup[index];
				if(position < (word.Absolute + word.Length))
					return index;
			}
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="wordIndex"></param>
		/// <returns>zero based letter position or null</returns>
		internal int? GetMarkupPosition(int wordIndex)
		{
			return wordIndex < Markup.Count ? Markup[wordIndex].Absolute : (int?)null;
		}

	}

}
