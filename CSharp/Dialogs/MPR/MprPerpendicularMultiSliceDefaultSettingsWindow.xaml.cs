using System.Windows;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Dicom.Mpr;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A window that allows to change the default settings of <see cref="MprPerpendicularMultiSlice"/>.
    /// </summary>
    public partial class MprPerpendicularMultiSliceDefaultSettingsWindow : Window
    {
        public MprPerpendicularMultiSliceDefaultSettingsWindow()
        {
            InitializeComponent();

            sliceCountNumericUpDown.Value = MprPerpendicularMultiSlice.DefaultSliceCount;
            planarSliceMarginPaddingFEditorControl.PaddingValue =
                VintasoftDrawingConverter.Convert(MprPerpendicularMultiSlice.DefaultPlanarSliceMargin);
        }

        /// <summary>
        /// Handles the Click event of Button object.
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MprPerpendicularMultiSlice.DefaultSliceCount = (int)sliceCountNumericUpDown.Value;
            MprPerpendicularMultiSlice.DefaultPlanarSliceMargin =
                VintasoftDrawingConverter.Convert(planarSliceMarginPaddingFEditorControl.PaddingValue);

            DialogResult = true;
        }
    }
}
