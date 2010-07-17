﻿namespace VisualOdometry.UI
{
	partial class DetailsForm
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
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
			this.m_AnglesChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
			((System.ComponentModel.ISupportInitialize)(this.m_AnglesChart)).BeginInit();
			this.SuspendLayout();
			// 
			// m_AnglesChart
			// 
			chartArea1.Name = "ChartArea1";
			this.m_AnglesChart.ChartAreas.Add(chartArea1);
			this.m_AnglesChart.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_AnglesChart.Location = new System.Drawing.Point(0, 0);
			this.m_AnglesChart.Name = "m_AnglesChart";
			series1.ChartArea = "ChartArea1";
			series1.Name = "Series1";
			this.m_AnglesChart.Series.Add(series1);
			this.m_AnglesChart.Size = new System.Drawing.Size(566, 483);
			this.m_AnglesChart.TabIndex = 0;
			this.m_AnglesChart.Text = "Angles";
			// 
			// DetailsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(566, 483);
			this.Controls.Add(this.m_AnglesChart);
			this.Name = "DetailsForm";
			this.Text = "DetailsForm";
			((System.ComponentModel.ISupportInitialize)(this.m_AnglesChart)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataVisualization.Charting.Chart m_AnglesChart;
	}
}