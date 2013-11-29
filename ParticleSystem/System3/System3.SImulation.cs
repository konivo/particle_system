using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace opentk.System3
{
	/// <summary>
	/// Meta information.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct MetaInformation
	{	
		[FieldOffset(0x0)]
		public int LifeLen;
		[FieldOffset(0x4)]
		public int Leader;
		[FieldOffset(0x8)]
		public float Size;
		[FieldOffset(0xC)]
		public Vector3 Velocity;
	}

	/// <summary>
	/// System3.
	/// </summary>
	public partial class System3
	{
		private long m_Step;

		protected override void InitializeSystem ()
		{
			ChaoticMap = ChaoticMap ?? new LorenzMap ();
			SimulationScheme = SimulationScheme ?? new ParticlesWithTrails ();
			GenerationScheme = GenerationScheme ?? new ParticlesWithTrailsGenerationScheme ();
			ParticleGenerator = ParticleGenerator ?? new SimpleGenerator ();
			TrailSize = Math.Max (TrailSize, 1);

			Meta = MetaBuffer.Data = new MetaInformation[Position.Length];
			for (int i = 0; i < Position.Length; i++) {
				ParticleGenerator.MakeBubble (this, i, i);
			}
		}
		
		protected override void Publish (int start, int count)
		{
			if(count == PARTICLES_COUNT)
				MetaBuffer.Publish ();
			else
				MetaBuffer.PublishPart(start, count);
		}
		
		protected override void PrepareStateCore ()
		{
			unsafe
			{
				MetaBuffer = new BufferObject<MetaInformation> (sizeof(MetaInformation), 0) { Name = "metadata_buffer", Usage = BufferUsageHint.DynamicDraw };
			}
		}

		protected override void Simulate (DateTime simulationTime)
		{
			ChaoticMap.UpdateMap (simulationTime, m_Step);
			GenerationScheme.Generate (this, simulationTime, m_Step);
			SimulationScheme.Simulate (this, simulationTime, m_Step);
			m_Step++;
		}
	}
}
