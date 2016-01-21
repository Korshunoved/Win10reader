using System;
using System.Collections;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
    public class EpigraphBlock : ContainerBlock
    {
        #region Constructors/Disposer
        public EpigraphBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region UpdateStyles
        public override void UpdateStyles()
        {
            Formatter.PushStyle(Styles.KnownStyles.Epigraph);
            base.UpdateStyles();
            Formatter.PopStyle();
        }
        #endregion
    }
}