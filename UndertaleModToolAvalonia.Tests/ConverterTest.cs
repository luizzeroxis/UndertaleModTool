using System.Globalization;
using Avalonia.Data;

namespace UndertaleModToolAvalonia.Tests;

public class ConverterTest
{
    [Fact]
    public void OneWayConverters_ReturnDoNothingOnConvertBack()
    {
        object?[] results =
        [
            new EnumTypeToValuesConverter().ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture),
            new EventsToExtendedEventConverter().ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture),
            new LevelToWidthConverter().ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture),
            new RoomItemToContextMenuConverter().ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture),
            new TreeDataGridItemToContextMenuConverter().ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture),
            new VersionCompareConverter().ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture),
        ];

        foreach (object? result in results)
            Assert.Same(BindingOperations.DoNothing, result);
    }

    [Theory]
    [InlineData("GE2", true)]
    [InlineData("GE2.0.0.0", true)]
    [InlineData("GE2.1", false)]
    [InlineData("L2.1", true)]
    [InlineData("L1.9", false)]
    public void VersionCompareConverter_ComparesVersionTuples(string parameter, bool expected)
    {
        object? result = new VersionCompareConverter().Convert(
            (2u, 0u, 1u, 0u),
            typeof(bool),
            parameter,
            CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("GE")]
    [InlineData("GE2.beta")]
    [InlineData("GE2.0.0.0.1")]
    public void VersionCompareConverter_ReturnsBindingErrorForInvalidParameters(string parameter)
    {
        object? result = new VersionCompareConverter().Convert(
            (2u, 0u, 1u, 0u),
            typeof(bool),
            parameter,
            CultureInfo.InvariantCulture);

        BindingNotification notification = Assert.IsType<BindingNotification>(result);
        Assert.Equal(BindingErrorType.Error, notification.ErrorType);
    }
}
