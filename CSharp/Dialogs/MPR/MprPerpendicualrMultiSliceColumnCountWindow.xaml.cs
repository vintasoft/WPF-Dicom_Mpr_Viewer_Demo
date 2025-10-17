using System.Windows;

namespace WpfDicomMprViewerDemo
{
    /// <summary>
    /// A window that allows to change the count of perpendicular multi slice columns.
    /// </summary>
    public partial class MprPerpendicualrMultiSliceColumnCountWindow : Window
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="MprPerpendicualrMultiSliceColumnCountWindow"/> class.
        /// </summary>
        public MprPerpendicualrMultiSliceColumnCountWindow()
        {
            InitializeComponent();
        }



        /// <summary>
        /// Gets or sets the column count.
        /// </summary>
        public int ColumnCount
        {
            get
            {
                return (int)columnCountNumericUpDown.Value;
            }
            set
            {
                columnCountNumericUpDown.Value = value;
            }
        }

        /// <summary>
        /// Handles the Click event of okButton object.
        /// </summary>
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
