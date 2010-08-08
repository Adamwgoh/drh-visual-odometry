﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Diagnostics;

namespace VisualOdometry
{
	public class VisualOdometer : IDisposable
	{
		internal static readonly double RadToDegree = 180.0 / Math.PI;

		private Capture m_Capture;
		private CameraParameters m_CameraParameters;

		private Matrix<float> m_UndistortMapX;
		private Matrix<float> m_UndistortMapY;

		private int m_GroundRegionTop;
		private int m_SkyRegionBottom;

		private List<TrackedFeature> m_TrackedFeatures;
		private int m_NotTrackedFeaturesCount;

		private Image<Bgr, Byte> m_RawImage;
		public Image<Bgr, Byte> CurrentImage { get; private set; }
		private Image<Gray, Byte> m_CurrentGrayImage;
		private OpticalFlow m_OpticalFlow;
		private RotationAnalyzer m_RotationAnalyzer;
		private TranslationAnalyzer m_TranslationAnalyzer;
		public int InitialFeaturesCount { get; private set; }
		private int m_ThresholdForFeatureRepopulation;
		private Pose m_RobotPose = new Pose();
		private int m_FrameNumber = 0;

		public event EventHandler Changed;

		public VisualOdometer(Capture capture, CameraParameters cameraParameters, HomographyMatrix birdsEyeViewTransformation, OpticalFlow opticalFlow)
		{
			m_Capture = capture;
			m_CameraParameters = cameraParameters;

			this.GroundRegionTop = OdometerSettings.Default.GroundRegionTop;
			this.SkyRegionBottom = OdometerSettings.Default.SkyRegionBottom;

			this.OpticalFlow = opticalFlow;
			m_RotationAnalyzer = new RotationAnalyzer(this);
			m_TranslationAnalyzer = new TranslationAnalyzer(this, birdsEyeViewTransformation);
		}

		public CameraParameters CameraParameters
		{
			get { return m_CameraParameters; }
			//set
			//{
			//    m_CameraParameters = value;
			//    if (m_UndistortMapX != null)
			//    {
			//        m_UndistortMapX.Dispose();
			//        m_UndistortMapX = null;
			//    }
			//    if (m_UndistortMapY != null)
			//    {
			//        m_UndistortMapY.Dispose();
			//        m_UndistortMapY = null;
			//    }
			//}
		}

		public OpticalFlow OpticalFlow
		{
			get { return m_OpticalFlow; }
			set
			{
				m_OpticalFlow = value;
				m_TrackedFeatures = new List<TrackedFeature>();
				m_ThresholdForFeatureRepopulation = int.MaxValue;
			}
		}

		public RotationAnalyzer RotationAnalyzer
		{
			get { return m_RotationAnalyzer; }
		}

		public TranslationAnalyzer TranslationAnalyzer
		{
			get { return m_TranslationAnalyzer; }
		}

		/// <summary>
		/// Top of the ground region in screen coordinates.
		/// </summary>
		public int GroundRegionTop
		{
			get { return m_GroundRegionTop; }
			set
			{
				if (value != m_GroundRegionTop)
				{
					// We ensure that the ground always ends up below the sky
					if (value <= m_SkyRegionBottom)
					{
						value = m_SkyRegionBottom + 1;
					}
					m_GroundRegionTop = value;
					RaiseChangedEvent();
				}
			}
		}

		/// <summary>
		/// Botton of the sky region in screen coordinates.
		/// </summary>
		public int SkyRegionBottom
		{
			get { return m_SkyRegionBottom; }
			set
			{
				if (value != m_SkyRegionBottom)
				{
					// We ensure that the sky always ends up above the ground
					if (value >= m_GroundRegionTop)
					{
						value = m_GroundRegionTop - 1;
					}
					m_SkyRegionBottom = value;
					RaiseChangedEvent();
				}
			}
		}

		public void ProcessFrame()
		{
			//m_RawImage = m_Capture.QueryFrame();
			for (int i = 0; i < 1; i++)
			{
				m_RawImage = m_Capture.QueryFrame();
			}
			if (m_RawImage == null)
			{
				// This occurs if we operate against a previously recorded video and the video has ended.
				return;
			}

			m_FrameNumber++;
			if (m_UndistortMapX == null)
			{
				InitializeUndistortMap(m_RawImage);
			}

			Image<Gray, Byte> previousGrayImage = m_CurrentGrayImage;

			if (previousGrayImage == null)
			{
				this.CurrentImage = m_RawImage.Clone();
			}

			Undistort(m_RawImage, this.CurrentImage);
			m_CurrentGrayImage = this.CurrentImage.Convert<Gray, Byte>();

			if (previousGrayImage == null)
			{
				return;
			}

			TrackFeatures(previousGrayImage);

			m_RotationAnalyzer.CalculateRotation();
			m_TranslationAnalyzer.CalculateTranslation(m_RotationAnalyzer.HeadingChange);
			UpdateRobotPose();

			if (m_TrackedFeatures.Count < m_ThresholdForFeatureRepopulation)
			{
				RepopulateFeaturePoints();
			}
		}

		private void InitializeUndistortMap(Image<Bgr, Byte> image)
		{
			m_CameraParameters.IntrinsicCameraParameters.InitUndistortMap(
				image.Width,
				image.Height,
				out m_UndistortMapX,
				out m_UndistortMapY);
		}

		private void Undistort(Image<Bgr, Byte> sourceImage, Image<Bgr, Byte> targetImage)
		{
			CvInvoke.cvRemap(sourceImage.Ptr, targetImage.Ptr, m_UndistortMapX.Ptr, m_UndistortMapY.Ptr, (int)Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, new MCvScalar());
		}

		private void RepopulateFeaturePoints()
		{
			System.Drawing.PointF[] newTrackedFeaturePoints = this.OpticalFlow.FindFeaturesToTrack(
				m_CurrentGrayImage,
				m_TrackedFeatures,
				m_SkyRegionBottom,
				m_GroundRegionTop);

			for (int i = 0; i < newTrackedFeaturePoints.Length; i++)
			{
				TrackedFeature trackedFeature = new TrackedFeature();
				trackedFeature.Add(newTrackedFeaturePoints[i]);
				m_TrackedFeatures.Add(trackedFeature);
			}

			this.InitialFeaturesCount = m_TrackedFeatures.Count;
			m_ThresholdForFeatureRepopulation = this.InitialFeaturesCount * 9 / 10;
			// We ensure that we don't drop below a fixed limit
			if (m_ThresholdForFeatureRepopulation < 100)
			{
				m_ThresholdForFeatureRepopulation = 100;
			}

			m_NotTrackedFeaturesCount = 0;
		}

		private void TrackFeatures(Image<Gray, Byte> previousGrayImage)
		{
			System.Drawing.PointF[] trackedFeaturePoints = new System.Drawing.PointF[m_TrackedFeatures.Count];
			for (int i = 0; i < trackedFeaturePoints.Length; i++)
			{
				trackedFeaturePoints[i] = m_TrackedFeatures[i][0];
			}
			OpticalFlowResult opticalFlowResult = this.OpticalFlow.CalculateOpticalFlow(previousGrayImage, m_CurrentGrayImage, trackedFeaturePoints);
			trackedFeaturePoints = opticalFlowResult.TrackedFeaturePoints;

			int fullHistoryFeaturesCount = 0;
			int unsmoothFeaturesCount = 0;
			for (int i = trackedFeaturePoints.Length - 1; i >= 0; i--)
			{
				bool isTracked = opticalFlowResult.TrackingStatusIndicators[i] == 1;
				if (isTracked)
				{
					TrackedFeature trackedFeature = m_TrackedFeatures[i];
					trackedFeature.Add(trackedFeaturePoints[i]);

					if (trackedFeature.HasFullHistory)
					{
						fullHistoryFeaturesCount++;
						if (!trackedFeature.IsSmooth)
						{
							unsmoothFeaturesCount++;
						}
					}
				}
				else
				{
					RemoveTrackedFeature(i);
				}
			}

			if (unsmoothFeaturesCount < fullHistoryFeaturesCount / 2)
			{
				// The majority of features is smooth. We downgrade unsmooth features
				//Debug.WriteLine("Consensus: Is smooth");
				ApplyUnsmoothGrades();
			}
			else
			{
				// Consensus not smooth; not downgrading unsmooth features.
				//Debug.WriteLine("Consensus: Is not smooth");
			}
		}

		private void RemoveTrackedFeature(int index)
		{
			m_NotTrackedFeaturesCount++;
			m_TrackedFeatures.RemoveAt(index);
		}

		private void ApplyUnsmoothGrades()
		{
			int unsmoothFeaturesOutCount = 0;
			for (int i = m_TrackedFeatures.Count - 1; i >= 0; i--)
			{
				TrackedFeature trackedFeature = m_TrackedFeatures[i];
				trackedFeature.ApplyScoreChange();
				if (trackedFeature.IsOut)
				{
					RemoveTrackedFeature(i);
					unsmoothFeaturesOutCount++;
				}
			}

			//Debug.WriteLine("Number of unsmooth features weeded out: " + unsmoothFeaturesOutCount);
		}

		private void UpdateRobotPose()
		{
			double headingChangeRad = m_RotationAnalyzer.HeadingChange.Rads;

			m_RobotPose.Heading = m_RobotPose.Heading + m_RotationAnalyzer.HeadingChange / 2;

			double cosHeading = Math.Cos(m_RobotPose.Heading.Rads);
			double sinHeading = Math.Sin(m_RobotPose.Heading.Rads);

			System.Windows.Point locationChangeRobot = m_TranslationAnalyzer.LocationChange;
			// The x coordinate of the location change in the robot's coordinate system is perpendicular to the robot's heading
			// in the global coordinate system; the y coordinate is parallel to the robot's heading.

			//Debug.WriteLine("dx_r: {0:0.000}  dy_r: {1:0.000}", locationChangeRobot.X, locationChangeRobot.Y);

			double deltaXGlobal = locationChangeRobot.X * cosHeading - locationChangeRobot.Y * sinHeading;
			double deltaYGlobal = locationChangeRobot.X * sinHeading + locationChangeRobot.Y * cosHeading;

			m_RobotPose.Location = new System.Windows.Point(
				m_RobotPose.Location.X + deltaXGlobal,
				m_RobotPose.Location.Y + deltaYGlobal);

			m_RobotPose.Heading = m_RobotPose.Heading + m_RotationAnalyzer.HeadingChange / 2;

			RaiseNewPoseEvent();
		}

		private void RaiseNewPoseEvent()
		{
			var handler = NewPose;
			if (handler != null)
			{
				Pose newPose = new Pose(m_RobotPose.Location, m_RobotPose.Heading);
				var e = new PoseEventArgs(newPose);
				handler(this, e);
			}
		}

		public int FrameNumber
		{
			get { return m_FrameNumber; }
		}

		public event EventHandler<PoseEventArgs> NewPose;

		public List<TrackedFeature> TrackedFeatures
		{
			get { return m_TrackedFeatures; }
		}

		public int NotTrackedFeaturesCount
		{
			get { return m_NotTrackedFeaturesCount; }
		}

		public Pose RobotPose
		{
			get { return m_RobotPose; }
		}

		private void RaiseChangedEvent()
		{
			EventHandler handler = this.Changed;
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		public void Dispose()
		{
			OdometerSettings.Default.GroundRegionTop = m_GroundRegionTop;
			OdometerSettings.Default.SkyRegionBottom = m_SkyRegionBottom;
			OdometerSettings.Default.Save();

			if (this.OpticalFlow != null)
			{
				this.OpticalFlow.Dispose();
			}
		}
	}
}
