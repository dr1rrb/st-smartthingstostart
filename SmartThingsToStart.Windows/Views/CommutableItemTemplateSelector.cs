using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SmartThingsToStart.Presentation;

namespace SmartThingsToStart.Views
{
	public class CommutableItemTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Device { get; set; }

		public DataTemplate Routine { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			if (item is RoutineViewModel)
			{
				return Routine;
			}
			else if (item is DeviceViewModel)
			{
				return Device;
			}
			else
			{
				return null;
			}
		}

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is RoutineViewModel)
			{
				return Routine;
			}
			else if (item is DeviceViewModel)
			{
				return Device;
			}
			else
			{
				return null;
			}
		}
	}
}
