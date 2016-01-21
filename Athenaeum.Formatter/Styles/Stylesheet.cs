using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Athenaeum.Styles
{

	public static class KnownStylesheets
	{
		public const string Text = "Text";
		public const string Notes = "Notes";
		public const string Annotation = "Annotation";
		public const string Toc = "Table Of Content";
		public const string Description = "Description";
	}

	public class Stylesheet
	{
		public string Name { get; private set; }
		public IDictionary<string, Style> Styles { get; private set; }

		#region Constructors/Disposer
		public Stylesheet()
			: this ( null )
		{
		}

		public Stylesheet( string name )
		{
			Styles = new Dictionary<string, Style> ();
			Name = name;
		}
		#endregion

		#region Public Indexers
		public Style this[string name]
		{
			get { return (Style) Styles[name]; }
		}
		#endregion

		#region Public Properties
		/*public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }*/

		/*public ICollection<Style> Styles
		{
			get { return Styles.Values; }
		}*/

		/*public bool IsModified
		{
			get
			{
				foreach (Style style in m_styles.Values)
					if (style.IsModified)
						return true;

				return false;
			}
			set
			{
				foreach (Style style in m_styles.Values)
					style.IsModified = value;
			}
		}*/
		#endregion

		#region Public Methods
		public void Add( Style style )
		{
			Styles[style.Name] = style;
		}

		public Stylesheet Clone()
		{
			Stylesheet clone = new Stylesheet ( Name );

			foreach (Style style in Styles.Values)
				clone.Add ( style.Clone () );

			return clone;
		}
		#endregion

		#region Save/Restore
		/*public XmlElement SaveToXml(XmlDocument document)
        {
            XmlElement element = document.CreateElement("Stylesheet");

            element.SetAttribute("Name", m_name);

            foreach (Style style in m_styles.Values)
                element.AppendChild(style.SaveToXml(document));

            return element;
        }

        public void RestoreFromXml(XmlElement element)
        {
            m_styles.Clear();

            m_name = element.GetAttribute("Name");

            foreach (XmlNode node in element.ChildNodes)
            {
                if (node is XmlElement && node.Name == "Style")
                {
                    Style style = new Style();

                    style.RestoreFromXml((XmlElement)node);

                    m_styles[style.Name] = style;
                }
            }
        }*/
		#endregion

		public static ICollection<Stylesheet> Load( Stream input )
		{
			List<Stylesheet> buffer = new List<Stylesheet> ();

			using (XmlReader reader = XmlReader.Create ( input ))
			{

				while (!reader.EOF)
				{
					if (!reader.Read ())
						break;

					if (reader.NodeType == XmlNodeType.Element)
						switch (reader.Name)
						{
							case "Stylesheet":
								buffer.Add ( Stylesheet.Load ( reader ) );
								break;
						}
				}
			}

			return buffer;
		}

		//public static ICollection<Stylesheet> LoadDefaultStyles()
		//{
			//using (Stream input = typeof(Stylesheet).Assembly.GetManifestResourceStream("Athenaeum.Athenaeum.Styles.DefaultStyles.xml"))
			//using (Stream input = Application.GetResourceStream ( new Uri ( "Styles/DefaultStyles.xml", UriKind.RelativeOrAbsolute ) ).Stream)
			//	return load ( input );
		//}

		internal static Stylesheet Load( XmlReader reader )
		{
			Stylesheet stylesheet = new Stylesheet ( reader.GetAttribute ( "Name" ) );
			int depth = reader.Depth;
			while (!reader.EOF)
			{
				if (!reader.Read () || depth >= reader.Depth)
					break;

				if (reader.NodeType == XmlNodeType.Element)
					switch (reader.Name)
					{
						case "Style":
							Style style = Style.Load ( reader );
							stylesheet.Styles.Add ( style.Name, style );
							break;
					}
			}
			return stylesheet;
		}

	}
}
