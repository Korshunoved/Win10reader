using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FictionBook
{

	#region public class Element: Root of all body elements
	/// <summary>
	/// This class is the base for all FictionBook body elements, such as section, paragraph, poem, etc.
	/// </summary>
	public class Element : INotifyPropertyChanged
	{
		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private string _language;
		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private string _id;
		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private readonly ElementCollection _children;

		#region Constructors/Disposer
		/// <summary>
		/// Constructs object and creates child element collection.
		/// </summary>
		protected Element()
		{
			_children = new ElementCollection( this );
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets parent element, i.e. element, that contains this element as a child.
		/// </summary>
		public Element Parent { get; protected internal set; }

		/// <summary>
		/// Gets or sets language for element.
		/// </summary>
		public string Language
		{
			get { return _language; }
			set { SetProperty( ref _language, value, "Language" ); }
		}

		/// <summary>
		/// Gets or sets element ID.
		/// </summary>
		public string Id
		{
			get { return _id; }
			set { SetProperty( ref _id, value, "Id" ); }
		}

		/// <summary>
		/// Gets child elements collection.
		/// </summary>
		public ElementCollection Children
		{
			get { return _children; }
		}
		#endregion

		#region Events and Event Raisers
		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetProperty<T>( ref T storage, T value, string propertyName )
		{
			if (!object.Equals( storage, value ))
			{
				storage = value;

				OnPropertyChanged( propertyName );
			}
		}

		protected virtual void OnPropertyChanged( string propertyName )
		{
			if (PropertyChanged != null)
			{
				PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
			}
		}
		#endregion

		#region Internal Methods
		internal bool ValidateChildInsertionEx( int index, Element child )
		{
			return ValidateChildInsertion( index, child );
		}

		internal bool ValidateChildRemovalEx( int index, Element child )
		{
			return ValidateChildRemoval( index, child );
		}
		#endregion

		#region Protected Methods
		protected virtual bool ValidateChildInsertion( int index, Element child )
		{
			return true;
		}

		protected virtual bool ValidateChildRemoval( int index, Element child )
		{
			return true;
		}
		#endregion

		#region Clone
		public virtual Element Clone()
		{
			Element clone = (Element) Activator.CreateInstance( GetType() );

			clone._language = _language;
			clone._id = _id;
			clone.Parent = null;

			foreach (var child in _children)
			{
				clone._children.Add( child.Clone() );
			}

			return clone;
		}
		#endregion

		#region Accessors
		public Element NearestOf<T>() where T : Element
		{
			if( Parent != null )
			{
				if( Parent is T )
				{
					return Parent;
				}

				return Parent.NearestOf<T>();
			}

			return null;
		}

		public Element Top()
		{
			if( Parent == null )
			{
				return this;
			}

			return Parent.Top();
		}

		public T Find<T>( Predicate<Element> match ) where T : Element
		{
			if( !(this is T) )
			{
				return default( T );
			}

			if( match( this ) )
			{
				return (T) this;
			}

			foreach( Element item in Children )
			{
				Element rc = item.Find<T>( match );

				if( rc != null )
				{
					return (T) rc;
				}
			}

			return default( T );
		}

		public Element Find( Predicate<Element> match )
		{
			if( match( this ) )
			{
				return this;
			}

			foreach( Element item in Children )
			{
				Element rc = item.Find( match );

				if( rc != null )
				{
					return rc;
				}
			}

			return null;
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			ToString( sb );

			return sb.ToString();
		}

		protected virtual void ToString( StringBuilder builder )
		{
			foreach( var child in _children )
			{
				child.ToString( builder );
			}
		}
		#endregion
	}
	#endregion

	#region public class ElementCollection : Collection<Element>
	public class ElementCollection : ObservableCollection<Element>
	{
		private Element Owner { get; set; }

		#region Constructors/Disposer
		/// <summary>
		/// Constructs new element collection owned by the specified element.
		/// </summary>
		/// <param name="owner">Collection owner.</param>
		internal ElementCollection( Element owner )
		{
			Owner = owner;
		}
		#endregion

		#region Public Indexers
		public IEnumerable<Element> this[Type elementType]
		{
			get
			{
				IEnumerable<Element> result =
					from item in Items
					where elementType.IsAssignableFrom( item.GetType() )
					select item;

				return result;
			}
		}
		#endregion

		#region Public Methods
		public void AddRange( IEnumerable<Element> items )
		{
			foreach (Element item in items)
			{
				Add( item );
			}
		}
		#endregion

		#region Collection<T> Overrides
		protected override void ClearItems()
		{
			if (Owner != null)
			{
				foreach (Element element in Items)
				{
					element.Parent = null;
				}
			}

			base.ClearItems();

			OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Reset ) );
		}

		public new void Add( Element item )
		{
			item.Parent = Owner;
			base.Add( item );
		}

		protected override void InsertItem( int index, Element item )
		{
			if (Owner != null)
			{
				if (!Owner.ValidateChildInsertionEx( index, item ))
				{
					throw new InvalidOperationException( string.Format( "{0} can not contain {1}", Owner.GetType().Name, item.GetType().Name ) );
				}
			}

			item.Parent = Owner;

			base.InsertItem( index, item );

			OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Add, item, index ) );
		}

		protected override void RemoveItem( int index )
		{
			Element item = Items[index];

			if (Owner != null)
			{
				if (!Owner.ValidateChildRemovalEx( index, item ))
				{
					throw new InvalidOperationException( string.Format( "{0} must contain {1}", Owner.GetType().Name, item.GetType().Name ) );
				}
			}

			item.Parent = null;

			OnCollectionChanged( new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Remove, item, index ) );
		}

	    public void RemoveItemCustom(int index)
	    {
	        if (Items.Count > index)
	        {
	            Element item = Items[index];

	            if (item != null)
	            {
	                Items.RemoveAt(index);
	                item.Parent = null;
	            }
	        }
	    }

		#endregion

		#region Clone
		public virtual ElementCollection Clone()
		{
			return Clone( null );
		}

		internal ElementCollection Clone( Element parent )
		{
			ElementCollection clone = new ElementCollection( parent );

			foreach (Element child in Items)
			{
				Element childClone = child.Clone();

				childClone.Parent = parent;
				clone.Add( childClone );
			}

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class ImageElement
	/// <summary>
	/// Represents image.
	/// </summary>
	public class ImageElement : MarkupElement
	{
		private string _type;
		private string _reference;
		private string _alternativeText;

		#region Public Properties
		/// <summary>
		/// Gets or sets image link type.
		/// </summary>
		public string Type
		{
			get { return _type; }
			set { SetProperty( ref _type, value, "Type" ); }
		}

		/// <summary>
		/// Gets or sets image link reference.
		/// </summary>
		public string Reference
		{
			get { return _reference; }
			set { SetProperty( ref _reference, value, "Reference" ); }
		}

		/// <summary>
		/// Gets or sets image link alternative text.
		/// </summary>
		public string AlternativeText
		{
			get { return _alternativeText; }
			set { SetProperty( ref _alternativeText, value, "AlternativeText" ); }
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			ImageElement clone = (ImageElement) base.Clone();

			clone._type = _type;
			clone._reference = _reference;
			clone._alternativeText = _alternativeText;

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class TitleElement
	public class TitleElement : Element
	{
		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return child.GetType() == typeof( ParagraphElement ) ||
				child.GetType() == typeof( EmptyLineElement );
		}
		#endregion

		#region Overridables
		/*
		public override string ToString()
		{
			MarkupElement content = (MarkupElement) Children.FirstOrDefault( p => p is MarkupElement );
			return content == default( MarkupElement ) ? base.ToString() : content.ToString();
		}
		*/
		#endregion

	}
	#endregion

	#region public class SubtitleElement
	public class SubtitleElement : ParagraphElement
	{
	}
	#endregion

	#region public class CiteElement
	public class CiteElement : Element
	{
		private readonly List<TextField> _textAuthors = new List<TextField>();

		#region Public Properties
		public IList<TextField> TextAuthors
		{
			get { return _textAuthors; }
		}
		#endregion

		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return child.GetType() == typeof( ParagraphElement ) ||
				child.GetType() == typeof( PoemElement ) ||
				child.GetType() == typeof( EmptyLineElement );
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			CiteElement clone = (CiteElement) base.Clone();

			foreach (var author in _textAuthors)
			{
				clone._textAuthors.Add( author );
			}

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class PoemElement
	public class PoemElement : Element
	{
		private readonly List<TextField> _textAuthors = new List<TextField>();
		private DateField _date;

		#region Public Properties
		public IList<TextField> TextAuthors
		{
			get { return _textAuthors; }
		}

		public DateField Date
		{
			get { return _date; }
			set { SetProperty( ref _date, value, "Date" ); }
		}
		#endregion

		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return child.GetType() == typeof( TitleElement ) ||
				child.GetType() == typeof( TextAuthorElement ) ||
				child.GetType() == typeof( EpigraphElement ) ||
				child.GetType() == typeof( StanzaElement );
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			PoemElement clone = (PoemElement) base.Clone();

			clone._date = _date;

			foreach (var author in _textAuthors)
			{
				clone._textAuthors.Add( author );
			}

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class StanzaElement
	public class StanzaElement : Element
	{
		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return child.GetType() == typeof( TitleElement ) ||
				child.GetType() == typeof( SubtitleElement ) ||
				child.GetType() == typeof( VerseElement );
		}
		#endregion
	}
	#endregion

	#region public class VerseElement
	public class VerseElement : ParagraphElement
	{
		#region Constructors/Disposer
		public VerseElement()
		{
		}

		public VerseElement( string text )
			: base( text )
		{
		}
		#endregion
	}
	#endregion

	#region public class TextAuthorElement
	public class TextAuthorElement : ParagraphElement
	{
		public TextAuthorElement()
		{
		}

		public TextAuthorElement( string text )
			: base( text )
		{
		}
	}
	#endregion

	#region public class EpigraphElement
	public class EpigraphElement : Element
	{
		private readonly List<TextField> _textAuthors = new List<TextField>();

		#region Public Properties
		public IList<TextField> TextAuthors
		{
			get { return _textAuthors; }
		}
		#endregion

		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return child.GetType() == typeof( ParagraphElement ) ||
				child.GetType() == typeof( TextAuthorElement ) ||
				child.GetType() == typeof( PoemElement ) ||
				child.GetType() == typeof( CiteElement ) ||
				child.GetType() == typeof( EmptyLineElement );
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			EpigraphElement clone = (EpigraphElement) base.Clone();

			foreach (var author in _textAuthors)
			{
				clone._textAuthors.Add( author );
			}

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class AnnotationElement
	public class AnnotationElement : Element
	{
		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return child.GetType() == typeof( ParagraphElement ) ||
				child.GetType() == typeof( PoemElement ) ||
				child.GetType() == typeof( CiteElement ) ||
				child.GetType() == typeof( EmptyLineElement );
		}
		#endregion
	}
	#endregion

	#region public class SectionElement
	public class SectionElement : Element
	{
		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			Type childType = child.GetType();

			if (childType != typeof( TitleElement ) &&
				childType != typeof( SectionElement ) &&
				childType != typeof( EpigraphElement ) &&
				childType != typeof( ImageElement ) &&
				childType != typeof( AnnotationElement ) &&
				childType != typeof( SectionElement ) &&
				childType != typeof( ParagraphElement ) &&
				childType != typeof( PoemElement ) &&
				childType != typeof( SubtitleElement ) &&
				childType != typeof( CiteElement ) &&
				childType != typeof( EmptyLineElement ) &&
				childType != typeof( TableElement ))
			{
				return false;
			}

			bool hasSections = false;
			bool hasMarkup = false;
			bool isSection = childType == typeof( SectionElement );

			for (int i = 0; i < Children.Count; ++i)
			{
				childType = Children[i].GetType();

				if (childType == typeof( SectionElement ))
				{
					hasSections = true;
				}
				else
				{
					if (childType == typeof( ParagraphElement ) ||
						childType == typeof( PoemElement ) ||
						childType == typeof( SubtitleElement ) ||
						childType == typeof( CiteElement ) ||
						childType == typeof( EmptyLineElement ) ||
						childType == typeof( TableElement ))
					{
						hasMarkup = true;
					}
				}
			}

			if (isSection && hasMarkup)
			{
				return false;
			}

			if (!isSection && hasSections)
			{
				return false;
			}

			return true;
		}

		/*
		public override string ToString()
		{
			TitleElement content = (TitleElement) Children.FirstOrDefault( p => p is TitleElement );
			return content == default( TitleElement ) ? base.ToString() : content.ToString();
		}
		*/
		#endregion
	}
	#endregion

	#region public class ParagraphElement
	public class ParagraphElement : MarkupElement
	{
		private string _style;

		#region Constructors/Disposer
		public ParagraphElement()
		{
		}

		public ParagraphElement( string text )
		{
			Children.Add( new TextElement( text, "paragraph" ) );
		}
		#endregion

		#region Public Properties
		public string Style
		{
			get { return _style; }
			set { SetProperty( ref _style, value, "Style" ); }
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			ParagraphElement clone = (ParagraphElement) base.Clone();

			clone._style = _style;

			return clone;
		}
		#endregion

		protected override void ToString( StringBuilder builder )
		{
			base.ToString( builder );

			builder.AppendLine();
		}
	}
	#endregion

	#region public class EmptyLineElement
	public class EmptyLineElement : Element
	{
		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return false;
		}
		#endregion

		protected override void ToString( StringBuilder builder )
		{
			base.ToString( builder );

			builder.AppendLine();
		}
	}
	#endregion

	#region public class TextElement
	public class TextElement : MarkupElement
	{
		private string _text;

	    private string _type;

		#region Constructors/Disposer
		public TextElement()
			: this( string.Empty, string.Empty)
		{
		}

		public TextElement( string text, string type )
		{
			_text = text;
		    _type = type;
		}
		#endregion

		#region Public Properties
		public string Text
		{
			get { return _text; }
			set { SetProperty( ref _text, value, "Text" ); }
		}

	    public string Type
	    {
	        get { return _type; }
	        set { SetProperty(ref _type, value, "Type"); }
	    }

	    #endregion

		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return false;
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			return _text;
		}

		protected override void ToString( StringBuilder builder )
		{
			builder.Append( _text );
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			TextElement clone = (TextElement) base.Clone();

			clone._text = _text;
		    clone._type = _type;

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class MarkupElement
	public class MarkupElement : Element
	{
		#region Element Overrides
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			// Markup element can only contain other markup elements. Links can not be nested.

			if (!( child is MarkupElement ))
			{
				return false;
			}

			if (!( child is LinkElement ))
			{
				return true;
			}

			MarkupElement parent = this;

			while (parent != null)
			{
				if (parent is LinkElement)
				{
					return false;
				}

				parent = parent.Parent as MarkupElement;
			}

			return true;
		}
		#endregion
	}
	#endregion

	#region public class StrongElement
	public class StrongElement : MarkupElement
	{
		#region Constructors/Disposer
		public StrongElement()
		{
		}

		public StrongElement( string text )
		{
			Children.Add( new TextElement( text, "strong" ) );
		}
		#endregion
	}
	#endregion

	#region public class EmphasisElement
	public class EmphasisElement : MarkupElement
	{
		#region Constructors/Disposer
		public EmphasisElement()
		{
		}

		public EmphasisElement( string text )
		{
			Children.Add( new TextElement( text, "emphasis" ) );
		}
		#endregion
	}
	#endregion

	#region public class StrikethroughElement
	public class StrikethroughElement : MarkupElement
	{
		public StrikethroughElement() { }
		public StrikethroughElement( string text ) { Children.Add( new TextElement( text, "Strikethrough") ); }
	}
	#endregion

	#region public class SubElement
	public class SubElement : MarkupElement
	{
		#region Constructors/Disposer
		public SubElement()
		{
		}

		public SubElement( string text )
		{
			Children.Add( new TextElement( text, "sub" ) );
		}
		#endregion
	}
	#endregion

	#region public class SupElement
	public class SupElement : MarkupElement
	{
		#region Constructors/Disposer
		public SupElement()
		{
		}

		public SupElement( string text )
		{
			Children.Add( new TextElement( text,  "sup" ) );
		}
		#endregion
	}
	#endregion

	#region public class LinkElement
	public class LinkElement : MarkupElement
	{
		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private string _linkType;
		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private string _linkReference;
		[DebuggerBrowsable( DebuggerBrowsableState.Never )]
		private string _type;

		#region Constructors/Disposer
		public LinkElement()
		{
		}

		public LinkElement( string linkReference, string text )
		{
			_linkReference = linkReference;

			Children.Add( new TextElement( text, "link" ) );
		}
		#endregion

		#region Public Properties
		public string LinkType
		{
			get { return _linkType; }
			set { SetProperty( ref _linkType, value, "LinkType" ); }
		}

		public string LinkReference
		{
			get { return _linkReference; }
			set { SetProperty( ref _linkReference, value, "LinkReference" ); }
		}

		public string IdReference
		{
			get
			{
				if (string.IsNullOrEmpty( _linkReference ) || _linkReference.Trim().Length < 2)
				{
					return null;
				}
                if (_linkReference.IndexOf("#") == 0) return _linkReference.Substring(1);
                return _linkReference;
			}
		}

		public string Type
		{
			get { return _type; }
			set { SetProperty( ref _type, value, "Type" ); }
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			LinkElement clone = (LinkElement) base.Clone();

			clone._linkType = _linkType;
			clone._linkReference = _linkReference;
			clone._type = _type;

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class StyleElement
	public class StyleElement : MarkupElement
	{
		private string _name;

		#region Public Properties
		public string Name
		{
			get { return _name; }
			set { SetProperty( ref _name, value, "Name" ); }
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			StyleElement clone = (StyleElement) base.Clone();

			clone._name = _name;

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class TableElement
	public class TableElement : Element
	{
	}
	#endregion

	#region public class TableRowElement
	public class TableRowElement : Element
	{
		private Alignment _alignment = Alignment.Left;

		#region Public Properties
		public Alignment Alignment
		{
			get { return _alignment; }
			set { SetProperty( ref _alignment, value, "Alignment" ); }
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			TableRowElement clone = (TableRowElement) base.Clone();

			clone._alignment = _alignment;

			return clone;
		}
		#endregion
	}
	#endregion

	#region public class TableCellElement
	public class TableCellElement : ParagraphElement
	{
		#region Constructors/Disposer
		public TableCellElement()
		{
		}

		public TableCellElement( string text )
			: base( text )
		{
		}
		#endregion
	}
	#endregion

	#region public enum Alignment
	public enum Alignment
	{
		Left,
		Right,
		Center
	}
	#endregion

	#region public class TableOfContentElement
	public class TableOfContentElement : Element
	{
		private string _target;

		#region Public Properties
		public string Target
		{
			get { return _target; }
			set { SetProperty( ref _target, value, "Target" ); }
		}
		#endregion

		#region Clone
		public override Element Clone()
		{
			TableOfContentElement clone = (TableOfContentElement) base.Clone();

			clone._target = _target;

			return clone;
		}
		#endregion

		#region ValidateChildInsertion
		protected override bool ValidateChildInsertion( int index, Element child )
		{
			return ( child is TableOfContentElement ) || ( child is TitleElement );
		}
		#endregion
	}
	#endregion

}
