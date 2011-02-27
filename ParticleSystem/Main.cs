using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Resources;

namespace opentk
{
	class MainClass
	{
		private const int PARTICLES_COUNT = 400;
		
		//private static ParticleSystem m_Particles = new ParticleSystem(100000);
		private static ArrayObject m_ParticleRenderingState;
		private static Program m_ParticleRenderingProgram;
		private static UniformState m_UniformState;
		private static MatrixStack m_TransformationStack;
		private static MatrixStack m_Projection;
		
		//private static BufferObject<Vector4> VelocityBuffer;
		private static BufferObject<Vector4> PositionBuffer;
		private static BufferObject<Vector4> ColorAndSize;
		
		private static ParticleSystem m_System;
		
		private static State m_SystemState;
		
		public static void Main (string[] args)
		{
			unsafe {
				//VelocityBuffer = new BufferObject<Vector4> (sizeof(Vector4), size) { Name = "velocity_buffer", Usage = BufferUsageHint.DynamicDraw };
				PositionBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				ColorAndSize = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "colorandsize_buffer", Usage = BufferUsageHint.DynamicDraw };
			}
		
			var win = 
				new OpenTK.GameWindow (200, 200, 
				                      GraphicsMode.Default, 
				                      "", 
				                      OpenTK.GameWindowFlags.Default
				                      );
			
			win.RenderFrame += HandleWinRenderFrame;
			win.Run ();
			
			Console.WriteLine ("Hello World!");
		}
		
		unsafe static void PrepareState ()
		{
			if (m_ParticleRenderingState != null)
			{							
				m_System.Simulate(DateTime.Now);
				
				PositionBuffer.Publish();
				ColorAndSize.Publish();			
				m_SystemState.Activate();
				return;
			}
			
			
			m_Projection = 
				new MatrixStack()
					.Push(Matrix4.CreateOrthographic(14, 14, -1, 1));
			
			m_TransformationStack = 
				new MatrixStack(m_Projection)
					.Push(Matrix4.Identity)
					.Push(Matrix4.Identity);
			
			m_UniformState = 
				new UniformState()
					.Set ("color", new Vector4(0, 0, 1,1))
					.Set ("red", 1.0f)
					.Set ("green", 0.0f)
					.Set ("blue", 1.0f)
					.Set ("colors", new float[]{ 0, 1, 0, 1})
					.Set ("colors2", new Vector4[] { new Vector4(1, 0.1f, 0.1f, 0), new Vector4 (1, 0, 0, 0), new Vector4 (1, 1, 0.1f, 0) })
					.Set ("modelview_transform", m_TransformationStack);
			
			var sprite = new [] {
				new Vector3 (-1, -1, 0), 
				new Vector3 (-1, 1, 0), 
				new Vector3 (1, 1, 0), 
				new Vector3 (1, -1, 0)
			};
			
			var vdata_buffer = new BufferObject<Vector3> (sizeof(Vector3)) { Name = "vdata_buffer", Usage = BufferUsageHint.DynamicDraw, Data = sprite};
		
			m_ParticleRenderingState = 
				new ArrayObject (
				
				new VertexAttribute {
					AttributeName = "vertex_pos",
					Buffer = vdata_buffer,
					Size = 3, 
					Type = VertexAttribPointerType.Float
					},
				new VertexAttribute { 
					AttributeName = "sprite_pos", 
					Buffer = PositionBuffer,
					Divisor = 1,
					Size = 4, 
					Stride = 0, 
					Type = VertexAttribPointerType.Float },
				new VertexAttribute { 
					AttributeName = "sprite_colorandsize", 
					Buffer = ColorAndSize,
					Divisor = 1,
					Size = 4, 
					Type = VertexAttribPointerType.Float }
				);
			
			var shaders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames()
				where res.Contains("glsl")
				select new Shader(res, ResourcesHelper.GetText(res, System.Text.Encoding.UTF8));

			m_ParticleRenderingProgram = new Program(
				"main_program",
				shaders.ToArray()
			);
			
			m_SystemState = new State
			(
				null,
				m_ParticleRenderingState,
				m_ParticleRenderingProgram,
				m_UniformState
				
			);
			
			var hnd = PositionBuffer.Handle;
			hnd = ColorAndSize.Handle;
			
			m_System = new ParticleSystem (PositionBuffer.Data, ColorAndSize.Data );			
			PrepareState ();
		}
		
		static void SetCamera (GameWindow window)
		{
			float aspect = window.Height/(float)window.Width;
			float projw = 14;		
			GL.Viewport(0, 0, window.Width, window.Height);
			
			if(m_Projection != null)
				m_Projection.Stack[0] = Matrix4.CreateOrthographic(projw, projw * aspect, -1, 1);
		}

		static void HandleWinRenderFrame (object sender, OpenTK.FrameEventArgs e)
		{
			var window = sender as GameWindow;
			
			GL.ClearColor (Color4.Black);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
			GL.BlendEquation(BlendEquationMode.FuncAdd);
			
			SetCamera (window);
			PrepareState ();
			GL.DrawArraysInstanced (BeginMode.TriangleFan, 0, 4, PARTICLES_COUNT);
			
			window.SwapBuffers ();
		}
		
		private static void UpdateStates ()
		{
			//map buffer
			
			//modify it ...
			
			//unmap it
		}
	}
}

