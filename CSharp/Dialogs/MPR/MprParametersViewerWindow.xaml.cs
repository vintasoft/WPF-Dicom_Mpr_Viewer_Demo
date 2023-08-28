using System.Windows;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Dicom.Mpr;
using Vintasoft.Imaging.ImageColors;
using Vintasoft.Imaging.Wpf;
using Vintasoft.Primitives;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A window that allows to view parameters of 3D MPR (multiplanar reconstruction).
    /// </summary>
    public partial class MprParametersViewerWindow : Window
    {
       
        #region Constants

        /// <summary>
        /// The MPR image location message template.
        /// </summary>
        const string MPR_IMAGE_LOCATION_TEMPLATE = "Location (millimeters): X={0:f2}, Y={1:f2}, Z={2:f2}";

        /// <summary>
        /// The MPR image size message template.
        /// </summary>
        const string MPR_IMAGE_SIZE_TEMPLATE = "Size (millimeters): X={0:f2}, Y={1:f2}, Z={2:f2}";

        /// <summary>
        /// The MPR image X axis message template.
        /// </summary>
        const string MPR_IMAGE_XAXIS_TEMPLATE = "XAxis (vector): X={0:f2}, Y={1:f2}, Z={2:f2}";

        /// <summary>
        /// The MPR image Y axis message template.
        /// </summary>
        const string MPR_IMAGE_YAXIS_TEMPLATE = "YAxis (vector): X={0:f2}, Y={1:f2}, Z={2:f2}";

        /// <summary>
        /// The MPR image Z axis message template.
        /// </summary>
        const string MPR_IMAGE_ZAXIS_TEMPLATE = "ZAxis (vector): X={0:f2}, Y={1:f2}, Z={2:f2}";


        /// <summary>
        /// The MPR slice location message template.
        /// </summary>
        const string SLICE_LOCATION_TEMPLATE = "Location (millimeters): X={0:f2}, Y={1:f2}, Z={2:f2}";

        /// <summary>
        /// The MPR slice X axis message template.
        /// </summary>
        const string SLICE_XAXIS_TEMPLATE = "XAxis (vector): X={0:f2}, Y={1:f2}, Z={2:f2}";

        /// <summary>
        /// The MPR slice Y axis message template.
        /// </summary>
        const string SLICE_YAXIS_TEMPLATE = "YAxis (vector): X={0:f2}, Y={1:f2}, Z={2:f2}";

        /// <summary>
        /// The MPR slice width message template.
        /// </summary>
        const string SLICE_WIDTH_TEMPLATE = "Width (miliimeters): {0:f2}";

        /// <summary>
        /// The MPR slice height message template.
        /// </summary>
        const string SLICE_HEIGHT_TEMPLATE = "Height (millimenters): {0:f2}";



        /// <summary>
        /// The Luminance measure location on image message template.
        /// </summary>
        const string LUMINANCE_LOCATION_ON_IMAGE_TEMPLATE = "Location on image (pixels): X={0:f0}, Y={1:f0}";

        /// <summary>
        /// The Luminance measure location on slice message template.
        /// </summary>
        const string LUMINANCE_LOCATION_ON_SLICE_TEMPLATE = "Location on slice (millimeters): X={0:f2}, Y={1:f2}";

        /// <summary>
        /// The Luminance measure location in MPR image message template.
        /// </summary>
        const string LUMINANCE_LOCATION_IN_MPR_IMAGE_TEMPLATE = "Location in MPR image (millimeters): X={0:f2}, Y={1:f2}, Z={2:f2}";

        /// <summary>
        /// The Luminance measure in MPR image, without interpolation.
        /// </summary>
        const string LUMINANCE_MPRIMAGE_TEMPLATE = "Luminance in MPR Image (16-bit unsigned): {0}";

        /// <summary>
        /// The Luminance measure in MPR image, linear interpolation.
        /// </summary>
        const string LUMINANCE_MPRIMAGE_LINEAR_TEMPLATE = "Luminance in MPR image, linear interpolation (16-bit unsigned): {0}";

        // <summary>
        /// The Luminance measure with LUT message template.
        /// </summary>
        const string LUMINANCE_WITH_LUT_TEMPLATE = "Luminance in Displayed Slice Image, with LUT (8-bit unsigned): {0}";

        #endregion



        #region Constructor

        /// <summary>
        /// Initializes a new instance of <see cref="MprParametersViewerWindow"/> class.
        /// </summary>
        /// <param name="locationOnImage">The location on image in pixels.</param>
        /// <param name="transformFromImageToSlice">The transformation from an image space to the slice space.</param>
        /// <param name="displayedImage">The displayed image.</param>
        /// <param name="mprImage">The MPR image.</param>
        /// <param name="slice">The MPR slice.</param>
        public MprParametersViewerWindow(
            Point locationOnImage,
            WpfPointTransform transformFromImageToSlice,
            VintasoftImage displayedImage,
            MprImage mprImage,
            MprSlice slice)
        {
            InitializeComponent();

            TransformFromImageToSlice = transformFromImageToSlice;
            DisplayedImage = displayedImage;
            MprImage = mprImage;
            Slice = slice;
            LocationOnImage = locationOnImage;

            UpdateValues();
        }

        #endregion



        #region Properties

        Point _locationOnImage;
        /// <summary>
        /// Gets or sets the location on image in pixels.
        /// </summary>
        public Point LocationOnImage
        {
            get
            {
                return _locationOnImage;
            }
            set
            {
                _locationOnImage = value;
            }
        }

        WpfPointTransform _transformFromImageToSlice;
        /// <summary>
        /// Gets or sets the transformation from an image space to the slice space.
        /// </summary>
        public WpfPointTransform TransformFromImageToSlice
        {
            get
            {
                return _transformFromImageToSlice;
            }
            set
            {
                _transformFromImageToSlice = value;
            }
        }

        VintasoftImage _displayedImage;
        /// <summary>
        /// Gets or sets the displayed image.
        /// </summary>
        public VintasoftImage DisplayedImage
        {
            get
            {
                return _displayedImage;
            }
            set
            {
                _displayedImage = value;
            }
        }

        MprImage _mprImage;
        /// <summary>
        /// Gets or sets the <see cref="MprImage"/>.
        /// </summary>
        public MprImage MprImage
        {
            get
            {
                return _mprImage;
            }
            set
            {
                _mprImage = value;
            }
        }

        MprSlice _slice;
        /// <summary>
        /// Gets or sets the <see cref="MprSlice"/>.
        /// </summary>
        public MprSlice Slice
        {
            get
            {
                return _slice;
            }
            set
            {
                _slice = value;
            }
        }

        #endregion



        #region Methods

        #region PUBLIC

        /// <summary>
        /// Updates all showing values.
        /// </summary>
        public void UpdateValues()
        {
            UpdateMprSliceValues();
            UpdateLuminanceValues();
        }

        #endregion


        #region PRIVATE

        /// <summary>
        /// Closes the form.
        /// </summary>
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Updates showing luminance values.
        /// </summary>
        private void UpdateLuminanceValues()
        {
            // location on image
            luminanceLocationOnImageLabel.Content = string.Format(
                LUMINANCE_LOCATION_ON_IMAGE_TEMPLATE, LocationOnImage.X, LocationOnImage.Y);

            // location on slice                     
            Point locationOnSlice = TransformFromImageToSlice.TransformPoint(LocationOnImage);
            VintasoftPoint locationOnSliceVP = new VintasoftPoint(locationOnSlice.X, locationOnSlice.Y);
            luminanceLocationOnSliceLabel.Content = string.Format(
                LUMINANCE_LOCATION_ON_SLICE_TEMPLATE, locationOnSlice.X, locationOnSlice.Y);

            // location in MPR image
            VintasoftPoint3D locationInMprImage = Slice.TransformPointFromSliceToWorldSpace(
                locationOnSliceVP);
            luminanceLocationInMprImageLabel.Content = string.Format(
                LUMINANCE_LOCATION_IN_MPR_IMAGE_TEMPLATE, locationInMprImage.X, locationInMprImage.Y, locationInMprImage.Z);

            // luminance in MPR image, without interpolation
            luminanceInMprImageLabel.Content = string.Format(
                LUMINANCE_MPRIMAGE_TEMPLATE, MprImage.GetPointLuminance(Slice, locationOnSliceVP, MprInterpolationMode.NearestNeighbor));

            // luminance in MPR image, linear interpolation
            luminanceInMprImageLinearLabel.Content = string.Format(
                LUMINANCE_MPRIMAGE_LINEAR_TEMPLATE, MprImage.GetPointLuminance(Slice, locationOnSliceVP, MprInterpolationMode.Linear));

            // luminance with LUT
            int luminanceWithLut = 0;
            if (LocationOnImage.X >= 0 && LocationOnImage.X < DisplayedImage.Width &&
                LocationOnImage.Y >= 0 && LocationOnImage.Y < DisplayedImage.Height)
            {
                ColorBase pixelColor = DisplayedImage.GetPixelColor((int)LocationOnImage.X, (int)LocationOnImage.Y);
                if (pixelColor is IndexedColor)
                {
                    IndexedColor indexedColor = (IndexedColor)pixelColor;
                    luminanceWithLut = indexedColor.Index;
                }
            }
            luminanceWithLutLabel.Content = string.Format(
                LUMINANCE_WITH_LUT_TEMPLATE, luminanceWithLut);
        }

        /// <summary>
        /// Updates showing MPR Slice values.
        /// </summary>
        private void UpdateMprSliceValues()
        {
            // slice location
            sliceLocationLabel.Content = string.Format(
                SLICE_LOCATION_TEMPLATE, Slice.Location.X, Slice.Location.Y, Slice.Location.Z);

            if (Slice is MprPlanarSlice)
            {
                MprPlanarSlice planarSlice = (MprPlanarSlice)Slice;

                // slice X axis
                sliceXAxisLabel.Content = string.Format(
                    SLICE_XAXIS_TEMPLATE, planarSlice.XAxis.X, planarSlice.XAxis.Y, planarSlice.XAxis.Z);

                // slice Y axis
                sliceYAxisLabel.Content = string.Format(
                    SLICE_YAXIS_TEMPLATE, planarSlice.YAxis.X, planarSlice.YAxis.Y, planarSlice.YAxis.Z);
            }

            if (Slice is MprPolylineSlice)
            {
                MprPolylineSlice polylineSlice = (MprPolylineSlice)Slice;

                // slice X axis
                sliceXAxisLabel.Content = string.Format(
                    SLICE_XAXIS_TEMPLATE, polylineSlice.XAxis.X, polylineSlice.XAxis.Y, polylineSlice.XAxis.Z);

                // slice Y axis
                sliceYAxisLabel.Content = string.Format(
                    SLICE_YAXIS_TEMPLATE, polylineSlice.YAxis.X, polylineSlice.YAxis.Y, polylineSlice.YAxis.Z);
            }

            // slice width
            sliceWidthLabel.Content = string.Format(
                SLICE_WIDTH_TEMPLATE, Slice.Width);

            // slice height
            sliceHeightLabel.Content = string.Format(
                SLICE_HEIGHT_TEMPLATE, Slice.Height);
        }

        #endregion

        #endregion

    }
}
