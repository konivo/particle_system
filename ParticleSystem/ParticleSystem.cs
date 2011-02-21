using System;
using OpenTK;
namespace opentk
{
	public class ParticleSystem
	{
		public readonly Vector4[] Position;
		public readonly Vector4[] OrigPosition;
		public readonly Vector2[] Oscilation;
		public readonly float[] Phase;
		public readonly Vector4[] ColorAndSize;
	
		public ParticleSystem (Vector4[] position, Vector4[] colorandsize)
		{
			//initialize particle system
			for (int i = 0; i < position.Length; i++) {
				double phi = 2 * Math.PI * i / (double)position.Length;
				position[i] = new Vector4 ((float)Math.Cos (phi), (float)Math.Sin (phi), 0, 1);
			}
		
			Position = position;
			OrigPosition = (Vector4[])Position.Clone();
			
			Oscilation = new Vector2[position.Length];
			Phase = new float[position.Length];
			
			System.Random rnd = new Random();
			
			for (int i = 0; i < position.Length; i++) {
				Oscilation[i] = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
				Oscilation[i].Normalize();
				Phase[i] = (float)rnd.NextDouble();
				
				colorandsize[i] = 0.1f * new Vector4(0, 0, 0, (float)rnd.NextDouble());
			} 
		}
		
		public void Simulate(DateTime simulationTime)
		{
			for (int i = 0; i < Position.Length; i++) {
				double phi = simulationTime.Ticks / (double)100000000;
				phi = (phi - Math.Floor(phi))* Math.PI * 2;
				Position[i] = OrigPosition[i] + new Vector4(Oscilation[i] * (float)Math.Sin(Phase[i] + phi));
			}
		}
	}
}

