using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartThingsToStart.Entities;

namespace SmartThingsToStart.Extensions
{
	public static class CommutableDeviceExtensions
	{
		public static bool HasCapability(this CommutableDevice device, string capability)
			=> device.Capabilities.Any(c => c.Name.Equals(capability, StringComparison.OrdinalIgnoreCase));


		public static string GetIcon(this CommutableDevice device)
			=> GetIcon(device, device.State);

		public static string GetIcon(this CommutableDevice device, CommutableState? state)
		{
			var icon = "st.Home.home30";
			if (device.HasCapability(Capabilities.Light)
				|| device.HasCapability(Capabilities.ColorControl))
			{
				icon = "st.switches.light";
			}
			else if (device.HasCapability(Capabilities.PowerMeter))
			{
				icon = "st.switches.switch";
			}

			var stateName = state.HasValue
				&& (state.Value == CommutableState.On || state.Value == CommutableState.TurningOn)
				? "on"
				: "off";

			return $"ms-appx:///Assets/{icon}.{stateName}.png";
		}

	}
}
