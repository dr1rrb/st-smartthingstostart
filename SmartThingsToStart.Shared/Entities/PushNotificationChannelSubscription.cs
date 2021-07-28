using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartThingsToStart.Entities
{
	public class PushNotificationChannelSubscription
	{
		[JsonProperty("subscriptionId")]
		public string Id { get; set; }
	}
}