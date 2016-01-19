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
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Digillect.Mvvm.UI
{
	internal class Breadcrumb
	{
		private readonly XParameters _parameters;
		private readonly Type _type;

		#region Constructors/Disposer
		/// <summary>
		///     Initializes a new instance of the Breadcrumb class.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		public Breadcrumb( Type type, XParameters parameters )
		{
		    //try
		    //{
		    //    //Contract.Requires<ArgumentNullException>(type != null, "type");
		    //}
		    //catch (Exception ex)
		    //{
		    //    Debug.WriteLine(ex.Message);
		    //}
		    _type = type;
			_parameters = parameters;
		}
		#endregion

		#region Public Properties
		public Type Type
		{
			get { return _type; }
		}

		public XParameters Parameters
		{
			get { return _parameters; }
		}
		#endregion

		public override bool Equals( object obj )
		{
			if( obj == null || !(obj is Breadcrumb) )
			{
				return false;
			}

			var other = (Breadcrumb) obj;

			return _type == other._type && Equals( _parameters, other._parameters );
		}

		public override int GetHashCode()
		{
			int hashCode = 17 + _type.GetHashCode();

			if( _parameters != null )
			{
				foreach( var p in _parameters )
				{
					hashCode = hashCode*37 + p.Key.GetHashCode();
					hashCode = hashCode*37 + p.Value.GetHashCode();
				}
			}

			return hashCode;
		}
	}
}