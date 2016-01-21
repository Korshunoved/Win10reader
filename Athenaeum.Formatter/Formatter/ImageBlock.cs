using System;
using System.Collections;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
    public class ImageBlock : Block, IDrawable
    {
        private XSize _originalSize, _paintSize;
        private byte[] _binary;

        #region Constructors/Disposer
        public ImageBlock(DocumentFormatter formatter, FictionBook.Element element, int id)
            : base(formatter, element, id)
        {
        }
        #endregion

        #region Public Properties
        public new FictionBook.ImageElement Element
        {
            get { return (FictionBook.ImageElement)base.Element; }
        }

        internal override PaintObject PaintObject
        {
            get { return Formatter.GetCurrentPaintObject(); }
            set { base.PaintObject = value; }
        }
        #endregion

        private byte[] GetBinary()
        {
            if (Element.Reference == null || (Element.Reference.Length == 1 && Element.Reference[0] == '#'))
                return null;
            string reference = Element.Reference.Substring(1);
            if (!Formatter.Document.Binaries.Contains(reference))
                return null;
            FictionBook.Binary binary = Formatter.Document.Binaries[reference];
            if (!binary.ContentType.ToLower().StartsWith("image/"))
                return null;            
            return binary.Data;
        }


        #region BuildChildBlocks
        public override void BuildChildrenBlocks(object additionalRoot = null)
        {
            base.BuildChildrenBlocks();

            try
            {
                _binary = GetBinary();
                if (_binary == null)
                    return;
                _originalSize = Formatter.Context.MeasureImage(_binary);
            }
            catch(Exception)
            {
                _originalSize = XSize.Empty;
            }
        }
        #endregion

        #region Format
        public override int Format(XRect bounds)
        {
            int y = (int)bounds.Top;

            Formatter.OnBlockFormatting(y);

            Athenaeum.Styles.Style style = Formatter.GetCurrentStyle();

            _paintSize = _originalSize;

            if (_paintSize != XSize.Empty)
            {
                if (_paintSize.Width > 0 && _paintSize.Height > 0 && (_paintSize.Width > bounds.Width || _paintSize.Height > bounds.Height))
                {
                    double coefH = (double)(bounds.Height-10) / (double)_paintSize.Height;
                    double coef = (double)(bounds.Width-10) / (double)_paintSize.Width;
                    if (coef >= coefH) coef = coefH;
                    _paintSize.Height = (int)(((double)_paintSize.Height) * coef);
                    _paintSize.Width = (int)(((double)_paintSize.Width) * coef);
                }

                if (!Formatter.RectangleFitsOnPage(new XRect(bounds.Left, y, _paintSize.Width, _paintSize.Height)))
                {
                    y = Formatter.AdjustToPage(y);
                }
            }

            Bounds = new XRect(bounds.Left, y, bounds.Width, (int)Math.Min(bounds.Height - 1, _paintSize.Height));

            Formatter.OnBlockFormatted(this);

            return (y + (int)_paintSize.Height) - (int)bounds.Top;
        }
        #endregion

        #region Paint
        public override void Paint(PaintingContext context)
        {
            base.Paint(context);

            if (_binary == null || _paintSize == XSize.Empty)
                return;

            int x = (int)Bounds.X - (int)context.Bounds.X + ((int)Bounds.Width - (int)_paintSize.Width) / 2;
            XRect dst = new XRect(x, Bounds.Y - context.Bounds.Y, _paintSize.Width, _paintSize.Height);
            context.Context.DrawImage(context.Target, dst, _binary);

            if (context.Pointer == null)
                context.Pointer = new Pointer(Id);
        }
        #endregion

        #region implementations
        int IDrawable.Y
        {
            get { return (int)Bounds.Y; }
        }
        Pointer IDrawable.GetPointer()
        {
            return new Pointer(Id);
        }
        #endregion
    }
}
