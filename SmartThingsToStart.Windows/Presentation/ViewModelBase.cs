using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SmartThingsToStart.Extensions;

namespace SmartThingsToStart.Presentation
{
	public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
	{
		private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
		private readonly Dictionary<string, IDisposable> _valueSubscriptions = new Dictionary<string, IDisposable>();

		public CancellationToken Token { get; }
		public CompositeDisposable Subscriptions { get; } = new CompositeDisposable();

		protected ViewModelBase()
		{
			var tokenProvider = new CancellationDisposable();
			Subscriptions.Add(tokenProvider);
			Token = tokenProvider.Token;
		}

		protected T Get<T>([CallerMemberName] string propertyName = null)
			=> (T) (_values.GetValueOrDefault(propertyName) ?? default(T));

		protected T Get<T>(Func<IObservable<T>> source, [CallerMemberName] string propertyName = null)
		{
			if (!_valueSubscriptions.ContainsKey(propertyName))
			{
				_valueSubscriptions[propertyName] = source()
					.DistinctUntilChanged()
					.ObserveOn(CoreDispatcherScheduler.Current)
					.Subscribe(value => Set(value, propertyName), e => Console.Error.WriteLine(e));
			}
			
			return Get<T>(propertyName);
		}

		protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
		{
			_values[propertyName] = value;
			OnPropertyChanged(propertyName);
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public void Dispose()
		{
			Subscriptions.Dispose();
			new CompositeDisposable(_valueSubscriptions.Values).Dispose();
		} 
	}

	public static class CommandExtensions
	{
		private static readonly ConditionalWeakTable<object, Dictionary<string, ICommand>> _commands = new ConditionalWeakTable<object, Dictionary<string, ICommand>>();

		public static ICommand Command(this ViewModelBase vm, Func<CancellationToken, object, Task> action, [CallerMemberName] string name = null)
			=> _commands.GetValue(vm, _ => new Dictionary<string, ICommand>()).GetOrCreate(name, n => new Command(n, action));

		public static ICommand Command(this ViewModelBase vm, Func<CancellationToken, Task> action, [CallerMemberName] string name = null)
			=> _commands.GetValue(vm, _ => new Dictionary<string, ICommand>()).GetOrCreate(name, n => new Command(n, action));
	}
}
