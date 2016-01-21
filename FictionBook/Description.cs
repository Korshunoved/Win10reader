using System;
using System.Collections.Generic;

namespace FictionBook
{
	public class Description
	{
		#region Constructors/Disposer
		public Description()
		{
			CustomInfos = new List<CustomInfo>();
		}
		#endregion

		#region Public Properties
		public TitleInfo TitleInfo { get; set; }
		public DocumentInfo DocumentInfo { get; set; }
		public PublishInfo PublishInfo { get; set; }

		public IList<CustomInfo> CustomInfos { get; private set; }
		#endregion

		public override int GetHashCode()
		{
			return TitleInfo != null ? TitleInfo.GetHashCode() : base.GetHashCode();
		}
	}
}
