using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace LitRes.Models
{
	public partial class Bookmark
	{
		public partial class Extract
		{
			public partial class ExtractTextElement
			{
				public string Text { get; set; }

				public XmlSchema GetSchema()
				{
					return null;
				}

				public void ReadXml( XmlReader reader )
				{
					Text = reader.ReadInnerXml();
				}

				public void WriteXml( XmlWriter writer )
				{
					if( !string.IsNullOrEmpty( Text ) )
					{
						writer.WriteString( Text );
					}
				}
			}
		}
	}
}
