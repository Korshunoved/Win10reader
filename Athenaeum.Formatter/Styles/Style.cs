using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.ComponentModel;
using System.Diagnostics;

using Athenaeum.Formatter;

namespace Athenaeum.Styles
{

	public static class KnownStyles
	{
		public const string Normal = "Normal";
		public const string Strikethrough = "Strikethrough";
		public const string Sub = "Sub";
		public const string Sup = "Sup";
		public const string Strong = "Strong";
		public const string Emphasis = "Emphasis";
		public const string Link = "Link";

		public const string Section = "Section";

		public const string Title = "Title";
		public const string Subtitle = "Subtitle";

		public const string Epigraph = "Epigraph";

		public const string Cite = "Cite";

		public const string Poem = "Poem";
		public const string Stanza = "Stanza";
		public const string Verse = "Verse";

		public const string Date = "Date";
		public const string TextAuthor = "TextAuthor";
	}

	public enum Flag
	{
		Inherit,
		On,
		Off
	}

	public enum HAlignment
	{
		Inherit = 0,
		Left = 1,
		Right = 2,
		Center = 3,
		Justify = 4,
	}

	public enum VAlignment
	{
		Inherit = 0,
		Top = 1,
		Bottom = 2,
		Middle = 3,
	}

	public class Measure
	{
		private int m_value;
		private bool m_isAbsolute;

		#region Constructors/Disposer
		public Measure()
		{
			m_value = 0;
			m_isAbsolute = false;
		}

		public Measure(int value, bool isAbsolute)
		{
			m_value = value;
			m_isAbsolute = isAbsolute;
		}

		public Measure(string size)
		{
			size = size.Trim();

			if (size.Length == 0)
			{
				m_value = 0;
				m_isAbsolute = false;
			}
			else
			{
				m_isAbsolute = char.IsDigit(size, 0);
				m_value = int.Parse(size);
			}
		}
		#endregion

		#region Public Properties
		public int Value
		{
			get { return m_value; }
			set { m_value = value; }
		}

		public bool IsAbsolute
		{
			get { return m_isAbsolute; }
			set { m_isAbsolute = value; }
		}
		#endregion

		#region Public Methods
		public Measure Clone()
		{
			return new Measure(m_value, m_isAbsolute);
		}
		#endregion

		#region Object Overrides
		public override bool Equals(object obj)
		{
			if (obj == null || !(obj is Measure))
				return false;

			Measure other = (Measure)obj;

			return m_value == other.m_value && m_isAbsolute == other.m_isAbsolute;
		}

		public override int GetHashCode()
		{
			return m_value.GetHashCode() ^ m_isAbsolute.GetHashCode();
		}

		public override string ToString()
		{
			if (m_isAbsolute)
				return m_value.ToString();

			if (m_value >= 0)
				return "+" + m_value.ToString();

			return m_value.ToString();
		}
		#endregion
	}

	public class Style : INotifyPropertyChanged
	{
		public static readonly Style Strong = new Style(Flag.On, Flag.Inherit, Flag.Inherit);
		public static readonly Style Emphasis = new Style(Flag.Inherit, Flag.On, Flag.Inherit);
		public static readonly Style Link = new Style(Flag.Inherit, Flag.Inherit, Flag.Inherit);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private string m_name;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private bool m_isModified;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private string m_fontFace;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Measure m_fontSize;		// in points

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Flag m_bold = Flag.Inherit;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Flag m_italic = Flag.Inherit;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Flag m_strikethrough = Flag.Inherit;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private XColor m_foreColor = XColors.Black;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private XColor m_backColor = XColors.White;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private HAlignment m_alignment = HAlignment.Inherit;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private int m_leftIndent = 0;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private int m_rightIndent = 0;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private int m_firstLineIndent = 0;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private int m_spacingBefore = 0;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private int m_spacingAfter = 0;

		#region Constructors/Disposer
		public Style()
			: this(null, null, Flag.Inherit, Flag.Inherit, Flag.Inherit, XColors.Black, XColors.White)
		{
		}

		public Style(string fontFace, Measure fontSize)
			: this(fontFace, fontSize, Flag.Inherit, Flag.Inherit, Flag.Inherit, XColors.Black, XColors.White)
		{
		}

		public Style(Measure fontSize)
			: this(null, fontSize, Flag.Inherit, Flag.Inherit, Flag.Inherit, XColors.Black, XColors.White)
		{
		}

		public Style(Flag bold, Flag italic, Flag underline)
			: this(null, null, bold, italic, underline, XColors.Black, XColors.White)
		{
		}

		public Style(string fontFace, Measure fontSize, Flag bold, Flag italic, Flag underline)
			: this(fontFace, fontSize, bold, italic, underline, XColors.Black, XColors.White)
		{
		}

		public Style(string fontFace, Measure fontSize, Flag bold, Flag italic, Flag underline, XColor foreColor, XColor backColor)
		{
			m_fontFace = fontFace;
			m_fontSize = fontSize;

			m_bold = bold;
			m_italic = italic;
			m_strikethrough = underline;

			m_foreColor = foreColor;
			m_backColor = backColor;
		}
		#endregion

		#region Public Properties
		public string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}

		public bool IsModified
		{
			get { return m_isModified; }
			set { m_isModified = value; }
		}

		public string FontFace
		{
			get { return m_fontFace; }
			set
			{
				if (value.Trim().Length == 0)
					value = null;

				if (m_fontFace != value)
				{
					m_fontFace = value;
					m_isModified = true;
				}
			}
		}

		public Measure FontSize
		{
			get { return m_fontSize; }
			set
			{
				if (m_fontSize != value)
				{
					m_fontSize = value;
					m_isModified = true;
				}
			}
		}

		public Flag Bold
		{
			get { return m_bold; }
			set
			{
				if (m_bold != value)
				{
					m_bold = value;
					m_isModified = true;
				}
			}
		}

		public Flag Italic
		{
			get { return m_italic; }
			set
			{
				if (m_italic != value)
				{
					m_italic = value;
					m_isModified = true;
				}
			}
		}

		public Flag Strikethrough
		{
			get { return m_strikethrough; }
			set
			{
				if (m_strikethrough != value)
				{
					m_strikethrough = value;
					m_isModified = true;
				}
			}
		}

		public XColor ForeColor
		{
			get { return m_foreColor; }
			set
			{
				if (m_foreColor != value)
				{
					m_foreColor = value;
					OnPropertyChanged("ForeColor");
				}
			}
		}

		public XColor BackColor
		{
			get { return m_backColor; }
			set
			{
				if (m_backColor != value)
				{
					m_backColor = value;
					OnPropertyChanged("BackColor");
				}
			}
		}

		private bool ShouldSerializeForeColor()
		{
			return ForeColor != XColors.Transparent;
		}

		private bool ShouldSerializeBackColor()
		{
			return BackColor != XColors.Transparent;
		}

		public HAlignment Alignment
		{
			get { return m_alignment; }
			set
			{
				if (m_alignment != value)
				{
					m_alignment = value;
					m_isModified = true;
				}
			}
		}

		public int LeftIndent
		{
			get { return m_leftIndent; }
			set
			{
				if (m_leftIndent != value)
				{
					m_leftIndent = value;
					m_isModified = true;
				}
			}
		}
        public int TopIndent { get { return m_leftIndent; } }

		public int RightIndent
		{
			get { return m_rightIndent; }
			set
			{
				if (m_rightIndent != value)
				{
					m_rightIndent = value;
					m_isModified = true;
				}
			}
		}

		public int FirstLineIndent
		{
			get { return m_firstLineIndent; }
			set
			{
				if (m_firstLineIndent != value)
				{
					m_firstLineIndent = value;
					m_isModified = true;
				}
			}
		}

		public int SpacingBefore
		{
			get { return m_spacingBefore; }
			set
			{
				if (m_spacingBefore != value)
				{
					m_spacingBefore = value;
					OnPropertyChanged("SpacingBefore");
				}
			}
		}

		public int SpacingAfter
		{
			get { return m_spacingAfter; }
			set
			{
				if (m_spacingAfter != value)
				{
					m_spacingAfter = value;
					OnPropertyChanged("SpacingAfter");
				}
			}
		}

		#endregion

		#region Clone
		public Style Clone()
		{
			Style clone = new Style();

			clone.m_name = m_name;

			clone.m_fontFace = m_fontFace;
			clone.m_fontSize = m_fontSize == null ? null : m_fontSize.Clone();

			clone.m_bold = m_bold;
			clone.m_italic = m_italic;
			clone.m_strikethrough = m_strikethrough;

			clone.m_foreColor = m_foreColor;
			clone.m_backColor = m_backColor;

			clone.m_alignment = m_alignment;
			clone.m_leftIndent = m_leftIndent;
			clone.m_rightIndent = m_rightIndent;
			clone.m_firstLineIndent = m_firstLineIndent;

			clone.m_spacingBefore = m_spacingBefore;
			clone.m_spacingAfter = m_spacingAfter;

			return clone;
		}
		#endregion

		#region Update
		public void Update(Style style)
		{
			m_fontFace = style.m_fontFace;
			m_fontSize = style.m_fontSize == null ? null : style.m_fontSize.Clone();

			m_bold = style.m_bold;
			m_italic = style.m_italic;
			m_strikethrough = style.m_strikethrough;

			m_foreColor = style.m_foreColor;
			m_backColor = style.m_backColor;

			m_alignment = style.m_alignment;
			m_leftIndent = style.m_leftIndent;
			m_rightIndent = style.m_rightIndent;
			m_firstLineIndent = style.m_firstLineIndent;

			m_spacingBefore = style.m_spacingBefore;
			m_spacingAfter = style.m_spacingAfter;
		}
		#endregion

		#region Merge
		public static void Merge(Style from, Style to)
		{
			if (from.m_fontFace != null)
				to.m_fontFace = from.m_fontFace;

			if (from.m_fontSize != null)
			{
				if (from.m_fontSize.IsAbsolute)
					to.m_fontSize = from.m_fontSize.Clone();
				else
					to.m_fontSize.Value += from.m_fontSize.Value;
			}

			if (from.m_bold != Flag.Inherit)
				to.m_bold = from.m_bold;

			if (from.m_italic != Flag.Inherit)
				to.m_italic = from.m_italic;

			if (from.m_strikethrough != Flag.Inherit)
				to.m_strikethrough = from.m_strikethrough;

			if (from.m_foreColor != XColors.Transparent)
				to.m_foreColor = from.m_foreColor;

			if (from.m_backColor != XColors.Transparent)
				to.m_backColor = from.m_backColor;

			if (from.m_alignment != HAlignment.Inherit)
				to.m_alignment = from.m_alignment;

			to.m_leftIndent += from.m_leftIndent;
			to.m_rightIndent += from.m_rightIndent;
			to.m_firstLineIndent += from.m_firstLineIndent;

			to.m_spacingBefore += from.m_spacingBefore;
			to.m_spacingAfter += from.m_spacingAfter;
		}
		#endregion

		#region XML Serialization
		/*public XmlElement SaveToXml(XmlDocument document)
        {
            XmlElement element = document.CreateElement("Style");

            element.SetAttribute("Name", m_name);

            if (m_fontFace != null)
                element.SetAttribute("FontFace", m_fontFace);

            if (m_fontSize != null)
                element.SetAttribute("FontSize", m_fontSize.ToString());

            if (m_bold != Flag.Inherit)
                element.SetAttribute("Bold", m_bold.ToString());

            if (m_italic != Flag.Inherit)
                element.SetAttribute("Italic", m_italic.ToString());

            if (m_underline != Flag.Inherit)
                element.SetAttribute("Underline", m_underline.ToString());

            TypeConverter converter = TypeDescriptor.GetConverter(typeof(Color));

            if (m_foreColor != Color.Empty)
                element.SetAttribute("ForeColor", converter.ConvertToString(m_foreColor));

            if (m_backColor != Color.Empty)
                element.SetAttribute("BackColor", converter.ConvertToString(m_backColor));

            if (m_alignment != HAlignment.Inherit)
                element.SetAttribute("Alignment", m_alignment.ToString());

            if (m_leftIndent != 0)
                element.SetAttribute("LeftIndent", m_leftIndent.ToString());

            if (m_rightIndent != 0)
                element.SetAttribute("RightIndent", m_rightIndent.ToString());

            if (m_firstLineIndent != 0)
                element.SetAttribute("FirstLineIndent", m_firstLineIndent.ToString());

            return element;
        }*/

		/*public void RestoreFromXml(XmlElement element)
		{
			m_name = element.GetAttribute("Name");

			if (element.HasAttribute("FontFace"))
				m_fontFace = element.GetAttribute("FontFace");

			if (element.HasAttribute("FontSize"))
				m_fontSize = new Measure(element.GetAttribute("FontSize"));

			if (element.HasAttribute("Bold"))
				m_bold = (Flag)Enum.Parse(typeof(Flag), element.GetAttribute("Bold"), true);

			if (element.HasAttribute("Italic"))
				m_italic = (Flag)Enum.Parse(typeof(Flag), element.GetAttribute("Italic"), true);

			if (element.HasAttribute("Underline"))
				m_underline = (Flag)Enum.Parse(typeof(Flag), element.GetAttribute("Underline"), true);

			TypeConverter converter = TypeDescriptor.GetConverter(typeof(Color));

			if (element.HasAttribute("ForeColor"))
				m_foreColor = (Color)converter.ConvertFromString(element.GetAttribute("ForeColor"));

			if (element.HasAttribute("BackColor"))
				m_backColor = (Color)converter.ConvertFromString(element.GetAttribute("BackColor"));

			if (element.HasAttribute("Alignment"))
				m_alignment = (HAlignment)Enum.Parse(typeof(HAlignment), element.GetAttribute("Alignment"), true);

			if (element.HasAttribute("LeftIndent"))
				m_leftIndent = int.Parse(element.GetAttribute("LeftIndent"));

			if (element.HasAttribute("RightIndent"))
				m_rightIndent = int.Parse(element.GetAttribute("RightIndent"));

			if (element.HasAttribute("FirstLineIndent"))
				m_firstLineIndent = int.Parse(element.GetAttribute("FirstLineIndent"));
		}*/
		#endregion

		#region Object Overrides
		public override bool Equals(object obj)
		{
			if (obj == null || !(obj is Style))
				return false;

			Style other = (Style)obj;

			if (m_fontFace != other.m_fontFace)
				return false;

			if (m_fontSize == null && other.m_fontSize != null)
				return false;

			if (m_fontSize != null && other.m_fontSize == null)
				return false;

			if (!m_fontSize.Equals(other.m_fontSize))
				return false;

			return m_bold == other.m_bold
				&& m_italic == other.m_italic
				&& m_strikethrough == other.m_strikethrough
				&& m_foreColor == other.m_foreColor
				&& m_backColor == other.m_backColor
				&& m_alignment == other.m_alignment
				&& m_leftIndent == other.m_leftIndent
				&& m_rightIndent == other.m_rightIndent
				&& m_firstLineIndent == other.m_firstLineIndent
				&& m_spacingBefore == other.m_spacingBefore
				&& m_spacingAfter == other.m_spacingAfter;
		}

		public override int GetHashCode()
		{
			int hashCode = m_bold.GetHashCode()
				^ m_italic.GetHashCode()
				^ m_strikethrough.GetHashCode()
				^ m_foreColor.GetHashCode()
				^ m_backColor.GetHashCode()
				^ m_alignment.GetHashCode()
				^ m_leftIndent.GetHashCode()
				^ m_rightIndent.GetHashCode()
				^ m_firstLineIndent.GetHashCode()
				^ m_spacingBefore.GetHashCode()
				^ m_spacingAfter.GetHashCode();

			if (m_fontFace != null)
				hashCode ^= m_fontFace.GetHashCode();

			if (m_fontSize != null)
				hashCode ^= m_fontSize.GetHashCode();

			return hashCode;
		}
		#endregion

		internal static Style Load(XmlReader reader)
		{
			Style style = new Style();
			string value = null;

			try
			{

				style.Name = reader.GetAttribute("Name");

				value = reader.GetAttribute("FontFace");
				if (!string.IsNullOrEmpty(value))
					style.FontFace = value;

				value = reader.GetAttribute("FontSize");
				if (!string.IsNullOrEmpty( value ))
					style.FontSize = new Measure(value);

				value = reader.GetAttribute("Bold");
				if (!string.IsNullOrEmpty( value ))
					style.Bold = (Flag)Enum.Parse(typeof(Flag), value, true);

				value = reader.GetAttribute("Italic");
				if (!string.IsNullOrEmpty( value ))
					style.Italic = (Flag)Enum.Parse(typeof(Flag), value, true);

				value = reader.GetAttribute("Strikethrough");
				if (!string.IsNullOrEmpty( value ))
					style.Strikethrough = (Flag)Enum.Parse(typeof(Flag), value, true);

				/*value = reader.GetAttribute("ForeColor");
				if (!string.IsNullOrWhiteSpace(value))
					style.ForeColor = (XColor)typeof(XColors).GetProperty(value).GetValue(null, null);

				value = reader.GetAttribute("BackColor");
				if (!string.IsNullOrWhiteSpace(value))
					style.BackColor = (XColor)typeof(XColors).GetProperty(value).GetValue(null, null);*/

				value = reader.GetAttribute("Alignment");
				if (!string.IsNullOrEmpty(value))
					style.Alignment = (HAlignment)Enum.Parse(typeof(HAlignment), value, true);

				value = reader.GetAttribute("LeftIndent");
				if (!string.IsNullOrEmpty( value ))
					style.LeftIndent = int.Parse(value);

				value = reader.GetAttribute("RightIndent");
				if (!string.IsNullOrEmpty( value ))
					style.RightIndent = int.Parse(value);

				value = reader.GetAttribute("FirstLineIndent");
				if (!string.IsNullOrEmpty( value ))
					style.FirstLineIndent = int.Parse(value);

				value = reader.GetAttribute("SpacingBefore");
				if (!string.IsNullOrEmpty( value ))
					style.SpacingBefore = int.Parse(value);

				value = reader.GetAttribute("SpacingAfter");
				if (!string.IsNullOrEmpty( value ))
					style.SpacingAfter = int.Parse(value);
			}
			catch (Exception)
			{
                //
			}

			return style;
		}

		public override string ToString()
		{
			return string.Format("{0}:{1}{2}{3}", Name, FontFace, FontSize.Value.ToString(), (Bold == Flag.On ? "B" : "") + (Italic == Flag.On ? "I" : ""));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}
