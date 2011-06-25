using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using System.ComponentModel;

namespace opentk.System3
{
	/// <summary>
	///
	/// </summary>
	public class AocParameters
	{
		public int SamplesCount
		{
			get; set;
		}

		public float OccMaxDist
		{
			get;
			set;
		}

		public float OccPixmax
		{
			get;
			set;
		}

		public float OccPixmin
		{
			get;
			set;
		}

		public float OccMinSampleRatio
		{
			get;
			set;
		}

		public bool OccConstantArea
		{
			get;
			set;
		}

		public int AocTextureSize
		{
			get;
			set;
		}
	}
}

