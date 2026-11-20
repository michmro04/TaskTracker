using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using TaskTracker.ViewModels;

namespace TaskTracker.Views;

public partial class TaskCardView : UserControl
{
    public TaskCardView()
    {
        InitializeComponent();

        DescriptionGrid.SizeChanged += (_, _) => UpdateCanExpand();
        DataContextChanged += (_, _) => UpdateCanExpand();
        Loaded += (_, _) => UpdateCanExpand();
    }

    private void UpdateCanExpand()
    {
        if (DataContext is not TaskCardViewModel vm || !vm.HasDescription)
        {
            return;
        }

        double availableWidth = DescriptionGrid.Bounds.Width;
        if (availableWidth <= 0)
        {
            return;
        }

        bool hasNewLines = vm.Item.Opis.Contains('\n') || vm.Item.Opis.Contains('\r');

        var formatted = new FormattedText(
            vm.DescriptionPreview,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                DescriptionPreviewText.FontFamily,
                DescriptionPreviewText.FontStyle,
                DescriptionPreviewText.FontWeight,
                DescriptionPreviewText.FontStretch),
            DescriptionPreviewText.FontSize,
            null);

        bool overflows = formatted.Width > availableWidth - 2;
        vm.CanExpandDescription = overflows || hasNewLines;

        if (!vm.CanExpandDescription && vm.IsDescriptionExpanded)
        {
            vm.IsDescriptionExpanded = false;
        }
    }
}