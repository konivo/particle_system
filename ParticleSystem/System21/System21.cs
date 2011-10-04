using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.Scene.ParticleSystem;

namespace opentk.System21
{
	public partial class System21: ParticleSystemBase
	{

		public int TrailSize
		{
			get;
			set;
		}

		public int StepsPerFrame
		{
			get;
			set;
		}

		public System21 ()
		{	}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System21 { PARTICLES_COUNT = 2000, VIEWPORT_WIDTH = 20, Fov = 0.9, NEAR = 1, FAR = 1000,
			ParticleScaleFactor = 2, ProjectionType = opentk.Scene.ProjectionType.Frustum };
			return result;
		}
		#region implemented abstract members of opentk.SystemBase.ParticleSystemBase

		protected override void PrepareStateCore ()
		{
			m_DebugView = new opentk.QnodeDebug.QuadTreeDebug(2, TransformationStack);
		}
		
		#endregion
	}
}

