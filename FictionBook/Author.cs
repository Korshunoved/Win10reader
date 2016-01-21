using System;
using System.Collections.Generic;

namespace FictionBook
{
	public class Author
	{
		#region Constructors/Disposer
		public Author()
		{
			HomePages = new List<string>();
			Emails = new List<string>();
		}
		#endregion

		public IList<string> Emails { get; private set; }
		public TextField FirstName { get; set; }
		public IList<string> HomePages { get; private set; }
		public TextField LastName { get; set; }
		public TextField MiddleName { get; set; }
		public TextField NickName { get; set; }

		public override bool Equals( object obj )
		{
			var other = obj as Author;

			if ( other != null )
			{
				return other.ToString() == ToString();
			}

			return base.Equals( obj );
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		public override string ToString()
		{
			string str;

			if ( FirstName != null &&
				LastName != null )
			{
				str = FirstName.ToString();

				if ( MiddleName != null )
				{
					str += " " + MiddleName.ToString();
				}
				str += " " + LastName.ToString();

				if ( NickName != null )
				{
					str += " (" + NickName.ToString() + ")";
				}
			}
			else
			{
				if ( NickName != null )
				{
					str = NickName.ToString();
				}
				else
				{
					str = String.Empty;
				}
			}

			return str;
		}
	}
}
