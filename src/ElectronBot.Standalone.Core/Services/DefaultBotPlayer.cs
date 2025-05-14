using ElectronBot.Standalone.Core.Contracts;
using ElectronBot.Standalone.Core.Models;
using Microsoft.Extensions.Logging;
using NetCoreAudio;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using SkiaSharp.Skottie;
using System.Device.Gpio;
using System.Device.Spi;
using System.Runtime.InteropServices;
using System.Text.Json;
using Verdure.Iot.Device;

namespace ElectronBot.Standalone.Core.Services;

public class DefaultBotPlayer : IBotPlayer, IDisposable
{
    private readonly ST7789Display? _st7789Display24;
    private readonly ST7789Display? _st7789Display47;
    private readonly Player _audioPlayer;
    private bool _disposed = false;
    private readonly SemaphoreSlim _emojiSemaphore = new SemaphoreSlim(1, 1);
    private readonly GpioController? _gpioController;
    private readonly int _csPin2Inch4 = 8; // 片选引脚
    private readonly int _csPin1Inch47 = 7; // 片选引脚
    private readonly ILogger<DefaultBotPlayer>? _logger;

    private readonly LottiePlayer _lottiePlayer;

    public DefaultBotPlayer(ILogger<DefaultBotPlayer>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("正在初始化 DefaultBotPlayer");

        _audioPlayer = new Player();
        _logger?.LogInformation("音频播放器初始化完成");

        _lottiePlayer = new LottiePlayer(ProcessFrame);
        _logger?.LogInformation("Lottie播放器初始化完成");

        // 订阅事件
        _lottiePlayer.PlayCompleted += (s, e) =>
        {
            _logger?.LogInformation($"动画播放完成: {e.FilePath}");
            Console.WriteLine($"动画播放完成: {e.FilePath}");
        };
        _lottiePlayer.PlayStopped += (s, e) =>
        {
            _logger?.LogInformation($"动画播放被停止: {e.FilePath}");
            Console.WriteLine($"动画播放被停止: {e.FilePath}");
        };
        _lottiePlayer.FrameRendered += FrameRendered;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger?.LogInformation("检测到Linux平台，初始化硬件...");
            _gpioController = new GpioController();
            _logger?.LogInformation("GPIO控制器初始化完成");

            _logger?.LogInformation("正在创建SPI连接设置...");
            var settings1 = new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = 24_000_000, // 尝试降低SPI时钟频率以减少闪烁
                Mode = SpiMode.Mode0,
            };

            var settings2 = new SpiConnectionSettings(0, 1)
            {
                ClockFrequency = 24_000_000,
                Mode = SpiMode.Mode0,
            };

            _logger?.LogInformation("正在初始化2.4寸显示器...");
            _st7789Display24 = new ST7789Display(settings1, _gpioController, true, dcPin: 25, resetPin: 27, displayType: DisplayType.Display24Inch);
            _logger?.LogInformation("2.4寸显示器初始化完成");

            _logger?.LogInformation("正在初始化1.47寸显示器...");
            _st7789Display47 = new ST7789Display(settings2, _gpioController, false, dcPin: 25, resetPin: 27, displayType: DisplayType.Display147Inch);
            _logger?.LogInformation("1.47寸显示器初始化完成");

            // 清屏以准备播放动画 不清屏是不能写入数据的
            _st7789Display24.FillScreen(0x0000);  // 黑色
            _st7789Display47.FillScreen(0x0000);  // 黑色

            _logger?.LogInformation("硬件初始化完成");
        }
        else
        {
            _logger?.LogInformation("非Linux平台，硬件功能将被禁用");
        }
    }

    private Task ProcessFrame(LottieFrameEventArgs frameData)
    {
        return Task.CompletedTask;
    }

    private async void FrameRendered(object? sender, LottieFrameRenderedEventArgs e)
    {
        if (e.Image != null)
        {
            await ShowImageToMainScreenAsync(e.Image);
        }

        // 在这里添加你的定时任务逻辑
        //using (Image<Bgra32> image1inch47 = Image.Load<Bgra32>("Asserts/verdure.png"))
        //{
        //    var collection = new FontCollection();
        //    var family = collection.Add("Asserts/SmileySans-Oblique.ttf");
        //    var font = family.CreateFont(24, FontStyle.Bold);

        //    var textOptions = new TextOptions(font)
        //    {
        //        HorizontalAlignment = HorizontalAlignment.Center,
        //        VerticalAlignment = VerticalAlignment.Center,
        //        WrappingLength = 320
        //    };
        //    // 获取当前时间
        //    string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        //    var displayText = $"当前时间:{currentTime}";

        //    var size = TextMeasurer.MeasureSize(displayText, textOptions);

        //    var position = new PointF((LCD1inch47.Height - size.Width) / 2, 8);
        //    // 在图片上绘制时间
        //    image1inch47.Mutate(ctx => ctx.DrawText(displayText, font, Color.White, position));

        //    //await image1inch47.SaveAsPngAsync(Path.Combine($"frame_{DateTime.Now.Ticks}.png"));
        //    await ShowImageToSubScreenAsync(image1inch47);
        //}
    }

    public async Task PlayLottieByNameIdAsync(string nameId, int times)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "LottieFiles", $"{nameId}.json");
        _logger?.LogInformation($"开始播放Lottie动画: {nameId}, 路径: {path}, 次数: {times}");
        await _lottiePlayer.PlayAsync(path, times);
    }

    // 提供停止播放的方法
    public async Task StopLottiePlaybackAsync()
    {
        _logger?.LogInformation("停止Lottie播放");
        await _lottiePlayer.StopAsync();
    }

    private void SelectScreen(int csPin)
    {
        _logger?.LogInformation($"选择屏幕: CS引脚 {csPin}");
        //_gpioController?.Write(_csPin2Inch4, PinValue.High);
        //_gpioController?.Write(_csPin1Inch47, PinValue.High);
        //_gpioController?.Write(csPin, PinValue.Low);
    }

    public async Task PlayEmojiToMainScreenAsync(string emojiName)
    {
        _logger?.LogInformation($"开始播放表情到主屏幕: {emojiName}");
        await _emojiSemaphore.WaitAsync();
        try
        {
            // 读取Lottie JSON文件
            var filePath = Path.Combine(AppContext.BaseDirectory, "LottieFiles", $"{emojiName}.json");
            _logger?.LogInformation($"加载Lottie文件: {filePath}");

            var animation = Animation.Create(filePath);
            if (animation != null)
            {
                animation.Seek(0);
                var list = new List<FaceFrame>();
                //帧数
                var frameCount = animation.OutPoint;
                _logger?.LogInformation($"动画帧数: {frameCount}, FPS: {animation.Fps}, 时长: {animation.Duration.TotalSeconds}秒");

                Console.WriteLine($"frame count :{frameCount}");
                Console.WriteLine($"fps :{animation.Fps}");
                Console.WriteLine($"Duration :{animation.Duration.TotalSeconds}");

                for (int i = 0; i < frameCount; i++)
                {
                    // 计算进度
                    var progress = i / frameCount * animation.Duration.TotalSeconds;
                    _logger?.LogInformation($"渲染帧 {i}/{frameCount}, 进度: {progress:F2}");

                    var frame = RenderLottieFrame(animation, progress, 320, 240);
                    //list.Add(new FaceFrame
                    //{
                    //    FrameBuffer = GetImageBytes(frame)
                    //});
                    //await frame.SaveAsPngAsync(Path.Combine($"frame_{i:D4}.png"));
                    await ShowImageToMainScreenAsync(frame);
                }
                //foreach (var item in list)
                //{
                //    SelectScreen(_csPin2Inch4);
                //    _lCD2Inch4?.ShowImageBytes(item.FrameBuffer);
                //    await Task.Delay(14);
                //}
            }
            else
            {
                _logger?.LogWarning($"无法加载Lottie动画: {emojiName}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"播放表情时发生错误: {emojiName}");
            throw;
        }
        finally
        {
            _emojiSemaphore.Release();
            _logger?.LogInformation($"表情播放完成: {emojiName}");
        }
    }

    public async Task PlayEmojiToMainScreenByJsonFileAsync(string emojiName)
    {
        _logger?.LogInformation($"从JSON文件播放表情到主屏幕: {emojiName}");
        await _emojiSemaphore.WaitAsync();
        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "EmojiFiles", $"{emojiName}.json");
            _logger?.LogInformation($"加载JSON文件: {filePath}");

            // JSON deserialization
            var deserializedData = JsonSerializer.Deserialize<FrameMetaData>(await File.ReadAllTextAsync(filePath));

            if (deserializedData != null)
            {
                _logger?.LogInformation($"帧数据数量: {deserializedData.FrameDatas?.Count ?? 0}");
                if (deserializedData.FrameDatas != null)
                {
                    int frameIndex = 0;
                    foreach (var frameData in deserializedData.FrameDatas)
                    {
                        _logger?.LogInformation($"显示帧: {frameIndex++}/{deserializedData.FrameDatas.Count}");
                        SelectScreen(_csPin2Inch4);
                        await Task.Delay(20);
                    }
                }
            }
            else
            {
                _logger?.LogWarning($"JSON文件数据为空: {emojiName}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"从JSON播放表情时发生错误: {emojiName}");
            throw;
        }
        finally
        {
            _emojiSemaphore.Release();
            _logger?.LogInformation($"JSON表情播放完成: {emojiName}");
        }
    }

    public Task PlayJointAnglesAsync(float j1, float j2, float j3, float j4, float j5, float j6, bool enable = false)
    {
        _logger?.LogInformation($"播放关节角度: [{j1}, {j2}, {j3}, {j4}, {j5}, {j6}], 启用: {enable}");
        throw new NotImplementedException();
    }

    public async Task<bool> ShowImageToMainScreenAsync(Image<Bgra32> image)
    {
        await _emojiSemaphore.WaitAsync();
        try
        {
            image.Mutate(x => x.Rotate(90));
            using Image<Bgr24> converted2inch4Image = image.CloneAs<Bgr24>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var data1 = _st7789Display24?.GetImageBytes(converted2inch4Image);

                if (data1 != null)
                {
                    _st7789Display24?.SendData(data1);
                }

                await Task.Delay(5); // 短暂延时确保传输完成
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "显示图像到主屏幕时发生错误");
            return false;
        }
        finally
        {
            _emojiSemaphore.Release();
        }
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
                var data2 = _st7789Display47?.GetImageBytes(converted1inch47Image);

                if (data2 != null)
                {
                    _st7789Display47?.SendData(data2);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "显示图像到副屏幕时发生错误");
            return false;
        }
        finally
        {
            _emojiSemaphore.Release();
        }
    }

    public async Task PlayAudioByFileAsync(string fileName)
    {
        try
        {
            await _audioPlayer.Play(fileName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"播放音频文件时发生错误: {fileName}");
            throw;
        }
    }

    public async Task StopAudioAsync()
    {
        try
        {
            await _audioPlayer.Stop();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "停止音频播放时发生错误");
            throw;
        }
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
                _logger?.LogInformation("正在释放资源...");
                // 释放托管资源
                _lCD2Inch4?.Dispose();
                _lCD1Inch47?.Dispose();
                _gpioController?.Dispose();
                _st7789Display24?.Dispose();
                _st7789Display47?.Dispose();
                _logger?.LogInformation("资源释放完成");
            }

            // 释放非托管资源（如果有）

            _disposed = true;
        }
    }

    public void Dispose()
    {
        _logger?.LogInformation("DefaultBotPlayer开始释放资源");
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
        _logger?.LogInformation("DefaultBotPlayer资源释放完成");
    }

    public async Task ShowDateToSubScreenAsync()
    {
        try
        {
            //在这里添加你的定时任务逻辑
            string imagePath = "Asserts/verdure.png";
            using (Image<Bgra32> image1inch47 = Image.Load<Bgra32>(imagePath))
            {
                var collection = new FontCollection();
                string fontPath = "Asserts/SmileySans-Oblique.ttf";
                var family = collection.Add(fontPath);
                var font = family.CreateFont(24, FontStyle.Bold);

                var textOptions = new TextOptions(font)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    WrappingLength = 320
                };
                // 获取当前时间
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                var displayText = $"当前时间:{currentTime}";

                var size = TextMeasurer.MeasureSize(displayText, textOptions);

                var position = new PointF((LCD1inch47.Height - size.Width) / 2, 8);
                _logger?.LogInformation($"文本位置: ({position.X}, {position.Y}), 尺寸: {size.Width}x{size.Height}");

                // 在图片上绘制时间
                image1inch47.Mutate(ctx => ctx.DrawText(displayText, font, Color.White, position));

                //await image1inch47.SaveAsPngAsync(Path.Combine($"frame_{DateTime.Now.Ticks}.png"));
                await ShowImageToSubScreenAsync(image1inch47);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "显示日期到副屏幕时发生错误");
        }
    }
}
