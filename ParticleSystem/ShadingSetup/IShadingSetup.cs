using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.PropertyGridCustom;
using opentk.Scene;
using opentk.Scene.ParticleSystem;

namespace opentk.ShadingSetup
{
	[InheritedExport]
	public interface IShadingSetup
	{
		string Name{ get; }

		RenderPass GetPass(ParticleSystemBase p);
	}
}

