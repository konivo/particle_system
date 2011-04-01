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
		}

		protected Vector4[] Dimension;
		protected Vector4[] Position;
		protected MetaInformation[] Meta;
		protected int Processed;
		protected int TrailSize = 1;
		private System.Random m_Rnd = new Random ();

		private Vector4d m_Koeficients = new Vector4d(10, 1.8, 2.71, 1.51);
		private Vector4d m_KDelta;
		private int m_AnimatedKoefStep = 0;

		private void MakeBubble (int i)
		{
			var size = (float)Math.Pow (m_Rnd.NextDouble (), 2) * 0.01f;
			var newpos = CreateRandom (12);
			
			Dimension[i] = new Vector4 (size, size, size, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, newpos.Z, 1);
			Meta[i] = new MetaInformation { LifeLen = m_Rnd.Next (20, 1000) };
		}

		private void InitializeSystem ()
		{
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
			AnimateKoeficients ();

			for (int i = 0; i < Position.Length; i += TrailSize)
			{
				Position[i] = Position[i] + new Vector4((Vector3)Pickover(i) * (float)DT, 0);
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
			double A = m_Koeficients.X, B = m_Koeficients.Y, C = m_Koeficients.Z, D = m_Koeficients.W;

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
			double sigma = m_Koeficients.X, rho = m_Koeficients.Y, beta = m_Koeficients.Z;

			var x_p = (double)Position[i].X;
			var y_p = (double)Position[i].Y;
			var z_p = (double)Position[i].Z;

			var x_n = sigma * (y_p - x_p);
			var y_n = x_p * (rho - z_p) - y_p;
			var z_n = x_p * y_p - z_p * beta;

			return new Vector3d(x_n, y_n, z_n);
		}

		private Vector3d Polynomial (int i)
		{
			double A = m_Koeficients.X, B = m_Koeficients.Y, C = m_Koeficients.Z;

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
			if (m_AnimatedKoefStep == 1000)
			{
				m_KDelta = (Vector4d)CreateRandom2 (0.5f) / 1000;
				m_AnimatedKoefStep = 0;
			}
			m_AnimatedKoefStep++;
			m_Koeficients = m_Koeficients + m_KDelta;
		}
	}
}
