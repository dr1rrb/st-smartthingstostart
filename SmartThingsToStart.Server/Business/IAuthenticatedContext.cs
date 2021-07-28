using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartThingsToStart.Server.Business
{
	public interface IAuthenticatedContext : IDisposable
	{
		HttpClient Client { get; }
		HttpResponseMessage EnsureSuccess(HttpResponseMessage response);
	}
}