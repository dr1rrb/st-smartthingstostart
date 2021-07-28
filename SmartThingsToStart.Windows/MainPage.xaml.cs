using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using SmartThingsToStart.Business;
using SmartThingsToStart.Client;
using SmartThingsToStart.Entities;
using SmartThingsToStart.Presentation;

namespace SmartThingsToStart
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			var vm = new DeviceListViewModel();

			this.InitializeComponent();

			DataContext = vm;
		}

		private void OnMenuItemLoaded(object sender, RoutedEventArgs e)
		{
			var item = (MenuFlyoutItem) sender;
			if (!item.IsEnabled)
			{
				var command = item.Command;
				item.Command = null;
				item.Command = command;
			}
		}

		private void OnItemLoaded(object sender, RoutedEventArgs e)
		{
			if (FocusManager.GetFocusedElement() == null)
			{
				((Control) sender).Focus(FocusState.Programmatic);
			}
		}
	}
}
