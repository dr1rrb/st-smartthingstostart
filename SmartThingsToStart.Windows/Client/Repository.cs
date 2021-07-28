using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartThingsToStart.Entities;

namespace SmartThingsToStart.Client
{
	public class Repository
	{
		private readonly HttpClient _client;
		private readonly Endpoint _endpoint;

		public Repository(HttpClient client, Endpoint endpoint)
		{
			_client = client;
			_endpoint = endpoint;
		}

		public Location Location => _endpoint.Location;

		public string BaseUri => _endpoint.Uri;

		public async Task<T> Get<T>(CancellationToken ct, string path)
		{
			var uri = new Uri(Path.Combine(_endpoint.Uri, path));
			var response = await _client.GetAsync(uri, ct);
			var data = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<T>(data);
		}

		public async Task Post(CancellationToken ct, string path, HttpContent content)
		{
			var uri = new Uri(Path.Combine(_endpoint.Uri, path));
			var response = await _client.PostAsync(uri, content, ct);

			response.EnsureSuccessStatusCode();
		}

		public async Task<T> Post<T>(CancellationToken ct, string path, HttpContent content)
		{
			var uri = new Uri(Path.Combine(_endpoint.Uri, path));
			var response = await _client.PostAsync(uri, content, ct);
			var data = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

			return JsonConvert.DeserializeObject<T>(data);
		}

		public async Task Put(CancellationToken ct, string path, HttpContent content)
		{
			var uri = new Uri(Path.Combine(_endpoint.Uri, path));
			var response = await _client.PutAsync(uri, content, ct);

			response.EnsureSuccessStatusCode();
		}
	}
}