using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Athenaeum.Formatter
{

	#region public class Pointer
	[XmlInclude(typeof(ParagraphPointer))]
	public class Pointer : IEquatable<Pointer>
	{
		public static readonly Pointer Start = new Pointer(-1);

		public int DocumentId { get; set; }
		public int BlockId { get; set; }

		#region Ctor
		public Pointer()
			: this(-1)
		{
		}
		public Pointer(int blockId)
		{
			BlockId = blockId;
		}
		#endregion

		#region ResolvePointerTop
		public virtual int ResolvePointerTop(DocumentFormatter formatter)
		{
			if(this.Equals(Start))
				return 0;
			Block block = formatter.FindBlockById(BlockId);
			return block == null ? -1 : (int)block.Bounds.Top;
		}
		#endregion

		#region Overridables
		public override string ToString()
		{
			return BlockId.ToString();
		}

		public virtual bool Equals(Pointer other)
		{
			return BlockId == other.BlockId;
		}

        public static bool operator <(Pointer p1, Pointer p2)
        {
            return p1.BlockId < p2.BlockId;
        }

        public static bool operator >(Pointer p1, Pointer p2)
        {
            return p1.BlockId > p2.BlockId;
        }

		#endregion

	}
	#endregion

	#region public class ParagraphPointer
	public class ParagraphPointer : Pointer
	{
		public int ItemId { get; set; }

		#region Constructors/Disposer
		public ParagraphPointer()
			: this(-1, -1)
		{
		}
		public ParagraphPointer(int blockId, int itemId)
			: base(blockId)
		{
			ItemId = itemId;
		}
		#endregion

		#region ResolvePointerTop
		public override int ResolvePointerTop(DocumentFormatter formatter)
		{
			ParagraphBlock block = formatter.FindBlockById(BlockId) as ParagraphBlock;

			if(block == null)
				return -1;

			TextLineWord word = null;
			foreach(TextLine line in block.Lines)
			{
				TextLineWord subWord = line.Items.LastOrDefault(w => w.Index == ItemId);
				if(subWord != null)
					word = subWord;
				else if(word != null)
					break;
			}

			return word != null ? (int)word.Bounds.Top : (int)block.Bounds.Top;
		}
		#endregion

		#region Overridables
		public override string ToString()
		{
			return BlockId.ToString() + ":" + ItemId.ToString();
		}
		public virtual bool Equals(Pointer other)
		{
			return base.Equals(other) && (other is ParagraphPointer) ? ItemId == ((ParagraphPointer)other).ItemId : false;
		}
		#endregion

	}
	#endregion
}
