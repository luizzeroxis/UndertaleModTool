using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Controls;

public partial class DataTreeView : UserControl
{
    public static readonly StyledProperty<UndertaleData?> DataProperty =
        AvaloniaProperty.Register<DataTreeView, UndertaleData?>(nameof(Data));

    public UndertaleData? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

    public ObservableCollection<TreeItemViewModel> TreeSource { get; set; } = [];

    public TreeItemViewModel DataTreeItem;
    public ObservableCollection<TreeItemViewModel> DataTreeItemSource;

    public DataTreeView()
    {
        InitializeComponent();

        DataTreeItemSource = new ObservableCollection<TreeItemViewModel>
        {
            new(this, value: "GeneralInfo", header: Assets.Resources.GeneralInfoText),
            new(this, value: "GlobalInitScripts", header: Assets.Resources.GlobalInitScriptsText),
            new(this, value: "GameEndScripts", header:  Assets.Resources.GameEndScriptsText),
            new(this, tag: "list", value: "AudioGroups", header: Assets.Resources.AudioGroupsText),
            new(this, tag: "list", value: "Sounds", header: Assets.Resources.SoundsText),
            new(this, tag: "list", value: "Sprites", header: Assets.Resources.SpritesText),
            new(this, tag: "list", value: "Backgrounds", header: Assets.Resources.BackgroundsText),
            new(this, tag: "list", value: "Paths", header: Assets.Resources.PathsText),
            new(this, tag: "list", value: "Scripts", header: Assets.Resources.ScriptsText),
            new(this, tag: "list", value: "Shaders", header: Assets.Resources.ShadersText),
            new(this, tag: "list", value: "Fonts", header: Assets.Resources.FontsText),
            new(this, tag: "list", value: "Timelines", header: Assets.Resources.TimelinesText),
            new(this, tag: "list", value: "GameObjects", header: Assets.Resources.GameObjectsText),
            new(this, tag: "list", value: "Rooms", header: Assets.Resources.RoomsText),
            new(this, tag: "list", value: "Extensions", header: Assets.Resources.ExtensionsText),
            new(this, tag: "list", value: "TexturePageItems", header: Assets.Resources.TexturePageItemsText),
            new(this, tag: "list", value: "Code", header: Assets.Resources.CodeText),
            new(this, tag: "list", value: "Variables", header: Assets.Resources.VariablesText),
            new(this, tag: "list", value: "Functions", header: Assets.Resources.FunctionsText),
            new(this, tag: "list", value: "CodeLocals", header: Assets.Resources.CodeLocalsText),
            new(this, tag: "list", value: "Strings", header: Assets.Resources.StringsText),
            new(this, tag: "list", value: "EmbeddedTextures", header: Assets.Resources.EmbeddedTexturesText),
            new(this, tag: "list", value: "EmbeddedAudio", header: Assets.Resources.EmbeddedAudioText),
            new(this, tag: "list", value: "TextureGroupInformation", header: Assets.Resources.TextureGroupInformationText),
            new(this, tag: "list", value: "EmbeddedImages", header: Assets.Resources.EmbeddedImagesText),
            new(this, tag: "list", value: "ParticleSystems", header: Assets.Resources.ParticleSystemsText),
            new(this, tag: "list", value: "ParticleSystemEmitters", header: Assets.Resources.ParticleSystemEmittersText),
        };

        DataTreeItem = new TreeItemViewModel(this, value: "Data", header: "Data", source: DataTreeItemSource);

        TreeSource.Add(DataTreeItem);

        DataTreeItem.UpdateSource();
        DataTreeItem.ExpandCollapse();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataProperty)
        {
            DataTreeItemSource.First(x => Equals(x.Value, "AudioGroups")).Source = Data?.AudioGroups;
            DataTreeItemSource.First(x => Equals(x.Value, "Sounds")).Source = Data?.Sounds;
            DataTreeItemSource.First(x => Equals(x.Value, "Sprites")).Source = Data?.Sprites;
            DataTreeItemSource.First(x => Equals(x.Value, "Backgrounds")).Source = Data?.Backgrounds;
            DataTreeItemSource.First(x => Equals(x.Value, "Paths")).Source = Data?.Paths;
            DataTreeItemSource.First(x => Equals(x.Value, "Scripts")).Source = Data?.Scripts;
            DataTreeItemSource.First(x => Equals(x.Value, "Shaders")).Source = Data?.Shaders;
            DataTreeItemSource.First(x => Equals(x.Value, "Fonts")).Source = Data?.Fonts;
            DataTreeItemSource.First(x => Equals(x.Value, "Timelines")).Source = Data?.Timelines;
            DataTreeItemSource.First(x => Equals(x.Value, "GameObjects")).Source = Data?.GameObjects;
            DataTreeItemSource.First(x => Equals(x.Value, "Rooms")).Source = Data?.Rooms;
            DataTreeItemSource.First(x => Equals(x.Value, "Extensions")).Source = Data?.Extensions;
            DataTreeItemSource.First(x => Equals(x.Value, "TexturePageItems")).Source = Data?.TexturePageItems;
            DataTreeItemSource.First(x => Equals(x.Value, "Code")).Source = Data?.Code;
            DataTreeItemSource.First(x => Equals(x.Value, "Variables")).Source = Data?.Variables;
            DataTreeItemSource.First(x => Equals(x.Value, "Functions")).Source = Data?.Functions;
            DataTreeItemSource.First(x => Equals(x.Value, "CodeLocals")).Source = Data?.CodeLocals;
            DataTreeItemSource.First(x => Equals(x.Value, "Strings")).Source = Data?.Strings;
            DataTreeItemSource.First(x => Equals(x.Value, "EmbeddedTextures")).Source = Data?.EmbeddedTextures;
            DataTreeItemSource.First(x => Equals(x.Value, "EmbeddedAudio")).Source = Data?.EmbeddedAudio;
            DataTreeItemSource.First(x => Equals(x.Value, "TextureGroupInformation")).Source = Data?.TextureGroupInfo;
            DataTreeItemSource.First(x => Equals(x.Value, "EmbeddedImages")).Source = Data?.EmbeddedImages;
            DataTreeItemSource.First(x => Equals(x.Value, "ParticleSystems")).Source = Data?.ParticleSystems;
            DataTreeItemSource.First(x => Equals(x.Value, "ParticleSystemEmitters")).Source = Data?.ParticleSystemEmitters;

            DataTreeItem.UpdateSource();
        }
    }

    TreeItemViewModel? GetItem(object? source)
    {
        if (source is Control control)
        {
            ListBoxItem? listBoxItem;
            if (control is ListBoxItem)
                listBoxItem = control as ListBoxItem;
            else
                listBoxItem = control.FindLogicalAncestorOfType<ListBoxItem>();

            if (listBoxItem is not null)
            {
                if (listBoxItem.DataContext is TreeItemViewModel treeItem)
                {
                    return treeItem;
                }
            }
        }
        return null;
    }

    void OpenItem(object? source)
    {
        TreeItemViewModel? treeItem = GetItem(source);
        if (treeItem is not null)
        {
            treeItem.ExpandCollapse();
            mainVM.TabOpen(treeItem);
        }
    }

    public void SetFilter(string text)
    {
        foreach (TreeItemViewModel treeItem in DataTreeItemSource)
        {
            if (treeItem.Source is not null)
            {
                treeItem.SetFilter(value =>
                {
                    string name = value switch
                    {
                        UndertaleNamedResource namedResource => namedResource.Name.Content,
                        UndertaleString _string => _string.Content,
                        _ => "",
                    };

                    return name.Contains(text, System.StringComparison.CurrentCultureIgnoreCase);
                });
            }
        }
    }

    public void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        OpenItem(e.Source);
    }

    public void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.PhysicalKey == PhysicalKey.Enter)
        {
            OpenItem(e.Source);
        }
    }

    public void ListMenu_Add_Click(object? sender, RoutedEventArgs e)
    {
        if (mainVM.Data is not null)
        {
            if (e.Source is Control control)
            {
                ListBoxItem? listBoxItem = control.FindLogicalAncestorOfType<ListBoxItem>();
                if (listBoxItem is not null && listBoxItem.DataContext is TreeItemViewModel treeItem)
                {
                    // This could probably be better
                    IList list = (treeItem.Value switch
                    {
                        "AudioGroups" => mainVM.Data.AudioGroups as IList,
                        "Sounds" => mainVM.Data.Sounds as IList,
                        "Sprites" => mainVM.Data.Sprites as IList,
                        "Backgrounds" => mainVM.Data.Backgrounds as IList,
                        "Paths" => mainVM.Data.Paths as IList,
                        "Scripts" => mainVM.Data.Scripts as IList,
                        "Shaders" => mainVM.Data.Shaders as IList,
                        "Fonts" => mainVM.Data.Fonts as IList,
                        "Timelines" => mainVM.Data.Timelines as IList,
                        "GameObjects" => mainVM.Data.GameObjects as IList,
                        "Rooms" => mainVM.Data.Rooms as IList,
                        "Extensions" => mainVM.Data.Extensions as IList,
                        "TexturePageItems" => mainVM.Data.TexturePageItems as IList,
                        "Code" => mainVM.Data.Code as IList,
                        "Variables" => mainVM.Data.Variables as IList,
                        "Functions" => mainVM.Data.Functions as IList,
                        "CodeLocals" => mainVM.Data.CodeLocals as IList,
                        "Strings" => mainVM.Data.Strings as IList,
                        "EmbeddedTextures" => mainVM.Data.EmbeddedTextures as IList,
                        "EmbeddedAudio" => mainVM.Data.EmbeddedAudio as IList,
                        "TextureGroupInformation" => mainVM.Data.TextureGroupInfo as IList,
                        "EmbeddedImages" => mainVM.Data.EmbeddedImages as IList,
                        "ParticleSystems" => mainVM.Data.ParticleSystems as IList,
                        "ParticleSystemEmitters" => mainVM.Data.ParticleSystemEmitters as IList,
                        _ => null,
                    })!;

                    mainVM.DataItemAdd(list);
                }
            }
        }
    }

    private void ExpandCollapseButton_Click(object? sender, RoutedEventArgs e)
    {
        // BUG: This selects the item if you expand or collapse. It's to 'fix' a bug where if you click the button
        // while something else below in the list is selected, not only will it select that item (no,
        // AutoScrollToSelectedItem="False" doesn't seem to work), it'll also slowly scroll down to that item, while
        // locking up the GUI. No idea why this happens.
        if (e.Source is Control control)
        {
            ListBoxItem? listBoxItem = control.FindLogicalAncestorOfType<ListBoxItem>();
            if (listBoxItem is not null)
            {
                listBoxItem.IsSelected = true;
            }
        }
    }
}