using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;

namespace opentk.System3
{
	public partial class System3
	{
		protected struct MetaInformation
		{
			public int LifeLen;
			public int Leader;
		}

		public enum MapType{
			Pickover,
			Polynomial,
			Lorenz
		}

		public int TrailSize{get; set;}

		public bool AnimateKoef{ get; set;}

		public MapType Map{get; set;}

		public double A {get; set;}
		public double B {get; set;}
		public double C {get; set;}
		public double D {get; set;}

		public double Sigma {get; set;}
		public double Rho {get; set;}
		public double Beta {get; set;}

		protected Vector4[] Dimension;
		protected Vector4[] Position;
		protected MetaInformation[] Meta;
		protected int Processed;
		private System.Random m_Rnd = new Random ();

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
			Func<int, Vector3d> fun;

			switch (Map) {
			case MapType.Lorenz:
				fun = Lorenz;
				break;
			case MapType.Pickover:
				fun = Pickover;
				break;
			case MapType.Polynomial:
				fun = Polynomial;
				break;
			default:
			fun = (i) => new Vector3d();
			break;
			}


			AnimateKoeficients ();
			TrailSize = TrailSize > 0? TrailSize: 1;

			for (int i = 0; i < Position.Length; i += TrailSize)
			{
				var pi = i + Meta[i].Leader;

				Meta[i].Leader += 1;
				Meta[i].Leader %= TrailSize;

				var ii = i + Meta[i].Leader;

				if(ii >= Position.Length)
					break;

				Position[ii] = Position[pi];

				Position[ii] = Position[ii] + new Vector4((Vector3)fun(ii) * (float)DT, 0);
			}
			
			for (int i = 0; i < Position.Length; i += TrailSize)
			{
				if (Meta[i].LifeLen <= 0)
					MakeBubble (i);
				else
					Meta[i].LifeLen--;
			}
		}

		private Vector3d Pickover (int i)
		{
			var x_p = (double)Position[i].X;
			var y_p = (double)Position[i].Y;
			var z_p = (double)Position[i].Z;

			var z_n = Math.Sin (x_p);
			var x_n = Math.Sin (A * y_p) - z_p * Math.Cos (B * x_p);
			var y_n = z_p * Math.Sin (C * x_p) - Math.Cos (D * y_p);

			return new Vector3d(x_n, y_n, z_n);
		}

		private Vector3d Lorenz (int i)
		{
			var x_p = (double)Position[i].X;
			var y_p = (double)Position[i].Y;
			var z_p = (double)Position[i].Z;

			var x_n = Sigma * (y_p - x_p);
			var y_n = x_p * (Rho - z_p) - y_p;
			var z_n = x_p * y_p - z_p * Beta;

			return new Vector3d(x_n, y_n, z_n);
		}

		private Vector3d Polynomial (int i)
		{
			var x_p = (double)Position[i].X;
			var y_p = (double)Position[i].Y;
			var z_p = (double)Position[i].Z;

			var x_n = A + y_p - z_p * y_p;
			var y_n = B + z_p - z_p * x_p;
			var z_n = C + x_p - y_p * x_p;

			return new Vector3d(x_n, y_n, z_n);
		}

		private void AnimateKoeficients ()
		{
//			if (m_AnimatedKoefStep == 1000)
//			{
//				m_KDelta = (Vector4d)CreateRandom2 (0.5f) / 1000;
//				m_AnimatedKoefStep = 0;
//			}
//			m_AnimatedKoefStep++;
//			m_Koeficients = m_Koeficients + m_KDelta;
		}
	}
}
