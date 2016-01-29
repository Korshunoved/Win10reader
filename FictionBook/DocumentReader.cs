using System;
using System.IO;
using System.Xml;

namespace FictionBook
{
	public class DocumentReader : IDisposable
	{
		private XmlReader _reader;

		#region Constants
		public static class Constants
		{
			public static class Ns
			{
				public const string FB = "http://www.gribuser.ru/xml/fictionbook/2.0";
				public const string XLink = "http://www.w3.org/1999/xlink";
				public const string Lang = "http://www.w3.org/XML/1998/namespace";
				public const string Genres = "http://www.gribuser.ru/xml/fictionbook/2.0/genres";
			}
		}
		#endregion

		#region Constructors/Disposer
		public DocumentReader(Stream stream)
		{
			if(stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			if(!stream.CanRead)
			{
				throw new ArgumentException("Unable to read from provided stream.", "stream");
			}

			_reader = XmlReader.Create(stream);
		}

		public DocumentReader(XmlReader reader)
		{
			if(reader == null)
			{
				throw new ArgumentNullException("reader");
			}

			_reader = reader;
		}

		~DocumentReader()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(_reader != null)
				{
					_reader.Dispose();
					_reader = null;
				}
			}
		}
		#endregion

		/// <summary>
		/// обходит дерево нод с текущей позиции.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="action"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		private T ProcessNodes<T>(Func<T, bool> action, T obj = null) where T : class
		{
			if(obj == null)
				obj = Activator.CreateInstance<T>();
			int depth = _reader.Depth;
			while(!_reader.EOF && (_reader.NodeType != XmlNodeType.EndElement || _reader.NodeType == XmlNodeType.EndElement && depth < _reader.Depth))
			{
				if(!_reader.Read())
					break;
				if(!action(obj))
					return obj;
			}
			return obj;
		}

		public Document ReadDocument(DocumentLoadOptions loadOptions = DocumentLoadOptions.Complete)
		{
			Document document = new Document() { LoadOptions = loadOptions };

			return ProcessNodes<Document>(delegate(Document b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "stylesheet":
							if((loadOptions & DocumentLoadOptions.Stylesheet) != DocumentLoadOptions.Nothing)
							{
								document.Stylesheets.Add(ParseStylesheet());
							}
							else
							{
								_reader.Skip();
							}

							break;

						case "description":
							if((loadOptions & DocumentLoadOptions.Description) != DocumentLoadOptions.Nothing)
							{
								document.Description = ReadDescription();

								if(loadOptions == DocumentLoadOptions.Description)
								{
									return false;
								}
							}
							else
							{
								_reader.Skip();
							}

							break;

						case "body":
					        if ((loadOptions & DocumentLoadOptions.Body) != DocumentLoadOptions.Nothing) //&& document.Bodies.Count == 0)
							{
								document.Bodies.Add(ParseBody());
							}
							else
							{
								_reader.Skip();
							}

							break;

						case "binary":
							if((loadOptions & DocumentLoadOptions.Binary) != DocumentLoadOptions.Nothing)
							{
								document.Binaries.Add(ParseBinary());
							}
							else
							{
								_reader.Skip();
							}

							break;
					}
				}
				return true;
			}, document);

		}

		#region Description (Title-Info, Document-Info, Publish-Info, Custom-Info)
		public Description ReadDescription()
		{
			return ProcessNodes<Description>(delegate(Description description)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "title-info":
							description.TitleInfo = ParseTitleInfo();
							break;

						case "document-info":
							description.DocumentInfo = ParseDocumentInfo();
							break;

						case "publish-info":
							description.PublishInfo = ParsePublishInfo();
							break;

						case "custom-info":
							description.CustomInfos.Add(ParseCustomInfo());
							break;
					}
				}
				return true;
			});
		}

		private TitleInfo ParseTitleInfo()
		{
			return ProcessNodes<TitleInfo>(delegate(TitleInfo info)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "genre":
							info.Genres.Add(ParseGenre());
							break;

						case "author":
							info.Authors.Add(ParseAuthor());
							break;

						case "book-title":
							info.Title = ParseTextField();
							break;

						case "annotation":
							info.Annotation = ReadAnnotationElement();
							break;

						case "keywords":
							info.Keywords = ParseTextField();
							break;

						case "date":
							info.Date = ParseDateField();
							break;

						case "coverpage":
							info.Coverpage = ParseCoverpage();
							break;

						case "lang":
							info.Language = _reader.ReadElementContentAsString();
							break;

						case "src-lang":
							info.SourceLanguage = _reader.ReadElementContentAsString();
							break;

						case "translator":
							info.Translators.Add(ParseAuthor());
							break;

						case "sequence":
							info.Sequences.Add(ParseSequence());
							break;
					}
				}
				return true;
			});
		}

		private DocumentInfo ParseDocumentInfo()
		{
			return ProcessNodes<DocumentInfo>(delegate(DocumentInfo info)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "author":
							info.Authors.Add(ParseAuthor());
							break;

						case "program-used":
							info.ProgramUsed = ParseTextField();
							break;

						case "date":
							info.Date = ParseDateField();
							break;

						case "src-url":
							info.SourceUrls.Add(_reader.ReadElementContentAsString());
							break;

						case "src-ocr":
							info.SourceOcr = ParseTextField();
							break;

						case "id":
							info.Id = _reader.ReadElementContentAsString();
							break;

						case "version":
							try
							{
								info.Version = (float)XmlConvert.ToDouble(_reader.ReadElementContentAsString());
							}
							catch
							{
							}

							break;

						case "history":
							info.History = ReadAnnotationElement();
							break;
					}
				}
				return true;
			});
		}

		private PublishInfo ParsePublishInfo()
		{

			return ProcessNodes<PublishInfo>(delegate(PublishInfo info)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "book-name":
							info.BookName = ParseTextField();
							break;

						case "publisher":
							info.Publisher = ParseTextField();
							break;

						case "city":
							info.City = ParseTextField();
							break;

						case "year":
							info.Year = XmlConvert.ToInt32(_reader.ReadElementContentAsString());
							break;

						case "isbn":
							info.ISBN = ParseTextField();
							break;

						case "sequence":
							info.Sequences.Add(ParseSequence());
							break;
					}
				}
				return true;
			});
	
		}

		private CustomInfo ParseCustomInfo()
		{
			CustomInfo info = new CustomInfo();

			ParseTextFieldCore(_reader, info);

			info.InfoType = _reader.GetAttribute("info-type");

			return info;
		}

		private Sequence ParseSequence()
		{
			Sequence sequence = new Sequence();
			sequence.Name = _reader.GetAttribute("name");

			string value = _reader.GetAttribute("number");
			sequence.Number = string.IsNullOrEmpty(value) ? 0 : XmlConvert.ToInt32(value);

			sequence.Language = _reader.GetAttribute("lang", Constants.Ns.Lang);

			return ProcessNodes<Sequence>(delegate(Sequence b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					if(_reader.Name == "sequence")
					{
						sequence.Sequences.Add(ParseSequence());
					}
				}
				return true;
			}, sequence);
		}

		private Author ParseAuthor()
		{
			return ProcessNodes<Author>(delegate(Author author)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "first-name":
							author.FirstName = ParseTextField();
							break;
						case "middle-name":
							author.MiddleName = ParseTextField();
							break;
						case "last-name":
							author.LastName = ParseTextField();
							break;
						case "nickname":
							author.NickName = ParseTextField();
							break;
						case "home-page":
							author.HomePages.Add(_reader.ReadElementContentAsString());
							break;
						case "email":
							author.Emails.Add(_reader.ReadElementContentAsString());
							break;
					}
				}
				return true;
			});
		}

		private void ParseTextFieldCore(XmlReader _reader, TextField field)
		{
			if(_reader.NodeType == XmlNodeType.Element)
			{
				field.Language = _reader.GetAttribute("lang", Constants.Ns.Lang);
				field.Text = _reader.ReadElementContentAsString();
			}
		}

		private TextField ParseTextField()
		{
			TextField field = new TextField();
			ParseTextFieldCore(_reader, field);

			return field;
		}

		private DateField ParseDateField()
		{
			DateField field = new DateField();

			ParseTextFieldCore(_reader, field);

			try
			{
				string buffer = _reader.GetAttribute("value");

				if(!string.IsNullOrEmpty(buffer))
				{
					field.Value = DateTime.Parse(buffer);
				}
			}
			catch
			{
			}

			return field;
		}

		private Genre ParseGenre()
		{
			Genre genre = new Genre(_reader.ReadElementContentAsString());
			string buffer = _reader.GetAttribute("match");

			if(!string.IsNullOrEmpty(buffer))
			{
				int match;

				int.TryParse(buffer, out match);

				genre.Match = match;
			}

			return genre;
		}

		private ImageLink ParseImageLink()
		{
			ImageLink link = new ImageLink();

			link.Type = _reader.GetAttribute("type", Constants.Ns.XLink);
			link.Reference = _reader.GetAttribute("href", Constants.Ns.XLink);
			link.AlternativeText = _reader.GetAttribute("alt");

			return link;
		}

		private Coverpage ParseCoverpage()
		{
			return ProcessNodes<Coverpage>(delegate(Coverpage coverpage)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					if(_reader.Name == "image")
					{
						coverpage.Images.Add(ParseImageLink());
					}
				}
				return true;
			});
		}
		#endregion

		#region Body
		private Body ParseBody()
		{
			Body body = new Body();

			ParseBodyElement(_reader, body);

			body.Name = _reader.GetAttribute("name");

			return ProcessNodes<Body>(delegate(Body b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "image":
							body.Children.Add(ParseImageElement());
							break;
						case "title":
							body.Children.Add(ParseTitleElement());
							break;
						case "epigraph":
							body.Children.Add(ParseEpigraphElement());
							break;
						case "section":
							body.Children.Add(ParseSectionElement());
							break;
					}
				}
				return true;
			}, body);
		}

		private TitleElement ParseTitleElement()
		{
			TitleElement title = new TitleElement();

			ParseBodyElement(_reader, title);

			return ProcessNodes<TitleElement>(delegate(TitleElement b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "p":
							title.Children.Add(ParseParagraph(_reader, typeof(ParagraphElement)));
							break;
						case "empty-line":
							title.Children.Add(new EmptyLineElement());
							break;
					}
				}
				return true;
			}, title);
		}

		private SectionElement ParseSectionElement()
		{
			SectionElement section = new SectionElement();

			ParseBodyElement(_reader, section);

			return ProcessNodes(delegate
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "title":
							section.Children.Add(ParseTitleElement());
							break;

						case "epigraph":
							section.Children.Add(ParseEpigraphElement());
							break;

						case "image":
							section.Children.Add(ParseImageElement());
							break;

						case "annotation":
							section.Children.Add(ReadAnnotationElement());
							break;

						case "section":
							section.Children.Add(ParseSectionElement());
							break;

						case "p":
							section.Children.Add(ParseParagraph(_reader, typeof(ParagraphElement)));
							break;

						case "poem":
							section.Children.Add(ParsePoemElement());
							break;

						case "cite":
							section.Children.Add(ParseCiteElement());
							break;

						case "subtitle":
							section.Children.Add(ParseParagraph(_reader, typeof(SubtitleElement)));
							break;

						case "empty-line":
							section.Children.Add(new EmptyLineElement());
							break;

						case "table":
							section.Children.Add(ParseTableElement());
							break;
					}
				}
				return true;
			}, section);
		}

		private void ParseBodyElement(XmlReader _reader, Element bodyElement)
		{
			bodyElement.Language = _reader.GetAttribute("lang", Constants.Ns.Lang);
			bodyElement.Id = _reader.GetAttribute("id");
		}

		private ParagraphElement ParseParagraph(XmlReader _reader, Type type)
		{
			return (ParagraphElement)ParseMarkupElement(_reader, type);
		}

		private MarkupElement ParseMarkupElement(XmlReader _reader, Type type)
		{
			MarkupElement markup = (MarkupElement)Activator.CreateInstance(type);

			ParseBodyElement(_reader, markup);

			if(type == typeof(StyleElement))
			{
				((StyleElement)markup).Name = _reader.GetAttribute("name");
			}
			else if(type == typeof(LinkElement))
			{
				LinkElement link = (LinkElement)markup;
				link.LinkType = _reader.GetAttribute("type", Constants.Ns.XLink);
				link.LinkReference = _reader.GetAttribute("href", Constants.Ns.XLink);
				link.Type = _reader.GetAttribute("type");
			}

			return ProcessNodes<MarkupElement>(delegate(MarkupElement b)
			{
				if(_reader.NodeType == XmlNodeType.Text)
				{
					markup.Children.Add(new TextElement(_reader.ReadContentAsString(), type.ToString()));
				}

				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "strikethrough":
							markup.Children.Add(ParseMarkupElement(_reader, typeof(StrikethroughElement)));
							break;
						case "sub":
							markup.Children.Add(ParseMarkupElement(_reader, typeof(SubElement)));
							break;
						case "sup":
							markup.Children.Add(ParseMarkupElement(_reader, typeof(SupElement)));
							break;
						case "strong":
							markup.Children.Add(ParseMarkupElement(_reader, typeof(StrongElement)));
							break;
						case "emphasis":
							markup.Children.Add(ParseMarkupElement(_reader, typeof(EmphasisElement)));
							break;
						case "image":
							markup.Children.Add(ParseImageElement());
							break;
						case "style":
							markup.Children.Add(ParseMarkupElement(_reader, typeof(StyleElement)));
							break;
						case "a":
							markup.Children.Add(ParseMarkupElement(_reader, typeof(LinkElement)));
							break;
					}
				}
				return true;
			}, markup);
		}

		public AnnotationElement ReadAnnotationElement()
		{
			AnnotationElement annotation = new AnnotationElement();

			ParseBodyElement(_reader, annotation);

			return ProcessNodes<AnnotationElement>(delegate(AnnotationElement b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "p":
							annotation.Children.Add(ParseParagraph(_reader, typeof(ParagraphElement)));
							break;

						case "poem":
							annotation.Children.Add(ParsePoemElement());
							break;

						case "cite":
							annotation.Children.Add(ParseCiteElement());
							break;

						case "empty-line":
							annotation.Children.Add(new EmptyLineElement());
							break;
					}
				}
				return true;
			}, annotation);
		}

		private CiteElement ParseCiteElement()
		{
			CiteElement cite = new CiteElement();

			ParseBodyElement(_reader, cite);

			return ProcessNodes<CiteElement>(delegate(CiteElement b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "p":
							cite.Children.Add(ParseParagraph(_reader, typeof(ParagraphElement)));
							break;

						case "poem":
							cite.Children.Add(ParsePoemElement());
							break;

						case "empty-line":
							cite.Children.Add(new EmptyLineElement());
							break;

                        case "subtitle":
                            cite.Children.Add(ParseParagraph(_reader, typeof(ParagraphElement)));
							break;
					}
				}
				return true;
			}, cite);
		}

		private PoemElement ParsePoemElement()
		{
			PoemElement poem = new PoemElement();

			ParseBodyElement(_reader, poem);

			return ProcessNodes<PoemElement>(delegate(PoemElement b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "title":
							poem.Children.Add(ParseTitleElement());
							break;

						case "epigraph":
							poem.Children.Add(ParseEpigraphElement());
							break;

						case "stanza":
							poem.Children.Add(ParseStanzaElement());
							break;

						case "text-author":
							poem.Children.Add(ParseMarkupElement(_reader, typeof(TextAuthorElement)));
							break;

						case "date":
							poem.Date = ParseDateField();
							break;
					}
				}
				return true;
			}, poem);
		}

		private StanzaElement ParseStanzaElement()
		{
			StanzaElement stanza = new StanzaElement();

			ParseBodyElement(_reader, stanza);

			return ProcessNodes<StanzaElement>(delegate(StanzaElement b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "title":
							stanza.Children.Add(ParseTitleElement());
							break;

						case "subtitle":
							stanza.Children.Add(ParseParagraph(_reader, typeof(SubtitleElement)));
							break;

						case "v":
							stanza.Children.Add(ParseParagraph(_reader, typeof(VerseElement)));
							break;
					}
				}
				return true;
			}, stanza);
		}

		private EpigraphElement ParseEpigraphElement()
		{
			EpigraphElement epigraph = new EpigraphElement();

			ParseBodyElement(_reader, epigraph);

			return ProcessNodes<EpigraphElement>(delegate(EpigraphElement b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "p":
							epigraph.Children.Add(ParseParagraph(_reader, typeof(ParagraphElement)));
							break;

						case "poem":
							epigraph.Children.Add(ParsePoemElement());
							break;

						case "cite":
							epigraph.Children.Add(ParseCiteElement());
							break;

						case "empty-line":
							epigraph.Children.Add(new EmptyLineElement());
							break;

						case "text-author":
							epigraph.Children.Add(ParseMarkupElement(_reader, typeof(TextAuthorElement)));
							break;
					}
				}
				return true;
			}, epigraph);
		}

		private ImageElement ParseImageElement()
		{
			ImageElement image = new ImageElement();

			image.Type = _reader.GetAttribute("type", Constants.Ns.XLink);
			image.Reference = _reader.GetAttribute("href", Constants.Ns.XLink);
			image.AlternativeText = _reader.GetAttribute("alt");

			return image;
		}

		private TableElement ParseTableElement()
		{
			TableElement table = new TableElement();

			ParseBodyElement(_reader, table);

			return ProcessNodes<TableElement>(delegate(TableElement b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "tr":
							table.Children.Add(ParseTableRowElement());
							break;
					}
				}
				return true;
			}, table);
		}

		private TableRowElement ParseTableRowElement()
		{
			TableRowElement tableRow = new TableRowElement();

			ParseBodyElement(_reader, tableRow);

			string value = _reader.GetAttribute("align");

			if(!string.IsNullOrEmpty(value))
			{
				tableRow.Alignment = (Alignment)Enum.Parse(typeof(Alignment), _reader.GetAttribute("align"), true);
			}

			return ProcessNodes<TableRowElement>(delegate(TableRowElement b)
			{
				if(_reader.NodeType == XmlNodeType.Element)
				{
					switch(_reader.Name)
					{
						case "td":
							tableRow.Children.Add(ParseParagraph(_reader, typeof(TableCellElement)));
							break;
					}
				}
				return true;
			}, tableRow);
		}

		#endregion

		private Binary ParseBinary()
		{
			Binary binary = new Binary();
			binary.ContentType = _reader.GetAttribute("content-type");
			binary.Id = _reader.GetAttribute("id");
			return ProcessNodes<Binary>(delegate(Binary b)
			{
				if(_reader.NodeType == XmlNodeType.Text)
					binary.Data = Convert.FromBase64String(_reader.ReadContentAsString());
				return true;
			}, binary);
		}

		private DocumentStylesheet ParseStylesheet()
		{
			DocumentStylesheet styleSheet = new DocumentStylesheet();
			styleSheet.Type = _reader.GetAttribute("type");
			return ProcessNodes<DocumentStylesheet>(delegate(DocumentStylesheet b)
			{
				if(_reader.NodeType == XmlNodeType.Text)
					styleSheet.Text = _reader.ReadContentAsString();
				return true;
			}, styleSheet);
		}
	}
}
