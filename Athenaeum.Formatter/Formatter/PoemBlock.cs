using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
    public class PoemBlock : ContainerBlock
    {
        #region Constructors/Disposer
        public PoemBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region UpdateStyles
        public override void UpdateStyles()
        {
            Formatter.PushStyle(Styles.KnownStyles.Poem);
            base.UpdateStyles();
            Formatter.PopStyle();
        }
        #endregion
    }

    public class StanzaBlock : ContainerBlock
    {
        #region Constructors/Disposer
        public StanzaBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region UpdateStyles
        public override void UpdateStyles()
        {
            Formatter.PushStyle(Styles.KnownStyles.Stanza);
            base.UpdateStyles();
            Formatter.PopStyle();
        }
        #endregion
    }

    public class VerseBlock : ParagraphBlock
    {
        #region Constructors/Disposer
        public VerseBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region UpdateStyles
        public override void UpdateStyles()
        {
            Formatter.PushStyle(Styles.KnownStyles.Verse);
            base.UpdateStyles();
            Formatter.PopStyle();
        }
        #endregion
    }

	public class TextAuthorBlock : ParagraphBlock
	{
		#region Constructors/Disposer
		public TextAuthorBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
			: base(formatter, element, id)
		{
		}
		#endregion

		#region UpdateStyles
		public override void UpdateStyles()
		{
			Formatter.PushStyle(Styles.KnownStyles.TextAuthor);
			base.UpdateStyles();
			Formatter.PopStyle();
		}
		#endregion

	}
}
