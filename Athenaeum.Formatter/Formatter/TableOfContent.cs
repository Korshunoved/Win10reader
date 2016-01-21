using System;
using System.Linq;
using System.Collections.Generic;

namespace Athenaeum.Formatter
{

	public interface ITableOfContentNodeContainer
	{
		IList<TableOfContentNode> Nodes { get; }
	}

	public enum TableOfContentBuildMode { Tree, Plain }

	public sealed class TableOfContent : ITableOfContentNodeContainer
	{

		public IList<TableOfContentNode> Nodes { get; private set; }

		internal TableOfContent()
		{
			Nodes = new List<TableOfContentNode>();
		}
	}

	public sealed class TableOfContentNode : ITableOfContentNodeContainer
	{

		public Pointer Pointer { get; internal set; }
		public int PageNumber { get; internal set; }
		public string Text { get; internal set; }
		public IList<TableOfContentNode> Nodes { get; private set; }

		internal TableOfContentNode()
		{
			Nodes = new List<TableOfContentNode>();
		}

		internal TableOfContentNode( string text, Pointer pointer, int pageNumber )
			: this()
		{
			Pointer = pointer;
			PageNumber = pageNumber;
			Text = text;
		}

		public override string ToString()
		{
			return ( Text == null ? base.ToString() : Text ) + " - " + PageNumber;
		}

	}

}