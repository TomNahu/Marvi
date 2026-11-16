using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace Marvi
{
    // Las dos ventanas de estudio comparten controles accesibles y el marco de madera del menú.
    internal abstract class StudySurface : Window
    {
        protected readonly StackPanel body = new StackPanel { Margin = new Thickness(14, 9, 14, 12) };
        protected static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(61, 43, 34));
        protected static readonly Brush Leaf = new SolidColorBrush(Color.FromRgb(41, 91, 76));
        private static readonly Brush Sand = new SolidColorBrush(Color.FromRgb(249, 228, 189));
        private static readonly Brush Edge = new SolidColorBrush(Color.FromRgb(130, 97, 66));
        private readonly ScrollViewer scroll;

        protected StudySurface(string title)
        {
            Title = title;
            Language = XmlLanguage.GetLanguage("es-AR");
            Width = 365;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.CanMinimize;
            Topmost = true;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 14;
            Foreground = Ink;
            var theme = new ResourceDictionary { Source = new Uri("pack://application:,,,/Marvi;component/MenuTheme.xaml") };
            scroll = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var paper = new Border { Style = (Style)theme["Paper"], Child = scroll };
            Content = new Border { Style = (Style)theme["Frame"], Child = paper };
        }

        protected void ClearPage() { body.Children.Clear(); scroll.ScrollToTop(); }

        protected TextBlock Text(string value, double size)
        {
            var label = new TextBlock { Text = value, FontSize = size, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 5) };
            body.Children.Add(label);
            return label;
        }

        protected void Heading(string value) { Text(value, 20).FontWeight = FontWeights.SemiBold; }

        protected TextBox Entry(string label, string value, bool multiline)
        {
            Text(label, 14).FontWeight = FontWeights.SemiBold;
            var input = new TextBox { Text = value ?? "", Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 8),
                MaxLength = multiline ? 2000 : 400, TextWrapping = TextWrapping.Wrap, AcceptsReturn = multiline,
                MinHeight = multiline ? 48 : 28, MaxHeight = 90, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            AutomationProperties.SetName(input, label);
            body.Children.Add(input);
            return input;
        }

        protected static Button MakeButton(string label, Action action, bool primary)
        {
            var button = new Button { Content = label, Foreground = primary ? Brushes.White : Ink,
                Background = primary ? Leaf : Sand, Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 4, 0, 0), BorderBrush = Edge };
            button.Click += (s,e) => action();
            return button;
        }

        protected Button ActionButton(string label, Action action, bool primary = false)
        {
            var button = MakeButton(label, action, primary);
            body.Children.Add(button);
            return button;
        }
    }
}
