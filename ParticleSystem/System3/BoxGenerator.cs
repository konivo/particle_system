using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace opentk.System3
{
	/// <summary>
	/// Generates particles randomly in a box of specified dimensions
	/// </summary>
	public class BoxGenerator: SimpleGenerator, IParticleGenerator
	{
		public float BoxInnerSize
		{
			get; 
			set;
		}
		
		public float BoxOuterSize
		{
			get; 
			set;
		}
	
		public new string Name
		{
			get
			{
				return "BoxGenerator";
			}
		}
		
		public BoxGenerator()
		{
			BoxInnerSize = 50;
			BoxOuterSize = 75;
		}
		
		protected override Vector3 NewPosition(System3 system, int bundleFirstItem, int i)
		{
			var dir = (Vector3) MathHelper2.RandomVector3(1);
			var l = dir.Length;
			var k = (float) MathHelper2.GetThreadLocalRandom().NextDouble();			
			return dir * BoxOuterSize * k + dir * BoxInnerSize * (1 - k);
		}
	}
}
