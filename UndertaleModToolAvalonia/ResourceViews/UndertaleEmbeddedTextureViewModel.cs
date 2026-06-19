using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public partial class UndertaleEmbeddedTextureViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => EmbeddedTexture;
    public UndertaleEmbeddedTexture EmbeddedTexture { get; }
    public IReadOnlyList<UndertaleTexturePageItem> TexturePageItems { get; }

    [Notify]
    private UndertaleTexturePageItem? _SelectedTexturePageItem;

    [Notify]
    private string _SelectedTexturePageItemDescription = "Click a texture region to select a page item.";

    [Notify]
    private double _PreviewZoom = 1;

    [Notify]
    private bool _IsPreviewRendered;

    [Notify]
    private string _PreviewStatus = "";

    public UndertaleEmbeddedTextureViewModel(UndertaleEmbeddedTexture embeddedTexture, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        EmbeddedTexture = embeddedTexture;
        TexturePageItems = MainVM.Data?.TexturePageItems?
            .Where(item => item.TexturePage == EmbeddedTexture)
            .ToList() ?? [];
        SelectedTexturePageItemDescription = TexturePageItems.Count == 0
            ? "No texture page items reference this texture."
            : "Click a texture region to select a page item.";
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

    private void OnSelectedTexturePageItemChanged()
    {
        if (SelectedTexturePageItem is null)
        {
            SelectedTexturePageItemDescription = TexturePageItems.Count == 0
                ? "No texture page items reference this texture."
                : "Click a texture region to select a page item.";
            return;
        }

        int index = MainVM.Data?.TexturePageItems.IndexOf(SelectedTexturePageItem) ?? -1;
        SelectedTexturePageItemDescription =
            $"Page item {index}: source {SelectedTexturePageItem.SourceX},{SelectedTexturePageItem.SourceY} " +
            $"{SelectedTexturePageItem.SourceWidth}x{SelectedTexturePageItem.SourceHeight}; target " +
            $"{SelectedTexturePageItem.TargetX},{SelectedTexturePageItem.TargetY} " +
            $"{SelectedTexturePageItem.TargetWidth}x{SelectedTexturePageItem.TargetHeight}.";

        if (MainVM.View is MainView mainView)
            mainView.SelectValueInTree(SelectedTexturePageItem);
    }

    public void OpenSelectedTexturePageItem()
    {
        if (SelectedTexturePageItem is not null)
            MainVM.TabOpen(SelectedTexturePageItem);
    }

    public void OpenSelectedTexturePageItemInNewTab()
    {
        if (SelectedTexturePageItem is not null)
            MainVM.TabOpen(SelectedTexturePageItem, inNewTab: true);
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
        IsPreviewRendered = EmbeddedTexture.TextureData?.Image is not null;
        UpdatePreviewStatus();
    }

    void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsFile.AutomaticallyRenderImagePreviews))
            ResetPreviewState();
    }

    void ResetPreviewState()
    {
        IsPreviewRendered = ShouldRenderImagePreviewsAutomatically() && EmbeddedTexture.TextureData?.Image is not null;
        UpdatePreviewStatus();
    }

    void UpdatePreviewStatus()
    {
        PreviewStatus = EmbeddedTexture.TextureData?.Image is null
            ? "No texture image loaded."
            : IsPreviewRendered
                ? "Texture preview rendered."
                : "Texture preview not rendered.";
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

        // TODO: Allow formats other than PNG, either directly or to convert it
        IReadOnlyList<IStorageFile> files = await view.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import image",
            FileTypeFilter = FilePickerFileTypes.Image,
        });

        if (files.Count != 1)
            return false;

        using (Stream stream = await files[0].OpenReadAsync())
        {
            await ImportExport.ImportEmbeddedTexture(EmbeddedTexture, stream);
        }

        ResetPreviewState();
        return true;
    }

    public async void ExportImage()
    {
        await ExportImageTask();
    }

    public async Task<bool> ExportImageTask()
    {
        if (MainVM.View is not { } view)
            return false;

        (IReadOnlyList<FilePickerFileType> filePickerFileTypeList, string extension) type = EmbeddedTexture.TextureData.Image.Format switch
        {
            GMImage.ImageFormat.Png => (FilePickerFileTypes.PNG, "png"),
            GMImage.ImageFormat.Qoi => (FilePickerFileTypes.QOI, "qoi"),
            GMImage.ImageFormat.Bz2Qoi => (FilePickerFileTypes.BZ2, "bz2"),
            _ => (FilePickerFileTypes.BIN, "bin"),
        };

        IStorageFile? file = await view.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export image",
            FileTypeChoices = type.filePickerFileTypeList,
            DefaultExtension = $"*.{type.extension}",
            SuggestedFileName = $"{EmbeddedTexture.Name.Content}.{type.extension}",
        });

        if (file is null)
            return false;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportEmbeddedTexture(EmbeddedTexture, stream);
        }

        return true;
    }

    public async void ExportImageAsPNG()
    {
        await ExportImageAsPNGTask();
    }

    public async Task<bool> ExportImageAsPNGTask()
    {
        if (MainVM.View is not { } view)
            return false;

        IStorageFile? file = await view.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export image as PNG",
            FileTypeChoices = FilePickerFileTypes.PNG,
            DefaultExtension = ".png",
            SuggestedFileName = $"{EmbeddedTexture.Name.Content}.png",
        });

        if (file is null)
            return false;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportEmbeddedTextureAsPNG(EmbeddedTexture, stream);
        }

        return true;
    }
}
