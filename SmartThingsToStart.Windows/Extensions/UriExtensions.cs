using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartThingsToStart.Extensions
{
	public static class UriExtensions
	{
		public static IEnumerable<KeyValuePair<string, string>> GetQueryParameters(this Uri uri)
		{
			return uri.Query.TrimStart('?').Split('&').Select(parameter =>
			{
				var keyAndValue = parameter.Split(new[] { '=' }, 2);
				return new KeyValuePair<string, string>(keyAndValue[0], keyAndValue.LastOrDefault());
			});
		}
	}
}