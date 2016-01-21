using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FictionBook
{
	public class Body : Element
	{
		private string _name;

		#region Public Properties
		public string Name
		{
			get { return _name; }
			set { SetProperty( ref _name, value, "Name" ); }
		}
		#endregion

		protected override bool ValidateChildInsertion( int index, Element child )
		{
			Type childType = child.GetType();

			return childType == typeof( ImageElement ) ||
				childType == typeof( TitleElement ) ||
				childType == typeof( EpigraphElement ) ||
				childType == typeof( SectionElement );
		}

		/*
		public override string ToString()
		{
			TitleElement content = (TitleElement) Children.FirstOrDefault( p => p is TitleElement );
			return content == default( TitleElement ) ? base.ToString() : content.ToString();
		}
		*/
	}

	public class BodyCollection : KeyedCollection<string, Body>
	{
		protected override string GetKeyForItem( Body item )
		{
			return item.Name;
		}
	}
}