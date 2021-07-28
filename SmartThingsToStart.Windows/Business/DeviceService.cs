using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using SmartThingsToStart.Client;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Extensions;

namespace SmartThingsToStart.Business
{
	public class DeviceService
	{
		public static DeviceService Instance { get; } = new DeviceService();

		private readonly SmartThingsClient _client;
		private readonly RepositoryService _repository;
		private readonly PushNotificationService _push;
		private readonly TileService _tiles;

		private ImmutableDictionary<string, ImmutableList<CommutableDeviceChannel>> _channels = ImmutableDictionary<string, ImmutableList<CommutableDeviceChannel>>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
		private readonly Subject<Unit> _channelsChanged = new Subject<Unit>();
		private readonly IObservable<Dictionary<string, CommutableState?>> _statePulling;
		private readonly Subject<Dictionary<string, CommutableState?>> _statePullingObserver = new Subject<Dictionary<string, CommutableState?>>();
		private readonly Subject<Unit> _deviceChanged = new Subject<Unit>();

		private DeviceService()
		{
			_repository = RepositoryService.Instance;
			_client = SmartThingsClient.Instance;
			_push = PushNotificationService.Instance;
			_tiles = TileService.Instance;

			_statePulling = Observable
				.DeferAsync(async ct =>
				{
					var repositories = await _repository.GetRepositories(ct);

					return Observable
						.Timer(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30), Scheduler.Default)
						.Select(_ => Unit.Default)
						.Merge(_deviceChanged)
						.Select(_ => Observable.FromAsync(async ct2 =>
						{
							var results = await Task.WhenAll(repositories.Select(r => _client.GetItems(ct2, r)));

							return results
								.SelectMany(result => result.Devices)
								.Distinct(DeviceIdComparer.Instance)
								.ToDictionary(device => device.Id, device => device.State, StringComparer.OrdinalIgnoreCase);
						}))
						.Switch();
				})
				.Retry(TimeSpan.FromSeconds(20 /* + 20 inital !*/), Scheduler.Default)
				.Do(_statePullingObserver)
				.Replay(1, Scheduler.Default)
				.RefCount();
		}

		public class DeviceIdComparer : EqualityComparer<CommutableDevice>
		{
			public static DeviceIdComparer Instance { get; } = new DeviceIdComparer();

			public override bool Equals(CommutableDevice x, CommutableDevice y) => x.Id.Equals(y.Id, StringComparison.OrdinalIgnoreCase);

			public override int GetHashCode(CommutableDevice obj) => obj.Id.GetHashCode();
		}

		public void Start()
		{
			_deviceChanged.OnNext(Unit.Default);
			RenewSubscriptions(CancellationToken.None);
		}

		public async Task<ItemsCollection> GetItems(CancellationToken ct)
		{
			var repositories = await _repository.GetRepositories(ct);
			var itemsTask = repositories
				.Select(async repository =>
				{
					var items = await _client.GetItems(ct, repository);
					foreach (var device in items.Devices)
					{
						device.Location = repository.Location;
					}
					foreach (var routine in items.Routines)
					{
						routine.Location = repository.Location;
					}
					return items;
				})
				.ToArray();

			await Task.WhenAll(itemsTask);

			return new ItemsCollection
			{
				Devices = itemsTask.SelectMany(t => t.Result.Devices).OrderBy(device => device.Name).ToArray(),
				Routines = itemsTask.SelectMany(t => t.Result.Routines).OrderBy(routine => routine.Name).ToArray()
			};
		}

		public async Task Execute(CancellationToken ct, CommutableDevice device, CommutableDeviceCommand command)
		{
			await _client.Execute(ct, device, command);

			_deviceChanged.OnNext(Unit.Default);
		}

		public async Task Execute(CancellationToken ct, Routine routine)
		{
			await _client.Execute(ct, routine);

			_deviceChanged.OnNext(Unit.Default);
		}

		public IObservable<CommutableState?> GetAndObserveState(CommutableDevice device)
		{
			return _channelsChanged
				.Select(_ => _channels.GetValueOrDefault(device.Id)?.FirstOrDefault())
				.Where(channel => channel != null)
				.DistinctUntilChanged()
				.Select(channel => channel.ObserveState().Merge(_statePullingObserver.Select(states => states.GetValueOrDefault(device.Id))))
				.StartWith(Scheduler.Default, _statePulling.Select(states => states.GetValueOrDefault(device.Id)))
				.Switch()
				.StartWith(Scheduler.Default, device.State.GetValueOrDefault(CommutableState.Undefined))
				.DistinctUntilChanged();
		}

		public async Task PinToStart(CancellationToken ct, CommutableDevice device, CommutableDeviceCommand command)
		{
			var directCommandUri = await _client.GetDirectExecuteUri(ct, device, command);
			var tile = await _tiles.PinToStart(ct, device, command, directCommandUri);

			var channel = await _push.GetOrCreateChannel(ct, tile, forceRenew: true);

			AddChannel(device.Id, channel);
		}

		public async Task PinToStart(CancellationToken ct, Routine routine)
		{
			var directCommandUri = await _client.GetDirectExecuteUri(ct, routine);
			await _tiles.PinToStart(ct, routine, directCommandUri);
		}

		public async Task RenewSubscriptions(CancellationToken ct)
		{
			try
			{
				var tiles = await _tiles.GetPinedDevices(ct);

				foreach (var tile in tiles) // We want to restore them one by one
				{
					try
					{
						AddChannel(tile.Arguments.TargetId, await _push.GetOrCreateChannel(ct, tile));
					}
					catch (Exception)
					{
						// Meh
					}
				}
			}
			catch (Exception)
			{
				// MEEEEHHH ....
			}
		}

		private void AddChannel(string deviceId, CommutableDeviceChannel channel)
		{
			Transactional.Update(ref _channels, channels => channels.SetItem(deviceId, channels.GetValueOrDefault(deviceId, ImmutableList<CommutableDeviceChannel>.Empty).Add(channel)));

			_channelsChanged.OnNext(Unit.Default);
		}
	}

	public class Transactional
	{
		public static T Update<T>(ref T value, Func<T, T> update)
			where T : class
		{
			while (true)
			{
				var capture = value;
				var updated = update(value);
				if (Interlocked.CompareExchange(ref value, updated, capture) == capture)
				{
					return updated;
				}
			}
		}
	}
}
