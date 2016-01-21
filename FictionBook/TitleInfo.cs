using System;
using System.Text;
using System.Collections.Generic;


namespace FictionBook
{
	public class TitleInfo
	{
		#region Constructor
		public TitleInfo()
		{
			Genres = new List<Genre>();
			Authors = new List<Author>();
			Translators = new List<Author>();
			Sequences = new List<Sequence>();
		}
		#endregion

		public AnnotationElement Annotation { get; set; }
		public IList<Author> Authors { get; private set; }
		public Coverpage Coverpage { get; set; }
		public DateField Date { get; set; }
		public IList<Genre> Genres { get; private set; }
		public TextField Keywords { get; set; }
		public string Language { get; set; }
		public IList<Sequence> Sequences { get; private set; }
		public string SourceLanguage { get; set; }
		public TextField Title { get; set; }
		public IList<Author> Translators { get; private set; }

		public override bool Equals( object obj )
		{
			var other = obj as TitleInfo;

			if( other != null )
			{
				if( object.Equals( other.Title, Title ) )
				{
					if( other.Authors.Count == Authors.Count )
					{
						for( var index = 0; index < Authors.Count; index++ )
						{
							if( !object.Equals( other.Authors[index], Authors[index] ) )
							{
								return false;
							}
						}

						return true;
					}
				}
			}

			return false;
		}

		public override int GetHashCode()
		{
			var rc = Title != null ? Title.GetHashCode() : 0;

			foreach( Author author in Authors )
			{
				rc |= author.GetHashCode();
			}

			return rc;
		}

		public override string ToString()
		{
			StringBuilder buffer = new StringBuilder();
			if (Title != null)
				buffer.Append( Title );
			return buffer.ToString();
		}
	}
}
