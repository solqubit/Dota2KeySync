using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Dota2KeySync.App
{
    /// <summary>Dark, borderless confirmation dialog.</summary>
    public class ConfirmDialog : Window
    {
        bool confirmed;
        CheckBox check;

        ConfirmDialog(Window owner, string title, string message, string confirmText, bool danger, string checkText)
        {
            Owner = owner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.Height;
            Width = 440;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = Theme.Font;

            var card = new Border
            {
                Background = Theme.Brush(Theme.Surface),
                BorderBrush = Theme.Brush(Theme.Line),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24, 22, 24, 20),
                Margin = new Thickness(16),
                Effect = new DropShadowEffect { BlurRadius = 30, ShadowDepth = 6, Opacity = 0.5, Color = Colors.Black },
            };

            var stack = new StackPanel();
            card.Child = stack;

            var head = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            var iconColor = danger ? Theme.Accent : Theme.Teal;
            var badge = new Border
            {
                Width = 34, Height = 34, CornerRadius = new CornerRadius(10),
                Background = Theme.Brush(iconColor, 0.16),
                Child = new TextBlock
                {
                    Text = danger ? Theme.IconWarning : Theme.IconKeyboard,
                    FontFamily = Theme.Icons, FontSize = 16,
                    Foreground = Theme.Brush(iconColor),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                },
            };
            head.Children.Add(badge);
            head.Children.Add(new TextBlock
            {
                Text = title, FontSize = 17, FontWeight = FontWeights.SemiBold,
                Foreground = Theme.Brush(Theme.Text), VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
            });
            stack.Children.Add(head);

            stack.Children.Add(new TextBlock
            {
                Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 13.5, LineHeight = 20,
                Foreground = Theme.Brush(Theme.Muted), Margin = new Thickness(0, 0, 0, 22),
            });

            if (!string.IsNullOrEmpty(checkText))
            {
                check = new CheckBox
                {
                    Content = new TextBlock { Text = checkText, FontSize = 13, TextWrapping = TextWrapping.Wrap },
                    IsChecked = false,
                    Margin = new Thickness(0, -8, 0, 22),
                };
                stack.Children.Add(check);
            }

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = new Button { Content = "Cancelar", Style = (Style)Application.Current.Resources["GhostBtn"], IsCancel = true };
            cancel.Click += (s, e) => Close();
            var ok = new Button
            {
                Content = confirmText, Margin = new Thickness(10, 0, 0, 0),
                Style = (Style)Application.Current.Resources["PrimaryBtn"],
                Padding = new Thickness(18, 8, 18, 8), FontSize = 13,
            };
            if (!danger)
            {
                ok.Background = new LinearGradientBrush(Theme.Hex("#3CC4D6"), Theme.Hex("#2394A6"), 45);
            }
            ok.Click += (s, e) => { confirmed = true; Close(); };
            buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            stack.Children.Add(buttons);

            Content = card;
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

            Opacity = 0;
            Loaded += (s, e) => BeginAnimation(OpacityProperty, new DoubleAnimation(1, new Duration(System.TimeSpan.FromMilliseconds(140))));
        }

        public static bool Ask(Window owner, string title, string message, string confirmText, bool danger)
        {
            bool unused;
            return Ask(owner, title, message, confirmText, danger, null, out unused);
        }

        /// <summary>Shows the dialog; when checkText is given, an unchecked checkbox is added and its state returned.</summary>
        public static bool Ask(Window owner, string title, string message, string confirmText, bool danger,
                               string checkText, out bool isChecked)
        {
            var dlg = new ConfirmDialog(owner, title, message, confirmText, danger, checkText);
            dlg.ShowDialog();
            isChecked = dlg.confirmed && dlg.check != null && dlg.check.IsChecked == true;
            return dlg.confirmed;
        }
    }
}
