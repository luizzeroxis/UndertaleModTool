using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleSoundViewModel : IUndertaleResourceViewModel
{
    public UndertaleResource Resource => Sound;
    public UndertaleSound Sound { get; set; }

    public UndertaleSoundViewModel(UndertaleSound sound)
    {
        Sound = sound;
    }

    public async void Play()
    {
        await AudioPlayback.PlayResource(Resource);
    }

    public void Stop()
    {
        AudioPlayback.StopResource();
    }
}
