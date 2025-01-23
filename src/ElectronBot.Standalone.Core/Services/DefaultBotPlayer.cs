using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Models;
using NetCoreAudio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using SkiaSharp.Skottie;
using System.Device.Gpio;
using System.Device.Pwm.Drivers;
using System.Device.Spi;
using System.Runtime.InteropServices;
using System.Text.Json;
using Verdure.Iot.Device;

namespace ElectronBot.Standalone.Core.Services;

public class DefaultBotPlayer : IBotPlayer, IDisposable
{
    private readonly LCD2inch4? _lCD2Inch4;
    private readonly LCD1inch47? _lCD1Inch47;
    private readonly Player _audioPlayer;
    private bool _disposed = false;
    private readonly SemaphoreSlim _emojiSemaphore = new SemaphoreSlim(1, 1);
    private readonly GpioController? _gpioController;
    private readonly int _csPin2Inch4 = 8; // 片选引脚
    private readonly int _csPin1Inch47 = 7; // 片选引脚

    public DefaultBotPlayer()
    {
        _audioPlayer = new Player();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _gpioController = new GpioController();
            _gpioController.OpenPin(_csPin2Inch4, PinMode.Output);
            _gpioController.OpenPin(_csPin1Inch47, PinMode.Output);

            var pwmBacklight = new SoftwarePwmChannel(pinNumber: 18, frequency: 1000);
            pwmBacklight.Start();
            var sender2inch4Device = SpiDevice.Create(new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = 50000000,
                Mode = SpiMode.Mode0
            });
            var sender1inch47Device = SpiDevice.Create(new SpiConnectionSettings(0, 1)
            {
                ClockFrequency = 50000000,
                Mode = SpiMode.Mode0
            });
            _lCD2Inch4 = new LCD2inch4(sender2inch4Device, pwmBacklight);
            _lCD2Inch4.Reset();
            _lCD2Inch4.Init();
            _lCD2Inch4.SetWindows(0, 0, LCD2inch4.Width, LCD2inch4.Height);
            _lCD2Inch4.Clear();
            _lCD2Inch4.BlDutyCycle(50);

            _lCD1Inch47 = new LCD1inch47(sender1inch47Device, pwmBacklight);
            _lCD1Inch47.Init();
            _lCD1Inch47.SetWindows(0, 0, LCD1inch47.Width, LCD1inch47.Height);
            _lCD1Inch47.Clear();
            _lCD1Inch47.BlDutyCycle(50);
        }
    }

    private void SelectScreen(int csPin)
    {
        _gpioController?.Write(_csPin2Inch4, PinValue.High);
        _gpioController?.Write(_csPin1Inch47, PinValue.High);
        _gpioController?.Write(csPin, PinValue.Low);
    }

    public async Task PlayEmojiToMainScreenAsync(string emojiName)
    {
        await _emojiSemaphore.WaitAsync();
        try
        {
            // 读取Lottie JSON文件
            var animation = Animation.Create(Path.Combine(AppContext.BaseDirectory, "LottieFiles", $"{emojiName}.json"));
            if (animation != null)
            {
                animation.Seek(0);
                var list = new List<FaceFrame>();
                //帧数
                var frameCount = animation.OutPoint;
                Console.WriteLine($"frame count :{frameCount}");
                Console.WriteLine($"fps :{animation.Fps}");
                Console.WriteLine($"Duration :{animation.Duration.TotalSeconds}");
                for (int i = 0; i < frameCount; i++)
                {
                    var progress = animation.Duration.TotalSeconds / (frameCount - i);
                    var frame = RenderLottieFrame(animation, progress, 320, 240);
                    list.Add(new FaceFrame
                    {
                        FrameBuffer = GetImageBytes(frame)
                    });
                    //await frame.SaveAsPngAsync(Path.Combine($"frame_{i:D4}.png"));
                    //await ShowImageToMainScreenAsync(frame);
                }
                foreach (var item in list)
                {
                    SelectScreen(_csPin2Inch4);
                    _lCD2Inch4?.ShowImageBytes(item.FrameBuffer);
                    await Task.Delay(14);
                }
            }
        }
        finally
        {
            _emojiSemaphore.Release();
        }
    }

    public async Task PlayEmojiToMainScreenByJsonFileAsync(string emojiName)
    {
        await _emojiSemaphore.WaitAsync();
        try
        {
            // JSON deserialization
            var deserializedData = JsonSerializer.Deserialize<FrameMetaData>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "EmojiFiles", $"{emojiName}.json")));

            if (deserializedData != null)
            {
                foreach (var frameData in deserializedData.FrameDatas)
                {
                    SelectScreen(_csPin2Inch4);
                    _lCD2Inch4?.ShowImageBytes(frameData);
                    await Task.Delay(20);
                }
            }
        }
        finally
        {
            _emojiSemaphore.Release();
        }
    }

    public Task PlayJointAnglesAsync(float j1, float j2, float j3, float j4, float j5, float j6, bool enable = false)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ShowImageToMainScreenAsync(Image<Bgra32> image)
    {
        image.Mutate(x => x.Rotate(90));
        using Image<Bgr24> converted2inch4Image = image.CloneAs<Bgr24>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            SelectScreen(_csPin2Inch4);
            _lCD2Inch4?.ShowImageData(converted2inch4Image);
        }
        return Task.FromResult(true);
    }

    public async Task<bool> ShowImageToSubScreenAsync(Image<Bgra32> image)
    {
        await _emojiSemaphore.WaitAsync();
        try
        {
            image.Mutate(x => x.Rotate(90));
            using Image<Bgr24> converted1inch47Image = image.CloneAs<Bgr24>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                SelectScreen(_csPin1Inch47);
                _lCD1Inch47?.ShowImageData(converted1inch47Image);
            }
            return true;
        }
        finally
        {
            _emojiSemaphore.Release();
        }

    }

    public async Task PlayAudioByFileAsync(string fileName)
    {
        await _audioPlayer.Play(fileName);
    }

    public async Task StopAudioAsync()
    {
        await _audioPlayer.Stop();
    }

    public byte[] GetImageBytes(Image<Bgra32> image)
    {
        image.Mutate(x => x.Rotate(90));
        using Image<Bgr24> converted2inch4Image = image.CloneAs<Bgr24>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (_lCD2Inch4 != null)
            {
                SelectScreen(_csPin2Inch4);
                return _lCD2Inch4.GetImageBytes(converted2inch4Image);
            }
        }
        return Array.Empty<byte>();
    }

    private static Image<Bgra32> RenderLottieFrame(Animation animation, double progress, int width, int height)
    {
        // 创建SKSurface用于渲染
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        // 清除背景
        canvas.Clear(SKColors.Transparent);

        animation.SeekFrameTime(progress);
        animation.Render(canvas, new SKRect(0, 0, width, height));

        // 将SKBitmap转换为byte数组
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = data.ToArray();

        // 转换为ImageSharp格式
        using var memStream = new MemoryStream(bytes);
        return Image.Load<Bgra32>(memStream);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
                _lCD2Inch4?.Dispose();
                _lCD1Inch47?.Dispose();
                _gpioController?.Dispose();
            }

            // 释放非托管资源（如果有）

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
