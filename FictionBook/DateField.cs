using System;

namespace FictionBook
{
	public class DateField : TextField
	{
		public DateTime Value { get; set; }

		#region Object Overrides
		public override bool Equals( object obj )
		{
			if( !base.Equals( obj ) )
			{
				return false;
			}

			return Value == ((DateField) obj).Value;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() ^ Value.GetHashCode();
		}
		#endregion
	}
}
