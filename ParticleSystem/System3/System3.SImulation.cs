using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace opentk.System3
{
	/// <summary>
	/// Meta information.
	/// </summary>
	public struct MetaInformation
	{
		public int LifeLen;
		public int Leader;
		public float Size;

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
			SimulationScheme = SimulationScheme ?? new ParticlesWithTrails();
			ParticleGenerator = ParticleGenerator ?? new SimpleGenerator();
			TrailSize = Math.Max(TrailSize, 1);

			Meta = new MetaInformation[Position.Length];
			for (int i = 0; i < Position.Length; i++)
			{
				ParticleGenerator.MakeBubble (this, i, i);
			}
		}

		protected override void Simulate (DateTime simulationTime)
		{
			ChaoticMap.UpdateMap(simulationTime, m_Step);
			SimulationScheme.Simulate(this, simulationTime, m_Step);
			m_Step++;
		}
	}
}
