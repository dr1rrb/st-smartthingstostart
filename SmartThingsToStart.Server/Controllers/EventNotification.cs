using System;
using System.Linq;
using System.Threading.Tasks;
using SmartThingsToStart.Entities;

namespace SmartThingsToStart.Server.Controllers
{
	public class EventNotification
	{
		public Location Location { get; set; }

		public CommutableDevice Device { get; set; }

		public EventData Event { get; set; }

		public EventSubscription[] Subscriptions { get; set; }
	}

	public class EventData
	{
		public EventNotificationSource Source { get; set; }

		//public string Id { get; set; }

		public DateTimeOffset Date { get; set; }

		public string Value { get; set; }

		public string Name { get; set; }

		//public string Description { get; set; }

		//public string DisplayName { get; set; }

		//public string DisplayDescription { get; set; }
	}

	public class EventSubscription
	{
		public string Id { get; set; }

		public string Token { get; set; }
	}
}