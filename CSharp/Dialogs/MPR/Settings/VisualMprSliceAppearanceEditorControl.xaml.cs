﻿using System;
using System.Windows;
using System.Windows.Controls;

using Vintasoft.Imaging.Dicom.Mpr;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A control that allows to change the slice appearance settings.
    /// </summary>
    public partial class VisualMprSliceAppearanceEditorControl : UserControl
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualMprSliceAppearanceEditorControl"/> class.
        /// </summary>
        public VisualMprSliceAppearanceEditorControl()
        {
            InitializeComponent();

            Init();
        }

        #endregion



        #region Properties

        VisualMprSliceAppearanceSettings _sliceSettings;
        /// <summary>
        /// Gets or sets the slice settings.
        /// </summary>
        public VisualMprSliceAppearanceSettings SliceSettings
        {
            get
            {
                return _sliceSettings;
            }
            set
            {
                _sliceSettings = value;
                UpdateUI();
            }
        }

        bool _showCurvilinearSliceSettings = false;
        /// <summary>
        /// Gets or set a value indicating whether the control should show settings for curvilinear slice.
        /// </summary>
        /// <value>
        /// <b>True</b> - the control should show settings for curvilinear slice;
        /// <b>false</b> - the control should NOT show settings for curvilinear slice.
        /// </value>
        public bool ShowCurvilinearSliceSettings
        {
            get
            {
                return _showCurvilinearSliceSettings;
            }
            set
            {
                _showCurvilinearSliceSettings = value;
                UpdateUI();
            }
        }

        #endregion



        #region Mehtods

        /// <summary>
        /// Initializes the control.
        /// </summary>
        private void Init()
        {
            // init rendering mode combo box values
            foreach (MprSliceRenderingMode renderingMode in Enum.GetValues(typeof(MprSliceRenderingMode)))
                renderingModeComboBox.Items.Add(renderingMode);
            renderingModeComboBox.SelectedIndex = 0;

            UpdateUI();
        }

        /// <summary>
        /// Updates the user interface.
        /// </summary>
        private void UpdateUI()
        {
            if (SliceSettings != null)
            {
                sliceColorPanelControl.Color = SliceSettings.SliceColor;
                sliceLineWidthNumericUpDown.Value = (double)SliceSettings.SliceLineWidth;
                markerPointDiameterNumericUpDown.Value = (double)SliceSettings.MarkerPointDiameter;
                thicknessNumericUpDown.Value = (double)SliceSettings.Thickness;
                renderingModeComboBox.SelectedItem = SliceSettings.RederingMode;
                curveTensionNumericUpDown.Value = SliceSettings.CurveTension;
            }

            if ((MprSliceRenderingMode)renderingModeComboBox.SelectedItem == MprSliceRenderingMode.MPR)
            {
                thicknessNumericUpDown.IsEnabled = false;
            }
            else
            {
                thicknessNumericUpDown.IsEnabled = true;
            }

            if (ShowCurvilinearSliceSettings)
            {
                curveTensionLabel.Visibility = Visibility.Visible;
                curveTensionNumericUpDown.Visibility = Visibility.Visible;
            }
            else
            {
                curveTensionNumericUpDown.Visibility = Visibility.Hidden;
                curveTensionLabel.Visibility = Visibility.Hidden;
            }

        }

        /// <summary>
        /// Slice color is changed.
        /// </summary>
        private void sliceColorPanelControl_ColorChanged(object sender, EventArgs e)
        {
            if (SliceSettings != null)
                SliceSettings.SliceColor = sliceColorPanelControl.Color;
        }

        /// <summary>
        /// Slice line width is changed.
        /// </summary>
        private void sliceLineWidthNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (SliceSettings != null)
                SliceSettings.SliceLineWidth = (float)sliceLineWidthNumericUpDown.Value;
        }

        /// <summary>
        /// Marker point diameter is changed.
        /// </summary>
        private void markerPointDiameterNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (SliceSettings != null)
                SliceSettings.MarkerPointDiameter = (float)markerPointDiameterNumericUpDown.Value;
        }

        /// <summary>
        /// Slice thickness is changed.
        /// </summary>
        private void thicknessNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (SliceSettings != null)
                SliceSettings.Thickness = (float)thicknessNumericUpDown.Value;
        }

        /// <summary>
        /// MPR image slice rendering mode is changed.
        /// </summary>
        private void renderingModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SliceSettings != null)
            {
                SliceSettings.RederingMode = (MprSliceRenderingMode)renderingModeComboBox.SelectedItem;

                if (SliceSettings.RederingMode == MprSliceRenderingMode.MPR)
                    thicknessNumericUpDown.IsEnabled = false;
                else
                    thicknessNumericUpDown.IsEnabled = true;
            }
        }

        /// <summary>
        /// Curve tension is changed.
        /// </summary>
        private void curveTensionNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (SliceSettings != null)
            {
                SliceSettings.CurveTension = curveTensionNumericUpDown.Value;
            }
        }

        #endregion

    }
}
