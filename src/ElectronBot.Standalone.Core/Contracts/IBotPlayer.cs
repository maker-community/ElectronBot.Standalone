using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ElectronBot.Standalone.Core.Contracts;

public interface IBotPlayer
{
    Task<bool> ShowImageToMainScreenAsync(Image<Bgra32> image);

    Task<bool> ShowImageToSubScreenAsync(Image<Bgra32> image);

    Task PlayEmojiToMainScreenAsync(string emojiName);

    Task PlayJointAnglesAsync(float j1, float j2, float j3, float j4, float j5, float j6, bool enable = false);
    Task PlayAudioByFileAsync(string fileName);
    Task StopAudioAsync();
}
