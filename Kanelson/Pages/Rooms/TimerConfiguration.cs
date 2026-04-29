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
        return $"{Math.Max(0, _max - _current):0}s";
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
        OnExpired = null;
    }

    public Action? OnExpired { get; set; }

    public void Extend(int seconds)
    {
        _max += seconds;
        Percentage = Math.Max(0, (_max - _current) / _max * 100);
        if (!_timerHandle.Enabled)
            _timerHandle.Start();
    }

    public void Increment()
    {
        _current++;
        Percentage = Math.Max(0, (_max - _current) / _max * 100);
        if (_current >= _max)
        {
            _timerHandle.Stop();
            var handler = OnExpired;
            OnExpired = null;
            handler?.Invoke();
        }
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