// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Assets/Services/ImageDimensionReader.cs
// Version : 1.0.0
// Latest commit: feature/assets-module
// Author : Gerrah
// Purpose : Reads width/height from PNG and JPEG headers without decoding the image.

using System.Buffers.Binary;

namespace TeamSamsara.Modules.Assets.Services;

public static class ImageDimensionReader
{
    #region Fields

    private static readonly byte[] _pngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    #endregion

    #region Public Methods

    // Reads an image's dimensions from its header. Returns (null, null) if the bytes
    // aren't a recognized PNG or JPEG.
    public static (int? Width, int? Height) TryRead(byte[] bytes)
    {
        if (TryReadPng(bytes, out var pngDimensions))
        {
            return pngDimensions;
        }

        if (TryReadJpeg(bytes, out var jpegDimensions))
        {
            return jpegDimensions;
        }

        return (null, null);
    }

    #endregion

    #region Private Methods

    // PNG dimensions sit at a fixed offset: 8-byte signature, then the IHDR chunk's
    // 4-byte length and 4-byte name, then a 4-byte width and 4-byte height, both big-endian.
    private static bool TryReadPng(byte[] bytes, out (int? Width, int? Height) dimensions)
    {
        dimensions = (null, null);

        if (bytes.Length < 24 || !bytes.AsSpan(0, 8).SequenceEqual(_pngSignature))
        {
            return false;
        }

        var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));

        dimensions = ((int)width, (int)height);
        return true;
    }

    // JPEG stores dimensions inside its Start Of Frame (SOF) segment. We scan the
    // marker segments from the start of the file until we find one.
    private static bool TryReadJpeg(byte[] bytes, out (int? Width, int? Height) dimensions)
    {
        dimensions = (null, null);

        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        {
            return false;
        }

        var offset = 2;

        while (offset + 3 < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            var marker = bytes[offset + 1];
            offset += 2;

            // Markers with no length-prefixed payload: SOI, standalone markers, restart markers.
            if (marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
            {
                continue;
            }

            if (marker == 0xD9 || offset + 1 >= bytes.Length)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));

            if (IsStartOfFrameMarker(marker) && offset + 7 < bytes.Length)
            {
                var height = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 3, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 5, 2));

                dimensions = (width, height);
                return true;
            }

            offset += segmentLength;
        }

        return false;
    }

    // SOF markers are 0xC0-0xCF, excluding DHT (0xC4), JPG (0xC8), and DAC (0xCC),
    // which share that range but aren't frame headers.
    private static bool IsStartOfFrameMarker(byte marker) =>
        marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;

    #endregion
}
