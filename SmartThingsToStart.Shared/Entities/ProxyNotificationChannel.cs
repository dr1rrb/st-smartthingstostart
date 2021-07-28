using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartThingsToStart.Business
{
	public class ProxyNotificationChannel
	{
		[JsonProperty("channelUri")] // Must be lower cased for ST
		public string ChannelUri { get; set; }

		[JsonProperty("token")] // Must be lower cased for ST
		public string Token { get; set; }
	}
}