using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartThingsToStart.Entities
{
	public class AppToStToken
	{
		[JsonProperty("access_token")]
		public string Value { get; set; }

		[JsonProperty("token_type")]
		public string Type { get; set; }

		[JsonProperty("expires_in")]
		public int Expiration { get; set; }

		[JsonProperty("scope")]
		public string Scope { get; set; }
	}
}