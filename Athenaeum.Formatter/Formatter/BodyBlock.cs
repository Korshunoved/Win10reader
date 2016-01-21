using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
	public class BodyBlock : ContainerBlock
    {
        #region Constructors/Disposer
        public BodyBlock(DocumentFormatter formatter, FictionBook.Body element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion
	}
}