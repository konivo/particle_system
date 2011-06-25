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

	}
	
	
}

