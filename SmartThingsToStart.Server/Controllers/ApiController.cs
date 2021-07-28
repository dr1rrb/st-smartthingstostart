using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using SmartThingsToStart.Business;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Extensions;
using SmartThingsToStart.Server.Business;
using SmartThingsToStart.Server.Entities;
using SmartThingsToStart.Server.Extensions;

namespace SmartThingsToStart.Server.Controllers
{
	[Route("[controller]")]
	public class ApiController : Controller
	{
		private readonly IWnsAuthenticationService _authenticationService;
		private readonly ISecurityService<AppToProxyToken> _appToProxySecurity;
		private readonly ISecurityService<StToProxyToken> _stToProxySecurity;

		public ApiController(IWnsAuthenticationService authenticationService, ISecurityService<AppToProxyToken> appSecurityService, ISecurityService<StToProxyToken> stToProxySecurity)
		{
			_authenticationService = authenticationService;
			_appToProxySecurity = appSecurityService;
			_stToProxySecurity = stToProxySecurity;
		}

		[HttpPost("channel")]
		public async Task<ProxyNotificationChannel> CreateChannel([FromHeader(Name = "Authorization")] string authHeader, [FromForm(Name = "channelUri")] string channelUri)
		{
			var token = new StToProxyToken
			{
				ChannelUri = channelUri,
				CreationDate = DateTimeOffset.Now,
				AppToProxyToken = ExtractBearer(authHeader),
			};
			var securedToken = _stToProxySecurity.Encrypt(token);

#if DEBUG
			var decryptedChannelUri = GetChannelUri(securedToken, null);
			if (token.ChannelUri != decryptedChannelUri.OriginalString)
			{
				throw new InvalidOperationException("Decryption failed");
			}
#endif

			return new ProxyNotificationChannel
			{
				ChannelUri = $"{EnvironnementHelper.GetServerUri(Request)}/api/push",
				Token = securedToken,
			};
		}

		public enum NotificationResult
		{
			Ok,
			Invalid,
			Failed
		}

		[HttpPost("push")]
		public async Task<ObjectResult> Push([FromBody] EventNotification notification)
		{
			var ct = HttpContext?.RequestAborted ?? CancellationToken.None;

			var sendNotifications = notification
				.Subscriptions
				.Select(async subscription =>
				{
					Uri channelUri;
					try
					{
						channelUri = GetChannelUri(subscription.Token, notification);
					}
					catch (Exception)
					{
						return Tuple.Create(subscription, NotificationResult.Invalid);
					}

					try
					{
						await SendTileNotification(ct, channelUri, notification.Device);
					}
					catch (Exception)
					{
						return Tuple.Create(subscription, NotificationResult.Failed);
					}

					return Tuple.Create(subscription, NotificationResult.Ok);
				});

			var results = (await Task.WhenAll(sendNotifications)).ToDictionary(t => t.Item1.Id, t => t.Item2);

			if (results.Values.Any(r => r == NotificationResult.Invalid))
			{
				return StatusCode(403, results);
			}
			else if (results.Values.Any(r => r == NotificationResult.Failed))
			{
				return StatusCode(421, results);
			}
			else
			{
				return StatusCode(200, results);
			}
		}

		private async Task<StatusCodeResult> SendTileNotification(CancellationToken ct, Uri channel, CommutableDevice device)
		{
			var color = device.Color?.Trim('#');
			var backgroundColor = string.IsNullOrWhiteSpace(color)
				? string.Empty
				: $@"<image src=""https://dummyimage.com/1x1/{color}/{color}.png"" placement=""background""/>";
			var icon = SmartThingsToStart.Extensions.CommutableDeviceExtensions.GetIcon(device);
			var wnsNotification = $@"<tile x_device=""{Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(device)))}"">
				<visual branding=""name"">
					<binding template=""TileSmall"">{backgroundColor}<image src=""{icon}"" /></binding>
					<binding template=""TileMedium"">{backgroundColor}<image src=""{icon}"" /></binding>
					<binding template=""TileWide"">{backgroundColor}<image src=""{icon}"" /></binding>
					<binding template=""TileLarge"">{backgroundColor}<image src=""{icon}"" /></binding>
				</visual>
			</tile>";

			using (var context = await _authenticationService.GetContext(ct))
			using (var content = new StringContent(wnsNotification, Encoding.UTF8, "text/xml") {Headers = {{"X-WNS-Type", "wns/tile"}}})
			{
				await content.LoadIntoBufferAsync();

				using (var response = await context.Client.PostAsync(channel, content, ct))
				{
					context.EnsureSuccess(response);

					return Ok();
				}
			}
		}

		private async Task<StatusCodeResult> SendRawNotification(CancellationToken ct, StToProxyToken token, EventNotification stNotification)
		{
			using (var context = await _authenticationService.GetContext(ct))
			{
				var response = await context.Client.PostAsync(
					new Uri(token.ChannelUri),
					new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(stNotification.Event.Value))) //, Encoding.UTF8, "application/octet-stream")
					{
						Headers = { { "X-WNS-Type", "wns/raw" } }
					},
					ct);

				context.EnsureSuccess(response);

				return Ok();
			}
		}

		private string ExtractBearer(string authHeader)
			=> authHeader?.TrimStart("Bearer ", StringComparison.OrdinalIgnoreCase) ?? string.Empty;

		private Uri GetChannelUri(string encryptedToken, EventNotification notification)
		{
			if (string.IsNullOrWhiteSpace(encryptedToken))
		    {
			    throw new ArgumentNullException("Authorization", "Auth header is empty.");
		    }

			var stToProxyToken = _stToProxySecurity.Decrypt(encryptedToken);
			var appToProxyToken = _appToProxySecurity.Decrypt(stToProxyToken.AppToProxyToken);

			if (Math.Abs((stToProxyToken.CreationDate - appToProxyToken.CreationDate).Ticks) > TimeSpan.FromMinutes(10).Ticks)
			{
				throw new UnauthorizedAccessException("Invalid timestamps.");
			}

			if (appToProxyToken.AppId != Constants.AppId)
			{
				throw new UnauthorizedAccessException("Invalid app id.");
			}

			if (
#if DEBUG
				notification != null &&
#endif
				!appToProxyToken.DeviceId.Equals(notification.Device.Id, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("This channel was not created for this device.");
			}

			if (!Uri.IsWellFormedUriString(stToProxyToken.ChannelUri, UriKind.Absolute))
			{
				throw new ArgumentOutOfRangeException("Authorization", "Invalid channel uri.");
			}

			var channelUri = new Uri(stToProxyToken.ChannelUri, UriKind.Absolute);
			if (!channelUri.Host.EndsWith(WnsAuthenticationService.Scope, StringComparison.CurrentCultureIgnoreCase))
			{
				throw new UnauthorizedAccessException("Invalid channel uri scope.");
			}

			return channelUri;
	    }
    }
}
