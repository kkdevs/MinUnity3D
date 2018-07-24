	partial class Form1
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.log = new System.Windows.Forms.RichTextBox();
			this.progressRatio = new System.Windows.Forms.ProgressBar();
			this.progressFile = new System.Windows.Forms.ProgressBar();
			this.textRatio = new System.Windows.Forms.Label();
			this.textProgress = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// log
			// 
			this.log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.log.Location = new System.Drawing.Point(13, 115);
			this.log.Name = "log";
			this.log.ReadOnly = true;
			this.log.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
			this.log.Size = new System.Drawing.Size(548, 110);
			this.log.TabIndex = 0;
			this.log.Text = "";
			// 
			// progressRatio
			// 
			this.progressRatio.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressRatio.Location = new System.Drawing.Point(13, 74);
			this.progressRatio.Name = "progressRatio";
			this.progressRatio.Size = new System.Drawing.Size(548, 23);
			this.progressRatio.TabIndex = 1;
			// 
			// progressFile
			// 
			this.progressFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressFile.Location = new System.Drawing.Point(13, 28);
			this.progressFile.Name = "progressFile";
			this.progressFile.Size = new System.Drawing.Size(548, 23);
			this.progressFile.TabIndex = 2;
			// 
			// textRatio
			// 
			this.textRatio.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textRatio.Location = new System.Drawing.Point(9, 56);
			this.textRatio.Name = "textRatio";
			this.textRatio.Size = new System.Drawing.Size(548, 15);
			this.textRatio.TabIndex = 3;
			this.textRatio.Text = "Ratio";
			this.textRatio.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// textProgress
			// 
			this.textProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textProgress.Location = new System.Drawing.Point(9, 11);
			this.textProgress.Name = "textProgress";
			this.textProgress.Size = new System.Drawing.Size(548, 14);
			this.textProgress.TabIndex = 4;
			this.textProgress.Text = "Progress";
			this.textProgress.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(573, 240);
			this.Controls.Add(this.textProgress);
			this.Controls.Add(this.textRatio);
			this.Controls.Add(this.progressFile);
			this.Controls.Add(this.progressRatio);
			this.Controls.Add(this.log);
			this.Name = "Form1";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Min.Unity3D";
			this.TopMost = true;
			this.ResumeLayout(false);

		}

	#endregion
	public System.Windows.Forms.RichTextBox log;
	public System.Windows.Forms.ProgressBar progressRatio;
	public System.Windows.Forms.ProgressBar progressFile;
	public System.Windows.Forms.Label textRatio;
	public System.Windows.Forms.Label textProgress;
}


