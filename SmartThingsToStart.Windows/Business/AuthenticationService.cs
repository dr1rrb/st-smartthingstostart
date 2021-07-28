using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using SmartThingsToStart.Client;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Extensions;
using SmartThingsToStart.Presentation;
using SmartThingsToStart.Utils;

namespace SmartThingsToStart.Business
{
	public class AuthenticationService
	{
		private readonly PasswordVault _vault;

		private readonly AsyncLock _gate = new AsyncLock();
		private readonly IPropertySet _settings;

		public static AuthenticationService Instance { get; } = new AuthenticationService();

		private AuthenticationService()
		{
			_vault = new PasswordVault();
			_settings = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
		}

		public async Task<HttpClient> GetToken(CancellationToken ct)
		{
			using (await _gate.LockAsync(ct))
			{

				try
				{
					var credentials = _vault.FindAllByResource(Constants.VaultKey);
					foreach (var credential in credentials)
					{
						try
						{
							credential.RetrievePassword();

							return new HttpClient
							{
								DefaultRequestHeaders =
								{
									{"Authorization", $"Bearer {credential.Password}"}
								}
							};
						}
						catch (Exception)
						{
						}
					}
				}
				catch (Exception)
				{
				}

				// Didn't found any endpoint, try to login
				var identity = await GetAppIdentity(ct);
				if (identity == null)
				{
					throw new InvalidOperationException("Not logged in.");
				}

				var token = await Login(ct, identity.Item1, identity.Item2);
				if (token == null)
				{
					throw new InvalidOperationException("Not logged in.");
				}

				_vault.Add(new PasswordCredential(Constants.VaultKey, token.Type, token.Value));

				return new HttpClient
				{
					DefaultRequestHeaders =
					{
						{"Authorization", $"Bearer {token.Value}"}
					}
				};
			}
		}

		public async Task Logout(CancellationToken ct)
		{
			using (await _gate.LockAsync(ct))
			{
				foreach (var credential in _vault.FindAllByResource(Constants.VaultKey))
				{
					_vault.Remove(credential);
				}
			}
		}

		private async Task<Tuple<string, string>> GetAppIdentity(CancellationToken ct)
		{
			await Task.Yield(); // Let the app complete setup (activate window)

			var frame = (Frame) Window.Current.Content;
			if (!frame.Navigate(typeof(SmartThingsSetup)))
			{
				return null;
			}

			var vm = (SmartThingsSetupViewModel) ((SmartThingsSetup) frame.Content).DataContext;
			vm.ClientId = _settings["AUTH_AppId"] as string;
			vm.ClientSecret = _settings["AUTH_AppSecret"] as string;

			var task = new TaskCompletionSource<Tuple<string, string>>();
			Interlocked.Exchange(ref _appIdentity, task)?.TrySetCanceled();

			NavigatingCancelEventHandler navigating = (snd, e) => task.TrySetCanceled();
			try
			{
				frame.Navigating += navigating;

				using (ct.Register(() =>
				{
					if (frame.CanGoBack)
					{
						frame.GoBack();
					}
				}))
				{
					return await task.Task;
				}
			}
			finally
			{
				frame.Navigating -= navigating;
				Interlocked.CompareExchange(ref _appIdentity, null, task);

				if (frame.CanGoBack)
				{
					frame.GoBack();
				}
			}
		}

		private TaskCompletionSource<Tuple<string, string>> _appIdentity;

		public async Task SetupAppIdentity(CancellationToken ct, string appId, string appSecret)
		{
			if (string.IsNullOrWhiteSpace(appId)
			    || string.IsNullOrWhiteSpace(appSecret))
			{
				throw new SmartthingsToStartException(SmartThingsToStartExceptionType.InvalidAppIdentity);
			}

			var appIdentityTask = _appIdentity;
			if (appIdentityTask == null)
			{
				throw new InvalidOperationException("Auth service is not waiting for application identity");
			}

			_settings["AUTH_AppId"] = appId;
			_settings["AUTH_AppSecret"] = appSecret;

			_appIdentity.SetResult(Tuple.Create(appId, appSecret));
		}

		private static async Task<AppToStToken> Login(CancellationToken ct, string appId, string appSecret)
		{
			var redirectUri = Constants.StAuthRedirectUri;
			var uri = $"https://graph.api.smartthings.com/oauth/authorize?response_type=code&client_id={appId}&scope=app&redirect_uri={redirectUri}";

			var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(uri), new Uri(redirectUri)).AsTask(ct);
			if (result.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
			{
				await new MessageDialog("Failed").ShowAsync();

				return null;
			}
			else if (result.ResponseStatus == WebAuthenticationStatus.UserCancel)
			{
				return null;
			}

			var code = new Uri(result.ResponseData)
				.GetQueryParameters()
				.ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase)["code"];

			var tokenUri = "https://graph.api.smartthings.com/oauth/token";
			var form = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				{"grant_type", "authorization_code"},
				{"code", code},
				{"client_id", appId},
				{"client_secret", appSecret},
				{"redirect_uri", redirectUri}
			});
			var response = await new HttpClient().PostAsync(new Uri(tokenUri), form, ct);
			var data = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
			var token = JsonConvert.DeserializeObject<AppToStToken>(data);

			return token;
		}

		public async Task Clear(CancellationToken ct)
		{
			foreach (var credential in _vault.RetrieveAll())
			{
				_vault.Remove(credential);
			}
		}
	}
}