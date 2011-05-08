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
	public sealed class DrawFramebufferBinding : ObjectBinding
	{
		public string VariableName;
		public FramebufferAttachment? Attachment;
		public int Layer;
		public int Level;
		public TextureBase Texture;
	}

	/// <summary>
	///
	/// </summary>
	public class DefaultFramebuffer : StatePart
	{
		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			return new Tuple<Action, Action> (() =>
			{
				//print unobserved errors so far
				GLHelper.PrintError ();
				GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
				//todo: is this specific to currently bound framebuffer?
				GL.DrawBuffer(DrawBufferMode.Back);
			}, null);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class FramebufferBindingSet : StatePart
	{
		public static readonly StatePart Default = new DefaultFramebuffer();

		public readonly List<DrawFramebufferBinding> Bindings = new List<DrawFramebufferBinding> ();

		public FramebufferBindingSet (params DrawFramebufferBinding[] states)
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
			//for vertex arrays object
			int Handle = -1;
			int MaxColorNumber = -1;
			DrawBuffersEnum[] DrawBuffers = null;

			return new Tuple<Action, Action> (() =>
			{
				//print unobserved errors so far
				GLHelper.PrintError ();

				if (Handle != -1)
				{
					//vertex array binding
					GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, Handle);
					//todo: is this specific to currently bound framebuffer?
					GL.DrawBuffers(DrawBuffers.Length, DrawBuffers);
					GLHelper.PrintError ();
				}
				else
				{
					//one time initialization
					var program = state.GetSingleState<Program> ();
					
					if (program == null)
						return;

					program.EnsureLinked ();
					
					//
					GL.GenFramebuffers (1, out Handle);
					GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, Handle);

					int i = 0;
					var bbindings = Bindings
						.Select(
						  item =>
						  new {
							colorNumber = item.VariableName == null? -1: GL.GetFragDataLocation(program.Handle, item.VariableName),
							attachment = item.Attachment?? (FramebufferAttachment.ColorAttachment0 + i++),
							binding = item
						  })
						.ToArray();

					MaxColorNumber = bbindings.Aggregate(-1, (result, x) => Math.Max(x.colorNumber, result));

					var matching =
						from colorNumber in Enumerable.Range(0, MaxColorNumber + 1)
						join b in bbindings on colorNumber equals b.colorNumber
						into m
						select
							m.Count() == 0?
							DrawBuffersEnum.None:
							(DrawBuffersEnum)(m.First().attachment - FramebufferAttachment.ColorAttachment0 + DrawBuffersEnum.ColorAttachment0);

					DrawBuffers = matching.ToArray();

					foreach (var binding in bbindings)
					{
						var item = binding.binding;

						GL.FramebufferTexture(FramebufferTarget.DrawFramebuffer, binding.attachment, item.Texture.Handle, item.Level);
						Console.WriteLine ("binding {0} to target {1}: {2}: {3}", item.Texture.Name, binding.attachment, item.VariableName, binding.colorNumber);
						PrintError (item.Attachment);
					}
				}
			}, () =>
			{
				var hnd = Handle;
				if (hnd != -1)
					GL.DeleteFramebuffers (1, ref hnd);
			});
		}

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
		}
		#endregion
	}

}


