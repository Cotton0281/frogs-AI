namespace AI_Evlo_Test
{
    partial class VisualizeNetwork
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
            this.components = new System.ComponentModel.Container();
            this.networkView = new AI_Evlo_Test.NeuralNetworkView();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusText = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnRefreshNNVisual = new System.Windows.Forms.Button();
            this.btnMutate = new System.Windows.Forms.Button();
            this.chkAutoRefresh = new System.Windows.Forms.CheckBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // networkView
            // 
            this.networkView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.networkView.BackColor = System.Drawing.Color.White;
            this.networkView.Location = new System.Drawing.Point(0, 38);
            this.networkView.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.networkView.Name = "networkView";
            this.networkView.Network = null;
            this.networkView.Size = new System.Drawing.Size(1221, 669);
            this.networkView.TabIndex = 0;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusText});
            this.statusStrip1.Location = new System.Drawing.Point(0, 716);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 16, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1221, 30);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusText
            // 
            this.toolStripStatusText.Name = "toolStripStatusText";
            this.toolStripStatusText.Size = new System.Drawing.Size(76, 25);
            this.toolStripStatusText.Text = "Loading";
            // 
            // btnRefreshNNVisual
            // 
            this.btnRefreshNNVisual.Location = new System.Drawing.Point(16, 0);
            this.btnRefreshNNVisual.Name = "btnRefreshNNVisual";
            this.btnRefreshNNVisual.Size = new System.Drawing.Size(90, 37);
            this.btnRefreshNNVisual.TabIndex = 2;
            this.btnRefreshNNVisual.Text = "Refresh";
            this.btnRefreshNNVisual.UseVisualStyleBackColor = true;
            this.btnRefreshNNVisual.Click += new System.EventHandler(this.btnRefreshNNVisual_Click);
            // 
            // btnMutate
            // 
            this.btnMutate.Location = new System.Drawing.Point(114, 2);
            this.btnMutate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnMutate.Name = "btnMutate";
            this.btnMutate.Size = new System.Drawing.Size(94, 35);
            this.btnMutate.TabIndex = 3;
            this.btnMutate.Text = "Mutate";
            this.btnMutate.UseVisualStyleBackColor = true;
            this.btnMutate.Click += new System.EventHandler(this.BtnMutate_Click);
            // 
            // chkAutoRefresh
            // 
            this.chkAutoRefresh.AutoSize = true;
            this.chkAutoRefresh.Location = new System.Drawing.Point(231, 5);
            this.chkAutoRefresh.Name = "chkAutoRefresh";
            this.chkAutoRefresh.Size = new System.Drawing.Size(130, 24);
            this.chkAutoRefresh.TabIndex = 4;
            this.chkAutoRefresh.Text = "Auto Refresh";
            this.chkAutoRefresh.UseVisualStyleBackColor = true;
            this.chkAutoRefresh.CheckedChanged += new System.EventHandler(this.ChkAutoRefresh_CheckedChanged);
            // 
            // timer1
            // 
            this.timer1.Interval = 300;
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);
            // 
            // VisualizeNetwork
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1221, 746);
            this.Controls.Add(this.chkAutoRefresh);
            this.Controls.Add(this.btnMutate);
            this.Controls.Add(this.btnRefreshNNVisual);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.networkView);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "VisualizeNetwork";
            this.Text = "VisualizeNetwork";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AI_Evlo_Test.NeuralNetworkView networkView;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusText;
        private System.Windows.Forms.Button btnRefreshNNVisual;
        private System.Windows.Forms.Button btnMutate;
        private System.Windows.Forms.CheckBox chkAutoRefresh;
        private System.Windows.Forms.Timer timer1;
    }
}
