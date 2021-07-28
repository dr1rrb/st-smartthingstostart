using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Popups;
using SmartThingsToStart.Business;

namespace SmartThingsToStart.Presentation
{
	public class Command : ICommand
	{
		private readonly string _name;
		private readonly Func<CancellationToken, object, Task> _action;

		private bool _isExecuting;

		public Command(string name, Func<CancellationToken, object, Task> action)
		{
			_name = name;
			_action = action;
		}

		public Command(string name, Func<CancellationToken, Task> action)
			: this(name, (ct, _) => action(ct))
		{
		}

		public bool CanExecute(object parameter) => !_isExecuting;

		public async void Execute(object parameter)
		{
			if (!CanExecute(parameter))
			{
				return;
			}

			_isExecuting = true;
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);

			try
			{
				await _action(CancellationToken.None, parameter);
			}
			catch (SmartthingsToStartException error)
			{
				var message = error.Message;
				switch (error.Type)
				{
					case SmartThingsToStartExceptionType.OpenPushNotificationChannel:
						message = @"Failed to open push notification channel. 

Validate that:
- you are connected to the internet
- application is allowed to use notifications (Windows Settings => System => Notifications)

Then retry to pin to start your tile, or restart the app.";
						break;

					case SmartThingsToStartExceptionType.PinTile:
						message = "Failed to pin to start";
						break;
				}

				await new MessageDialog(message, "Error").ShowAsync();
			}
			catch (Exception error)
			{
				await new MessageDialog($"Failed to {_name}: {error.Message}", "Error").ShowAsync();
			}

			_isExecuting = false;
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler CanExecuteChanged;
	}
}