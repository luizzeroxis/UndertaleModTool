using System.Collections.Generic;
using System.Linq;
using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleFontViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Font;
    public UndertaleFont Font { get; }

    [Notify]
    private UndertaleFont.Glyph? _GlyphsSelected;

    [Notify]
    private bool _IsPreviewRendered;

    [Notify]
    private string _PreviewStatus = "";

    public UndertaleFontViewModel(UndertaleFont font, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        Font = font;
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
        PreviewStatus = Font.Texture?.TexturePage?.TextureData?.Image is null
            ? "No texture image loaded."
            : IsPreviewRendered
                ? "Texture preview rendered."
                : "Texture preview not rendered.";
    }

    bool HasPreviewImage()
    {
        return Font.Texture?.TexturePage?.TextureData?.Image is not null;
    }

    bool ShouldRenderImagePreviewsAutomatically()
    {
        return MainVM.Settings?.AutomaticallyRenderImagePreviews ?? true;
    }

    public void GlyphsSelectedChanged(object? item)
    {
        GlyphsSelected = (UndertaleFont.Glyph?)item!;
    }

    public void SortGlyphs()
    {
        List<UndertaleFont.Glyph> sortedGlyphs = Font.Glyphs.OrderBy(x => x.Character).ToList();

        Font.Glyphs.Clear();
        foreach (UndertaleFont.Glyph glyph in sortedGlyphs)
            Font.Glyphs.Add(glyph);
    }

    public void UpdateRange()
    {
        IEnumerable<ushort> characters = Font.Glyphs.Select(x => x.Character);
        Font.RangeStart = characters.Min();
        Font.RangeEnd = characters.Max();
    }

    public static UndertaleFont.Glyph CreateGlyph() => new();
    public static UndertaleFont.Glyph.GlyphKerning CreateGlyphKerning() => new();
}
