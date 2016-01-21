using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Athenaeum.Formatter
{
	public static partial class Hyphenator
	{
		const char Marker = '.';
		static bool IsDigit(char c) { return c >= '0' && c <= '9'; }
		static bool IsMarker(char c) { return c == Marker; }
		static char Char2Digit(char c) { return (char)(c - '0'); }
		private static Dictionary<int, Pattern> __patterns;

		static Hyphenator()
		{
			int a = 0;
			__patterns = new Dictionary<int, Pattern>();
			foreach(string s in __patterns_RU_ru)
			{
				Pattern p = new Pattern(s.ToLower());
				try
				{
					__patterns.Add(GetHashCode(p.Str), p);
				}
				catch(Exception)
				{
					//object zz = __patterns.Keys.Where( q => q == Pattern.GeyHashCode( p.pattern ) );
					a++;
				}
			}
		}

		/*private static Dictionary<string, Pattern> Patterns
		{
			get
			{
				if (patterns == null)
				{
					patterns = new Dictionary<string, Pattern>();
					foreach (string s in __patterns_RU_ru)
					{
						Pattern p = new Pattern( s.ToLower() );
						patterns.Add( p.str, p );
					}
				}
				return patterns;
			}
		}*/

		/*public bool[] Hyphenate( string word )
		{
			word = Marker + word.ToLower() + Marker;
			int[] levels = new int[word.Length];
			for (int i = 0; i < word.Length - 2; ++i)
			{
				for (int count = 1; count < word.Length - i; ++count)
				{
					Pattern pattern;
					if (Patterns.TryGetValue( word.Substring( i, count ), out pattern ))
					{
						for (int j = 0; j < pattern.levels.Length; ++j)
						{
							int l = pattern.levels[j];
							if (l > levels[i + j])
								levels[i + j] = l;
						}
					}
				}
			}
			int size = levels.Length - 2;
			bool[] buffer = new bool[size];
			for (int i = 0; i < size; ++i)
			{
				if (levels[i + 1] % 2 != 0 && i != 0)
					buffer[i] = true;
			}
			return buffer;
		}*/

		/*public bool[] Hyphenate2( string word, int position, int length )
		{
			word = ( Marker + word.Substring( position, length ) + Marker );
			int[] levels = new int[word.Length];
			for (int i = 0; i < word.Length - 2; ++i)
			{
				for (int count = 1; count < word.Length - i; ++count)
				{
					int h = Pattern.GetHashCode( word, i, count );
					Pattern pattern;
					if (__patterns.TryGetValue( h, out pattern ))
					{
						for (int j = 0; j < pattern.levels.Length; ++j)
						{
							int l = pattern.levels[j];
							if (l > levels[i + j])
								levels[i + j] = l;
						}
					}
				}
			}
			int size = levels.Length - 2;
			bool[] buffer = new bool[size];
			for (int i = 0; i < size; ++i)
			{
				if (levels[i + 1] % 2 != 0 && i != 0)
					buffer[i] = true;
			}
			return buffer;
		}*/

		public static bool[] Hyphenate(string word)
		{
			return Hyphenate(word, 0, word.Length);
		}

		public static bool[] Hyphenate(string word, int position, int length)
		{
			if(length <= 3)
				return new bool[length];

			int origilalLength = length;

			if(char.IsPunctuation(word[position + length - 1]))
			{
				length--;
				if(length <= 3)
					return new bool[origilalLength];
			}

			int[] levels = new int[length + 2];
			for(int i = 0; i < levels.Length - 2; ++i)
			{
				for(int count = 1; count < levels.Length - i; ++count)
				{
					int hash = 0;
					if(i == 0)
					{
						hash = GetHashCode(Marker);
						if(count > 1)
							hash = GetHashCode(word, position + i, count - 1, hash);
					}
					else
					{
						hash = GetHashCode(word, position + i - 1, count - 1);
					}
					Pattern pattern;
					if(__patterns.TryGetValue(hash, out pattern))
					{
						for(int j = 0; j < pattern.Levels.Length; ++j)
						{
							int l = pattern.Levels[j];
							if(l > levels[i + j])
								levels[i + j] = l;
						}
					}
				}
			}
			int size = levels.Length - 2;
			bool[] buffer = new bool[origilalLength];
			for(int i = 0; i < size; ++i)
			{
				if(levels[i + 1] % 2 != 0 && i != 0)
					buffer[i] = true;
			}
			return buffer;
		}

		/*public bool[] Hyphenate( string input, int position, int length )
		{

			string word = Marker + input.Substring( position, length ).ToLower() + Marker;
			int[] levels = new int[word.Length];
			for (int i = 0; i < word.Length - 2; ++i)
			{
				for (int count = 1; count < word.Length - i; ++count)
				{
					Pattern pattern;
					if (Patterns.TryGetValue( word.Substring( i, count ), out pattern ))
					{
						for (int j = 0; j < pattern.levels.Length; ++j)
						{
							int l = pattern.levels[j];
							if (l > levels[i + j])
								levels[i + j] = l;
						}
					}
				}
			}
			int size = levels.Length - 2;
			bool[] buffer = new bool[size];
			for (int i = 0; i < size; ++i)
			{
				if (levels[i + 1] % 2 != 0 && i != 0)
					buffer[i] = true;
			}
			return buffer;
		}*/

		/*static int GetHashCode( string value, int offset = 0, int count = -1, int mul = 0 )
		{
			if (count < 0)
				count = value.Length;
			int hash = 0, multiplier = 1;
			for (; multiplier > 0; multiplier--)
				multiplier = ( multiplier << 5 ) - multiplier;
			for (int i = offset; i < offset + count; i++)
			{
				hash += char.ToLower( value[i] ) * multiplier;
				multiplier = ( multiplier << 5 ) - multiplier;
			}
			return hash;
		}

		static int GetHashCode( char c, int mul = 0 )
		{
			int multiplier = 1;
			for (; mul > 0; mul--)
				multiplier = ( multiplier << 5 ) - multiplier;
			return char.ToLower( c ) * multiplier;
		}*/

		static int GetHashCode(char c, int multiplier = 5381)
		{
			return multiplier = ((multiplier << 5) + multiplier) ^ char.ToLower(c);
		}

		static int GetHashCode(string value, int offset = 0, int count = -1, int multiplier = 5381)
		{
			if(count < 0)
				count = value.Length;
			for(int i = offset; i < offset + count; ++i)
				multiplier = ((multiplier << 5) + multiplier) ^ char.ToLower(value[i]);
			return multiplier;
		}

		#region Pattern descriptor
		class Pattern : IComparable<Pattern>
		{
			internal string Str;
			internal int[] Levels;

			internal Pattern(string s)
			{
				bool waitDigit = true;
				List<char> ss = new List<char>();
				List<int> ll = new List<int>();
				for(int i = 0; i < s.Length; i++)
				{
					char c = s[i];
					if(IsDigit(c))
					{
						ll.Add(Char2Digit(c));
						waitDigit = false;
					}
					else
					{
						if(!IsMarker(c) && waitDigit)
							ll.Add(0);
						ss.Add(c);
						waitDigit = true;
					}
				}
				if(waitDigit)
					ll.Add(0);
				Str = new string(ss.ToArray());
				Levels = ll.ToArray();
			}

			int IComparable<Pattern>.CompareTo(Pattern other)
			{
				return Str.CompareTo(other.Str);
			}

			public override string ToString()
			{
				return Str;
			}

			public override bool Equals(object obj)
			{
				Pattern p = obj as Pattern;
				if(p != null)
					return object.Equals(Str, p.Str);
				return base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return Hyphenator.GetHashCode(Str);
			}

		}
		#endregion
	}
}
