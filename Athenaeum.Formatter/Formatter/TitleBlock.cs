using System;
using System.Collections;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
    public class TitleBlock : ContainerBlock
    {
        #region Constructors/Disposer
        public TitleBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region UpdateStyles
        public override void UpdateStyles()
        {
            Formatter.PushStyle(Styles.KnownStyles.Title);
            base.UpdateStyles();
            Formatter.PopStyle();
        }
        #endregion
    }

    public class SubtitleBlock : ParagraphBlock
    {
        #region Constructors/Disposer
        public SubtitleBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region UpdateStyles
        public override void UpdateStyles()
        {
            Formatter.PushStyle(Styles.KnownStyles.Subtitle);
            base.UpdateStyles();
            Formatter.PopStyle();
        }
        #endregion
    }
}
