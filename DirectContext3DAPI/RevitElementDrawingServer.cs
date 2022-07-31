using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.DirectContext3D;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.ExternalService;

namespace DirectContext3DAPI
{
    /// <summary>
    /// DirectContext Server for drawing the 3D representation of selected objects
    /// </summary>
    public class RevitElementDrawingServer : IDirectContext3DServer
    {
        private Guid m_guid;
        private Element m_element;
        private XYZ m_offset;
        private UIDocument m_uiDocument;

        private RenderingPassBufferStorage m_nonTransparentFaceBufferStorage;
        private RenderingPassBufferStorage m_transparentFaceBufferStorage;
        private RenderingPassBufferStorage m_edgeBufferStorage;

        public Document Document
        {
            get { return (m_uiDocument != null) ? m_uiDocument.Document : null; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RevitElementDrawingServer(UIDocument uiDoc, Element elem, XYZ offset)
        {
            m_guid = Guid.NewGuid();

            m_uiDocument = uiDoc;
            m_element = elem;
            m_offset = offset;
        }

        #region Trivial Fields
        public System.Guid GetServerId() { return m_guid; }
        public System.String GetVendorId() { return "GenFusions"; }
        public ExternalServiceId GetServiceId() { return ExternalServices.BuiltInExternalServices.DirectContext3DService; }
        public System.String GetName() { return "Revit Element Drawing Server"; }
        public System.String GetDescription() { return "Duplicates graphics from a Revit element."; }
        #endregion

        #region Not Used
        public System.String GetApplicationId() { return ""; }
        public System.String GetSourceId() { return ""; }
        public bool UsesHandles() { return false; }
        #endregion

        /// <summary>
        /// Indicates that this server will submit geometry during the rendering pass for transparent geometry.
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool UseInTransparentPass(Autodesk.Revit.DB.View view) { return true; }

        /// <summary>
        /// Tests whether this server should be invoked for the given view. 
        /// The server only wants to be invoked for 3D views that are part 
        /// of the document that contains the element in m_element.
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool CanExecute(View view)
        {
            if (!m_element.IsValidObject)
                return false;
            if (view.ViewType != ViewType.ThreeD)
                return false;

            Document doc = view.Document;
            Document otherDoc = m_element.Document;
            return doc.Equals(otherDoc);
        }

        public Outline GetBoundingBox(View dBView)
        {
            try
            {
                BoundingBoxXYZ boundingBox = m_element.get_BoundingBox(null);

                Outline outline = new Outline(boundingBox.Min + m_offset, boundingBox.Max + m_offset);

                return outline;
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
                throw;
            }
        }

        public void RenderScene(View dBView, DisplayStyle displayStyle)
        {
            try
            {
                // Populate geometry buffers if they are not initialized or need updating.
                if (m_nonTransparentFaceBufferStorage == null || m_nonTransparentFaceBufferStorage.needsUpdate(displayStyle) ||
                    m_transparentFaceBufferStorage == null || m_transparentFaceBufferStorage.needsUpdate(displayStyle) ||
                    m_edgeBufferStorage == null || m_edgeBufferStorage.needsUpdate(displayStyle))
                {
                    Options options = new Options();
                    GeometryElement geomElem = m_element.get_Geometry(options);

                    CreateBufferStorageForElement(geomElem, displayStyle);
                }

                // Submit a subset of the geometry for drawing. Determine what geometry should be submitted based on
                // the type of the rendering pass (opaque or transparent) and DisplayStyle (wireframe or shaded).

                // If the server is requested to submit transparent geometry, DrawContext().IsTransparentPass()
                // will indicate that the current rendering pass is for transparent objects.
                RenderingPassBufferStorage faceBufferStorage = DrawContext.IsTransparentPass() ? m_transparentFaceBufferStorage : m_nonTransparentFaceBufferStorage;

                // Conditionally submit triangle primitives (for non-wireframe views).
                if (displayStyle != DisplayStyle.Wireframe && faceBufferStorage.PrimitiveCount > 0)
                    DrawContext.FlushBuffer(faceBufferStorage.VertexBuffer,
                                            faceBufferStorage.VertexBufferCount,
                                            faceBufferStorage.IndexBuffer,
                                            faceBufferStorage.IndexBufferCount,
                                            faceBufferStorage.VertexFormat,
                                            faceBufferStorage.EffectInstance, PrimitiveType.TriangleList, 0,
                                            faceBufferStorage.PrimitiveCount);

                // Conditionally submit line segment primitives.
                if (displayStyle != DisplayStyle.Shading && m_edgeBufferStorage.PrimitiveCount > 0)
                    DrawContext.FlushBuffer(m_edgeBufferStorage.VertexBuffer,
                                            m_edgeBufferStorage.VertexBufferCount,
                                            m_edgeBufferStorage.IndexBuffer,
                                            m_edgeBufferStorage.IndexBufferCount,
                                            m_edgeBufferStorage.VertexFormat,
                                            m_edgeBufferStorage.EffectInstance, PrimitiveType.LineList, 0,
                                            m_edgeBufferStorage.PrimitiveCount);
                
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }
        }

        private void CreateBufferStorageForElement(GeometryElement geomElem, DisplayStyle displayStyle)
        {
            //Initialize the buffer storages
            m_nonTransparentFaceBufferStorage = new RenderingPassBufferStorage(displayStyle);
            m_transparentFaceBufferStorage = new RenderingPassBufferStorage(displayStyle);
            m_edgeBufferStorage = new RenderingPassBufferStorage(displayStyle);

            //Create a list for containing solids inside the geomElem object of the element selected
            List<Solid> allSolids = new List<Solid>();

            //Iterate over GeometryElement and append the solids
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid)
                {
                    Solid solid = (Solid)geomObj;
                    if (solid.Volume > 1e-06)
                        allSolids.Add(solid);
                }
            }

            foreach (Solid solid in allSolids)
            {
                #region Process Faces in the solid
                foreach(Face face in solid.Faces)
                {
                    //If the area is below tol threshold continue
                    if (face.Area <= 1e-06) continue;

                    //Get the mesh of the face
                    Mesh mesh = face.Triangulate();

                    //Declare params with default values
                    bool isTransparent = false;
                    ColorWithTransparency colorRGBA = new ColorWithTransparency(127, 127, 127, 0);

                    //Extract the material info to set the color and alpha
                    Helper.MaterialExtract(m_uiDocument.Document, face.MaterialElementId, ref isTransparent, ref colorRGBA);

                    //Compute face information
                    BoundingBoxUV env = face.GetBoundingBox();
                    UV center = 0.5 * (env.Min + env.Max);
                    XYZ normal = face.ComputeNormal(center);

                    //Create a meshinfo object
                    MeshInfo meshInfo = new MeshInfo(mesh, normal, colorRGBA);

                    //Buffer storage is broken into two by Transparancy criteria
                    if (isTransparent)
                    {
                        m_transparentFaceBufferStorage.Meshes.Add(meshInfo);
                        m_transparentFaceBufferStorage.VertexBufferCount += mesh.Vertices.Count;
                        m_transparentFaceBufferStorage.PrimitiveCount += mesh.NumTriangles;
                    }
                    else
                    {
                        m_nonTransparentFaceBufferStorage.Meshes.Add(meshInfo);
                        m_nonTransparentFaceBufferStorage.VertexBufferCount += mesh.Vertices.Count;
                        m_nonTransparentFaceBufferStorage.PrimitiveCount += mesh.NumTriangles;
                    }
                }

                #endregion

                #region Process Edges in the solid
                foreach(Edge edge in solid.Edges)
                {
                    if (edge.ApproximateLength <= 1e-06) continue;

                    IList<XYZ> xyzs = edge.Tessellate();

                    m_edgeBufferStorage.VertexBufferCount += xyzs.Count;
                    m_edgeBufferStorage.PrimitiveCount += xyzs.Count - 1;
                    m_edgeBufferStorage.EdgeXYZs.Add(xyzs);
                }
                #endregion

                //Fill out buffers with primitives based on the intermediate information about faces and edges.
                ProcessFaces(m_nonTransparentFaceBufferStorage);
                ProcessFaces(m_transparentFaceBufferStorage);
                ProcessEdges(m_edgeBufferStorage);
            }
        }
        /// <summary>
        /// Fill the buffers with Faces
        /// </summary>
        /// <param name="bufferStorage"></param>
        private void ProcessFaces(RenderingPassBufferStorage bufferStorage)
        {
            //Get the list of meshes
            List<MeshInfo> meshes = bufferStorage.Meshes;

            //Return if there is no mesh
            if (meshes.Count == 0) return;

            //This will be used for pairing index buffer with vertext buffer
            List<int> numVerticesInMeshesBefore = new List<int>();
            numVerticesInMeshesBefore.Add(0);

            //If the display style is shading we use normals 
            bool useNormals = bufferStorage.DisplayStyle == DisplayStyle.Shading || bufferStorage.DisplayStyle == DisplayStyle.ShadingWithEdges;

            #region Vertex Format Explanation
            // Vertex attributes are stored sequentially in vertex buffers. The attributes can include position, normal vector, and color.
            // All vertices within a vertex buffer must have the same format. Possible formats are enumerated by VertexFormatBits.
            // Vertex format also determines the type of rendering effect that can be used with the vertex buffer. In this sample,
            // the color is always encoded in the vertex attributes.
            #endregion

            //Set format bits
            bufferStorage.FormatBits = useNormals ? VertexFormatBits.PositionNormalColored : VertexFormatBits.PositionColored;

            //Set the buffer size -- The format of the vertices determines the size of the vertex buffer.
            int vertexBufferSizeInFloats = (useNormals ? VertexPositionNormalColored.GetSizeInFloats() : VertexPositionColored.GetSizeInFloats()) * bufferStorage.VertexBufferCount;

            //Create Vertex Buffer and map so that the vertex data can be written into it
            bufferStorage.VertexBuffer = new VertexBuffer(vertexBufferSizeInFloats);
            bufferStorage.VertexBuffer.Map(vertexBufferSizeInFloats);

            #region Write Into Vertex Buffer

            if (useNormals)
            {
                // A VertexStream is used to write data into a VertexBuffer.
                VertexStreamPositionNormalColored vertexStream = bufferStorage.VertexBuffer.GetVertexStreamPositionNormalColored();

                foreach(MeshInfo meshInfo in meshes)
                {
                    Mesh mesh = meshInfo.Mesh;
                    foreach(XYZ vertex in mesh.Vertices)
                    {
                        vertexStream.AddVertex(new VertexPositionNormalColored(vertex + m_offset, meshInfo.Normal, meshInfo.ColorWithTransparency));
                    }
                    numVerticesInMeshesBefore.Add(numVerticesInMeshesBefore.Last() + mesh.Vertices.Count);
                }
            }

            else
            {
                // A VertexStream is used to write data into a VertexBuffer.
                VertexStreamPositionColored vertexStream = bufferStorage.VertexBuffer.GetVertexStreamPositionColored();
                foreach (MeshInfo meshInfo in meshes)
                {
                    Mesh mesh = meshInfo.Mesh;

                    //Make the color of all faces white in HLR
                    ColorWithTransparency color = (bufferStorage.DisplayStyle == DisplayStyle.HLR) ? new ColorWithTransparency(255, 255, 255, meshInfo.ColorWithTransparency.GetTransparency()) : meshInfo.ColorWithTransparency;
                    
                    foreach (XYZ vertex in mesh.Vertices)
                    {
                        vertexStream.AddVertex(new VertexPositionColored(vertex + m_offset, color));
                    }

                    numVerticesInMeshesBefore.Add(numVerticesInMeshesBefore.Last() + mesh.Vertices.Count);
                }
            }
            //Unmap the buffers so they can be used for rendering
            bufferStorage.VertexBuffer.Unmap();
            #endregion

            //Index of mesh
            int meshNumber = 0;

            //Get the size of the index buffer
            bufferStorage.IndexBufferCount = bufferStorage.PrimitiveCount * IndexTriangle.GetSizeInShortInts();
            int indexBufferSizeInShortInts = 1 * bufferStorage.IndexBufferCount;

            #region Write Into Index Buffer
            // Primitives are specified using a pair of vertex and index buffers. An index buffer contains a sequence of indices into
            // the associated vertex buffer, each index referencing a particular vertex.

            //Create Index Buffer and map so that the vertex data can be written into it
            bufferStorage.IndexBuffer = new IndexBuffer(indexBufferSizeInShortInts);
            bufferStorage.IndexBuffer.Map(indexBufferSizeInShortInts);
            {
                // An IndexStream is used to write data into an IndexBuffer.
                IndexStreamTriangle indexStream = bufferStorage.IndexBuffer.GetIndexStreamTriangle();

                //Iterate through all the meshes
                foreach (MeshInfo meshInfo in meshes)
                {
                    Mesh mesh = meshInfo.Mesh;
                    int startIndex = numVerticesInMeshesBefore[meshNumber];

                    //Iterate through all the triangles in one mesh
                    for (int i = 0; i < mesh.NumTriangles; i++)
                    {
                        MeshTriangle mt = mesh.get_Triangle(i);

                        //Add three indices that define a triangle.
                        indexStream.AddTriangle(new IndexTriangle((int)(startIndex + mt.get_Index(0)), (int)(startIndex + mt.get_Index(1)), (int)(startIndex + mt.get_Index(2))));
                    }
                    meshNumber++;
                }
            }
            //Unmap the buffers so they can be used for rendering
            bufferStorage.IndexBuffer.Unmap();
            #endregion

            // VertexFormat is a specification of the data that is associated with a vertex (e.g., position)
            bufferStorage.VertexFormat = new VertexFormat(bufferStorage.FormatBits);

            // Effect instance is a specification of the appearance of geometry. For example, it may be used to specify color, if there is no color information provided with the vertices.
            bufferStorage.EffectInstance = new EffectInstance(bufferStorage.FormatBits);
        }

        /// <summary>
        /// Fill the buffers with Edges
        /// </summary>
        /// <param name="m_edgeBufferStorage"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ProcessEdges(RenderingPassBufferStorage bufferStorage)
        {
            //Get the list of edges
            List<IList<XYZ>> edges = bufferStorage.EdgeXYZs;

            //Return if there are no edges
            if(edges.Count == 0 ) return;

            // Edges are encoded as line segment primitives whose vertices contain only position information.
            bufferStorage.FormatBits = VertexFormatBits.Position;

            //Set the buffer size
            int edgeVertexBufferSizeInFloats = VertexPosition.GetSizeInFloats() * bufferStorage.VertexBufferCount;

            //This will be used for pairing index buffer with vertext buffer
            List<int> numVerticesInEdgesBefore = new List<int>();
            numVerticesInEdgesBefore.Add(0);

            #region Write to VertexBuffer

            //Create Vertex Buffer and map so that the vertex data can be written into it
            bufferStorage.VertexBuffer = new VertexBuffer(edgeVertexBufferSizeInFloats);
            bufferStorage.VertexBuffer.Map(edgeVertexBufferSizeInFloats);
            {
                VertexStreamPosition vertexStream = bufferStorage.VertexBuffer.GetVertexStreamPosition();
                foreach (IList<XYZ> xyzs in edges)
                {
                    foreach (XYZ vertex in xyzs)
                    {
                        vertexStream.AddVertex(new VertexPosition(vertex + m_offset));
                    }

                    numVerticesInEdgesBefore.Add(numVerticesInEdgesBefore.Last() + xyzs.Count);
                }
            }
            //Unmap the buffers so they can be used for rendering
            bufferStorage.VertexBuffer.Unmap();

            #endregion

            //Index of edge
            int edgeNumber = 0;

            //Get the size of the index buffer
            bufferStorage.IndexBufferCount = bufferStorage.PrimitiveCount * IndexLine.GetSizeInShortInts();
            int indexBufferSizeInShortInts = 1 * bufferStorage.IndexBufferCount;

            #region Write to IndexBuffer
            //Create Index Buffer and map so that the vertex data can be written into it
            bufferStorage.IndexBuffer = new IndexBuffer(indexBufferSizeInShortInts);
            bufferStorage.IndexBuffer.Map(indexBufferSizeInShortInts);
            {
                IndexStreamLine indexStream = bufferStorage.IndexBuffer.GetIndexStreamLine();
                foreach (IList<XYZ> xyzs in edges)
                {
                    int startIndex = numVerticesInEdgesBefore[edgeNumber];
                    for (int i = 1; i < xyzs.Count; i++)
                    {
                        // Add two indices that define a line segment.
                        indexStream.AddLine(new IndexLine((int)(startIndex + i - 1), (int)(startIndex + i)));
                    }
                    edgeNumber++;
                }
            }
            //Unmap the buffer so they can be used for rendering
            bufferStorage.IndexBuffer.Unmap();
            #endregion

            //VertexFormat and Effect Instance
            bufferStorage.VertexFormat = new VertexFormat(bufferStorage.FormatBits);
            bufferStorage.EffectInstance = new EffectInstance(bufferStorage.FormatBits);

        }
    }
}
