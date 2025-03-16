namespace ElectronBot.Standalone.Core.Contracts;
public interface IWakeWordListener : IDisposable
{
    Task<bool> WaitForWakeWordAsync(CancellationToken cancellationToken);
}
