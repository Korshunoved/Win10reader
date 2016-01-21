using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FictionBook
{
	public class Binary
	{
		public string Id { get; set; }
		public string ContentType { get; set; }
		public byte[] Data { get; set; }

		public override string ToString()
		{
			return Data == null ? "null" : Data.Length.ToString();
		}
	}

	public class BinaryCollection : KeyedCollection<string, Binary>
	{
		protected override string GetKeyForItem( Binary item )
		{
			return item.Id;
		}

		public override string ToString()
		{
			return string.Format( "Count ({0}), Total {1}", Count, Items.Sum( p => p.Data == null ? 0 : p.Data.Length ) );
		}
	}
}