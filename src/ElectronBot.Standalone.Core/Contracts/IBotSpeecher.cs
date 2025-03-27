namespace ElectronBot.Standalone.Core.Contracts;

public interface IBotSpeecher : IDisposable
{
    string Provider
    {
        get;
    }
    Task<string> ListenAsync(CancellationToken cancellationToken = default);

    Task SpeakAsync(string text, CancellationToken cancellationToken = default);
}
