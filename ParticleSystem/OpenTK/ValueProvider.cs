using System;
using System.Reflection;
using System.ComponentModel;
namespace OpenTK
{
/// <summary>
///
/// </summary>
	public static class ValueProvider
	{
	/// <summary>
	///
	/// </summary>
		public class _ValueProvider<T> : IValueProvider<T>
		{
			Func<T> m_Getter;

			public _ValueProvider (Func<T> getter, Action<Action> invalidator)
			{
				m_Getter = getter;

				if (invalidator != null)
					invalidator (() =>
					{
						if (PropertyChanged != null)
						{
							PropertyChanged (this, new PropertyChangedEventArgs ("Value"));
						}
					});
			}

			#region IValueProvider[T] implementation
			public T Value
			{
				get { return m_Getter (); }
			}
			#endregion

			#region INotifyPropertyChanged implementation
			public event PropertyChangedEventHandler PropertyChanged;
			#endregion
		}

		public static IValueProvider<T> Create<T> (Func<T> getter, Action<Action> invalidator = null)
		{
			return new _ValueProvider<T>(getter, invalidator);
		}

		public static IValueProvider<T> Create<T> (T val)
		{
			return Create(() => val);
		}
		
		public static IValueProvider<T> Combine<T> (IValueProvider<T> input, Func<Func<T>, Action<Action>, IValueProvider<T>> combineFunc)
		{
			return combineFunc(() => input.Value, act => input.PropertyChanged += (sender, e) => act());
		}
		
		public static IValueProvider<T> Combine<T> (IValueProvider<T> input1, Func<T, T> combineFunc)
		{
			return Combine(input1, 
		    (i1, invalidator) => Create(() => combineFunc(i1()), invalidator));
		}
		
		public static IValueProvider<T> Combine<T> (IValueProvider<T> input1, IValueProvider<T> input2, Func<T, T, T> combineFunc)
		{
			return Combine(input1, 
			(i1, invalidator) => 
			{
				return Create(() => combineFunc(i1(), input2.Value), act => { input2.PropertyChanged += (sender, e) => act(); invalidator(act);});
			});
		}
	}
	
	
}

