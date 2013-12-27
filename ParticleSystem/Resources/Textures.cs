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
using OpenTK.Graphics;
using System.Windows;

namespace opentk.Resources
{
	/// <summary>
	/// Color ramps.
	/// </summary>
	public static class Textures
	{
		public static TextureBase FromVectors(string name, int size, double magnitude, params Vector2d[] dataset)
		{
			var data = new float[size, size];
			for (int i = 0; i < dataset.Length; i++) {
				var fpixel = Vector2d.Multiply (dataset[i], 0.5 * size/magnitude) + new Vector2d(size * 0.5);
				data[(int)fpixel.Y % size, (int)fpixel.X % size] = 1; 
			}

			var texture = new DataTexture<float>
			{
				Name = "from_vectors_" + name,
				InternalFormat = PixelInternalFormat.R32f,
				Data2D = data,
				Params = new TextureBase.Parameters
				{
					GenerateMipmap = false,
					MinFilter = TextureMinFilter.Linear,
					MagFilter = TextureMagFilter.Linear
				}};

		  return texture;
		}
	}
}