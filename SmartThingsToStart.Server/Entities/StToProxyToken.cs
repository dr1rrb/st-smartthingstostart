using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartThingsToStart.Server.Entities
{
	public class StToProxyToken
	{
		public string ChannelUri { get; set; }
		public DateTimeOffset CreationDate { get; set; }
		public string AppToProxyToken { get; set; }
	}
}