using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Athenaeum.Formatter
{

	internal sealed class PaintObject : IDisposable
	{

		internal IXFont Font;
		internal Athenaeum.Styles.Style Style;

		internal PaintObject( IXFont font, Athenaeum.Styles.Style style )
		{
			this.Font = font;
			this.Style = style;
		}

		void IDisposable.Dispose()
		{
		}

	}
}
