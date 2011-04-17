using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using OpenTK;
using System.ComponentModel;
namespace opentk
{
	[InheritedExport]
	public abstract class ParticleSystem : INotifyPropertyChanged, IDisposable
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="win">
		/// A <see cref="GameWindow"/>
		/// </param>
		/// <returns>
		/// A <see cref="ParticleSystem"/>
		/// </returns>
		public ParticleSystem GetInstance (GameWindow win)
		{
			var result = GetInstanceInternal (win);
			win.RenderFrame += result.HandleWinRenderFrame;
			
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="win">
		/// A <see cref="GameWindow"/>
		/// </param>
		/// <returns>
		/// A <see cref="ParticleSystem"/>
		/// </returns>
		protected abstract ParticleSystem GetInstanceInternal (GameWindow win);

		/// <summary>
		///
		/// </summary>
		/// <param name="window">
		/// A <see cref="GameWindow"/>
		/// </param>
		protected abstract void HandleFrame (GameWindow window);

		/// <summary>
		///
		/// </summary>
		/// <param name="sender">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="e">
		/// A <see cref="OpenTK.FrameEventArgs"/>
		/// </param>
		private void HandleWinRenderFrame (object sender, OpenTK.FrameEventArgs e)
		{
			HandleFrame ((GameWindow)sender);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>
		/// A <see cref="IEnumerable<Shader>"/>
		/// </returns>
		public IEnumerable<Shader> GetShaders ()
		{
			var parentNamespace = GetType ().Namespace.Split ('.').Last ();
			
			var shaders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains ("glsl") && res.Contains (parentNamespace)
				select new Shader (res, ResourcesHelper.GetText (res, System.Text.Encoding.UTF8));
			
			return shaders;
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="placeholder">
		/// A <see cref="T"/>
		/// </param>
		/// <param name="value">
		/// A <see cref="T"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		protected void DoPropertyChange<T> (ref T placeholder, T value, string name)
		{
			placeholder = value;
			RaisePropertyChanged (name);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		protected void RaisePropertyChanged (string name)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (name));
		}

		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region IDisposable implementation
		public virtual void Dispose ()
		{	}
		#endregion
	}
}

