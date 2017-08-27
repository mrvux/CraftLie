﻿using FeralTic.DX11;
using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace CraftLie
{
    [Type]
    public enum TransformationSpace
    {
        World,
        View,
        Projection
    }

    [Type]
    public enum BlendMode
    {
        Disabled,
        Add,
        Blend,
        Multiply,
        AlphaAdd,
        TextDefault,
        Keep
    }

    [Type]
    public class DrawDescription : IDisposable
    {
        public GeometryDescriptor GeometryDescriptor;
        public Matrix Transformation;
        public Color4 Color;
        public TransformationSpace Space = TransformationSpace.World;
        public BlendMode Blending = BlendMode.Blend;
        public RectangleF ClipRect = new RectangleF { Left = -1, Top = -1, Right = 1, Bottom = 1 };
        public string TexturePath;
        public int LayerOrder;

        public IDX11Geometry GetGeometry(DX11RenderContext context)
        {
            IDX11Geometry geo;
            if (!GeometryCache.TryGetValue(context, out geo))
            {
                geo = PrimitiveFactory.GetGeometry(context, GeometryDescriptor);
                GeometryCache[context] = geo;
            }

            return geo;
        }

        [Node] 
        public void SetSpace(TransformationSpace space)
        {
            Space = space;
        }

        [Node]
        public void SetClipRect(RectangleF clipRect)
        {
            ClipRect = clipRect;
        }

        [Node] 
        public void Transform(Matrix transformation)
        {
            Matrix.Multiply(ref Transformation, ref transformation, out Transformation);

            //var pos = new Vector4(ClipRect.X, ClipRect.Y, 0, 1);
            //var size = new Vector4(ClipRect.Width, ClipRect.Height, 0, 0);

            //Vector4.Transform(ref pos, ref transformation, out pos);
            //Vector4.Transform(ref size, ref transformation, out size);

            //ClipRect = new RectangleF(pos.X, pos.Y, size.X, size.Y);
        }

        [Node]
        public void Dispose()
        {
            DisposeGeometry();
        }

        protected void DisposeGeometry()
        {
            try
            {
                foreach (var geo in GeometryCache.Values)
                {
                    try
                    {
                        geo?.Dispose();
                    }
                    catch (Exception)
                    {
                        //safe dispose
                    }
                }

                GeometryCache.Clear();
            }
            catch (Exception)
            {
                //safe dispose
            }
        }

        readonly Dictionary<DX11RenderContext, IDX11Geometry> GeometryCache = new Dictionary<DX11RenderContext, IDX11Geometry>();
    }

}
