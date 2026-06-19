using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia.Tests;

public class SearchInCodeViewModelTest
{
    [Fact]
    public async Task SearchTask_ReturnsFalseWithoutData()
    {
        (MainViewModel _, SearchInCodeViewModel vm) = CreateViewModel();

        bool result = await vm.SearchTask();

        Assert.False(result);
        Assert.Equal("Error: No data file loaded.", vm.StatusBarText);
    }

    [Fact]
    public async Task SearchTask_ReturnsFalseWithoutSearchText()
    {
        (MainViewModel mainVM, SearchInCodeViewModel vm) = CreateViewModel();
        await mainVM.NewData();

        bool result = await vm.SearchTask();

        Assert.False(result);
        Assert.Equal("Error: No text to search.", vm.StatusBarText);
    }

    [Fact]
    public async Task SearchTask_ReturnsFalseForInvalidRegex()
    {
        (MainViewModel mainVM, SearchInCodeViewModel vm) = CreateViewModel();
        await mainVM.NewData();
        vm.SearchText = "(";
        vm.IsRegexSearch = true;

        bool result = await vm.SearchTask();

        Assert.False(result);
        Assert.StartsWith("Error: Invalid regex", vm.StatusBarText);
    }

    [Fact]
    public async Task SearchTask_ReturnsFalseWithoutView()
    {
        (MainViewModel mainVM, SearchInCodeViewModel vm) = CreateViewModel();
        await mainVM.NewData();
        vm.SearchText = "anything";

        bool result = await vm.SearchTask();

        Assert.False(result);
        Assert.Equal("Error: Search window is not attached.", vm.StatusBarText);
    }

    private static (MainViewModel MainVM, SearchInCodeViewModel SearchVM) CreateViewModel()
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SearchInCodeViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        MainViewModel mainVM = serviceProvider.GetRequiredService<MainViewModel>();
        mainVM.Initialize();
        return (mainVM, serviceProvider.GetRequiredService<SearchInCodeViewModel>());
    }
}
