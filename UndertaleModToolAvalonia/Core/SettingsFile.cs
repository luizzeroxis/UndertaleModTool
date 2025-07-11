using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModToolAvalonia.Views;
using System.ComponentModel;
using System.Globalization;
using UndertaleModToolAvalonia.Assets;
using UndertaleModToolAvalonia.Controls;

namespace UndertaleModToolAvalonia.Core;

public partial class SettingsFile
{
    public MainViewModel MainVM = null!;

    public SettingsFile() { }
    public SettingsFile(IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();
    }

    public static async Task<SettingsFile> Load(IServiceProvider serviceProvider)
    {
        MainViewModel mainVM = serviceProvider.GetRequiredService<MainViewModel>();

        string roamingAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModToolAvalonia");

        try
        {
            string path = Path.Join(roamingAppData, "Settings.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                SettingsFile? settings = JsonSerializer.Deserialize<SettingsFile>(json, new JsonSerializerOptions()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });

                if (settings is not null)
                {
                    settings.MainVM = mainVM;
                    return settings;
                }
            }
        }
        catch (Exception e)
        {
            await mainVM.ShowMessageDialog($"Error when loading settings file: {e.Message}");
            throw;
        }

        return new SettingsFile(serviceProvider);
    }

    public async void Save()
    {
        string roamingAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModToolAvalonia");
        Directory.CreateDirectory(roamingAppData);

        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
        });

        try
        {
            File.WriteAllText(Path.Join(roamingAppData, "Settings.json"), json);
        }
        catch (Exception e)
        {
            await MainVM.ShowMessageDialog($"Error when saving settings file: {e.Message}");
        }
    }

    public string Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?.?.?.?";

    public enum ThemeValue
    {
        SystemDefault = 0,
        Light = 1,
        Dark = 2,
    }
    
    public enum LanguageValue
    {
        English = 0,
        ChineseSimplified = 1
    }
    
    private LanguageValue _Language = GetDefaultLanguage();
    public LanguageValue Language
    {
        get => _Language;
        set
        {
            if (_Language != value)
            {
                _Language = value;
                OnLanguageChanged();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            }
        }
    }


    public LanguageValue CurrentLanguage
    {
        get
        {
            var name = CultureInfo.CurrentUICulture.Name;
            if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return LanguageValue.ChineseSimplified;
            return LanguageValue.English;
        }
    }

    private static LanguageValue GetDefaultLanguage()
    {
        var name = CultureInfo.CurrentUICulture.Name;
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return LanguageValue.ChineseSimplified;
        return LanguageValue.English;
    }
    
    [Notify]
    private ThemeValue _Theme;

    void OnThemeChanged()
    {
        if (App.Current is not null)
        {
            App.Current.RequestedThemeVariant = Theme switch
            {
                ThemeValue.SystemDefault => ThemeVariant.Default,
                ThemeValue.Light => ThemeVariant.Light,
                ThemeValue.Dark => ThemeVariant.Dark,
                _ => throw new NotImplementedException(),
            };
        }
        Save();
    }
    
        
    void OnLanguageChanged()
    {
        MessageWindow window = new MessageWindow(titleText: Resources.LanguageChangedTitle,
            message: Resources.RestartToApplyNewLanguageText, hasNoButton: true, hasYesButton: true);
        window.Initialize();
        window.Show();
        switch (_Language)
        {
            case LanguageValue.English:
                CultureInfo.CurrentUICulture = new CultureInfo("en");
                break;
            case LanguageValue.ChineseSimplified:
                CultureInfo.CurrentUICulture = new CultureInfo("zh-Hans");
                break;
        }

    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

