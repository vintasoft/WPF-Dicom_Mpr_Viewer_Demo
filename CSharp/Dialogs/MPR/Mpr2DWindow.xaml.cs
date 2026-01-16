using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Codecs.Encoders;
using Vintasoft.Imaging.Codecs.ImageFiles.Dicom;
using Vintasoft.Imaging.Dicom.Mpr;
using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI;
using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI.VisualTools;
using Vintasoft.Imaging.Dicom.Wpf.UI.VisualTools;
using Vintasoft.Imaging.ImageProcessing;
using Vintasoft.Imaging.ImageProcessing.Color;
using Vintasoft.Imaging.ImageProcessing.Filters;
using Vintasoft.Imaging.UI;
using Vintasoft.Imaging.Wpf.UI.VisualTools;

using WpfDemosCommonCode;
using WpfDemosCommonCode.Imaging;
using WpfDemosCommonCode.Imaging.Codecs;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A window that allows to view 2D MPR (multiplanar reconstruction): the sagittal, coronal or axial slices.
    /// </summary>
    public partial class Mpr2DWindow : Window
    {

        #region Constants

        /// <summary>
        /// The text overlay collection owner name.
        /// </summary>
        const string OVERLAY_OWNER_NAME = "2D MPR";

        #endregion



        #region Fields

        /// <summary>
        /// The visualization controller.
        /// </summary>
        WpfMprVisualizationController _visualizationController;

        /// <summary>
        /// The current slice.
        /// </summary>
        MprPlanarSlice _currentSlice;

        /// <summary>
        /// The DicomMprTool.
        /// </summary>
        WpfDicomMprTool _dicomMprTool;

        /// <summary>
        /// Determines that form is initialized.
        /// </summary>
        bool _isInitialized = false;

        /// <summary>
        /// The default value VOI LUT of slice.
        /// </summary>
        DicomImageVoiLookupTable _defaultVoiLut;

        /// <summary>
        /// The default location of slice.
        /// </summary>
        VintasoftPoint3D _defaultLocation;

        /// <summary>
        /// The previous window state value.
        /// </summary>
        WindowState _previousWindowState;

        /// <summary>
        /// The MPR settings manager.
        /// </summary>
        MprImageToolAppearanceSettings _mprSettingsManager;

        /// <summary>
        /// The slice type.
        /// </summary>
        SliceType _sliceType;

        /// <summary>
        /// The processing commands, which can be applied to an image region of DICOM MPR viewer.
        /// </summary>
        ProcessingCommandBase[] _processingCommands = new ProcessingCommandBase[]
        {
            null,
            new InvertCommand(),
            new BlurCommand(7),
            new SharpenCommand(),
        };


        #region Animation

        /// <summary>
        /// A value indicating whether the animation is cycled.
        /// </summary>
        bool _isAnimationCycled = true;

        /// <summary>
        /// Animation delay in milliseconds.
        /// </summary>
        int _animationDelay = 100;

        /// <summary>
        /// Animation thread.
        /// </summary>
        Thread _animationThread = null;

        /// <summary>
        /// Index of current animated frame.
        /// </summary>
        int _currentAnimatedFrameIndex = 0;

        /// <summary>
        /// A value indicating whether the focused index is changing.
        /// </summary>
        bool _isPageChanging = false;

        /// <summary>
        /// A value indicating whether the window is closed.
        /// </summary>
        bool _isWindowClosed = false;

        #endregion


        #region Hot keys

        public static RoutedCommand _saveImageCommand = new RoutedCommand();
        public static RoutedCommand _saveImageSliceCommand = new RoutedCommand();
        public static RoutedCommand _exitCommand = new RoutedCommand();

        public static RoutedCommand _resetSceneCommand = new RoutedCommand();
        public static RoutedCommand _fitSceneCommand = new RoutedCommand();
        public static RoutedCommand _negativeImageCommand = new RoutedCommand();
        public static RoutedCommand _resetToDefaultWindowLevelCommand = new RoutedCommand();
        public static RoutedCommand _useInterpolationCommand = new RoutedCommand();
        public static RoutedCommand _fullScreenCommand = new RoutedCommand();
        public static RoutedCommand _topPanelAlwaysVisibleCommand = new RoutedCommand();
        public static RoutedCommand _showTextOverlayCommand = new RoutedCommand();

        #endregion

        #endregion



        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Mpr2DWindow"/> class.
        /// </summary>
        /// <param name="mprImage">The MPR cube.</param>
        /// <param name="currentSlice">The current slice.</param>
        /// <param name="voiLut">The current VOI LUT of slice.</param>
        /// <param name="isNegativeImage">Indicates that the image must be inverted.</param>
        /// <param name="settingsManager">The settings manager.</param>
        public Mpr2DWindow(
            MprImage mprImage,
            MprPlanarSlice currentSlice,
            DicomImageVoiLookupTable voiLut,
            bool isNegativeImage,
            MprImageToolAppearanceSettings settingsManager)
        {
            InitializeComponent();

            dicomMprToolInteractionModeToolBar.SupportedInteractionModes = new WpfDicomMprToolInteractionMode[] {
                WpfDicomMprToolInteractionMode.Browse,
                WpfDicomMprToolInteractionMode.Pan,
                WpfDicomMprToolInteractionMode.WindowLevel,
                WpfDicomMprToolInteractionMode.Zoom,
                WpfDicomMprToolInteractionMode.Measure,
                WpfDicomMprToolInteractionMode.ViewProcessing
            };

            _mprSettingsManager = settingsManager;

            // create visualization controller
            _visualizationController = new WpfMprVisualizationController(mprImage, imageViewer1);

            settingsManager.SetMprVisualizationControllerSettings(_visualizationController);

            // save current slice
            _currentSlice = currentSlice;

            _sliceType = GetSliceType(currentSlice);

            // save current location
            _defaultLocation = currentSlice.Location;
            // save current VOI LUT
            _defaultVoiLut = voiLut;

            // create DicomMprTool
            _dicomMprTool = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(imageViewer1);
            dicomMprToolInteractionModeToolBar.DicomMprTools = new WpfDicomMprTool[] { _dicomMprTool };
            _dicomMprTool.MprImageTool.AllowRotate3D = false;
            _dicomMprTool.DicomViewerTool.DicomImageVoiLut = _defaultVoiLut;
            _dicomMprTool.DicomViewerTool.IsImageNegative = isNegativeImage;
            view_negativeImageMenuItem.IsChecked = isNegativeImage;

            // init the text overlays on image viewer
            InitTextOverlaysOnViewer();

            // set appearance to DicomMprTool
            settingsManager.SetMprToolSettings(_dicomMprTool.MprImageTool);
            // hide the color mark
            _dicomMprTool.MprImageTool.IsFocusedSliceColorMarkVisible = false;

            // add slice to the visualization controller
            WpfMprSliceVisualizer visualMprSlice = _visualizationController.AddSliceVisualization(
                currentSlice, Colors.Transparent);

            settingsManager.SetSliceSettings(_sliceType, visualMprSlice);

            // set scroll bar and tool strip settings

            imageViewerToolBar.UseImageViewerImages = false;
            imageViewerToolBar.PageCount = GetPerpendicularAxisLengthInPx(currentSlice);
            imageViewerToolBar.SelectedPageIndex = GetPerpendicularAxisPositionPx(currentSlice);
            imageViewerToolBar.ImageViewer = imageViewer1;

            currentSlice.PropertyChanged += new EventHandler<ObjectPropertyChangedEventArgs>(currentSlice_PropertyChanged);

            this.Loaded += new RoutedEventHandler(Mpr2DWindow_Loaded);

            _isWindowClosed = false;

            foreach (ProcessingCommandBase processingCommand in _processingCommands)
            {
                string processingCommandName = "None";

                if (processingCommand != null)
                    processingCommandName = processingCommand.Name;

                processingComboBox.Items.Add(processingCommandName);
            }
            processingComboBox.SelectedIndex = 0;

            _isInitialized = true;

            imageViewer1.Focus();
        }

        #endregion



        #region Properties

        bool _isAnimationStarted = false;
        /// <summary>
        /// Gets or sets a value indicating whether the animation is started.
        /// </summary>
        bool IsAnimationStarted
        {
            get
            {
                return _isAnimationStarted;
            }
            set
            {
                if (IsAnimationStarted == value)
                    return;

                if (value)
                    StartAnimation();
                else
                    StopAnimation();

                imageViewerToolBar.IsNavigationEnabled = !value;
            }
        }

        #endregion



        #region Methods

        #region PROTECTED

        /// <summary>
        /// Raises the <see cref="Control.OnPreviewKeyDown"/> event.
        /// </summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && view_fullScreenMenuItem.IsChecked)
            {
                view_fullScreenMenuItem.IsChecked = false;
                ChangeWindowFullScreenMode(view_fullScreenMenuItem.IsChecked);
            }

            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// The window is closed.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _isWindowClosed = true;

            _currentSlice.PropertyChanged -= new EventHandler<ObjectPropertyChangedEventArgs>(currentSlice_PropertyChanged);

            _visualizationController.Dispose();
            _visualizationController = null;

            base.OnClosed(e);
        }

        #endregion


        #region PRIVATE

        #region Init

        /// <summary>
        /// Inits the text overlays on image viewer.
        /// </summary>
        private void InitTextOverlaysOnViewer()
        {
            // add a text overlay, which shows the loading progress of slices, at the left-top corner of image viewer

            WpfDicomMprFillDataProgressTextOverlay loadingProgressOverlay = new WpfDicomMprFillDataProgressTextOverlay();
            _dicomMprTool.TextOverlay.Add(loadingProgressOverlay);


            // add a text overlay, which shows slice thickness and location, at the left-bottom corner of image viewer

            WpfTextOverlayGroup sliceInfoGroup = new WpfTextOverlayGroup(
                  AnchorType.Bottom | AnchorType.Left,
                  new WpfMprPlanarSliceThicknessTextOverlay(),
                  new WpfMprPlanarSliceLocationTextOverlay());
            sliceInfoGroup.TextColor = sliceInfoGroup.Items[0].TextColor;
            sliceInfoGroup.TextFont = sliceInfoGroup.Items[0].TextFont;
            sliceInfoGroup.TextFontSize = sliceInfoGroup.Items[0].TextFontSize;
            _dicomMprTool.TextOverlay.Add(sliceInfoGroup);


            // add a text overlay, which shows content date and time, at the right-bottom corner of image viewer

            WpfTextOverlayGroup contentInfoGroup = new WpfTextOverlayGroup(
                AnchorType.Bottom | AnchorType.Right,
                new WpfStandardDicomDataElementTextOverlay(DicomDataElementId.ContentDate),
                new WpfStandardDicomDataElementTextOverlay(DicomDataElementId.ContentTime));
            contentInfoGroup.TextColor = contentInfoGroup.Items[0].TextColor;
            contentInfoGroup.TextFont = contentInfoGroup.Items[0].TextFont;
            contentInfoGroup.TextFontSize = contentInfoGroup.Items[0].TextFontSize;
            _dicomMprTool.TextOverlay.Add(contentInfoGroup);
        }

        #endregion


        #region 'File' menu

        /// <summary>
        /// Saves the screenshot of image viewer.
        /// </summary>
        private void file_saveViewerScreenshotMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get image of image viewer
            using (VintasoftImage image = imageViewer1.RenderViewerImage())
            {
                // save image to a file
                SaveImageFileWindow.SaveImageToFile(image, ImagingEncoderFactory.Default);
            }
        }

        /// <summary>
        /// Saves the image of the MPR slice.
        /// </summary>
        private void file_saveImageSliceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveSliceImage();
        }

        /// <summary>
        /// Saves the image slices to a TIFF file.
        /// </summary>
        private void file_saveSlicesToMultipageTiffMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // create an image slices encoding dialog
            MprImageSlicesEncodingPropertiesWindow encodingPropertiesForm =
                new MprImageSlicesEncodingPropertiesWindow();
            encodingPropertiesForm.Owner = this;

            if (encodingPropertiesForm.ShowDialog() == true)
            {
                // create a file save dialog
                SaveFileDialog fileSaveDialog = new SaveFileDialog();
                fileSaveDialog.Filter = "Tiff files|*.tiff;*.tif";

                if (fileSaveDialog.ShowDialog() == true)
                {
                    // create images
                    ImageCollection images = CreateMprImageSlices(
                        encodingPropertiesForm.UseGray16MprImages,
                        encodingPropertiesForm.ApplyVoiLutToMprImageSlices);
                    try
                    {
                        // create TIFF encoder
                        using (TiffEncoder tiffEncoder = new TiffEncoder(true))
                        {
                            // subscribe to the image collection saving events
                            images.ImageCollectionSavingProgress +=
                                new EventHandler<ProgressEventArgs>(images_ImageCollectionSavingProgress);
                            images.ImageCollectionSaved += new EventHandler(images_ImageCollectionSaved);

                            // initialize progress bar and description label
                            progressBar.Value = 0;
                            progressBar.Visibility = Visibility.Visible;
                            statusLabel.Content = string.Format("Save to '{0}'...",
                                System.IO.Path.GetFileName(fileSaveDialog.FileName));
                            statusLabel.Visibility = Visibility.Visible;

                            // set TIFF encoder settings
                            tiffEncoder.Settings = encodingPropertiesForm.TiffEncoderSettings;

                            // save images to the specified file
                            images.SaveSync(fileSaveDialog.FileName, tiffEncoder);
                        }
                    }
                    finally
                    {
                        images.ClearAndDisposeItems();
                    }
                }
            }
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        private void file_exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion


        #region 'View' menu

        /// <summary>
        /// "View" menu is opened.
        /// </summary>
        private void viewMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            // indicates whether interpolation is enabled
            bool isInterpolationEnabled = true;

            if (_dicomMprTool.MprImageTool.RenderingInterpolationMode == MprInterpolationMode.NearestNeighbor)
                isInterpolationEnabled = false;
            else
                isInterpolationEnabled = true;

            // set the "Use Interpolation" item
            view_useInterpolationMenuItem.IsChecked = isInterpolationEnabled;
        }

        /// <summary>
        /// Resets the slice location.
        /// </summary>
        private void view_resetSceneMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FitScene();

            _currentSlice.Location = _defaultLocation;
        }

        /// <summary>
        /// Fits the slice on viewer.
        /// </summary>
        private void view_fitSceneMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FitScene();
        }

        //// <summary>
        /// Inverts the image in viewer.
        /// </summary>
        private void view_negativeImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_negativeImageMenuItem.IsChecked ^= true;

            _dicomMprTool.DicomViewerTool.IsImageNegative = view_negativeImageMenuItem.IsChecked;
        }

        /// <summary>
        /// Resets the VOI LUT of slice.
        /// </summary>
        private void view_resetWindowLevelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _dicomMprTool.DicomViewerTool.DicomImageVoiLut = _defaultVoiLut;
        }

        /// <summary>
        /// Resets to the default VOI LUT of slice.
        /// </summary>
        private void view_resetToDefaultWindowLevelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _dicomMprTool.DicomViewerTool.DicomImageVoiLut = _dicomMprTool.DicomViewerTool.DefaultDicomImageVoiLut;
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
                _dicomMprTool.MprImageTool.RenderingInterpolationMode = MprInterpolationMode.NearestNeighbor;
            }
            // if interpolation is disabled
            else
            {
                // enable interpolation
                _dicomMprTool.MprImageTool.RenderingInterpolationMode = MprInterpolationMode.Linear;
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
        /// Shows the dialog that allows to change the text overlay, which must be shown on image viewer.
        /// </summary>
        private void view_textOverlaySettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DicomOverlaySettingEditorWindow dialog = new DicomOverlaySettingEditorWindow(OVERLAY_OWNER_NAME, _dicomMprTool);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Owner = this;
            // show dialog
            dialog.ShowDialog();

            DicomOverlaySettingEditorWindow.SetTextOverlay(OVERLAY_OWNER_NAME, _dicomMprTool);
        }

        /// <summary>
        /// Shows the window level.
        /// </summary>
        private void view_showTextOverlayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            view_showTextOverlayMenuItem.IsChecked ^= true;
            bool value = view_showTextOverlayMenuItem.IsChecked;

            _dicomMprTool.IsTextOverlayVisible = value;
        }

        /// <summary>
        /// Opens an MPR settings form.
        /// </summary>
        private void view_settingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MprImageToolAppearanceSettingsWindow dialog = new MprImageToolAppearanceSettingsWindow(_mprSettingsManager, _sliceType);

            dialog.CanChangeColorMarkSettings = false;
            dialog.CanChangeSliceType = false;

            dialog.Owner = this;
            dialog.ShowDialog();

            UpdateMprSettings();

            // hide the color mark
            _dicomMprTool.MprImageTool.IsFocusedSliceColorMarkVisible = false;
        }

        /// <summary>
        /// Handles the SelectionChanged event of processingComboBox object.
        /// </summary>
        private void processingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
                return;

            ProcessingCommandBase command = _processingCommands[processingComboBox.SelectedIndex];

            if (_dicomMprTool.ViewProcessingCommand == command)
                return;

            if (_dicomMprTool.GetMouseButtonsForInteractionMode(WpfDicomMprToolInteractionMode.ViewProcessing) == VintasoftMouseButtons.None)
                _dicomMprTool.SetInteractionMode(VintasoftMouseButtons.Left, WpfDicomMprToolInteractionMode.ViewProcessing);

            _dicomMprTool.ViewProcessingCommand = command;
        }

        #endregion


        #region Animation

        #region 'Animation' menu

        /// <summary>
        /// Handles the Click event of showAnimationMenuItem object.
        /// </summary>
        private void showAnimationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IsAnimationStarted = showAnimationMenuItem.IsChecked;
        }

        /// <summary>
        /// Handles the Click event of animationRepeatMenuItem object.
        /// </summary>
        private void animationRepeatMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _isAnimationCycled = animationRepeatMenuItem.IsChecked;
        }

        /// <summary>
        /// Handles the TextChanged event of animationDelayComboBox object.
        /// </summary>
        private void animationDelayComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int delay;
            if (int.TryParse(animationDelay_valueComboBox.Text, out delay))
                _animationDelay = Math.Max(1, delay);
            else
                animationDelay_valueComboBox.Text = _animationDelay.ToString();
        }

        #endregion


        #region Frame animation

        /// <summary>
        /// Starts animation.
        /// </summary>
        private void StartAnimation()
        {
            StopAnimation();

            _animationThread = new Thread(AnimationMethod);
            _animationThread.IsBackground = false;

            // disable visual tool
            _dicomMprTool.Enabled = false;
            // disable tool strip
            imageViewerToolBar.IsEnabled = false;
            dicomMprToolInteractionModeToolBar.IsEnabled = false;

            _animationThread.Start();
            _isAnimationStarted = true;
        }

        /// <summary>
        /// Stops animation.
        /// </summary>
        private void StopAnimation()
        {
            _isAnimationStarted = false;

            if (_animationThread == null)
                return;

            showAnimationMenuItem.IsChecked = false;

            _animationThread = null;

            _isPageChanging = false;

            // if the focused index in thumbnail viewer was NOT changed during animation
            if (!_isPageChanging)
            {
                if (imageViewerToolBar.SelectedPageIndex != _currentAnimatedFrameIndex)
                {
                    imageViewerToolBar.SelectedPageIndex = _currentAnimatedFrameIndex;
                }
            }

            // enable tool strip
            imageViewerToolBar.IsEnabled = true;
            dicomMprToolInteractionModeToolBar.IsEnabled = true;
            // enable visual tool
            _dicomMprTool.Enabled = true;
        }

        /// <summary>
        /// Animation thread.
        /// </summary>
        private void AnimationMethod()
        {
            Thread currentThread = Thread.CurrentThread;
            _currentAnimatedFrameIndex = imageViewerToolBar.SelectedPageIndex;
            int count = imageViewerToolBar.PageCount;

            for (; _currentAnimatedFrameIndex < count || _isAnimationCycled;)
            {
                if (_animationThread != currentThread)
                    break;

                _isPageChanging = true;

                if (!_isWindowClosed)
                    Dispatcher.BeginInvoke(new ChangePageDelegate(ChangePage));

                _isPageChanging = false;
                Thread.Sleep(_animationDelay);

                if (_isWindowClosed)
                {
                    break;
                }

                _currentAnimatedFrameIndex++;
                if (_isAnimationCycled && _currentAnimatedFrameIndex >= count)
                    _currentAnimatedFrameIndex = 0;
            }

            _currentAnimatedFrameIndex--;
            Dispatcher.BeginInvoke(new ThreadStart(StopAnimation));
        }

        /// <summary>
        /// Changes page in the viewer.
        /// </summary>
        private void ChangePage()
        {
            if (!_isWindowClosed)
                imageViewerToolBar.SelectedPageIndex = _currentAnimatedFrameIndex + 1;
        }

        #endregion

        #endregion


        #region Image viewer toolbar

        /// <summary>
        /// Saves the image of the MPR slice.
        /// </summary>
        private void file_saveImageSliceMenuItem_Click(object sender, EventArgs e)
        {
            SaveSliceImage();
        }

        /// <summary>
        /// Moves the slice.
        /// </summary>
        private void imageViewerToolBar_PageIndexChanged(object sender, PageIndexChangedEventArgs e)
        {
            // if form is not initialized
            if (!_isInitialized || _isWindowClosed)
                return;

            // get the new perpendicular axis position of slice
            // because the page index contains information about perpendicular axis position
            int newPerpendicularAxisPosition = e.SelectedPageIndex;
            // get the current perpendicular axis position of slice
            int currentPerpendicularAxisPosition = GetPerpendicularAxisPositionPx(_currentSlice);

            // if perpendicular axis position is changed
            if (newPerpendicularAxisPosition != currentPerpendicularAxisPosition)
            {
                // update perpendicular axis position
                SetPerpendicularAxisPositionPx(_currentSlice, newPerpendicularAxisPosition);
            }
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
        /// Handles the CanExecute event of saveImageSliceCommandBinding object.
        /// </summary>
        private void saveImageSliceCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = file_saveImageSliceMenuItem.IsEnabled;
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

        /// <summary>
        /// Handles the CanExecute event of showTextOverlayCommandBinding object.
        /// </summary>
        private void showTextOverlayCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = view_showTextOverlayMenuItem.IsEnabled;
        }

        #endregion


        #region Image saving

        /// <summary>
        /// Saves the image of the MPR slice.
        /// </summary>
        private void SaveSliceImage()
        {
            // get the image slice from DICOM MPR tool
            using (VintasoftImage image = _dicomMprTool.DicomViewerTool.GetDisplayedImage())
            {
                // save image to a file
                SaveImageFileWindow.SaveImageToFile(image, ImagingEncoderFactory.Default);
            }
        }

        /// <summary>
        /// Image collection is saved successfully.
        /// </summary>
        private void images_ImageCollectionSaved(object sender, EventArgs e)
        {
            progressBar.Visibility = Visibility.Collapsed;
            statusLabel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Image collection saving process is in progress.
        /// </summary>
        private void images_ImageCollectionSavingProgress(object sender, ProgressEventArgs e)
        {
            progressBar.Value = e.Progress;

            DemosTools.DoEvents();
        }

        #endregion


        #region Perpendicular axis of slice

        /// <summary>
        /// Returns the perpendicular axis position, in pixels.
        /// </summary>
        /// <param name="slice">The slice.</param>
        /// <returns>
        /// The perpendicular axis position.
        /// </returns>
        private int GetPerpendicularAxisPositionPx(MprPlanarSlice slice)
        {
            int xAxisPx = _visualizationController.MprImage.XDataLength - 1;
            int yAxisPx = _visualizationController.MprImage.YDataLength - 1;
            int zAxisPx = _visualizationController.MprImage.ZDataLength - 1;
            double xAxisMm = _visualizationController.MprImage.XLength;
            double yAxisMm = _visualizationController.MprImage.YLength;
            double zAxisMm = _visualizationController.MprImage.ZLength;


            double value = 0;

            if (MprPlanarSlice.IsSagittalSlice(slice))
            {
                value = slice.Location.X * xAxisPx / xAxisMm;
            }
            else if (MprPlanarSlice.IsCoronalSlice(slice))
            {
                value = slice.Location.Y * yAxisPx / yAxisMm;
            }
            else if (MprPlanarSlice.IsAxialSlice(slice))
            {
                value = slice.Location.Z * zAxisPx / zAxisMm;
            }

            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Returns the perpendicular axis length, in pixels.
        /// </summary>
        /// <param name="slice">The slice.</param>
        /// <returns>
        /// The perpendicular axis length.
        /// </returns>
        private int GetPerpendicularAxisLengthInPx(MprPlanarSlice slice)
        {
            int xAxisPx = _visualizationController.MprImage.XDataLength;
            int yAxisPx = _visualizationController.MprImage.YDataLength;
            int zAxisPx = _visualizationController.MprImage.ZDataLength;
            if (MprPlanarSlice.IsSagittalSlice(slice))
                return xAxisPx;

            if (MprPlanarSlice.IsCoronalSlice(slice))
                return yAxisPx;

            if (MprPlanarSlice.IsAxialSlice(slice))
                return zAxisPx;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the perpendicular axis position.
        /// </summary>
        /// <param name="slice">The slice.</param>
        /// <param name="location">The location, in pixels.</param>
        private void SetPerpendicularAxisPositionPx(MprPlanarSlice slice, int location)
        {
            VintasoftPoint3D newLocation = slice.Location;

            int xAxisPx = _visualizationController.MprImage.XDataLength;
            int yAxisPx = _visualizationController.MprImage.YDataLength;
            int zAxisPx = _visualizationController.MprImage.ZDataLength;
            double xAxisMm = _visualizationController.MprImage.XLength;
            double yAxisMm = _visualizationController.MprImage.YLength;
            double zAxisMm = _visualizationController.MprImage.ZLength;

            if (MprPlanarSlice.IsSagittalSlice(slice))
            {
                newLocation.X = location * (xAxisMm / xAxisPx);
            }
            else if (MprPlanarSlice.IsCoronalSlice(slice))
            {
                newLocation.Y = location * (yAxisMm / yAxisPx);
            }
            else if (MprPlanarSlice.IsAxialSlice(slice))
            {
                newLocation.Z = location * (zAxisMm / zAxisPx);
            }
            else
            {
                throw new NotImplementedException();
            }

            slice.Location = newLocation;
        }

        #endregion


        /// <summary>
        /// Scales the slice proportion to the viewer.
        /// </summary>
        private void FitScene()
        {
            _dicomMprTool.MprImageTool.FitScene();
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
                if (!view_topPanelAlwaysVisibleMenuItem.IsChecked)
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
        /// Updates the tool strip and scroll bar.
        /// </summary>
        private void currentSlice_PropertyChanged(object sender, ObjectPropertyChangedEventArgs e)
        {
            // if form is not initialized
            if (!_isInitialized)
                return;

            // get slice perpendicular axis position
            int perpendicularAxisPosition = GetPerpendicularAxisPositionPx(_currentSlice);
            // get slice perpendicular axis length
            int perpendicularAxisLength = GetPerpendicularAxisLengthInPx(_currentSlice);
            perpendicularAxisPosition = Math.Max(0,
                Math.Min(perpendicularAxisPosition, perpendicularAxisLength));

            // if value must be changed in tool strip
            if (imageViewerToolBar.SelectedPageIndex != perpendicularAxisPosition)
                imageViewerToolBar.SelectedPageIndex = perpendicularAxisPosition;
        }

        /// <summary>
        /// Creates the MPR slice images.
        /// </summary>
        /// <param name="useGray16Images">Indicates that the MPR slice image must be Gray16.</param>
        /// <param name="applyVoiLutCommand">
        /// Indicates that the <see cref="ApplyDicomImageVoiLutCommand"/> must be applied to MPR slice image.
        /// </param>
        /// <returns>
        /// The MPR slices image.
        /// </returns>
        private ImageCollection CreateMprImageSlices(
            bool useGray16Images,
            bool applyVoiLutCommand)
        {
            MprImage mprImage = _dicomMprTool.MprImageTool.MprImage;

            // create MPR slice offset
            VintasoftPoint3D sliceLocationOffset = new VintasoftPoint3D(0, 0, 0);
            // slice of MPR image
            MprSlice slice;
            // slice count
            int imageSliceCount;

            if (MprPlanarSlice.IsSagittalSlice(_currentSlice))
            {
                sliceLocationOffset.X = mprImage.XLength / mprImage.XDataLength;
                imageSliceCount = mprImage.XDataLength;
                slice = mprImage.CreateSagittalSlice(0);
            }
            else if (MprPlanarSlice.IsCoronalSlice(_currentSlice))
            {
                sliceLocationOffset.Y = mprImage.YLength / mprImage.YDataLength;
                imageSliceCount = mprImage.YDataLength;
                slice = mprImage.CreateCoronalSlice(0);
            }
            else if (MprPlanarSlice.IsAxialSlice(_currentSlice))
            {
                sliceLocationOffset.Z = mprImage.ZLength / mprImage.ZDataLength;
                imageSliceCount = mprImage.ZDataLength;
                slice = mprImage.CreateAxialSlice(0);
            }
            else
            {
                throw new NotImplementedException();
            }

            // create processing command for MPR image slices
            ProcessingCommandBase command = GetMprImageSliceProcessing(useGray16Images, applyVoiLutCommand);

            // create result collection
            ImageCollection images = new ImageCollection();

            // initialize progress bar and description label
            progressBar.Value = 0;
            progressBar.Maximum = imageSliceCount;
            progressBar.Visibility = Visibility.Visible;
            statusLabel.Content = "Create MPR Image Slices...";
            statusLabel.Visibility = Visibility.Visible;

            for (int i = 0; i < imageSliceCount; i++)
            {
                // update progress bar
                progressBar.Value = i;
                DemosTools.DoEvents();

                // render the MPR slice image
                MprImageSlice mprImageSlice = mprImage.RenderSlice(slice);

                // move slice
                slice.Location += sliceLocationOffset;

                // get image of MPR slice
                VintasoftImage image = mprImageSlice.GetImageAndDispose();
                // process image
                command.ExecuteInPlace(image);
                // save image
                images.Add(image);
            }

            progressBar.Maximum = 100;
            progressBar.Visibility = Visibility.Collapsed;
            statusLabel.Content = string.Empty;
            statusLabel.Visibility = Visibility.Collapsed;

            return images;
        }

        /// <summary>
        /// Returns the processing command that should be applied to the MPR slice image.
        /// </summary>
        /// <param name="useGray16Images">Indicates whether the MPR slice image must be Gray16.</param>
        /// <param name="applyVoiLutCommand">
        /// Indicates whether the <see cref="ApplyDicomImageVoiLutCommand"/> must be applied to MPR slice image.
        /// </param>
        /// <returns>
        /// The processing command for apply to MPR slice image.
        /// </returns>
        private ProcessingCommandBase GetMprImageSliceProcessing(
            bool useGray16Images,
            bool applyVoiLutCommand)
        {
            ProcessingCommandBase command = null;

            // if VOI LUT must be applied
            if (applyVoiLutCommand)
            {
                // create command
                ApplyDicomImageVoiLutCommand voiLutCommand =
                    _dicomMprTool.DicomViewerTool.CreateApplyDicomImageVoiLutCommand();

                // if result image must be Gray16
                if (useGray16Images)
                    voiLutCommand.OutputPixelFormat = DicomImagePixelFormat.Gray16;
                else
                    voiLutCommand.OutputPixelFormat = DicomImagePixelFormat.Gray8;

                command = voiLutCommand;
            }
            else
            {
                // create command
                ChangePixelFormatCommand changePixelFormat = new ChangePixelFormatCommand();

                // if result image must be Gray16
                if (useGray16Images)
                    changePixelFormat.PixelFormat = Vintasoft.Imaging.PixelFormat.Gray16;
                else
                    changePixelFormat.PixelFormat = Vintasoft.Imaging.PixelFormat.Gray8;

                command = changePixelFormat;
            }
            return command;
        }

        /// <summary>
        /// The window is loaded.
        /// </summary>
        private void Mpr2DWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // shows the slices on viewers
            _visualizationController.ShowSliceInViewer(imageViewer1, _currentSlice);
        }

        /// <summary>
        /// Returns the MPR slice type.
        /// </summary>
        /// <param name="slice">The MPR planar slice.</param>
        /// <returns>
        /// The MPR slice type.
        /// </returns>
        private SliceType GetSliceType(MprPlanarSlice slice)
        {
            // if slice is sagittal
            if (MprPlanarSlice.IsSagittalSlice(slice))
                return SliceType.Sagittal;

            // if slice is coronal
            if (MprPlanarSlice.IsCoronalSlice(slice))
                return SliceType.Coronal;

            // if slice is axial
            if (MprPlanarSlice.IsAxialSlice(slice))
                return SliceType.Axial;

            throw new NotImplementedException();
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

            // update slice
            WpfMprSliceVisualizer visualMprSlice = _visualizationController.GetVisualMprSliceAssociatedWithMprSlice(_currentSlice);
            // if slice is sagittal
            if (MprPlanarSlice.IsSagittalSlice(_currentSlice))
                _mprSettingsManager.SagittalSliceAppearance.SetSettings(visualMprSlice);
            // if slice is coronal
            else if (MprPlanarSlice.IsCoronalSlice(_currentSlice))
                _mprSettingsManager.CoronalSliceAppearance.SetSettings(visualMprSlice);
            // if slice is axial
            else if (MprPlanarSlice.IsAxialSlice(_currentSlice))
                _mprSettingsManager.AxialSliceAppearance.SetSettings(visualMprSlice);

            // update MPR tool
            _mprSettingsManager.SetMprToolSettings(_dicomMprTool.MprImageTool);
        }

        #endregion

        #endregion



        #region Delegates

        delegate void ChangePageDelegate();

        #endregion

    }
}
