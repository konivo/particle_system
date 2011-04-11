using System;
using System.ComponentModel.Composition.Hosting;
namespace opentk
{
	/// <summary>
	/// 
	/// </summary>
	public static class GlobalContext
	{
		static GlobalContext ()
		{
			var catalog = new AssemblyCatalog (System.Reflection.Assembly.GetExecutingAssembly ());
			Container = new CompositionContainer (catalog, true);
		}

		public static CompositionContainer Container
		{
			get;
			private set;
		}
	}
}

