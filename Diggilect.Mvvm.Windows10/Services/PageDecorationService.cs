#region Copyright (c) 2011-2014 Gregory Nickonov and Andrew Nefedkin (Actis® Wunderman)
// Copyright (c) 2011-2014 Gregory Nickonov and Andrew Nefedkin (Actis® Wunderman).
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;

using Autofac;

using Digillect.Mvvm.UI;

namespace Digillect.Mvvm.Services
{
	/// <summary>
	///     Default implementation of <see cref="IPageDecorationService" />.
	/// </summary>
	internal sealed class PageDecorationService : IPageDecorationService
	{
		#region Add/Remove Page decoration
		/// <summary>
		///     Performs decoration of the page.
		/// </summary>
		/// <param name="windowsRtPage">The page.</param>
		public void AddDecoration( WindowsRTPage windowsRtPage )
		{
			if( windowsRtPage == null )
			{
				throw new ArgumentNullException( "windowsRtPage" );
			}

			foreach( var decorator in windowsRtPage.Scope.Resolve<IEnumerable<IPageDecorator>>() )
			{
				decorator.AddDecoration( windowsRtPage );
			}
		}

		/// <summary>
		///     Optionally removes decoration from the page.
		/// </summary>
		/// <param name="windowsRtPage">The page.</param>
		public void RemoveDecoration( WindowsRTPage windowsRtPage )
		{
			if( windowsRtPage == null )
			{
				throw new ArgumentNullException( "windowsRtPage" );
			}

			foreach( var decorator in windowsRtPage.Scope.Resolve<IEnumerable<IPageDecorator>>() )
			{
				decorator.RemoveDecoration( windowsRtPage );
			}
		}
		#endregion
	}
}