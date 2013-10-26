using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace opentk.System3
{
	/// <summary>
	///
	/// </summary>
	[InheritedExport]
	public interface IParticleGenerator
	{
		string Name	{	get; }

		void NewBundle(System3 system, int bundleFirstItem);

		void MakeBubble (System3 system, int bundleFirstItem, int i);

		float UpdateSize(System3 system, int bundleFirstItem, int i);
	}
}