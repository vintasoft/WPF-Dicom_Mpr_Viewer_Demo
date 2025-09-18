using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Codecs.ImageFiles.Dicom;
using Vintasoft.Imaging.Dicom.Mpr;
using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI;
using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI.VisualTools;
using Vintasoft.Imaging.Dicom.Wpf.UI.VisualTools;
using Vintasoft.Imaging.ImageProcessing;
using Vintasoft.Imaging.UI;
using Vintasoft.Imaging.Wpf;
using Vintasoft.Imaging.Wpf.UI;
using Vintasoft.Imaging.Wpf.UI.VisualTools;
using Vintasoft.Imaging.Wpf.UI.VisualTools.UserInteraction;

using WpfDemosCommonCode;
using WpfDemosCommonCode.Imaging;
using WpfDemosCommonCode.Imaging.Codecs;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A window that allows to view a curvilinear slice, which is constructed from the sagittal, coronal or axial slice.
    /// </summary>
    public partial class MprCurvilinearSliceWindow : Window
    {

        #region Constants

        /// <summary>
        /// The form title format.
        /// </summary>
        const string TITLE_DEFAULT_FORMAT = "Curvlinear Slice From {0}";

        /// <summary>
        /// The text overlay collection owner name.
        /// </summary>
        const string OVERLAY_OWNER_NAME = "Curvlinear Slice From";

        #endregion



        #region Fields

        /// <summary>
        /// The visualization controller for image viewers, which show planar slice and curvilinear slice.
        /// </summary>
        WpfMprVisualizationController _visualizationController;

        /// <summary>
        /// The slice type.
        /// </summary>
        SliceType _sliceType;


        #region Planar slice

        /// <summary>
        /// The DicomMprTool for planar slice.
        /// </summary>
        WpfDicomMprTool _planarSliceDicomMprTool;

        /// <summary>
        /// The current planar slice.
        /// </summary>
        MprPlanarSlice _currentPlanarSlice;

        /// <summary>
        /// The default state of planar slice.
        /// </summary>
        MprSlice _defaultPlanarSlice;

        #endregion


        #region Curvilinear slice

        /// <summary>
        /// The DicomMprTool for curvilinear slice.
        /// </summary>
        WpfDicomMprTool _curvilinearSliceDicomMprTool;

        /// <summary>
        /// The current curvilinear slice.
        /// </summary>
        MprPolylineSlice _currentCurvilinearSlice;

        /// <summary>
        /// The default state of curvilinear slice.
        /// </summary>
        MprSlice _defaultCurvilinearSlice;

        #endregion


        /// <summary>
        /// The default VOI LUT for planar and curvilinear slices.
        /// </summary>
        DicomImageVoiLookupTable _defaultVoiLut;


        /// <summary>
        /// The MPR image tool appearance settings.
        /// </summary>
        MprImageToolAppearanceSettings _imageToolAppearanceSettings;


        /// <summary>
        /// A window with MPR parameters information.
        /// </summary>
        MprParametersViewerWindow _mprParametersWindow;


        /// <summary>
        /// A window state, which was used in non full screen mode.
        /// </summary>
        WindowState _nonFullScreenWindowState;


        #region Hot keys

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
        public static RoutedCommand _fullScreenCommand = new RoutedCommand();
        public static RoutedCommand _topPanelAlwaysVisibleCommand = new RoutedCommand();

        #endregion

        #endregion



        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MprCurvilinearSliceWindow"/> class.
        /// </summary>
        /// <param name="mprImage">The MPR image.</param>
        /// <param name="currentPlanarSlice">The current planar slice.</param>
        /// <param name="voiLut">The default VOI LUT.</param>
        /// <param name="isNegativeImage">Indicates whether the image must be inverted.</param>
        /// <param name="imageToolAppearanceSettings">The MPR image tool appearance settings.</param>
        public MprCurvilinearSliceWindow(
            MprImage mprImage,
            MprPlanarSlice currentPlanarSlice,
            DicomImageVoiLookupTable voiLut,
            bool isNegativeImage,
            MprImageToolAppearanceSettings imageToolAppearanceSettings)
        {
            InitializeComponent();

            dicomMprToolInteractionModeToolBar.SupportedInteractionModes = new WpfDicomMprToolInteractionMode[] {
                WpfDicomMprToolInteractionMode.Browse,
                WpfDicomMprToolInteractionMode.Pan,
                WpfDicomMprToolInteractionMode.Roll,
                WpfDicomMprToolInteractionMode.WindowLevel,
                WpfDicomMprToolInteractionMode.Zoom,
                WpfDicomMprToolInteractionMode.Measure
            };

            _imageToolAppearanceSettings = imageToolAppearanceSettings;

            // create visualization controller
            _visualizationController = new WpfMprVisualizationController(
                mprImage, planarSliceImageViewer, curvilinearSliceImageViewer);

            _imageToolAppearanceSettings.SetMprVisualizationControllerSettings(_visualizationController);

            // save current planar slice
            _currentPlanarSlice = currentPlanarSlice;
            _defaultPlanarSlice = currentPlanarSlice.CreateCopy();

            // save current VOI LUT
            _defaultVoiLut = voiLut;

            // create DicomMprTool
            CreatePlanarSliceDicomMprTool(isNegativeImage);
            CreateCurvilinearSliceDicomMprTool(isNegativeImage);

            dicomMprToolInteractionModeToolBar.DicomMprTools = new WpfDicomMprTool[] {
                _planarSliceDicomMprTool, _curvilinearSliceDicomMprTool };
            view_negativeImageMenuItem.IsChecked = isNegativeImage;
            _planarSliceDicomMprTool.MprImageTool.AllowRotate3D = false;
            _planarSliceDicomMprTool.MprImageTool.ScrollProperties.Anchor = AnchorType.Left;

            // set appearance to DicomMprTool
            imageToolAppearanceSettings.SetMprToolSettings(_planarSliceDicomMprTool.MprImageTool);

            // add slice to the visualization controller
            WpfMprSliceVisualizer visualMprPlanarSlice = _visualizationController.AddSliceVisualization(
                currentPlanarSlice, Colors.Transparent);

            string sliceName = "Unknown";
            // if slice is sagittal
            if (MprPlanarSlice.IsSagittalSlice(currentPlanarSlice))
            {
                sliceName = "Sagittal";
                imageToolAppearanceSettings.SagittalSliceAppearance.SetSettings(visualMprPlanarSlice);
                _sliceType = SliceType.Sagittal;
            }
            // if slice is coronal
            else if (MprPlanarSlice.IsCoronalSlice(currentPlanarSlice))
            {
                sliceName = "Coronal";
                imageToolAppearanceSettings.CoronalSliceAppearance.SetSettings(visualMprPlanarSlice);
                _sliceType = SliceType.Coronal;
            }
            // if slice is axial
            else if (MprPlanarSlice.IsAxialSlice(currentPlanarSlice))
            {
                sliceName = "Axial";
                imageToolAppearanceSettings.AxialSliceAppearance.SetSettings(visualMprPlanarSlice);
                _sliceType = SliceType.Axial;
            }

            // update title of form
            this.Title = string.Format(TITLE_DEFAULT_FORMAT, sliceName);


            planarSliceImageViewer.GotFocus += new RoutedEventHandler(ImageViewer_GotFocus);
            curvilinearSliceImageViewer.GotFocus += new RoutedEventHandler(ImageViewer_GotFocus);

            planarSliceImageViewer.Focus();

            this.Loaded += new RoutedEventHandler(MprCurvilinearSliceWindow_Loaded);
        }

        #endregion



        #region Properties

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
                if (planarSliceImageViewer.IsFocused)
                    return planarSliceImageViewer;

                if (curvilinearSliceImageViewer.IsFocused)
                    return curvilinearSliceImageViewer;

                return null;
            }
        }

        #endregion



        #region Methods

        #region PROTECTED

        /// <summary>
        /// Raises the <see cref="Control.OnPreviewKeyDown"/> event.
        /// </summary>
        /// <param name="e">A <see cref="KeyEventArgs"/> that contains the event data.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_planarSliceDicomMprTool.MprImageTool.IsSliceBuilding)
                {
                    _planarSliceDicomMprTool.MprImageTool.CancelSliceBuilding();
                }
                else if (view_fullScreenMenuItem.IsChecked)
                {
                    view_fullScreenMenuItem.IsChecked = false;
                    ChangeWindowFullScreenMode(view_fullScreenMenuItem.IsChecked);
                }
            }

            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// The form is closed.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _visualizationController.Dispose();

            base.OnClosed(e);
        }

        #endregion


        #region PRIVATE

        #region 'File' menu

        /// <summary>
        /// Saves the screenshot of focused image viewer.
        /// </summary>
        private void file_saveViewerScreenshotMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get image of focused image viewer
            using (VintasoftImage image = FocusedViewer.RenderViewerImage())
            {
                // save image to a file
                SaveImageFileWindow.SaveImageToFile(image, ImagingEncoderFactory.Default);
            }
        }

        /// <summary>
        /// Saves the screenshots of all image viewers.
        /// </summary>
        private void file_saveAllViewersScreenshotsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // create a temporary image collection
            using (ImageCollection images = new ImageCollection())
            {
                // get image of the first viewer to the temporary image collection
                images.Add(planarSliceImageViewer.RenderViewerImage());
                // get image of the second viewer to the temporary image collection
                images.Add(curvilinearSliceImageViewer.RenderViewerImage());

                // save images to a file
                SaveImageFileWindow.SaveImagesToFile(images, ImagingEncoderFactory.Default);

                // clear and dispose images in the temporary image collection
                images.ClearAndDisposeItems();
            }
        }

        /// <summary>
        /// Copies to the clipboard the image of focused image viewer.
        /// </summary>
        private void file_copyImageToClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get image of focused image viewer
            using (VintasoftImage image = FocusedViewer.RenderViewerImage())
            {
                // copy image to the clipboard
                CopyToClipboard(image);
            }
        }

        /// <summary>
        /// Saves the image of the focused MPR slice.
        /// </summary>
        private void file_saveImageSliceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFocusedSliceImage();
        }

        /// <summary>
        /// Saves the images of all MPR slices.
        /// </summary>
        private void file_saveAllImagesSlicesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // create a temporary image collection
            using (ImageCollection images = new ImageCollection())
            {
                // get the slice image from DICOM MPR tool
                VintasoftImage planarSliceImage = _planarSliceDicomMprTool.DicomViewerTool.GetDisplayedImage();
                if (planarSliceImageViewer != null)
                    // add the slice image to the temporary image collection
                    images.Add(planarSliceImage);

                // get the slice image from DICOM MPR tool
                VintasoftImage curvilinearSliceImage = _curvilinearSliceDicomMprTool.DicomViewerTool.GetDisplayedImage();
                if (curvilinearSliceImage != null)
                    // add the slice image to the temporary image collection
                    images.Add(curvilinearSliceImage);

                // save images to a file
                SaveImageFileWindow.SaveImagesToFile(images, ImagingEncoderFactory.Default);

                // clear and dispose images in the temporary image collection
                images.ClearAndDisposeItems();
            }
        }

        /// <summary>
        /// Copies to the clipboard the image of the focused MPR slice.
        /// </summary>
        private void file_copyImageSliceToClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get the DICOM MPR tool, which is associated with focused image viewer
            WpfDicomMprTool dicomMprTool =
                _visualizationController.GetDicomMprToolAssociatedWithImageViewer(FocusedViewer);

            if (dicomMprTool.MprImageTool.FocusedSlice == null)
                return;

            // get the image slice from DICOM MPR tool
            using (VintasoftImage image = dicomMprTool.DicomViewerTool.GetDisplayedImage())
            {
                // copy image to the clipboard
                CopyToClipboard(image);
            }
        }

        /// <summary>
        /// Closes the form.
        /// </summary>
        private void file_exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion


        #region 'View' menu

        /// <summary>
        /// "View" menu is opening.
        /// </summary>
        private void viewMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            // indicates whether interpolation is enabled
            bool isInterpolationEnabled = true;

            if (_planarSliceDicomMprTool.MprImageTool.RenderingInterpolationMode == MprInterpolationMode.NearestNeighbor)
                isInterpolationEnabled = false;
            else
                isInterpolationEnabled = true;

            view_useInterpolationMenuItem.IsChecked = isInterpolationEnabled;
        }

        /// <summary>
        /// Resets the slice location.
        /// </summary>
        private void view_resetSceneMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_defaultPlanarSlice != null)
                _defaultPlanarSlice.CopyTo(_currentPlanarSlice);

            if (_defaultCurvilinearSlice != null)
                _defaultCurvilinearSlice.CopyTo(_currentCurvilinearSlice);

            FitScene();
        }

        /// <summary>
        /// Fits the slice on viewer.
        /// </summary>
        private void view_fitSceneMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FitScene();
        }

        /// <summary>
        /// Synchronizes the VOI LUT of slice.
        /// </summary>
        private void view_synchronizeWindowLevelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_synchronizeWindowLevelMenuItem.IsChecked ^= true;

            // if VOI LUT must be synchronized
            if (view_synchronizeWindowLevelMenuItem.IsChecked)
            {
                if (planarSliceImageViewer.IsFocused)
                {
                    _curvilinearSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut =
                        _planarSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut;
                }
                else
                {
                    _planarSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut =
                        _curvilinearSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut;
                }
            }
        }

        /// <summary>
        /// Inverts the image in viewer.
        /// </summary>
        private void view_negativeImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_negativeImageMenuItem.IsChecked ^= true;

            _planarSliceDicomMprTool.DicomViewerTool.IsImageNegative =
                view_negativeImageMenuItem.IsChecked;
            _curvilinearSliceDicomMprTool.DicomViewerTool.IsImageNegative =
                view_negativeImageMenuItem.IsChecked;
        }

        /// <summary>
        /// Resets the VOI LUT of slices.
        /// </summary>
        private void view_resetWindowLevelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _planarSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut = _defaultVoiLut;
            _curvilinearSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut = _defaultVoiLut;
        }

        /// <summary>
        /// Resets to the default VOI LUT of slices.
        /// </summary>
        private void view_resetToDefaultWindowLevelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _planarSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut =
                _planarSliceDicomMprTool.DicomViewerTool.DefaultDicomImageVoiLut;
            _curvilinearSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut =
                _curvilinearSliceDicomMprTool.DicomViewerTool.DefaultDicomImageVoiLut;
        }

        /// <summary>
        /// Shows the window level.
        /// </summary>
        private void view_showWindowLevelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_showWindowLevelMenuItem.IsChecked ^= true;
            bool value = view_showWindowLevelMenuItem.IsChecked;

            WpfDicomImageVoiLutTextOverlay planarSliceVoiLutTextOverlay =
                _planarSliceDicomMprTool.TextOverlay.Find<WpfDicomImageVoiLutTextOverlay>();
            if (planarSliceVoiLutTextOverlay != null)
                planarSliceVoiLutTextOverlay.IsVisible = value;

            WpfDicomImageVoiLutTextOverlay curvilinearSliceVoiLutTextOverlay =
                _curvilinearSliceDicomMprTool.TextOverlay.Find<WpfDicomImageVoiLutTextOverlay>();
            if (curvilinearSliceVoiLutTextOverlay != null)
                curvilinearSliceVoiLutTextOverlay.IsVisible = value;
        }

        /// <summary>
        /// Shows the axis of the slice.
        /// </summary>
        private void view_showAxisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_showAxisMenuItem.IsChecked ^= true;

            _planarSliceDicomMprTool.MprImageTool.AreAxesVisible =
                view_showAxisMenuItem.IsChecked;
            _curvilinearSliceDicomMprTool.MprImageTool.AreAxesVisible =
                view_showAxisMenuItem.IsChecked;
        }

        /// <summary>
        /// Shows the patient direction.
        /// </summary>
        private void view_showPatientDirectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_showPatientDirectionMenuItem.IsChecked ^= true;

            foreach (WpfTextOverlay textOverlay in _planarSliceDicomMprTool.TextOverlay)
            {
                if (textOverlay is WpfMprSliceOrientationTextOverlay)
                    textOverlay.IsVisible = view_showPatientDirectionMenuItem.IsChecked;
            }

            foreach (WpfTextOverlay textOverlay in _curvilinearSliceDicomMprTool.TextOverlay)
            {
                if (textOverlay is WpfMprSliceOrientationTextOverlay)
                    textOverlay.IsVisible = view_showPatientDirectionMenuItem.IsChecked;
            }
        }

        /// <summary>
        /// Shows a form with MPR parameters information.
        /// </summary>
        private void view_showMPRParametersMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!view_showMPRParametersMenuItem.IsChecked)
            {
                // if MPR parameters form is not created
                if (_mprParametersWindow == null)
                    CreateMprParametersViewerForm();
                // show form
                _mprParametersWindow.Show();
                view_showMPRParametersMenuItem.IsChecked = true;

                // disable all MPR image tools
                _planarSliceDicomMprTool.MprImageTool.Enabled = false;
                _curvilinearSliceDicomMprTool.MprImageTool.Enabled = false;
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
        /// Handles the showSlicePropertiesMenuItem_Click event of view object.
        /// </summary>
        private void view_showSlicePropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get curvlinear slice tool
            WpfDicomMprTool dicomMprToolWithCurvlinearSlice = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(curvilinearSliceImageViewer);
            // get curvlinear slice
            MprCurvilinearSlice curvlinearSlice = (MprCurvilinearSlice)dicomMprToolWithCurvlinearSlice.MprImageTool.FocusedSlice;

            if (curvlinearSlice == null)
                return;

            // get curvlinear slice reference points
            VintasoftPoint3D[] curvlinearSliceReferencePoints = curvlinearSlice.GetReferencePointsInWorldSpace();


            // get planar slice tool
            WpfDicomMprTool dicomMprToolWithPlanarSlice = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(planarSliceImageViewer);
            // get planar slice
            MprPlanarSlice planarSlice = (MprPlanarSlice)dicomMprToolWithPlanarSlice.MprImageTool.FocusedSlice;

            // get transform from planar slice to viewer space
            WpfMprSliceView planarSliceView = dicomMprToolWithPlanarSlice.MprImageTool.FocusedSliceView;
            WpfPointTransform transformFromPlanarSliceToViewerSpace = planarSliceView.GetPointTransform(planarSliceImageViewer, planarSliceImageViewer.Image);


            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Curvlinear slice reference points");


            for (int i = 0; i < curvlinearSliceReferencePoints.Length; i++)
            {
                // get point in world space
                VintasoftPoint3D pointInWorldSpace = curvlinearSliceReferencePoints[i];
                // calculate point in planar slice space
                Vintasoft.Primitives.VintasoftPoint pointInPlanarSliceSpace = planarSlice.GetPointProjectionOnSlice(pointInWorldSpace);
                // calculate point in viewer space
                Point pointInViewerSpace = transformFromPlanarSliceToViewerSpace.TransformPoint(VintasoftWpfConverter.Convert(pointInPlanarSliceSpace));


                // add points information

                builder.AppendLine(string.Format("Point{0}", i + 1));
                builder.AppendLine(string.Format("World space: {0:F1} {1:F1} {2:F1}", pointInWorldSpace.X, pointInWorldSpace.Y, pointInWorldSpace.Z));
                builder.AppendLine(string.Format("Planar slice: {0:F1} {1:F1}", pointInPlanarSliceSpace.X, pointInPlanarSliceSpace.Y));
                builder.AppendLine(string.Format("Viewer space: {0:F1} {1:f1}", pointInViewerSpace.X, pointInViewerSpace.Y));

                if (i != curvlinearSliceReferencePoints.Length - 1)
                    builder.AppendLine();
            }

            // show points information
            MessageBox.Show(builder.ToString());
        }

        /// <summary>
        /// Enables or disables interpolation in slice rendering.
        /// </summary>
        private void view_useInterpolationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // indicates whether interpolation is enabled
            bool isInterpolationEnabled = view_useInterpolationMenuItem.IsChecked;

            // if interpolation is enabled
            if (isInterpolationEnabled)
            {
                // disable interpolation
                _planarSliceDicomMprTool.MprImageTool.RenderingInterpolationMode = MprInterpolationMode.NearestNeighbor;
                _curvilinearSliceDicomMprTool.MprImageTool.RenderingInterpolationMode = MprInterpolationMode.NearestNeighbor;
            }
            // if interpolation is disabled
            else
            {
                // enable interpolation
                _planarSliceDicomMprTool.MprImageTool.RenderingInterpolationMode = MprInterpolationMode.Linear;
                _curvilinearSliceDicomMprTool.MprImageTool.RenderingInterpolationMode = MprInterpolationMode.Linear;
            }

            view_useInterpolationMenuItem.IsChecked = !isInterpolationEnabled;
        }

        /// <summary>
        /// Enables or disables the full screen mode of form.
        /// </summary>
        private void view_fullScreenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_fullScreenMenuItem.IsChecked = !view_fullScreenMenuItem.IsChecked;
            ChangeWindowFullScreenMode(view_fullScreenMenuItem.IsChecked);
        }

        /// <summary>
        /// Shows or hides the top panel.
        /// </summary>
        private void view_topPanelAlwaysVisibleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_topPanelAlwaysVisibleMenuItem.IsChecked = !view_topPanelAlwaysVisibleMenuItem.IsChecked;

            if (view_fullScreenMenuItem.IsChecked)
            {
                if (view_topPanelAlwaysVisibleMenuItem.IsChecked)
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
        /// Shows a dialog that allows to change the text overlay, which must be shown on image viewer.
        /// </summary>
        private void view_textOverlaySettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DicomOverlaySettingEditorWindow dialog = new DicomOverlaySettingEditorWindow(OVERLAY_OWNER_NAME, _planarSliceDicomMprTool);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Owner = this;
            // show dialog
            dialog.ShowDialog();

            DicomOverlaySettingEditorWindow.SetTextOverlay(OVERLAY_OWNER_NAME, _planarSliceDicomMprTool);
            DicomOverlaySettingEditorWindow.SetTextOverlay(OVERLAY_OWNER_NAME, _curvilinearSliceDicomMprTool);
        }

        /// <summary>
        /// Opens an MPR settings form.
        /// </summary>
        private void view_settingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SliceType selectedSliceType = _sliceType;

            if (curvilinearSliceImageViewer.IsFocused)
                selectedSliceType = SliceType.Curvilinear;

            MprImageToolAppearanceSettingsWindow dialog = new MprImageToolAppearanceSettingsWindow(
                 _imageToolAppearanceSettings, selectedSliceType,
                 _sliceType, SliceType.Curvilinear);

            dialog.Owner = this;
            dialog.ShowDialog();

            UpdateDicomMprSettings();
        }

        #endregion


        #region 'Slice' menu

        /// <summary>
        /// Starts the building of curvilinear slice.
        /// </summary>
        private void slice_buildMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartBuildCurvilinearSlice();
        }

        #endregion


        #region HotKeys

        /// <summary>
        /// Handles the CanExecute event of saveImageCommandBinding object.
        /// </summary>
        private void saveImageCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = file_saveImageMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of saveAllImagesCommandBinding object.
        /// </summary>
        private void saveAllImagesCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = file_saveAllImagesMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of copyImageToClipboardCommandBinding object.
        /// </summary>
        private void copyImageToClipboardCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = file_copyImageToClipboardMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of saveImageSliceCommandBinding object.
        /// </summary>
        private void saveImageSliceCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = file_saveImageSliceMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of saveAllImagesSlicesCommandBinding object.
        /// </summary>
        private void saveAllImagesSlicesCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = file_saveAllImagesSlicesMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of copyImageSliceToClipboardCommandBinding object.
        /// </summary>
        private void copyImageSliceToClipboardCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = file_copyImageSliceToClipboardMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of exitCommandBinding object.
        /// </summary>
        private void exitCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = file_exitMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of resetSceneCommandBinding object.
        /// </summary>
        private void resetSceneCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = view_resetSceneMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of fitSceneCommandBinding object.
        /// </summary>
        private void fitSceneCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = view_fitSceneMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of negativeImageCommandBinding object.
        /// </summary>
        private void negativeImageCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = view_negativeImageMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of resetToDefaultWindowLevelCommandBinding object.
        /// </summary>
        private void resetToDefaultWindowLevelCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = view_resetToDefaultWindowLevelMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of useInterpolationCommandBinding object.
        /// </summary>
        private void useInterpolationCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = view_useInterpolationMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of fullScreenCommandBinding object.
        /// </summary>
        private void fullScreenCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = view_fullScreenMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of topPanelAlwaysVisibleCommandBinding object.
        /// </summary>
        private void topPanelAlwaysVisibleCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = view_topPanelAlwaysVisibleMenuItem.IsEnabled;
        }

        #endregion


        #region DICOM MPR settings

        /// <summary>
        /// Updates DICOM MPR settings.
        /// </summary>
        private void UpdateDicomMprSettings()
        {
            // update MPR image
            _imageToolAppearanceSettings.SetMprImageSettings(_visualizationController.MprImage);
            // update controller
            _imageToolAppearanceSettings.SetMprVisualizationControllerSettings(_visualizationController);

            // update slices
            WpfMprSliceVisualizer visualMprSlice = _visualizationController.GetVisualMprSliceAssociatedWithMprSlice(
                _currentPlanarSlice);
            _imageToolAppearanceSettings.SetSliceSettings(_sliceType, visualMprSlice);
            // if curvilinear slice is created
            if (_currentCurvilinearSlice != null)
                _imageToolAppearanceSettings.SetCurvilinearSliceSettings(_visualizationController.GetVisualMprSliceAssociatedWithMprSlice(_currentCurvilinearSlice));

            // update default slices
            _defaultPlanarSlice.Thickness = _currentPlanarSlice.Thickness;
            _defaultPlanarSlice.RenderingMode = _currentPlanarSlice.RenderingMode;
            if (_currentCurvilinearSlice != null && _defaultCurvilinearSlice != null)
            {
                _defaultCurvilinearSlice.Thickness = _currentCurvilinearSlice.Thickness;
                _defaultCurvilinearSlice.RenderingMode = _currentCurvilinearSlice.RenderingMode;
            }

            // update MPR tools
            _imageToolAppearanceSettings.SetMprToolSettings(_planarSliceDicomMprTool.MprImageTool);
            _imageToolAppearanceSettings.SetMprToolSettings(_curvilinearSliceDicomMprTool.MprImageTool);
        }

        #endregion


        #region DICOM viewer tool

        /// <summary>
        /// Updates the VOI LUT in DICOM viewer tool.
        /// </summary>
        private void DicomViewerTool_DicomImageVoiLutChanged(object sender, WpfVoiLutChangedEventArgs e)
        {
            // if VOI LUT can be synchronized
            if (view_synchronizeWindowLevelMenuItem.IsChecked)
            {
                DicomImageVoiLookupTable newVoiLut = new DicomImageVoiLookupTable(e.WindowCenter, e.WindowWidth);

                if (sender == _curvilinearSliceDicomMprTool)
                {
                    _planarSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut = newVoiLut;
                }
                else
                {
                    _curvilinearSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut = newVoiLut;
                }
            }
        }

        #endregion


        #region DICOM MPR image tool

        /// <summary>
        /// The slice building is started.
        /// </summary>
        private void DicomMprImageTool_SliceBuildingStarted(object sender, WpfMprSliceViewEventArgs e)
        {
            // remove information about current curvilinear slice
            _currentCurvilinearSlice = null;
            _defaultCurvilinearSlice = null;
        }

        /// <summary>
        /// The slice building is finished.
        /// </summary>
        private void DicomMprImageTool_SliceBuildingFinished(object sender, WpfMprSliceViewEventArgs e)
        {
            // update information about current curvilinear slice

            MprPolylineSlice curvlinearSlice = e.SliceView.MprSliceVisualizer.Slice as MprPolylineSlice;

            if (curvlinearSlice == null)
                throw new ArgumentNullException();

            _currentCurvilinearSlice = curvlinearSlice;
            _defaultCurvilinearSlice = curvlinearSlice.CreateCopy();
        }

        /// <summary>
        /// Mouse is down.
        /// </summary>
        private void DicomMprImageTool_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WpfImageViewer viewer = ((WpfVisualTool)sender).ImageViewer;
            // if image viewer is focused AND
            // mouse left button is clicked AND
            // MPR parameters form is shown
            if (viewer.IsFocused &&
                e.ChangedButton == MouseButton.Left &&
                _mprParametersWindow != null)
            {
                // update values in MPR parameters viewer form
                Point location = e.GetPosition(viewer);
                UpdateMprParametersForm(viewer, location.X, location.Y);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Mouse is moved.
        /// </summary>
        private void DicomMprImageTool_MouseMove(object sender, MouseEventArgs e)
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
                UpdateMprParametersForm(viewer, location.X, location.Y);
                e.Handled = true;
            }
        }

        #endregion


        #region MPR slice building

        /// <summary>
        /// Starts the building of curvilinear slice.
        /// </summary>
        private void StartBuildCurvilinearSlice()
        {
            // if previous curvlinear slice must be removed
            if (_curvilinearSliceDicomMprTool.MprImageTool.FocusedSlice != null)
            {
                // remove previous curvlinear slice
                _visualizationController.RemoveSliceVisualization(
                    _curvilinearSliceDicomMprTool.MprImageTool.FocusedSlice);
            }

            // create curvilinear slice
            MprCurvilinearSlice curvilinearSlice =
                _visualizationController.MprImage.CreateCurvilinearSlice(_currentPlanarSlice, null);
            // create visualizer for curvilinear slice
            WpfMprSliceVisualizer curvilinearSliceVisualizer = new WpfMprSliceVisualizer(curvilinearSlice, Colors.White);
            // set the visual settings of curvilinear slice
            _imageToolAppearanceSettings.SetCurvilinearSliceSettings(curvilinearSliceVisualizer);

            // start the building of curvilinear slice
            WpfMprSliceView curvilinearSliceView = _planarSliceDicomMprTool.MprImageTool.AddAndBuildSlice(curvilinearSliceVisualizer);

            // show slice in the right viewer
            _visualizationController.ShowSliceInViewer(curvilinearSliceImageViewer, curvilinearSlice);

            // if image viewer zoom must be changed automatically when slice is building
            if (view_automaticallyChangeZoomWhenSliceBuildingMenuItem.IsChecked)
            {
                // subscribe to the interaction events of slice builder

                curvilinearSliceView.Builder.Interaction +=
                    new EventHandler<WpfInteractionEventArgs>(CurvilinearSlice_Builder_Interaction);
                curvilinearSliceView.Builder.InteractionFinished +=
                    new EventHandler<WpfInteractionEventArgs>(CurvilinearSlice_Builder_InteractionFinished);
            }
        }

        /// <summary>
        /// User is interacted with the curvilinear slice.
        /// </summary>
        private void CurvilinearSlice_Builder_Interaction(object sender, WpfInteractionEventArgs e)
        {
            // calculate the image viewer zoom for viewing
            double viewerZoom = WpfMprVisualizationController.CalculateZoomForViewSliceOnBestFit(
                _curvilinearSliceDicomMprTool.MprImageTool.MprImage,
                _curvilinearSliceDicomMprTool.MprImageTool.FocusedSlice,
                curvilinearSliceImageViewer);

            // change the zoom in image viewer

            curvilinearSliceImageViewer.SizeMode = ImageSizeMode.Zoom;
            curvilinearSliceImageViewer.Zoom = viewerZoom;
        }

        /// <summary>
        /// User is finished interacting with the curvilinear slice.
        /// </summary>
        private void CurvilinearSlice_Builder_InteractionFinished(object sender, WpfInteractionEventArgs e)
        {
            // unsubscribe from the slice builder events

            IWpfInteractionController builder = (IWpfInteractionController)sender;
            builder.Interaction -= CurvilinearSlice_Builder_Interaction;
            builder.InteractionFinished -= CurvilinearSlice_Builder_InteractionFinished;
        }

        #endregion


        #region Image viewer toolbar

        /// <summary>
        /// Saves the image of the focused MPR slice.
        /// </summary>
        private void file_saveImageSliceMenuItem_Click(object sender, EventArgs e)
        {
            SaveFocusedSliceImage();
        }

        /// <summary>
        /// Updates the current image viewer in tool strip.
        /// </summary>
        private void ImageViewer_GotFocus(object sender, RoutedEventArgs e)
        {
            // if focused viewer is changed
            if (imageViewerToolBar.ImageViewer != sender)
            {
                imageViewerToolBar.ImageViewer = (WpfImageViewer)sender;

                WpfDicomMprToolInteractionMode[] disabledInteractionModes = null;

                // is curvilinear viewer is focused
                if (curvilinearSliceImageViewer.IsFocused)
                    // disable the Roll interaction mode
                    disabledInteractionModes = new WpfDicomMprToolInteractionMode[] { WpfDicomMprToolInteractionMode.Roll };

                // update disabled interaction modes
                dicomMprToolInteractionModeToolBar.DisabledInteractionModes = disabledInteractionModes;
            }
        }

        #endregion


        #region DICOM MPR parameters form

        /// <summary>
        /// Creates a form for viewing parameters of 3D MPR (multiplanar reconstruction).
        /// </summary>
        private void CreateMprParametersViewerForm()
        {
            // get DICOM MPR tool, which is associated with focused image viewer
            WpfDicomMprTool dicomMprTool = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(FocusedViewer);
            // get transform from the image space to the slice space
            WpfPointTransform transformFromImageToSlice = dicomMprTool.MprImageTool.FocusedSliceView.GetPointTransform(FocusedViewer, FocusedViewer.Image);
            // inverse the transform, i.e. get transform from the slice space to the image space
            transformFromImageToSlice = transformFromImageToSlice.GetInverseTransform();

            // create a form
            _mprParametersWindow = new MprParametersViewerWindow(
                new Point(0, 0),
                transformFromImageToSlice,
                dicomMprTool.DicomViewerTool.DisplayedImage,
                dicomMprTool.MprImageTool.MprImage,
                dicomMprTool.MprImageTool.FocusedSlice);
            _mprParametersWindow.Owner = this;
            _mprParametersWindow.Closing += new CancelEventHandler(MprParametersForm_Closing);
        }

        /// <summary>
        /// Updates values of <see cref="MprParametersViewerForm"/>.
        /// </summary>
        /// <param name="viewer">The focused image viewer.</param>
        /// <param name="mouseLocationX">The mouse X coordinate.</param>
        /// <param name="mouseLocationY">The mouse Y coordinate.</param>
        private void UpdateMprParametersForm(WpfImageViewer viewer, double mouseLocationX, double mouseLocationY)
        {
            // get mouse location on image
            Point location = viewer.TranslatePoint(new Point(0, 0), this);
            double xCoordinate = mouseLocationX - location.X;
            double yCoordinate = mouseLocationY - location.X;
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
        /// The window with MPR parameters information is closing.
        /// </summary>
        private void MprParametersForm_Closing(object sender, CancelEventArgs e)
        {
            _mprParametersWindow.Closing -= MprParametersForm_Closing;
            _mprParametersWindow = null;
            view_showMPRParametersMenuItem.IsChecked = false;

            _planarSliceDicomMprTool.MprImageTool.Enabled = true;
            _curvilinearSliceDicomMprTool.MprImageTool.Enabled = true;
        }

        #endregion


        #region Image saving

        /// <summary>
        /// Saves the image of the focused MPR slice.
        /// </summary>
        private void SaveFocusedSliceImage()
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

        #endregion


        /// <summary>
        /// Scales the slice proportion to the viewer.
        /// </summary>
        private void FitScene()
        {
            _planarSliceDicomMprTool.MprImageTool.FitScene();
            _curvilinearSliceDicomMprTool.MprImageTool.FitScene();
        }

        /// <summary>
        /// Changes window full screen mode.
        /// </summary>
        /// <param name="fullScreenMode">Determines whether full screen mode is on.</param>
        private void ChangeWindowFullScreenMode(bool fullScreenMode)
        {
            if (fullScreenMode)
            {
                // if the top panel must be hided
                if (!view_topPanelAlwaysVisibleMenuItem.IsChecked)
                {
                    topPanel.Visibility = Visibility.Collapsed;
                }

                // update the form settings
                _nonFullScreenWindowState = WindowState;

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
                if (WindowState != _nonFullScreenWindowState)
                    WindowState = _nonFullScreenWindowState;
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

        /// <summary>
        /// Creates the <see cref="WpfDicomMprTool"/> for the planar slice.
        /// </summary>
        /// <param name="isNegativeImage">The value indicating whether the image must be inverted.</param>
        private void CreatePlanarSliceDicomMprTool(bool isNegativeImage)
        {
            _planarSliceDicomMprTool = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(
                planarSliceImageViewer);
            _planarSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut = _defaultVoiLut;
            _planarSliceDicomMprTool.DicomViewerTool.IsImageNegative = isNegativeImage;
            _planarSliceDicomMprTool.DicomViewerTool.DicomImageVoiLutChanged +=
                new EventHandler<WpfVoiLutChangedEventArgs>(DicomViewerTool_DicomImageVoiLutChanged);
            _planarSliceDicomMprTool.MouseMove += new MouseEventHandler(DicomMprImageTool_MouseMove);
            _planarSliceDicomMprTool.MouseDown += new MouseButtonEventHandler(DicomMprImageTool_MouseDown);
            _planarSliceDicomMprTool.MprImageTool.SliceBuildingStarted +=
                new EventHandler<WpfMprSliceViewEventArgs>(DicomMprImageTool_SliceBuildingStarted);
            _planarSliceDicomMprTool.MprImageTool.SliceBuildingFinished +=
                new EventHandler<WpfMprSliceViewEventArgs>(DicomMprImageTool_SliceBuildingFinished);

            _planarSliceDicomMprTool.TextOverlay.Add(new WpfDicomMprFillDataProgressTextOverlay());
        }

        /// <summary>
        /// Creates the <see cref="WpfDicomMprTool"/> for the curvilinear slice.
        /// </summary>
        /// <param name="isNegativeImage">The value indicating whether the image must be inverted.</param>
        private void CreateCurvilinearSliceDicomMprTool(bool isNegativeImage)
        {
            _curvilinearSliceDicomMprTool = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(
                curvilinearSliceImageViewer);
            _curvilinearSliceDicomMprTool.DicomViewerTool.DicomImageVoiLut = _defaultVoiLut;
            _curvilinearSliceDicomMprTool.DicomViewerTool.IsImageNegative = isNegativeImage;
            _curvilinearSliceDicomMprTool.DicomViewerTool.DicomImageVoiLutChanged +=
                new EventHandler<WpfVoiLutChangedEventArgs>(DicomViewerTool_DicomImageVoiLutChanged);
            _curvilinearSliceDicomMprTool.MouseMove += new MouseEventHandler(DicomMprImageTool_MouseMove);
            _curvilinearSliceDicomMprTool.MouseDown += new MouseButtonEventHandler(DicomMprImageTool_MouseDown);
            _curvilinearSliceDicomMprTool.TextOverlay.Add(new WpfDicomMprFillDataProgressTextOverlay());
        }

        /// <summary>
        /// The window is loaded.
        /// </summary>
        private void MprCurvilinearSliceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // show slice in viewer
            _visualizationController.ShowSliceInViewer(planarSliceImageViewer, _currentPlanarSlice);

            StartBuildCurvilinearSlice();
        }

        #endregion

        #endregion

    }
}
