
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
	public class GridGenerator: SimpleGenerator, IParticleGenerator
	{
		public float StepX {
			get; 
			set;
		}
		
		public float StepY {
			get; 
			set;
		}
		
		public float StepZ {
			get; 
			set;
		}
		
		public float AmountX {
			get; 
			set;
		}
		
		public float AmountY {
			get; 
			set;
		}
		
		public float AmountZ {
			get; 
			set;
		}
		
		public new string Name {
			get {
				return "GridGenerator";
			}
		}
		
		public GridGenerator ()
		{
			StepX = StepY = StepZ = 1;
			AmountX = AmountY = 0.5f;
			AmountZ = 2;
		}
		
		protected override Vector3 NewPosition (System3 system, int bundleFirstItem, int i)
		{
			var pcount = system.PARTICLES_COUNT;
			var amount = Math.Max (AmountX * AmountY * AmountZ, float.Epsilon);
			var gridCount = (int)Math.Floor (Math.Pow (pcount / amount, 1 / 3.0));

			var gcx = (int)Math.Max(gridCount * AmountY * gridCount * AmountZ, 1);
			var gcy = (int)Math.Max(gridCount * AmountZ, 1);
			
			int ix = i / gcx;
			int iy = (i - gcx * ix) / gcy;
			int iz = i - gcx * ix - gcy * iy;
			
			return 
				Vector3.Multiply (new Vector3 (ix, iy, iz), new Vector3 (StepX, StepY, StepZ)) -
				Vector3.Multiply (new Vector3 (gridCount * StepX, gridCount * StepY, gridCount * StepZ), new Vector3 (AmountX, AmountY, AmountZ)) / 2;
		}
	}
}
