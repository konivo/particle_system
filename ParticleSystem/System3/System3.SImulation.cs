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
		}

		protected Vector4[] Dimension;
		protected Vector4[] Position;
		protected Vector4[] Color;
		protected MetaInformation[] Meta;
		protected int Processed;
		private System.Random m_Rnd = new Random ();
		private ChaoticMap m_ChaoticMap;
		private bool m_MapModeComputed;
		private float m_SpeedUpperBound;

		private void MakeBubble (int i)
		{
			if (Meta[i].Leader != 0)
			{
				MakeBubble (Meta[i].Leader + i);
				Meta[i] = new MetaInformation { LifeLen = m_Rnd.Next (20, 1000), Leader = Meta[i].Leader };
				return;
			}
			
			var size = (float)(Math.Pow (m_Rnd.NextDouble (), 2) * 0.001);
			var newpos = (Vector3)MathHelper2.RandomVector3 (12);
			
			Dimension[i] = new Vector4 (size, size, size, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, newpos.Z, 1);
			Color[i] = new Vector4 (0, 1, 0, 1);
			Meta[i] = new MetaInformation { LifeLen = m_Rnd.Next (20, 1000), Leader = 0 };
		}

		private void InitializeSystem ()
		{
			ChaoticMap = new LorenzMap ();
			TrailSize = 1;
			
			Dimension = DimensionBuffer.Data;
			Position = PositionBuffer.Data;
			Color = ColorBuffer.Data;
			Meta = new MetaInformation[Position.Length];
			
			for (int i = 0; i < Position.Length; i++)
			{
				MakeBubble (i);
			}
		}

		public void Simulate (DateTime simulationTime)
		{
			var fun = m_ChaoticMap.Map;
			TrailSize = TrailSize > 0 ? TrailSize : 1;
			StepsPerFrame = StepsPerFrame > 0 ? StepsPerFrame : 1;
			m_SpeedUpperBound = m_SpeedUpperBound > 0? m_SpeedUpperBound * 0.75f: 1;
			
			if (MapMode)
			{
				var step = 150;
				var ld = (Meta[0].Leader + 1) % step;
				Meta[0].Leader = ld;
				
				//prepare seed points
				if (!m_MapModeComputed)
				{
					for (int i = 0; i < step; i++)
					{
						Position[i] = new Vector4 ((float)(m_Rnd.NextDouble () * 0.01), 0, 0, 1);
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
			else
			{
				m_MapModeComputed = false;
				
				var trailCount = (Position.Length + TrailSize - 1) / TrailSize;
				var trailPacketSize = 10;
				var trailPacketCount = (trailCount + 10 - 1) / 10;
				
				Parallel.For (0, trailPacketCount, packetIndex =>
				{
					var packetoffset = packetIndex * trailPacketSize;
					var packetupper = packetoffset + trailPacketSize;
					packetupper = packetupper <= trailCount ? packetupper : trailCount;
					
					for (int j = 0; j < StepsPerFrame; j++)
					{
						for (int ti = packetoffset; ti < packetupper; ti++)
						{
							//ti is trail index
							//i is the trail's first element
							var i = ti * TrailSize;
							var pi = i + Meta[i].Leader;
							
							Meta[i].Leader += 1;
							Meta[i].Leader %= TrailSize;
							
							var ii = i + Meta[i].Leader;
							if (ii >= Position.Length)
							{
								ii = 0;
								Meta[i].Leader = 0;
							}

							var delta = new Vector4 ((Vector3)fun ((Vector3d)Position[pi].Xyz) * (float)DT, 0);
							Position[ii] = Position[pi] + delta;

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
