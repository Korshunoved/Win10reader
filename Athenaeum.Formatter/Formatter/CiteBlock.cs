using System;
using System.Collections;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
    public class CiteBlock : ContainerBlock
    {
        #region Constructors/Disposer
        public CiteBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region UpdateStyles
        public override void UpdateStyles()
        {
            Formatter.PushStyle(Styles.KnownStyles.Cite);
            base.UpdateStyles();
            Formatter.PopStyle();
        }
        #endregion
    }
}
