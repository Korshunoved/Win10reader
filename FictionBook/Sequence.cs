using System;
using System.Collections.Generic;

namespace FictionBook
{
	public class Sequence
	{
		public string Name { get; set; }
		public int Number { get; set; }
		public string Language { get; set; }
		public IList<Sequence> Sequences { get; private set; }

		public Sequence()
		{
			this.Sequences = new List<Sequence>();
		}
	}
}
