using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SmartThingsToStart.Business;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Presentation;

namespace SmartThingsToStart.Client
{
	public class SmartThingsClient
	{
		private RepositoryService _repository;
		public static SmartThingsClient Instance { get; } = new SmartThingsClient();

		private SmartThingsClient()
		{
			_repository = RepositoryService.Instance;
		}

		public async Task<ItemsCollection> GetItems(CancellationToken ct, Repository repository)
		{
			return  await repository.Get<ItemsCollection>(ct, $"items");
		}

		public async Task<CommutableDevice> Get(CancellationToken ct, CommutableDevice device)
		{
			return await (await GetRepository(ct, device)).Get<CommutableDevice>(ct, $"device/{device.Id}");
		}

		public async Task<string> GetDirectExecuteUri(CancellationToken ct, CommutableDevice device, CommutableDeviceCommand command)
		{
			return Path.Combine((await GetRepository(ct, device)).BaseUri, $"device/{device.Id}/{command.ToString().ToLowerInvariant()}");
		}

		public async Task<string> GetDirectExecuteUri(CancellationToken ct, Routine routine)
		{
			return Path.Combine((await GetRepository(ct, routine)).BaseUri, $"routine/{routine.Id}/execute");
		}

		public async Task Execute(CancellationToken ct, CommutableDevice device, CommutableDeviceCommand command)
		{
			await (await GetRepository(ct, device)).Put(ct, $"device/{device.Id}/{command.ToString().ToLowerInvariant()}");
		}

		public async Task Execute(CancellationToken ct, Routine routine)
		{
			await (await GetRepository(ct, routine)).Put(ct, $"routine/{routine.Id}/execute");
		}

		public async Task SetSubscription(CancellationToken ct, CommutableDeviceCommandTile tile, string subscriptionId, ProxyNotificationChannel proxyChannel)
		{
			await (await GetRepository(ct, tile.Arguments.LocationId)).Put(
				ct, 
				$"device/{tile.Arguments.TargetId}/subscription/{subscriptionId}", 
				new StringContent(JsonConvert.SerializeObject(proxyChannel)));
		}

		private async Task<Repository> GetRepository(CancellationToken ct, CommutableDevice device)
			=> await _repository.GetRepository(ct, device.Location);

		private async Task<Repository> GetRepository(CancellationToken ct, Routine routine)
			=> await _repository.GetRepository(ct, routine.Location);

		private async Task<Repository> GetRepository(CancellationToken ct, string locationId)
			=> await _repository.GetRepository(ct, locationId);

	}
}
