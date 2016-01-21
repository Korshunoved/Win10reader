using System;
using System.Collections.Generic;

namespace FictionBook
{
	public class PublishInfo
	{
		public TextField BookName { get; set; }
		public TextField Publisher { get; set; }
		public TextField City { get; set; }
		public int Year { get; set; }
		public TextField ISBN { get; set; }
		public IList<Sequence> Sequences { get; private set; }

		public PublishInfo()
		{
			this.Sequences = new List<Sequence>();
		}
	}
}
