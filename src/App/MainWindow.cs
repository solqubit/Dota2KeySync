using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Dota2KeySync.Core;

namespace Dota2KeySync.App
{
    public class MainWindow : Window
    {
        string steamPath;
        List<SteamAccount> accounts = new List<SteamAccount>();
        SteamAccount origin;
        SteamAccount target;
        List<string> blockers = new List<string>();

        readonly StackPanel cardList = new StackPanel();
        readonly TextBlock countText = new TextBlock();
        readonly Border originSlot = new Border();
        readonly Border targetSlot = new Border();
        readonly Button copyButton = new Button();
        readonly Ellipse statusDot = new Ellipse { Width = 8, Height = 8 };
        readonly TextBlock statusText = new TextBlock();
        readonly Border dimmer = new Border();

        string flashMessage;
        Color flashColor;
        DateTime flashUntil = DateTime.MinValue;

        public MainWindow()
        {
            Title = "Dota2KeySync";
            Width = 860;
            Height = 680;
            MinWidth = 700;
            MinHeight = 520;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.CanMinimize;
            Background = Brushes.Transparent;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = Theme.Font;
            Foreground = Theme.Brush(Theme.Text);
            Icon = BuildIcon();
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            Content = BuildShell();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            timer.Tick += (s, e) => RefreshBlockers();
            timer.Start();

            Loaded += (s, e) => { Reload(); RefreshBlockers(); };
        }

        // ------------------------------------------------------------------ layout

        UIElement BuildShell()
        {
            var outer = new Border
            {
                Margin = new Thickness(12),
                CornerRadius = new CornerRadius(16),
                Background = Theme.Brush(Theme.Bg),
                BorderBrush = Theme.Brush(Theme.Line),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 4, Opacity = 0.45, Color = Colors.Black },
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // subtle red glow in the top-left corner
            var glow = new Border
            {
                CornerRadius = new CornerRadius(16),
                IsHitTestVisible = false,
                Background = new RadialGradientBrush
                {
                    Center = new Point(0.05, 0), GradientOrigin = new Point(0.05, 0),
                    RadiusX = 0.7, RadiusY = 0.55,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(46, 229, 69, 47), 0),
                        new GradientStop(Color.FromArgb(0, 229, 69, 47), 1),
                    },
                },
            };
            Grid.SetRowSpan(glow, 5);
            root.Children.Add(glow);

            var title = BuildTitleBar();
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            var header = BuildSectionHeader();
            Grid.SetRow(header, 1);
            root.Children.Add(header);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 4, 18, 8),
                Content = cardList,
            };
            Grid.SetRow(scroll, 2);
            root.Children.Add(scroll);

            var action = BuildActionBar();
            Grid.SetRow(action, 3);
            root.Children.Add(action);

            var status = BuildStatusBar();
            Grid.SetRow(status, 4);
            root.Children.Add(status);

            dimmer.Background = Theme.Brush(Colors.Black, 0.45);
            dimmer.CornerRadius = new CornerRadius(16);
            dimmer.Visibility = Visibility.Collapsed;
            Grid.SetRowSpan(dimmer, 5);
            root.Children.Add(dimmer);

            outer.Child = root;
            return outer;
        }

        UIElement BuildTitleBar()
        {
            var bar = new Grid { Margin = new Thickness(20, 16, 12, 6), Background = Brushes.Transparent };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bar.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

            var logo = new Border
            {
                Width = 40, Height = 40, CornerRadius = new CornerRadius(12),
                Background = new LinearGradientBrush(Theme.Hex("#F25A3F"), Theme.Hex("#A92A19"), 45),
                Child = new TextBlock
                {
                    Text = Theme.IconKeyboard, FontFamily = Theme.Icons, FontSize = 19, Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                },
            };
            bar.Children.Add(logo);

            var titles = new StackPanel { Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var name = new TextBlock { FontSize = 18, FontWeight = FontWeights.Bold };
            name.Inlines.Add(new System.Windows.Documents.Run("Dota2") { Foreground = Theme.Brush(Theme.Text) });
            name.Inlines.Add(new System.Windows.Documents.Run("KeySync") { Foreground = Theme.Brush(Theme.Accent) });
            titles.Children.Add(name);
            titles.Children.Add(new TextBlock
            {
                Text = "Pasa tus teclas de Dota 2 entre cuentas de Steam",
                FontSize = 12, Foreground = Theme.Brush(Theme.Muted),
            });
            Grid.SetColumn(titles, 1);
            bar.Children.Add(titles);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
            var refresh = TitleButton(Theme.IconRefresh, "TitleBtn", "Volver a leer las cuentas");
            refresh.Click += (s, e) => { Reload(); Flash("Cuentas actualizadas", Theme.Teal, 1.5); };
            var min = TitleButton(Theme.IconMinimize, "TitleBtn", "Minimizar");
            min.Click += (s, e) => WindowState = WindowState.Minimized;
            var close = TitleButton(Theme.IconClose, "CloseBtn", "Cerrar");
            close.Click += (s, e) => Close();
            buttons.Children.Add(refresh);
            buttons.Children.Add(min);
            buttons.Children.Add(close);
            Grid.SetColumn(buttons, 2);
            bar.Children.Add(buttons);
            return bar;
        }

        Button TitleButton(string glyph, string style, string tip)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources[style],
                Content = new TextBlock { Text = glyph, FontFamily = Theme.Icons, FontSize = 12 },
                ToolTip = tip,
                Margin = new Thickness(2, 0, 0, 0),
            };
            System.Windows.Automation.AutomationProperties.SetName(b, tip);
            return b;
        }

        UIElement BuildSectionHeader()
        {
            var panel = new Grid { Margin = new Thickness(24, 14, 24, 10) };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(new TextBlock
            {
                Text = "CUENTAS EN ESTE PC", FontSize = 11.5, FontWeight = FontWeights.Bold,
                Foreground = Theme.Brush(Theme.Muted), VerticalAlignment = VerticalAlignment.Center,
            });
            countText.FontSize = 11.5;
            countText.FontWeight = FontWeights.Bold;
            countText.Foreground = Theme.Brush(Theme.Text);
            countText.VerticalAlignment = VerticalAlignment.Center;
            left.Children.Add(new Border
            {
                Margin = new Thickness(10, 0, 0, 0), Padding = new Thickness(8, 1, 8, 2),
                CornerRadius = new CornerRadius(8), Background = Theme.Brush(Theme.Surface2), Child = countText,
            });
            panel.Children.Add(left);

            var hint = new TextBlock
            {
                FontSize = 12, Foreground = Theme.Brush(Theme.Muted), VerticalAlignment = VerticalAlignment.Center,
            };
            hint.Inlines.Add(new System.Windows.Documents.Run("Marca "));
            hint.Inlines.Add(new System.Windows.Documents.Run("ORIGEN") { Foreground = Theme.Brush(Theme.Accent), FontWeight = FontWeights.Bold });
            hint.Inlines.Add(new System.Windows.Documents.Run(" y "));
            hint.Inlines.Add(new System.Windows.Documents.Run("DESTINO") { Foreground = Theme.Brush(Theme.Teal), FontWeight = FontWeights.Bold });
            Grid.SetColumn(hint, 1);
            panel.Children.Add(hint);
            return panel;
        }

        UIElement BuildActionBar()
        {
            var bar = new Border
            {
                Margin = new Thickness(24, 8, 24, 10),
                Padding = new Thickness(14),
                CornerRadius = new CornerRadius(14),
                Background = Theme.Brush(Theme.Surface),
                BorderBrush = Theme.Brush(Theme.Line),
                BorderThickness = new Thickness(1),
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(originSlot);

            var arrow = new Border
            {
                Width = 34, Height = 34, CornerRadius = new CornerRadius(17), Margin = new Thickness(10, 0, 10, 0),
                Background = Theme.Brush(Theme.Surface2),
                Child = new TextBlock
                {
                    Text = Theme.IconArrow, FontFamily = Theme.Icons, FontSize = 13, Foreground = Theme.Brush(Theme.Muted),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                },
            };
            Grid.SetColumn(arrow, 1);
            grid.Children.Add(arrow);

            Grid.SetColumn(targetSlot, 2);
            grid.Children.Add(targetSlot);

            copyButton.Style = (Style)Application.Current.Resources["PrimaryBtn"];
            copyButton.Margin = new Thickness(14, 0, 0, 0);
            var cbContent = new StackPanel { Orientation = Orientation.Horizontal };
            cbContent.Children.Add(new TextBlock { Text = Theme.IconKeyboard, FontFamily = Theme.Icons, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 1, 9, 0) });
            cbContent.Children.Add(new TextBlock { Text = "Copiar teclas", VerticalAlignment = VerticalAlignment.Center });
            copyButton.Content = cbContent;
            copyButton.Click += (s, e) => DoCopy();
            ToolTipService.SetShowOnDisabled(copyButton, true);
            Grid.SetColumn(copyButton, 3);
            grid.Children.Add(copyButton);

            bar.Child = grid;
            return bar;
        }

        UIElement BuildStatusBar()
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(28, 0, 24, 16) };
            statusDot.VerticalAlignment = VerticalAlignment.Center;
            statusText.FontSize = 12;
            statusText.Margin = new Thickness(9, 0, 0, 0);
            statusText.VerticalAlignment = VerticalAlignment.Center;
            panel.Children.Add(statusDot);
            panel.Children.Add(statusText);
            return panel;
        }

        // ------------------------------------------------------------------ data

        void Reload()
        {
            steamPath = SteamLocator.FindSteamPath();
            accounts = steamPath == null ? new List<SteamAccount>() : AccountScanner.Scan(steamPath);

            // keep selections that still exist
            origin = origin == null ? null : accounts.FirstOrDefault(a => a.AccountId == origin.AccountId && a.HasDotaKeys);
            target = target == null ? null : accounts.FirstOrDefault(a => a.AccountId == target.AccountId);

            RenderCards(true);
            RenderSlots();
            UpdateState();
        }

        void RefreshBlockers()
        {
            List<string> now;
            try { now = KeyOps.RunningBlockers(); }
            catch { return; }
            bool changed = now.Count != blockers.Count || now.Except(blockers).Any();
            bool justClosed = blockers.Count > 0 && now.Count == 0;
            blockers = now;
            if (justClosed) Reload();          // Steam may have changed who is remembered
            else if (changed) RenderCards(false);
            UpdateState();
        }

        void UpdateState()
        {
            bool blocked = blockers.Count > 0;
            bool ready = origin != null && target != null && origin.AccountId != target.AccountId;
            copyButton.IsEnabled = ready && !blocked;
            if (blocked) copyButton.ToolTip = "Cierra " + string.Join(" y ", blockers) + " antes de copiar";
            else if (!ready) copyButton.ToolTip = "Elige una cuenta de origen y una de destino";
            else copyButton.ToolTip = "Reemplaza las teclas de " + target.DisplayName + " por las de " + origin.DisplayName;

            countText.Text = accounts.Count.ToString();

            if (DateTime.Now < flashUntil)
            {
                SetStatus(flashColor, flashMessage);
            }
            else if (steamPath == null)
            {
                SetStatus(Theme.Accent, "No se encontró Steam en este PC.");
            }
            else if (blocked)
            {
                SetStatus(Theme.Amber, string.Join(" y ", blockers) + (blockers.Count > 1 ? " están abiertos" : " está abierto") +
                    ": ciérralo para poder copiar, importar o eliminar (Steam Cloud podría devolver las teclas antiguas).");
            }
            else if (accounts.Any(a => a.RememberedUnknown))
            {
                SetStatus(Theme.Amber, "No se pudo leer la lista de cuentas de Steam. Pulsa actualizar; eliminar está bloqueado mientras tanto.");
            }
            else
            {
                int forgotten = accounts.Count(a => !a.Remembered);
                SetStatus(Theme.Green, "Listo. Steam y Dota 2 están cerrados." +
                    (forgotten > 0 ? "  " + forgotten + (forgotten == 1 ? " cuenta olvidada" : " cuentas olvidadas") + " se puede eliminar." : ""));
            }
        }

        void SetStatus(Color c, string text)
        {
            statusDot.Fill = Theme.Brush(c);
            statusText.Foreground = Theme.Brush(Color.FromRgb(
                (byte)((c.R + Theme.Text.R * 2) / 3), (byte)((c.G + Theme.Text.G * 2) / 3), (byte)((c.B + Theme.Text.B * 2) / 3)));
            statusText.Text = text;
        }

        void Flash(string text, Color c)
        {
            Flash(text, c, 7);
        }

        void Flash(string text, Color c, double seconds)
        {
            flashMessage = text;
            flashColor = c;
            flashUntil = DateTime.Now.AddSeconds(seconds);
            UpdateState();
        }

        // ------------------------------------------------------------------ cards

        void RenderCards(bool animate)
        {
            cardList.Children.Clear();
            if (steamPath == null)
            {
                cardList.Children.Add(EmptyState("No se encontró Steam", "Instala Steam o inicia sesión al menos una vez y pulsa actualizar."));
                return;
            }
            if (accounts.Count == 0)
            {
                cardList.Children.Add(EmptyState("Sin cuentas", "No hay carpetas de cuentas en " + steamPath + "\\userdata."));
                return;
            }
            for (int i = 0; i < accounts.Count; i++)
            {
                var card = BuildCard(accounts[i]);
                cardList.Children.Add(card);
                if (animate) AnimateIn(card, i);
            }
        }

        static void AnimateIn(FrameworkElement el, int index)
        {
            var tt = new TranslateTransform(0, 10);
            el.RenderTransform = tt;
            el.Opacity = 0;
            var delay = TimeSpan.FromMilliseconds(45 * index);
            var dur = new Duration(TimeSpan.FromMilliseconds(260));
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            el.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, dur) { BeginTime = delay, EasingFunction = ease });
            tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(10, 0, dur) { BeginTime = delay, EasingFunction = ease });
        }

        UIElement EmptyState(string title, string text)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 60, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
            sp.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = text, FontSize = 13, Foreground = Theme.Brush(Theme.Muted), Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
            return sp;
        }

        FrameworkElement BuildCard(SteamAccount a)
        {
            bool isOrigin = origin != null && origin.AccountId == a.AccountId;
            bool isTarget = target != null && target.AccountId == a.AccountId;
            Color edge = isOrigin ? Theme.Accent : isTarget ? Theme.Teal : Theme.Line;

            var card = new Border
            {
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(14, 12, 14, 12),
                CornerRadius = new CornerRadius(14),
                Background = (isOrigin || isTarget)
                    ? (Brush)new LinearGradientBrush(Color.FromArgb(34, edge.R, edge.G, edge.B), Theme.Surface, 0)
                    : Theme.Brush(Theme.Surface),
                BorderBrush = Theme.Brush(edge),
                BorderThickness = new Thickness(isOrigin || isTarget ? 1.5 : 1),
            };
            if (!isOrigin && !isTarget)
            {
                card.MouseEnter += (s, e) => card.BorderBrush = Theme.Brush(Theme.Hex("#3A4050"));
                card.MouseLeave += (s, e) => card.BorderBrush = Theme.Brush(Theme.Line);
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(Avatar(a, 48));

            var info = new StackPanel { Margin = new Thickness(14, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text = a.DisplayName, FontSize = 15.5, FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            string sub = (string.IsNullOrEmpty(a.AccountName) ? "" : "@" + a.AccountName + "  ·  ") + "ID " + a.AccountId;
            info.Children.Add(new TextBlock
            {
                Text = sub, FontSize = 12, Foreground = Theme.Brush(Theme.Muted), Margin = new Thickness(0, 1, 0, 6),
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            var badges = new WrapPanel();
            badges.Children.Add(a.RememberedUnknown ? Pill("¿Recordada?", Theme.Muted)
                : a.Remembered ? Pill("Recordada", Theme.Green) : Pill("Olvidada", Theme.Amber));
            badges.Children.Add(a.HasDotaKeys ? Pill(Theme.IconCheck + " Teclas de Dota", Theme.Teal) : Pill("Sin teclas", Theme.Muted));
            if (a.MostRecent) badges.Children.Add(Pill("Última sesión", Theme.Blue));
            info.Children.Add(badges);
            Grid.SetColumn(info, 1);
            grid.Children.Add(info);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var originBtn = RoleButton("ORIGEN", Theme.Accent, isOrigin);
            originBtn.IsEnabled = a.HasDotaKeys;
            originBtn.ToolTip = a.HasDotaKeys ? "Copiar las teclas DE esta cuenta" : "Esta cuenta no tiene teclas de Dota 2 guardadas";
            ToolTipService.SetShowOnDisabled(originBtn, true);
            originBtn.Click += (s, e) =>
            {
                origin = isOrigin ? null : a;
                if (origin != null && target != null && target.AccountId == origin.AccountId) target = null;
                RenderCards(false); RenderSlots(); UpdateState();
            };
            var targetBtn = RoleButton("DESTINO", Theme.Teal, isTarget);
            targetBtn.ToolTip = "Copiar las teclas A esta cuenta (se reemplazan)";
            targetBtn.Margin = new Thickness(6, 0, 0, 0);
            targetBtn.Click += (s, e) =>
            {
                target = isTarget ? null : a;
                if (target != null && origin != null && origin.AccountId == target.AccountId) origin = null;
                RenderCards(false); RenderSlots(); UpdateState();
            };
            actions.Children.Add(originBtn);
            actions.Children.Add(targetBtn);

            var exportBtn = IconAction(Theme.IconExport, Theme.Text, a.HasDotaKeys,
                a.HasDotaKeys ? "Exportar teclas a un .zip" : "Esta cuenta no tiene teclas para exportar", "Exportar teclas de " + a.DisplayName);
            exportBtn.Margin = new Thickness(10, 0, 0, 0);
            exportBtn.Click += (s, e) => DoExport(a);
            actions.Children.Add(exportBtn);

            var importBtn = IconAction(Theme.IconImport, Theme.Text, blockers.Count == 0,
                blockers.Count == 0 ? "Importar teclas desde un .zip a esta cuenta" : "Cierra " + string.Join(" y ", blockers) + " para poder importar",
                "Importar teclas a " + a.DisplayName);
            importBtn.Margin = new Thickness(6, 0, 0, 0);
            importBtn.Click += (s, e) => DoImport(a);
            actions.Children.Add(importBtn);

            if (!a.Remembered && !a.RememberedUnknown)
            {
                var del = new Button
                {
                    Style = (Style)Application.Current.Resources["Btn"],
                    Background = Theme.Brush(Theme.Accent, 0.12),
                    Foreground = Theme.Brush(Theme.Hex("#FF7A66")),
                    Padding = new Thickness(11, 8, 11, 8),
                    Margin = new Thickness(10, 0, 0, 0),
                    Content = new TextBlock { Text = Theme.IconDelete, FontFamily = Theme.Icons, FontSize = 14 },
                    IsEnabled = blockers.Count == 0,
                    ToolTip = blockers.Count == 0
                        ? "Eliminar la carpeta de esta cuenta olvidada"
                        : "Cierra " + string.Join(" y ", blockers) + " para poder eliminar",
                };
                ToolTipService.SetShowOnDisabled(del, true);
                System.Windows.Automation.AutomationProperties.SetName(del, "Eliminar " + a.DisplayName);
                del.Click += (s, e) => DoDelete(a);
                actions.Children.Add(del);
            }
            Grid.SetColumn(actions, 2);
            grid.Children.Add(actions);

            card.Child = grid;
            return card;
        }

        Button IconAction(string glyph, Color c, bool enabled, string tip, string name)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["Btn"],
                Background = Theme.Brush(Theme.Surface2),
                Foreground = Theme.Brush(c),
                Padding = new Thickness(11, 8, 11, 8),
                Content = new TextBlock { Text = glyph, FontFamily = Theme.Icons, FontSize = 14 },
                IsEnabled = enabled,
                ToolTip = tip,
            };
            ToolTipService.SetShowOnDisabled(b, true);
            System.Windows.Automation.AutomationProperties.SetName(b, name);
            return b;
        }

        Button RoleButton(string text, Color c, bool active)
        {
            return new Button
            {
                Style = (Style)Application.Current.Resources["Btn"],
                Content = text,
                FontSize = 11.5,
                Padding = new Thickness(12, 7, 12, 7),
                Background = active ? Theme.Brush(c) : Theme.Brush(c, 0.10),
                BorderBrush = active ? Theme.Brush(c) : Theme.Brush(c, 0.35),
                Foreground = active ? Brushes.White : Theme.Brush(c),
            };
        }

        static UIElement Pill(string text, Color c)
        {
            return new Border
            {
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(8, 2, 8, 3),
                CornerRadius = new CornerRadius(7),
                Background = Theme.Brush(c, 0.14),
                Child = new TextBlock
                {
                    Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Theme.Brush(c),
                    FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Segoe Fluent Icons, Segoe MDL2 Assets"),
                },
            };
        }

        static readonly Color[] AvatarColors =
        {
            Theme.Hex("#E5452F"), Theme.Hex("#33B6C8"), Theme.Hex("#6C8CFF"), Theme.Hex("#B36CFF"),
            Theme.Hex("#35C27A"), Theme.Hex("#F0A43A"), Theme.Hex("#FF6CA8"),
        };

        static FrameworkElement Avatar(SteamAccount a, double size)
        {
            if (a.AvatarPath != null)
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.UriSource = new Uri(a.AvatarPath);
                    bmp.DecodePixelWidth = (int)(size * 2);
                    bmp.EndInit();
                    bmp.Freeze();
                    return new Border
                    {
                        Width = size, Height = size, CornerRadius = new CornerRadius(size * 0.3),
                        Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill },
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                }
                catch
                {
                    // fall back to initials
                }
            }
            long n;
            long.TryParse(a.AccountId, out n);
            var c = AvatarColors[(int)(n % AvatarColors.Length)];
            string initial = string.IsNullOrEmpty(a.DisplayName) ? "?"
                : System.Globalization.StringInfo.GetNextTextElement(a.DisplayName).ToUpperInvariant();
            return new Border
            {
                Width = size, Height = size, CornerRadius = new CornerRadius(size * 0.3),
                Background = new LinearGradientBrush(c, Color.FromRgb((byte)(c.R * 0.6), (byte)(c.G * 0.6), (byte)(c.B * 0.6)), 45),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = initial, FontSize = size * 0.42, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                },
            };
        }

        void RenderSlots()
        {
            FillSlot(originSlot, origin, "ORIGEN", Theme.Accent, "Elige de dónde copiar");
            FillSlot(targetSlot, target, "DESTINO", Theme.Teal, "Elige qué cuenta recibe");
        }

        static void FillSlot(Border slot, SteamAccount a, string label, Color c, string empty)
        {
            slot.CornerRadius = new CornerRadius(11);
            slot.Padding = new Thickness(10, 8, 10, 8);
            slot.Background = a == null ? Theme.Brush(Theme.Bg) : Theme.Brush(c, 0.08);
            slot.BorderBrush = a == null ? Theme.Brush(Theme.Line) : Theme.Brush(c, 0.5);
            slot.BorderThickness = new Thickness(1);

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            if (a != null)
            {
                sp.Children.Add(Avatar(a, 32));
            }
            else
            {
                sp.Children.Add(new Border
                {
                    Width = 32, Height = 32, CornerRadius = new CornerRadius(10),
                    BorderBrush = Theme.Brush(c, 0.5), BorderThickness = new Thickness(1.5),
                    Background = Theme.Brush(c, 0.06),
                });
            }
            var texts = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            texts.Children.Add(new TextBlock { Text = label, FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = Theme.Brush(c) });
            texts.Children.Add(new TextBlock
            {
                Text = a == null ? empty : a.DisplayName, FontSize = 13.5,
                FontWeight = a == null ? FontWeights.Normal : FontWeights.SemiBold,
                Foreground = a == null ? Theme.Brush(Theme.Muted) : Theme.Brush(Theme.Text),
                TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 190,
            });
            sp.Children.Add(texts);
            slot.Child = sp;
        }

        // ------------------------------------------------------------------ actions

        bool EnsureClosed()
        {
            blockers = KeyOps.RunningBlockers();
            if (blockers.Count == 0) return true;
            Flash("Cierra " + string.Join(" y ", blockers) + " antes de continuar.", Theme.Amber);
            RenderCards(false);
            return false;
        }

        bool Confirm(string title, string message, string ok, bool danger)
        {
            bool unused;
            return Confirm(title, message, ok, danger, null, out unused);
        }

        bool Confirm(string title, string message, string ok, bool danger, string checkText, out bool isChecked)
        {
            dimmer.Visibility = Visibility.Visible;
            try { return ConfirmDialog.Ask(this, title, message, ok, danger, checkText, out isChecked); }
            finally { dimmer.Visibility = Visibility.Collapsed; }
        }

        /// <summary>Remembers a folder for the next dialog, unless it is inside Steam.</summary>
        void RememberFolder(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            if (steamPath != null)
            {
                string root = System.IO.Path.GetFullPath(steamPath).TrimEnd('\\') + "\\";
                if ((System.IO.Path.GetFullPath(dir).TrimEnd('\\') + "\\").StartsWith(root, StringComparison.OrdinalIgnoreCase)) return;
            }
            Settings.LastFolder = dir;
        }

        bool ExportWithDialog(SteamAccount a)
        {
            bool cancelled;
            return ExportWithDialog(a, out cancelled);
        }

        /// <summary>Asks where to save and exports the account's keys. Returns false if cancelled or failed.</summary>
        bool ExportWithDialog(SteamAccount a, out bool cancelled)
        {
            cancelled = false;
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exportar teclas de " + a.DisplayName,
                Filter = "Teclas de Dota 2 (*.zip)|*.zip",
                DefaultExt = ".zip",
                AddExtension = true,
                OverwritePrompt = true,
                // Full path so Windows opens the folder we remember, not its own last-visited one.
                FileName = System.IO.Path.Combine(Settings.LastFolder, KeyArchive.SuggestedFileName(a, DateTime.Now)),
                InitialDirectory = Settings.LastFolder,
            };
            if (dlg.ShowDialog(this) != true) { cancelled = true; return false; }
            try
            {
                KeyArchive.Export(a, dlg.FileName);
                RememberFolder(System.IO.Path.GetDirectoryName(dlg.FileName));
                Flash("Teclas de " + a.DisplayName + " exportadas a " + dlg.FileName, Theme.Green, 10);
                return true;
            }
            catch (Exception ex)
            {
                Flash("No se pudo exportar: " + ex.Message, Theme.Accent);
                return false;
            }
        }

        void DoExport(SteamAccount a)
        {
            ExportWithDialog(a);
        }

        void DoImport(SteamAccount a)
        {
            if (!EnsureClosed()) return;
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Importar teclas a " + a.DisplayName,
                Filter = "Teclas de Dota 2 (*.zip;*.lst)|*.zip;*.lst|Todos los archivos (*.*)|*.*",
                InitialDirectory = Settings.LastFolder,
                CheckFileExists = true,
            };
            if (dlg.ShowDialog(this) != true) return;
            RememberFolder(System.IO.Path.GetDirectoryName(dlg.FileName));

            KeyPackage pkg;
            try { pkg = KeyArchive.Load(dlg.FileName); }
            catch (Exception ex)
            {
                Flash("No se pudo leer el archivo: " + ex.Message, Theme.Accent);
                return;
            }

            string source = pkg.SourceName != null
                ? "Las teclas de " + pkg.SourceName + (string.IsNullOrEmpty(pkg.Date) ? "" : " (" + pkg.Date + ")")
                : "Las teclas del archivo";
            string msg = source + " reemplazarán por completo las de " + a.DisplayName + ".\n\nArchivo: " +
                         System.IO.Path.GetFileName(pkg.FilePath);
            bool exportFirst;
            if (!Confirm("Importar teclas", msg, "Importar y reemplazar", false,
                    a.HasDotaKeys ? "Exportar antes las teclas actuales de " + a.DisplayName : null, out exportFirst)) return;
            bool exportCancelled;
            if (exportFirst && !ExportWithDialog(a, out exportCancelled))
            {
                // On failure ExportWithDialog already shows the reason; only explain a plain cancel.
                if (exportCancelled) Flash("Importación cancelada: no se exportaron las teclas actuales.", Theme.Amber);
                return;
            }
            if (!EnsureClosed()) return; // Steam/Dota may have started meanwhile
            try
            {
                KeyArchive.Apply(pkg, a);
                Reload();
                Flash("Teclas importadas a " + a.DisplayName + ". Si Steam avisa de un conflicto de Steam Cloud, elige los archivos de este PC.",
                      Theme.Green, 15);
            }
            catch (Exception ex)
            {
                Flash("No se pudo importar: " + ex.Message, Theme.Accent);
            }
        }

        void DoCopy()
        {
            if (origin == null || target == null || !EnsureClosed()) return;
            var from = origin;
            var to = target;
            string msg = "Las teclas de " + from.DisplayName + " reemplazarán por completo las de " + to.DisplayName + ".";
            bool exportFirst;
            if (!Confirm("Copiar teclas", msg, "Copiar y reemplazar", false,
                    to.HasDotaKeys ? "Exportar antes las teclas actuales de " + to.DisplayName : null, out exportFirst)) return;
            bool exportCancelled;
            if (exportFirst && !ExportWithDialog(to, out exportCancelled))
            {
                if (exportCancelled) Flash("Copia cancelada: no se exportaron las teclas actuales.", Theme.Amber);
                return;
            }
            if (!EnsureClosed()) return; // Steam/Dota may have started while the dialog was open
            try
            {
                KeyOps.CopyKeys(from, to);
                target = null;
                Reload();
                Flash("Teclas copiadas a " + to.DisplayName + ". Si Steam avisa de un conflicto de Steam Cloud, elige los archivos de este PC.",
                      Theme.Green, 15);
            }
            catch (Exception ex)
            {
                Flash("No se pudo copiar: " + ex.Message, Theme.Accent);
            }
        }

        void DoDelete(SteamAccount a)
        {
            if (!EnsureClosed()) return;
            string msg = "Se eliminará toda la carpeta de " + a.DisplayName + " (ID " + a.AccountId + "): teclas, ajustes y datos locales " +
                         "de todos sus juegos y capturas.\n\n" + a.FolderPath + "\n\nEsto no se puede deshacer.";
            if (!Confirm("Eliminar cuenta olvidada", msg, "Eliminar para siempre", true)) return;
            if (!EnsureClosed()) return; // Steam/Dota may have started while the dialog was open
            try
            {
                KeyOps.DeleteAccountFolder(a);
                if (origin != null && origin.AccountId == a.AccountId) origin = null;
                if (target != null && target.AccountId == a.AccountId) target = null;
                Reload();
                Flash(a.DisplayName + " eliminada de este PC.", Theme.Green);
            }
            catch (Exception ex)
            {
                Reload();
                Flash("No se pudo eliminar: " + ex.Message, Theme.Accent);
            }
        }

        // ------------------------------------------------------------------ icon

        static ImageSource BuildIcon()
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var bg = new LinearGradientBrush(Theme.Hex("#F25A3F"), Theme.Hex("#A92A19"), 45);
                dc.DrawRoundedRectangle(bg, null, new Rect(0, 0, 64, 64), 16, 16);
                var ft = new FormattedText(Theme.IconKeyboard, System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, new Typeface(Theme.Icons, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    34, Brushes.White, 1.0);
                dc.DrawText(ft, new Point((64 - ft.Width) / 2, (64 - ft.Height) / 2));
            }
            var rtb = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }
    }
}
