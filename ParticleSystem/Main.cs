using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Resources;
using System.Windows.Forms;
using System.Drawing;

namespace opentk
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var win = new OpenTK.GameWindow (200, 200, GraphicsMode.Default, "", OpenTK.GameWindowFlags.Default);

			var form1 = new Form ();
			form1.Size = new Size(400, 1000);
			PropertyGrid propertyGrid1 = new PropertyGrid ();
			propertyGrid1.CommandsVisibleIfAvailable = true;
			propertyGrid1.Location = new Point (10, 20);
			propertyGrid1.TabIndex = 1;
			propertyGrid1.Text = "Property Grid";
			propertyGrid1.Dock = DockStyle.Fill;
			propertyGrid1.Font = new Font("URW Gothic L", 10.25f, GraphicsUnit.Point);
			propertyGrid1.CategoryForeColor = SystemColors.ControlLight;
			propertyGrid1.ViewForeColor = SystemColors.ControlText;
			propertyGrid1.ViewBackColor = SystemColors.Control;
			propertyGrid1.LineColor = SystemColors.ControlLight;
			form1.Controls.Add (propertyGrid1);
			form1.Show ();

			win.RenderFrame += (sender, aaa) => { Application.DoEvents (); };

			var system = (new System3.System3 ()).GetInstance (win);
			propertyGrid1.SelectedObject = system;
			win.Run ();
		}
	}
}

