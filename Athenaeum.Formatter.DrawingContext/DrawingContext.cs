using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Athenaeum.Formatter.DrawingContext
{
    public class DrawingContext : IDrawingContext
    {

        private static int __size(int offset)
        {
            return PaintingContext.Settings.Sizes[(int)PaintingContext.Settings.Scale + ((int)PaintingContext.Settings.Dencity) * 3 + 1];
        }

        private static XFont Default
        {
            get { return XFont.Find(__size(0)); }
        }

        IXFont IDrawingContext.GetFont(Faces face, int size, bool bold, bool italic)
        {
            int c = 6;
            var calculatedSize = size + (int)PaintingContext.Settings.Dencity * c;
            var indx = PaintingContext.Settings.Sizes.IndexOf(calculatedSize) +
                       (int)PaintingContext.Settings.Scale;
            size = PaintingContext.Settings.Sizes[indx];

            XFont font = XFont.TryFind(face, size, bold, italic);
            if (font != null)
                return font;
            foreach (Faces f in PaintingContext.Settings.Faces.Where(p => p != face))
            {
                font = XFont.TryFind(f, size, bold, italic);
                if (font != null)
                    return font;
            }
            return Default;
        }

        private Queue<WriteableBitmap> _targetPool = new Queue<WriteableBitmap>();
        private int _targetPoolWidth, _targetPoolHeight;

        object IDrawingContext.CreateTarget(int width, int height)
        {
            if (width != _targetPoolWidth || height != _targetPoolHeight)
            {
                _targetPool.Clear();

                _targetPoolWidth = width;
                _targetPoolHeight = height;
            }


#if WINDOWS_PHONE
			//return new WriteableBitmap( width, height );
			var target = _targetPool.Count > 0 ? _targetPool.Dequeue() : new WriteableBitmap( width, height );
#else
            //return new WriteableBitmap(width, height, 300, 300, PixelFormats.Pbgra32, null);
            var target = _targetPool.Count > 0 ? _targetPool.Dequeue() : new WriteableBitmap(width, height, 300, 300, PixelFormat.Pbgra32, null);
#endif

            return target;
        }

        void IDrawingContext.ReleaseTarget(object o)
        {
            if (o is WriteableBitmap)
            {
                _targetPool.Enqueue((WriteableBitmap)o);
            }
        }

        void IDrawingContext.Clear(object target, XColor color)
        {
            ((WriteableBitmap)target).Clear(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        void IDrawingContext.Clear(object target, string uri, double brightness)
        {
            brightness = Math.Min(Math.Max(0, brightness), 1); ;
            WriteableBitmap bmp = (WriteableBitmap)target;
            WriteableBitmap background = LoadImage(uri);
            if (brightness < 1)
                for (int x = 0; x < (int)background.PixelWidth; x++)
                    for (int y = 0; y < (int)background.PixelWidth; y++)
                    {
                        Color c = background.GetPixel(x, y);
                        c.R = (byte)(c.R * brightness);
                        c.G = (byte)(c.G * brightness);
                        c.B = (byte)(c.B * brightness);
                        background.SetPixel(x, y, c);
                    }
            Rect src = new Rect(0, 0, background.PixelWidth, background.PixelHeight);
            Rect dst = src;
            for (; dst.X < bmp.PixelWidth; dst.X += background.PixelWidth)
                for (dst.Y = src.Y; dst.Y < bmp.PixelHeight; dst.Y += background.PixelHeight)
                    bmp.Blit(dst, background, src);
        }

        internal static WriteableBitmap LoadImage(string uri)
        {
            using (Stream stream = Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream)
                return LoadImage(stream);
        }

        private static WriteableBitmap LoadImage(Stream stream)
        {
            BitmapImage image = new BitmapImage();
#if WINDOWS_PHONE
			image.SetSource( stream );
			return new WriteableBitmap( image );
#else
            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            WriteableBitmap bmp = new WriteableBitmap(image);
            return BitmapFactory.ConvertToPbgra32Format(bmp);
#endif
        }

        internal static WriteableBitmap LoadImage(byte[] source)
        {
            using (Stream stream = new MemoryStream(source))
                return LoadImage(stream);
        }

        XSize IDrawingContext.MeasureImage(byte[] source)
        {
            XSize size = XSize.Empty;
            try
            {
                using (Stream stream = new MemoryStream(source))
                    size = decode(stream);
                if (size != XSize.Empty)
                    return size;
            }
            catch
            {
            }

            WriteableBitmap bmp = LoadImage(source);
            size = new XSize(bmp.PixelWidth, bmp.PixelHeight);
            bmp = null;
            return size;
        }

        void IDrawingContext.DrawImage(object target, XRect destination, byte[] source)
        {
            WriteableBitmap bmp = LoadImage(source); ;
            ((WriteableBitmap)target).Blit(new Rect(destination.X, destination.Y, destination.Width, destination.Height), bmp, new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
        }

        void IDrawingContext.FillRectangle(object target, XRect destination, XColor color)
        {
            ((WriteableBitmap)target).FillRectangle(destination.Left, destination.Top, destination.Right, destination.Bottom, Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        ICollection<Styles.Stylesheet> IDrawingContext.GetStyles()
        {
            using (Stream input = Application.GetResourceStream(new Uri("Styles/DefaultStyles.xml", UriKind.RelativeOrAbsolute)).Stream)
                return Styles.Stylesheet.Load(input);
        }

        static byte[] mwPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        static byte[] mwJfif = new byte[] { 0xff, 0xd8, 0xff, 0xe0 };
        static byte[] mwGif1 = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 };
        static byte[] mwGif2 = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };
        static byte[] mwBmp = new byte[] { 0x42, 0x4D };
        static byte[] mwJpg = new byte[] { 0xff, 0xd8, 0xff };

        private static Dictionary<byte[], Func<Stream, XSize>> decoders = new Dictionary<byte[], Func<Stream, XSize>>()
        {
            { mwPng, decodePng },
            { mwJfif, decodeJfif },
            { mwGif1, decodeGif },
            { mwGif2, decodeGif },
            { mwBmp, decodeBmp },
            { mwJpg, decodeJpg },
       };

        private static XSize decode(Stream stream)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("Stream must be seekable");
            foreach (byte[] mw in decoders.Keys)
            {
                stream.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[mw.Length];
                stream.Read(buffer, 0, mw.Length);
                if (buffer.SequenceEqual(mw))
                {
                    buffer = null;
                    return decoders[mw](stream);
                }
            }
            return XSize.Empty;
        }

        private static int readInt32(Stream stream)
        {
            byte[] bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i += 1)
                bytes[i] = (byte)stream.ReadByte();
            var _int = BitConverter.ToInt32(bytes, 0);
            bytes = null;
            return _int;
        }

        private static short readInt16(Stream stream)
        {
            byte[] bytes = new byte[sizeof(short)];
            for (int i = 0; i < sizeof(short); i += 1)
                bytes[i] = (byte)stream.ReadByte();
            var _int = BitConverter.ToInt16(bytes, 0);
            bytes = null;
            return _int;
        }

        private static short readLittleEndianInt16(Stream stream)
        {
            byte[] bytes = new byte[sizeof(short)];
            for (int i = 0; i < sizeof(short); i += 1)
                bytes[sizeof(short) - 1 - i] = (byte)stream.ReadByte();
            var _int = BitConverter.ToInt16(bytes, 0);
            bytes = null;
            return _int;
        }

        private static int readLittleEndianInt32(Stream stream)
        {
            byte[] bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i += 1)
                bytes[sizeof(int) - 1 - i] = (byte)stream.ReadByte();
            var _int = BitConverter.ToInt32(bytes, 0);
            bytes = null;
            return _int;
        }

        private static XSize decodeJpg(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var size = new XSize(200, 200);
            bool eof = false;

            var reader = new BinaryReader(stream);

            while (!eof && (reader.BaseStream.Position < reader.BaseStream.Length))
            {
                // read 0xFF and the type
                reader.ReadByte();
                byte type = reader.ReadByte();

                // get length
                int len = 0;
                switch (type)
                {
                    // start and end of the image
                    case 0xD8:
                    case 0xD9:
                        len = 0;
                        break;

                    // restart interval
                    case 0xDD:
                        len = 2;
                        break;

                    // the next two bytes is the length
                    default:
                        int lenHi = reader.ReadByte();
                        int lenLo = reader.ReadByte();
                        len = (lenHi << 8 | lenLo) - 2;
                        break;
                }

                // EOF?
                if (type == 0xD9)
                    eof = true;

                // process the data
                if (len > 0)
                {
                    // read the data
                    byte[] data = reader.ReadBytes(len);

                    // this is what we are looking for
                    if (type >= 0xC0 && type <= 0xC3)
                    {
                        size.Height = data[1] << 8 | data[2];
                        size.Width = data[3] << 8 | data[4];
                        eof = true;
                    }
                }
            }
            reader.Close();
            return size;
        }

        private static XSize decodeBmp(Stream stream)
        {
            stream.Seek(18, SeekOrigin.Begin);
            int width = readInt32(stream);
            int height = readInt32(stream);
            return new XSize(width, height);
        }

        private static XSize decodeGif(Stream stream)
        {
            stream.Seek(6, SeekOrigin.Begin);
            int width = readInt16(stream);
            int height = readInt16(stream);
            return new XSize(width, height);
        }

        private static XSize decodePng(Stream stream)
        {
            stream.Seek(16, SeekOrigin.Begin);
            int width = readLittleEndianInt32(stream);
            int height = readLittleEndianInt32(stream);
            return new XSize(width, height);
        }

        private static XSize decodeJfif(Stream stream)
        {
            stream.Seek(4, SeekOrigin.Begin);
            byte[] buf = new byte[4];
            long blockStart = stream.Position;
            stream.Read(buf, 0, 2);
            var blockLength = ((buf[0] << 8) + buf[1]);
            stream.Read(buf, 0, 4);
            string s = "";
            foreach (byte b in buf)
                s += (char)b;
            if (s == "JFIF" && stream.ReadByte() == 0)
            {
                blockStart += blockLength;
                while (blockStart < stream.Length)
                {
                    stream.Position = blockStart;
                    stream.Read(buf, 0, 4);
                    blockLength = ((buf[2] << 8) + buf[3]);
                    if (blockLength >= 7 && buf[0] == 0xff && buf[1] >= 0xc0 && buf[1] <= 0xcf && buf[1] != 0xcf && buf[1] != 0xc4)
                    {
                        stream.Position += 1;
                        stream.Read(buf, 0, 4);
                        var height = (buf[0] << 8) + buf[1];
                        var width = (buf[2] << 8) + buf[3];
                        return new XSize(width, height);
                    }
                    blockStart += blockLength + 2;
                }
            }
            return XSize.Empty;
        }

    }

    internal class XFont : IXFont
    {

        private static string _spaceString = new string((char)160, 3);
        private static string _hyphenString = "-";
        private const char _defaultCharacter = '?';
        private const char _accentChar = (char)0x301;
        private const string _descriptorsUri = "Fonts/common.descriptor";
        private const string _fontsUri = "Fonts/common.spritefont";
        private static IDictionary<string, long> _descriptors = new Dictionary<string, long>();
        private char _maxChar;
        private Metrix[] _chars = null;
        private InfoStruct _info;
        private CommonStruct _common;
        private Dictionary<int, WriteableBitmap> _images = new Dictionary<int, WriteableBitmap>();
        private static IList<XFont> _fonts = new List<XFont>();
        private XSize _space, _hyphen;

        #region properties
        public string HyphenString { get { return _hyphenString; } }
        public Faces Face { get; set; }
        public int Size { get; set; }
        public bool Bold
        {
            get { return _info.Bold; }
        }
        public bool Italic
        {
            get { return _info.Italic; }
        }
        public string Ext
        {
            get { return GetExt(Bold, Italic); }
        }
        public string Hash
        {
            get { return GetHash(Face, Size, Bold, Italic); }
        }
        private Metrix this[char c]
        {
            get
            {
                Metrix metrix = c > _maxChar ? null : _chars[c];
                if (metrix == null)
                    metrix = _chars[_defaultCharacter];
                return metrix;
            }
        }
        #endregion

        #region ctor
        static XFont()
        {
            using (System.IO.Stream stream = Application.GetResourceStream(new Uri(_descriptorsUri, UriKind.Relative)).Stream)
            using (BinaryReader reader = new BinaryReader(stream))
                while (reader.PeekChar() != -1)
                    _descriptors.Add(reader.ReadString(), reader.ReadInt64());
        }
        #endregion

        #region methods
        private XSize MeasureString(string text, int pos, int length)
        {
            if (string.IsNullOrEmpty(text))
                return XSize.Empty;

            if (length == 0)
                length = text.Length - pos;

            int x = 0;
            Metrix prev = null;

            if (PaintingContext.Settings.UseKerning)
                for (int i = 0; i < length; i++)
                {
                    char c = text[i + pos];

                    Metrix current = this[c];

                    int kerning = 0;
                    if (prev != null && prev.Kernings.ContainsKey(current.Id))
                        kerning = prev.Kernings[current.Id];

                    if (c == _accentChar)
                        continue;

                    if (i == 0)
                        x += current.Xadvance + kerning;
                    else if (i == (length - 1))
                        x += current.Xoffset + current.Width;
                    else
                        x += current.Xoffset + current.Xadvance + kerning;

                    prev = current;
                }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    char c = text[i + pos];

                    Metrix current = this[c];

                    if (c == _accentChar)
                        continue;

                    if (i == 0)
                        x += current.Xadvance;
                    else if (i == (length - 1))
                        x += current.Xoffset + current.Width;
                    else
                        x += current.Xoffset + current.Xadvance;

                    prev = current;
                }
            }

            return new XSize(x, _common.LineHeight);
        }
        private void DrawString(WriteableBitmap bmp, Point point, Color color, string text, int pos = 0, int length = 0)
        {

            if (string.IsNullOrEmpty(text))
                return;

            if (length == 0)
                length = text.Length - pos;

            double x = point.X;
            Metrix prev = null;

            if (PaintingContext.Settings.UseKerning)
                for (int i = 0; i < length; i++)
                {
                    char c = text[i + pos];

                    Metrix current = this[c];

                    int kerning = 0;
                    if (prev != null && prev.Kernings.ContainsKey(current.Id))
                        kerning = prev.Kernings[current.Id];

                    if (x > 0)
                        x += current.Xoffset + kerning;

                    Point destRect = new Point(x, point.Y + current.Yoffset);

                    if (c == (char)0x301)
                        x -= current.Xoffset;

                    bmp.Blit(destRect, current.Image, new Rect(current.X, current.Y, current.Width, current.Height), color, WriteableBitmapExtensions.BlendMode.Alpha);
                    x += current.Xadvance;
                    prev = current;
                }
            else
                for (int i = 0; i < length; i++)
                {
                    char c = text[i + pos];

                    Metrix current = this[c];

                    if (x > 0)
                        x += current.Xoffset;

                    Point destRect = new Point(x, point.Y + current.Yoffset);

                    if (c == (char)0x301)
                        x -= current.Xoffset;

                    bmp.Blit(destRect, current.Image, new Rect(current.X, current.Y, current.Width, current.Height), color, WriteableBitmapExtensions.BlendMode.Alpha);
                    x += current.Xadvance;
                    prev = current;
                }

        }
        internal static XFont TryFind(Faces face, int size, bool bold, bool italic)
        {
            string hash = GetHash(face, size, bold, italic);
            XFont font = _fonts.FirstOrDefault(p => p.Hash == hash);
            if (font != null)
                return font;
            if (_descriptors.ContainsKey(hash))
                return font = Load(_descriptors[hash]);
            return null;
        }
        internal static XFont TryFind(int size)
        {
            XFont font = _fonts.FirstOrDefault(p => p.Size >= size && !p.Bold && !p.Italic);
            if (font != null)
                return font;
            foreach (Faces face in PaintingContext.Settings.Faces)
                foreach (int i in PaintingContext.Settings.Sizes.Where(p => p >= size))
                {
                    string hash = GetHash(face, i, false, false);
                    if (_descriptors.ContainsKey(hash))
                        return font = Load(_descriptors[hash]);
                }
            return null;
        }
        internal static XFont Find(Faces face, int size, bool bold, bool italic)
        {
            XFont font = TryFind(face, size, bold, italic);
            if (font != null)
                return font;
            throw new ArgumentOutOfRangeException(string.Format("Cant find font {0}", GetHash(face, size, bold, italic)));
        }
        internal static XFont Find(int size)
        {
            XFont font = TryFind(size);
            if (font != null)
                return font;
            throw new ArgumentOutOfRangeException(string.Format("Cant find font {0}", size));
        }
        private static XFont Load(long offset)
        {
            using (Stream stream = Application.GetResourceStream(new Uri(_fontsUri, UriKind.Relative)).Stream)
            {
                stream.Position = offset;
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    Faces face = (Faces)Enum.Parse(typeof(Faces), reader.ReadString(), false);
                    int size = reader.ReadInt32();
                    string ext = reader.ReadString();
                    XFont font = new XFont();
                    font.Face = face;
                    font.Size = size;
                    Dictionary<char, Metrix> chars = new Dictionary<char, Metrix>();
                    Read(font, reader, chars);
                    if (_fonts.Contains(font) == false)
                        _fonts.Add(font);
                    font._maxChar = chars.Keys.Max();
                    font._chars = new Metrix[font._maxChar + 1];
                    foreach (char c in chars.Keys)
                        font._chars[c] = chars[c];
                    font._space = ((IXFont)font).MeasureString(_spaceString);
                    font._hyphen = ((IXFont)font).MeasureString(_hyphenString);

                    /*try
					{
						using(StreamWriter s = new StreamWriter("Dump\\" + font.Hash + ".txt", false))
						{
							s.WriteLine(font.Hash);
							foreach(Metrix ch in font._chars)
							{
								if(ch != null)
								{
									s.WriteLine("{0} {1} : {2}, {3}, {4}", ch.Id, ((int)ch.Id).ToString("X4"), ch.Xoffset, ch.Yoffset, ch.Xadvance);
									foreach(char c in ch.Kernings.Keys)
									{
										s.WriteLine("\t{0} {1} : {2}", c, ((int)c).ToString("X4"), ch.Kernings[c]);
									}
								}
							}
						}
					}
					catch
					{
					}*/


                    return font;
                }
            }
        }
        private static void Read(XFont font, BinaryReader reader, Dictionary<char, Metrix> chars)
        {
            font._info.Face = reader.ReadString();
            font._info.Size = reader.ReadInt32();
            font._info.Bold = reader.ReadBoolean();
            font._info.Italic = reader.ReadBoolean();

            font._common.LineHeight = reader.ReadInt32();
            font._common.Pages = reader.ReadInt32();

            for (int i = 0; i < font._common.Pages; i++)
            {
                int n = reader.ReadInt32();
                byte[] b = reader.ReadBytes(n);
                font._images.Add(i, DrawingContext.LoadImage(b));
            }

            int charsCount = reader.ReadInt32();

            for (int i = 0; i < charsCount; i++)
            {
                Metrix ch = new Metrix();
                ch.Id = reader.ReadChar();
                ch.X = reader.ReadInt32();
                ch.Y = reader.ReadInt32();
                ch.Width = reader.ReadInt32();
                ch.Height = reader.ReadInt32();
                ch.Xoffset = reader.ReadInt32();
                ch.Yoffset = reader.ReadInt32();
                ch.Xadvance = reader.ReadInt32();
                ch.Page = reader.ReadInt32();
                ch.Image = font._images[ch.Page];
                chars.Add(ch.Id, ch);
            }

            int kerningsCount = reader.ReadInt32();

            for (int i = 0; i < kerningsCount; i++)
            {
                char first = reader.ReadChar();
                char second = reader.ReadChar();
                byte amount = reader.ReadByte();
                int a = amount > 127 ? amount - 255 : amount;
                if (chars.ContainsKey(first))
                {
                    Metrix ch = chars[first];
                    if (ch.Kernings.ContainsKey(second) == false)
                        ch.Kernings.Add(second, a);
                }
            }

        }
        private static string GetExt(bool bold, bool italic)
        {
            if (bold && italic)
                return "BI";
            else if (bold && !italic)
                return "B";
            else if (!bold && italic)
                return "I";
            else return "";
        }
        private static string GetHash(Faces face, int size, bool bold, bool italic)
        {
            return string.Format("{0}{1}{2}", face, size, GetExt(bold, italic));
        }
        #endregion

        #region overridables
        public override string ToString()
        {
            return Hash;
        }
        public override bool Equals(object obj)
        {
            if (obj is IXFont)
                return ((IEquatable<IXFont>)this).Equals((IXFont)obj);
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return Face.GetHashCode() | Size.GetHashCode() | Bold.GetHashCode() | Italic.GetHashCode();
        }
        #endregion

        #region implementations
        int IXFont.Height
        {
            get { return _common.LineHeight; }
        }
        void IXFont.DrawString(object target, XPoint destination, XColor color, string text, int pos, int length)
        {
            DrawString((WriteableBitmap)target, new Point(destination.X, destination.Y), Color.FromArgb(color.A, color.R, color.G, color.B), text, pos, length);
        }
        XSize IXFont.MeasureString(string text, int pos, int length)
        {
            return MeasureString(text, pos, length);
        }
        XSize IXFont.Hyphen
        {
            get { return _hyphen; }
        }
        XSize IXFont.Space
        {
            get { return _space; }
        }
        bool IEquatable<IXFont>.Equals(IXFont other)
        {
            return Hash == other.Hash;
        }
        #endregion

        #region nested
        internal class Metrix
        {
            public char Id;
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int Xoffset;
            public int Yoffset;
            public int Xadvance;
            public int Page;
            public WriteableBitmap Image;
            public Dictionary<char, int> Kernings = new Dictionary<char, int>();
            public override string ToString()
            {
                return Id.ToString();
            }
        }

        public struct InfoStruct
        {
            public string Face { get; internal set; }
            public int Size { get; internal set; }
            public bool Bold { get; internal set; }
            public bool Italic { get; internal set; }
        }

        public struct CommonStruct
        {
            public int LineHeight { get; internal set; }
            public int Pages { get; internal set; }
        }
        #endregion

    }
}
