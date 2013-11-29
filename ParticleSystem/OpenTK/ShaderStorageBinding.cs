using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenTK
{
	/// <summary>
	///
	/// </summary>
	public sealed class ShaderStorageBinding : BufferObjectBinding
	{	
		public string StorageName;
		
		public override BufferTarget Target 
		{
			get 
			{
				//SHADER_STORAGE_BUFFER
				return (BufferTarget)0x90D2;
			}
			set {	}
		}
	}
	/// <summary>
	///
	/// </summary>
	public class ShaderStorageSet : StatePart, IEnumerable<ShaderStorageBinding>
	{
		public readonly List<ShaderStorageBinding> Bindings = new List<ShaderStorageBinding> ();

		public ShaderStorageSet (params ShaderStorageBinding[] states)
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
			var emitLog = true;
			
			return new Tuple<Action, Action> (() =>
			{
				//print unobserved errors so far
				GLHelper.PrintError ();
				
				//one time initialization
				var program = state.GetSingleState<Program> ();				
				if (program == null)
					return;				
				program.EnsureLinked ();
				
				//
				int bindingIndex = 0;
				foreach (var item in Bindings)
				{
					int blockIndex = GLExtensions.GetProgramResourceIndex(program.Handle, ProgramInterface.ShaderStorageBlock, item.StorageName);
					if (blockIndex == -1)
						continue;			
					
					GL.BindBufferBase(item.Target, bindingIndex, item.Buffer.Handle);
					GLExtensions.BindShaderStorage(program.Handle, blockIndex, bindingIndex);
					
					if(emitLog)
						Console.WriteLine ("binding {0} to target {1}: {2}: {3}", item.Buffer.Name, "SHADER_STORAGE", item.StorageName, blockIndex);
						
					PrintError (item.Target);
					
					bindingIndex++;
				}
				
				emitLog = false;
			}, 
			() =>
			{
				//
			});
		}
		
		public void Add (string storageName, BufferObjectBase buffer)
		{
			Bindings.Add (new ShaderStorageBinding { StorageName = storageName, Buffer = buffer });
		}
		
		public void Add (ShaderStorageBinding binding)
		{
			Bindings.Add (binding);
		}
		
		#region IEnumerable[ShaderStorageBinding] implementation
		public IEnumerator<ShaderStorageBinding> GetEnumerator ()
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

