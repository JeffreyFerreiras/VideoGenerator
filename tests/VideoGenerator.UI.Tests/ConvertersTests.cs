using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FluentAssertions;
using NUnit.Framework;
using VideoGenerator.UI;
using System.Threading;

namespace VideoGenerator.UI.Tests
{
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class ConvertersTests
    {
        private static readonly IValueConverter BoolToVisibility = Converters.BoolToVisibilityConverter;
        private static readonly IValueConverter InverseBoolToVisibility = Converters.InverseBoolToVisibilityConverter;
        private static readonly IValueConverter InverseBool = Converters.InverseBoolConverter;
        private static readonly IValueConverter StringToBool = Converters.StringToBoolConverter;
        private static readonly IValueConverter BoolToLoadModelText = Converters.BoolToLoadModelText;

        [TestCase(true, Visibility.Visible)]
        [TestCase(false, Visibility.Collapsed)]
        public void BoolToVisibility_Convert_ReturnsExpected(bool input, Visibility expected)
        {
            var result = BoolToVisibility.Convert(input, null, null, CultureInfo.InvariantCulture);
            result.Should().Be(expected);
        }

        [TestCase(Visibility.Visible, true)]
        [TestCase(Visibility.Collapsed, false)]
        public void BoolToVisibility_ConvertBack_ReturnsExpected(Visibility input, bool expected)
        {
            var result = BoolToVisibility.ConvertBack(input, null, null, CultureInfo.InvariantCulture);
            result.Should().Be(expected);
        }

        [TestCase(true, Visibility.Collapsed)]
        [TestCase(false, Visibility.Visible)]
        public void InverseBoolToVisibility_Convert_ReturnsExpected(bool input, Visibility expected)
        {
            var result = InverseBoolToVisibility.Convert(input, null, null, CultureInfo.InvariantCulture);
            result.Should().Be(expected);
        }

        [TestCase(Visibility.Collapsed, true)]
        [TestCase(Visibility.Visible, false)]
        public void InverseBoolToVisibility_ConvertBack_ReturnsExpected(Visibility input, bool expected)
        {
            var result = InverseBoolToVisibility.ConvertBack(input, null, null, CultureInfo.InvariantCulture);
            result.Should().Be(expected);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void InverseBool_Convert_ReturnsExpected(bool input, bool expected)
        {
            var result = InverseBool.Convert(input, null, null, CultureInfo.InvariantCulture);
            result.Should().Be(expected);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void InverseBool_ConvertBack_ReturnsExpected(bool input, bool expected)
        {
            var result = InverseBool.ConvertBack(input, null, null, CultureInfo.InvariantCulture);
            result.Should().Be(expected);
        }

        [TestCase("hello", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void StringToBool_Convert_ReturnsExpected(string input, bool expected)
        {
            var result = StringToBool.Convert(input, null, null, CultureInfo.InvariantCulture);
            result.Should().Be(expected);
        }

        [Test]
        public void StringToBool_ConvertBack_ThrowsNotSupportedException()
        {
            Action act = () => StringToBool.ConvertBack("true", null, null, CultureInfo.InvariantCulture);
            act.Should().Throw<NotSupportedException>();
        }

        [TestCase(true, "Model Loaded ✓")]
        [TestCase(false, "Load Model")]
        public void BoolToLoadModelText_Convert_ReturnsExpected(bool input, string expected)
        {
            var result = BoolToLoadModelText.Convert(input, null, null, CultureInfo.InvariantCulture);
            result.Should().Be(expected);
        }

        [Test]
        public void BoolToLoadModelText_ConvertBack_ThrowsNotSupportedException()
        {
            Action act = () => BoolToLoadModelText.ConvertBack("Load Model", null, null, CultureInfo.InvariantCulture);
            act.Should().Throw<NotSupportedException>();
        }
    }
}