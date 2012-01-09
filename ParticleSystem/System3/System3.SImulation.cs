using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Threading.Tasks;

namespace opentk.System3
{
	public partial class System3
	{
		protected struct MetaInformation
		{
			public int LifeLen;
			public int Leader;
			public Vector3 Velocity;
		}

		protected MetaInformation[] Meta;
		protected int Processed;
		private ChaoticMap m_ChaoticMap;
		private bool m_MapModeComputed;
		private float m_SpeedUpperBound;

		private void MakeBubble (int i)
		{
			if (Meta[i].Leader != 0)
			{
				MakeBubble (Meta[i].Leader + i);
				Meta[i] = new MetaInformation { LifeLen = MathHelper2.GetThreadLocalRandom().Next (20, 1000), Leader = Meta[i].Leader };
				return;
			}

			var size = (float)(Math.Pow (MathHelper2.GetThreadLocalRandom().NextDouble (), 2) * 0.001);
			var newpos = (Vector3)MathHelper2.RandomVector3 (12);

			if(SeedDistribution == System3.SeedDistributionType.RegularGrid)
			{
				var gridStepSize = 0.1f;
				var gridLongStepSize = 5 * 2.5f;
				var gridCount = (int)Math.Floor(Math.Pow(Position.Length, 1/3.0) * 25);
				var gridCount2 = (int)Math.Ceiling(Math.Pow(Position.Length, 1/3.0) / 5);
				var gridOrigin = - (gridCount * gridStepSize) / 2;

				var gc2 = gridCount * gridCount2;

				int ix = i / gc2;
				int iy = (i - gc2 * ix) / gridCount;
				int iz = i - gc2 * ix - iy * gridCount;

				newpos = Vector3.Multiply(new Vector3(ix, iy, iz), new Vector3(gridLongStepSize, gridLongStepSize, gridStepSize)) +
					new Vector3(gridOrigin, gridOrigin, gridOrigin);
			}
			
			Dimension[i] = new Vector4 (size, size, size, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, newpos.Z, 1);
			Color[i] = new Vector4 (0, 1, 0, 1);
			Meta[i] = new MetaInformation { LifeLen = MathHelper2.GetThreadLocalRandom().Next (20, 1000), Leader = 0 };
			Rotation[i] = Matrix4.Identity;
		}

		protected override void InitializeSystem ()
		{
			ChaoticMap = ChaoticMap ?? new LorenzMap ();
			TrailSize = Math.Max(TrailSize, 1);

			Meta = new MetaInformation[Position.Length];
			for (int i = 0; i < Position.Length; i++)
			{
				MakeBubble (i);
			}
		}

		protected override void Simulate (DateTime simulationTime)
		{
			var fun = m_ChaoticMap.Map;
			TrailSize = Math.Max(TrailSize, 1);
			StepsPerFrame = Math.Max(StepsPerFrame, 1);
			m_SpeedUpperBound = Math.Max(m_SpeedUpperBound * 0.75f, 1);
			
			if (MapMode == System3.MapModeType.Iterated)
			{
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
					Position[i] = new Vector4 ((Vector3)fun ((Vector3d)Position[i - step].Xyz), 1);
					
					switch (ColorScheme)
					{
					case ColorSchemeType.Distance:
						var speed = (Position[i] - Position[i - step]).LengthFast / (float)DT;
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
			else if(MapMode == System3.MapModeType.SingleStep)
			{
				//prepare seed points
				if (!m_MapModeComputed)
				{
					for (int i = 0; i < Position.Length; i++)
					{
						MakeBubble(i);
						Position[i] = new Vector4 ((Vector3)fun ((Vector3d)Position[i].Xyz), 1);
					}
					m_MapModeComputed = true;
				}
			}
			else
			{
				m_MapModeComputed = false;

				var trailCount = (Position.Length + TrailSize - 1) / TrailSize;
				var trailBundleSize = 100;
				var trailBundleCount = (trailCount + trailBundleSize - 1) / trailBundleSize;
				
				Parallel.For (0, trailBundleCount, bundleIndex =>
				{
					var particleCount = Position.Length;
					var trailSize = TrailSize;
					var firsttrail = bundleIndex * trailBundleSize * trailSize;
					var lasttrail = Math.Min(firsttrail + trailBundleSize , particleCount);
					var dt = (float)DT;
					
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

							Vector4 delta;
							if(MapMode == MapModeType.ForceField)
							{
								delta = new Vector4 (Meta[i].Velocity * dt, 0);
								var dv = (Vector3)fun ((Vector3d)Position[pi].Xyz) * dt;
								Meta[i].Velocity += dv;
							}
							else
							{
								delta = new Vector4 ((Vector3)fun ((Vector3d)Position[pi].Xyz) * dt, 0);
							}

							Position[ii] = Position[pi] + delta;

							//
							var b0 = delta;
							var b2 = new Vector4( Vector3.Cross( b0.Xyz, Rotation[pi].Row1.Xyz), 0);
							var b1 = new Vector4( Vector3.Cross( b2.Xyz, b0.Xyz), 0);

							b0.Normalize();
							b1.Normalize();
							b2.Normalize();

							Rotation[ii] = new Matrix4(b0, b1, b2, new Vector4(0,0,0,1));

							//
							switch (ColorScheme)
							{
							case ColorSchemeType.Distance:
								var speed = delta.LengthFast/ (float) DT;
								var A = MathHelper2.Clamp (2 * speed / m_SpeedUpperBound, 0, 1);
								m_SpeedUpperBound = m_SpeedUpperBound < speed? speed: m_SpeedUpperBound;

								Color[ii] = (new Vector4 (1, 0.2f, 0.2f, 1) * A + new Vector4 (0.2f, 1, 0.2f, 1) * (1 - A));
								break;
							case ColorSchemeType.Color:
								Color[ii] = new Vector4 (0.2f, 1, 0.2f, 1);
								break;
							default:
								break;
							}
							
							//
							if (j == StepsPerFrame - 1)
							{
								if (Meta[i].LifeLen <= 0)
									MakeBubble (i);
								else
									Meta[i].LifeLen--;
							}
						}
					}
				});
			}
		}
	}
}
