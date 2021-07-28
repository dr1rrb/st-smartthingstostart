using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartThingsToStart.Entities
{
	public class CommutableDevice
	{
		public Location Location { get; set; }

		public string Id { get; set; }

		public string Name { get; set; }

		public CommutableState? State { get; set; }

		public string Color { get; set; }

		public int? Hue { get; set; }

		public int? Saturation { get; set; }

		public Capability[] Capabilities { get; set; }
	}

	public class Capability
	{
		public string Name { get; set; }
	}

	public static class Capabilities
	{
		public const string Bulb = "Bulb";

		public const string Light = "Light";

		public const string Outlet = "Outlet";

		public const string Switch = "Switch";

		public const string RelaySwitch = "Relay Switch";

		public const string ColorControl = "Color Control";

		public const string PowerMeter = "Power Meter";
	}

	public class ItemsCollection
	{
		public CommutableDevice[] Devices { get; set; }

		public Routine[] Routines { get; set; }
	}

	public class Routine
	{
		public Location Location { get; set; }

		public string Id { get; set; }

		public string Name { get; set; }
	}

	public enum CommutableDeviceCommand
	{
		Toggle,
		On,
		Off
	}
}