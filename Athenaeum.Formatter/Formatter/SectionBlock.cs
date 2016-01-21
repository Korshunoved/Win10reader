using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
	public class SectionBlock : ContainerBlock
	{
		#region Constructors/Disposer
		public SectionBlock( DocumentFormatter formatter, FictionBook.Element element, int id )
			: base( formatter, element, id )
		{
		}
		#endregion

		#region UpdateStyles
		public override void UpdateStyles()
		{
			Formatter.PushStyle( Styles.KnownStyles.Section );
			base.UpdateStyles();
			Formatter.PopStyle();
		}
		#endregion

		#region Format
		public override int Format( XRect bounds )
		{
			return base.Format( bounds );
		}
		#endregion

	}
}
