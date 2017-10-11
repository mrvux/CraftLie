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
using VL.Lib.Collections;

namespace CraftLie
{
    public enum RawBufferDescriptionDataType
    {
        IntPtr,
        Array,
        Spread
    }

    public class DynamicRawBufferDescription
    {
        public readonly RawBufferDescriptionDataType DataType;
        public readonly long DataSizeInBytes;

        internal DynamicRawBufferDescription(long dataSizeInBytes, RawBufferDescriptionDataType dataType)
        {
            DataSizeInBytes = Math.Max(dataSizeInBytes, 0);
            DataType = dataType;
        }

        public virtual IntPtr GetDataPointer() => IntPtr.Zero;
        public virtual Array GetDataArray() => new byte[0];
    }

    public class DynamicRawBufferDescriptionIntPtr : DynamicRawBufferDescription
    {
        public readonly IntPtr Data;

        public DynamicRawBufferDescriptionIntPtr(IntPtr data, long dataSizeInBytes)
            : base(dataSizeInBytes, RawBufferDescriptionDataType.IntPtr)
        {
            Data = data;
        }

        public override IntPtr GetDataPointer() => Data;
    }

    public class DynamicRawBufferDescriptionArray : DynamicRawBufferDescription
    {
        public readonly byte[] Data;

        public DynamicRawBufferDescriptionArray(byte[] data)
            : base(data.LongLength, RawBufferDescriptionDataType.Array)
        {
            Data = data;
        }

        public override Array GetDataArray() => Data;
    }

    public class DynamicRawBufferDescriptionSpread : DynamicRawBufferDescription
    {
        public readonly Spread<byte> Data;

        public DynamicRawBufferDescriptionSpread(Spread<byte> data)
            : base(data.Count, RawBufferDescriptionDataType.Spread)
        {
            Data = data;
        }

        public override Array GetDataArray() => Data.GetInternalArray();
    }
}