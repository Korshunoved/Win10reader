using System;

namespace FictionBook
{
	public class TextField
	{
		public string Language { get; set; }
		public string Text { get; set; }

		public override bool Equals( object obj )
		{
			if( obj == null || obj.GetType() != GetType() )
			{
				return false;
			}

			var other = (TextField) obj;

			if( Text != other.Text )
			{
				return false;
			}

			if( (Language != null && other.Language != null) && (Language != other.Language) )
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var hashCode = 0;

			if( Text != null )
			{
				hashCode ^= Text.GetHashCode();
			}

			if( Language != null )
			{
				hashCode ^= Language.GetHashCode();
			}

			return hashCode;
		}

		public override string ToString()
		{
			return Text;
		}
	}
}
