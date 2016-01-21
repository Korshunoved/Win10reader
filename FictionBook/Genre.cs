using System;

namespace FictionBook
{
	public class Genre
	{
		public string Name { get; set; }
		public int Match { get; set; }

		public Genre( string Name, int Match = 100 )
		{
			this.Name = Name;
			this.Match = Match;
		}
	}
}
