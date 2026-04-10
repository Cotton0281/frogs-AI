
namespace AI_Evlo_Test
{
    partial class PopulationList
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
            this.button1 = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.iDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cyclesDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ofspringsDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.smartObjectBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.geneEvaluationBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.fitnessDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.generationDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Ofsprings = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.smartObjectBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.geneEvaluationBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(11, 10);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Refresh";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 3000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.id,
            this.fitnessDataGridViewTextBoxColumn,
            this.generationDataGridViewTextBoxColumn,
            this.Ofsprings});
            this.dataGridView1.DataSource = this.geneEvaluationBindingSource;
            this.dataGridView1.Location = new System.Drawing.Point(-2, 42);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersWidth = 62;
            this.dataGridView1.RowTemplate.Height = 28;
            this.dataGridView1.Size = new System.Drawing.Size(500, 407);
            this.dataGridView1.TabIndex = 2;
            // 
            // dataGridView2
            // 
            this.dataGridView2.AllowUserToAddRows = false;
            this.dataGridView2.AllowUserToDeleteRows = false;
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView2.AutoGenerateColumns = false;
            this.dataGridView2.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.iDDataGridViewTextBoxColumn,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn1,
            this.HP,
            this.cyclesDataGridViewTextBoxColumn,
            this.ofspringsDataGridViewTextBoxColumn});
            this.dataGridView2.DataSource = this.smartObjectBindingSource;
            this.dataGridView2.Location = new System.Drawing.Point(510, 42);
            this.dataGridView2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.ReadOnly = true;
            this.dataGridView2.RowHeadersWidth = 62;
            this.dataGridView2.RowTemplate.Height = 28;
            this.dataGridView2.Size = new System.Drawing.Size(500, 407);
            this.dataGridView2.TabIndex = 3;
            this.dataGridView2.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dataGridView2_DataError);
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "Generation";
            this.dataGridViewTextBoxColumn2.HeaderText = "Generation";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Fitness";
            this.dataGridViewTextBoxColumn1.HeaderText = "Fitness";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 8;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // HP
            // 
            this.HP.DataPropertyName = "HP";
            this.HP.HeaderText = "HP";
            this.HP.MinimumWidth = 8;
            this.HP.Name = "HP";
            this.HP.ReadOnly = true;
            // 
            // iDDataGridViewTextBoxColumn
            // 
            this.iDDataGridViewTextBoxColumn.DataPropertyName = "ID";
            this.iDDataGridViewTextBoxColumn.HeaderText = "ID";
            this.iDDataGridViewTextBoxColumn.MinimumWidth = 6;
            this.iDDataGridViewTextBoxColumn.Name = "iDDataGridViewTextBoxColumn";
            this.iDDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // cyclesDataGridViewTextBoxColumn
            // 
            this.cyclesDataGridViewTextBoxColumn.DataPropertyName = "Cycles";
            this.cyclesDataGridViewTextBoxColumn.HeaderText = "Cycles";
            this.cyclesDataGridViewTextBoxColumn.MinimumWidth = 6;
            this.cyclesDataGridViewTextBoxColumn.Name = "cyclesDataGridViewTextBoxColumn";
            this.cyclesDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // ofspringsDataGridViewTextBoxColumn
            // 
            this.ofspringsDataGridViewTextBoxColumn.DataPropertyName = "Ofsprings";
            this.ofspringsDataGridViewTextBoxColumn.HeaderText = "Ofsprings";
            this.ofspringsDataGridViewTextBoxColumn.MinimumWidth = 6;
            this.ofspringsDataGridViewTextBoxColumn.Name = "ofspringsDataGridViewTextBoxColumn";
            this.ofspringsDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // smartObjectBindingSource
            // 
            this.smartObjectBindingSource.DataSource = typeof(AI_Evlo_Test.Objects.SmartObject);
            // 
            // geneEvaluationBindingSource
            // 
            this.geneEvaluationBindingSource.DataSource = typeof(AI_Evlo_Test.Objects.GenomeRecord);
            // 
            // id
            // 
            this.id.DataPropertyName = "ID";
            this.id.HeaderText = "ID";
            this.id.MinimumWidth = 6;
            this.id.Name = "id";
            this.id.ReadOnly = true;
            // 
            // fitnessDataGridViewTextBoxColumn
            // 
            this.fitnessDataGridViewTextBoxColumn.DataPropertyName = "Fitness";
            this.fitnessDataGridViewTextBoxColumn.HeaderText = "Fitness";
            this.fitnessDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.fitnessDataGridViewTextBoxColumn.Name = "fitnessDataGridViewTextBoxColumn";
            this.fitnessDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // generationDataGridViewTextBoxColumn
            // 
            this.generationDataGridViewTextBoxColumn.DataPropertyName = "Generation";
            this.generationDataGridViewTextBoxColumn.HeaderText = "Generation";
            this.generationDataGridViewTextBoxColumn.MinimumWidth = 8;
            this.generationDataGridViewTextBoxColumn.Name = "generationDataGridViewTextBoxColumn";
            this.generationDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // Ofsprings
            // 
            this.Ofsprings.DataPropertyName = "Ofsprings";
            this.Ofsprings.HeaderText = "Ofsprings";
            this.Ofsprings.MinimumWidth = 6;
            this.Ofsprings.Name = "Ofsprings";
            this.Ofsprings.ReadOnly = true;
            // 
            // PopulationList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1021, 450);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button1);
            this.Name = "PopulationList";
            this.Text = "PopulationList";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.smartObjectBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.geneEvaluationBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.BindingSource geneEvaluationBindingSource;
        private System.Windows.Forms.BindingSource smartObjectBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn agentIdDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn iDDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn HP;
        private System.Windows.Forms.DataGridViewTextBoxColumn cyclesDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ofspringsDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn id;
        private System.Windows.Forms.DataGridViewTextBoxColumn fitnessDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn generationDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn Ofsprings;
    }
}