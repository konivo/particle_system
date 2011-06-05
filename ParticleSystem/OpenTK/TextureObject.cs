using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenTK
{
	/// <summary>
	///
	/// </summary>
	public sealed class TextureBinding : ObjectBinding
	{
		public string VariableName;
		public TextureBase Texture;
	}

	/// <summary>
	///
	/// </summary>
	public class TextureBindingSet : StatePart
	{
		public readonly List<TextureBinding> Bindings = new List<TextureBinding> ();

		public TextureBindingSet (params TextureBinding[] states)
		{
			Bindings.AddRange (states);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		private void PrintError (object bufferTarget)
		{
			var err = GL.GetError ();
			if (err != ErrorCode.NoError)
			{
				StackTrace str = new StackTrace ();
				Console.WriteLine ("error {0} at method {1}, line {2} when binding to target {3}", err, str.GetFrame (0).GetMethod ().Name, str.GetFrame (0).GetFileLineNumber (), bufferTarget);
			}
		}

		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			//for texture bindings
			UniformState m_TextureUniformState = new UniformState ();

			var textureBindings = Bindings
				.GroupBy (x => x.Texture)
				.Zip (Enum.GetValues (typeof(TextureUnit)).Cast<TextureUnit> (), (a, b) => new { texture = a.Key, bindings = a.AsEnumerable (), unit = b })
				.ToArray();

			return new Tuple<Action, Action> (() =>
			{
				//print unobserved errors so far
				GLHelper.PrintError ();

				//texture array binding
				foreach (var bgroup in textureBindings)
				{
					int samplerValue = bgroup.unit - TextureUnit.Texture0;

					GL.ActiveTexture (bgroup.unit);
					GLHelper.PrintError ();
					GL.BindTexture (bgroup.texture.Target, bgroup.texture.Handle);
					GLHelper.PrintError ();

					foreach (var _binding in bgroup.bindings)
					{
						m_TextureUniformState.Set (_binding.VariableName, samplerValue);

						//Console.WriteLine ("binding {0} to target {1}: {2}: {3}", bgroup.texture.Name, _binding.Texture.Target, _binding.VariableName, samplerValue);
						PrintError (_binding.Texture.Target);
					}
				}

				m_TextureUniformState.GetActivator (state).Activate ();
				GLHelper.PrintError ();

			}, null);
		}

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public class TextureBase : IDisposable, IHandle
	{
		/// <summary>
		///
		/// </summary>
		public struct Parameters
		{
			public bool? GenerateMipmap;
			public TextureMinFilter? MinFilter;
			public TextureMagFilter? MagFilter;

			public void SetParameters(TextureTarget target)
			{
				if(GenerateMipmap.HasValue)
					GL.TexParameter(target, TextureParameterName.GenerateMipmap, GenerateMipmap.Value? 1: 0);

				if(MinFilter.HasValue)
					GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)MinFilter.Value);

				if(MagFilter.HasValue)
					GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)MagFilter.Value);
			}
		}

		private Lazy<int> m_Handle;
		private int m_Width;
		private int m_Height;
		private int m_Depth;
		private int m_Count;
		private TextureTarget m_Target;
		private Parameters m_Params;

		public string Name;

		public virtual TextureTarget Target
		{
			get { return m_Target; }
			set { m_Target = value; }
		}


		public PixelInternalFormat InternalFormat;

		public virtual int Width
		{
			get { return m_Width; }
			set { m_Width = value; }
		}


		public virtual int Height
		{
			get { return m_Height; }
			set { m_Height = value; }
		}

		public virtual int Depth
		{
			get { return m_Depth; }
			set { m_Depth = value; }
		}

		public virtual int Count
		{
			get { return m_Count; }
			set { m_Count = value; }
		}

		public Parameters Params
		{
			get { return m_Params; }
			set { m_Params = value; }
		}

		public bool IsCreated
		{
			get{ return m_Handle.IsValueCreated; }
		}

		public int Handle
		{
			get { return m_Handle.Value; }
		}

		public TextureBase ()
		{
			m_Count = 1;

			m_Handle = new Lazy<int> (() =>
			{
				int result;
				result = GL.GenTexture ();
				Initialize (result);
				return result;
			});
		}

		protected virtual void Initialize (int handle)
		{	}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (m_Handle.IsValueCreated)
				GL.DeleteTextures (1, new int[] { Handle });
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public sealed class DataTexture<T> : TextureBase where T : struct
	{
		private T[,] m_Data2D;

		public T[,] Data2D
		{
			get { return m_Data2D; }
			set
			{
				if (m_Data2D == value)
					return;

				m_Data2D = value;
				base.Target = TextureTarget.Texture2D;

				if (m_Data2D != null)
				{
					base.Height = m_Data2D.GetLength(0);
					base.Width = m_Data2D.GetLength(1);

					if(IsCreated)
						Initialize (Handle);
				}
			}
		}

		public override TextureTarget Target
		{
			get
			{
				return base.Target;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public override int Count
		{
			get
			{
				return base.Count;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public override int Depth
		{
			get
			{
				return base.Depth;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public override int Height
		{
			get
			{
				return base.Height;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public override int Width
		{
			get
			{
				return base.Width;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public PixelFormat? Format
		{
			get;
			set;
		}

		public PixelType Type
		{
			get;
			private set;
		}

		public DataTexture ()
		{		}

		protected override void Initialize (int handle)
		{
			if (m_Data2D == null)
			{
				throw new InvalidOperationException ();
			}

			//select proper format and type
			if (typeof(T).IsAssignableFrom (typeof(Vector3)))
			{
				Type = PixelType.Float;
				Format = Format ?? PixelFormat.Rgb;
			}

			else if (typeof(T).IsAssignableFrom (typeof(Vector2)))
			{
				Type = PixelType.Float;
				Format = Format ?? PixelFormat.Rg;
			}
			else if (typeof(T).IsAssignableFrom (typeof(Vector4)))
			{
				Type = PixelType.Float;
				Format = Format ?? PixelFormat.Rgba;
			}
			else if (typeof(T) ==  typeof(float))
			{
				Type = PixelType.Float;
				Format = Format ?? PixelFormat.Red;
			}
			else throw new NotSupportedException ();

			Publish (handle, true);
			Console.WriteLine ("Texture {3}: {0}, {1}, {2}", typeof(T), -1, m_Data2D.Length, Name);
		}

		private void Publish (int handle, bool initializeParams)
		{
			GLHelper.PrintError ();

			GL.PixelStore (PixelStoreParameter.UnpackAlignment, 4);
			GL.PixelStore (PixelStoreParameter.UnpackImageHeight, 0);
			GL.PixelStore (PixelStoreParameter.UnpackRowLength, 0);

			GL.ActiveTexture (TextureUnit.Texture31);
			GL.BindTexture (Target, handle);

			Params.SetParameters(Target);

			GL.TexImage2D (Target, 0, InternalFormat, m_Data2D.GetLength (0), m_Data2D.GetLength (1), 0, Format.Value, Type, m_Data2D);

			GLHelper.PrintError ();
		}

		public void Publish ()
		{
			Publish (Handle, false);
		}

		public void PublishRect (int l, int r, int b, int t)
		{
			GLHelper.PrintError ();
			
			GL.PixelStore (PixelStoreParameter.UnpackAlignment, 4);
			GL.PixelStore (PixelStoreParameter.UnpackImageHeight, m_Data2D.GetLength (0));
			GL.PixelStore (PixelStoreParameter.UnpackRowLength, m_Data2D.GetLength (1));
			GL.PixelStore (PixelStoreParameter.UnpackSkipRows, b);
			GL.PixelStore (PixelStoreParameter.UnpackSkipPixels, l);
			
			GL.ActiveTexture (TextureUnit.Texture31);
			GL.BindTexture (TextureTarget.Texture2D, Handle);
			GL.TexSubImage2D (Target, 0, l, b, r - l, t - b, Format.Value, Type, m_Data2D);
			
			GLHelper.PrintError ();
		}
	}

	/// <summary>
	///
	/// </summary>
	public sealed class BufferTexture<T> : TextureBase where T : struct
	{
		public BufferObject<T> SourceBuffer;
	}
}

