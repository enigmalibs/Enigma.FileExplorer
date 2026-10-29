using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Enigma.FileExplorer.Desktop.UnitTests.Infrastructure;
using Enigma.FileExplorer.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enigma.FileExplorer.Desktop.UnitTests;

/// <summary>
/// The application icon as the assembly ships it: the ten frames of <c>app.ico</c>, the copy of its
/// largest frame the About dialog shows, and the window that carries it.
/// </summary>
/// <remarks>
/// The frames are rendered from <c>packaging/icon/enigma-file-explorer.svg</c> and packed by the Branding
/// repository's export tool, so these pin what a regeneration has to keep: the sizes, the encoding, and
/// the copy being a copy.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class AppIconTests(HeadlessAvaloniaFixture fixture)
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void TheIcon_HoldsTenSquare32BitPngFramesFrom16To256()
    {
        fixture.Run(() =>
        {
            byte[] icon = ReadAsset("app.ico");
            IReadOnlyList<IconFrame> frames = ReadFrames(icon);

            // 20, 24 and 40 are the frames Windows picks at 125 % and 150 % display scaling.
            Assert.Equal([16, 20, 24, 32, 40, 48, 64, 96, 128, 256], frames.Select(frame => frame.Size));

            foreach (IconFrame frame in frames)
            {
                Assert.Equal(32, frame.BitCount);
                Assert.True(frame.Png.AsSpan().StartsWith(PngSignature), $"the {frame.Size} frame is not a PNG");

                // The PNG's own header says the same size the directory does, and RGBA at 8 bits.
                Assert.Equal(frame.Size, BinaryPrimitives.ReadInt32BigEndian(frame.Png.AsSpan(16, 4)));
                Assert.Equal(frame.Size, BinaryPrimitives.ReadInt32BigEndian(frame.Png.AsSpan(20, 4)));
                Assert.Equal(8, frame.Png[24]);
                Assert.Equal(6, frame.Png[25]);
            }
        });
    }

    [Fact]
    public void The256Image_IsTheIconsLargestFrameByteForByte()
    {
        fixture.Run(() =>
        {
            byte[] image = ReadAsset("app-256.png");
            IconFrame largest = ReadFrames(ReadAsset("app.ico"))[^1];

            Assert.Equal(largest.Png, image);

            using MemoryStream stream = new(image);
            using Bitmap bitmap = new(stream);
            Assert.Equal(new PixelSize(256, 256), bitmap.PixelSize);
        });
    }

    [Fact]
    public void TheMainWindow_CarriesTheIcon()
    {
        bool hasIcon = fixture.Run(() =>
        {
            using ServiceProvider provider = CompositionRootTests.BuildProvider();

            return provider.GetRequiredService<MainWindow>().Icon is not null;
        });

        Assert.True(hasIcon, "The main window has no icon: Linux and macOS would show none.");
    }

    private static byte[] ReadAsset(string name)
    {
        using Stream asset = AssetLoader.Open(new Uri($"avares://Enigma.FileExplorer.Desktop/Assets/{name}"));
        using MemoryStream copy = new();
        asset.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>
    /// Reads an .ico's directory: a six-byte header (reserved, type 1, count), then sixteen bytes per
    /// frame, in which a width or height of 0 means 256.
    /// </summary>
    private static List<IconFrame> ReadFrames(byte[] icon)
    {
        ReadOnlySpan<byte> data = icon;
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(data[0..]));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(data[2..]));

        int count = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        List<IconFrame> frames = new(count);

        for (int index = 0; index < count; index++)
        {
            ReadOnlySpan<byte> entry = data.Slice(6 + (16 * index), 16);
            int width = entry[0] == 0 ? 256 : entry[0];
            int height = entry[1] == 0 ? 256 : entry[1];
            Assert.Equal(width, height);

            int length = BinaryPrimitives.ReadInt32LittleEndian(entry[8..]);
            int offset = BinaryPrimitives.ReadInt32LittleEndian(entry[12..]);
            Assert.InRange(offset + length, 0, icon.Length);

            frames.Add(new IconFrame(width, BinaryPrimitives.ReadUInt16LittleEndian(entry[6..]), icon[offset..(offset + length)]));
        }

        return frames;
    }

    private sealed record IconFrame(int Size, int BitCount, byte[] Png);
}
