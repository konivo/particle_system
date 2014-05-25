using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Structure;
using OpenTK.Graphics;
using System.ComponentModel.Composition;

namespace opentk.System21
{
	public partial class System21
	{
		protected Vector2[] Oscilation;
		protected Vector4[] Velocity;
		protected Vector4[] VelocityUpdate;
		protected float[] Phase;
		protected Vector4[] Bmin;
		protected Vector4[] Bmax;

		protected Vector4 Leader;
		protected BezierCurveCubic? LeaderPath;
		protected BezierCurveCubic? LeaderPathYZ;
		protected float LeaderPathPosition;
		protected int Processed;
		protected int EmittedCount = 1;
		private QuadTree<int> Qtree;
		private System.Random m_Rnd = new Random ();

		private int InitializedCount;

		private opentk.QnodeDebug.QuadTreeDebug m_DebugView;

		private void InitializeQtree ()
		{
			//compute extents
			var min = Position[0].Xy;
			var max = Position[0].Xy;

			for (int i = 0; i < InitializedCount; i++)
			{
				Vector2 pp = Position[i].Xy;
				Vector2.ComponentMin (ref min, ref pp, out min);
				Vector2.ComponentMax (ref max, ref pp, out max);
			}
			
			Qtree = new QuadTree<int> { Min = min - Vector2.One, Max = max + Vector2.One };
		  Qtree.Payload.AddRange (Enumerable.Range (0, InitializedCount));
			Qtree.Split (node => 0.5f * (node.Min + node.Max), (node) =>
			{
				for (int i = 0; i < node.Payload.Count; i++)
				{
					var item = node.Payload[i];
					//if node fully contains payload bubble then return true
					var bmin = Bmin[item];
					var bmax = Bmax[item];

					if(node.Max.X >= bmax.X && node.Max.Y >= bmax.Y && node.Min.X < bmin.X && node.Min.Y < bmin.Y)
					{
						node.Payload.RemoveAt (i);
						i--;

						foreach (var childnode in node.Children) {
							if(childnode.Max.X >= bmin.X && childnode.Min.X < bmax.X && childnode.Max.Y >= bmin.Y && childnode.Min.Y < bmax.Y)
								childnode.Payload.Add (item);
						}
					}
				}
			}, node => node.Payload.Count <= 1 || node.Depth > 20);

			//
			m_DebugView.Tree = Qtree;
		}

		private void MakeBubble(int i)
		{
			var size = (float)Math.Pow(m_Rnd.NextDouble (), 2) * 5;
			var newpos = LeaderPath.Value.CalculatePoint (LeaderPathPosition);
			var newposyz = LeaderPathYZ.Value.CalculatePoint (LeaderPathPosition);

			Dimension[i] = new Vector4 (size, size, size, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, newposyz.Y, 1);
			Velocity[i] = new Vector4 (0, (float)Math.Min(1.0 / size, 100), 0, 0);
			Color[i] = new Vector4(1, 1, 0, 1);
			//Velocity[i] = new Vector4 (0, 1/1000.0f, 0, 0);

			Bmin[i] = Position[i] - new Vector4(size, size, 0, 0);
			Bmax[i] = Position[i] + new Vector4(size, size, 0, 0);
		}

		private void UpdateBubble(int i)
		{
			var size = Dimension[i].W;

			Velocity[i] = (Vector4)((Vector4d)Velocity[i]  + (Vector4d)VelocityUpdate[i]);
			Position[i] = (Vector4)((Vector4d)Position[i]  + (Vector4d)Velocity[i] * 0.0001);

			VelocityUpdate[i] = Vector4.Zero;

			Bmin[i] = Position[i] - new Vector4(size, size, 0, 0);
			Bmax[i] = Position[i] + new Vector4(size, size, 0, 0);
		}

		protected override void InitializeSystem ()
		{
			Oscilation = new Vector2[Position.Length];
			Phase = new float[Position.Length];
			Velocity = new Vector4[Position.Length];
			VelocityUpdate = new Vector4[Position.Length];
			Bmin = new Vector4[Position.Length];
			Bmax = new Vector4[Position.Length];
		}

		protected override void Simulate (DateTime simulationTime)
		{
			PreparePath ();

			for (int i = 0; i < EmittedCount; i++,Processed = (Processed + 1) % Position.Length)
			{
				InitializedCount = Math.Max(Processed, InitializedCount);
				MakeBubble(Processed);
			}
			
			InitializeQtree ();
			LeaderPathPosition += 0.01f;

			bool[] coupleBuffer = new bool[InitializedCount];
			bool coupleMask = true;

			for (int i = 0; i < InitializedCount; i+= 1)
			{

				var bmin = Bmin[i];
				var bmax = Bmax[i];

//				for (int j = 0; j < i; j++)
//				{
//					ComputeVelocity (i, j);
//				}
//
//				continue;

				Qtree.Traverse (node =>
				{
					for (int j = 0; j < node.Payload.Count; j++)
					{
						var other = node.Payload[j];

						if(coupleBuffer[other] ^ coupleMask)
							ComputeVelocity (i, other);

						coupleBuffer[other] = coupleMask;
					}
					
				}, node =>
				{
					if(node.Max.X >= bmin.X && node.Min.X < bmax.X && node.Max.Y >= bmin.Y && node.Min.Y < bmax.Y)
						return true;
					
					return false;
				});

				for(int j = 0; j < InitializedCount; j++)
					coupleBuffer[j] = coupleMask;

				coupleMask = !coupleMask;
			}

			for (int i = 0; i < InitializedCount; i+= 1)
			{
					UpdateBubble(i);
			}
		}

		private void ComputeVelocity (int i, int j)
		{
			if (i == j)
				return;
			
			var dir = (Vector4d) Position[i] - (Vector4d)Position[j];
			var mi = (double)Dimension[i].W;
			var mj = (double)Dimension[j].W;

			var len = dir.Length;
			if (len > (mj + mi))
				return;

			if (len < 0.000001)
				return;

			var unitdir = dir * (1/len);
			
			var vi0 = Vector4d.Dot ((Vector4d)Velocity[i], unitdir);
			var vj0 = Vector4d.Dot ((Vector4d)Velocity[j], unitdir);

			//if(vi0 > 0 && vj0 < 0)
			//	return;
			
			var vi1 = vi0 * (mi - mj) / (mi + mj) + vj0 * (2 * mj) / (mi + mj);

			//VelocityUpdate[i] += (Vector4)((vi1 - vi0) * unitdir);
			Velocity[i] += (Vector4)((vi1 - vi0) * unitdir);
		}

		private void PreparePath ()
		{
			if (LeaderPath.HasValue)
			{
				if (LeaderPathPosition > 1.0)
				{
					System.Random rnd = new Random ();
					float koef = 105.0f;
					Vector2 center = new Vector2 (0.5f, 0.5f) * koef;
					
					LeaderPath = new BezierCurveCubic (LeaderPath.Value.EndAnchor, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, 2 * LeaderPath.Value.EndAnchor - LeaderPath.Value.SecondControlPoint, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center);
					LeaderPathYZ = new BezierCurveCubic(LeaderPathYZ.Value.EndAnchor, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, 2 * LeaderPathYZ.Value.EndAnchor - LeaderPathYZ.Value.SecondControlPoint, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center);
					LeaderPathPosition -= 1.0f;
				}
			}

			else
			{
				System.Random rnd = new Random ();
				float koef = 105.0f;
				
				Vector2 center = new Vector2 (0.5f, 0.5f) * koef;
				
				LeaderPath = new BezierCurveCubic (new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center);
				LeaderPathYZ = new BezierCurveCubic (new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center);
				LeaderPathPosition = 0;
			}
			
		}
	}
}
