using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartThingsToStart.Server.Extensions
{
	public static class StringExtensions
	{
		public static string TrimStart(this string source, string value, StringComparison comparison)
		{
			if (source.StartsWith(value, comparison))
			{
				return source.Substring(value.Length);
			}
			else
			{
				return source;
			}
		}
	}
}