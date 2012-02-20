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
	public interface ISimulationScheme
	{
		string Name
		{
			get;
		}

		void Simulate(System3 system, DateTime simulationTime, long simulationStep);
	}

	/// <summary>
	/// Particles with trails.
	/// </summary>
	public class ParticlesWithTrails: ISimulationScheme
	{
		/// <summary>
		/// Map mode type.
		/// </summary>
		public enum MapModeType
		{
			D = 0x3,
			ForceField = 0x4
		}

		/// <summary>
		/// Compute metadata.
		/// </summary>
		public enum ComputeMetadata
		{
			Tangent, Speed,
		}

		private float m_SpeedUpperBound;

		public MapModeType MapMode
		{
			get; set;
		}

		public int TrailBundleSize
		{
			get; set;
		}

		public ComputeMetadata ComputeMetadataMode{
			get; set;
		}

		string ISimulationScheme.Name
		{
			get
			{
				return "ParticlesWithTrails";
			}
		}

		void ISimulationScheme.Simulate (System3 system, DateTime simulationTime, long simulationStep)
		{
			var Position = system.Position;
			var Dimension = system.Dimension;
			var Meta = system.Meta;
			var Rotation = system.Rotation;
			var Color = system.Color;

			var fun = system.ChaoticMap.Map;
			var TrailSize = Math.Max(system.TrailSize, 1);
			var StepsPerFrame = Math.Max(system.StepsPerFrame, 1);
			m_SpeedUpperBound = Math.Max(m_SpeedUpperBound * 0.75f, 0.01f);

			var trailCount = (Position.Length + TrailSize - 1) / TrailSize;
			var trailBundleSize = Math.Max(TrailBundleSize, 1);
			var trailBundleCount = (trailCount + trailBundleSize - 1) / trailBundleSize;

			Parallel.For (0, trailBundleCount,
			bundleIndex =>
			{
				var particleCount = Position.Length;
				var trailSize = TrailSize;
				var firsttrail = bundleIndex * trailBundleSize * trailSize;
				var lasttrail = Math.Min(firsttrail + trailBundleSize , particleCount);
				var dt = (float)system.DT;
				var speedBound = m_SpeedUpperBound;
				var delta = Vector4.Zero;
				var delta2 = Vector4.Zero;
				var size = 0f;

				for (int j = 0; j < StepsPerFrame; j++)
				{
					for (int i = firsttrail ; i < lasttrail ; i += 1)
					{
						//i is the trail's first element
						var pi = i + Meta[i].Leader;

						Meta[i].Leader = (Meta[i].Leader + trailBundleSize) % (trailSize * trailBundleSize);
						
						var ii = i + Meta[i].Leader;
						if (ii >= particleCount)
						{
							ii = i;
							Meta[i].Leader = 0;
						}

						if(MapMode == MapModeType.ForceField)
						{
							delta = new Vector4 (Meta[i].Velocity * dt, 0);
							fun (ref Position[pi], ref delta2);
							Meta[i].Velocity += delta2.Xyz * dt;
						}
						else
						{
							fun (ref Position[pi], ref delta);
							delta *= dt;
							//delta.W = 0;
						}

						//Vector4.Add(ref position_old, ref delta, out Position[ii]);
						size = system.ParticleGenerator.UpdateSize(system, i, ii);

						Position[ii] = Position[pi] + delta;
						Dimension[ii] = new Vector4 (size, size, size, size);

						//
						var b0 = new Vector4( delta.Xyz, 0);
						var b2 = new Vector4( Vector3.Cross( b0.Xyz, Rotation[pi].Row1.Xyz), 0);
						var b1 = new Vector4( Vector3.Cross( b2.Xyz, b0.Xyz), 0);

						b0.Normalize();
						b1.Normalize();
						b2.Normalize();

						Rotation[ii] = new Matrix4(b0, b1, b2, new Vector4(0,0,0,1));

//						//
//						switch (ComputeMetadataMode)
//						{
//						case ColorSchemeType.Distance:
////							var speed = delta.LengthFast/ dt;
////							var A = MathHelper2.Clamp (speed / speedBound, 0, 1);
////							speedBound = Math.Max(speedBound, speed);
////							Color[ii] = (new Vector4 (1, 0.2f, 0.2f, 1) * A + new Vector4 (0.2f, 1, 0.2f, 1) * (1 - A));
//							var speed = delta.LengthFast/ dt;
//							var A = speed / 100.1f;
//							A = A - (float)Math.Floor(A);
//							speedBound = Math.Max(speedBound, speed);
//							Color[ii] = (new Vector4 (1, 0.2f, 0.2f, 1) * A + new Vector4 (0.2f, 1, 0.2f, 1) * (1 - A));
//							break;
//						case ColorSchemeType.Color:
//							Color[ii] = new Vector4 (0.2f, 1, 0.2f, 1);
//							break;
//						default:
//							break;
//						}

						switch (ComputeMetadataMode) {
						case ComputeMetadata.Speed:
							Color[ii] = delta;
						break;
						case ComputeMetadata.Tangent:
							Color[ii] = b0;
						break;
						default:
						break;
						}
						
						//
						if (j == StepsPerFrame - 1)
						{
							if (Meta[i].LifeLen <= 0)
								system.ParticleGenerator.NewBundle (system, i);
							else
								Meta[i].LifeLen--;
						}
					}
				}

				var orig = m_SpeedUpperBound;
				var help = orig;
				while(
					speedBound > orig &&
					(help = Interlocked.CompareExchange(ref m_SpeedUpperBound, speedBound, orig)) != orig)
					orig = help;
			});
		}
	}

	/// <summary>
	/// IFS scheme.
	/// </summary>
	public class IFSScheme: ISimulationScheme
	{
		private float m_SpeedUpperBound;
		private bool m_MapModeComputed;
		private long m_PrevSimulStep;

		#region ISimulationScheme implementation
		void ISimulationScheme.Simulate (System3 system, DateTime simulationTime, long simulationStep)
		{
			m_MapModeComputed &= simulationStep == m_PrevSimulStep + 1;
			m_PrevSimulStep = simulationStep;

			var Position = system.Position;
			var Meta = system.Meta;
			var ColorScheme = system.ColorScheme;
			var Color = system.Color;
			var dt = (float)system.DT;

			var fun = system.ChaoticMap.Map;
			m_SpeedUpperBound = Math.Max(m_SpeedUpperBound * 0.75f, 1);

			var step = 150;
			var ld = (Meta[0].Leader + 1) % step;
			Meta[0].Leader = ld;

			//prepare seed points
			if (!m_MapModeComputed)
			{
				for (int i = 0; i < step; i++)
				{
					Position[i] = new Vector4 ((float)(MathHelper2.GetThreadLocalRandom().NextDouble () * 0.01), 0, 0, 1);
				}
				m_MapModeComputed = true;
			}

			//iterate the seed values
			ld += step;
			for (int i = ld; i < Position.Length; i += step)
			{
				fun (ref Position[i - step], ref Position[i] );
				Position[i].W = 1;

				switch (ColorScheme)
				{
				case ColorSchemeType.Distance:
					var speed = (Position[i] - Position[i - step]).LengthFast / dt;
					var A = MathHelper2.Clamp (2 * speed / m_SpeedUpperBound, 0, 1);
					m_SpeedUpperBound = m_SpeedUpperBound < speed? speed: m_SpeedUpperBound;

					Color[i] = (new Vector4 (1, 0.2f, 0.2f, 1) * A + new Vector4 (0.2f, 1, 0.2f, 1) * (1 - A));
					break;
				case ColorSchemeType.Color:
					Color[i] = new Vector4 (0.2f, 1, 0.2f, 1);
					break;
				default:
					break;
				}
		}
		}

		string ISimulationScheme.Name
		{
			get
			{
				return "IFSScheme";
			}
		}
		#endregion
	}

	/// <summary>
	/// Single step scheme.
	/// </summary>
	public class SingleStepScheme: ISimulationScheme
	{
		private float m_SpeedUpperBound;
		private bool m_MapModeComputed;
		private long m_PrevSimulStep;

		#region ISimulationScheme implementation
		void ISimulationScheme.Simulate (System3 system, DateTime simulationTime, long simulationStep)
		{
			m_MapModeComputed &= simulationStep == m_PrevSimulStep + 1;
			m_PrevSimulStep = simulationStep;

			var Position = system.Position;
			var fun = system.ChaoticMap.Map;
			m_SpeedUpperBound = Math.Max(m_SpeedUpperBound * 0.75f, 1);

			//prepare seed points
			if (!m_MapModeComputed)
			{
				for (int i = 0; i < Position.Length; i++)
				{
					system.ParticleGenerator.MakeBubble(system, i, i);
					fun (ref Position[i], ref Position[i]);
					Position[i].W = 1;
				}
				m_MapModeComputed = true;
			}
		}

		string ISimulationScheme.Name
		{
			get
			{
				return "SingleStepScheme";
			}
		}
		#endregion
	}
}


