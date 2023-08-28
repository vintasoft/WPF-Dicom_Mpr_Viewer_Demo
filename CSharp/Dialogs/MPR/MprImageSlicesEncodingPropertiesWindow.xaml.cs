using System.Windows;

using Vintasoft.Imaging.Codecs.Encoders;

using WpfDemosCommonCode.Imaging.Codecs.Dialogs;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A window that allows to view and edit the MPR slice image encoding settings.
    /// </summary>
    public partial class MprImageSlicesEncodingPropertiesWindow : Window
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MprImageSlicesEncodingPropertiesWindow"/> class.
        /// </summary>
        public MprImageSlicesEncodingPropertiesWindow()
        {
            InitializeComponent();

            imageFormatComboBox.SelectedIndex = 1;
        }

        #endregion



        #region Properties

        /// <summary>
        /// Gets a value indicating whether the VOI LUT must be applied to MPR slice image.
        /// </summary>
        /// <value>
        /// <b>true</b> if the VOI LUT is applied to MPR slice image; otherwise, <b>false</b>.
        /// </value>
        public bool ApplyVoiLutToMprImageSlices
        {
            get
            {
                return applyVoiLutToImageSlicesCheckBox.IsChecked.Value == true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the MPR slice image pixel format must be Gray16.
        /// </summary>
        /// <value>
        ///   <b>true</b> if [the MPR slice image pixel format is Gray16; otherwise, <b>false</b>.
        /// </value>
        public bool UseGray16MprImages
        {
            get
            {
                return imageFormatComboBox.SelectedIndex == 1;
            }
        }

        TiffEncoderSettings _tiffEncoderSettings = new TiffEncoderSettings();
        /// <summary>
        /// Gets the TIFF encoder settings.
        /// </summary>
        public TiffEncoderSettings TiffEncoderSettings
        {
            get
            {
                return _tiffEncoderSettings;
            }
        }

        #endregion



        #region Methods

        /// <summary>
        /// Shows the TIFF encoder settings dialog.
        /// </summary>
        private void tiffEncoderSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            TiffEncoderSettingsWindow window = new TiffEncoderSettingsWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Owner = this;
            window.EncoderSettings = _tiffEncoderSettings;
            window.EditAnnotationSettings = false;
            window.CanAddImagesToExistingFile = false;

            window.ShowDialog();
        }

        /// <summary>
        /// "OK" button is clicked.
        /// </summary>
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        #endregion

    }
}
