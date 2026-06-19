using System.IO;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;

namespace UndertaleModToolAvalonia.Tests;

public class HeadlessTest
{
    [AvaloniaFact]
    public async Task Save_New_Data()
    {
        MainViewModel vm = App.Services.GetRequiredService<MainViewModel>();

        var mainWindow = new MainWindow
        {
            DataContext = vm,
        };

        try
        {
            mainWindow.Show();

            await vm.NewData();

            Assert.NotNull(vm.Data);

            using MemoryStream stream = new();

            await vm.SaveData(stream);
            Assert.Equal(2200, stream.Length);
        }
        finally
        {
            mainWindow.Close();
        }
    }
}
