using System;
using OpenTK;
namespace opentk
{
	public class ParticleSystem
	{
		public readonly Vector4[] Position;
		public readonly Vector4[] OrigPosition;
		public readonly Vector2[] Oscilation;
		public readonly Vector4[] Velocity;
		public readonly float[] Phase;
		public readonly Vector4[] ColorAndSize;

		public Vector4 Leader;
		public BezierCurveCubic? LeaderPath;
		public float LeaderPathPosition;
		
		public int Processed;
		
		public int EmittedCount = 1;

		public ParticleSystem (Vector4[] position, Vector4[] colorandsize)
		{
			//initialize particle system
			for (int i = 0; i < position.Length; i++)
			{
				double phi = 2 * Math.PI * i / (double)position.Length;
				position[i] = new Vector4 ((float)Math.Cos (phi), (float)Math.Sin (phi), 0, 1);
			}
			
			ColorAndSize = colorandsize;
			Position = position;
			OrigPosition = (Vector4[])Position.Clone ();
			
			Oscilation = new Vector2[position.Length];
			Phase = new float[position.Length];
			Velocity = new Vector4[position.Length];
			
			System.Random rnd = new Random ();
			
			for (int i = 0; i < position.Length; i++)
			{
				Oscilation[i] = new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ());
				Oscilation[i].Normalize ();
				Phase[i] = (float)rnd.NextDouble ();
				
				colorandsize[i] = 0.1f * new Vector4 (0, 0, 0, (float)rnd.NextDouble ());
				
				Velocity[i] = new Vector4 (0, 0.01f / (10 * ColorAndSize[i].W), 0, 0);
			}
		}
		
		private void Initialize(Vector4 position, int i)
		{
			System.Random rnd = new Random ();
			Position[i] = OrigPosition[i] = position;
			
			Oscilation[i] = new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ());
			Oscilation[i].Normalize ();
			Phase[i] = (float)rnd.NextDouble ();
			
			ColorAndSize[i] = 0.1f * new Vector4 (0, 0, 0, (float)rnd.NextDouble ());
			
			Velocity[i] = new Vector4 (0, 0.01f / (10 * ColorAndSize[i].W), 0, 0);
		}

		public void Simulate2 (DateTime simulationTime)
		{
			PreparePath();
			
			for (int i = 0; i < EmittedCount; i++, Processed = (Processed + 1) % Position.Length)
			{
				Initialize(new Vector4(LeaderPath.Value.CalculatePoint(LeaderPathPosition)), Processed);
			}
			
			LeaderPathPosition += 0.01f;
			double phi = simulationTime.Ticks / (double)5000000;
			phi = (phi - Math.Floor (phi)) * Math.PI * 2;
			
			for (int i = 0; i < Position.Length; i++)
			{
				OrigPosition[i] = OrigPosition[i] + new Vector4(0, 0.01f/ (10 * ColorAndSize[i].W), 0, 0);				
				Position[i] = OrigPosition[i] + new Vector4((float) Math.Sin(Phase[i] + phi / (10 * ColorAndSize[i].W)) * (float)Math.Pow( ColorAndSize[i].W, 2), 0, 0, 0);

			}
		}
		
		public void Simulate (DateTime simulationTime)
		{
			PreparePath ();
			
			for (int i = 0; i < EmittedCount; i++,Processed = (Processed + 1) % Position.Length)
			{
				Position[Processed] = new Vector4 (LeaderPath.Value.CalculatePoint (LeaderPathPosition));
				OrigPosition[Processed] = Position[Processed];
			}
			
			LeaderPathPosition += 0.01f;
			
			for (int i = 0; i < Position.Length; i++)
			{
				for (int j = 0; j < i; j++)
				{				
					var dir = Position[i] - Position[j];
					var mi = ColorAndSize[i].W;
					var mj = ColorAndSize[j].W;
					
					if(dir.LengthFast > (mj + mi))
						continue;
					
					var unitdir = Vector4.Normalize(dir);
					
					var vi0 = Vector4.Dot(Velocity[i], unitdir);
					var vj0 = Vector4.Dot (Velocity[j], unitdir);
					
					var vi1 = vi0 * (mi - mj)/(mi + mj) + vj0 * (2 * mj)/ (mi + mj);
					var vj1 = vj0 * (mj - mi) / (mi + mj) + vi0 * (2 * mi) / (mi + mj);
					
					Velocity[i] += (vi1 - vi0) * unitdir;
					Velocity[j] += (vj1 - vj0) * unitdir;
				}
			}
			
			for (int i = 0; i < Position.Length; i++)
			{
				Position[i] += Velocity[i] * 0.1f;
			}
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
					
					LeaderPath = new BezierCurveCubic (
						LeaderPath.Value.EndAnchor, 
						new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, 
						2 * LeaderPath.Value.EndAnchor - LeaderPath.Value.SecondControlPoint,  
						new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center);
					LeaderPathPosition -= 1.0f;
				}				
			}

			else
			{
				System.Random rnd = new Random ();
				float koef = 5.0f;
				
				Vector2 center = new Vector2(0.5f, 0.5f) * koef;
				
				LeaderPath = new BezierCurveCubic (
					new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, 
					new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, 
					new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center, 
					new Vector2 ((float)rnd.NextDouble (), (float)rnd.NextDouble ()) * koef - center);
				LeaderPathPosition = 0;
			}
			
		}
	}
}

