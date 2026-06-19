using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public class SKImageViewer : Control
{
    public static readonly StyledProperty<object?> ImageProperty =
        AvaloniaProperty.Register<SKImageViewer, object?>(nameof(Image));

    public object? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public static readonly StyledProperty<IList<object?>> BindingsProperty =
        AvaloniaProperty.Register<SKImageViewer, IList<object?>>(nameof(Bindings));

    public IList<object?> Bindings
    {
        get => GetValue(BindingsProperty);
        set => SetValue(BindingsProperty, value);
    }

    public static readonly StyledProperty<IReadOnlyList<UndertaleTexturePageItem>?> TexturePageItemsProperty =
        AvaloniaProperty.Register<SKImageViewer, IReadOnlyList<UndertaleTexturePageItem>?>(nameof(TexturePageItems));

    public IReadOnlyList<UndertaleTexturePageItem>? TexturePageItems
    {
        get => GetValue(TexturePageItemsProperty);
        set => SetValue(TexturePageItemsProperty, value);
    }

    public static readonly StyledProperty<UndertaleTexturePageItem?> SelectedTexturePageItemProperty =
        AvaloniaProperty.Register<SKImageViewer, UndertaleTexturePageItem?>(
            nameof(SelectedTexturePageItem),
            defaultBindingMode: BindingMode.TwoWay);

    public UndertaleTexturePageItem? SelectedTexturePageItem
    {
        get => GetValue(SelectedTexturePageItemProperty);
        set => SetValue(SelectedTexturePageItemProperty, value);
    }

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<SKImageViewer, double>(
            nameof(Zoom),
            defaultValue: 1,
            defaultBindingMode: BindingMode.TwoWay);

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, ClampZoom(value));
    }

    public static readonly StyledProperty<bool> IsRenderingEnabledProperty =
        AvaloniaProperty.Register<SKImageViewer, bool>(
            nameof(IsRenderingEnabled),
            defaultValue: true);

    public bool IsRenderingEnabled
    {
        get => GetValue(IsRenderingEnabledProperty);
        set => SetValue(IsRenderingEnabledProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ImageProperty)
        {
            if (Image is UndertaleTexturePageItem)
            {
                // Bind these values to a property so we can get updates when they change.
                IList<BindingBase> bindings =
                [
                    new Binding("Image.TexturePage.TextureData.Image")
                        {Source = this},
                    new Binding("Image.SourceX")
                        {Source = this},
                    new Binding("Image.SourceY")
                        {Source = this},
                    new Binding("Image.SourceWidth")
                        {Source = this},
                    new Binding("Image.SourceHeight")
                        {Source = this},
                    new Binding("Image.TargetX")
                        {Source = this},
                    new Binding("Image.TargetY")
                        {Source = this},
                    new Binding("Image.TargetWidth")
                        {Source = this},
                    new Binding("Image.TargetHeight")
                        {Source = this},
                    new Binding("Image.BoundingWidth")
                        {Source = this},
                    new Binding("Image.BoundingHeight")
                        {Source = this},
                ];

                MultiBinding multiBinding = new()
                {
                    Bindings = bindings,
                    Converter = new FuncMultiValueConverter<object?, IList<object?>>(x => new List<object?>(x))
                };

                Bind(BindingsProperty, multiBinding);
            }
            else
            {
                // NOTE: Unbind?
            }

            Invalidate();
        }
        else if (change.Property == BindingsProperty ||
                 change.Property == TexturePageItemsProperty ||
                 change.Property == SelectedTexturePageItemProperty ||
                 change.Property == ZoomProperty ||
                 change.Property == IsRenderingEnabledProperty)
        {
            if (change.Property == ZoomProperty)
                Zoom = ClampZoom(Zoom);

            Invalidate();
        }
    }

    readonly CustomDrawOperation customDrawOperation;

    public SKImageViewer()
    {
        ClipToBounds = true;
        customDrawOperation = new CustomDrawOperation();
        PointerReleased += SKImageViewer_PointerReleased;
        PointerWheelChanged += SKImageViewer_PointerWheelChanged;
    }

    private void SKImageViewer_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (!IsRenderingEnabled ||
            e.InitialPressMouseButton != Avalonia.Input.MouseButton.Left ||
            TexturePageItems is null)
            return;

        Point pointerPosition = e.GetPosition(this);
        Point point = new(pointerPosition.X / Zoom, pointerPosition.Y / Zoom);
        SelectedTexturePageItem = TexturePageItems
            .Where(item => item.SourceWidth > 0 && item.SourceHeight > 0)
            .Where(item => point.X >= item.SourceX && point.X < item.SourceX + item.SourceWidth &&
                           point.Y >= item.SourceY && point.Y < item.SourceY + item.SourceHeight)
            .OrderBy(item => (long)item.SourceWidth * item.SourceHeight)
            .FirstOrDefault();
    }

    private void SKImageViewer_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        if (e.Delta.Y == 0)
            return;

        Zoom = ClampZoom(Zoom * (e.Delta.Y > 0 ? 2 : 0.5));
        e.Handled = true;
    }

    static double ClampZoom(double zoom)
    {
        if (double.IsNaN(zoom) || double.IsInfinity(zoom))
            return 1;

        return Math.Clamp(zoom, 0.125, 32);
    }

    void Invalidate()
    {
        Size size = GetSize();
        Width = size.Width;
        Height = size.Height;

        InvalidateMeasure();
        InvalidateVisual();
    }

    Size GetSize()
    {
        Size size;

        if (Image is UndertaleTexturePageItem texturePageItem)
            size = new Size(texturePageItem.BoundingWidth, texturePageItem.BoundingHeight);
        else if (Image is GMImage gmImage)
            size = new Size(gmImage.Width, gmImage.Height);
        else if (Image is UndertaleSprite.MaskEntry maskEntry)
            size = new Size(maskEntry.Width, maskEntry.Height);
        else
            size = new Size(0, 0);

        return size * Zoom;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return GetSize();
    }

    public override void Render(DrawingContext context)
    {
        Size size = GetSize();
        customDrawOperation.Bounds = new Rect(0, 0, size.Width, size.Height);
        customDrawOperation.Image = Image;
        customDrawOperation.TexturePageItems = TexturePageItems;
        customDrawOperation.SelectedTexturePageItem = SelectedTexturePageItem;
        customDrawOperation.Zoom = Zoom;
        customDrawOperation.IsRenderingEnabled = IsRenderingEnabled;

        context.Custom(customDrawOperation);
    }

    public class CustomDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds { get; set; }

        public object? Image;
        public IReadOnlyList<UndertaleTexturePageItem>? TexturePageItems;
        public UndertaleTexturePageItem? SelectedTexturePageItem;
        public double Zoom = 1;
        public bool IsRenderingEnabled = true;

        readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

        public CustomDrawOperation()
        {
        }

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            try
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                    return;

                using var lease = leaseFeature.Lease();
                SKCanvas canvas = lease.SkCanvas;
                canvas.Save();
                double zoom = ClampZoom(Zoom);
                canvas.Scale((float)zoom);

                float naturalWidth = (float)(Bounds.Width / zoom);
                float naturalHeight = (float)(Bounds.Height / zoom);

                if (!IsRenderingEnabled)
                {
                    RenderPlaceholder(canvas, naturalWidth, naturalHeight);
                    canvas.Restore();
                    return;
                }

                // Checkered background
                int gridSize = 8;
                using SKPaint gridColor1 = new() { Color = new SKColor(102, 102, 102) };
                using SKPaint gridColor2 = new() { Color = new SKColor(153, 153, 153) };

                canvas.DrawRect(SKRect.Create(0, 0, naturalWidth, naturalHeight), gridColor1);

                for (int x = 0; x < naturalWidth / gridSize; x++)
                    for (int y = 0; y < naturalHeight / gridSize; y++)
                    {
                        if ((x + y) % 2 != 0)
                            canvas.DrawRect(SKRect.Create(x * gridSize, y * gridSize, gridSize, gridSize), gridColor2);
                    }

                // Image
                RenderImage(canvas);
                RenderTexturePageItemSelection(canvas);

                canvas.Restore();
            }
            catch (Exception)
            {
                throw;
            }
        }

        static void RenderPlaceholder(SKCanvas canvas, float width, float height)
        {
            if (width <= 0 || height <= 0)
                return;

            using SKPaint fillPaint = new()
            {
                Color = new SKColor(35, 39, 45),
                Style = SKPaintStyle.Fill
            };
            using SKPaint strokePaint = new()
            {
                Color = new SKColor(78, 86, 96),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = false
            };

            SKRect rect = SKRect.Create(0, 0, width, height);
            canvas.DrawRect(rect, fillPaint);
            canvas.DrawRect(rect, strokePaint);
        }

        public void RenderImage(SKCanvas canvas)
        {
            if (Image is UndertaleTexturePageItem texturePageItem)
            {
                if (texturePageItem.TexturePage is not null)
                {
                    SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texturePageItem);

                    if (image is not null)
                    {
                        canvas.DrawImage(image, SKRect.Create(texturePageItem.TargetX, texturePageItem.TargetY, texturePageItem.TargetWidth, texturePageItem.TargetHeight));
                    }
                }
            }
            else if (Image is GMImage gmImage)
            {
                SKImage image = mainVM.ImageCache.GetCachedImageFromGMImage(gmImage);
                canvas.DrawImage(image, 0, 0);
            }
            else if (Image is UndertaleSprite.MaskEntry maskEntry)
            {
                int size = maskEntry.Width * maskEntry.Height;
                byte[] pixels = new byte[size];

                for (int y = 0; y < maskEntry.Height; y++)
                {
                    int rowWidth = (maskEntry.Width + 7) / 8;
                    int byteRowIndex = y * rowWidth;

                    for (int x = 0; x < maskEntry.Width; x++)
                    {
                        int i = y * maskEntry.Width + x;
                        int byteIndex = byteRowIndex + (x / 8);
                        int bitIndex = x % 8;

                        pixels[i] = (maskEntry.Data[byteIndex] & (1 << (7 - bitIndex))) != 0 ? (byte)255 : (byte)0;
                    }
                }

                SKImage image = SKImage.FromPixelCopy(new SKImageInfo(maskEntry.Width, maskEntry.Height, SKColorType.Gray8), pixels);
                canvas.DrawImage(image, 0, 0);
            }
        }

        void RenderTexturePageItemSelection(SKCanvas canvas)
        {
            if (SelectedTexturePageItem is null || TexturePageItems is null || Image is not GMImage gmImage)
                return;

            if (!TexturePageItems.Contains(SelectedTexturePageItem) ||
                SelectedTexturePageItem.TexturePage?.TextureData?.Image != gmImage)
                return;

            SKRect rect = SKRect.Create(
                SelectedTexturePageItem.SourceX,
                SelectedTexturePageItem.SourceY,
                SelectedTexturePageItem.SourceWidth,
                SelectedTexturePageItem.SourceHeight);

            using SKPaint fillPaint = new()
            {
                Color = new SKColor(73, 130, 188, 72),
                Style = SKPaintStyle.Fill
            };
            using SKPaint strokePaint = new()
            {
                Color = new SKColor(124, 184, 255, 220),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = false
            };

            canvas.DrawRect(rect, fillPaint);
            canvas.DrawRect(rect, strokePaint);
        }
    }
}
