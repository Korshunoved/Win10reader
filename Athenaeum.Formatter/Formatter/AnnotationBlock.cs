using System;
using System.Collections;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
    public class AnnotationBlock : ContainerBlock
    {
        #region Constructors/Disposer
        public AnnotationBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region UpdateStyles
        public override void UpdateStyles()
        {
            base.UpdateStyles();
        }
        #endregion
    }
}
