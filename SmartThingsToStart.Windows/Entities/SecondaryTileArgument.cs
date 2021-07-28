using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartThingsToStart.Entities
{
	public class SecondaryTileArgument
	{
		public int Version { get; set; }

		public string LocationId { get; set; }

		public string TargetId { get; set; }

		public SecondaryTileType Type { get; set; }

		public string Command { get; set; }

		public string CommandUri { get; set; }
	}
}