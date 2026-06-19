using System;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleGameObjectViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => GameObject;
    public UndertaleGameObject GameObject { get; }
    public UndertaleTexturePageItem? PreviewImage => GetPreviewImage();

    [Notify]
    private bool _IsPreviewRendered;

    [Notify]
    private string _PreviewStatus = "";

    public UndertaleGameObjectViewModel(UndertaleGameObject gameObject, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        GameObject = gameObject;
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
        PreviewStatus = GetPreviewImage() is null
            ? "No sprite texture loaded."
            : IsPreviewRendered
                ? "Sprite preview rendered."
                : "Sprite preview not rendered.";
    }

    UndertaleTexturePageItem? GetPreviewImage()
    {
        return GameObject.Sprite?.Textures.Count > 0 ? GameObject.Sprite.Textures[0].Texture : null;
    }

    bool HasPreviewImage()
    {
        UndertaleTexturePageItem? texture = GetPreviewImage();
        return texture?.TexturePage?.TextureData?.Image is not null;
    }

    bool ShouldRenderImagePreviewsAutomatically()
    {
        return MainVM.Settings?.AutomaticallyRenderImagePreviews ?? true;
    }

    public static UndertaleGameObject.UndertalePhysicsVertex CreatePhysicsVertex() => new();
    public static UndertaleGameObject.Event CreateEvent() => new();
    public static UndertaleGameObject.EventAction CreateEventAction() => new();

    public Task<UndertaleResource?> CreateEventActionCode(object? argument)
    {
        if (argument is not IList list || list is not [EventType eventType, uint eventSubtype])
            return Task.FromResult<UndertaleResource?>(null);

        return Task.FromResult<UndertaleResource?>(GameObject?.EventHandlerFor(eventType, eventSubtype, MainVM.Data));
    }
}
