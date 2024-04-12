using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Codecs;
using Vintasoft.Imaging.Codecs.ImageFiles.Dicom;
using Vintasoft.Imaging.Dicom.Mpr;
using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI;
using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI.VisualTools;
using Vintasoft.Imaging.Dicom.Wpf.UI.VisualTools;
using Vintasoft.Imaging.ImageProcessing;
using Vintasoft.Imaging.Wpf;
using Vintasoft.Imaging.Wpf.UI;
using Vintasoft.Imaging.Wpf.UI.VisualTools;

using WpfDemosCommonCode;
using WpfDemosCommonCode.Imaging;
using WpfDemosCommonCode.Imaging.Codecs;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// Main window of DICOM MPR viewer demo.
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Constants

        /// <summary>
        /// The text overlay collection owner name.
        /// </summary>
        const string OVERLAY_OWNER_NAME = "Dicom Mpr Viewer";

        #endregion



        #region Fields

        /// <summary>
        /// Template of the application title.
        /// </summary>
        string _titlePrefix = "VintaSoft WPF DICOM MPR Viewer Demo v" + ImagingGlobalSettings.ProductVersion + " - {0}";

        /// <summary>
        /// Controller of files in current DICOM series.
        /// </summary>
        DicomSeriesController _dicomSeriesController = new DicomSeriesController();

        /// <summary>
        /// Controller of source data in MPR image.
        /// </summary>
        WpfMprSourceDataController _mprSourceDataController = new WpfMprSourceDataController();

        /// <summary>
        /// The MPR settings manager.
        /// </summary>
        MprImageToolAppearanceSettings _mprSettingsManager = new MprImageToolAppearanceSettings();

        /// <summary>
        /// The visualization controller that manages the visualization of MPR image in image viewers.
        /// </summary>
        WpfMprVisualizationController _visualizationController;

        /// <summary>
        /// The visualized slices
        /// (the first item - sagittal slice, the second item - coronal slice, the third item - axial slice).
        /// </summary>
        MprSlice[] _slices;

        /// <summary>
        /// The default slices.
        /// </summary>
        MprSlice[] _defaultSlices;

        /// <summary>
        /// The <see cref="DicomMprTool"/> of viewers.
        /// </summary>
        WpfDicomMprTool[] _dicomMprTools;

        /// <summary>
        /// A value indicating whether DICOM image VOI LUT is changing.
        /// </summary>
        bool _isImageVoiLutChanging = false;

        /// <summary>
        /// The previous window state value.
        /// </summary>
        WindowState _previousWindowState;

        /// <summary>
        /// The window with MPR parameters information.
        /// </summary>
        MprParametersViewerWindow _mprParametersWindow;

        /// <summary>
        /// The DICOM images.
        /// </summary>
        ImageCollection _images = new ImageCollection();

        /// <summary>
        /// A value indicating whether application window is closing.
        /// </summary> 
        bool _isWindowClosing = false;

        /// <summary>
        /// The open file dialog for DICOM files.
        /// </summary>
        OpenFileDialog _openDicomFileDialog = new OpenFileDialog();

        /// <summary>
        /// The folder browser dialog for DICOM file series.
        /// </summary>
        System.Windows.Forms.FolderBrowserDialog _folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();


        #region Hot keys

        public static RoutedCommand _openCommand = new RoutedCommand();
        public static RoutedCommand _openFromFolderCommand = new RoutedCommand();
        public static RoutedCommand _saveImageCommand = new RoutedCommand();
        public static RoutedCommand _saveAllImagesCommand = new RoutedCommand();
        public static RoutedCommand _copyImageToClipboardCommand = new RoutedCommand();
        public static RoutedCommand _saveImageSliceCommand = new RoutedCommand();
        public static RoutedCommand _saveAllImagesSlicesCommand = new RoutedCommand();
        public static RoutedCommand _copyImageSliceToClipboardCommand = new RoutedCommand();
        public static RoutedCommand _exitCommand = new RoutedCommand();

        public static RoutedCommand _resetSceneCommand = new RoutedCommand();
        public static RoutedCommand _fitSceneCommand = new RoutedCommand();
        public static RoutedCommand _negativeImageCommand = new RoutedCommand();
        public static RoutedCommand _resetToDefaultWindowLevelCommand = new RoutedCommand();
        public static RoutedCommand _useInterpolationCommand = new RoutedCommand();
        public static RoutedCommand _showAxisCommand = new RoutedCommand();
        public static RoutedCommand _show3DAxisCommand = new RoutedCommand();
        public static RoutedCommand _fullScreenCommand = new RoutedCommand();
        public static RoutedCommand _topPanelAlwaysVisibleCommand = new RoutedCommand();
        public static RoutedCommand _showTextOverlayCommand = new RoutedCommand();

        public static RoutedCommand _aboutCommand = new RoutedCommand();

        #endregion

        #endregion



        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            // register the evaluation license for VintaSoft Imaging .NET SDK
            Vintasoft.Imaging.ImagingGlobalSettings.Register("REG_USER", "REG_EMAIL", "EXPIRATION_DATE", "REG_CODE");

            InitializeComponent();

            this.Title = string.Format(_titlePrefix, "(Untitled)");

            MoveDicomCodecToFirstPosition();

            // load VintaSoft JPEG2000 Plug-in - it is necessary for DICOM images with JPEG2000 compression
            Jpeg2000AssemblyLoader.Load();

            // subscribe to the image viewers events
            imageViewer1.GotFocus += new RoutedEventHandler(ImageViewer_GotFocus);
            imageViewer2.GotFocus += new RoutedEventHandler(ImageViewer_GotFocus);
            imageViewer3.GotFocus += new RoutedEventHandler(ImageViewer_GotFocus);

            InitFileDialogs();

            UpdateUI();

        }

        #endregion



        #region Properties

        bool _isDicomFileOpening = false;
        /// <summary>
        /// Gets or sets a value indicating whether the DICOM file is opening.
        /// </summary>
        bool IsDicomFileOpening
        {
            get
            {
                return _isDicomFileOpening;
            }
            set
            {
                _isDicomFileOpening = value;
                InvokeUpdateUI();
            }
        }

        /// <summary>
        /// Gets the focused viewer.
        /// </summary>
        /// <value>
        /// The focused viewer.
        /// </value>
        private WpfImageViewer FocusedViewer
        {
            get
            {
                return imageViewerToolBar.ImageViewer;
            }
        }

        /// <summary>
        /// Gets the DICOM MPR tool of focused viewer.
        /// </summary>
        private WpfDicomMprTool FocusedViewerDicomMprTool
        {
            get
            {
                return _visualizationController.GetDicomMprToolAssociatedWithImageViewer(FocusedViewer);
            }
        }

        #endregion



        #region Methods

        #region UI

        #region Main Form

        /// <summary>
        /// Raises the <see cref="Control.OnPreviewKeyDown"/> event.
        /// </summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && fullScreenMenuItem.IsChecked)
            {
                fullScreenMenuItem.IsChecked = false;
                ChangeWindowFullScreenMode(fullScreenMenuItem.IsChecked);
            }

            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// The window is closing.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            _isWindowClosing = true;

            base.OnClosing(e);
        }

        /// <summary>
        /// The window is closed.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            if (_visualizationController != null)
            {
                _visualizationController.Dispose();
                _visualizationController = null;
            }

            base.OnClosed(e);
        }

        #endregion


        #region 'File' menu

        /// <summary>
        /// Handles the Click event of openDicomFilesFromFolderMenuItem object.
        /// </summary>
        private void openDicomFilesFromFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // get the DICOM codec
                Codec dicomCodec = AvailableCodecs.GetCodecByName("Dicom");

                // get the folder path
                string folderPath = _folderBrowserDialog.SelectedPath;
                // get the file names
                List<string> fileNames = new List<string>();

                // for each DICOM file extensions
                foreach (string fileExtension in dicomCodec.FileExtensions)
                {
                    // get files
                    string[] files = Directory.GetFiles(folderPath, "*" + fileExtension, SearchOption.TopDirectoryOnly);

                    // add files
                    fileNames.AddRange(files);
                }

                OpenDicomFiles(fileNames.ToArray());
            }
        }

        /// <summary>
        /// Handles the Click event of openDicomFilesMenuItem object.
        /// </summary>
        private void openDicomFilesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenDicomFiles();
        }

        /// <summary>
        /// Handles the OpenFile event of imageViewerToolBar object.
        /// </summary>
        private void imageViewerToolBar_OpenFile(object sender, EventArgs e)
        {
            OpenDicomFiles();
        }

        /// <summary>
        /// Handles the Click event of closeDicomSeriesMenuItem object.
        /// </summary>
        private void closeDicomSeriesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // close the previously opened DICOM files
            CloseDicomSeries();
        }

        /// <summary>
        /// Handles the Click event of saveImageToolStripMenuItem object.
        /// </summary>
        private void saveImageToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get image of focused image viewer
            using (VintasoftImage image = FocusedViewer.RenderViewerImage())
            {
                // save image to a file
                SaveImageFileWindow.SaveImageToFile(image, ImagingEncoderFactory.Default);
            }
        }

        /// <summary>
        /// Handles the Click event of saveAllImagesToolStripMenuItem object.
        /// </summary>
        private void saveAllImagesToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // create a temporary image collection
            using (ImageCollection images = new ImageCollection())
            {
                // get image of the first viewer and add it to the temporary image collection
                images.Add(imageViewer1.RenderViewerImage());
                // get image of the second viewer and add it to the temporary image collection
                images.Add(imageViewer2.RenderViewerImage());
                // get image of the third viewer and add it to the temporary image collection
                images.Add(imageViewer3.RenderViewerImage());

                // save images to a file
                SaveImageFileWindow.SaveImagesToFile(images, ImagingEncoderFactory.Default);

                // clear and dispose images in the temporary image collection
                images.ClearAndDisposeItems();
            }
        }

        /// <summary>
        /// Handles the Click event of copyImageToClipboardMenuItem object.
        /// </summary>
        private void copyImageToClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get image of focused image viewer
            using (VintasoftImage image = FocusedViewer.RenderViewerImage())
            {
                // copy image to the clipboard
                CopyToClipboard(image);
            }
        }

        /// <summary>
        /// Handles the Click event of saveImageSliceMenuItem object.
        /// </summary>
        private void saveImageSliceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get the DICOM MPR tool, which is associated with focused image viewer
            WpfDicomMprTool dicomMprTool =
                _visualizationController.GetDicomMprToolAssociatedWithImageViewer(FocusedViewer);

            // get the image slice from DICOM MPR tool
            using (VintasoftImage image = dicomMprTool.DicomViewerTool.GetDisplayedImage())
            {
                // save image to a file
                SaveImageFileWindow.SaveImageToFile(image, ImagingEncoderFactory.Default);
            }
        }

        /// <summary>
        /// Handles the Click event of saveAllImagesSlicesMenuItem object.
        /// </summary>
        private void saveAllImagesSlicesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // create a temporary image collection
            using (ImageCollection images = new ImageCollection())
            {
                // for each DICOM MPR tool
                foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                {
                    // get the slice image from DICOM MPR tool
                    VintasoftImage sliceImage = dicomMprTool.DicomViewerTool.GetDisplayedImage();
                    // add the slice image to the temporary image collection
                    images.Add(sliceImage);
                }

                // save images to a file
                SaveImageFileWindow.SaveImagesToFile(images, ImagingEncoderFactory.Default);

                // clear and dispose images in the temporary image collection
                images.ClearAndDisposeItems();
            }
        }

        /// <summary>
        /// Handles the Click event of copyImageSliceToClipboardMenuItem object.
        /// </summary>
        private void copyImageSliceToClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get the DICOM MPR tool, which is associated with focused image viewer
            WpfDicomMprTool dicomMprTool =
                _visualizationController.GetDicomMprToolAssociatedWithImageViewer(FocusedViewer);

            // get the image slice from DICOM MPR tool
            using (VintasoftImage image = dicomMprTool.DicomViewerTool.GetDisplayedImage())
            {
                // copy image to the clipboard
                CopyToClipboard(image);
            }
        }

        /// <summary>
        /// Handles the Click event of exitMenuItem object.
        /// </summary>
        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion


        #region 'View' menu

        /// <summary>
        /// Handles the Click event of resetSceneMenuItem object.
        /// </summary>
        private void resetSceneMenuItem_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _slices.Length; i++)
                _defaultSlices[i].CopyTo(_slices[i]);

            FitScene();
        }

        /// <summary>
        /// Handles the Click event of fitSceneMenuItem object.
        /// </summary>
        private void fitSceneMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FitScene();
        }

        /// <summary>
        /// Handles the Click event of synchronizeWindowLevelMenuItem object.
        /// </summary>
        private void synchronizeWindowLevelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            synchronizeWindowLevelMenuItem.IsChecked ^= true;

            // if VOI LUT must be synchronized
            if (synchronizeWindowLevelMenuItem.IsChecked)
            {
                // get the current DicomMprTool
                WpfDicomMprTool focusedViewerMprTool =
                    _visualizationController.GetDicomMprToolAssociatedWithImageViewer(FocusedViewer);
                // get the VOI LUT of focused DICOM image
                DicomImageVoiLookupTable focusedImageVoiLutTable = focusedViewerMprTool.DicomViewerTool.DicomImageVoiLut;

                foreach (WpfDicomMprTool tool in _dicomMprTools)
                {
                    // if visual tool must be skipped
                    if (tool == focusedViewerMprTool)
                        continue;

                    tool.DicomViewerTool.DicomImageVoiLut = focusedImageVoiLutTable;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of negativeImageMenuItem object.
        /// </summary>
        private void negativeImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            negativeImageMenuItem.IsChecked ^= true;

            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                dicomMprTool.DicomViewerTool.IsImageNegative = negativeImageMenuItem.IsChecked;
        }

        /// <summary>
        /// Handles the Click event of resetToDefaultWindowLevelMenuItem object.
        /// </summary>
        private void resetToDefaultWindowLevelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DicomImageVoiLookupTable defaultVoiLut = _dicomMprTools[0].DicomViewerTool.DefaultDicomImageVoiLut;

            UpdateDicomImageVoiLookupTable(defaultVoiLut);
        }

        /// <summary>
        /// Handles the Click event of useInterpolationMenuItem object.
        /// </summary>
        private void useInterpolationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get focused viewer DICOM tool
            WpfDicomMprTool focusedViewerMprTool = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(FocusedViewer);
            // indicates whether interpolation is enabled
            bool isInterpolationEnabled = useInterpolationMenuItem.IsChecked;

            // if interpolation is enabled
            if (isInterpolationEnabled)
            {
                // disable interpolation
                focusedViewerMprTool.MprImageTool.RenderingInterpolationMode = MprInterpolationMode.NearestNeighbor;
            }
            // if interpolation is disabled
            else
            {
                // enable interpolation
                focusedViewerMprTool.MprImageTool.RenderingInterpolationMode = MprInterpolationMode.Linear;
            }

            useInterpolationMenuItem.IsChecked = !isInterpolationEnabled;
        }

        /// <summary>
        /// Handles the Click event of showAxisMenuItem object.
        /// </summary>
        private void showAxisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            showAxisMenuItem.IsChecked ^= true;
            bool value = showAxisMenuItem.IsChecked;

            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                dicomMprTool.MprImageTool.AreAxesVisible = value;
        }

        /// <summary>
        /// Handles the Click event of show3DAxisMenuItem object.
        /// </summary>
        private void show3DAxisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            show3DAxisMenuItem.IsChecked ^= true;

            Set3DAxisVisibility(show3DAxisMenuItem.IsChecked);
        }

        /// <summary>
        /// Handles the Click event of showMPRParametersMenuItem object.
        /// </summary>
        private void showMPRParametersMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!showMPRParametersMenuItem.IsChecked)
            {
                // if MPR parameters form is not created
                if (_mprParametersWindow == null)
                    CreateDicomMprParametersViewerForm();
                // show form
                _mprParametersWindow.Show();
                showMPRParametersMenuItem.IsChecked = true;

                // disable all MPR image tools
                foreach (WpfDicomMprTool tool in _dicomMprTools)
                    tool.MprImageTool.Enabled = false;
            }
            else
            {
                // if MPR parameters form is created
                if (_mprParametersWindow != null)
                {
                    // close the MPR parameters form
                    _mprParametersWindow.Close();
                }
            }
        }

        /// <summary>
        /// Handles the Click event of fullScreenMenuItem object.
        /// </summary>
        private void fullScreenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            fullScreenMenuItem.IsChecked = !fullScreenMenuItem.IsChecked;
            ChangeWindowFullScreenMode(fullScreenMenuItem.IsChecked);
        }

        /// <summary>
        /// Handles the Click event of topPanelAlwaysVisibleMenuItem object.
        /// </summary>
        private void topPanelAlwaysVisibleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            topPanelAlwaysVisibleMenuItem.IsChecked = !topPanelAlwaysVisibleMenuItem.IsChecked;

            if (fullScreenMenuItem.IsChecked)
            {
                if (topPanelAlwaysVisibleMenuItem.IsChecked)
                {
                    topPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    topPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of textOverlaySettingsMenuItem object.
        /// </summary>
        private void textOverlaySettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DicomOverlaySettingEditorWindow dlg = new DicomOverlaySettingEditorWindow(OVERLAY_OWNER_NAME, _dicomMprTools[0]);
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Owner = this;
            // show dialog
            dlg.ShowDialog();

            DicomOverlaySettingEditorWindow.SetTextOverlay(OVERLAY_OWNER_NAME, _dicomMprTools[0]);
            DicomOverlaySettingEditorWindow.SetTextOverlay(OVERLAY_OWNER_NAME, _dicomMprTools[1]);
            DicomOverlaySettingEditorWindow.SetTextOverlay(OVERLAY_OWNER_NAME, _dicomMprTools[2]);
        }

        /// <summary>
        /// Handles the Click event of showTextOverlayMenuItem object.
        /// </summary>
        private void showTextOverlayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            showTextOverlayMenuItem.IsChecked ^= true;
            bool value = showTextOverlayMenuItem.IsChecked;

            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                dicomMprTool.IsTextOverlayVisible = value;
        }

        /// <summary>
        /// Handles the Click event of settingsMenuItem object.
        /// </summary>
        private void settingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SliceType selectedSliceType;

            if (imageViewer1.IsFocused)
                selectedSliceType = SliceType.Sagittal;
            else if (imageViewer2.IsFocused)
                selectedSliceType = SliceType.Coronal;
            else
                selectedSliceType = SliceType.Axial;

            MprImageToolAppearanceSettingsWindow dlg = new MprImageToolAppearanceSettingsWindow(
                _mprSettingsManager,
                selectedSliceType,
                SliceType.Sagittal,
                SliceType.Coronal,
                SliceType.Axial);
            dlg.Owner = this;
            dlg.ShowDialog();

            UpdateMprSettings();
        }

        #endregion


        #region 'MPR' menu

        /// <summary>
        /// Handles the Click event of sagittalMenuItem object.
        /// </summary>
        private void sagittalMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MprImage mprImage = _mprSourceDataController.MprImage;
            // create the planar slice
            MprPlanarSlice planarSlice = mprImage.CreateSagittalSlice(mprImage.XLength / 2.0);

            // show the slice
            ShowMprForm(planarSlice);
        }

        /// <summary>
        /// Handles the Click event of coronalMenuItem object.
        /// </summary>
        private void coronalMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MprImage mprImage = _mprSourceDataController.MprImage;
            // create the planar slice
            MprPlanarSlice planarSlice = mprImage.CreateCoronalSlice(mprImage.YLength / 2.0);

            // show the slice
            ShowMprForm(planarSlice);
        }

        /// <summary>
        /// Handles the Click event of axialMenuItem object.
        /// </summary>
        private void axialMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MprImage mprImage = _mprSourceDataController.MprImage;
            // create the planar slice
            MprPlanarSlice planarSlice = mprImage.CreateAxialSlice(mprImage.ZLength / 2.0);

            // show the slice
            ShowMprForm(planarSlice);
        }

        /// <summary>
        /// Handles the Click event of curvilinearSliceOnSagittalMenuItem object.
        /// </summary>
        private void curvilinearSliceOnSagittalMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MprImage mprImage = _mprSourceDataController.MprImage;
            // create the planar slice
            MprPlanarSlice planarSlice = mprImage.CreateSagittalSlice(mprImage.XLength / 2.0);

            // show the slice
            ShowMprCurvilinearSliceForm(planarSlice);
        }

        /// <summary>
        /// Handles the Click event of curvilinearSliceOnCoronalMenuItem object.
        /// </summary>
        private void curvilinearSliceOnCoronalMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MprImage mprImage = _mprSourceDataController.MprImage;
            // create the planar slice
            MprPlanarSlice planarSlice = mprImage.CreateCoronalSlice(mprImage.YLength / 2.0);

            // show the slice
            ShowMprCurvilinearSliceForm(planarSlice);
        }

        /// <summary>
        /// Handles the Click event of curvilinearSliceOnAxialMenuItem object.
        /// </summary>
        private void curvilinearSliceOnAxialMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MprImage mprImage = _mprSourceDataController.MprImage;
            // create the planar slice
            MprPlanarSlice planarSlice = mprImage.CreateAxialSlice(mprImage.ZLength / 2.0);

            // show the slice
            ShowMprCurvilinearSliceForm(planarSlice);
        }

        /// <summary>
        /// Handles the Click event of ImagePropertiesMenuItem object.
        /// </summary>
        private void ImagePropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PropertyGridWindow dlg = new PropertyGridWindow(_mprSourceDataController.MprImage, "MPR Image");
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        /// <summary>
        /// Handles the Closing event of DicomMprParametersForm object.
        /// </summary>
        private void DicomMprParametersForm_Closing(object sender, CancelEventArgs e)
        {
            _mprParametersWindow.Closing -= DicomMprParametersForm_Closing;
            _mprParametersWindow = null;
            showMPRParametersMenuItem.IsChecked = false;

            foreach (WpfDicomMprTool tool in _dicomMprTools)
                tool.MprImageTool.Enabled = true;
        }

        /// <summary>
        /// Handles the DicomImageVoiLutChanged event of dicomViewerTool object.
        /// </summary>
        private void dicomViewerTool_DicomImageVoiLutChanged(
            object sender,
            WpfVoiLutChangedEventArgs e)
        {
            // if VOI LUT can not be synchronized
            if (!synchronizeWindowLevelMenuItem.IsChecked)
                return;

            // if VOI LUT is changing
            if (_isImageVoiLutChanging)
                return;

            _isImageVoiLutChanging = true;
            foreach (WpfDicomMprTool mprTool in _dicomMprTools)
            {
                if (sender != mprTool)
                {
                    // update VOI LUT
                    mprTool.DicomViewerTool.DicomImageVoiLut =
                        new DicomImageVoiLookupTable(e.WindowCenter, e.WindowWidth);
                }
            }
            _isImageVoiLutChanging = false;
        }

        /// <summary>
        /// Handles the MouseDown event of dicomMprTool object.
        /// </summary>
        private void dicomMprTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WpfImageViewer viewer = ((WpfVisualTool)sender).ImageViewer;
            // if image viewer is focused AND
            // mouse left button is clicked AND
            // MPR parameters form is not null
            if (viewer.IsFocused &&
                e.ChangedButton == MouseButton.Left &&
                _mprParametersWindow != null)
            {
                Point location = e.GetPosition(viewer);
                UpdateDicomMprParametersForm(viewer, location.X, location.Y);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the MouseMove event of dicomMprTool object.
        /// </summary>
        private void dicomMprTool_MouseMove(object sender, MouseEventArgs e)
        {
            WpfImageViewer viewer = ((WpfVisualTool)sender).ImageViewer;
            // if image viewer is focused AND
            // mouse left button is clicked AND
            // MPR parameters form is not null
            if (viewer.IsFocused &&
                e.LeftButton == MouseButtonState.Pressed &&
                _mprParametersWindow != null)
            {
                Point location = e.GetPosition(viewer);
                UpdateDicomMprParametersForm(viewer, location.X, location.Y);
                e.Handled = true;
            }
        }

        #endregion


        #region 'Help' menu

        /// <summary>
        /// Handles the Click event of aboutMenuItem object.
        /// </summary>
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder description = new StringBuilder();

            description.AppendLine("This project demonstrates how to preview DICOM files and allows to:");
            description.AppendLine();
            description.AppendLine("Create and view 3D multiplanar reconstruction of DICOM images.");
            description.AppendLine();
            description.AppendLine("Create and view 2D multiplanar reconstruction of DICOM images.");
            description.AppendLine();

            description.AppendLine();
            description.AppendLine("The project is available in C# and VB.NET for Visual Studio .NET.");

            WpfAboutBoxBaseWindow dlg = new WpfAboutBoxBaseWindow("vsdicom-dotnet");
            dlg.Description = description.ToString();
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.ShowDialog();
        }

        #endregion


        /// <summary>
        /// Handles the GotFocus event of ImageViewer object.
        /// </summary>
        private void ImageViewer_GotFocus(object sender, EventArgs e)
        {
            WpfImageViewer viewer = (WpfImageViewer)sender;

            if (viewer != imageViewerToolBar.ImageViewer)
                imageViewerToolBar.ImageViewer = (WpfImageViewer)sender;
        }

        #endregion


        #region Init

        /// <summary>
        /// Moves the DICOM codec to the first position in <see cref="AvailableCodecs"/>.
        /// </summary>
        private static void MoveDicomCodecToFirstPosition()
        {
            ReadOnlyCollection<Codec> codecs = AvailableCodecs.Codecs;

            for (int i = codecs.Count - 1; i >= 0; i--)
            {
                Codec codec = codecs[i];

                if (codec.Name.Equals("DICOM", StringComparison.InvariantCultureIgnoreCase))
                {
                    AvailableCodecs.RemoveCodec(codec);
                    AvailableCodecs.InsertCodec(0, codec);
                    break;
                }
            }
        }

        /// <summary>
        /// Initializes the file dialogs.
        /// </summary>
        private void InitFileDialogs()
        {
            _openDicomFileDialog.Filter =
                "DICOM files|*.dcm;*.dic;*.acr|" +
                "All files|*.*";
        }

        #endregion


        #region UI state

        /// <summary>
        /// Updates UI safely.
        /// </summary>
        private void InvokeUpdateUI()
        {
            if (Dispatcher.Thread == Thread.CurrentThread)
                UpdateUI();
            else
                Dispatcher.Invoke(new UpdateUIDelegate(UpdateUI));
        }

        /// <summary>
        /// Updates the user interface of this window.
        /// </summary>
        private void UpdateUI()
        {
            // if application is closing
            if (_isWindowClosing)
                // exit
                return;

            bool isDicomFileLoaded = _dicomSeriesController.ImageCount > 0 && _visualizationController != null;
            bool isDicomFileOpening = _isDicomFileOpening;

            // 'File' menu
            //
            openDicomFilesMenuItem.IsEnabled = !isDicomFileOpening;
            openDicomFilesFromFolderMenuItem.IsEnabled = !isDicomFileOpening;
            saveImageMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            saveAllImagesMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            copyImageToClipboardMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;

            saveImageSliceMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            saveAllImagesSlicesMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            copyImageSliceToClipboardMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;

            closeDicomSeriesMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;

            // 'View' menu
            //
            resetSceneMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            fitSceneMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            synchronizeWindowLevelMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            negativeImageMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;

            resetToDefaultWindowLevelMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            useInterpolationMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            showAxisMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            show3DAxisMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            showMPRParametersMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;

            fullScreenMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            topPanelAlwaysVisibleMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            textOverlaySettingsMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            showTextOverlayMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            settingsMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;

            // "MPR" menu
            //
            sagittalMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            coronalMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            axialMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            curvilinearSliceOnSagittalMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            curvilinearSliceOnCoronalMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            curvilinearSliceOnAxialMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
            imagePropertiesMenuItem.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;

            dicomMprToolInteractionModeToolBar.IsEnabled = isDicomFileLoaded && !isDicomFileOpening;
        }

        #endregion


        #region 'File' menu

        /// <summary>
        /// Opens a DICOM files.
        /// </summary>
        private void OpenDicomFiles()
        {
            _openDicomFileDialog.Multiselect = true;
            if (_openDicomFileDialog.ShowDialog() == true)
            {
                OpenDicomFiles(_openDicomFileDialog.FileNames);
            }
        }

        /// <summary>
        /// Opens a DICOM files.
        /// </summary>
        private void OpenDicomFiles(params string[] fileNames)
        {
            if (fileNames != null && fileNames.Length > 0)
            {
                IsDicomFileOpening = true;
                try
                {
                    // close the previously opened DICOM files
                    CloseDicomSeries();

                    // add DICOM files to the DICOM series
                    AddDicomFilesToSeries(fileNames);

                    // if DICOM images are loaded
                    if (_images.Count > 0)
                    {
                        // update source data of MPR image
                        _mprSourceDataController.SetSourceMprData(_dicomSeriesController.GetFilesOfSeries());

                        if (!InitializeVisualizationController())
                            CloseDicomSeries();
                    }
                }
                finally
                {
                    if (!_isWindowClosing)
                        IsDicomFileOpening = false;
                }
            }
        }

        /// <summary>
        /// Initializes the <see cref="_visualizationController"/>.
        /// </summary>
        private bool InitializeVisualizationController()
        {
            DicomFrame dicomFrame = DicomFrame.GetFrameAssociatedWithImage(_images[0]);

            // create the MPR image
            if (!_mprSourceDataController.SetMprData(dicomFrame, true))
                return false;

            WpfMprVisualizationController oldVisualizationController = _visualizationController;
            // create visualization controller
            _visualizationController = new WpfMprVisualizationController(
                _mprSourceDataController.MprImage,
                imageViewer1, imageViewer2, imageViewer3);

            _mprSettingsManager.SetMprVisualizationControllerSettings(_visualizationController);

            // get DICOM MPR tools of image viewers
            _dicomMprTools = new WpfDicomMprTool[] {
                _visualizationController.GetDicomMprToolAssociatedWithImageViewer(imageViewer1),
                _visualizationController.GetDicomMprToolAssociatedWithImageViewer(imageViewer2),
                _visualizationController.GetDicomMprToolAssociatedWithImageViewer(imageViewer3)
            };
            // set main DICOM MPR tool
            dicomMprToolInteractionModeToolBar.DicomMprTools = _dicomMprTools;

            // create slices
            _slices = _visualizationController.AddThreeSlicesVisualization(
                _mprSourceDataController.MprImage.XLength / 2.0, _mprSettingsManager.SagittalSliceAppearance.SliceColor,
                _mprSourceDataController.MprImage.YLength / 2.0, _mprSettingsManager.CoronalSliceAppearance.SliceColor,
                _mprSourceDataController.MprImage.ZLength / 2.0, _mprSettingsManager.AxialSliceAppearance.SliceColor);

            // apply the appearance settings to the slices
            _mprSettingsManager.SetSliceSettings(
                _visualizationController.GetVisualMprSliceAssociatedWithMprSlice(_slices[0]),
                _visualizationController.GetVisualMprSliceAssociatedWithMprSlice(_slices[1]),
                _visualizationController.GetVisualMprSliceAssociatedWithMprSlice(_slices[2]));

            // save the default values of slices
            _defaultSlices = new MprSlice[_slices.Length];
            for (int i = 0; i < _slices.Length; i++)
                _defaultSlices[i] = _slices[i].CreateCopy();

            // apply appearance settings to the visual tools
            _mprSettingsManager.SetMprToolSettings(_dicomMprTools[0].MprImageTool);
            _mprSettingsManager.SetMprToolSettings(_dicomMprTools[1].MprImageTool);
            _mprSettingsManager.SetMprToolSettings(_dicomMprTools[2].MprImageTool);

            // for each DICOM MPR tool
            foreach (WpfDicomMprTool mprTool in _dicomMprTools)
            {
                mprTool.DicomViewerTool.DicomImageVoiLutChanged += new EventHandler<WpfVoiLutChangedEventArgs>(dicomViewerTool_DicomImageVoiLutChanged);
                mprTool.MouseMove += new MouseEventHandler(dicomMprTool_MouseMove);
                mprTool.MouseDown += new MouseButtonEventHandler(dicomMprTool_MouseDown);

                WpfDicomMprFillDataProgressTextOverlay loadingProgressOverlay = new WpfDicomMprFillDataProgressTextOverlay();
                mprTool.TextOverlay.Add(loadingProgressOverlay);
            }

            // shows the slices on viewers
            _visualizationController.ShowSliceInViewer(imageViewer1, _slices[0]);
            _visualizationController.ShowSliceInViewer(imageViewer2, _slices[1]);
            _visualizationController.ShowSliceInViewer(imageViewer3, _slices[2]);

            if (oldVisualizationController != null)
            {
                useInterpolationMenuItem.IsChecked = true;

                oldVisualizationController.SetProperties(_visualizationController);
                Set3DAxisVisibility(show3DAxisMenuItem.IsChecked);

                oldVisualizationController.Dispose();
            }

            imageViewer1.Focus();

            return true;
        }

        /// <summary>
        /// Updates values of <see cref="MprParametersViewerForm"/>.
        /// </summary>
        /// <param name="viewer">The focused image viewer.</param>
        /// <param name="mouseLocationX">The mouse X coordinate.</param>
        /// <param name="mouseLocationY">The mouse Y coordinate.</param>
        private void UpdateDicomMprParametersForm(
            WpfImageViewer viewer,
            double mouseLocationX,
            double mouseLocationY)
        {
            // get location on image
            double xCoordinate = mouseLocationX;
            double yCoordinate = mouseLocationY;
            AffineMatrix transformFromControlToImage = FocusedViewer.GetTransformFromControlToImage();
            Point locationOnImage = WpfPointAffineTransform.TransformPoint(transformFromControlToImage, new Point(xCoordinate, yCoordinate));

            // get focused viewer visual tool
            WpfDicomMprTool dicomMprTool = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(viewer);
            // get transform from image to slice
            WpfPointTransform transformFormImageToSlice = dicomMprTool.MprImageTool.FocusedSliceView.GetPointTransform(viewer, viewer.Image);
            transformFormImageToSlice = transformFormImageToSlice.GetInverseTransform();

            // update values
            _mprParametersWindow.LocationOnImage = new Point(locationOnImage.X, locationOnImage.Y);
            _mprParametersWindow.TransformFromImageToSlice = transformFormImageToSlice;
            _mprParametersWindow.DisplayedImage = dicomMprTool.DicomViewerTool.DisplayedImage;
            _mprParametersWindow.MprImage = dicomMprTool.MprImageTool.MprImage;
            _mprParametersWindow.Slice = dicomMprTool.MprImageTool.FocusedSlice;
            _mprParametersWindow.UpdateValues();
        }

        /// <summary>
        /// Adds the DICOM files to the series.
        /// </summary>
        /// <param name="filesPath">Files path.</param>
        private void AddDicomFilesToSeries(params string[] filesPath)
        {
            try
            {
                List<DicomFile> filesForLoadPresentationState = new List<DicomFile>();
                string dirPath = null;

                // show action label and progress bar
                actionLabel.Visibility = Visibility.Visible;
                progressBar1.Visibility = Visibility.Visible;
                progressBar1.Maximum = filesPath.Length;
                progressBar1.Value = 0;

                bool skipCorruptedFiles = false;

                foreach (string filePath in filesPath)
                {
                    if (dirPath == null)
                        dirPath = Path.GetDirectoryName(filePath);

                    // set action info
                    actionLabel.Content = string.Format("Loading {0}", Path.GetFileName(filePath));
                    // update progress bar
                    progressBar1.Value++;
                    DemosTools.DoEvents();

                    DicomFile dicomFile = null;
                    try
                    {
                        // if the series already contains the specified DICOM file
                        if (_dicomSeriesController.Contains(filePath))
                        {
                            DemosTools.ShowInfoMessage(string.Format("The series already contains DICOM file \"{0}\".", Path.GetFileName(filePath)));
                            return;
                        }

                        // instance number of new DICOM file
                        int newDicomFileInstanceNumber = 0;
                        // add DICOM file to the current series of DICOM images and get the DICOM images of new DICOM file
                        ImageCollection newDicomImages =
                            _dicomSeriesController.AddDicomFileToSeries(filePath, out dicomFile, out newDicomFileInstanceNumber);

                        // if DICOM file does not contain images
                        if (dicomFile.Pages.Count == 0)
                        {
                            // show message for user
                            DemosTools.ShowInfoMessage("DICOM file does not contain image.");
                        }
                        else
                        {
                            // update frame count in series
                            imageViewerToolBar.PageCount = _images.Count + dicomFile.Pages.Count;

                            // get image index in image collection of current DICOM file
                            int imageIndex = GetImageIndexInImageCollectionForNewImage(newDicomFileInstanceNumber);

                            try
                            {
                                // insert images to the specified index
                                _images.InsertRange(imageIndex, newDicomImages.ToArray());
                            }
                            catch
                            {
                                // remove new DICOM images from image collection of image viewer
                                foreach (VintasoftImage newDicomImage in newDicomImages)
                                    _images.Remove(newDicomImage);

                                // close new DICOM file
                                _dicomSeriesController.CloseDicomFile(dicomFile);
                                dicomFile = null;

                                // update frame count in series
                                imageViewerToolBar.PageCount = imageViewer1.Images.Count;

                                throw;
                            }
                        }

                        // update header of form
                        this.Title = string.Format(_titlePrefix, Path.GetFileName(filePath));
                    }
                    catch (Exception ex)
                    {
                        // close file
                        if (dicomFile != null)
                            _dicomSeriesController.CloseDicomFile(dicomFile);

                        if (!skipCorruptedFiles)
                        {
                            if (filesPath.Length == 1)
                            {
                                DemosTools.ShowErrorMessage(ex);

                                dirPath = null;
                                CloseDicomSeries();
                            }
                            else
                            {
                                string exceptionMessage = string.Format(
                                    "The file '{0}' can not be opened:\r\n\"{1}\"\r\nDo you want to continue anyway?",
                                    Path.GetFileName(filePath), DemosTools.GetFullExceptionMessage(ex).Trim());
                                if (MessageBox.Show(
                                    exceptionMessage,
                                    "Error",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Error) == MessageBoxResult.No)
                                {
                                    dirPath = null;
                                    CloseDicomSeries();
                                    break;
                                }
                            }

                            skipCorruptedFiles = true;
                        }
                    }
                }

                // hide action label and progress bar
                actionLabel.Content = string.Empty;
                actionLabel.Visibility = Visibility.Collapsed;
                progressBar1.Visibility = Visibility.Collapsed;

                // update UI
                UpdateUI();
            }
            finally
            {
                // hide action label and progress bar
                actionLabel.Content = string.Empty;
                actionLabel.Visibility = Visibility.Collapsed;
                progressBar1.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Returns the index, in image collection, where the new DICOM image must be inserted.
        /// </summary>
        /// <param name="dicomFileInstanceNumber">The DICOM file instance number of new image.</param>
        /// <returns>
        /// The image index of image collection.
        /// </returns>
        private int GetImageIndexInImageCollectionForNewImage(int newImageDicomFileInstanceNumber)
        {
            int imageIndex = imageViewer1.Images.Count;
            while (imageIndex > 0)
            {
                // get DICOM file instance number for the image from image collection
                int imageDicomFileInstanceNumber =
                    _dicomSeriesController.GetDicomFileInstanceNumber(imageViewer1.Images[imageIndex - 1]);

                // if new image must be inserted after the image from image collection
                if (newImageDicomFileInstanceNumber > imageDicomFileInstanceNumber)
                    break;

                imageIndex--;
            }
            return imageIndex;
        }

        /// <summary>
        /// Closes series of DICOM frames.
        /// </summary>
        private void CloseDicomSeries()
        {
            dicomMprToolInteractionModeToolBar.DicomMprTools = null;

            if (_visualizationController != null)
                _visualizationController.IsEnabled = false;

            // if DICOM series has files
            if (_dicomSeriesController.FileCount > 0)
            {
                _mprSourceDataController.ClearSourceMprData();

                imageViewerToolBar.SelectedPageIndex = -1;
                imageViewerToolBar.PageCount = 0;

                // clear image collection of image viewer and dispose all images
                _images.ClearAndDisposeItems();

                _dicomSeriesController.CloseSeries();

                this.Title = string.Format(_titlePrefix, "(Untitled)");

                // update the UI
                UpdateUI();
            }
        }

        /// <summary>
        /// Copies the specified image to the clipboard.
        /// </summary>
        /// <param name="image">The image.</param>
        private void CopyToClipboard(VintasoftImage image)
        {
            try
            {
                // if image is Gray16 image
                if (image.PixelFormat == Vintasoft.Imaging.PixelFormat.Gray16)
                {
                    // convert image to Gray8 image
                    ChangePixelFormatToGrayscaleCommand command =
                        new ChangePixelFormatToGrayscaleCommand(Vintasoft.Imaging.PixelFormat.Gray8);
                    image = command.Execute(image);
                }

                // copy image to the clipboard
                Clipboard.SetImage(VintasoftImageConverter.ToBitmapSource(image));
            }
            catch (Exception ex)
            {
                DemosTools.ShowErrorMessage(ex);
            }
        }

        #endregion


        #region 'View' menu

        /// <summary>
        /// Changes the visibility of 3D axis.
        /// </summary>
        /// <param name="visibility">The visibility of 3D axis.</param>
        private void Set3DAxisVisibility(bool visibility)
        {
            if (visibility)
            {
                foreach (WpfDicomMprTool mprTool in _dicomMprTools)
                {
                    mprTool.DicomViewerTool.GraphicalOverlay.Clear();
                    mprTool.DicomViewerTool.GraphicalOverlay.Add(new WpfMprImage3DAxisGraphicObject(_visualizationController));
                    mprTool.DicomViewerTool.IsGraphicalOverlayVisible = true;
                }
            }
            else
            {
                foreach (WpfDicomMprTool mprTool in _dicomMprTools)
                {
                    mprTool.DicomViewerTool.IsGraphicalOverlayVisible = false;
                    mprTool.DicomViewerTool.GraphicalOverlay.Clear();
                }
            }
        }

        /// <summary>
        /// Scales the slices proportion to the viewer.
        /// </summary>
        private void FitScene()
        {
            foreach (WpfDicomMprTool mprTool in _dicomMprTools)
                mprTool.MprImageTool.FitScene();
        }

        /// <summary>
        /// Updates the VOI LUT in DicomViewerTools.
        /// </summary>
        /// <param name="voiLut">The VOI LUT.</param>
        private void UpdateDicomImageVoiLookupTable(DicomImageVoiLookupTable voiLut)
        {
            _isImageVoiLutChanging = true;
            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                dicomMprTool.DicomViewerTool.DicomImageVoiLut = voiLut;
            _isImageVoiLutChanging = false;
        }


        /// <summary>
        /// Creates a form that allows to view parameters of DICOM 3D MPR (multiplanar reconstruction).
        /// </summary>
        private void CreateDicomMprParametersViewerForm()
        {
            WpfDicomMprTool dicomMprTool = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(FocusedViewer);
            // get transform from image to slice
            WpfPointTransform transformFormImageToSlice = dicomMprTool.MprImageTool.FocusedSliceView.GetPointTransform(FocusedViewer, FocusedViewer.Image);
            transformFormImageToSlice = transformFormImageToSlice.GetInverseTransform();

            // create form
            _mprParametersWindow = new MprParametersViewerWindow(
                new Point(0, 0),
                transformFormImageToSlice,
                dicomMprTool.DicomViewerTool.DisplayedImage,
                dicomMprTool.MprImageTool.MprImage,
                dicomMprTool.MprImageTool.FocusedSlice);
            _mprParametersWindow.Owner = this;
            _mprParametersWindow.Closing += new CancelEventHandler(DicomMprParametersForm_Closing);
        }

        /// <summary>
        /// Changes window full screen mode.
        /// </summary>
        /// <param name="fullScreenMode">Determines whether full screen mode is on.</param>
        private void ChangeWindowFullScreenMode(bool fullScreenMode)
        {
            if (fullScreenMode)
            {
                // if the top panel must be hidden
                if (!topPanelAlwaysVisibleMenuItem.IsChecked)
                {
                    topPanel.Visibility = Visibility.Collapsed;
                }

                // update the form settings
                _previousWindowState = WindowState;

                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
            {
                // show the top panel
                topPanel.Visibility = Visibility.Visible;

                // update the form settings
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                if (WindowState != _previousWindowState)
                    WindowState = _previousWindowState;
            }
        }

        /// <summary>
        /// Updates MPR settings.
        /// </summary>
        private void UpdateMprSettings()
        {
            // update MPR Image
            _mprSettingsManager.SetMprImageSettings(_visualizationController.MprImage);
            // update controller
            _mprSettingsManager.SetMprVisualizationControllerSettings(_visualizationController);
            // update slices
            _mprSettingsManager.SetSliceSettings(
                _visualizationController.GetVisualMprSliceAssociatedWithMprSlice(_slices[0]),
                _visualizationController.GetVisualMprSliceAssociatedWithMprSlice(_slices[1]),
                _visualizationController.GetVisualMprSliceAssociatedWithMprSlice(_slices[2]));
            // update default slices
            _defaultSlices[0].Thickness = _slices[0].Thickness;
            _defaultSlices[1].Thickness = _slices[1].Thickness;
            _defaultSlices[2].Thickness = _slices[2].Thickness;
            _defaultSlices[0].RenderingMode = _slices[0].RenderingMode;
            _defaultSlices[1].RenderingMode = _slices[1].RenderingMode;
            _defaultSlices[2].RenderingMode = _slices[2].RenderingMode;
            // update MPR tools
            _mprSettingsManager.SetMprToolSettings(_dicomMprTools[0].MprImageTool);
            _mprSettingsManager.SetMprToolSettings(_dicomMprTools[1].MprImageTool);
            _mprSettingsManager.SetMprToolSettings(_dicomMprTools[2].MprImageTool);
        }

        #endregion


        #region 'MPR' menu

        /// <summary>
        /// Shows the MPR planar slice.
        /// </summary>
        /// <param name="planarSlice">The planar slice.</param>
        private void ShowMprForm(MprPlanarSlice planarSlice)
        {
            // get VOI LUT of current frame
            DicomImageVoiLookupTable voiLut = FocusedViewerDicomMprTool.DicomViewerTool.DicomImageVoiLut;
            bool isNegative = FocusedViewerDicomMprTool.DicomViewerTool.IsImageNegative;

            // create dialog
            Mpr2DWindow dlg = new Mpr2DWindow(
                _mprSourceDataController.MprImage,
                planarSlice,
                voiLut,
                isNegative,
                _mprSettingsManager);

            // set start position on dialog
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Owner = this;
            // show dialog
            dlg.ShowDialog();
        }

        /// <summary>
        /// Shows the MPR curvilinear slice.
        /// </summary>
        /// <param name="planarSlice">The planar slice.</param>
        private void ShowMprCurvilinearSliceForm(MprPlanarSlice planarSlice)
        {
            // get VOI LUT of current frame
            DicomImageVoiLookupTable voiLut = FocusedViewerDicomMprTool.DicomViewerTool.DicomImageVoiLut;
            bool isNegative = FocusedViewerDicomMprTool.DicomViewerTool.IsImageNegative;

            // create dialog
            MprCurvilinearSliceWindow dlg = new MprCurvilinearSliceWindow(
                _mprSourceDataController.MprImage,
                planarSlice,
                voiLut,
                isNegative,
                _mprSettingsManager);

            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        #endregion


        #region Hot keys

        /// <summary>
        /// Handles the CanExecute event of openCommandBinding object.
        /// </summary>
        private void openCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = openDicomFilesMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of openFromFolderCommandBinding object.
        /// </summary>
        private void openFromFolderCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = openDicomFilesFromFolderMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of saveImageCommandBinding object.
        /// </summary>
        private void saveImageCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = saveImageMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of saveAllImagesCommandBinding object.
        /// </summary>
        private void saveAllImagesCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = saveAllImagesMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of copyImageToClipboardCommandBinding object.
        /// </summary>
        private void copyImageToClipboardCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = copyImageToClipboardMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of saveImageSliceCommandBinding object.
        /// </summary>
        private void saveImageSliceCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = saveImageSliceMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of saveAllImagesSlicesCommandBinding object.
        /// </summary>
        private void saveAllImagesSlicesCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = saveAllImagesSlicesMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of copyImageSliceToClipboardCommandBinding object.
        /// </summary>
        private void copyImageSliceToClipboardCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = copyImageSliceToClipboardMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of exitCommandBinding object.
        /// </summary>
        private void exitCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = exitMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of resetSceneCommandBinding object.
        /// </summary>
        private void resetSceneCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = resetSceneMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of fitSceneCommandBinding object.
        /// </summary>
        private void fitSceneCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = fitSceneMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of negativeImageCommandBinding object.
        /// </summary>
        private void negativeImageCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = negativeImageMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of resetToDefaultWindowLevelCommandBinding object.
        /// </summary>
        private void resetToDefaultWindowLevelCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = resetToDefaultWindowLevelMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of useInterpolationCommandBinding object.
        /// </summary>
        private void useInterpolationCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = useInterpolationMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of showAxisCommandBinding object.
        /// </summary>
        private void showAxisCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = showAxisMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of show3DAxisCommandBinding object.
        /// </summary>
        private void show3DAxisCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = show3DAxisMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of fullScreenCommandBinding object.
        /// </summary>
        private void fullScreenCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = fullScreenMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of topPanelAlwaysVisibleCommandBinding object.
        /// </summary>
        private void topPanelAlwaysVisibleCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = topPanelAlwaysVisibleMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of showTextOverlayCommandBinding object.
        /// </summary>
        private void showTextOverlayCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = showTextOverlayMenuItem.IsEnabled;
        }

        #endregion

        #endregion



        #region Delegates

        /// <summary>
        /// Represents the <see cref="UpdateUI"/> method.
        /// </summary>
        delegate void UpdateUIDelegate();

        #endregion

    }
}
