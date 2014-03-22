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
	public sealed class TextureBinding : ObjectBinding
	{
		public string VariableName;
		public TextureBase Texture;
		public IValueProvider<TextureBase> DynamicTexture;
	}

	/// <summary>
	///
	/// </summary>
	public class TextureBindingSet : StatePart, IEnumerable<TextureBinding>
	{
		private readonly List<TextureBinding> Bindings = new List<TextureBinding> ();

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

			var textureUnits = Enum.GetValues (typeof(TextureUnit)).Cast<TextureUnit> ().ToArray();

			//statically assigned textures go first
			var textureBindings = Bindings
				.Where(x => x.Texture != null)
				.GroupBy (x => x.Texture, (texture, b) => new { texture, bindings = b.AsEnumerable ()})
				.ToArray()
				.AsEnumerable();

			//dynamically assigned are taken separately, cause they can change over time
			var dynamicTextureBindings =
				Bindings
				.Where(x => x.DynamicTexture != null)
				.GroupBy (x => x.DynamicTexture.Value, (texture, b) => new { texture, bindings = b.AsEnumerable ()});

			//
			textureBindings = textureBindings.Concat(dynamicTextureBindings);

			return new Tuple<Action, Action> (() =>
			{
				//print unobserved errors so far
				GLHelper.PrintError ();
				int unitCounter = 0;

				//texture array binding
				foreach (var bgroup in textureBindings)
				{
					var unit = textureUnits[unitCounter++];
					var samplerValue = unit - TextureUnit.Texture0;

					GL.ActiveTexture (unit);
					GLHelper.PrintError ();
					GL.BindTexture (bgroup.texture.Target, bgroup.texture.Handle);
					GLHelper.PrintError ();

					//if mipmaps are declared to be generated, ensure they are
					var gmipmap = bgroup.texture.Params.GenerateMipmap;
					if(gmipmap ?? false)
					{
						GL.GenerateMipmap((GenerateMipmapTarget)bgroup.texture.Target);
						GLHelper.PrintError ();
					}

					foreach (var _binding in bgroup.bindings)
					{
						m_TextureUniformState.Set (_binding.VariableName, samplerValue);
						PrintError (bgroup.texture.Target);
					}
				}

				m_TextureUniformState.GetActivator (state).Activate ();
				GLHelper.PrintError ();

			}, null);
		}

		public void Add (string variablename, IValueProvider<TextureBase> texture)
		{
			Bindings.Add(new TextureBinding{ VariableName = variablename, DynamicTexture = texture });
		}

		public void Add (string variablename, TextureBase texture)
		{
			Bindings.Add(new TextureBinding{ VariableName = variablename, Texture = texture});
		}

		public void Add (TextureBinding binding)
		{
			Bindings.Add(binding);
		}

		#region IEnumerable[TextureBinding] implementation
		public IEnumerator<TextureBinding> GetEnumerator ()
		{
			return Bindings.GetEnumerator();
		}
		#endregion

		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return Bindings.GetEnumerator();
		}
		#endregion

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public abstract class TextureBase : IDisposable, IHandle
	{
		private class Change: IDisposable
		{
			public TextureBase m_Texture;
			
			#region IDisposable implementation
			public void Dispose ()
			{
				m_Texture.m_IsPerformingChanges = false;
				
				if(m_Texture.IsCreated)
					m_Texture.Initialize(m_Texture.Handle);
			}
			#endregion
		}
	
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
		private volatile bool m_IsPerformingChanges;

		public string Name
		{
			get;
			set;
		}

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
		
		public bool IsBulkChange
		{
			get{ return m_IsPerformingChanges; }
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
				
				if(!m_IsPerformingChanges)
					Initialize (result);
					
				return result;
			});
		}
		
		public IDisposable BulkChange()
		{
			m_IsPerformingChanges = true;
			return new Change{ m_Texture = this};
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
			else if (
				typeof(T).IsAssignableFrom (typeof(Vector4)) ||
				typeof(T).IsAssignableFrom( typeof(Color4)))
			{
				Type = PixelType.Float;
				Format = Format ?? PixelFormat.Rgba;
			}
			else if (typeof(T) ==  typeof(float))
			{
				Type = PixelType.Float;
				Format = Format ?? PixelFormat.Red;
			}
			else if (typeof(T) ==  typeof(int))
			{
				Type = PixelType.Int;
				Format = Format ?? PixelFormat.RedInteger;
			}
			else if (typeof(T) ==  typeof(short))
			{
				Type = PixelType.Short;
				Format = Format ?? PixelFormat.RedInteger;
			}
			else if (typeof(T) ==  typeof(sbyte))
			{
				Type = PixelType.Byte;
				Format = Format ?? PixelFormat.RedInteger;
			}
			else throw new NotSupportedException ();

			Publish (handle, true);
			Console.WriteLine ("Texture {4}: {0}+{1}, {2}, {3}", typeof(T), InternalFormat, -1, m_Data2D.Length, Name);
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
	public sealed class Texture : TextureBase
	{		
		public override TextureTarget Target
		{
			get
			{
				return base.Target;
			}
			set
			{
				if (base.Target == value)
					return;
					
				base.Target = value;
				if(IsCreated && !IsBulkChange)
					Initialize (Handle);
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
				if (base.Depth == value)
					return;
				
				base.Depth = value;
				if(IsCreated && !IsBulkChange)
					Initialize (Handle);
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
				if (base.Height == value)
					return;
				
				base.Height = value;
				if(IsCreated && !IsBulkChange)
					Initialize (Handle);
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
				if (base.Width == value)
					return;
				
				base.Width = value;
				if(IsCreated && !IsBulkChange)
					Initialize (Handle);
			}
		}
		
		public Texture ()
		{		}
		
		protected override void Initialize (int handle)
		{			
			GLHelper.PrintError ();
			GL.ActiveTexture (TextureUnit.Texture31);
			GL.BindTexture (Target, handle);
			Params.SetParameters(Target);
			
			var format = PixelFormat.Rgba;
			var type = PixelType.Int;
			var size = 0;

			switch (Target) {
			case TextureTarget.Texture1D:
				GL.TexImage1D (Target, 0, InternalFormat, Math.Max(1, Width), /* border */0, format, type, (IntPtr)0);
				size = Math.Max (1, Width);
				break;
			case TextureTarget.Texture2D:
				GL.TexImage2D (Target, 0, InternalFormat, Math.Max(1, Width), Math.Max (1, Height), /* border */0, format, type, (IntPtr)0);
				size = Math.Max (1, Width) * Math.Max (1, Height);
				break;
			case TextureTarget.Texture3D:
				GL.TexImage3D (Target, 0, InternalFormat, Math.Max(1, Width), Math.Max (1, Height), Math.Max (1, Depth), /* border */0, format, type, (IntPtr)0);
				size = Math.Max (1, Width) * Math.Max (1, Height) * Math.Max (1, Depth);
				break;
			}
			GLHelper.PrintError ();			
			Console.WriteLine ("Texture {3}: {0}, {1}, {2}", InternalFormat, -1, size, Name);
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

