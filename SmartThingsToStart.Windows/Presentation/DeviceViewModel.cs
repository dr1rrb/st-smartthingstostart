using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SmartThingsToStart.Business;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Extensions;

namespace SmartThingsToStart.Presentation
{
	public class DeviceViewModel : ViewModelBase
	{
		private readonly CommutableDevice _device;
		private readonly DeviceService _service;

		public DeviceViewModel(CommutableDevice device)
		{
			_service = DeviceService.Instance;
			_device = device;
		}

		public string Location => _device.Location.Name;

		public string Name => _device.Name;

		public CommutableState State => Get(() => _service.GetAndObserveState(_device).Select(state => state.GetValueOrDefault()));

		public string Icon => Get(() => _service.GetAndObserveState(_device).Select(s => _device.GetIcon(s)));

		public ICommand Execute => this.Command((ct, p) => _service.Execute(ct, _device, ToCommand(p)));

		public ICommand PinToStart => this.Command((ct, p) =>  _service.PinToStart(ct, _device, ToCommand(p)));

		private CommutableDeviceCommand ToCommand(object commandParameter)
			=> (CommutableDeviceCommand) Enum.Parse(typeof(CommutableDeviceCommand), commandParameter.ToString(), ignoreCase: true);
	}
}