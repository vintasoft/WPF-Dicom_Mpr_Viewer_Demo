using System.Windows;
using System.Windows.Media;

using Vintasoft.Imaging.Dicom.Mpr.Wpf.UI.VisualTools;
using Vintasoft.Imaging.Wpf;
using Vintasoft.Imaging.Wpf.UI;
using Vintasoft.Imaging.Wpf.UI.VisualTools.GraphicObjects;

namespace Vintasoft.Imaging.Dicom.Mpr.Wpf.UI
{
    /// <summary>
    /// Displays 3D axis of MPR image.
    /// </summary>
    public class WpfMprImage3DAxisGraphicObject : WpfGraphicObject
    {

        #region Fields

        /// <summary>
        /// The MPR visualization controller.
        /// </summary>
        WpfMprVisualizationController _visualizationController;

        #endregion


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfMprImage3DAxisGraphicObject"/> class.
        /// </summary>
        /// <param name="visualizationController">The MPR visualization controller.</param>
        public WpfMprImage3DAxisGraphicObject(WpfMprVisualizationController visualizationController)
            : base()
        {
            _visualizationController = visualizationController;
        }

        #endregion


        #region Methods

        /// <summary>
        /// Renders the object on specified <see cref="T:System.Windows.Media.DrawingContext" /> in the object space.
        /// </summary>
        /// <param name="viewer">An image viewer.</param>
        /// <param name="context">A drawing context where the object must be rendered.</param>
        /// <remarks>
        /// This method draws object after the <see cref="P:Vintasoft.Imaging.Wpf.UI.VisualTools.GraphicObjects.WpfGraphicObject.PointTransform" /> is applied to
        /// the DrawingContext, specified by <i>context</i> parameter.<br /><br />
        /// By default this method does not do anything.
        /// </remarks>
        public override void RenderInObjectSpace(WpfImageViewer viewer, DrawingContext context)
        {
            WpfDicomMprTool dicomMprTool = _visualizationController.GetDicomMprToolAssociatedWithImageViewer(viewer);
            if (dicomMprTool == null)
                return;

            MprImage mprImage = dicomMprTool.MprImageTool.MprImage;

            // get 3D axis projections on slice (2D space, mm)
            MprSlice focusedSlice = dicomMprTool.MprImageTool.FocusedSlice;
            Point p0 = VintasoftWpfConverter.Convert(focusedSlice.GetPointProjectionOnSlice(new VintasoftPoint3D(0, 0, 0)));
            Point pX00 = VintasoftWpfConverter.Convert(focusedSlice.GetPointProjectionOnSlice(new VintasoftPoint3D(mprImage.XLength, 0, 0)));
            Point pXY0 = VintasoftWpfConverter.Convert(focusedSlice.GetPointProjectionOnSlice(new VintasoftPoint3D(mprImage.XLength, mprImage.YLength, 0)));
            Point p0Y0 = VintasoftWpfConverter.Convert(focusedSlice.GetPointProjectionOnSlice(new VintasoftPoint3D(0, mprImage.YLength, 0)));
            Point p0YZ = VintasoftWpfConverter.Convert(focusedSlice.GetPointProjectionOnSlice(new VintasoftPoint3D(0, mprImage.YLength, mprImage.ZLength)));
            Point p00Z = VintasoftWpfConverter.Convert(focusedSlice.GetPointProjectionOnSlice(new VintasoftPoint3D(0, 0, mprImage.ZLength)));
            Point pX0Z = VintasoftWpfConverter.Convert(focusedSlice.GetPointProjectionOnSlice(new VintasoftPoint3D(mprImage.XLength, 0, mprImage.ZLength)));

            // transform points from slice 2D space to image viewer space
            WpfPointTransform transformFormSliceToViewer = dicomMprTool.MprImageTool.FocusedSliceView.GetPointTransform(viewer, viewer.Image);
            p0 = transformFormSliceToViewer.TransformPoint(p0);
            pX00 = transformFormSliceToViewer.TransformPoint(pX00);
            pXY0 = transformFormSliceToViewer.TransformPoint(pXY0);
            p0Y0 = transformFormSliceToViewer.TransformPoint(p0Y0);
            p0YZ = transformFormSliceToViewer.TransformPoint(p0YZ);
            p00Z = transformFormSliceToViewer.TransformPoint(p00Z);
            pX0Z = transformFormSliceToViewer.TransformPoint(pX0Z);


            double thickness = 1;

            // draw axis
            context.DrawLine(new Pen(Brushes.Gray, thickness), pX00, pX0Z);
            context.DrawLine(new Pen(Brushes.Gray, thickness), pX00, pXY0);
            context.DrawLine(new Pen(Brushes.Gray, thickness), p0Y0, pXY0);
            context.DrawLine(new Pen(Brushes.Gray, thickness), p0Y0, p0YZ);
            context.DrawLine(new Pen(Brushes.Gray, thickness), p00Z, p0YZ);
            context.DrawLine(new Pen(Brushes.Gray, thickness), p00Z, pX0Z);

            context.DrawLine(new Pen(Brushes.Lime, thickness), p0, pX00);
            context.DrawLine(new Pen(Brushes.Red, thickness), p0, p0Y0);
            context.DrawLine(new Pen(Brushes.Blue, thickness), p0, p00Z);
        }

        /// <summary>
        /// Creates a new <see cref="T:WpfMprImage3DAxisGraphicObject" /> that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="T:WpfMprImage3DAxisGraphicObject" /> that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new WpfMprImage3DAxisGraphicObject(_visualizationController);
        }

        /// <summary>
        /// Returns a bounding box of object, in object space.
        /// </summary>
        /// <returns>
        /// Bounding box of object, in object space.
        /// </returns>
        public override Rect GetBoundingBox()
        {
            return Rect.Empty;
        }

        /// <summary>
        /// Returns a value indicating whether point belongs the object.
        /// </summary>
        /// <param name="p">Point in object space.</param>
        /// <param name="ignoreContainmentCheckDistance">A value indicating whether the point must be checked on the object only
        /// (ignore the "containment" region around object that is used for object selection).</param>
        /// <returns>
        /// <b>true</b> if point belongs the object;
        /// otherwise, <b>false</b>.
        /// </returns>
        public override bool IsPointOnObject(Point p, bool ignoreContainmentCheckDistance)
        {
            return false;
        }

        #endregion
    }
}
