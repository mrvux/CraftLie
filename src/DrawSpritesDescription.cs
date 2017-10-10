using System;
using System.Linq;
using System.Collections.Generic;
using FeralTic.DX11;
using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using SharpDX;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace CraftLie
{

    public class DrawSpritesDescription : DrawDescription
    {
        public IReadOnlyList<Vector3> Positions;
        public IReadOnlyList<Vector2> Sizes;
        public IReadOnlyList<Color4> Colors;

        static readonly IReadOnlyList<Vector3> NoPositions = new List<Vector3>(0);
        static readonly IReadOnlyList<Vector2> NoSizes = new List<Vector2>(0);
        static readonly IReadOnlyList<Color4> NoColors = new List<Color4>(0);

        static readonly IReadOnlyList<Vector2> DefaultSizes = new List<Vector2>(1) { new Vector2(0.01f) };
        static readonly IReadOnlyList<Color4> DefaultColors = new List<Color4>(1) { Color4.White };

        public static readonly DrawSpritesDescription Default = new DrawSpritesDescription(
            Matrix.Identity,
            BlendMode.Blend,
            NoPositions,
            NoSizes,
            NoColors);

        public int SpriteCount;

        public DrawSpritesDescription()
        {
            GeometryDescriptor = new SpritesDescriptor();
        }

        public void SetLayerOrder(int layerOrder)
        {
            LayerOrder = layerOrder;
        }

        public DrawSpritesDescription(Matrix transformation, BlendMode blendMode, IReadOnlyList<Vector3> positions, IReadOnlyList<Vector2> sizes, IReadOnlyList<Color4> colors, string texturePath = "")
        {
            GeometryDescriptor = new SpritesDescriptor();

            Update(transformation, blendMode, positions, sizes, colors, texturePath);
        }

        public void Update(
            Matrix transformation,
            BlendMode blendMode = BlendMode.Blend,
            IReadOnlyList<Vector3> positions = null, 
            IReadOnlyList<Vector2> sizes = null, 
            IReadOnlyList<Color4> colors = null, 
            string texturePath = "")
        {

            Transformation = transformation;
            TexturePath = texturePath;
            Space = TransformationSpace.World; //always set default, can be changed by Within node
            Blending = blendMode;

            if (positions == null)
                positions = NoPositions;

            if (sizes == null)
            {
                if (positions.Count > 0)
                    sizes = DefaultSizes;
                else
                    sizes = NoSizes;
            }

            if (colors == null)
            {
                if (positions.Count > 0)
                    colors = DefaultColors;
                else
                    colors = NoColors;
            }

            Positions = positions;
            Sizes = sizes;
            Colors = colors;
            SpriteCount = Math.Max(Math.Max(Positions.Count, Sizes.Count), Colors.Count);
        }
    }

}