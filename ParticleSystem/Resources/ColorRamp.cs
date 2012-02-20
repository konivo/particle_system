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

namespace opentk.System3
{
	/// <summary>
	///
	/// </summary>
	public sealed class ColorRamp
	{
		[Browsable(false)]
		public string Name	{	get; private set; }

		[Browsable(false)]
		public TextureBase Texture { get; private set;}

		public ColorRamp(string name, TextureBase texture)
		{
			Name = name;
			Texture = texture;
		}
	}

	/// <summary>
	/// Color ramps.
	/// </summary>
	public static class ColorRamps
	{
		[Export]
		public static readonly ColorRamp RedBlue = FromValues("Red-Blue", Color4.Red, Color4.Blue);

		[Export]
		public static readonly ColorRamp RedGreen = FromValues("Red-Green", Color4.Green);

		private static ColorRamp FromValues(string name, params OpenTK.Graphics.Color4[] colors)
		{
			var sizeX = 2;
			var sizeY = 1;
			var data = new Color4[sizeY, sizeX];

			//var dX = sizeX / (float)colors.Length;
			//var dY = sizeY / (float)colors.Length;
			//		var colorIndex = 0;

			for (int i = 0; i < sizeY; i++)
			{
				for (int j = 0; j < sizeX; j++)
				{
					//var t = dX * (j / colors.Length);
					var cindex = j % colors.Length;
					data[i, j] = colors[cindex];
				}
			}

			var texture = new DataTexture<Color4>
			{
				Name = "color-ramp-" + name,
				InternalFormat = PixelInternalFormat.Rgb,
				Data2D = data,
				Params = new TextureBase.Parameters
				{
					GenerateMipmap = false,
					MinFilter = TextureMinFilter.Linear,
					MagFilter = TextureMagFilter.Linear
				}};

		  return new ColorRamp(name, texture);
		}
	}
}