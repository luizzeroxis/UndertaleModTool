using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Tests;

public class ShaderViewModelTest
{
    [Fact]
    public async Task ImportRawShaderDataTask_ReturnsFalseWithoutView()
    {
        UndertaleShaderViewModel vm = CreateViewModel(out _);

        bool result = await vm.ImportRawShaderDataTask("HLSL11_VertexData");

        Assert.False(result);
    }

    [Fact]
    public async Task ImportRawShaderDataTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleShaderViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;

        bool result = await vm.ImportRawShaderDataTask("HLSL11_VertexData");

        Assert.False(result);
        Assert.NotNull(view.LastOpenFileOptions);
        Assert.Equal("Import shader", view.LastOpenFileOptions.Title);
    }

    [Fact]
    public async Task ExportRawShaderDataTask_ReturnsFalseWithoutView()
    {
        UndertaleShaderViewModel vm = CreateViewModel(out _);
        vm.Shader.HLSL11_VertexData = new UndertaleShader.UndertaleRawShaderData { IsNull = false, Data = [1, 2, 3] };

        bool result = await vm.ExportRawShaderDataTask("HLSL11_VertexData");

        Assert.False(result);
    }

    [Fact]
    public async Task ExportRawShaderDataTask_ReturnsFalseWhenDataIsMissing()
    {
        UndertaleShaderViewModel vm = CreateViewModel(out MainViewModel mainVM);
        mainVM.View = new DialogView();

        bool result = await vm.ExportRawShaderDataTask("HLSL11_VertexData");

        Assert.False(result);
    }

    [Fact]
    public async Task ExportRawShaderDataTask_ReturnsFalseWhenPickerIsCanceled()
    {
        UndertaleShaderViewModel vm = CreateViewModel(out MainViewModel mainVM);
        DialogView view = new();
        mainVM.View = view;
        vm.Shader.HLSL11_VertexData = new UndertaleShader.UndertaleRawShaderData { IsNull = false, Data = [1, 2, 3] };

        bool result = await vm.ExportRawShaderDataTask("HLSL11_VertexData");

        Assert.False(result);
        Assert.NotNull(view.LastSaveFileOptions);
        Assert.Equal("Export shader", view.LastSaveFileOptions.Title);
    }

    private static UndertaleShaderViewModel CreateViewModel(out MainViewModel mainVM)
    {
        ServiceCollection services = new();
        services.AddSingleton<MainViewModel>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        mainVM = serviceProvider.GetRequiredService<MainViewModel>();
        mainVM.Initialize();

        UndertaleShader shader = new()
        {
            Name = new UndertaleString("shd_test"),
        };

        return new UndertaleShaderViewModel(shader, serviceProvider);
    }

    private sealed class DialogView : IView
    {
        public FilePickerOpenOptions? LastOpenFileOptions { get; private set; }
        public FilePickerSaveOptions? LastSaveFileOptions { get; private set; }

        public Task<MessageWindow.Result> MessageDialog(
            string message,
            string? title = null,
            MessageWindow.Buttons buttons = MessageWindow.Buttons.OK)
        {
            return Task.FromResult(MessageWindow.Result.OK);
        }

        public Task<IReadOnlyList<IStorageFile>> OpenFileDialog(FilePickerOpenOptions options)
        {
            LastOpenFileOptions = options;
            return Task.FromResult<IReadOnlyList<IStorageFile>>([]);
        }

        public Task<IStorageFile?> SaveFileDialog(FilePickerSaveOptions options)
        {
            LastSaveFileOptions = options;
            return Task.FromResult<IStorageFile?>(null);
        }

        public Task<IReadOnlyList<IStorageFolder>> OpenFolderDialog(FolderPickerOpenOptions options)
        {
            throw new NotSupportedException();
        }

        public Task<bool> LaunchUriAsync(Uri uri)
        {
            throw new NotSupportedException();
        }

        public Task<string?> TextBoxDialog(string message, string text = "", string? title = null, bool isMultiline = false, bool isReadOnly = false)
        {
            throw new NotSupportedException();
        }

        public ILoaderWindow LoaderOpen()
        {
            throw new NotSupportedException();
        }

        public IInputElement? GetFocusedElement()
        {
            return null;
        }
    }
}
