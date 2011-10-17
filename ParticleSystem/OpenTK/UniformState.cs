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
	using Key = Tuple<int, string>;
	public class UniformState : StatePart
	{
	/// <summary>
	///
	/// </summary>
		private class SubroutineList
		{
			private readonly int[] m_Indexes = new int[100];
			private int m_Peak = -1;

			public SubroutineList ()
			{
				Reset();
			}

			public void Set(int location, int subroutineIndex)
			{
				m_Peak = Math.Max(location, m_Peak);
				m_Indexes[location] = subroutineIndex;
			}

			public int[] GetList()
			{
				var result = new int[m_Peak + 1];
				Array.Copy(m_Indexes, result, result.Length);

				return result;
			}

			public void Reset()
			{
				for(int i=0;i<m_Indexes.Length; i++){
					m_Indexes[i] = -1;
				}

				m_Peak = -1;
			}
		}

		private readonly Dictionary<Key, object> m_Values = new Dictionary<Key, object> ();
		private readonly UniformState[] m_BaseUniforms;
		private readonly Dictionary<ShaderType, SubroutineList> m_StageSubroutines = new Dictionary<ShaderType, SubroutineList>()
		{
			{ ShaderType.FragmentShader, new SubroutineList()},
			{ ShaderType.GeometryShader, new SubroutineList()},
			{ ShaderType.VertexShader, new SubroutineList()}
		};

		public readonly bool Default;
		public readonly string Name;

		public UniformState (params UniformState[] baseUniforms)
		{
			Default = true;
			Name = string.Empty;

			m_BaseUniforms = baseUniforms.ToArray();
		}

		public UniformState (string name, params UniformState[] baseUniforms)
		{
			Default = false;
			Name = name;

			m_BaseUniforms = baseUniforms.ToArray();
		}

		public UniformState Set<T> (string name, T val)
		{
			m_Values[GetKey(name)] = val;
			return this;
		}

		public UniformState Set<T> (string name, IValueProvider<T> val)
		{
			m_Values[GetKey(name)] = val;
			return this;
		}

		public UniformState Set<T> (string name, ShaderType stage, T val)
		{
			m_Values[GetKey(name, stage)] = val;
			return this;
		}

		public UniformState Set<T> (string name, ShaderType stage, IValueProvider<T> val)
		{
			m_Values[GetKey(name, stage)] = val;
			return this;
		}

		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			return new Tuple<Action, Action> (() =>
			{
				var program = state.GetSingleState<Program> ();
				
				if (program != null)
				{
					//TODO: this is the responsibility of another state part .. so it should be done in a different way
					//or use ProgramUniform
					GL.UseProgram(program.Handle);

					foreach(var subroutines in m_StageSubroutines.Values)
					{
						subroutines.Reset();
					}

					foreach(var dict in m_BaseUniforms.Concat(new [] {this}))
					{
						foreach (var item in dict.m_Values)
						{
							string name = GetValueName (item.Key, item.Value);
							var stage = GetStageName(item.Key);
							int uloc;

							if(stage == null &&
							   (uloc = GL.GetUniformLocation (program.Handle, name)) >= 0)
							{
								SetValue (program.Handle, uloc, null, name, item.Value);
							}
							else if(
							   stage != null &&
							   (uloc = GLExtensions.GetSubroutineUniformLocation(program.Handle, stage.Value, name)) >= 0)
							{
								SetValue(program.Handle, uloc, stage, name, item.Value);
							}
						}
					}

					foreach(var subroutines in m_StageSubroutines)
					{
						var sublist = subroutines.Value.GetList();

						if(sublist.Length > 0)
							GLExtensions.UniformSubroutinesuiv(subroutines.Key, sublist);
					}
				}
			}, null);
		}

		private void SetValueDynamic(int program, int location, ShaderType? stage, string name, dynamic valueprovider)
		{
			SetValue(program, location, stage, name, valueprovider.Value);
		}

		private void SetValue (int program, int location, ShaderType? stage, string name, object val)
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
			else if (val is int)
			{
				var mat = (int)val;
				GL.Uniform1 (location, mat);
			}
			else if (val is bool)
			{
				var vval = (bool)val;
				GL.Uniform1 (location, vval? 1 : 0);
			}
			else if (val is string)
			{
			//TODO: add check, that the subroutine exists
				var vval = (string)val;
				if(stage != null)
					//get subroutine index
					m_StageSubroutines[stage.Value].Set(location, GLExtensions.GetSubroutineIndex(program, stage.Value, vval));
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
						int uloc = GL.GetUniformLocation (program, name + "[" + i + "]");
						if (uloc >= 0)
						{
							SetValue (program, uloc, stage, name + "[" + i + "]",  ((Array)val).GetValue (i));
						}
					}
				}
			}
			//todo: generate properly and generically
			else// if (val.GetType().GetGenericTypeDefinition() == typeof(IValueProvider<>))
			{
				dynamic provider = val;
				SetValue (program, location, stage, name, provider.Value);
			}
		}

		private string GetValueName (Key prefix, object val)
		{
			return prefix.Item2;
		}

		private ShaderType? GetStageName (Key prefix)
		{
			return prefix.Item1 == -1? null: (ShaderType?) prefix.Item1;
		}

		private Key GetKey(string name)
		{
			return Tuple.Create(-1, name);
		}

		private Key GetKey(string name, ShaderType type)
		{
			return Tuple.Create((int)type, name);
		}
	}
	
}
