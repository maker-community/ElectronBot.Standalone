using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ElectronBot.Standalone.Core.Models;
public class LottieFrameEventArgs
{
    public byte[] FrameData
    {
        get; set;
    } = [];

    public int Width
    {
        get; set;
    }

    public int Height
    {
        get; set;
    }
}
