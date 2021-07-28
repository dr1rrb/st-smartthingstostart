using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json;
using SmartThingsToStart.Business;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Server.Business;

namespace SmartThingsToStart.Client
{
	public class PushProxyClient
	{
		public static PushProxyClient Instance { get; } = new PushProxyClient();

		private readonly ISecurityService<AppToProxyToken> _security;

		private PushProxyClient()
		{
			_security = new AesSecurityService<AppToProxyToken>(
				Constants.AppToProxyTokenPwd,
				Constants.AppToProxyTokenSalt);
		}

		public async Task<ProxyNotificationChannel> CreateProxyChannel(CancellationToken ct, string deviceId, Uri channel)
		{
			var token = _security.Encrypt(new AppToProxyToken
			{
				AppId = Constants.AppId,
				DeviceId = deviceId,
				CreationDate = DateTimeOffset.Now
			});
			var response = await new HttpClient
				{
					DefaultRequestHeaders =
					{
						{"Authorization", $"Bearer {token}" }
					}
				}
				.PostAsync(
					new Uri(Constants.ProxyBaseUri + "api/channel"),
					new HttpFormUrlEncodedContent(new Dictionary<string, string>
					{
						{"channelUri", channel.OriginalString}
					}))
				.AsTask(ct);

			var data = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().AsTask(ct);
			return JsonConvert.DeserializeObject<ProxyNotificationChannel>(data);
		}
	}
}
