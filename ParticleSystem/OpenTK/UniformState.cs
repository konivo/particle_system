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
	public class UniformState : StatePart
	{
		private readonly Dictionary<string, object> m_Values = new Dictionary<string, object> ();

		public readonly bool Default;
		public readonly string Name;

		public UniformState ()
		{
			Default = true;
			Name = string.Empty;
		}

		public UniformState (string name)
		{
			Default = false;
			Name = name;
		}

		public UniformState Set<T> (string name, T val)
		{
			m_Values[name] = val;
			return this;
		}

		public UniformState Set<T> (string name, IValueProvider<T> val)
		{
			m_Values[name] = val;
			return this;
		}

		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			return new Tuple<Action, Action> (() =>
			{
				var program = state.GetSingleState<Program> ();
				
				if (program != null)
				{
					foreach (var item in m_Values)
					{
						string name = GetValueName (item.Key, item.Value);
						
						int uloc = GL.GetUniformLocation (program.Handle, name);
						if (uloc >= 0)
						{
							SetValue (program.Handle, uloc, name, item.Value);
						}
					}
				}
			}, null);
		}

		private string GetValueName (string prefix, object val)
		{
			return prefix;
		}

		private void SetValueDynamic(int program, int location, string name, dynamic valueprovider)
		{
			SetValue(program, location, name, valueprovider.Value);
		}

		private void SetValue (int program, int location, string name, object val)
		{
			if (val is Matrix4)
			{
				var mat = (Matrix4)val;
				GL.UniformMatrix4 (location, false, ref mat);
			}

			else if (val is Vector4)
			{
				var mat = (Vector4)val;
				GL.Uniform4 (location, ref mat);
			}
			else if (val is Vector3)
			{
				var mat = (Vector3)val;
				GL.Uniform3 (location, ref mat);
			}
			else if (val is Vector2)
			{
				var mat = (Vector2)val;
				GL.Uniform2 (location, ref mat);
			}
			else if (val is float)
			{
				var mat = (float)val;
				GL.Uniform1 (location, mat);
			}
			else if (val is Array)
			{
				var elementtype = val.GetType ().GetElementType ();
				
				if (elementtype == typeof(float))
				{
					GL.Uniform1 (location, ((Array)val).Length, (float[])val);
				}

				else
				{
					for (int i = 0; i < ((Array)val).Length; i++)
					{
						SetValue (program, location, name + "[" + i + "]", ((Array)val).GetValue (i));
					}
				}
			}
			//todo: generate properly and generically
			else// if (val.GetType().GetGenericTypeDefinition() == typeof(IValueProvider<>))
			{
				dynamic provider = val;
				SetValue (program, location, name, provider.Value);
			}
		}
	}
	
}
