using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LotteryDetectionMobile.Models.Voice;

public class WaveformBar : INotifyPropertyChanged
{
    private double height;

    public WaveformBar(double initialHeight)
    {
        height = initialHeight;
    }

    public double Height
    {
        get => height;
        set
        {
            if (Math.Abs(height - value) < 0.01) return;
            height = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
