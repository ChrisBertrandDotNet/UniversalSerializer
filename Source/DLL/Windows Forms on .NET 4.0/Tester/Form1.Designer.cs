namespace Tester
{
	partial class Form1
	{
		/// <summary>
		/// Variable nécessaire au concepteur.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Nettoyage des ressources utilisées.
		/// </summary>
		/// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Code généré par le Concepteur Windows Form

		/// <summary>
		/// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
		/// le contenu de cette méthode avec l'éditeur de code.
		/// </summary>
		private void InitializeComponent()
		{
			this.panel1 = new System.Windows.Forms.Panel();
			this.NumberOfFailures = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.dataGridViewResults = new System.Windows.Forms.DataGridView();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewResults)).BeginInit();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel1.Controls.Add(this.NumberOfFailures);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(703, 20);
			this.panel1.TabIndex = 0;
			// 
			// NumberOfFailures
			// 
			this.NumberOfFailures.AutoSize = true;
			this.NumberOfFailures.BackColor = System.Drawing.SystemColors.Highlight;
			this.NumberOfFailures.Dock = System.Windows.Forms.DockStyle.Left;
			this.NumberOfFailures.ForeColor = System.Drawing.SystemColors.HighlightText;
			this.NumberOfFailures.Location = new System.Drawing.Point(95, 0);
			this.NumberOfFailures.Name = "NumberOfFailures";
			this.NumberOfFailures.Size = new System.Drawing.Size(46, 13);
			this.NumberOfFailures.TabIndex = 1;
			this.NumberOfFailures.Text = "waiting..";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Dock = System.Windows.Forms.DockStyle.Left;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(95, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Number of failures:";
			// 
			// dataGridViewResults
			// 
			this.dataGridViewResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridViewResults.Location = new System.Drawing.Point(0, 20);
			this.dataGridViewResults.Name = "dataGridViewResults";
			this.dataGridViewResults.Size = new System.Drawing.Size(703, 492);
			this.dataGridViewResults.TabIndex = 1;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(703, 512);
			this.Controls.Add(this.dataGridViewResults);
			this.Controls.Add(this.panel1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewResults)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label NumberOfFailures;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.DataGridView dataGridViewResults;
	}
}

