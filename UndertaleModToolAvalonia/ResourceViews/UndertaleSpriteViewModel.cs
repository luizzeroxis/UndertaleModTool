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

namespace UndertaleModToolAvalonia;

public partial class UndertaleSpriteViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Sprite;
    public UndertaleSprite Sprite { get; }

    [Notify]
    private UndertaleSprite.TextureEntry? _TexturesSelected;
    [Notify]
    private UndertaleSprite.MaskEntry? _CollisionMasksSelected;

    [Notify]
    private bool _IsTexturePreviewRendered;
    [Notify]
    private bool _IsCollisionMaskPreviewRendered;
    [Notify]
    private string _TexturePreviewStatus = "";
    [Notify]
    private string _CollisionMaskPreviewStatus = "";

    public UndertaleSpriteViewModel(UndertaleSprite sprite, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        Sprite = sprite;

        if (Sprite.Textures.Count > 0)
            TexturesSelected = Sprite.Textures[0];
        if (Sprite.CollisionMasks.Count > 0)
            CollisionMasksSelected = Sprite.CollisionMasks[0];

        ResetTexturePreviewState();
        ResetCollisionMaskPreviewState();
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

    public void TexturesSelectedChanged(object? item)
    {
        if (item is null)
        {
            if (Sprite.Textures.Count > 0)
                TexturesSelected = Sprite.Textures[0];
            else
                TexturesSelected = null;
        }
        else
            TexturesSelected = (UndertaleSprite.TextureEntry?)item!;

        ResetTexturePreviewState();
    }

    public void CollisionMasksSelectedChanged(object? item)
    {
        if (item is null)
        {
            if (Sprite.CollisionMasks.Count > 0)
                CollisionMasksSelected = Sprite.CollisionMasks[0];
            else
                CollisionMasksSelected = null;
        }
        else
            CollisionMasksSelected = (UndertaleSprite.MaskEntry?)item!;

        ResetCollisionMaskPreviewState();
    }

    public void RenderTexturePreview()
    {
        IsTexturePreviewRendered = TexturesSelected?.Texture is not null;
        UpdateTexturePreviewStatus();
    }

    public void RenderCollisionMaskPreview()
    {
        IsCollisionMaskPreviewRendered = CollisionMasksSelected is not null;
        UpdateCollisionMaskPreviewStatus();
    }

    void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsFile.AutomaticallyRenderImagePreviews))
        {
            ResetTexturePreviewState();
            ResetCollisionMaskPreviewState();
        }
    }

    void ResetTexturePreviewState()
    {
        IsTexturePreviewRendered = ShouldRenderImagePreviewsAutomatically() && TexturesSelected?.Texture is not null;
        UpdateTexturePreviewStatus();
    }

    void ResetCollisionMaskPreviewState()
    {
        IsCollisionMaskPreviewRendered = ShouldRenderImagePreviewsAutomatically() && CollisionMasksSelected is not null;
        UpdateCollisionMaskPreviewStatus();
    }

    void UpdateTexturePreviewStatus()
    {
        TexturePreviewStatus = TexturesSelected?.Texture is null
            ? "No texture frame selected."
            : IsTexturePreviewRendered
                ? "Texture preview rendered."
                : "Texture preview not rendered.";
    }

    void UpdateCollisionMaskPreviewStatus()
    {
        CollisionMaskPreviewStatus = CollisionMasksSelected is null
            ? "No collision mask selected."
            : IsCollisionMaskPreviewRendered
                ? "Collision mask preview rendered."
                : "Collision mask preview not rendered.";
    }

    bool ShouldRenderImagePreviewsAutomatically()
    {
        return MainVM.Settings?.AutomaticallyRenderImagePreviews ?? true;
    }

    public async void ExportAllTexturesAsPNGs()
    {
        await ExportAllTexturesAsPNGsTask();
    }

    public async Task<bool> ExportAllTexturesAsPNGsTask()
    {
        if (MainVM.View is not { } view)
            return false;

        string GetFileNameOfTexture(int i) => $"{Sprite.Name.Content}_{i}.png";

        IReadOnlyList<IStorageFolder> folders = await view.OpenFolderDialog(new FolderPickerOpenOptions()
        {
            Title = "Export all textures into folder",
        });

        if (folders.Count != 1)
            return false;

        IStorageFolder folder = folders[0];

        List<string> filesThatAlreadyExist = [];
        for (int i = 0; i < Sprite.Textures.Count; i++)
        {
            var fileName = GetFileNameOfTexture(i);
            if (await folder.GetFileAsync(fileName) is not null)
            {
                filesThatAlreadyExist.Add(fileName);
            }
        }

        if (filesThatAlreadyExist.Count > 0)
        {
            MessageWindow.Result result = await view.MessageDialog($"The following files already exist. Do you want to replace them?"
                + $"\n\n{string.Join("\n", filesThatAlreadyExist)}", buttons: MessageWindow.Buttons.YesCancel);

            if (result != MessageWindow.Result.Yes)
                return false;
        }

        for (int i = 0; i < Sprite.Textures.Count; i++)
        {
            var fileName = GetFileNameOfTexture(i);
            var texture = Sprite.Textures[i].Texture;

            IStorageFile? file = await folder.CreateFileAsync(fileName);
            if (file is null)
            {
                await view.MessageDialog($"Error: Could not create file \"{fileName}\"");
                return false;
            }

            using (var stream = await file.OpenWriteAsync())
            {
                await ImportExport.ExportTexturePageItemAsPNG(texture, stream, MainVM);
            }
        }

        return true;
    }

    public async void ImportCollisionMaskData()
    {
        await ImportCollisionMaskDataTask();
    }

    public async Task<bool> ImportCollisionMaskDataTask()
    {
        if (CollisionMasksSelected is null)
            return false;

        if (MainVM.View is not { } view)
            return false;

        IReadOnlyList<IStorageFile> files = await view.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import collision mask data",
            FileTypeFilter = FilePickerFileTypes.BIN,
        });

        if (files.Count != 1)
            return false;

        using (Stream stream = await files[0].OpenReadAsync())
        {
            await ImportExport.ImportSpriteCollisionMaskData(Sprite, Sprite.CollisionMasks.IndexOf(CollisionMasksSelected), stream, MainVM);
        }

        return true;
    }

    public async void ExportCollisionMaskData()
    {
        await ExportCollisionMaskDataTask();
    }

    public async Task<bool> ExportCollisionMaskDataTask()
    {
        if (CollisionMasksSelected is null)
            return false;

        if (MainVM.View is not { } view)
            return false;

        IStorageFile? file = await view.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export collision mask data",
            FileTypeChoices = FilePickerFileTypes.BIN,
            DefaultExtension = ".bin",
        });

        if (file is null)
            return false;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportSpriteCollisionMaskData(Sprite, Sprite.CollisionMasks.IndexOf(CollisionMasksSelected), stream);
        }

        return true;
    }

    public static UndertaleSprite.TextureEntry CreateTextureEntry() => new();
    public static UndertaleSprite.MaskEntry CreateMaskEntry() => new();
}
