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
	public class State : IDisposable
	{
		private readonly Dictionary<Type, ISet<StatePart>> m_StateSet = new Dictionary<Type, ISet<StatePart>> ();

		private Lazy<List<StateActivator>> m_Activators;

		public IEnumerable<StatePart> StateParts
		{
			get { return m_StateSet.Values.SelectMany (x => x); }
		}

		public T GetSingleState<T> () where T : StatePart
		{
			return GetSet (typeof(T)).OfType<T> ().FirstOrDefault ();
		}

		public IEnumerable<T> GetStates<T> () where T : StatePart
		{
			return GetSet (typeof(T)).OfType<T> ();
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

		private void PutState (StatePart state)
		{
			GetSet (state.GetType ()).Add (state);
		}

		private State ()
		{
			m_Activators = new Lazy<List<StateActivator>> (() =>
			{
				var acts = from i in m_StateSet.Values
					from j in i
					select j.GetActivator (this);
				
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
				PutState (item);
			}
		}

		public void Activate ()
		{
			foreach (var item in m_Activators.Value)
				item.Activate ();
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			foreach (var item in m_Activators.Value)
			{
				item.Dispose ();
			}
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