﻿using FeralTic.DX11;
using FeralTic.DX11.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeralTic.DX11.Geometry;
using SlimDX;
using SlimDX.Direct3D11;

using SharpDX.DirectWrite;
using DWriteFactory = SharpDX.DirectWrite.Factory;
using D2DFactory = SharpDX.Direct2D1.Factory;
using D2DGeometry = SharpDX.Direct2D1.Geometry;

using SharpDX.Direct2D1;

using InputElement = SlimDX.Direct3D11.InputElement;
using System.Runtime.InteropServices;

namespace CraftLie
{
    public static class PrimitiveFactory
    {
        public static IDX11Geometry GetGeometry(DX11RenderContext context, GeometryDescriptor descriptor)
        {
            switch (descriptor.Type)
            {
                case PrimitiveType.Quad:
                    return context.Primitives.QuadNormals(((QuadDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.RoundQuad:
                    return context.Primitives.RoundRect(((RoundQuadDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Box:
                    return context.Primitives.Box(((BoxDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Disc:
                    return context.Primitives.Segment(((DiscDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Polygon:
                    return Polygon2D(context, ((PolygonDescriptor)descriptor).Positions);
                    break;
                case PrimitiveType.Sphere:
                    return context.Primitives.Sphere(((SphereDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Cylinder:
                    return context.Primitives.Cylinder(((CylinderDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Tube:
                    return context.Primitives.SegmentZ(((TubeDescriptor)descriptor).Settings);
                    break;
                case PrimitiveType.Line:
                    var lineDesc = (LineDescriptor)descriptor;
                    return LineStrip3d(context, lineDesc.Positions, lineDesc.Directions, lineDesc.IsClosed);
                    break;
                case PrimitiveType.MeshJoin:
                    var meshDesc = (MeshJoinDescriptor)descriptor;

                    if (meshDesc.Topology == MeshTopology.TriangleList || meshDesc.Topology == MeshTopology.Undefined)
                    {
                        return CreateIndexedGeometry(context, meshDesc.Positions, meshDesc.Directions, meshDesc.Tex, meshDesc.Indices);
                    }
                    else
                    {
                        return CreateVertexGeometry(context, meshDesc.Positions, meshDesc.Directions, meshDesc.Tex, meshDesc.Topology);
                    }
                    break;
                case PrimitiveType.Sprites:
                    return CreateNullGeometry(context);
                    break;
                case PrimitiveType.Text:
                    var textDesc = (TextDescriptor)descriptor;
                    return Text3d(context, textDesc.Text, textDesc.FontName, textDesc.FontSize, textDesc.Extrude, (TextAlignment)textDesc.TextAlignment, (ParagraphAlignment)textDesc.ParagraphAlignment);
                    break;
                default:
                    var settings = new Quad() { Size = new SlimDX.Vector2(1) };
                    return context.Primitives.QuadNormals(settings);
                    break;
            }
        }

        private static IDX11Geometry CreateNullGeometry(DX11RenderContext context)
        {
            DX11NullGeometry geom = new DX11NullGeometry(context);
            geom.Topology = SlimDX.Direct3D11.PrimitiveTopology.Undefined;
            geom.InputLayout = new SlimDX.Direct3D11.InputElement[0];
            geom.HasBoundingBox = false;

            return geom;
        }

        public static IDX11Geometry CreateIndexedGeometry(DX11RenderContext context, List<Vector3> points, List<Vector3> normals, List<Vector2> tex, int[] indices)
        {
            var count = points.Count;
            Pos3Norm3Tex2Vertex[] vertices = new Pos3Norm3Tex2Vertex[count];

            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Pos3Norm3Tex2Vertex()
                {
                    Position = points[i],
                    Normals = normals[i % normals.Count],
                    TexCoords = tex[i % tex.Count]
                };
            }

            DataStream ds = new DataStream(vertices.Length * Pos3Norm3Tex2Vertex.VertexSize, true, true);
            ds.Position = 0;

            ds.WriteRange(vertices);

            ds.Position = 0;

            var vbuffer = new SlimDX.Direct3D11.Buffer(context.Device, ds, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = (int)ds.Length,
                Usage = ResourceUsage.Default
            });

            ds.Dispose();

            var indexstream = new DataStream(indices.Length * 4, true, true);
            indexstream.WriteRange(indices);
            indexstream.Position = 0;

            DX11IndexedGeometry geom = new DX11IndexedGeometry(context);
            geom.VertexBuffer = vbuffer;
            geom.IndexBuffer = new DX11IndexBuffer(context, indexstream, false, true);
            geom.InputLayout = Pos3Norm3Tex2Vertex.Layout;
            geom.Topology = PrimitiveTopology.TriangleList;
            geom.VerticesCount = count;
            geom.VertexSize = Pos3Norm3Tex2Vertex.VertexSize;

            geom.HasBoundingBox = false;

            return geom;
        }

        public static IDX11Geometry Polygon2D(DX11RenderContext context, List<Vector2> points)
        {
            var count = points.Count;
            float cx = 0;
            float cy = 0;
            float x, y;

            float minx = float.MaxValue, miny = float.MaxValue;
            float maxx = float.MinValue, maxy = float.MinValue;

            Pos3Norm3Tex2Vertex[] vertices = new Pos3Norm3Tex2Vertex[count + 1];

            for (int j = 0; j < count; j++)
            {
                var point = points[j];
                x = point.X;
                y = point.Y;
                vertices[j + 1].Position = new Vector3(x, y, 0);
                vertices[j + 1].Normals = new Vector3(0, 0, -1);
                vertices[j + 1].TexCoords = new Vector2(0.0f, 0.0f);
                cx += x;
                cy += y;

                if (x < minx) { minx = x; }
                if (x > maxx) { maxx = x; }
                if (y < miny) { miny = y; }
                if (y > maxy) { maxy = y; }
            }

            vertices[0].Position = new Vector3(cx / count, cy / count, 0);
            vertices[0].Normals = new Vector3(0, 0, -1);

            float w = maxx - minx;
            float h = maxy - miny;
            for (int j = 0; j <= count; j++)
            {
                vertices[j].TexCoords = new Vector2((vertices[j].Position.X - minx) / w,
                     (vertices[j].Position.Y - miny) / h);
            }

            List<int> inds = new List<int>();

            var indices = new int[count * 3];

            var outerJ = 0;
            for (int j = 0; j < count - 1; j++)
            {
                outerJ = j * 3;
                indices[outerJ] = 0;
                indices[outerJ + 1] = j + 1;
                indices[outerJ + 2] = j + 2;
            }

            outerJ = count - 1;
            indices[outerJ] = 0;
            indices[outerJ + 1] = count - 1;
            indices[outerJ + 2] = 1;

            DataStream ds = new DataStream(vertices.Length * Pos3Norm3Tex2Vertex.VertexSize, true, true);
            ds.Position = 0;

            ds.WriteRange(vertices);

            ds.Position = 0;

            var vbuffer = new SlimDX.Direct3D11.Buffer(context.Device, ds, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = (int)ds.Length,
                Usage = ResourceUsage.Default
            });

            ds.Dispose();

            var indexstream = new DataStream(indices.Length * 4, true, true);
            indexstream.WriteRange(indices);
            indexstream.Position = 0;

            DX11IndexedGeometry geom = new DX11IndexedGeometry(context);
            geom.VertexBuffer = vbuffer;
            geom.IndexBuffer = new DX11IndexBuffer(context, indexstream, false, true);
            geom.InputLayout = Pos3Norm3Tex2Vertex.Layout;
            geom.Topology = PrimitiveTopology.TriangleList;
            geom.VerticesCount = count;
            geom.VertexSize = Pos3Norm3Tex2Vertex.VertexSize;

            geom.HasBoundingBox = false;

            return geom;
        }

        public static IDX11Geometry CreateVertexGeometry(DX11RenderContext context, List<Vector3> points, List<Vector3> normals, List<Vector2> tex, MeshTopology topology)
        {
            var count = points.Count;
            Pos3Norm3Tex2Vertex[] vertices = new Pos3Norm3Tex2Vertex[count];

            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Pos3Norm3Tex2Vertex()
                {
                    Position = points[i],
                    Normals = normals[i % normals.Count],
                    TexCoords = tex[i % tex.Count]
                };
            }

            DataStream ds = new DataStream(count * FeralTic.DX11.Geometry.Pos3Norm3Tex2Vertex.VertexSize, true, true);
            ds.Position = 0;
            ds.WriteRange(vertices);
            ds.Position = 0;

            var vbuffer = new SlimDX.Direct3D11.Buffer(context.Device, ds, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = (int)ds.Length,
                Usage = ResourceUsage.Default
            });

            ds.Dispose();

            DX11VertexGeometry geom = new DX11VertexGeometry(context);
            geom.VertexBuffer = vbuffer;
            geom.InputLayout = FeralTic.DX11.Geometry.Pos3Norm3Tex2Vertex.Layout;
            geom.Topology = (PrimitiveTopology)topology;
            geom.VerticesCount = count;
            geom.VertexSize = FeralTic.DX11.Geometry.Pos3Norm3Tex2Vertex.VertexSize;

            geom.HasBoundingBox = false;

            return geom;
        }

        public static DX11VertexGeometry LineStrip3d(DX11RenderContext context, List<Vector3> points, List<Vector3> directions, bool loop)
        {
            //Use direction verctor as normal, useful when we have analytical derivatives for direction
            DX11VertexGeometry geom = new DX11VertexGeometry(context);

            int ptcnt = Math.Max(points.Count, directions.Count);

            int vcount = loop ? ptcnt + 1 : ptcnt;

            FeralTic.DX11.Geometry.Pos3Norm3Tex2Vertex[] verts = new FeralTic.DX11.Geometry.Pos3Norm3Tex2Vertex[vcount];

            float inc = loop ? 1.0f / (float)vcount : 1.0f / ((float)vcount + 1.0f);

            float curr = 0.0f;


            for (int i = 0; i < ptcnt; i++)
            {
                verts[i].Position = points[i % points.Count];
                verts[i].Normals = directions[i % directions.Count];
                verts[i].TexCoords.X = curr;
                curr += inc;
            }

            if (loop)
            {
                verts[ptcnt].Position = points[0];
                verts[ptcnt].Normals = directions[0];
                verts[ptcnt].TexCoords.X = 1.0f;
            }


            DataStream ds = new DataStream(vcount * FeralTic.DX11.Geometry.Pos3Norm3Tex2Vertex.VertexSize, true, true);
            ds.Position = 0;
            ds.WriteRange(verts);
            ds.Position = 0;

            var vbuffer = new SlimDX.Direct3D11.Buffer(context.Device, ds, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = (int)ds.Length,
                Usage = ResourceUsage.Default
            });

            ds.Dispose();

            geom.VertexBuffer = vbuffer;
            geom.InputLayout = FeralTic.DX11.Geometry.Pos3Norm3Tex2Vertex.Layout;
            geom.Topology = PrimitiveTopology.LineStrip;
            geom.VerticesCount = vcount;
            geom.VertexSize = FeralTic.DX11.Geometry.Pos3Norm3Tex2Vertex.VertexSize;

            geom.HasBoundingBox = false;

            return geom;
        }

        private static D2DFactory d2dFactory;
        private static DWriteFactory dwFactory;
        private static Dictionary<string, Dictionary<DX11RenderContext, DX11VertexGeometry>> TextGeometryCache = new Dictionary<string, Dictionary<DX11RenderContext, DX11VertexGeometry>>();

        public static DX11VertexGeometry Text3d(DX11RenderContext device, string text, string fontName, float fontSize, float extrude, TextAlignment textAlignment, ParagraphAlignment paragraphAlignment)
        {

            //Dictionary<DX11RenderContext, DX11VertexGeometry> deviceDict = null;
            //if (TextGeometryCache.TryGetValue(text, out deviceDict))
            //{
            //    DX11VertexGeometry geom;
            //    if(deviceDict.TryGetValue(device, out geom))
            //    {
            //        return geom;
            //    }
            //}

            if (d2dFactory == null)
            {
                d2dFactory = new D2DFactory();
                dwFactory = new DWriteFactory(SharpDX.DirectWrite.FactoryType.Shared);
            }

            TextFormat fmt = new TextFormat(dwFactory, fontName, fontSize);

            TextLayout tl = new TextLayout(dwFactory, text, fmt, 0.0f, .0f);
            tl.WordWrapping = WordWrapping.NoWrap;
            tl.TextAlignment = (SharpDX.DirectWrite.TextAlignment)textAlignment;
            tl.ParagraphAlignment = (SharpDX.DirectWrite.ParagraphAlignment)paragraphAlignment;

            OutlineRenderer renderer = new OutlineRenderer(d2dFactory);
            Extruder ex = new Extruder(d2dFactory);

            tl.Draw(renderer, 0.0f, 0.0f);

            var geo = renderer.GetGeometry();
            var result = ex.GetVertices(geo, extrude).ToArray();           
            
            renderer.Dispose();
            geo.Dispose();
            fmt.Dispose();
            tl.Dispose();

            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            foreach (var pn in result)
            {
                min.X = pn.Position.X < min.X ? pn.Position.X : min.X;
                min.Y = pn.Position.Y < min.Y ? pn.Position.Y : min.Y;
                min.Z = pn.Position.Z < min.Z ? pn.Position.Z : min.Z;

                max.X = pn.Position.X > max.X ? pn.Position.X : max.X;
                max.Y = pn.Position.Y > max.Y ? pn.Position.Y : max.Y;
                max.Z = pn.Position.Z > max.Z ? pn.Position.Z : max.Z;
            }

            SlimDX.DataStream ds = new SlimDX.DataStream(result.Length * Pos3Norm3VertexSDX.VertexSize, true, true);
            ds.Position = 0;

            ds.WriteRange(result);

            ds.Position = 0;

            var vbuffer = new SlimDX.Direct3D11.Buffer(device.Device, ds, new SlimDX.Direct3D11.BufferDescription()
            {
                BindFlags = SlimDX.Direct3D11.BindFlags.VertexBuffer,
                CpuAccessFlags = SlimDX.Direct3D11.CpuAccessFlags.None,
                OptionFlags = SlimDX.Direct3D11.ResourceOptionFlags.None,
                SizeInBytes = (int)ds.Length,
                Usage = SlimDX.Direct3D11.ResourceUsage.Default,
            });

            ds.Dispose();

            DX11VertexGeometry vg = new DX11VertexGeometry(device);
            vg.InputLayout = Pos3Norm3VertexSDX.Layout;
            vg.Topology = SlimDX.Direct3D11.PrimitiveTopology.TriangleList;
            vg.VertexBuffer = vbuffer;
            vg.VertexSize = Pos3Norm3VertexSDX.VertexSize;
            vg.VerticesCount = result.Length;
            vg.HasBoundingBox = true;
            vg.BoundingBox = new SlimDX.BoundingBox(new SlimDX.Vector3(min.X, min.Y, min.Z), new SlimDX.Vector3(max.X, max.Y, max.Z));


            //if(deviceDict != null)
            //{
            //    deviceDict[device] = vg;
            //}
            //else
            //{
            //    deviceDict = new Dictionary<DX11RenderContext, DX11VertexGeometry>();
            //    deviceDict[device] = vg;
            //    TextGeometryCache[text] = deviceDict;
            //}

            return vg;
        }

    }
}
