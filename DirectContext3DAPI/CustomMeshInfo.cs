using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectContext3DAPI
{
    public class MeshCube
    {
        public int VertexBufferCount;
        public int NumTriangles;
        public int EdgeCount;
        public int DistinctEdgeCount;

        public List<XYZ> Vertices;
        public List<XYZ> Normals;
        public List<Index3d> Triangles;
        public List<Index2d> Edges;
        public List<Index2d> DistinctEdges;

        public MeshCube(double a) //A is cube length
        {
            Vertices = new List<XYZ>()
            {
                //Surface 1
                new XYZ(0,0,0), //0
                new XYZ(a,0,0), //1
                new XYZ(0,0,a), //2
                new XYZ(a,0,a), //3

                //Surface 2
                new XYZ(0,0,0), //4
                new XYZ(0,a,0), //5
                new XYZ(0,0,a), //6
                new XYZ(0,a,a), //7

                //Surface 3
                new XYZ(a,0,0), //8
                new XYZ(a,a,0), //9
                new XYZ(a,0,a), //10
                new XYZ(a,a,a), //11

                //Surface 4
                new XYZ(0,a,0), //12
                new XYZ(a,a,0), //13
                new XYZ(0,a,a), //14
                new XYZ(a,a,a), //15

                //Surface a
                new XYZ(0,0,0), //16
                new XYZ(a,0,0), //17
                new XYZ(0,a,0), //18
                new XYZ(a,a,0), //19

                //Surface 6
                new XYZ(0,0,a), //20
                new XYZ(0,a,a), //21
                new XYZ(a,0,a), //22
                new XYZ(a,a,a), //23
            };

            Normals = new List<XYZ>()
            {
                //Surface 1 
                new XYZ(0,-1,0), //0
                new XYZ(0,-1,0), //1
                new XYZ(0,-1,0), //2
                new XYZ(0,-1,0), //3

                //Surface 2
                new XYZ(-1,0,0), //4
                new XYZ(-1,0,0), //5
                new XYZ(-1,0,0), //6
                new XYZ(-1,0,0), //7

                //Surface 3
                new XYZ(0,1,0), //8
                new XYZ(0,1,0), //9
                new XYZ(0,1,0), //10
                new XYZ(0,1,0), //11

                //Surface 4
                new XYZ(0,1,0), //12
                new XYZ(0,1,0), //13
                new XYZ(0,1,0), //14
                new XYZ(0,1,0), //15

                //Surface 5
                new XYZ(0,0,-1), //16
                new XYZ(0,0,-1), //17
                new XYZ(0,0,-1), //18
                new XYZ(0,0,-1), //19

                //Surface 6
                new XYZ(0,0,1), //20
                new XYZ(0,0,1), //21
                new XYZ(0,0,1), //22
                new XYZ(0,0,1), //23
            };

            Triangles = new List<Index3d>()
            {
                //Surface 1
                new Index3d(0,1,2),
                new Index3d(3,1,2),

                //Surface 2
                new Index3d(4,5,6),
                new Index3d(7,5,6),

                //Surface 3
                new Index3d(8,9,10),
                new Index3d(11,9,10),

                //Surface 4
                new Index3d(12,13,14),
                new Index3d(15,13,14),

                //Surface 5
                new Index3d(16,17,18),
                new Index3d(19,17,18),

                //Surface 6
                new Index3d(20,21,22),
                new Index3d(23,21,22),

            };

            Edges = new List<Index2d>();

            foreach (XYZ vertex1 in Vertices)
            {
                foreach (XYZ vertex2 in Vertices)
                {
                    if (vertex1.DistanceTo(vertex2) == a)
                    {
                        int index1 = Vertices.IndexOf(vertex1);
                        int index2 = Vertices.IndexOf(vertex2);

                        Edges.Add(new Index2d(index1, index2));
                    }
                }
            }

            DistinctEdges = Edges.Distinct().ToList();


            VertexBufferCount = Vertices.Count();
            NumTriangles = Triangles.Count();
            EdgeCount = Edges.Count();
            DistinctEdgeCount = Edges.Count();

        }
    }

    public struct Index2d : IEquatable<Index2d>
    {
        public int a;
        public int b;

        public Index2d(int a, int b)
        {
            this.a = a;
            this.b = b;
        }

        //public override bool Equals(object obj)
        //{
        //    if (!(obj is Index2d item)) return false;

        //    return (this.a + this.b) == (item.a + item.b);
        //}

        public bool Equals(Index2d other)
        {
            return (this.a + this.b) == (other.a + other.b);
        }
    }

    public struct Index3d
    {
        public int a;
        public int b;
        public int c;

        public Index3d(int a, int b, int c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }
}
