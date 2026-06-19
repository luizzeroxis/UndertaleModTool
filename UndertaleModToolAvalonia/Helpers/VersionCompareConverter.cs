using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace UndertaleModToolAvalonia;

/// <summary>
/// Checks if a version is greater then or equal, or less than, some value. Bind it to MainViewModel.DataVersion.
/// Parameter follows this pattern: [operation][major[.minor[.release[.build]]]]
/// Operation can be GE (greater or equal) or L (less than).
/// Usage:
/// <code>
/// {Binding $parent[l:MainView].((l:MainViewModel)DataContext).DataVersion,
/// Converter={StaticResource VersionCompareConverter},
/// ConverterParameter=2}
/// </code>
/// </summary>
public class VersionCompareConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ValueTuple<uint, uint, uint, uint> _version && parameter is string compareString)
        {
            (uint Major, uint Minor, uint Release, uint Build) version = _version;
            uint[] versionList = [version.Major, version.Minor, version.Release, version.Build];

            string operation = "GE";

            if (compareString.StartsWith("GE", StringComparison.Ordinal))
            {
                operation = "GE";
                compareString = compareString[("GE".Length)..];
            }
            else if (compareString.StartsWith("L", StringComparison.Ordinal))
            {
                operation = "L";
                compareString = compareString[("L".Length)..];
            }

            if (!TryParseVersion(compareString, out uint[] versionCompareList))
                return VersionCompareError(compareString);

            for (int i = 0; i < versionCompareList.Length; i++)
            {
                if (versionList[i] != versionCompareList[i])
                    if (operation == "GE")
                        return versionList[i] > versionCompareList[i];
                    else if (operation == "L")
                        return versionList[i] < versionCompareList[i];
            }

            if (operation == "GE")
                return true;
            else if (operation == "L")
                return false;
        }

        return VersionCompareError(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }

    private static bool TryParseVersion(string compareString, out uint[] versionCompareList)
    {
        versionCompareList = [];

        string[] parts = compareString.Split('.');
        if (parts.Length == 0 || parts.Length > 4)
            return false;

        versionCompareList = new uint[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!uint.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out uint versionPart))
            {
                versionCompareList = [];
                return false;
            }

            versionCompareList[i] = versionPart;
        }

        return true;
    }

    private static BindingNotification VersionCompareError(object? parameter)
    {
        return new BindingNotification(
            new InvalidOperationException($"Invalid version comparison parameter: {parameter ?? "<null>"}"),
            BindingErrorType.Error);
    }
}
