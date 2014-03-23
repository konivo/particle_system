using System;
using System.Reflection;
using System.ComponentModel;
namespace OpenTK.Extensions
{
	/// <summary>
	///
	/// </summary>
	public static class ValueProviderExtensions
	{
		public static IValueProvider<T> Combine<T> (this IValueProvider<T> input, Func<Func<T>, Action<Action>, IValueProvider<T>> combineFunc)
		{
			return ValueProvider.Combine(input, combineFunc);
		}
		
		public static IValueProvider<T> Combine<T> (this IValueProvider<T> input1, IValueProvider<T> input2, Func<T, T, T> combineFunc)
		{
			return ValueProvider.Combine(input1, input2, combineFunc);
		}
		
		public static IValueProvider<T> Combine<T> (this IValueProvider<T> input1, Func<T, T> combineFunc)
		{
			return ValueProvider.Combine (input1, combineFunc);
		}
	}
}

