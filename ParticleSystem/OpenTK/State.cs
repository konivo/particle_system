using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenTK
{
	/// <summary>
	///
	/// </summary>
	public class State : IDisposable, IEnumerable<StatePart>
	{
		private class ActivatorListMember
		{
			public StatePart State;
			public StateActivator Activator;
			public int ActivationCount;
		}
		//
		private readonly Dictionary<Type, ISet<StatePart>> m_StateSet = new Dictionary<Type, ISet<StatePart>> ();
		//
		private Lazy<List<ActivatorListMember>> m_Activators;
		/// <summary>
		/// Gets the activation count.
		/// </summary>
		/// <value>The activation count.</value>
		public int ActivationCount
		{
			get;
			private set;
		}
		/// <summary>
		/// Gets the single.
		/// </summary>
		/// <returns>The single.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T GetSingle<T> () where T : StatePart
		{
			return GetSet (typeof(T)).OfType<T> ().FirstOrDefault ();
		}
		/// <summary>
		/// Gets all states of the given type
		/// </summary>
		/// <returns>The all.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public IEnumerable<T> GetAll<T> () where T : StatePart
		{
			return GetSet (typeof(T)).OfType<T> ();
		}
		/// <summary>
		/// Gets the state of the given type and activates it.
		/// </summary>
		/// <returns>The activate single.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T GetActivateSingle<T> () where T : StatePart
		{
			var result = m_Activators.Value.FirstOrDefault(m => m.State is T);
			if(result != null)
			{
				if(result.ActivationCount != ActivationCount)
					result.Activator.Activate ();
					
				result.ActivationCount = ActivationCount;
				return (T)result.State;
			}
			return default(T);
		}
		/// <summary>
		/// Gets all the states of the given type and activates them.
		/// </summary>
		/// <returns>The activate all.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public IEnumerable<T> GetActivateAll<T> () where T : StatePart
		{
			var result = m_Activators.Value.Where(m => m.State is T);
			foreach(var m in result)
			{
				if(m.ActivationCount != ActivationCount)
					m.Activator.Activate ();
					
				m.ActivationCount = ActivationCount;
				yield return (T)m.State;
			}
		}

		private ISet<StatePart> GetSet (Type t)
		{
			ISet<StatePart> states;
			
			if (!m_StateSet.TryGetValue (t, out states))
			{
				m_StateSet[t] = states = new HashSet<StatePart> ();
			}
			
			return states;
		}

		private State ()
		{
			m_Activators = new Lazy<List<ActivatorListMember>> (() =>
			{
				var acts = from i in m_StateSet.Values
					from j in i
					select new ActivatorListMember{ State = j, Activator = j.GetActivator (this)};
				
				return acts.ToList ();
			}, true);
		}

		public State (State basestate, params StatePart[] states) : this()
		{
			if (basestate != null)
			{
				throw new NotImplementedException ();
			}
			
			foreach (StatePart item in states)
			{
				Add (item);
			}
		}
		
		public void Add (StatePart state)
		{
			GetSet (state.GetType ()).Add (state);
		}

		public void Activate ()
		{
			ActivationCount++;
			
			foreach (var item in m_Activators.Value)
			{
				if(item.ActivationCount != ActivationCount)
					item.Activator.Activate ();
					
				item.ActivationCount = ActivationCount;
			}
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			foreach (var item in m_Activators.Value)
			{
				item.Activator.Dispose ();
			}
		}
		#endregion

		#region IEnumerable implementation

		public IEnumerator<StatePart> GetEnumerator ()
		{			
			return m_StateSet.Values.SelectMany (x => x).GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator();
		}

		#endregion
	}

	/// <summary>
	///
	/// </summary>
	internal class StateActivator : IDisposable
	{
		private readonly Action m_Activate;
		private readonly Action m_Dispose;

		public StateActivator (Action activate, Action dispose)
		{
			m_Activate = activate ?? (() => { });
			m_Dispose = dispose ?? (() => { });
		}

		public void Activate ()
		{
			m_Activate ();
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			m_Dispose ();
		}
		#endregion
		
	}

	/// <summary>
	///
	/// </summary>
	public abstract class StatePart : IDisposable
	{
		protected virtual void DisposeCore ()
		{
		}

		internal StateActivator GetActivator (State state)
		{
			var act = GetActivatorCore (state);
			return new StateActivator (act.Item1, act.Item2);
		}

		protected virtual Tuple<Action, Action> GetActivatorCore (State state)
		{
			return null;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			//for each member which implements IDisposable and is of the type StateBase do dispose() and then do InternalDispose
			
			DisposeCore ();
		}
		#endregion
	}
}