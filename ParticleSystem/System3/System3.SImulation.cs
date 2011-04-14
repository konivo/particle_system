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
		public struct MetaInformation
		{
			public int LifeLen;
			public int Leader;
		}

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

		public bool MapMode
		{
			get;
			set;
		}

		public float ParticleScaleFactor
		{
			get;
			set;
		}

		protected Vector4[] Dimension;
		protected Vector4[] Position;
		protected MetaInformation[] Meta;
		protected int Processed;
		private System.Random m_Rnd = new Random ();
		private ChaoticMap m_ChaoticMap;
		private bool m_MapModeComputed;

		[Category("Map properties")]
		[TypeConverter(typeof(ChaoticMapConverter))]
		[DescriptionAttribute("Expand to see the parameters of the map.")]
		public ChaoticMap ChaoticMap
		{
			get { return m_ChaoticMap; }
			set { DoPropertyChange (ref m_ChaoticMap, value, "ChaoticMap"); }
		}

		private void MakeBubble (int i)
		{
			var size = (float)(Math.Pow (m_Rnd.NextDouble (), 2) * 0.001);
			var newpos = (Vector3)MathHelper2.RandomVector3(12);
			
			Dimension[i] = new Vector4 (size, size, size, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, newpos.Z, 1);
			Meta[i] = new MetaInformation { LifeLen = m_Rnd.Next (20, 1000), Leader = 0 };
		}

		private void InitializeSystem ()
		{
			ChaoticMap = new LorenzMap ();
			TrailSize = 1;
			
			Dimension = DimensionBuffer.Data;
			Position = PositionBuffer.Data;
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
			
			if (MapMode)
			{
				if(m_MapModeComputed)
					return;

				Position[0] = new Vector4(0.0f, 0, 0, 1);

				var ld = (Meta[0].Leader + 1) % Position.Length;
				Meta[0].Leader = ld == 0? 1 : ld;

				for (int i = Meta[0].Leader; i < Position.Length; i += TrailSize)
				{
					Position[i] = new Vector4 ((Vector3)fun ((Vector3d)Position[i - 1].Xyz), 1);
				}

				m_MapModeComputed = true;
			}
			else
			{
				m_MapModeComputed = false;

				var trailCount = (Position.Length + TrailSize - 1) / TrailSize;
				var trailPacketSize = 10;
				var trailPacketCount = (trailCount + 10 - 1) / 10;

				Parallel.For(0, trailPacketCount,
				(packetIndex) =>
				{
					var packetoffset = packetIndex * trailPacketSize;
					var packetupper = packetoffset + trailPacketSize;
					packetupper = packetupper < trailCount ? packetupper : trailCount;

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
								break;

							Position[ii] = Position[pi];
							Position[ii] = Position[ii] + new Vector4 ((Vector3)fun ((Vector3d)Position[ii].Xyz) * (float)DT, 0);

							//
							if (Meta[i].LifeLen <= 0)
								MakeBubble (i);
							else
								Meta[i].LifeLen--;
						}
					}

				});
			}
		}
	}
}
