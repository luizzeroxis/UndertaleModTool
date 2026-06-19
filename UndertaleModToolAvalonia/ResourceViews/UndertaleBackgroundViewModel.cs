using UndertaleModLib;
using UndertaleModLib.Models;
using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia;

public partial class UndertaleBackgroundViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Background;
    public UndertaleBackground Background { get; }

    [Notify]
    private bool _IsPreviewRendered;

    [Notify]
    private string _PreviewStatus = "";

    public UndertaleBackgroundViewModel(UndertaleBackground background, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        Background = background;
        ResetPreviewState();
    }

    public void OnAttached()
    {
        if (MainVM.Settings is not null)
            MainVM.Settings.PropertyChanged += Settings_PropertyChanged;
    }

    public void OnDetached()
    {
        if (MainVM.Settings is not null)
            MainVM.Settings.PropertyChanged -= Settings_PropertyChanged;
    }

    public void RenderPreview()
    {
        IsPreviewRendered = HasPreviewImage();
        UpdatePreviewStatus();
    }

    void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsFile.AutomaticallyRenderImagePreviews))
            ResetPreviewState();
    }

    void ResetPreviewState()
    {
        IsPreviewRendered = ShouldRenderImagePreviewsAutomatically() && HasPreviewImage();
        UpdatePreviewStatus();
    }

    void UpdatePreviewStatus()
    {
        PreviewStatus = Background.Texture?.TexturePage?.TextureData?.Image is null
            ? "No texture image loaded."
            : IsPreviewRendered
                ? "Texture preview rendered."
                : "Texture preview not rendered.";
    }

    bool HasPreviewImage()
    {
        return Background.Texture?.TexturePage?.TextureData?.Image is not null;
    }

    bool ShouldRenderImagePreviewsAutomatically()
    {
        return MainVM.Settings?.AutomaticallyRenderImagePreviews ?? true;
    }

    public static UndertaleBackground.TileID CreateTileID() => new();

    public void AutoTileIDs()
    {
        Background.GMS2TileIds.Clear();

        for (uint i = 0; i < Background.GMS2TileCount; i++)
            for (uint j = 0; j < Background.GMS2ItemsPerTileCount; j++)
                Background.GMS2TileIds.Add(new UndertaleBackground.TileID() { ID = i });
    }
}
