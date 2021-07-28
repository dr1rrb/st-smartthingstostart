using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartThingsToStart.Server.Business
{
	public interface IWnsAuthenticationService
	{
		Task<IAuthenticatedContext> GetContext(CancellationToken ct);
	}
}