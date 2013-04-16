using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms;

namespace opentk.PropertyGridCustom
{
	public class ContrastEditor : UITypeEditor
	{
		/// <summary>
		/// Summary description for frmContrast.
		/// </summary>

		public class frmContrast : System.Windows.Forms.UserControl
		{
			public float BarValue;
			public IWindowsFormsEditorService _wfes;
			public System.Windows.Forms.TrackBar trackBar1;

			private System.Windows.Forms.Button btnTrack;
			/// <summary>
			/// Required designer variable.
			/// </summary>
			private System.ComponentModel.Container components = null;

			public frmContrast ()
			{
				//
				// Required for Windows Form Designer support
				//
				InitializeComponent ();
				btnTrack.Text = BarValue.ToString ();

				//
				// TODO: Add any constructor code after InitializeComponent call
				//
			}

			/// <summary>
			/// Clean up any resources being used.
			/// </summary>
			protected override void Dispose (bool disposing)
			{
				if (disposing)
				{
					if (components != null)
					{
						components.Dispose ();
					}
				}
				base.Dispose (disposing);
			}


			#region Windows Form Designer generated code
			/// <summary>
			/// Required method for Designer support - do not modify
			/// the contents of this method with the code editor.
			/// </summary>
			private void InitializeComponent ()
			{
				this.trackBar1 = new System.Windows.Forms.TrackBar ();
				this.btnTrack = new System.Windows.Forms.Button ();
				((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit ();
				this.SuspendLayout ();
				//
				// trackBar1
				//
				this.trackBar1.LargeChange = 10;
				this.trackBar1.Location = new System.Drawing.Point (0, 8);
				this.trackBar1.Maximum = 1000;
				this.trackBar1.Minimum = 0;
				this.trackBar1.Name = "trackBar1";
				this.trackBar1.Size = new System.Drawing.Size (152, 45);
				this.trackBar1.TabIndex = 0;
				this.trackBar1.TickFrequency = 10;
				this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.Both;
				this.trackBar1.ValueChanged += new System.EventHandler (this.trackBar1_ValueChanged);
				//
				// btnTrack
				//
				this.btnTrack.DialogResult = System.Windows.Forms.DialogResult.OK;
				this.btnTrack.Location = new System.Drawing.Point (160, 16);
				this.btnTrack.Name = "btnTrack";
				this.btnTrack.Size = new System.Drawing.Size (25, 30);
				this.btnTrack.TabIndex = 2;
				this.btnTrack.Text = "45";
				this.btnTrack.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
				//this.btnTrack.Click += new System.EventHandler (this.btnTrack_Click);
				//
				// frmContrast
				//
				this.Size = new System.Drawing.Size (192, 70);
				this.Controls.Add (this.btnTrack);
				this.Controls.Add (this.trackBar1);
				this.Name = "frmContrast";
				((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit ();
				this.ResumeLayout (false);
				
			}
			#endregion

			private void trackBar1_ValueChanged (object sender, System.EventArgs e)
			{
				BarValue = trackBar1.Value/100.0f;
				btnTrack.Text = BarValue.ToString ();
				
			}

			private void btnTrack_Click (object sender, System.EventArgs e)
			{
				_wfes.CloseDropDown ();
			}

			private void frmContrast_Closed (object sender, System.EventArgs e)
			{
				_wfes.CloseDropDown ();
			}
		}

		public override UITypeEditorEditStyle GetEditStyle (ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.DropDown;
		}

		public override object EditValue (ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			IWindowsFormsEditorService wfes = provider.GetService (typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
			
			if (wfes != null)
			{
				var instance = context.Instance;
				var prop = context.PropertyDescriptor;
				frmContrast _frmContrast = new frmContrast ();
				_frmContrast.trackBar1.Value = (int)((float)value * 100);
				_frmContrast.trackBar1.MouseUp += (sender, e) => 
				{
					wfes.CloseDropDown ();					
				};
				_frmContrast.BarValue = ((float)value);
				_frmContrast._wfes = wfes;
				
				wfes.DropDownControl(_frmContrast);				
				value = _frmContrast.BarValue;
			}
			return value;
		}
	}
}
