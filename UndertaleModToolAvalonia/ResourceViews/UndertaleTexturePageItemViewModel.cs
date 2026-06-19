using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public partial class UndertaleTexturePageItemViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => TexturePageItem;
    public UndertaleTexturePageItem TexturePageItem { get; }

    [Notify]
    private double _PreviewZoom = 1;

    [Notify]
    private bool _IsPreviewRendered;

    [Notify]
    private string _PreviewStatus = "";

    public UndertaleTexturePageItemViewModel(UndertaleTexturePageItem texturePageItem, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        TexturePageItem = texturePageItem;
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

    public void ZoomIn()
    {
        PreviewZoom = Math.Min(32, PreviewZoom * 2);
    }

    public void ZoomOut()
    {
        PreviewZoom = Math.Max(0.125, PreviewZoom / 2);
    }

    public void ZoomReset()
    {
        PreviewZoom = 1;
    }

    public void OpenTexturePage()
    {
        OpenTexturePage(inNewTab: false);
    }

    public void OpenTexturePageInNewTab()
    {
        OpenTexturePage(inNewTab: true);
    }

    void OpenTexturePage(bool inNewTab)
    {
        if (TexturePageItem.TexturePage is null)
            return;

        TabItemViewModel? tab = MainVM.TabOpen(TexturePageItem.TexturePage, inNewTab);
        if (tab?.Content is UndertaleEmbeddedTextureViewModel textureViewModel)
            textureViewModel.SelectedTexturePageItem = TexturePageItem;
    }

    private void OnPreviewZoomChanged()
    {
        if (double.IsNaN(PreviewZoom) || double.IsInfinity(PreviewZoom))
        {
            PreviewZoom = 1;
            return;
        }

        PreviewZoom = Math.Clamp(PreviewZoom, 0.125, 32);
    }

    public void RenderPreview()
    {
        IsPreviewRendered = TexturePageItem.TexturePage?.TextureData?.Image is not null;
        UpdatePreviewStatus();
    }

    void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsFile.AutomaticallyRenderImagePreviews))
            ResetPreviewState();
    }

    void ResetPreviewState()
    {
        IsPreviewRendered = ShouldRenderImagePreviewsAutomatically() && TexturePageItem.TexturePage?.TextureData?.Image is not null;
        UpdatePreviewStatus();
    }

    void UpdatePreviewStatus()
    {
        PreviewStatus = TexturePageItem.TexturePage?.TextureData?.Image is null
            ? "No texture image loaded."
            : IsPreviewRendered
                ? "Texture page item preview rendered."
                : "Texture page item preview not rendered.";
    }

    bool ShouldRenderImagePreviewsAutomatically()
    {
        return MainVM.Settings?.AutomaticallyRenderImagePreviews ?? true;
    }

    public async void ImportImage()
    {
        await ImportImageTask();
    }

    public async Task<bool> ImportImageTask()
    {
        if (MainVM.View is not { } view)
            return false;

        IReadOnlyList<IStorageFile> files = await view.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import PNG",
            FileTypeFilter = FilePickerFileTypes.PNG,
        });

        if (files.Count != 1)
            return false;

        try
        {
            GMImage.ImageFormat previousFormat = TexturePageItem.TexturePage.TextureData.Image.Format;

            using (Stream stream = await files[0].OpenReadAsync())
            {
                await ImportExport.ImportTexturePageItemAsPNG(TexturePageItem, stream);
            }

            GMImage.ImageFormat currentFormat = TexturePageItem.TexturePage.TextureData.Image.Format;
            if (previousFormat == GMImage.ImageFormat.Dds && currentFormat == GMImage.ImageFormat.Png)
            {
                await view.MessageDialog(
                    $"{TexturePageItem.TexturePage} was converted into PNG format because DDS texture writing is not supported.",
                    "Texture converted");
            }

            ResetPreviewState();
            return true;
        }
        catch (Exception e)
        {
            await view.MessageDialog(e.Message, "Failed to import image");
            return false;
        }
    }

    public async void ExportImage()
    {
        await ExportImageTask();
    }

    public async Task<bool> ExportImageTask()
    {
        if (MainVM.View is not { } view)
            return false;

        IStorageFile? file = await view.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export PNG",
            FileTypeChoices = FilePickerFileTypes.PNG,
            DefaultExtension = ".png",
        });

        if (file is null)
            return false;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportTexturePageItemAsPNG(TexturePageItem, stream, MainVM);
        }

        return true;
    }
}
