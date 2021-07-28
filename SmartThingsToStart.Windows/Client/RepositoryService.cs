using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartThingsToStart.Business;
using SmartThingsToStart.Client;
using SmartThingsToStart.Entities;

namespace SmartThingsToStart.Client
{
	public class RepositoryService
	{
		public static RepositoryService Instance { get; } = new RepositoryService();

		private Lazy<Task<Repository[]>> _repositories;
		private readonly AuthenticationService _authenticationService;

		private RepositoryService()
		{
			_authenticationService = AuthenticationService.Instance;
		}

		public async Task<Repository> GetRepository(CancellationToken ct, Location location)
		{
			var repositories = await GetRepositories(ct);
			return repositories.FirstOrDefault(r => r.Location == location)
				?? repositories.First(r => r.Location.Id.Equals(location.Id, StringComparison.OrdinalIgnoreCase));
		}

		public async Task<Repository> GetRepository(CancellationToken ct, string locationId)
		{
			var repositories = await GetRepositories(ct);
			return repositories.First(r => r.Location.Id.Equals(locationId, StringComparison.OrdinalIgnoreCase));
		}

		public async Task<Repository[]> GetRepositories(CancellationToken ct)
		{
			var repositories = _repositories;
			if (repositories == null)
			{
				repositories = new Lazy<Task<Repository[]>>(() => GetRepositoriesCore(CancellationToken.None), true);
				repositories = Interlocked.CompareExchange(ref _repositories, repositories, null) ?? repositories;
			}

			try
			{
				return await repositories.Value;
			}
			catch (Exception)
			{
				Interlocked.CompareExchange(ref _repositories, null, repositories);
				throw;
			}
		}

		private async Task<Repository[]> GetRepositoriesCore(CancellationToken ct)
		{
			HttpClient client;
			Endpoint[] endpoints;
			try
			{
				client = await _authenticationService.GetToken(ct);
				endpoints = await ValidateEndpoints(ct, client);
			}
			catch (Exception)
			{
				// Retry once after having POTENTIALLY cleared the password vault
				client = await _authenticationService.GetToken(ct);
				endpoints = await ValidateEndpoints(ct, client);
			}

			return endpoints
				.Select(endpoint => new Repository(client, endpoint))
				.ToArray();
		}

		private async Task<Endpoint[]> ValidateEndpoints(CancellationToken ct, HttpClient client)
		{
			var uri = "https://graph.api.smartthings.com/api/smartapps/endpoints";
			var response = await client.GetAsync(new Uri(uri), ct);

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				await _authenticationService.Clear(ct);
			}

			var data = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
			var endpoints = JsonConvert.DeserializeObject<Endpoint[]>(data);

			if (!endpoints.Any())
			{
				await _authenticationService.Clear(ct);
			}

			return endpoints;
		}
	}
}