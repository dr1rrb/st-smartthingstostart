using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using SmartThingsToStart.Business;

namespace SmartThingsToStart.Presentation
{
	public class SmartThingsSetupViewModel : ViewModelBase
	{
		public string ClientId { get; set; }

		public string ClientSecret { get; set; }

		public ICommand Continue => this.Command(ct => AuthenticationService.Instance.SetupAppIdentity(ct, ClientId, ClientSecret));
	}
}
