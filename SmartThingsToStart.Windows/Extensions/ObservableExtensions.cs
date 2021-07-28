using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SmartThingsToStart.Extensions
{
	public static class ObservableExtensions
	{
		public static IObservable<T> Retry<T>(this IObservable<T> source, TimeSpan retryDelay, IScheduler scheduler)
		{
			return source.Catch(source.DelaySubscription(retryDelay, scheduler).Retry());
		}
	}
}