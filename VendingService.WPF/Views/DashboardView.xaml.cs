using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VendingService.WPF.ViewModels;

namespace VendingService.WPF.Views;

public partial class DashboardView : UserControl
{
    private Point _dragStartPoint;
    private DashboardTileViewModel? _draggedTile;

    public DashboardView()
    {
        InitializeComponent();
    }

    private void TilesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(TilesListBox);
        _draggedTile = GetTileUnderMouse(TilesListBox, _dragStartPoint);
    }

    private void TilesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedTile is null)
        {
            return;
        }

        var currentPosition = e.GetPosition(TilesListBox);
        if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(TilesListBox, new DataObject("DashboardTile", _draggedTile), DragDropEffects.Move);
    }

    private void TilesListBox_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("DashboardTile") ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void TilesListBox_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("DashboardTile"))
        {
            return;
        }

        if (DataContext is not DashboardViewModel dashboard)
        {
            return;
        }

        if (e.Data.GetData("DashboardTile") is not DashboardTileViewModel dragged)
        {
            return;
        }

        var dropPosition = e.GetPosition(TilesListBox);
        var target = GetTileUnderMouse(TilesListBox, dropPosition);
        if (target is null || ReferenceEquals(dragged, target))
        {
            return;
        }

        var fromIndex = dashboard.Tiles.IndexOf(dragged);
        var toIndex = dashboard.Tiles.IndexOf(target);

        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
        {
            return;
        }

        dashboard.Tiles.Move(fromIndex, toIndex);
    }

    private static DashboardTileViewModel? GetTileUnderMouse(ListBox listBox, Point point)
    {
        var element = listBox.InputHitTest(point) as DependencyObject;
        var container = FindAncestor<ListBoxItem>(element);
        return container?.DataContext as DashboardTileViewModel;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
