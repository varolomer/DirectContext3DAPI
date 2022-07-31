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
    public class Helper
    {
        /// <summary>
        /// Extracts the material information and sets the given color with 
        /// alpha channel and ref bool isTransparent.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="materialId"></param>
        /// <param name="isTransparent"></param>
        /// <param name="colorRGBA"></param>
        public static void MaterialExtract(Document doc, ElementId materialId, ref bool isTransparent, ref ColorWithTransparency colorRGBA)
        {
            //If invalid id return
            if (materialId == ElementId.InvalidElementId) return;

            //Get the material
            Material material = doc.GetElement(materialId) as Material;

            //Get the color and convert the transparancy
            Color color = material.Color;
            int transparency0To100 = material.Transparency;
            uint transparency0To255 = (uint)((float)transparency0To100 / 100f * 255f);

            //Set the values of refs
            colorRGBA = new ColorWithTransparency(color.Red, color.Green, color.Blue, transparency0To255);
            if (transparency0To255 > 0)
            {
                isTransparent = true;
            }
        }
    }
}
