using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Annotation.Measurements;
using Vintasoft.Imaging.Annotation.Wpf.UI;
using Vintasoft.Imaging.Annotation.Wpf.UI.Measurements;
using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI.VisualTools;
using Vintasoft.Imaging.UI;
using Vintasoft.Imaging.Wpf;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A toolbar for WPF DICOM MPR tool.
    /// </summary>
    public partial class DicomMprToolInteractionModeToolBar : ToolBar
    {

        #region Nested enums

        /// <summary>
        /// Specifies available types of measurement annotations.
        /// </summary>
        private enum MeasurementType
        {
            /// <summary>
            /// The length.
            /// </summary>
            Length,

            /// <summary>
            /// The ellipse.
            /// </summary>
            Ellipse,

            /// <summary>
            /// The angle.
            /// </summary>
            Angle,
        }

        #endregion



        #region Fields

        /// <summary>
        /// Dictionary: the DICOM MPR tool interaction mode => menu button.
        /// </summary>
        Dictionary<WpfDicomMprToolInteractionMode, Control> _interactionModeToMenuButton =
            new Dictionary<WpfDicomMprToolInteractionMode, Control>();

        /// <summary>
        /// Dictionary: menu button => the DICOM MPR tool interaction mode.
        /// </summary>
        Dictionary<Control, WpfDicomMprToolInteractionMode> _menuButtonToInteractionMode =
            new Dictionary<Control, WpfDicomMprToolInteractionMode>();

        /// <summary>
        /// Dictionary: the DICOM MPR tool interaction mode => icon name format for menu button.
        /// </summary>
        Dictionary<WpfDicomMprToolInteractionMode, string> _interactionModeToIconNameFormat =
            new Dictionary<WpfDicomMprToolInteractionMode, string>();


        /// <summary>
        /// The current measurement menu button.
        /// </summary>
        MenuItem _currentMeasurementAnnotationTypeMenuButton = null;

        /// <summary>
        /// The current unit of measure menu button.
        /// </summary>
        MenuItem _currentMeasurementAnnotationUnitOfMeasureMenuButton = null;


        /// <summary>
        /// The menu button that deletes focused measurement annotation in focused image viewer.
        /// </summary>
        MenuItem _measurementAnnotationDeleteMenuButton;

        /// <summary>
        /// The menu button that deletes all measurement annotations in focused image viewer.
        /// </summary>
        MenuItem _measurementAnnotationDeleteAllMenuButton;

        /// <summary>
        /// The menu button that deletes all measurement annotations in all image viewers.
        /// </summary>
        MenuItem _measurementAnnotationDeleteAllOnViewersMenuButton;

        /// <summary>
        /// The drop down menu that contains available interaction modes for mouse wheel.
        /// </summary>
        MenuItem _mouseWheelDropDownMenu;

        /// <summary>
        /// The icon name of mouse wheel button.
        /// </summary>
        readonly string MOUSE_WHEEL_BUTTON_ICON_NAME;

        /// <summary>
        /// The available mouse buttons.
        /// </summary>
        VintasoftMouseButtons[] _availableMouseButtons = new VintasoftMouseButtons[] {
            VintasoftMouseButtons.Left, VintasoftMouseButtons.Middle, VintasoftMouseButtons.Right
        };


        #region Hot keys

        public static RoutedCommand _deleteCommand = new RoutedCommand();
        public static RoutedCommand _deleteAllCommand = new RoutedCommand();
        public static RoutedCommand _deleteAllOnViewersCommand = new RoutedCommand();

        #endregion

        #endregion



        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomMprToolInteractionModeToolBar"/> class.
        /// </summary>
        public DicomMprToolInteractionModeToolBar()
        {
            InitializeComponent();

            // initialize interaction mode of WpfDicomMprTool
            _supportedInteractionModes = new WpfDicomMprToolInteractionMode[] {
                        WpfDicomMprToolInteractionMode.Browse,
                        WpfDicomMprToolInteractionMode.Pan,
                        WpfDicomMprToolInteractionMode.Roll,
                        WpfDicomMprToolInteractionMode.Rotate3D,
                        WpfDicomMprToolInteractionMode.Zoom,
                        WpfDicomMprToolInteractionMode.ViewProcessing,
                        WpfDicomMprToolInteractionMode.WindowLevel,
                        WpfDicomMprToolInteractionMode.Measure};

            // initilize name of icons

            MOUSE_WHEEL_BUTTON_ICON_NAME = "MouseWheelIcon";

            _interactionModeToIconNameFormat.Add(WpfDicomMprToolInteractionMode.Browse,
                "Browse_{0}{1}{2}Icon");
            _interactionModeToIconNameFormat.Add(WpfDicomMprToolInteractionMode.Measure,
                "Measure_{0}{1}{2}Icon");
            _interactionModeToIconNameFormat.Add(WpfDicomMprToolInteractionMode.Pan,
                "Pan_{0}{1}{2}Icon");
            _interactionModeToIconNameFormat.Add(WpfDicomMprToolInteractionMode.Rotate3D,
                "Rotate3D_{0}{1}{2}Icon");
            _interactionModeToIconNameFormat.Add(WpfDicomMprToolInteractionMode.Roll,
                "Roll_{0}{1}{2}Icon");
            _interactionModeToIconNameFormat.Add(WpfDicomMprToolInteractionMode.WindowLevel,
                "WindowLevel_{0}{1}{2}Icon");
            _interactionModeToIconNameFormat.Add(WpfDicomMprToolInteractionMode.Zoom,
                "Zoom_{0}{1}{2}Icon");
            _interactionModeToIconNameFormat.Add(WpfDicomMprToolInteractionMode.ViewProcessing,
                "ViewProcessing_{0}{1}{2}Icon");

            // initialize buttons
            InitButtons();
        }

        #endregion



        #region Properties

        WpfDicomMprTool[] _dicomMprTools = null;
        /// <summary>
        /// Gets or sets an array of WpfDicomMprTool objects.
        /// </summary>
        /// <value>
        /// Default value is <b>null</b>.
        /// </value>
        public WpfDicomMprTool[] DicomMprTools
        {
            get
            {
                return _dicomMprTools;
            }
            set
            {
                // if value is changed
                if (_dicomMprTools != value)
                {
                    if (_dicomMprTools != null)
                    {
                        foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                            UnsubscribeFromDicomMprToolEvents(dicomMprTool);
                    }

                    _dicomMprTools = value;

                    if (_dicomMprTools != null && _dicomMprTools.Length > 0)
                    {
                        foreach (VintasoftMouseButtons mouseButton in _availableMouseButtons)
                        {
                            WpfDicomMprToolInteractionMode interactionMode = _dicomMprTools[0].GetInteractionMode(mouseButton);
                            SubscribeToDicomMprToolEvents(_dicomMprTools[0]);

                            for (int i = 1; i < _dicomMprTools.Length; i++)
                            {
                                _dicomMprTools[i].SetInteractionMode(mouseButton, interactionMode);
                                SubscribeToDicomMprToolEvents(_dicomMprTools[i]);
                            }

                            if (_measurementAnnotationDeleteAllOnViewersMenuButton != null)
                            {
                                if (_dicomMprTools.Length == 1)
                                    _measurementAnnotationDeleteAllOnViewersMenuButton.Visibility = Visibility.Collapsed;
                                else
                                    _measurementAnnotationDeleteAllOnViewersMenuButton.Visibility = Visibility.Visible;
                            }
                        }
                    }

                    ResetUnsupportedInteractionModes();
                    UpdateInteractionButtonIcons();
                }
            }
        }

        WpfDicomMprToolInteractionMode[] _supportedInteractionModes;
        /// <summary>
        /// Gets or sets the supported interaction modes of toolbar.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <i>value</i> is <b>null</b>.</exception>
        public WpfDicomMprToolInteractionMode[] SupportedInteractionModes
        {
            get
            {
                return _supportedInteractionModes;
            }
            set
            {
                if (_supportedInteractionModes != value)
                {
                    if (value == null)
                        throw new ArgumentNullException();

                    _supportedInteractionModes = value;

                    InitButtons();

                    ResetUnsupportedInteractionModes();
                }
            }
        }

        WpfDicomMprToolInteractionMode[] _disabledInteractionModes = null;
        /// <summary>
        /// Gets or sets the disabled interaction modes of toolbar.
        /// </summary>
        /// <value>
        /// Default value is <b>null</b>.
        /// </value>
        public WpfDicomMprToolInteractionMode[] DisabledInteractionModes
        {
            get
            {
                return _disabledInteractionModes;
            }
            set
            {
                // if value is changed
                if (_disabledInteractionModes != value)
                {
                    // save new value
                    _disabledInteractionModes = value;

                    // for each interaction mode
                    foreach (WpfDicomMprToolInteractionMode interactionMode in _interactionModeToMenuButton.Keys)
                        // enable button for interaction mode
                        _interactionModeToMenuButton[interactionMode].IsEnabled = true;

                    // if disabled interaction modes are specified
                    if (_disabledInteractionModes != null)
                    {
                        // the menu button of interaction mode
                        Control menuButton = null;

                        // for each interaction mode
                        foreach (WpfDicomMprToolInteractionMode interactionMode in _disabledInteractionModes)
                        {
                            // if button is enabled
                            if (_interactionModeToMenuButton.TryGetValue(interactionMode, out menuButton))
                                // disable the button for interaction mode
                                menuButton.IsEnabled = false;
                        }
                    }
                }
            }
        }

        #endregion



        #region Methods

        #region PROTECTED

        /// <summary>
        /// Adds hot key commands to the parent window.
        /// </summary>
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.InputBindings.AddRange(InputBindings);
                window.CommandBindings.AddRange(CommandBindings);

                window.Loaded += Window_Loaded;
            }

            base.OnVisualParentChanged(oldParent);
        }

        #endregion


        #region PRIVATE

        #region Init

        /// <summary>
        /// Initializes the buttons.
        /// </summary>
        private void InitButtons()
        {
            // remove old buttons
            Items.Clear();

            InitMouseWheelButtons();

            if (Items.Count > 0)
                Items.Add(new Separator());

            InitInteractionModeMenuButtons();
        }

        /// <summary>
        /// Initializes the mouse wheel buttons.
        /// </summary>
        private void InitMouseWheelButtons()
        {
            // the button name
            string name = "Mouse Wheel";
            // create the "Mouse Wheel" button
            Button mouseWheelMenuButton = new Button();
            mouseWheelMenuButton.Content = name;
            mouseWheelMenuButton.ToolTip = name;
            mouseWheelMenuButton.Click += new RoutedEventHandler(mouseWheelMenuButton_Click);

            // set the button icon
            SetToolStripButtonIcon(mouseWheelMenuButton, MOUSE_WHEEL_BUTTON_ICON_NAME);

            // available interaction modes of mouse wheel
            WpfDicomMprToolInteractionMode[] mouseWheelInteractionMode =
                new WpfDicomMprToolInteractionMode[] {
                     WpfDicomMprToolInteractionMode.None,
                     WpfDicomMprToolInteractionMode.Browse,
                     WpfDicomMprToolInteractionMode.Zoom };

            Menu mouseWheelDropDownMenuParent = new Menu();
            mouseWheelDropDownMenuParent.Width = 24;
            mouseWheelDropDownMenuParent.VerticalAlignment = VerticalAlignment.Center;
            MenuItem mouseWheelDropDownMenu = new MenuItem();
            mouseWheelDropDownMenu.Background = Brushes.Transparent;
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            path.Fill = Brushes.Black;
            path.Data = Geometry.Parse("M 0 0 L 4 4 L 8 0 Z");
            mouseWheelDropDownMenu.Header = path;
            mouseWheelDropDownMenuParent.Items.Add(mouseWheelDropDownMenu);

            // for each interaction mode
            foreach (WpfDicomMprToolInteractionMode interactionMode in mouseWheelInteractionMode)
            {
                // create button
                MenuItem menuButton = new MenuItem();
                menuButton.Header = interactionMode.ToString();

                // if interaction mode is "Browse"
                if (interactionMode == WpfDicomMprToolInteractionMode.Browse)
                {
                    // mark button as checked
                    menuButton.IsChecked = true;
                }

                // save information about interaction mode in button
                menuButton.Tag = interactionMode;
                // subscribe to the button click event
                menuButton.Click += new RoutedEventHandler(mouseWheelInteractionModeButton_Click);

                // add button
                mouseWheelDropDownMenu.Items.Add(menuButton);
            }

            // add button to ToolStrip
            Items.Add(mouseWheelMenuButton);
            _mouseWheelDropDownMenu = mouseWheelDropDownMenu;
            Items.Add(mouseWheelDropDownMenuParent);
        }

        /// <summary>
        /// Initializes the interaction mode menu buttons.
        /// </summary>
        private void InitInteractionModeMenuButtons()
        {
            // clear dictionaries
            _interactionModeToMenuButton.Clear();
            _menuButtonToInteractionMode.Clear();
            _measurementAnnotationDeleteMenuButton = null;
            _measurementAnnotationDeleteAllMenuButton = null;

            // for each suported interaction mode
            foreach (WpfDicomMprToolInteractionMode interactionMode in _supportedInteractionModes)
            {
                // create button

                // get button name
                string name = interactionMode.ToString();

                Control menuButton = null;
                Menu menuDropDownButton = null;

                // if interaction mode is measurement
                if (interactionMode == WpfDicomMprToolInteractionMode.Measure)
                {
                    menuButton = new Button();
                    ((Button)menuButton).Content = name;


                    menuDropDownButton = new Menu();
                    menuDropDownButton.Width = 24;
                    menuDropDownButton.VerticalAlignment = VerticalAlignment.Center;
                    MenuItem mouseMeasureDropDownMenu = new MenuItem();
                    mouseMeasureDropDownMenu.Background = Brushes.Transparent;
                    System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
                    path.Fill = Brushes.Black;
                    path.Data = Geometry.Parse("M 0 0 L 4 4 L 8 0 Z");
                    mouseMeasureDropDownMenu.Header = path;
                    menuDropDownButton.Items.Add(mouseMeasureDropDownMenu);

                    // init measurement buttons for this button
                    InitMeasurementMenuButtons(mouseMeasureDropDownMenu);
                }
                else
                {
                    menuButton = new Button();
                    ((Button)menuButton).Content = name;
                }

                menuButton.ToolTip = name;

                // set the button icon
                SetButtonIcon(menuButton, interactionMode, VintasoftMouseButtons.None);

                // add button to the dictionaries
                _interactionModeToMenuButton.Add(interactionMode, menuButton);
                _menuButtonToInteractionMode.Add(menuButton, interactionMode);

                // if button must be disabled
                if (_disabledInteractionModes != null &&
                    Array.IndexOf(_disabledInteractionModes, interactionMode) >= 0)
                    // disable the button
                    menuButton.IsEnabled = false;

                menuButton.PreviewMouseDown += new MouseButtonEventHandler(interactionModeButton_PreviewMouseDown);

                // add button to the ToolBar
                Items.Add(menuButton);
                if (menuDropDownButton != null)
                    Items.Add(menuDropDownButton);
            }
        }

        #endregion


        #region Interaction mode

        /// <summary>
        /// Selects the interaction mode button.
        /// </summary>
        private void dicomMprTool_InteractionModeChanged(
            object sender,
            WpfDicomMprToolInteractionModeChangedEventArgs e)
        {
            UpdateInteractionMode(e.Button, e.InteractionMode);
        }

        /// <summary>
        /// Selects the interaction mode of DicomMprTools.
        /// </summary>
        private void interactionModeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MenuItem splitMenuButton = sender as MenuItem;
            if (splitMenuButton != null && !splitMenuButton.IsPressed)
                return;

            Button menuButton = (Button)sender;
            WpfDicomMprToolInteractionMode interactionMode = _menuButtonToInteractionMode[menuButton];

            UpdateInteractionMode(WpfObjectConverter.CreateVintasoftMouseButtons(e), interactionMode);
        }

        /// <summary>
        /// Updates the interaction mode in DicomMprTools.
        /// </summary>
        /// <param name="mouseButton">Mouse button.</param>
        /// <param name="interactionMode">Interaction mode.</param>
        private void UpdateInteractionMode(
            VintasoftMouseButtons mouseButton,
            WpfDicomMprToolInteractionMode interactionMode)
        {
            // if interaction mode is NOT supported
            if (Array.IndexOf(SupportedInteractionModes, interactionMode) == -1)
                interactionMode = WpfDicomMprToolInteractionMode.None;

            // if mouse button is NOT supported
            if (Array.IndexOf(_availableMouseButtons, mouseButton) == -1)
                interactionMode = WpfDicomMprToolInteractionMode.None;

            // for each DICOM MPR tool
            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
            {
                // if interaction mode is NOT Measure
                if (interactionMode != WpfDicomMprToolInteractionMode.Measure)
                {
                    // clear focused annotation in measure tool
                    dicomMprTool.MeasureTool.FocusedAnnotationView = null;
                    // clear selected annotations in measure tool
                    dicomMprTool.MeasureTool.SelectedAnnotations.Clear();
                }

                // set the interaction mode for DICOM MPR tool
                dicomMprTool.SetInteractionMode(mouseButton, interactionMode);
            }

            // update icons of interaction buttons
            UpdateInteractionButtonIcons();
        }

        #endregion


        #region Measurements

        /// <summary>
        /// Initializes the measurement menu buttons.
        /// </summary>
        /// <param name="measureButton">Measure menu button.</param>
        private void InitMeasurementMenuButtons(MenuItem measureMenuButton)
        {
            measureMenuButton.SubmenuOpened += new RoutedEventHandler(measureButton_SubmenuOpened);
            measureMenuButton.SubmenuClosed += new RoutedEventHandler(measureButton_SubmenuClosed);

            ItemCollection measureMenuButtonItems = measureMenuButton.Items;

            // available measurement annotation types
            MeasurementType[] measurementTypes = new MeasurementType[] {
                MeasurementType.Length,
                MeasurementType.Ellipse,
                MeasurementType.Angle };
            // for each measurement annotation types
            foreach (MeasurementType measurementType in measurementTypes)
            {
                // create button
                MenuItem menuButton = new MenuItem();
                menuButton.Header = measurementType.ToString();

                // if measurement type is Length (default)
                if (measurementType == MeasurementType.Length)
                {
                    // check the button
                    menuButton.IsChecked = true;
                    // save reference to the current button
                    _currentMeasurementAnnotationTypeMenuButton = menuButton;
                }

                menuButton.Tag = measurementType;
                menuButton.Click += new RoutedEventHandler(measurementAnnotationTypeButton_Click);

                // add button
                measureMenuButtonItems.Add(menuButton);
            }

            // add seperator
            measureMenuButtonItems.Add(new Separator());

            // add "Delete" button
            _measurementAnnotationDeleteMenuButton = new MenuItem();
            _measurementAnnotationDeleteMenuButton.HeaderStringFormat = "{0} (Del)";
            _measurementAnnotationDeleteMenuButton.Header = "Delete";

            CommandBinding deleteCommandBinding = new CommandBinding(_deleteCommand, measurementAnnotationDeleteButton_Click, deleteCommandBinding_CanExecute);
            KeyGesture deleteKeyGesture = new KeyGesture(Key.Delete);
            InputBinding deleteInputBinding = new InputBinding(_deleteCommand, deleteKeyGesture);
            _deleteCommand.InputGestures.Add(deleteKeyGesture);
            CommandBindings.Add(deleteCommandBinding);
            InputBindings.Add(deleteInputBinding);

            _measurementAnnotationDeleteMenuButton.Click += new RoutedEventHandler(measurementAnnotationDeleteButton_Click);
            measureMenuButtonItems.Add(_measurementAnnotationDeleteMenuButton);

            // add "Delete All" button
            _measurementAnnotationDeleteAllMenuButton = new MenuItem();
            _measurementAnnotationDeleteAllMenuButton.HeaderStringFormat = "{0} (Alt + Del)";
            _measurementAnnotationDeleteAllMenuButton.Header = "Delete All";

            CommandBinding deleteAllCommandBinding = new CommandBinding(_deleteAllCommand, measurementAnnotationDeleteAllButton_Click, deleteAllCommandBinding_CanExecute);
            KeyGesture deleteAllKeyGesture = new KeyGesture(Key.Delete, ModifierKeys.Alt);
            InputBinding deleteAllInputBinding = new InputBinding(_deleteAllCommand, deleteAllKeyGesture);
            _deleteAllCommand.InputGestures.Add(deleteAllKeyGesture);
            CommandBindings.Add(deleteAllCommandBinding);
            InputBindings.Add(deleteAllInputBinding);

            _measurementAnnotationDeleteAllMenuButton.Click += new RoutedEventHandler(measurementAnnotationDeleteAllButton_Click);
            measureMenuButtonItems.Add(_measurementAnnotationDeleteAllMenuButton);

            // add "Delete All On Viewers" button
            _measurementAnnotationDeleteAllOnViewersMenuButton = new MenuItem();
            _measurementAnnotationDeleteAllOnViewersMenuButton.HeaderStringFormat = "{0} (Ctrl + Del)";
            _measurementAnnotationDeleteAllOnViewersMenuButton.Header = "Delete All On Viewers";

            CommandBinding deleteAllOnViewersCommandBinding = new CommandBinding(_deleteAllOnViewersCommand,
                measurementAnnotationDeleteAllOnViewersButton_Click, deleteAllOnViewersCommandBinding_CanExecute);
            KeyGesture deleteAllOnViewersKeyGesture = new KeyGesture(Key.Delete, ModifierKeys.Control);
            InputBinding deleteAllOnViewersInputBinding = new InputBinding(_deleteAllOnViewersCommand, deleteAllOnViewersKeyGesture);
            _deleteAllOnViewersCommand.InputGestures.Add(deleteAllOnViewersKeyGesture);
            CommandBindings.Add(deleteAllOnViewersCommandBinding);
            InputBindings.Add(deleteAllOnViewersInputBinding);

            _measurementAnnotationDeleteAllOnViewersMenuButton.Click += new RoutedEventHandler(measurementAnnotationDeleteAllOnViewersButton_Click);
            measureMenuButtonItems.Add(_measurementAnnotationDeleteAllOnViewersMenuButton);


            // add seperator
            measureMenuButtonItems.Add(new Separator());

            // add "Units of Measure" button
            MenuItem unitsOfMeasureButton = new MenuItem();
            unitsOfMeasureButton.Header = "Units of Measure";
            InitUnitsOfMeasureButtons(unitsOfMeasureButton);
            measureMenuButtonItems.Add(unitsOfMeasureButton);
        }

        /// <summary>
        /// Initializes the units of measure buttons.
        /// </summary>
        private void InitUnitsOfMeasureButtons(MenuItem unitsOfMeasureButton)
        {
            // available units of measure
            UnitOfMeasure[] unitOfMeasures = new UnitOfMeasure[] {
                UnitOfMeasure.Millimeters,
                UnitOfMeasure.Centimeters,
                UnitOfMeasure.Inches,
                UnitOfMeasure.Pixels};
            // for each unit of measure
            foreach (UnitOfMeasure unitOfMeasure in unitOfMeasures)
            {
                // create button
                MenuItem menuButton = new MenuItem();
                menuButton.Header = unitOfMeasure.ToString();
                menuButton.Tag = unitOfMeasure;

                // if unit of measure is millimeter (default value)
                if (unitOfMeasure == UnitOfMeasure.Millimeters)
                {
                    // check the button
                    menuButton.IsChecked = true;
                    // save reference to the current unit of measure button
                    _currentMeasurementAnnotationUnitOfMeasureMenuButton = menuButton;
                }

                menuButton.Click += new RoutedEventHandler(measurementAnnotationUnitOfMeasureButton_Click);

                // add button
                unitsOfMeasureButton.Items.Add(menuButton);
            }
        }

        /// <summary>
        /// The drop down menu of "Units of Measure" button is opening.
        /// </summary>
        private void measureButton_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            bool deleteButtonEnabled = false;
            bool deleteAllButtonEnabled = false;

            // get the active DICOM MPR tool
            WpfDicomMprTool dicomMprTool = GetActiveMprTool();
            // if active DICOM MPR tool exists
            if (dicomMprTool != null)
            {
                deleteButtonEnabled = dicomMprTool.MeasureTool.DeleteAction.IsEnabled;
                deleteAllButtonEnabled = dicomMprTool.MeasureTool.DeleteAllAction.IsEnabled;
            }

            // update "Delete measurement annotation" buttons

            if (_measurementAnnotationDeleteMenuButton != null)
                _measurementAnnotationDeleteMenuButton.IsEnabled = deleteButtonEnabled;

            if (_measurementAnnotationDeleteAllMenuButton != null)
                _measurementAnnotationDeleteAllMenuButton.IsEnabled = deleteAllButtonEnabled;
        }

        /// <summary>
        /// The drop down menu of "Units of Measure" button is closed.
        /// </summary>
        private void measureButton_SubmenuClosed(object sender, RoutedEventArgs e)
        {
            if (_measurementAnnotationDeleteMenuButton != null)
                _measurementAnnotationDeleteMenuButton.IsEnabled = true;

            if (_measurementAnnotationDeleteAllMenuButton != null)
                _measurementAnnotationDeleteAllMenuButton.IsEnabled = true;
        }

        /// <summary>
        /// The building annotation type is changed.
        /// </summary>
        private void measurementAnnotationTypeButton_Click(object sender, RoutedEventArgs e)
        {
            // if button with previous measurement annotation type exists
            if (_currentMeasurementAnnotationTypeMenuButton != null)
                // disable the button with previous measurement annotation type
                _currentMeasurementAnnotationTypeMenuButton.IsChecked = false;

            // save reference to the button with new measurement annotation type
            _currentMeasurementAnnotationTypeMenuButton = (MenuItem)sender;
            // check the button
            _currentMeasurementAnnotationTypeMenuButton.IsChecked = true;
        }

        /// <summary>
        /// The units of measure are changed.
        /// </summary>
        private void measurementAnnotationUnitOfMeasureButton_Click(object sender, RoutedEventArgs e)
        {
            // if button with previous unit of measure exists
            if (_currentMeasurementAnnotationUnitOfMeasureMenuButton != null)
                // disable button with previous unit of measure
                _currentMeasurementAnnotationUnitOfMeasureMenuButton.IsChecked = false;

            // save reference to the button with new unit of measure
            _currentMeasurementAnnotationUnitOfMeasureMenuButton = (MenuItem)sender;
            // check the button
            _currentMeasurementAnnotationUnitOfMeasureMenuButton.IsChecked = true;

            // get unit of measure from the button
            UnitOfMeasure unitOfMeasure = (UnitOfMeasure)_currentMeasurementAnnotationUnitOfMeasureMenuButton.Tag;

            // for each DICOM MPR tool
            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                // set unit of measure for DICOM MPR tool
                dicomMprTool.MeasureTool.UnitsOfMeasure = unitOfMeasure;
        }

        /// <summary>
        /// Deletes the selected measurement annotation.
        /// </summary>
        private void measurementAnnotationDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // get active DICOM MPR tool
            WpfDicomMprTool dicomMprTool = GetActiveMprTool();
            if (dicomMprTool == null)
                return;

            // if focused measurement annotation can be removed
            if (dicomMprTool.MeasureTool.DeleteAction.IsEnabled)
                // remove focused measurement annotation
                dicomMprTool.MeasureTool.DeleteAction.Execute();
        }

        /// <summary>
        /// Deletes all measurement annotations in active DICOM MPR tool.
        /// </summary>
        private void measurementAnnotationDeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            // get active DICOM MPR tool
            WpfDicomMprTool dicomMprTool = GetActiveMprTool();
            if (dicomMprTool == null)
                return;

            // if all measurement annotations can be removed
            if (dicomMprTool.MeasureTool.DeleteAllAction.IsEnabled)
                // remove all measurement annotations
                dicomMprTool.MeasureTool.DeleteAllAction.Execute();
        }

        /// <summary>
        /// Deletes all measurement annotations in all DICOM MPR tools.
        /// </summary>
        private void measurementAnnotationDeleteAllOnViewersButton_Click(object sender, RoutedEventArgs e)
        {
            // for each DICOM MPR tool
            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
            {
                // if all measurement annotations can be removed
                if (dicomMprTool.MeasureTool.DeleteAllAction.IsEnabled)
                    // remove all measurement annotations
                    dicomMprTool.MeasureTool.DeleteAllAction.Execute();
            }
        }

        /// <summary>
        /// Begins building the measurement annotation.
        /// </summary>
        /// <param name="measureTool">The measure tool.</param>
        private void StartBuildMeasurementAnnotation(WpfImageMeasureTool measureTool)
        {
            if (_currentMeasurementAnnotationTypeMenuButton == null)
                return;

            // get the annotation type, which should be built
            MeasurementType measurementType = (MeasurementType)_currentMeasurementAnnotationTypeMenuButton.Tag;

            WpfMeasurementAnnotationView annotationView = null;
            // begin the building of annotation
            switch (measurementType)
            {
                case MeasurementType.Length:
                    annotationView = measureTool.BeginLineMeasurement();
                    break;

                case MeasurementType.Ellipse:
                    annotationView = measureTool.BeginEllipseMeasurement();
                    break;

                case MeasurementType.Angle:
                    annotationView = measureTool.BeginAngleMeasurement();
                    break;
            }

            if (annotationView == null)
                throw new NotImplementedException();

            // update the measuring text template
            annotationView.MeasurementAnnotationData.MeasuringTextTemplate =
                DicomMeasureToolUtils.GetMeasuringTextTemplate(annotationView.MeasurementAnnotationData);
        }

        /// <summary>
        /// Begins the building of measurement annotation.
        /// </summary>
        private void measureTool_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WpfImageMeasureTool measureTool = (WpfImageMeasureTool)sender;

            if (measureTool.ActionButton == VintasoftMouseButtons.None)
                return;

            if ((measureTool.ActionButton & WpfObjectConverter.CreateVintasoftMouseButtons(e)) != VintasoftMouseButtons.None)
            {
                Point location = e.GetPosition(measureTool.ImageViewer);
                // if mouse cursor is over interaction area
                if (measureTool.IsPointOnInteractionArea(location))
                    return;
                // if mouse cursor is over annotation
                if (measureTool.FindAnnotationView(location) != null)
                    return;

                // begin the building of measurement annotation
                StartBuildMeasurementAnnotation(measureTool);
            }
        }

        /// <summary>
        /// Units of measure is changed.
        /// </summary>
        private void measureTool_UnitsOfMeasureChanged(object sender, PropertyChangedEventArgs<UnitOfMeasure> e)
        {
            WpfImageMeasureTool measureTool = (WpfImageMeasureTool)sender;
            // if image viewer is not empty
            if (measureTool.ImageViewer != null)
            {
                // for each image
                foreach (VintasoftImage image in measureTool.ImageViewer.Images)
                {
                    // get annotation list
                    IList<WpfAnnotationView> annotations = measureTool.GetAnnotationsFromImage(image);
                    if (annotations != null)
                    {
                        // for each annotation
                        foreach (WpfAnnotationView annotation in annotations)
                        {
                            MeasurementAnnotationData annotationData = annotation.Data as MeasurementAnnotationData;
                            // if annotation is measurement annotation
                            if (annotationData != null)
                                // set new measuring text template
                                annotationData.MeasuringTextTemplate = DicomMeasureToolUtils.GetMeasuringTextTemplate(annotationData);
                        }
                    }
                }
            }

            // update the measurement unit of measure in DicomMprTools
            UpdateMeasurementUnitOfMeasure(e.NewValue);
        }

        /// <summary>
        /// Updates the measurement unit of measure in DicomMprTools.
        /// </summary>
        /// <param name="unitOfMeasure">The unit of measure.</param>
        private void UpdateMeasurementUnitOfMeasure(UnitOfMeasure unitOfMeasure)
        {
            // for each DICOM MPR tool
            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
            {
                // set the unit of measure for measurement tool
                dicomMprTool.MeasureTool.UnitsOfMeasure = unitOfMeasure;
            }
        }

        #endregion


        #region Button icons

        /// <summary>
        /// Updates the icon of interaction buttons.
        /// </summary>
        private void UpdateInteractionButtonIcons()
        {
            // for each interaction mode
            foreach (WpfDicomMprToolInteractionMode interactionMode in _interactionModeToMenuButton.Keys)
            {
                // get the menu button of interaction mode
                Control menuButton = _interactionModeToMenuButton[interactionMode];

                // get mouse buttons of interaction mode
                VintasoftMouseButtons mouseButtons = GetMouseButtonsForInteractionMode(interactionMode);

                // update icon for menu button
                SetButtonIcon(menuButton, interactionMode, mouseButtons);
            }
        }

        /// <summary>
        /// Returns the icon name of specified interaction mode and buttons.
        /// </summary>
        /// <param name="interactionMode">The interaction mode.</param>
        /// <param name="mouseButtons">The mouse buttons of interaction mode.</param>
        /// <returns>
        /// The icon name.
        /// </returns>
        private string GetInteractionModeIconName(WpfDicomMprToolInteractionMode interactionMode, VintasoftMouseButtons mouseButtons)
        {
            // indices of action buttons (left, middle, right)
            byte[] indexes = new byte[] { 0, 0, 0 };

            // if mouse buttons are not empty
            if (mouseButtons != VintasoftMouseButtons.None)
            {
                // if left mouse button is active
                if ((mouseButtons & VintasoftMouseButtons.Left) != 0)
                    indexes[0] = 1;
                // if middle mouse button is active
                if ((mouseButtons & VintasoftMouseButtons.Middle) != 0)
                    indexes[1] = 1;
                // if right mouse button is active
                if ((mouseButtons & VintasoftMouseButtons.Right) != 0)
                    indexes[2] = 1;
            }

            // get the icon name format
            string iconNameFormat = _interactionModeToIconNameFormat[interactionMode];

            // return the icon name
            return string.Format(iconNameFormat, indexes[0], indexes[1], indexes[2]);
        }

        /// <summary>
        /// Sets the icon for the tool strip button.
        /// </summary>
        /// <param name="menuButton">The menu button.</param>
        /// <param name="interactionMode">The interaction mode.</param>
        /// <param name="mouseButtons">The mouse button.</param>
        private void SetButtonIcon(
            Control menuButton,
            WpfDicomMprToolInteractionMode interactionMode,
            VintasoftMouseButtons mouseButtons)
        {
            // get icon name for interaction mode
            string iconName = GetInteractionModeIconName(interactionMode, mouseButtons);

            // set the icon for button
            SetToolStripButtonIcon(menuButton, iconName);
        }

        /// <summary>
        /// Sets the icon for the tool strip button.
        /// </summary>
        /// <param name="menuButton">The menu button.</param>
        /// <param name="iconName">The icon name.</param>
        private void SetToolStripButtonIcon(Control menuButton, string iconName)
        {
            // if the icon name is NOT specified
            if (string.IsNullOrEmpty(iconName))
                return;

            // if menu button contains infomation about the button icon
            if (menuButton.Tag is string)
            {
                // get the icon name
                string currentIconName = menuButton.Tag.ToString();

                // if icon is not changed
                if (String.Equals(currentIconName, iconName, StringComparison.InvariantCultureIgnoreCase))
                    return;
            }

            menuButton.Background = Brushes.Transparent;

            Image image = new Image();
            image.Width = 16;
            image.Height = 16;
            image.Stretch = Stretch.None;
            // load resource
            image.Source = (BitmapImage)Resources[iconName];
            // load image
            if (menuButton is Button)
                ((Button)menuButton).Content = image;
            if (menuButton is MenuItem)
                ((MenuItem)menuButton).Header = image;
            // save icon name
            menuButton.Tag = iconName;
        }

        #endregion


        #region HotKeys

        /// <summary>
        /// Handles the CanExecute_Delete event of _deleteCommand control.
        /// </summary>
        private void deleteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _measurementAnnotationDeleteMenuButton.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute_DeleteAll event of _deleteAllCommand control.
        /// </summary>
        private void deleteAllCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _measurementAnnotationDeleteAllMenuButton.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute_DeleteAllOnViewers event of _deleteAllOnViewersCommand control.
        /// </summary>
        private void deleteAllOnViewersCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _measurementAnnotationDeleteAllOnViewersMenuButton.IsEnabled;
        }

        #endregion


        /// <summary>
        /// Subscribes to the WpfDicomMprTool events.
        /// </summary>
        /// <param name="tool">The WpfDicomMprTool.</param>
        private void SubscribeToDicomMprToolEvents(WpfDicomMprTool tool)
        {
            tool.ImageViewer.GotFocus += new RoutedEventHandler(imageViewer_GotFocus);
            tool.InteractionModeChanged +=
                new EventHandler<WpfDicomMprToolInteractionModeChangedEventArgs>(dicomMprTool_InteractionModeChanged);

            tool.MeasureTool.PreviewMouseDown += new MouseButtonEventHandler(measureTool_PreviewMouseDown);
            tool.MeasureTool.UnitsOfMeasureChanged +=
                new EventHandler<PropertyChangedEventArgs<UnitOfMeasure>>(measureTool_UnitsOfMeasureChanged);
        }

        /// <summary>
        /// Unsubscribes from the WpfDicomMprTool events.
        /// </summary>
        /// <param name="tool">The WpfDicomMprTool.</param>
        private void UnsubscribeFromDicomMprToolEvents(WpfDicomMprTool tool)
        {
            tool.InteractionModeChanged -= dicomMprTool_InteractionModeChanged;
            tool.MeasureTool.UnitsOfMeasureChanged -= measureTool_UnitsOfMeasureChanged;
        }

        /// <summary>
        /// Opens drop down menu with available interaction modes for mouse wheel.
        /// </summary>
        private void mouseWheelMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _mouseWheelDropDownMenu.IsSubmenuOpen = true;
        }

        /// <summary>
        /// The mouse wheel interaction mode is changed.
        /// </summary>
        private void mouseWheelInteractionModeButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem currentMenuButton = (MenuItem)sender;
            // get the interaction mode
            WpfDicomMprToolInteractionMode interactionMode = (WpfDicomMprToolInteractionMode)currentMenuButton.Tag;

            // for each DICOM MPR tool
            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                // update the interaction mode for mouse wheel
                dicomMprTool.MouseWheelInteractionMode = interactionMode;


            // uncheck all buttons

            MenuItem parentMenuButton = currentMenuButton.Parent as MenuItem;
            // if parent menu button exists
            if (parentMenuButton != null)
            {
                // for each item in parent menu item
                foreach (Control item in parentMenuButton.Items)
                {
                    // if item is menu button
                    if (item is MenuItem)
                        // uncheck the menu button
                        ((MenuItem)item).IsChecked = false;
                }
            }

            // check the current menu button
            currentMenuButton.IsChecked ^= true;
        }

        /// <summary>
        /// Returns the active <see cref="WpfDicomMprTool"/>.
        /// </summary>
        /// <returns>
        /// The active <see cref="WpfDicomMprTool"/>.
        /// </returns>
        private WpfDicomMprTool GetActiveMprTool()
        {
            if (_dicomMprTools != null)
            {
                foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
                {
                    if (dicomMprTool.ImageViewer != null &&
                        dicomMprTool.ImageViewer.IsFocused)
                    {
                        return dicomMprTool;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the mouse buttons for interaction mode.
        /// </summary>
        /// <param name="interactionMode">The interaction mode.</param>
        /// <returns>
        /// The mouse buttons for interaction mode.
        /// </returns>
        private VintasoftMouseButtons GetMouseButtonsForInteractionMode(
            WpfDicomMprToolInteractionMode interactionMode)
        {
            // get the active DICOM MPR tool
            WpfDicomMprTool dicomMprTool = GetActiveMprTool();

            // if active tool not exists
            if (dicomMprTool == null)
                return VintasoftMouseButtons.None;

            return dicomMprTool.GetMouseButtonsForInteractionMode(interactionMode);
        }

        /// <summary>
        /// Resets the unsupported interaction modes.
        /// </summary>
        private void ResetUnsupportedInteractionModes()
        {
            if (_dicomMprTools == null)
                return;

            // for each DICOM MPR tool
            foreach (WpfDicomMprTool dicomMprTool in _dicomMprTools)
            {
                // for each mouse button
                foreach (VintasoftMouseButtons mouseButton in _availableMouseButtons)
                {
                    // get the interaction mode for mouse button
                    WpfDicomMprToolInteractionMode interactionMode = dicomMprTool.GetInteractionMode(mouseButton);

                    // if interaction mode is None
                    if (interactionMode == WpfDicomMprToolInteractionMode.None)
                        continue;

                    // is interaction mode is not supported
                    if (Array.IndexOf(SupportedInteractionModes, interactionMode) == -1)
                        // reset the interaction mode for mouse button
                        dicomMprTool.SetInteractionMode(mouseButton, WpfDicomMprToolInteractionMode.None);
                }
            }
        }

        /// <summary>
        /// Updates the focused measurement annotations.
        /// </summary>
        private void imageViewer_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateInteractionButtonIcons();
        }

        /// <summary>
        /// Window is loaded.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMenuItemTemplatePartBackground(this);
        }

        /// <summary>
        /// Updates the template of <see cref="MenuItem"/> object for fixing the bug in <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="menuItem">The menu item.</param>
        /// <remarks>
        /// The <see cref="MenuItem"/> has bug and displays black rectangle in element if MenuItem.IsChecked property is set to True.
        /// This method fixes the bug.
        /// </remarks>
        private void UpdateMenuItemTemplatePartBackground(Control control)
        {
            MenuItem menuItem = control as MenuItem;
            if (menuItem != null)
            {
                const string TEMPLATE_PART_NAME = "GlyphPanel";
                Border border = menuItem.Template.FindName(TEMPLATE_PART_NAME, menuItem) as Border;

                if (border == null)
                {
                    menuItem.ApplyTemplate();
                    border = menuItem.Template.FindName(TEMPLATE_PART_NAME, menuItem) as Border;
                }

                if (border != null)
                    border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            }

            ItemsControl itemsControl = control as ItemsControl;
            if (itemsControl != null)
            {
                foreach (object item in itemsControl.Items)
                {
                    if (item is Control)
                        UpdateMenuItemTemplatePartBackground((Control)item);
                }
            }
        }

        #endregion

        #endregion

    }
}
