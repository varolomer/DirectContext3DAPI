using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectContext3DAPI
{
    public class CustomServer : IDirectContext3DServer
    {
        #region Fields
        private Guid m_guid;
        private UIDocument m_uiDocument;
        private Random m_random = new Random();
        private double m_CubeLength = 0;


        private CustomBufferStorage m_FaceBufferStorage; //Not transparent
        private CustomBufferStorage m_edgeBufferStorage;

        public Document Document
        {
            get { return (m_uiDocument != null) ? m_uiDocument.Document : null; }
        }

        public static Outline boundingBox = new Outline(new XYZ(-50,50,-50), new XYZ(50,50,50));
        #endregion

        #region Constructor
        public CustomServer(UIDocument uiDoc)
        {
            m_guid = Guid.NewGuid();
            m_uiDocument = uiDoc;
            m_CubeLength = m_random.Next(1, 12);

            //Set bounding box
            boundingBox = new Outline(new XYZ(0, 0, 0), new XYZ(m_CubeLength, m_CubeLength, m_CubeLength));

        }
        #endregion

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
            if (view.ViewType != ViewType.ThreeD)
                return false;

            return true;
        }

        public Outline GetBoundingBox(View dBView)
        {
            return boundingBox;
        }

        public void RenderScene(View dBView, DisplayStyle displayStyle)
        {
            try
            {
                // Populate geometry buffers if they are not initialized or need updating.
                if (m_FaceBufferStorage == null || m_FaceBufferStorage.needsUpdate(displayStyle) ||
                    m_edgeBufferStorage == null || m_edgeBufferStorage.needsUpdate(displayStyle))
                {
                    CreateBufferStorageForMesh(displayStyle);
                }

                // Submit a subset of the geometry for drawing. Determine what geometry should be submitted based on
                // the type of the rendering pass (opaque or transparent) and DisplayStyle (wireframe or shaded).

                // If the server is requested to submit transparent geometry, DrawContext().IsTransparentPass()
                // will indicate that the current rendering pass is for transparent objects.

                // Conditionally submit triangle primitives (for non-wireframe views).
                if (displayStyle != DisplayStyle.Wireframe && m_FaceBufferStorage.PrimitiveCount > 0)
                    DrawContext.FlushBuffer(m_FaceBufferStorage.VertexBuffer,
                                            m_FaceBufferStorage.VertexBufferCount,
                                            m_FaceBufferStorage.IndexBuffer,
                                            m_FaceBufferStorage.IndexBufferCount,
                                            m_FaceBufferStorage.VertexFormat,
                                            m_FaceBufferStorage.EffectInstance, PrimitiveType.TriangleList, 0,
                                            m_FaceBufferStorage.PrimitiveCount);

                // Conditionally submit line segment primitives.
                if (m_edgeBufferStorage.PrimitiveCount > 0)
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

        private void CreateBufferStorageForMesh(DisplayStyle displayStyle)
        {
            //Initialize the buffer storages
            m_FaceBufferStorage = new CustomBufferStorage(displayStyle);
            m_edgeBufferStorage = new CustomBufferStorage(displayStyle);

            //Read and get mesh info
            MeshCube customMesh = new MeshCube(m_CubeLength);

            //Populate face buffer
            m_FaceBufferStorage.VertexBufferCount += customMesh.VertexBufferCount;
            m_FaceBufferStorage.PrimitiveCount += customMesh.NumTriangles;

            //Set format bits
            m_FaceBufferStorage.FormatBits = VertexFormatBits.PositionNormalColored;

            #region VertexBuffer
            //Set the buffer size -- The format of the vertices determines the size of the vertex buffer.
            int vertexBufferSizeInFloats = VertexPositionNormalColored.GetSizeInFloats() * m_FaceBufferStorage.VertexBufferCount;

            //Create Vertex Buffer and map so that the vertex data can be written into it
            m_FaceBufferStorage.VertexBuffer = new VertexBuffer(vertexBufferSizeInFloats);
            m_FaceBufferStorage.VertexBuffer.Map(vertexBufferSizeInFloats);

            //A VertexStream is used to write data into a VertexBuffer.
            VertexStreamPositionNormalColored vertexStream = m_FaceBufferStorage.VertexBuffer.GetVertexStreamPositionNormalColored();

            int index = 0;
            foreach (XYZ vertex in customMesh.Vertices)
            {
                XYZ normal = customMesh.Normals[index];

                vertexStream.AddVertex(new VertexPositionNormalColored(vertex, normal, new ColorWithTransparency(204,153,33,0)));

                index++;
            }

            //Unmap
            m_FaceBufferStorage.VertexBuffer.Unmap();
            #endregion

            #region IndexBuffer

            //Get the size of the index buffer
            m_FaceBufferStorage.IndexBufferCount = customMesh.NumTriangles * IndexTriangle.GetSizeInShortInts();
            int indexBufferSizeInShortInts = 1 * m_FaceBufferStorage.IndexBufferCount;

            //Create Index Buffer and map so that the vertex data can be written into it
            m_FaceBufferStorage.IndexBuffer = new IndexBuffer(indexBufferSizeInShortInts);
            m_FaceBufferStorage.IndexBuffer.Map(indexBufferSizeInShortInts);
            
            // An IndexStream is used to write data into an IndexBuffer.
            IndexStreamTriangle indexStream = m_FaceBufferStorage.IndexBuffer.GetIndexStreamTriangle();

            foreach(Index3d indexTri in customMesh.Triangles)
            {
                indexStream.AddTriangle(new IndexTriangle(indexTri.a, indexTri.b, indexTri.c));
            }

            //Unmap the buffers so they can be used for rendering
            m_FaceBufferStorage.IndexBuffer.Unmap();
            #endregion

            // VertexFormat is a specification of the data that is associated with a vertex (e.g., position)
            m_FaceBufferStorage.VertexFormat = new VertexFormat(m_FaceBufferStorage.FormatBits);

            // Effect instance is a specification of the appearance of geometry. For example, it may be used to specify color, if there is no color information provided with the vertices.
            m_FaceBufferStorage.EffectInstance = new EffectInstance(m_FaceBufferStorage.FormatBits);

            //https://bassemtodary.wordpress.com/2013/04/13/ambient-diffuse-specular-and-emissive-lighting/
            m_FaceBufferStorage.EffectInstance.SetColor(new Color(0, 255, 0));
            m_FaceBufferStorage.EffectInstance.SetEmissiveColor(new Color(255, 0, 0));
            m_FaceBufferStorage.EffectInstance.SetSpecularColor(new Color(0, 0, 255));
            m_FaceBufferStorage.EffectInstance.SetAmbientColor(new Color(255, 255, 0));



            //------------------------- Edges ------------

            //Populate edge buffer
            m_edgeBufferStorage.VertexBufferCount += customMesh.VertexBufferCount; //The same vertex buffer count
            m_edgeBufferStorage.PrimitiveCount += customMesh.DistinctEdgeCount;

            //Set format bits
            m_edgeBufferStorage.FormatBits = VertexFormatBits.Position;

            //Set the buffer size
            int edgeVertexBufferSizeInFloats = VertexPosition.GetSizeInFloats() * m_edgeBufferStorage.VertexBufferCount;


            #region Vertex Buffer
            //Create Vertex Buffer and map so that the vertex data can be written into it
            m_edgeBufferStorage.VertexBuffer = new VertexBuffer(edgeVertexBufferSizeInFloats);
            m_edgeBufferStorage.VertexBuffer.Map(edgeVertexBufferSizeInFloats);

            VertexStreamPosition vertexStreamEdge = m_edgeBufferStorage.VertexBuffer.GetVertexStreamPosition();
            foreach (XYZ vertex in customMesh.Vertices)
            {
                vertexStreamEdge.AddVertex(new VertexPosition(vertex));
            }
            //Unmap the buffers so they can be used for rendering
            m_edgeBufferStorage.VertexBuffer.Unmap();

            #endregion

            #region IndexBuffer
            //Get the size of the index buffer
            m_edgeBufferStorage.IndexBufferCount = m_edgeBufferStorage.PrimitiveCount * IndexLine.GetSizeInShortInts();
            int indexEdgeBufferSizeInShortInts = 1 * m_edgeBufferStorage.IndexBufferCount;

            m_edgeBufferStorage.IndexBuffer = new IndexBuffer(indexEdgeBufferSizeInShortInts);
            m_edgeBufferStorage.IndexBuffer.Map(indexEdgeBufferSizeInShortInts);

            IndexStreamLine indexStreamEdge = m_edgeBufferStorage.IndexBuffer.GetIndexStreamLine();

            foreach(Index2d edge in customMesh.DistinctEdges)
            {
                // Add two indices that define a line segment.
                indexStreamEdge.AddLine(new IndexLine(edge.a, edge.b)); //A and be points to the vertices of the edge
            }

            //Unmap the buffer so they can be used for rendering
            m_edgeBufferStorage.IndexBuffer.Unmap();

            #endregion

            //VertexFormat and Effect Instance
            m_edgeBufferStorage.VertexFormat = new VertexFormat(m_edgeBufferStorage.FormatBits);
            m_edgeBufferStorage.EffectInstance = new EffectInstance(m_edgeBufferStorage.FormatBits);
        }

        private void ProcessEdges(CustomBufferStorage m_edgeBufferStorage)
        {
            throw new NotImplementedException();
        }

        private void ProcessFaces(CustomBufferStorage m_FaceBufferStorage)
        {
            throw new NotImplementedException();
        }
    }
}
