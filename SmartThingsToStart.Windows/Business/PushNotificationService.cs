using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.PushNotifications;
using Newtonsoft.Json;
using SmartThingsToStart.Client;
using SmartThingsToStart.Entities;

namespace SmartThingsToStart.Business
{
	public class PushNotificationService
	{
		private readonly IPropertySet _settings;
		private readonly PushProxyClient _pushProxy;
		private readonly SmartThingsClient _smartThings;

		public static PushNotificationService Instance { get; } = new PushNotificationService();

		private PushNotificationService()
		{
			_settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
			_pushProxy = PushProxyClient.Instance;
			_smartThings = SmartThingsClient.Instance;
		}

		public async Task<CommutableDeviceChannel> GetOrCreateChannel(CancellationToken ct, CommutableDeviceCommandTile tile, bool forceRenew = false)
		{
			try
			{
				var channel = await PushNotificationChannelManager
					.CreatePushNotificationChannelForSecondaryTileAsync(tile.TileId)
					.AsTask(ct);

				object data;
				var channelData = default(PushNotificationChannelData);
				if (_settings.TryGetValue($"PUSH_{tile.TileId}", out data))
				{
					try
					{
						channelData = JsonConvert.DeserializeObject<PushNotificationChannelData>((string) data);
					}
					catch (Exception)
					{
						// Meh...
					}
				}

				if (forceRenew || (!channelData?.WnsChannelUri?.Equals(channel.Uri, StringComparison.OrdinalIgnoreCase) ?? true))
				{
					try
					{
						channelData = string.IsNullOrWhiteSpace(channelData?.StSubscriptionId)
							? new PushNotificationChannelData(channel.Uri)
							: new PushNotificationChannelData(channel.Uri, channelData.StSubscriptionId);

						var channelUri = new Uri(channel.Uri, UriKind.Absolute);
						var proxyChannel = await _pushProxy.CreateProxyChannel(ct, tile.Arguments.TargetId, channelUri);
						await _smartThings.SetSubscription(ct, tile, channelData.StSubscriptionId, proxyChannel);

						_settings[$"PUSH_{tile.TileId}"] = JsonConvert.SerializeObject(channelData);
					}
					catch (Exception)
					{
						channel.Close();

						throw;
					}
				}

				return new CommutableDeviceChannel(channel);
			}
			catch (Exception error)
			{
				throw new SmartthingsToStartException(SmartThingsToStartExceptionType.OpenPushNotificationChannel, error);
			}
		}
	}


	public enum SmartThingsToStartExceptionType
	{
		PinTile,

		OpenPushNotificationChannel,

		InvalidAppIdentity
	}

	public class SmartthingsToStartException : Exception
	{
		public SmartThingsToStartExceptionType Type { get; }

		public SmartthingsToStartException(SmartThingsToStartExceptionType type, Exception inner)
			: base(type.ToString(), inner)
		{
			Type = type;
		}

		public SmartthingsToStartException(SmartThingsToStartExceptionType type)
			: base(type.ToString())
		{
			Type = type;
		}


	}

	public class PushNotificationChannelData
	{
		public PushNotificationChannelData()
		{
		}

		public PushNotificationChannelData(string wnsChannelUri, string stSubscriptionId)
		{
			WnsChannelUri = wnsChannelUri;
			StSubscriptionId = stSubscriptionId;
		}

		public PushNotificationChannelData(string wnsChannelUri)
			: this(wnsChannelUri, Guid.NewGuid().ToString("D"))
		{
		}

		public string StSubscriptionId { get; set; }

		public string WnsChannelUri { get; set; }
	}

	public class CommutableDeviceChannel
	{
		private readonly PushNotificationChannel _channel;
		private readonly IObservable<CommutableDevice> _stateObservable;

		public CommutableDeviceChannel(PushNotificationChannel channel)
		{
			_channel = channel;

			ChannelUri = new Uri(channel.Uri);

			_stateObservable = Observable
				.FromEventPattern<TypedEventHandler<PushNotificationChannel, PushNotificationReceivedEventArgs>, PushNotificationReceivedEventArgs>(
					h => channel.PushNotificationReceived += h,
					h => channel.PushNotificationReceived -= h)
				.Select(args =>
					{
						var content = args.EventArgs.TileNotification?.Content;
						var xml = content?.GetXml();

						Console.WriteLine(xml);

						var rootElement = content?.FirstChild as XmlElement;
						var deviceData = rootElement?.GetAttribute("x_device");

						return deviceData;
					})
				.Where(deviceData => !string.IsNullOrWhiteSpace(deviceData))
				.Select(deviceData =>
				{
					try
					{
						return JsonConvert.DeserializeObject<CommutableDevice>(Encoding.UTF8.GetString(Convert.FromBase64String(deviceData)));
					}
					catch (Exception)
					{
						return null;
					}
				})
				.Where(device => device != null)
				.Publish()
				.RefCount();
		}

		public Uri ChannelUri { get; }

		public IObservable<CommutableState?> ObserveState()
			=> _stateObservable.Select(device => device.State);

		public async Task Close(CancellationToken ct)
		{
			_channel.Close();
		}
	}
}
