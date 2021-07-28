using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SmartThingsToStart.Business;
using SmartThingsToStart.Entities;

namespace SmartThingsToStart.Presentation
{
	public class DeviceListViewModel : ViewModelBase
	{
		private readonly DeviceService _deviceService;

		public DeviceListViewModel()
		{
			_deviceService = DeviceService.Instance;

			Load.Execute(null);
		}

		public IEnumerable<ItemsGroupViewModel> Items
		{
			get { return Get<IEnumerable<ItemsGroupViewModel>>(); }
			private set { Set(value); }
		}

		public ICommand Load => this.Command(ExecuteLoad);

		private async Task ExecuteLoad(CancellationToken ct)
		{
			var items = await _deviceService.GetItems(ct);

			var routines = items.Routines.Select(routine => new RoutineViewModel(routine) as ViewModelBase).ToArray();
			var devices = items.Devices.Select(device => new DeviceViewModel(device) as ViewModelBase).ToArray();

			Items = new[]
			{
				new ItemsGroupViewModel("Routines", routines),
				new ItemsGroupViewModel("Devices", devices)
			};
		}
	}

	public class ItemsGroupViewModel : IEnumerable<ViewModelBase>
	{
		private readonly ViewModelBase[] _items;

		public ItemsGroupViewModel(string name, ViewModelBase[] items)
		{
			Name = name;
			_items = items;
		}

		public string Name { get; }

		public IEnumerator<ViewModelBase> GetEnumerator() => ((IEnumerable<ViewModelBase>)_items).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
	}

	//public class Group<TKey, TValue> : IGrouping<TKey, TValue>
	//{
	//	private readonly TValue[] _values;

	//	public Group(TKey key, TValue[] values)
	//	{
	//		_values = values;
	//		Key = key;
	//	}

	//	public TKey Key { get; set; }

		
	//}

	public class RoutineViewModel : ViewModelBase
	{
		private readonly DeviceService _service;
		private readonly Routine _routine;

		public RoutineViewModel(Routine routine)
		{
			_service = DeviceService.Instance;
			_routine = routine;
		}

		public string Location => _routine.Location.Name;

		public string Name => _routine.Name;

		public ICommand Execute => this.Command((ct, p) => _service.Execute(ct, _routine));

		public ICommand PinToStart => this.Command((ct, p) => _service.PinToStart(ct, _routine));
	}
}
