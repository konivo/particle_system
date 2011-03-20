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
		protected Vector4[] Position;
		protected Vector2[] Oscilation;
		protected Vector4[] Velocity;
		protected Vector4[] VelocityUpdate;
		protected float[] Phase;
		protected Vector4[] Dimension;
		protected Vector4[] Bmin;
		protected Vector4[] Bmax;

		protected Vector4 Leader;
		protected BezierCurveCubic? LeaderPath;
		protected float LeaderPathPosition;
		protected int Processed;
		protected int EmittedCount = 1;
		private System.Random m_Rnd = new Random ();

		private void MakeBubble (int i)
		{
			var size = (float)Math.Pow (m_Rnd.NextDouble (), 2) * 0.1f;
			var newpos = (Vector4)new Vector4d (m_Rnd.NextDouble (), m_Rnd.NextDouble (), m_Rnd.NextDouble (), 1);

			Dimension[i] = new Vector4 (size, size, size, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, newpos.Z, 1);
			Velocity[i] = new Vector4 (0, (float)Math.Min (1.0 / size, 100), 0, 0);

			Bmin[i] = Position[i] - new Vector4 (size, size, 0, 0);
			Bmax[i] = Position[i] + new Vector4 (size, size, 0, 0);
		}

		private void InitializeSystem ()
		{
			Dimension = DimensionBuffer.Data;
			Position = PositionBuffer.Data;
			Oscilation = new Vector2[Position.Length];
			Phase = new float[Position.Length];
			Velocity = new Vector4[Position.Length];
			VelocityUpdate = new Vector4[Position.Length];
			Bmin = new Vector4[Position.Length];
			Bmax = new Vector4[Position.Length];

			for (int i = 0; i < Position.Length; i++)
			{
				MakeBubble(i);
			}
		}

		public void Simulate (DateTime simulationTime)
		{
			double A = 2.0, B = 2.0, C = 2.9, D = .6;

			for (int i = 0; i < Position.Length; i++)
			{
				var x_p = (double)Position[i].X;
				var y_p = (double)Position[i].Y;
				var z_p = (double)Position[i].Z;

				var z_n = Math.Sin (x_p);
				var x_n = Math.Sin (A * y_p) - z_p * Math.Cos (B * x_p);
				var y_n = z_n * Math.Sin (C * x_p) - Math.Cos (D * y_p);

				Position[i].X = (float)x_n;
				Position[i].Y = (float)y_n;
				Position[i].Z = (float)z_n;
			}
		}
	}
}
