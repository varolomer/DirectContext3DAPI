using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.DirectContext3D;

namespace DirectContext3DAPI
{
    // A class that brings together all the data and rendering parameters that are needed to draw one sequence
    // of primitives (e.g., triangles) with the same format and appearance.
    class RenderingPassBufferStorage
    {
        public DisplayStyle DisplayStyle { get; set; }

        //Geometry Info
        public List<MeshInfo> Meshes { get; set; }
        public List<IList<XYZ>> EdgeXYZs { get; set; }

        //Counts
        public int PrimitiveCount { get; set; }
        public int VertexBufferCount { get; set; }
        public int IndexBufferCount { get; set; }

        //Buffers
        public VertexBuffer VertexBuffer { get; set; }
        public IndexBuffer IndexBuffer { get; set; }

        //Formatting -- See the image: D:\OneDrive\Yazılım - GenFusions\02_Labs\RevitDirectContext3D_Lab\DirectContext3DAPI\DirectContext3DAPI\Assets\SS\VertexFormat.PNG
        public VertexFormatBits FormatBits { get; set; } //Formatting is used in the creation of both of the VertexFormat and EffectInstance objects
        public VertexFormat VertexFormat { get; set; }
        public EffectInstance EffectInstance { get; set; }


        //VertexFormatBits is not to be confused with VertexFormat. The latter type of object is
        //associated with low-level graphics functionality and may become invalid. VertexFormat is
        //needed to submit a set of vertex and index buffers for rendering (see Autodesk::Revit::DB::DirectContext3D::DrawContext).


        public RenderingPassBufferStorage(DisplayStyle displayStyle)
        {
            DisplayStyle = displayStyle;
            Meshes = new List<MeshInfo>();
            EdgeXYZs = new List<IList<XYZ>>();
        }

        /// <summary>
        /// If the user changes the display style (i.e. from hidden line to shaded) the graphics
        /// is needed to be re-rendered. The same applies if the low-level vertex buffer loses validity
        /// or if it gets null.
        /// </summary>
        /// <param name="newDisplayStyle"></param>
        /// <returns></returns>
        public bool needsUpdate(DisplayStyle newDisplayStyle)
        {
            if (newDisplayStyle != DisplayStyle)
                return true;

            if (PrimitiveCount > 0)
                if (VertexBuffer == null || !VertexBuffer.IsValid() ||
                    IndexBuffer == null || !IndexBuffer.IsValid() ||
                    VertexFormat == null || !VertexFormat.IsValid() ||
                    EffectInstance == null || !EffectInstance.IsValid())
                    return true;

            return false;
        }


    }
}
