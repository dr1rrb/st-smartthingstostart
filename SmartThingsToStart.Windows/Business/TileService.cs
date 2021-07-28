using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Newtonsoft.Json;
using SmartThingsToStart.Client;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Extensions;

namespace SmartThingsToStart.Business
{
	public class TileService
	{
		private readonly RepositoryService _repositoryService;
		private readonly AuthenticationService _authenticationService;

		public static TileService Instance { get; } = new TileService();

		private TileService()
		{
			_authenticationService = AuthenticationService.Instance;
			_repositoryService = RepositoryService.Instance;
		}

		public async void Handle(string arguments)
		{
			if (string.IsNullOrWhiteSpace(arguments))
			{
				return;
			}

			var ct = CancellationToken.None;
			try
			{
				var args = JsonConvert.DeserializeObject<SecondaryTileArgument>(arguments);

				switch (args.Type)
				{
					case SecondaryTileType.Switch:
						await HandleSwitch(ct, args);
						break;

					case SecondaryTileType.Routine:
						await HandleRoutine(ct, args);
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(args.Type), "Unkown device type");
				}
			}
			catch (Exception)
			{
				await new MessageDialog("Failed to handle activation arguments", "Error").ShowAsync().AsTask(ct);
			}
		}

		private async Task HandleSwitch(CancellationToken ct, SecondaryTileArgument arguments)
		{
			if (string.IsNullOrWhiteSpace(arguments.CommandUri))
			{
				var repositories = await _repositoryService.GetRepositories(ct);
				var endpoint = repositories.First(r => r.Location.Id.Equals(arguments.LocationId, StringComparison.OrdinalIgnoreCase));
				await endpoint.Put(ct, $"device/{arguments.TargetId}/{arguments.Command}");
			}
			else // Fast path: directly execute the command uri
			{
				var user = await _authenticationService.GetToken(ct);
				(await user.PutAsync(new Uri(arguments.CommandUri), new StringContent(""), ct)).EnsureSuccessStatusCode();
			}
		}

		private async Task HandleRoutine(CancellationToken ct, SecondaryTileArgument arguments)
		{
			if (string.IsNullOrWhiteSpace(arguments.CommandUri))
			{
				var repositories = await _repositoryService.GetRepositories(ct);
				var endpoint = repositories.First(r => r.Location.Id.Equals(arguments.LocationId, StringComparison.OrdinalIgnoreCase));
				await endpoint.Put(ct, $"routine/{arguments.TargetId}/{arguments.Command}");
			}
			else // Fast path: directly execute the command uri
			{
				var user = await _authenticationService.GetToken(ct);
				(await user.PutAsync(new Uri(arguments.CommandUri), new StringContent(""), ct)).EnsureSuccessStatusCode();
			}
		}

		public async Task<CommutableDeviceCommandTile> PinToStart(CancellationToken ct, CommutableDevice device, CommutableDeviceCommand command, string directCommandUri = null)
		{
			try
			{
				var tileId = $"{device.Id}_{command.ToString().ToLowerInvariant()}";
				var tiles = await SecondaryTile.FindAllForPackageAsync().AsTask(ct);
				var tile = tiles.FirstOrDefault(t => t.TileId == tileId);
				if (tile == null)
				{
					var args = new SecondaryTileArgument
					{
						Version = 1,
						LocationId = device.Location.Id,
						Type = SecondaryTileType.Switch,
						TargetId = device.Id,
						Command = command.ToString().ToLowerInvariant(),
						CommandUri = directCommandUri
					};

					tile = new SecondaryTile(tileId)
					{
						DisplayName = device.Name,
						Arguments = JsonConvert.SerializeObject(args),
						VisualElements =
					{
						ShowNameOnSquare150x150Logo = true,
						ShowNameOnWide310x150Logo = true,
						ShowNameOnSquare310x310Logo = true,
						Square44x44Logo = new Uri(device.GetIcon()),
						Square71x71Logo = new Uri(device.GetIcon()),
						Square150x150Logo = new Uri(device.GetIcon()),
						Square310x310Logo = new Uri(device.GetIcon()),
					}
					};

					if (device.Capabilities.Any(c => c.Name.Equals(Capabilities.ColorControl, StringComparison.OrdinalIgnoreCase)))
					{
						tile.VisualElements.ForegroundText = ForegroundText.Dark;
					}

					await tile.RequestCreateAsync().AsTask(ct);

					return new CommutableDeviceCommandTile(tileId, args);
				}
				else
				{
					return new CommutableDeviceCommandTile(tile.TileId, JsonConvert.DeserializeObject<SecondaryTileArgument>(tile.Arguments));
				}
			}
			catch (Exception error)
			{
				throw new SmartthingsToStartException(SmartThingsToStartExceptionType.PinTile, error);
			}
		}

		public async Task PinToStart(CancellationToken ct, Routine routine, string directCommandUri = null)
		{
			try
			{
				var tileId = $"{routine.Id}_execute";
				var tiles = await SecondaryTile.FindAllForPackageAsync().AsTask(ct);
				var tile = tiles.FirstOrDefault(t => t.TileId == tileId);
				if (tile == null)
				{
					var args = new SecondaryTileArgument
					{
						Version = 1,
						LocationId = routine.Location.Id,
						Type = SecondaryTileType.Routine,
						TargetId = routine.Id,
						Command = "execute",
						CommandUri = directCommandUri
					};

					tile = new SecondaryTile(tileId)
					{
						DisplayName = routine.Name,
						Arguments = JsonConvert.SerializeObject(args),
						VisualElements =
					{
						ShowNameOnSquare150x150Logo = true,
						ShowNameOnWide310x150Logo = true,
						ShowNameOnSquare310x310Logo = true,
						Square44x44Logo = new Uri("ms-appx:///Assets/Routine.png"),
						Square71x71Logo = new Uri("ms-appx:///Assets/Routine.png"),
						Square150x150Logo = new Uri("ms-appx:///Assets/Routine.png"),
						Square310x310Logo = new Uri("ms-appx:///Assets/Routine.png"),
					}
					};

					await tile.RequestCreateAsync().AsTask(ct);
				}
			}
			catch (Exception error)
			{
				throw new SmartthingsToStartException(SmartThingsToStartExceptionType.PinTile, error);
			}
		}

		public async Task<CommutableDeviceCommandTile[]> GetPinedDevices(CancellationToken ct)
		{
			var tiles = await SecondaryTile.FindAllAsync().AsTask(ct);

			return tiles
				.Select(tile => new CommutableDeviceCommandTile(tile.TileId, JsonConvert.DeserializeObject<SecondaryTileArgument>(tile.Arguments)))
				.ToArray();
		}
	}

	public class CommutableDeviceCommandTile
	{
		public CommutableDeviceCommandTile(string tileId, SecondaryTileArgument arguments)
		{
			TileId = tileId;
			Arguments = arguments;
		}

		public string TileId { get; }

		public SecondaryTileArgument Arguments { get; }
	}
}