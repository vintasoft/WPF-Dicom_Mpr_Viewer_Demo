using System;
using System.Windows;
using System.Windows.Controls;

using Vintasoft.Imaging;
using Vintasoft.Imaging.UI;
using Vintasoft.Primitives;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A window that allows to customize appearance of <see cref="WpfMprImageTool"/>.
    /// </summary>
    public partial class MprImageToolAppearanceSettingsWindow : Window
    {

        #region Fields

        /// <summary>
        /// The sagittal slice appearance settings.
        /// </summary>
        VisualMprSliceAppearanceSettings _sagittalSliceSettings;

        /// <summary>
        /// The coronal slice appearance settings.
        /// </summary>
        VisualMprSliceAppearanceSettings _coronalSliceSettings;

        /// <summary>
        /// The axial slice appearance settings.
        /// </summary>
        VisualMprSliceAppearanceSettings _axialSliceSettings;

        /// <summary>
        /// The curvilinear slice appearance settings.
        /// </summary>
        VisualMprSliceAppearanceSettings _curvilinearSliceSettings;

        /// <summary>
        /// The available slice types.
        /// </summary>
        SliceType[] _availableSliceTypes;

        /// <summary>
        /// The check box, which determines whether max FPS value is limited.
        /// </summary>
        CheckBox _limitMaxFpsCheckBox;

        #endregion



        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MprImageToolAppearanceSettingsWindow"/> class.
        /// </summary>
        /// <param name="manager">The MPR appearance manager.</param>
        public MprImageToolAppearanceSettingsWindow(MprImageToolAppearanceSettings manager)
            : this(manager, SliceType.Sagittal)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MprImageToolAppearanceSettingsWindow"/> class.
        /// </summary>
        /// <param name="manager">The MPR appearance manager.</param>
        /// <param name="selectedSliceType">The selected slice type.</param>
        public MprImageToolAppearanceSettingsWindow(
            MprImageToolAppearanceSettings manager,
            SliceType selectedSliceType)
            : this(manager, selectedSliceType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MprImageToolAppearanceSettingsWindow"/> class.
        /// </summary>
        /// <param name="manager">The MPR appearance manager.</param>
        /// <param name="selectedSliceType">The selected slice type.</param>
        /// <param name="availableSliceTypes">The available slice types.</param>
        public MprImageToolAppearanceSettingsWindow(
            MprImageToolAppearanceSettings manager,
            SliceType selectedSliceType,
            params SliceType[] availableSliceTypes)
        {
            InitializeComponent();

            _manager = manager;
            _availableSliceTypes = availableSliceTypes;

            if (!CanChangeSliceSettings(selectedSliceType))
                throw new InvalidOperationException();

            _limitMaxFpsCheckBox = (CheckBox)maxFpsValueEditorControl.ValueHeader;

            Init();

            sliceTypeComboBox.SelectedItem = selectedSliceType;

            UpdateUI();

            stepCountValueEditorControl.ValueChanged += new EventHandler(stepCountValueEditorControl_ValueChanged);
        }

        #endregion



        #region Properties

        MprImageToolAppearanceSettings _manager;
        /// <summary>
        /// The manager, which contains the settings.
        /// </summary>
        public MprImageToolAppearanceSettings Manager
        {
            get
            {
                return _manager;
            }
        }

        bool _canChangeColorMarkSettings = true;
        /// <summary> 
        /// Gets or sets a value indicating whether the color mark settings can be changed.
        /// </summary>
        /// <value>
        /// <b>true</b> if color mark settings can be changed; otherwise, <b>false</b>.
        /// </value>
        public bool CanChangeColorMarkSettings
        {
            get
            {
                return _canChangeColorMarkSettings;
            }
            set
            {
                if (_canChangeColorMarkSettings != value)
                {
                    _canChangeColorMarkSettings = value;

                    if (_canChangeColorMarkSettings)
                    {
                        colorMarkVisibleCheckBox.Visibility = Visibility.Visible;
                        colorMarkGroupBox.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        colorMarkVisibleCheckBox.Visibility = Visibility.Collapsed;
                        colorMarkGroupBox.Visibility = Visibility.Collapsed;
                    }

                    UpdateUI();
                }
            }
        }

        bool _canChangeSliceType = true;
        /// <summary>
        /// Gets or sets a value indicating whether the slice type can be changed.
        /// </summary>
        /// <value>
        /// <b>true</b> if the slice type can be changed; otherwise, <b>false</b>.
        /// </value>
        public bool CanChangeSliceType
        {
            get
            {
                return _canChangeSliceType;
            }
            set
            {
                if (_canChangeSliceType != value)
                {
                    _canChangeSliceType = value;

                    sliceTypeComboBox.IsEnabled = value;
                    UpdateUI();
                }
            }
        }

        #endregion



        #region Methods

        /// <summary>
        /// Updates the user interface.
        /// </summary>
        private void UpdateUI()
        {
            maxFpsValueEditorControl.IsContentEnabled = (bool)_limitMaxFpsCheckBox.IsChecked;

            if (stepCountValueEditorControl.Value == 0)
                complexityThresholdValueEditorControl.IsEnabled = false;
            else
                complexityThresholdValueEditorControl.IsEnabled = true;

            if (!CanChangeColorMarkSettings)
            {
                colorMarkGroupBox.IsEnabled = false;
            }
        }

        /// <summary>
        /// Initializes form controls.
        /// </summary>
        private void Init()
        {
            // init slices settings

            if (CanChangeSliceSettings(SliceType.Sagittal))
            {
                _sagittalSliceSettings = new VisualMprSliceAppearanceSettings();
                Manager.SagittalSliceAppearance.CopyTo(_sagittalSliceSettings);
            }

            if (CanChangeSliceSettings(SliceType.Coronal))
            {
                _coronalSliceSettings = new VisualMprSliceAppearanceSettings();
                Manager.CoronalSliceAppearance.CopyTo(_coronalSliceSettings);
            }

            if (CanChangeSliceSettings(SliceType.Axial))
            {
                _axialSliceSettings = new VisualMprSliceAppearanceSettings();
                Manager.AxialSliceAppearance.CopyTo(_axialSliceSettings);
            }

            if (CanChangeSliceSettings(SliceType.Curvilinear))
            {
                _curvilinearSliceSettings = new VisualMprSliceAppearanceSettings();
                Manager.CurvilinearSliceAppearance.CopyTo(_curvilinearSliceSettings);
            }

            if (_availableSliceTypes == null ||
                _availableSliceTypes.Length == 0)
            {
                // init slice combo box values
                sliceTypeComboBox.Items.Add(SliceType.Sagittal);
                sliceTypeComboBox.Items.Add(SliceType.Coronal);
                sliceTypeComboBox.Items.Add(SliceType.Axial);
                sliceTypeComboBox.Items.Add(SliceType.Curvilinear);
            }
            else
            {
                foreach (SliceType sliceType in _availableSliceTypes)
                    sliceTypeComboBox.Items.Add(sliceType);
            }

            // init visualization performance settings
            maxThreadValueEditorControl.MaxValue = 2 * Environment.ProcessorCount;
            maxThreadValueEditorControl.DefaultValue = Environment.ProcessorCount;
            maxThreadValueEditorControl.Value = _manager.RenderingThreadCount;
            if (_manager.MaxFps == 0)
            {
                _limitMaxFpsCheckBox.IsChecked = false;
                maxFpsValueEditorControl.IsContentEnabled = false;
            }
            else
            {
                _limitMaxFpsCheckBox.IsChecked = true;
                maxFpsValueEditorControl.IsContentEnabled = true;
                maxFpsValueEditorControl.Value = _manager.MaxFps;
            }

            stepCountValueEditorControl.Value = _manager.AdaptiveRenderingQualityStepCount;
            complexityThresholdValueEditorControl.Value = _manager.ComplexityThreshold;

            focusedImageViewerColorMarkPanelControl.Color = Manager.FocusedImageViewerColorMark;
            focusedImageViewerMarkSizeNumericUpDown.Value = Manager.FocusedImageViewerMarkSize;
            colorMarkVisibleCheckBox.IsChecked = Manager.IsColorMarkVisible;
            colorMarkWidthNumericUpDown.Value = Manager.ColorMarkSize.Width;
            colorMarkHeigthNumericUpDown.Value = Manager.ColorMarkSize.Height;
            colorMarkAnchorTypeEditor.SelectedAnchorType = Manager.ColorMarkAnchor;
        }

        /// <summary>
        /// Returns a value indicating whether the slice settings can be changed.
        /// </summary>
        /// <param name="sliceType">The slice type.</param>
        /// <returns>
        /// <b>True</b> if slice settings can be changed; otherwise, <b>false</b>.
        /// </returns>
        private bool CanChangeSliceSettings(SliceType sliceType)
        {
            if (_availableSliceTypes == null ||
                _availableSliceTypes.Length == 0)
                return true;

            if (Array.IndexOf(_availableSliceTypes, sliceType) == -1)
                return false;

            return true;
        }

        /// <summary>
        /// OK button is clicked.
        /// </summary>
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            // slice settings
            if (_sagittalSliceSettings != null)
                _sagittalSliceSettings.CopyTo(Manager.SagittalSliceAppearance);

            if (_coronalSliceSettings != null)
                _coronalSliceSettings.CopyTo(Manager.CoronalSliceAppearance);

            if (_axialSliceSettings != null)
                _axialSliceSettings.CopyTo(Manager.AxialSliceAppearance);

            if (_curvilinearSliceSettings != null)
                _curvilinearSliceSettings.CopyTo(Manager.CurvilinearSliceAppearance);

            if (CanChangeColorMarkSettings)
            {
                // color mark settings
                bool isColorMarkVisible = (bool)colorMarkVisibleCheckBox.IsChecked;
                Manager.IsColorMarkVisible = isColorMarkVisible;
                if (isColorMarkVisible)
                {
                    Manager.FocusedImageViewerColorMark = focusedImageViewerColorMarkPanelControl.Color;
                    Manager.FocusedImageViewerMarkSize = focusedImageViewerMarkSizeNumericUpDown.Value;
                    Manager.ColorMarkSize = new VintasoftSize(
                        (int)colorMarkWidthNumericUpDown.Value,
                        (int)colorMarkHeigthNumericUpDown.Value);
                    Manager.ColorMarkAnchor = colorMarkAnchorTypeEditor.SelectedAnchorType;
                }
            }

            // visualization performance settings
            Manager.FillDataThreadCount = (int)maxThreadValueEditorControl.Value;
            Manager.RenderingThreadCount = (int)maxThreadValueEditorControl.Value;

            if (_limitMaxFpsCheckBox.IsChecked.Value == false)
                Manager.MaxFps = 0;
            else
                Manager.MaxFps = (int)maxFpsValueEditorControl.Value;
            Manager.AdaptiveRenderingQualityStepCount = (int)stepCountValueEditorControl.Value;
            Manager.ComplexityThreshold = (int)complexityThresholdValueEditorControl.Value;

            DialogResult = true;
        }

        /// <summary>
        /// Cancel button is clicked.
        /// </summary>
        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// "Limit Max FPS" button value is changed.
        /// </summary>
        private void limitMaxFpsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            maxFpsValueEditorControl.IsContentEnabled = (bool)_limitMaxFpsCheckBox.IsChecked;
        }

        /// <summary>
        /// Adaptive rendering value is changed.
        /// </summary>
        private void stepCountValueEditorControl_ValueChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        /// <summary>
        /// Slice type is changed.
        /// </summary>
        private void sliceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sliceAppearanceEditorControl.ShowCurvilinearSliceSettings = false;

            switch ((SliceType)sliceTypeComboBox.SelectedItem)
            {
                case SliceType.Sagittal:
                    sliceAppearanceEditorControl.SliceSettings = _sagittalSliceSettings;
                    break;

                case SliceType.Coronal:
                    sliceAppearanceEditorControl.SliceSettings = _coronalSliceSettings;
                    break;

                case SliceType.Axial:
                    sliceAppearanceEditorControl.SliceSettings = _axialSliceSettings;
                    break;

                case SliceType.Curvilinear:
                    sliceAppearanceEditorControl.ShowCurvilinearSliceSettings = true;
                    sliceAppearanceEditorControl.SliceSettings = _curvilinearSliceSettings;
                    break;
            }
        }

        #endregion

    }
}
