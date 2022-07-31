using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectContext3DAPI
{
    public class CustomBufferStorage
    {
        public DisplayStyle DisplayStyle { get; set; }

        //Counts
        public int PrimitiveCount { get; set; }
        public int VertexBufferCount { get; set; }
        public int IndexBufferCount { get; set; }

        //Buffers
        public VertexBuffer VertexBuffer { get; set; }
        public IndexBuffer IndexBuffer { get; set; }

        //Formatting -- See the image: https://github.com/varolomer/DirectContext3DAPI/blob/master/DirectContext3DAPI/Assets/SS/VertexFormat.PNG
        public VertexFormatBits FormatBits { get; set; } //Formatting is used in the creation of both of the VertexFormat and EffectInstance objects
        public VertexFormat VertexFormat { get; set; }
        public EffectInstance EffectInstance { get; set; }


        public CustomBufferStorage(DisplayStyle displayStyle)
        {
            DisplayStyle = displayStyle;
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
