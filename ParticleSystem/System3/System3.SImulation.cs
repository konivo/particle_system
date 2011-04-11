using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;
using System.ComponentModel;

namespace opentk.System3
{
	public partial class System3
	{
		public struct MetaInformation
		{
			public int LifeLen;
			public int Leader;
		}

		public enum MapType
		{
			Pickover,
			Polynomial,
			Lorenz,
			Chua
		}

		public int TrailSize
		{
			get;
			set;
		}

		public bool AnimateKoef
		{
			get;
			set;
		}

		public MapType Map
		{
			get;
			set;
		}

		public double A
		{
			get;
			set;
		}
		public double B
		{
			get;
			set;
		}
		public double C
		{
			get;
			set;
		}
		public double D
		{
			get;
			set;
		}

		public double Sigma
		{
			get;
			set;
		}
		public double Rho
		{
			get;
			set;
		}
		public double Beta
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
			var size = (float)Math.Pow (m_Rnd.NextDouble (), 2) * 0.1f;
			var newpos = CreateRandom (12);
			
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

		private Vector4 CreateRandom (float magnitude)
		{
			double dmag = 2 * magnitude;
			return (Vector4)new Vector4d (m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag, 1) - new Vector4 (magnitude, magnitude, magnitude, 0);
		}

		private Vector4 CreateRandom2 (float magnitude)
		{
			double dmag = 2 * magnitude;
			return (Vector4)new Vector4d (m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag) - new Vector4 (magnitude, magnitude, magnitude, magnitude);
		}

		public void Simulate (DateTime simulationTime)
		{
			var fun = m_ChaoticMap.Map;
			TrailSize = TrailSize > 0 ? TrailSize : 1;
			
			for (int i = 0; i < Position.Length; i += TrailSize)
			{
				var pi = i + Meta[i].Leader;
				
				Meta[i].Leader += 1;
				Meta[i].Leader %= TrailSize;
				
				var ii = i + Meta[i].Leader;
				
				if (ii >= Position.Length)
					break;
				
				Position[ii] = Position[pi];
				
				Position[ii] = Position[ii] + new Vector4 ((Vector3)fun ( (Vector3d)Position[ii].Xyz) * (float)DT, 0);
			}
			
			for (int i = 0; i < Position.Length; i += TrailSize)
			{
				if (Meta[i].LifeLen <= 0)
					MakeBubble (i);
				else
					Meta[i].LifeLen--;
			}
		}
	}
}
