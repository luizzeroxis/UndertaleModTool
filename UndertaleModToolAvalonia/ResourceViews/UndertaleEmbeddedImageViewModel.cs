using UndertaleModLib;
using UndertaleModLib.Models;
using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;

namespace UndertaleModToolAvalonia;

public partial class UndertaleEmbeddedImageViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => EmbeddedImage;
    public UndertaleEmbeddedImage EmbeddedImage { get; }

    [Notify]
    private bool _IsPreviewRendered;

    [Notify]
    private string _PreviewStatus = "";

    public UndertaleEmbeddedImageViewModel(UndertaleEmbeddedImage embeddedImage, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        EmbeddedImage = embeddedImage;
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
        PreviewStatus = EmbeddedImage.TextureEntry?.TexturePage?.TextureData?.Image is null
            ? "No texture image loaded."
            : IsPreviewRendered
                ? "Texture preview rendered."
                : "Texture preview not rendered.";
    }

    bool HasPreviewImage()
    {
        return EmbeddedImage.TextureEntry?.TexturePage?.TextureData?.Image is not null;
    }

    bool ShouldRenderImagePreviewsAutomatically()
    {
        return MainVM.Settings?.AutomaticallyRenderImagePreviews ?? true;
    }
}
