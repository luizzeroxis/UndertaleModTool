using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Tests;

public class SoundViewModelTest
{
    [Fact]
    public async Task PlayAudioTask_ReturnsFalseWithoutAudioFile()
    {
        UndertaleSoundViewModel vm = CreateViewModel(audio: null);

        bool result = await vm.PlayAudioTask();

        Assert.False(result);
    }

    [Fact]
    public async Task PlayAudioTask_ReturnsFalseWithoutAudioData()
    {
        UndertaleEmbeddedAudio audio = new()
        {
            Name = new UndertaleString("audio_test"),
            Data = [],
        };
        UndertaleSoundViewModel vm = CreateViewModel(audio);

        bool result = await vm.PlayAudioTask();

        Assert.False(result);
    }

    private static UndertaleSoundViewModel CreateViewModel(UndertaleEmbeddedAudio? audio)
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        MainViewModel mainVM = serviceProvider.GetRequiredService<MainViewModel>();
        mainVM.Initialize();

        UndertaleSound sound = new()
        {
            Name = new UndertaleString("snd_test"),
            AudioFile = audio,
        };

        return new UndertaleSoundViewModel(sound, serviceProvider);
    }
}
