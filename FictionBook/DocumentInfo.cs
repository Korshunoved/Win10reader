using System;
using System.Collections.Generic;

namespace FictionBook
{
	public class DocumentInfo
	{
		private readonly List<Author> _authors = new List<Author>();
		private readonly List<string> _sourceUrls = new List<string>();

		public TextField ProgramUsed { get; set; }
		public DateField Date { get; set; }
		public TextField SourceOcr { get; set; }
		public string Id { get; set; }
		public float Version { get; set; }
		public AnnotationElement History { get; set; }

        public IList<Author> Authors
		{
			get { return _authors; }
		}

		public IList<string> SourceUrls
		{
			get { return _sourceUrls; }
		}
	}
}
