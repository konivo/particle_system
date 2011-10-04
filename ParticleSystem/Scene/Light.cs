using System;
using System.Linq;
using OpenTK;

namespace opentk.Scene
{
	/// <summary>
	///
	/// </summary>
	public class Light
	{
		public Vector3 Position
		{
			get; set;
		}

		public Vector3 Direction
		{
			get; set;
		}

		public LightType Type
		{
			get; set;
		}
	}
}

