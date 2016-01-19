using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

#if NET45
using System.Web;
#endif

namespace LitRes.Models
{
	public partial class Book
	{
		public partial class Annotation
		{
			//private static Regex reg = new Regex( "<p\\b[^>]*>(.*?)</p>" );
			private static Regex reg = new Regex( "<p>(.*?)</p>", RegexOptions.Singleline );

			public XmlSchema GetSchema()
			{
				return null;
			}

			public void ReadXml(XmlReader reader)
			{
				try
				{
					string inner = reader.ReadInnerXml();

					string text = string.Empty;

					var matches = reg.Matches( inner );

					if( matches.Count > 0 )
					{
						text = string.Join( "\n", matches.Cast<Match>().Select( x => x.Groups[1].Value ) );
					}

					Text = HttpUtility.HtmlDecode( text );
				}
				catch (Exception)
				{
					
				}
			}

			public void WriteXml(XmlWriter writer)
			{
				string write = string.Format( "<p>{0}</p>", HttpUtility.HtmlEncode( Text ) );
				writer.WriteRaw( write );
			}
		}
	}
}
