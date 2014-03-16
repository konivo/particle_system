using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;

namespace OpenTK
{
	/// <summary>
	///
	/// </summary>
	public sealed class ImageBinding : ObjectBinding
	{
		public string VariableName;
		public TextureBase Texture;
		public IValueProvider<TextureBase> DynamicTexture;
		public int? Layer;
	}
	/// <summary>
	///
	/// </summary>
	public class ImageBindingSet : StatePart, IEnumerable<ImageBinding>
	{
		private readonly List<ImageBinding> Bindings = new List<ImageBinding> ();

		public ImageBindingSet ()
		{
		}

		[System.Diagnostics.Conditional("DEBUG")]
		private void PrintError (object bufferTarget)
		{
			var err = GL.GetError ();
			if (err != ErrorCode.NoError) {
				StackTrace str = new StackTrace ();
				Console.WriteLine ("error {0} at method {1}, line {2} when binding to target {3}", err, str.GetFrame (0).GetMethod ().Name, str.GetFrame (0).GetFileLineNumber (), bufferTarget);
			}
		}

		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			//for texture bindings
			var bindingUniforms = new UniformState ();
			
			//statically assigned textures go first
			var textureBindings = Bindings
				.Where (x => x.Texture != null)
				.GroupBy (x => x.Texture, (texture, b) => new { texture, bindings = b.AsEnumerable ()})
				.ToArray ()
				.AsEnumerable ();

			//dynamically assigned are taken separately, cause they can change over time
			var dynamicTextureBindings =
				Bindings
				.Where (x => x.DynamicTexture != null)
				.GroupBy (x => x.DynamicTexture.Value, (texture, b) => new { texture, bindings = b.AsEnumerable ()});

			//
			textureBindings = textureBindings.Concat (dynamicTextureBindings);

			return new Tuple<Action, Action> (() =>
			{
				//print unobserved errors so far
				GLHelper.PrintError ();
				int unitCounter = 0;

				//texture array binding
				foreach (var bgroup in textureBindings) 
				{					
					GLExtensions.BindImageTexture (unitCounter, bgroup.texture.Handle, 0, true, 0, ImageAccess.ReadWrite, ImageFormat.RGBA32F);
					GLHelper.PrintError ();
					
					foreach(var binding in bgroup.bindings)
					{
						bindingUniforms.Set (binding.VariableName, unitCounter);
					}
					unitCounter++;
				}

				bindingUniforms.GetActivator (state).Activate ();
				GLHelper.PrintError ();

			}, null);
		}

		public void Add (string variablename, IValueProvider<TextureBase> texture)
		{
			Bindings.Add (new ImageBinding{ VariableName = variablename, DynamicTexture = texture });
		}

		public void Add (string variablename, TextureBase texture)
		{
			Bindings.Add (new ImageBinding{ VariableName = variablename, Texture = texture});
		}
		
		public void Add (string variablename, Func<TextureBase> val)
		{
			Add(variablename, ValueProvider.Create (val, null));
		}

		public void Add (ImageBinding binding)
		{
			Bindings.Add (binding);
		}

		#region IEnumerable[ImageBinding] implementation
		public IEnumerator<ImageBinding> GetEnumerator ()
		{
			return Bindings.GetEnumerator ();
		}
		#endregion

		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return Bindings.GetEnumerator ();
		}
		#endregion

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
		}
		#endregion
	}
}

