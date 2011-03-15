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
		public class QNode<T>
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

			public void Split (Func<QNode<T>, Vector2> centerSelector, Action<QNode<T>> payloadSplitter, Func<QNode<T>, bool> terminationCondition)
			{
				if (terminationCondition (this))
					return;
				
				var center = centerSelector (this);
				
				var q11 = new QNode<T> { Min = Min, Max = center, Depth = Depth + 1 };
				var q12 = new QNode<T> { Min = new Vector2 (center.X, Min.Y), Max = new Vector2 (Max.X, center.Y), Depth = Depth + 1 };
				var q21 = new QNode<T> { Min = new Vector2 (Min.X, center.Y), Max = new Vector2 (center.X, Max.Y), Depth = Depth + 1 };
				var q22 = new QNode<T> { Min = center, Max = Max, Depth = Depth + 1 };

				Children.Add (q11);
				Children.Add (q12);
				Children.Add (q21);
				Children.Add (q22);

				payloadSplitter (this);
				
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

		private opentk.QnodeDebug.QnodeDebug m_DebugView;

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
			var size = (float)Math.Pow(m_Rnd.NextDouble (), 8);
			//size /= 4;
			var newpos = LeaderPath.Value.CalculatePoint (LeaderPathPosition);

			ColorAndSize[i] = new Vector4 (0, 0, 0, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, 0, 1);
			Velocity[i] = new Vector4 (0, (float)Math.Min(1.0 / size, 100), 0, 0);
			//Velocity[i] = new Vector4 (0, 1/1000.0f, 0, 0);

			Bmin[i] = Position[i] - new Vector4(size, size, 0, 0);
			Bmax[i] = Position[i] + new Vector4(size, size, 0, 0);
		}

		private void UpdateBubble(int i)
		{
			var size = ColorAndSize[i].W;

			Velocity[i] = (Vector4)((Vector4d)Velocity[i]  + (Vector4d)VelocityUpdate[i]);
			Position[i] = (Vector4)((Vector4d)Position[i]  + (Vector4d)Velocity[i] * 0.0001);

			VelocityUpdate[i] = Vector4.Zero;

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
			var mi = (double)ColorAndSize[i].W;
			var mj = (double)ColorAndSize[j].W;

			var len = dir.Length;
			if (len > (mj + mi))
				return;

			if (len < 0.000001)
				return;

			var unitdir = dir * (1/len);
			
			var vi0 = Vector4d.Dot ((Vector4d)Velocity[i], unitdir);
			var vj0 = Vector4d.Dot ((Vector4d)Velocity[j], unitdir);

			if(vi0 > 0 && vj0 < 0)
				return;
			
			var vi1 = vi0 * (mi - mj) / (mi + mj) + vj0 * (2 * mj) / (mi + mj);

			VelocityUpdate[i] += (Vector4)((vi1 - vi0) * unitdir);
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
