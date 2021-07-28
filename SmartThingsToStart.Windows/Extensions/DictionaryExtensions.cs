using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartThingsToStart.Extensions
{
	public static class DictionaryExtensions
	{
		public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
		{
			TValue value;
			return dictionary.TryGetValue(key, out value)
				? value
				: defaultValue;
		}

		public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
		{
			TValue value;
			return dictionary.TryGetValue(key, out value)
				? value
				: dictionary[key] = factory(key);
		}
	}
}