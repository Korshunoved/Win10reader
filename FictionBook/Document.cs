using System;
using System.Collections.Generic;

namespace FictionBook
{
	/// <summary>
	/// Represent FictionBook document - top level object to deal with the
	/// book.
	/// </summary>
	public class Document : Element
	{
		public DocumentLoadOptions LoadOptions { get; internal set; }
		public Description Description { get; internal set; }

		public IList<DocumentStylesheet> Stylesheets { get; private set; }
		public ElementCollection Bodies { get { return Children; } }
		public BinaryCollection Binaries { get; private set; }

        public ElementCollection AnnotationBody { get; set; }

		#region Constructors/Disposer
		public Document()
		{
			this.Stylesheets = new List<DocumentStylesheet>();
			this.Binaries = new BinaryCollection();
		}
		#endregion

		#region Properties
		public string Title { get { return ToString(); } }
		#endregion

		public override int GetHashCode()
		{
			return Description != null ? Description.GetHashCode() : base.GetHashCode();
		}

		public override string ToString()
		{
			return Description != null && Description.TitleInfo != null && Description.TitleInfo.Title != null ? Description.TitleInfo.Title.ToString() : base.ToString();
		}
	}

	[Flags]
	public enum DocumentLoadOptions
	{
		Nothing = 0x0,
		Description = 0x1,
		Body = 0x2,
		Binary = 0x4,
		Stylesheet = 0x8,
		
		Content = Body | Binary | Stylesheet,
		Complete = Description | Content
	}
}
