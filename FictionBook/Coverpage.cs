using System;
using System.Collections.ObjectModel;

namespace FictionBook
{
	public class Coverpage
	{
		private readonly ObservableCollection<ImageLink> _images = new ObservableCollection<ImageLink>();

		public ObservableCollection<ImageLink> Images
		{
			get { return _images; }
		}
	}
}
