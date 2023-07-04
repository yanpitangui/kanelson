using System.Timers;
using Timer = System.Timers.Timer;

namespace Kanelson.Pages.Rooms;

public sealed class TimerConfiguration : IDisposable
{
    private double _current;
    private readonly Timer _timerHandle = new(TimeSpan.FromSeconds(1));
    public double Percentage { get; private set; }
    private double _max;
    public bool Enabled => _timerHandle.Enabled;

    public string Format(double percentage)
    {
        return $"{_max - _current}s";
    }

    public void SetupAction(ElapsedEventHandler action)
    {
        _timerHandle.Elapsed += action;
    }
        
        
    public void Stop() => _timerHandle.Stop();

    public void ResetAndStart(int maxValue)
    {
        _timerHandle.Start();
        _current = 0;
        Percentage = 100;
        _max = maxValue;
    }

    public void Increment()
    {
        _current++;
        Percentage = (_max - _current)/_max * 100;
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (disposing && _timerHandle is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}