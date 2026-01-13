using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MedReminder.Pages
{
    public class ReminderTimeToColorConverter : IValueConverter
    {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not TimeSpan reminderTime)
                return Colors.Transparent;

            var now = DateTime.Now.TimeOfDay;
            var deltaMinutes = (reminderTime - now).TotalMinutes;

            int windowMinutes = 60;
            if (parameter is string s && int.TryParse(s, out var parsed))
                windowMinutes = parsed;

            if (deltaMinutes < 0)
                return Color.FromArgb("#FFE0E0E0");

            if (deltaMinutes <= 5)
                return Color.FromArgb("#FFFFCDD2");

            if (deltaMinutes <= windowMinutes)
                return Color.FromArgb("#FFFFF9C4");

            return Colors.White;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class IntToDoubleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is int i ? (double)i : 0d;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is double d ? (int)Math.Round(d) : 0;
    }

    public class PhoneNumberBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender!;
            var digits = new string(e.NewTextValue?.Where(char.IsDigit).ToArray() ?? []);

            entry.Text = digits.Length switch
            {
                > 6 => $"({digits[..3]}) {digits[3..6]}-{digits[6..Math.Min(10, digits.Length)]}",
                > 3 => $"({digits[..3]}) {digits[3..]}",
                > 0 => $"({digits}",
                _ => ""
            };
        }
    }

    public class SinBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender!;
            var digits = new string(e.NewTextValue?.Where(char.IsDigit).ToArray() ?? []);

            entry.Text = digits.Length switch
            {
                > 6 => $"{digits[..3]}-{digits[3..6]}-{digits[6..Math.Min(9, digits.Length)]}",
                > 3 => $"{digits[..3]}-{digits[3..]}",
                > 0 => digits,
                _ => ""
            };
        }
    }

    public class DobBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender!;

            // Digits only
            var digits = new string(e.NewTextValue?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());

            if (digits.Length >= 8)
                digits = digits[..8]; // YYYYMMDD max

            string formatted = digits.Length switch
            {
                >= 7 => $"{digits[..4]}-{digits[4..6]}-{digits[6..]}",
                >= 5 => $"{digits[..4]}-{digits[4..]}",
                >= 1 => digits,
                _ => ""
            };

            if (entry.Text != formatted)
                entry.Text = formatted;
        }
    }
    public class StringNotEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !string.IsNullOrWhiteSpace(value as string);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}