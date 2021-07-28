using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartThingsToStart.Server.Entities
{
	public class WnsToken
	{
		[JsonProperty("access_token")]
		public string Value { get; set; }

		[JsonProperty("token_type")]
		public string Type { get; set; }

		[JsonProperty("expires_in")]
		public int Expiration { get; set; }
	}
}