using System.Xml;
using System.Xml.Schema;

namespace LitRes.Models
{
	public partial class Hidden
	{
		public XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			//goto inner hidden
			reader.Read();
		    Text = reader.ReadInnerXml();
			reader.Read();
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteString(Text);
		}
	}	 
}
