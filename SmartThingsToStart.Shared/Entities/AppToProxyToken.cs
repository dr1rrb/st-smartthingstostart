using System;
using System.Collections.Generic;
using System.Text;

namespace SmartThingsToStart.Entities
{
	public class AppToProxyToken
    {
	    public string AppId { get; set; }

		public string DeviceId { get; set; }

		public DateTimeOffset CreationDate { get; set; }
    }
}
