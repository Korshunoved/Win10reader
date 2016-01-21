using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
	public class DocumentBlock : ContainerBlock
	{
		public DocumentBlock( DocumentFormatter formatter, FictionBook.Document element, int id ) 
			: base( formatter, element, id ) 
		{ 
		}
	}
}
