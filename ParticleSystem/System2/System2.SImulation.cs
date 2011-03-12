using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;

namespace opentk.System2
{
	public partial class System2
	{
		private class QNode<T>
		{
			public Vector2 Min;
			public Vector2 Max;
			public int Depth;

			public List<QNode<T>> Children
			{
				get;
				private set;
			}
			public List<T> Payload
			{
				get;
				private set;
			}

			public QNode ()
			{
				Children = new List<QNode<T>> ();
				Payload = new List<T> ();
			}

			public void Split (Func<QNode<T>, Vector2> centerSelector, Func<T, QNode<T>, bool> payloadSplitter, Func<QNode<T>, bool> terminationCondition)
			{
				if (terminationCondition (this))
					return;
				
				var center = centerSelector (this);
				
				var q11 = new QNode<T> { Min = Min, Max = center, Depth = Depth + 1 };
				var q12 = new QNode<T> { Min = new Vector2 (center.X, Min.Y), Max = new Vector2 (Max.X, center.Y), Depth = Depth + 1 };
				var q21 = new QNode<T> { Min = new Vector2 (Min.X, center.Y), Max = new Vector2 (center.X, Max.Y), Depth = Depth + 1 };
				var q22 = new QNode<T> { Min = center, Max = Max, Depth = Depth + 1 };
				
				for (int i = 0; i < Payload.Count; i++)
				{
					var item = Payload[i];
					Payload.RemoveAt (i);
					
					if (payloadSplitter (item, q11))
						q11.Payload.Add (item);
					else if (payloadSplitter (item, q12))
						q12.Payload.Add (item);
					else if (payloadSplitter (item, q22))
						q22.Payload.Add (item);
					else if (payloadSplitter (item, q21))
						q21.Payload.Add (item);
					else
					{
						Payload.Insert (i, item);
					}
				}
				
				Children.Add (q11);
				Children.Add (q12);
				Children.Add (q21);
				Children.Add (q22);
				
				foreach (var item in Children)
				{
					item.Split (centerSelector, payloadSplitter, terminationCondition);
				}
			}

			public void Traverse (Action<QNode<T>> visitor, Func<QNode<T>, bool> navigator)
			{
				visitor (this);
				
				foreach (var item in Children)
				{
					if (navigator (item))
						item.Traverse (visitor, navigator);
				}
			}
		}

		protected Vector4[] Position;
		protected Vector2[] Oscilation;
		protected Vector4[] Velocity;
		protected Vector4[] VelocityUpdate;
		protected float[] Phase;
		protected Vector4[] ColorAndSize;
		protected Vector4[] Bmin;
		protected Vector4[] Bmax;

		protected Vector4 Leader;
		protected BezierCurveCubic? LeaderPath;
		protected float LeaderPathPosition;
		protected int Processed;
		protected int EmittedCount = 1;
		private QNode<int> Qtree;
		private System.Random m_Rnd = new Random ();

		private int InitializedCount;

		private void InitializeQtree ()
		{
			//compute extents
			var min = Position[0].Xy;
			var max = Position[0].Xy;

			unsafe
			{
				fixed (Vector4* p = Position)
				{
					for (int i = 0; i < InitializedCount; i++)
					{
						Vector2* pp = (Vector2*)(p + i);
						Vector2.ComponentMin (ref min, ref *pp, out min);
						Vector2.ComponentMax (ref max, ref *pp, out max);
					}
				}
			}
			
			Qtree = new QNode<int> { Min = min - Vector2.One, Max = max + Vector2.One };
//			Qtree.Payload.AddRange (
//			  Enumerable.Range (0, Position.Length)
//			  .Where(x => Position[x].Length < 100));
		  Qtree.Payload.AddRange (Enumerable.Range (0, Position.Length));
			Qtree.Split (node => 0.5f * (node.Min + node.Max), (i, node) =>
			{
				//if node fully contains payload bubble then return true
				var bmin = Bmin[i];
				var bmax = Bmax[i];

				if (node.Max.X >= bmax.X && node.Max.Y >= bmax.Y && node.Min.X < bmin.X && node.Min.Y < bmin.Y)
					return true;

				return false;
			}, node => node.Payload.Count == 0 || node.Depth > 20);
		}

		private void MakeBubble(int i)
		{
			var size = (float)m_Rnd.NextDouble () * 0.1f;
			var newpos = LeaderPath.Value.CalculatePoint (LeaderPathPosition);

			ColorAndSize[i] = new Vector4 (0, 0, 0, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, 0, 1);
			Velocity[i] = new Vector4 (0, 0.1f / (10 * ColorAndSize[i].W), 0, 0);

			Bmin[i] = Position[i] - new Vector4(size, size, 0, 0);
			Bmax[i] = Position[i] + new Vector4(size, size, 0, 0);
		}

		private void InitializeSystem ()
		{
			ColorAndSize = ColorAndSizeBuffer.Data;
			Position = PositionBuffer.Data;
			Oscilation = new Vector2[Position.Length];
			Phase = new float[Position.Length];
			Velocity = new Vector4[Position.Length];
			VelocityUpdate = new Vector4[Position.Length];
			Bmin = new Vector4[Position.Length];
			Bmax = new Vector4[Position.Length];
		}

		public void Simulate (DateTime simulationTime)
		{
			PreparePath ();

			for (int i = 0; i < EmittedCount; i++,Processed = (Processed + 1) % Position.Length)
			{
				InitializedCount = Math.Max(Processed, InitializedCount);
				MakeBubble(Processed);
			}
			
			InitializeQtree ();
			LeaderPathPosition += 0.1f;
			
			for (int i = Processed % 2; i < InitializedCount; i+= 2)
			{
				VelocityUpdate[i] = Vector4.Zero;
				var bmin = Bmin[i];
				var bmax = Bmax[i];

				Qtree.Traverse (node =>
				{
					for (int j = 0; j < node.Payload.Count; j++)
					{
						ComputeVelocity (i, node.Payload[j]);
					}
					
				}, node =>
				{
					if (node.Max.X >= bmin.X && node.Min.X < bmax.X && node.Max.Y >= bmin.Y && node.Min.Y < bmax.Y)
						return true;
					
					return false;
				});
			}

			for (int i = Processed % 2; i < InitializedCount; i+= 2)
			{
				Velocity[i] += VelocityUpdate[i];
				Position[i] += Velocity[i] * 0.000001f;
			}
		}

		private void ComputeVelocity (int i, int j)
		{
			if (i == j)
				return;
			
			var dir = (Vector4d) Position[i] - (Vector4d)Position[j];
			var mi = ColorAndSize[i].W;
			var mj = ColorAndSize[j].W;

			if (dir.LengthFast > (mj + mi))
				return;

//			if (dir.LengthSquared < 0.000001)
//				return;

			var unitdir = Vector4d.Normalize (dir);
			
			var vi0 = Vector4d.Dot ((Vector4d)Velocity[i], unitdir);
			var vj0 = Vector4d.Dot ((Vector4d)Velocity[j], unitdir);
			
			var vi1 = vi0 * (mi - mj) / (mi + mj) + vj0 * (2 * mj) / (mi + mj);
			//var vj1 = vj0 * (mj - mi) / (mi + mj) + vi0 * (2 * mi) / (mi + mj);

			VelocityUpdate[i] += (Vector4)((vi1 - vi0) * unitdir);
			//Velocity[i] += (vi1 - vi0) * unitdir;
			//Velocity[j] += (vj1 - vj0) * unitdir;
		}

		private void PreparePath ()
		{
			if (LeaderPath.HasValue)
			{
				if (LeaderPathPosition > 1.0)
				{
					System.Random rnd = new Random ();
					float koef = 5.0f;
					Vector2 center = new Vector2 (0.5f, 0.5f) * koef;
					
					LeaderPath = new BezierCurveCubic (LeaderPath.Value.EndAnchor, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, 2 * LeaderPath.Value.EndAnchor - LeaderPath.Value.SecondControlPoint, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center);
					LeaderPathPosition -= 1.0f;
				}
			}

			else
			{
				System.Random rnd = new Random ();
				float koef = 5.0f;
				
				Vector2 center = new Vector2 (0.5f, 0.5f) * koef;
				
				LeaderPath = new BezierCurveCubic (new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center);
				LeaderPathPosition = 0;
			}
			
		}
	}
}
