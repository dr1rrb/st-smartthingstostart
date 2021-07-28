using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartThingsToStart.Server.Business
{
	public interface ISecurityService<T>
	{
		string Encrypt(T value);
		T Decrypt(string value);
	}
}