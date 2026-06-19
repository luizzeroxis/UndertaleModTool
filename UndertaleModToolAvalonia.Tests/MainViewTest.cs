using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia.Tests;

public class MainViewTest
{
    [AvaloniaFact]
    public void TreeSelectionWithoutTreeSourceDoesNotThrow()
    {
        MainViewModel vm = CreateViewModel();
        object value = new();
        MainViewModel.TreeDataGridItem item = new()
        {
            Text = "item",
            Value = value,
        };
        vm.TreeDataGridData.Add(item);

        MainView view = new()
        {
            DataContext = vm,
        };
        TreeDataGrid treeDataGrid = view.FindControl<TreeDataGrid>("MainTreeDataGrid")!;
        treeDataGrid.Source = null;

        view.ExpandItemOnTree(item);
        view.SelectValueInTree(value);
    }

    private static MainViewModel CreateViewModel()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton<MainViewModel>()
            .BuildServiceProvider();

        return serviceProvider.GetRequiredService<MainViewModel>();
    }
}
