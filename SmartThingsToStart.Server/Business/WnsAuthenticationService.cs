using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartThingsToStart.Server.Controllers;
using SmartThingsToStart.Server.Entities;
using SmartThingsToStart.Utils;

namespace SmartThingsToStart.Server.Business
{
	public class WnsAuthenticationService : IWnsAuthenticationService
	{
		public const string Scope = "notify.windows.com";

		private readonly HttpClient _unauthorizedClient = new HttpClient();
		private readonly AsyncLock _contextGate = new AsyncLock();
		private WnsContext _context;

		public async Task<IAuthenticatedContext> GetContext(CancellationToken ct)
		{
			var context = _context;
			if (context != null && context.IsValid())
			{
				return context;
			}

			using (await _contextGate.LockAsync(ct))
			{
				context = _context;
				if (context != null && context.IsValid())
				{
					return context;
				}

				return _context = await CreateContext(CancellationToken.None);
			}
		}

		private async Task<WnsContext> CreateContext(CancellationToken ct)
		{
			var response = await _unauthorizedClient.PostAsync(
				new Uri("https://login.live.com/accesstoken.srf"),
				new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{"grant_type", "client_credentials"},
					{"client_id", Constants.ProxyToWnsId},
					{"client_secret", Constants.ProxyToWnsSecret},
					{"scope", Scope}
				}),
				ct);
			var data = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
			var token = JsonConvert.DeserializeObject<WnsToken>(data);

			var client = new HttpClient
			{
				DefaultRequestHeaders =
				{
					{"Authorization", $"Bearer {token.Value}"}
				}
			};
			var expiration = DateTimeOffset.Now.AddSeconds(token.Expiration).AddMinutes(-1);

			return new WnsContext(this, client, expiration);
		}

		private class WnsContext : IDisposable, IAuthenticatedContext
		{
			private readonly DateTimeOffset _expiration;
			private readonly WnsAuthenticationService _service;

			public WnsContext(WnsAuthenticationService service, HttpClient client, DateTimeOffset expiration)
			{
				_service = service;
				_expiration = expiration;
				Client = client;
			}

			public HttpClient Client { get; }

			public bool IsValid() => DateTimeOffset.Now < _expiration;

			public HttpResponseMessage EnsureSuccess(HttpResponseMessage response)
			{
				if (response.StatusCode == HttpStatusCode.Unauthorized)
				{
					Interlocked.CompareExchange(ref _service._context, null, this);
				}

				return response.EnsureSuccessStatusCode();
			}

			public void Dispose()
			{
			}
		}
	}
}