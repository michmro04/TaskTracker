using System;
using Avalonia;
using Avalonia.Controls;

namespace TaskTracker.Controls;

/// <summary>
/// Układa dzieci w poziomie, a szerokość każdego jest proporcjonalna do jego liczby minut
/// (dołączona właściwość <see cref="MinutesProperty"/>). Cała szerokość panelu odpowiada
/// <see cref="TotalMinutes"/>, więc oś czasu zawsze wypełnia dostępne miejsce.
/// </summary>
public class TimelinePanel : Panel
{
    public static readonly StyledProperty<double> TotalMinutesProperty =
        AvaloniaProperty.Register<TimelinePanel, double>(nameof(TotalMinutes));

    public static readonly AttachedProperty<double> MinutesProperty =
        AvaloniaProperty.RegisterAttached<TimelinePanel, Control, double>("Minutes");

    static TimelinePanel()
    {
        AffectsMeasure<TimelinePanel>(TotalMinutesProperty);
        AffectsParentMeasure<TimelinePanel>(MinutesProperty);
    }

    /// <summary>Ile minut odpowiada pełnej szerokości panelu.</summary>
    public double TotalMinutes
    {
        get => GetValue(TotalMinutesProperty);
        set => SetValue(TotalMinutesProperty, value);
    }

    public static double GetMinutes(Control control) => control.GetValue(MinutesProperty);

    public static void SetMinutes(Control control, double value) => control.SetValue(MinutesProperty, value);

    private double EffectiveTotal()
    {
        var sum = 0.0;
        foreach (var child in Children)
        {
            sum += Math.Max(0, GetMinutes(child));
        }
        // jeśli sesje wyjdą poza zadeklarowany czas, skalujemy do ich sumy
        return Math.Max(TotalMinutes, sum);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var total = EffectiveTotal();
        var width = double.IsInfinity(availableSize.Width) ? total * 2 : availableSize.Width;
        var height = 0.0;

        foreach (var child in Children)
        {
            var w = total > 0 ? width * Math.Max(0, GetMinutes(child)) / total : 0;
            child.Measure(new Size(w, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(double.IsInfinity(availableSize.Width) ? width : 0, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var total = EffectiveTotal();
        var x = 0.0;

        foreach (var child in Children)
        {
            var w = total > 0 ? finalSize.Width * Math.Max(0, GetMinutes(child)) / total : 0;
            child.Arrange(new Rect(x, 0, w, finalSize.Height));
            x += w;
        }

        return finalSize;
    }
}
