﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualOdometry;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace VisualOdometry.UI
{
	public partial class MapForm : Form
	{
		private RobotPath m_RobotPath = new RobotPath();
		private Bitmap m_Bitmap;
		private Graphics m_Graphics;
		//private Matrix m_InverseTransform; // world coordinates to pixel coordinates

		private Pen m_RobotPen = new Pen(Color.Blue);
		private Pen m_PathPen = new Pen(Color.Black);

		private int m_CircleRadius = 6;

		public MapForm(RobotPath robotPath)
		{
			InitializeComponent();
			m_RobotPath = robotPath;

			InitializeMapImage();
		}

		private void InitializeMapImage()
		{
			InitializeBitmap();
			m_PictureBox.Image = m_Bitmap;
			DrawFullPath();
		}

		private void InitializeBitmap()
		{
			m_Bitmap = new Bitmap(m_PictureBox.Width, m_PictureBox.Height);
			m_Graphics = Graphics.FromImage(m_Bitmap);

			Matrix matrix = new Matrix();
			float zoomFactor = 1.0f;
			matrix.Scale(zoomFactor, -zoomFactor);
			matrix.Translate(m_PictureBox.Width / 2, -m_PictureBox.Height / 2);

			m_Graphics.Transform = matrix;

			//m_InverseTransform = matrix.Clone();
			//m_InverseTransform.Invert();
		}

		private void DrawFullPath()
		{
			Debug.WriteLine("Drawing full path");
			m_Graphics.Clear(Color.White);
			for (int i = 0; i < m_RobotPath.Poses.Count; i++)
			{
				DrawPose(i);
			}
		}

		private void DrawPose(int index)
		{
			if (m_AutoScaleCheckBox.Checked)
			{
				PointF lowerLeftBound = new PointF((float)m_RobotPath.MinX, (float)m_RobotPath.MinY);
				PointF upperRightBound = new PointF((float)m_RobotPath.MaxX, (float)m_RobotPath.MaxY);
				
				PointF[] points = new PointF[] { lowerLeftBound, upperRightBound };

				m_Graphics.Transform.TransformPoints(points);

				PointF lowerLeftPixel = points[0];
				PointF upperRightPixel = points[1];
				if (lowerLeftPixel.X < 0 || lowerLeftPixel.Y > m_Bitmap.Height || upperRightPixel.X > m_Bitmap.Width || upperRightPixel.Y < 0)
				{
					ZoomOut();
					return;
				}
			}

			Pose pose = m_RobotPath.Poses[index];
			if (index % 5 == 0)
			{
				// Draw robot
				m_Graphics.DrawEllipse(
					m_RobotPen,
					RoundToInt(pose.X - m_CircleRadius),
					RoundToInt(pose.Y - m_CircleRadius),
					RoundToInt(2 * m_CircleRadius),
					RoundToInt(2 * m_CircleRadius));



				m_Graphics.DrawLine(
					m_RobotPen,
					RoundToInt(pose.X),
					RoundToInt(pose.Y),
					RoundToInt(pose.X - 2 * m_CircleRadius * Math.Sin(pose.Heading.Rads)),
					RoundToInt(pose.Y + 2 * m_CircleRadius * Math.Cos(pose.Heading.Rads)));
			}

			if (index > 0)
			{
				// draw line from last location
				Pose previousPose = m_RobotPath.Poses[index - 1];
				m_Graphics.DrawLine(
					m_PathPen,
					RoundToInt(previousPose.X),
					RoundToInt(previousPose.Y),
					RoundToInt(pose.X),
					RoundToInt(pose.Y));
			}
		}

		private int RoundToInt(double value)
		{
			return (int)(value + 0.5);
		}

		private void ZoomIn()
		{
			Zoom(1.25f);
		}

		private void ZoomOut()
		{
			Zoom(0.75f);
		}

		private void Zoom(float zoomFactor)
		{
			Matrix matrix = m_Graphics.Transform;
			matrix.Scale(zoomFactor, zoomFactor);
			m_Graphics.Transform = matrix;
			DrawFullPath();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			InitializeMapImage();
		}

		bool m_Dragging = false;
		Point m_LastPosition;

		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				m_Dragging = true;
				m_LastPosition = e.Location;
				Debug.WriteLine("Dragging start");
			}
		}

		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			m_Dragging = false;
			Debug.WriteLine("Dragging end");
		}

		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (m_Dragging)
			{
				Point currentLocation = e.Location;

				float deltaX = (float)(currentLocation.X - m_LastPosition.X);
				float deltaY = (float)(currentLocation.Y - m_LastPosition.Y);

				Matrix matrix = m_Graphics.Transform;
				float zoomFactor = matrix.Elements[0];
				matrix.Translate(deltaX / zoomFactor, -deltaY / zoomFactor);
				m_Graphics.Transform = matrix;
				
				DrawFullPath();
				Refresh();

				m_LastPosition = e.Location;
			}
		}

		private void OnZoomOutButtonClicked(object sender, EventArgs e)
		{
			m_AutoScaleCheckBox.Checked = false;
			ZoomOut();
			Refresh();
		}

		internal void UpdateMap()
		{
			DrawPose(m_RobotPath.Poses.Count - 1);
			Refresh();
		}

		private void OnZoomInButtonClicked(object sender, EventArgs e)
		{
			m_AutoScaleCheckBox.Checked = false;
			ZoomIn();
			Refresh();
		}
	}
}
