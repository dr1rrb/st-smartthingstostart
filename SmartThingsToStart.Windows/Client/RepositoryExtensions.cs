using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SmartThingsToStart.Client
{
	public static class RepositoryExtensions
	{
		public static Task<T> Post<T>(this Repository repository, CancellationToken ct, string path, IDictionary<string, string> form)
			=> repository.Post<T>(ct, path, new FormUrlEncodedContent(form));

		public static Task<T> Post<T>(this Repository repository, CancellationToken ct, string path, string key, string value)
			=> repository.Post<T>(ct, path, new FormUrlEncodedContent(new Dictionary<string, string> { { key, value } }));

		public static Task<T> Post<T>(this Repository repository, CancellationToken ct, string path)
			=> repository.Post<T>(ct, path, new StringContent(string.Empty));

		public static Task Put(this Repository repository, CancellationToken ct, string path, IDictionary<string, string> form)
			=> repository.Put(ct, path, new FormUrlEncodedContent(form));

		public static Task Put(this Repository repository, CancellationToken ct, string path, string key, string value)
			=> repository.Put(ct, path, new FormUrlEncodedContent(new Dictionary<string, string> { { key, value } }));

		public static Task Put(this Repository repository, CancellationToken ct, string path)
			=> repository.Put(ct, path, new StringContent(string.Empty));
	}
}