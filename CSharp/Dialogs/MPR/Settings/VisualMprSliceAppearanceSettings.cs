using System.Windows.Media;

using Vintasoft.Imaging.Dicom.Mpr;
using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// Stores and manages the appearance settings for <see cref="WpfVisualMprSlice"/>.
    /// </summary>
    public class VisualMprSliceAppearanceSettings
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualMprSliceAppearanceSettings"/> class.
        /// </summary>
        public VisualMprSliceAppearanceSettings()
        {
        }

        #endregion



        #region Properties

        Color _sliceColor = Colors.Gray;
        /// <summary>
        /// Gets or sets a slice color.
        /// </summary>
        public Color SliceColor
        {
            get
            {
                return _sliceColor;
            }
            set
            {
                _sliceColor = value;
            }
        }

        double _sliceLineWidth = 2;
        /// <summary>
        /// Gets or sets a slice line width.
        /// </summary>
        public double SliceLineWidth
        {
            get
            {
                return _sliceLineWidth;
            }
            set
            {
                _sliceLineWidth = value;
            }
        }

        Color _focusedSliceColor = Colors.Gray;
        /// <summary>
        /// Gets or sets a focused slice color.
        /// </summary>
        public Color FocusedSliceColor
        {
            get
            {
                return _focusedSliceColor;
            }
            set
            {
                _focusedSliceColor = value;
            }
        }

        double _focusedSliceLineWidth = 2;
        /// <summary>
        /// Gets or sets a focused slice line width.
        /// </summary>
        public double FocusedSliceLineWidth
        {
            get
            {
                return _focusedSliceLineWidth;
            }
            set
            {
                _focusedSliceLineWidth = value;
            }
        }

        double _markerPointDiameter = 4;
        /// <summary>
        /// Gets or sets a marker point diameter.
        /// </summary>
        public double MarkerPointDiameter
        {
            get
            {
                return _markerPointDiameter;
            }
            set
            {
                _markerPointDiameter = value;
            }
        }

        double _thickness = 0f;
        /// <summary>
        /// Gets or sets a slice thickness.
        /// </summary>
        public double Thickness
        {
            get
            {
                return _thickness;
            }
            set
            {
                _thickness = value;
            }
        }

        MprSliceRenderingMode _renderingMode = MprSliceRenderingMode.MPR;
        /// <summary>
        /// Gets or sets the MPR image slice rendering mode.
        /// </summary>
        public MprSliceRenderingMode RenderingMode
        {
            get
            {
                return _renderingMode;
            }
            set
            {
                _renderingMode = value;
            }
        }

        double _curveTension = 1;
        /// <summary>
        /// Gets or sets the curve tension for curvilinear slice.
        /// </summary>
        public double CurveTension
        {
            get
            {
                return _curveTension;
            }
            set
            {
                _curveTension = value;
            }
        }

        int _sliceCount = 50;
        /// <summary>
        /// Gets or sets the planar slice count.
        /// </summary>
        public int SliceCount
        {
            get
            {
                return _sliceCount;
            }
            set
            {
                _sliceCount = value;
            }
        }

        #endregion



        #region Methods

        /// <summary>
        /// Copies the appearance to the specified manager.
        /// </summary>
        /// <param name="manager">The slice appearance manager.</param>
        public void CopyTo(VisualMprSliceAppearanceSettings manager)
        {
            manager.SliceColor = SliceColor;
            manager.SliceLineWidth = SliceLineWidth;
            manager.FocusedSliceColor = FocusedSliceColor;
            manager.FocusedSliceLineWidth = FocusedSliceLineWidth;
            manager.MarkerPointDiameter = MarkerPointDiameter;
            manager.Thickness = Thickness;
            manager.RenderingMode = RenderingMode;
            manager.CurveTension = CurveTension;
            manager.SliceCount = SliceCount;
        }

        /// <summary>
        /// Sets the appearance to the specified slice.
        /// </summary>
        /// <param name="sliceVisualizer">The MPR slice visualizer.</param>
        public void SetSettings(WpfMprSliceVisualizer sliceVisualizer)
        {
            sliceVisualizer.SlicePen = new Pen(new SolidColorBrush(SliceColor), SliceLineWidth);
            sliceVisualizer.FocusedSlicePen = new Pen(new SolidColorBrush(FocusedSliceColor), FocusedSliceLineWidth);

            sliceVisualizer.MarkerPointDiameter = MarkerPointDiameter;
            sliceVisualizer.Slice.Thickness = Thickness;
            sliceVisualizer.Slice.RenderingMode = RenderingMode;

            MprCurvilinearSlice slice = sliceVisualizer.Slice as MprCurvilinearSlice;
            if (slice != null)
            {
                SolidColorBrush sliceBrush = new SolidColorBrush(
                    Color.FromArgb((byte)(SliceColor.A / 3), SliceColor.R, SliceColor.G, SliceColor.B));
                Brush previousBrush = sliceVisualizer.SliceThicknessBrush;
                sliceVisualizer.SliceThicknessBrush = sliceBrush;

                slice.CurveTension = CurveTension;
            }
            else
            {
                Pen thicknessPen = new Pen(new SolidColorBrush(SliceColor), SliceLineWidth * 0.75f);
                thicknessPen.DashStyle = DashStyles.Dash;

                sliceVisualizer.SliceThicknessPen = thicknessPen;
            }

            MprPerpendicularMultiSlice multiSlice = sliceVisualizer.Slice as MprPerpendicularMultiSlice;

            if (multiSlice != null)
            {
                multiSlice.SliceCount = SliceCount;
            }
        }

        /// <summary>
        /// Updates the current settings from specified visualizer.
        /// </summary>
        /// <param name="visualizer">The visualizer.</param>
        public void Update(WpfMprSliceVisualizer visualizer)
        {
            SliceColor = ((SolidColorBrush)visualizer.SlicePen.Brush).Color;
            SliceLineWidth = visualizer.SlicePen.Thickness;

            FocusedSliceColor = ((SolidColorBrush)visualizer.FocusedSlicePen.Brush).Color;
            FocusedSliceLineWidth = visualizer.FocusedSlicePen.Thickness;

            MarkerPointDiameter = (float)visualizer.MarkerPointDiameter;
            Thickness = (float)visualizer.Slice.Thickness;
            RenderingMode = visualizer.Slice.RenderingMode;

            if (visualizer.Slice is MprCurvilinearSlice)
            {
                MprCurvilinearSlice curvilinearSlice = (MprCurvilinearSlice)visualizer.Slice;

                CurveTension = curvilinearSlice.CurveTension;
            }
            else if (visualizer.Slice is MprPerpendicularMultiSlice)
            {
                MprPerpendicularMultiSlice perpendicularMultiSlice = (MprPerpendicularMultiSlice)visualizer.Slice;

                SliceCount = perpendicularMultiSlice.SliceCount;
            }
        }

        #endregion

    }
}

