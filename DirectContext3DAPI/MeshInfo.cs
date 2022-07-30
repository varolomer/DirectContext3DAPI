using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectContext3DAPI
{
    class MeshInfo
    {
        public Mesh Mesh;
        public XYZ Normal;
        public ColorWithTransparency ColorWithTransparency;

        public MeshInfo(Mesh mesh, XYZ normal, ColorWithTransparency color)
        {
            Mesh = mesh;
            Normal = normal;
            ColorWithTransparency = color;
        }
    }
}
