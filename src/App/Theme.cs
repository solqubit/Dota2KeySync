using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace Dota2KeySync.App
{
    public static class Theme
    {
        public static readonly Color Bg        = Hex("#121419");
        public static readonly Color Surface   = Hex("#1B1E26");
        public static readonly Color Surface2  = Hex("#232733");
        public static readonly Color Line      = Hex("#2A2F3B");
        public static readonly Color Text      = Hex("#ECEEF3");
        public static readonly Color Muted     = Hex("#8B91A1");
        public static readonly Color Accent    = Hex("#E5452F");
        public static readonly Color AccentDark= Hex("#B8321F");
        public static readonly Color Teal      = Hex("#33B6C8");
        public static readonly Color Green     = Hex("#35C27A");
        public static readonly Color Amber     = Hex("#F0A43A");
        public static readonly Color Blue      = Hex("#6C8CFF");

        public static readonly FontFamily Font  = new FontFamily("Segoe UI Variable Display, Segoe UI");
        public static readonly FontFamily Icons = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

        public const string IconRefresh  = "";
        public const string IconMinimize = "";
        public const string IconClose    = "";
        public const string IconDelete   = "";
        public const string IconKeyboard = "";
        public const string IconArrow    = "";
        public const string IconCheck    = "";
        public const string IconWarning  = "";

        public static Color Hex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        public static SolidColorBrush Brush(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public static SolidColorBrush Brush(Color c, double opacity)
        {
            var b = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), c.R, c.G, c.B));
            b.Freeze();
            return b;
        }

        public static ResourceDictionary Load()
        {
            return (ResourceDictionary)XamlReader.Parse(Xaml);
        }

        const string Xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

  <Style x:Key='Btn' TargetType='Button'>
    <Setter Property='Foreground' Value='#ECEEF3'/>
    <Setter Property='Background' Value='#262A35'/>
    <Setter Property='BorderBrush' Value='Transparent'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='Padding' Value='14,8'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FontWeight' Value='SemiBold'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='SnapsToDevicePixels' Value='True'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Button'>
          <Grid x:Name='root'>
            <Border x:Name='bd' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}'
                    BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='9'/>
            <Border x:Name='hover' Background='#FFFFFF' Opacity='0' CornerRadius='9'/>
            <ContentPresenter Margin='{TemplateBinding Padding}' HorizontalAlignment='Center' VerticalAlignment='Center'
                              TextElement.Foreground='{TemplateBinding Foreground}'/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='hover' Property='Opacity' Value='0.07'/></Trigger>
            <Trigger Property='IsPressed' Value='True'><Setter TargetName='hover' Property='Opacity' Value='0.14'/></Trigger>
            <Trigger Property='IsEnabled' Value='False'><Setter TargetName='root' Property='Opacity' Value='0.35'/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='PrimaryBtn' TargetType='Button' BasedOn='{StaticResource Btn}'>
    <Setter Property='Foreground' Value='White'/>
    <Setter Property='Padding' Value='22,11'/>
    <Setter Property='FontSize' Value='14'/>
    <Setter Property='Background'>
      <Setter.Value>
        <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
          <GradientStop Color='#F25A3F' Offset='0'/>
          <GradientStop Color='#C4321E' Offset='1'/>
        </LinearGradientBrush>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='GhostBtn' TargetType='Button' BasedOn='{StaticResource Btn}'>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='BorderBrush' Value='#323745'/>
  </Style>

  <Style x:Key='TitleBtn' TargetType='Button'>
    <Setter Property='Foreground' Value='#8B91A1'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Width' Value='40'/>
    <Setter Property='Height' Value='34'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Button'>
          <Border x:Name='bd' Background='{TemplateBinding Background}' CornerRadius='8'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'
                              TextElement.Foreground='{TemplateBinding Foreground}'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='bd' Property='Background' Value='#262A35'/>
              <Setter Property='Foreground' Value='#ECEEF3'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='CloseBtn' TargetType='Button' BasedOn='{StaticResource TitleBtn}'>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Button'>
          <Border x:Name='bd' Background='{TemplateBinding Background}' CornerRadius='8'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'
                              TextElement.Foreground='{TemplateBinding Foreground}'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='bd' Property='Background' Value='#E5452F'/>
              <Setter Property='Foreground' Value='White'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='ToolTip'>
    <Setter Property='Background' Value='#2A2F3B'/>
    <Setter Property='Foreground' Value='#ECEEF3'/>
    <Setter Property='BorderBrush' Value='#3A4050'/>
    <Setter Property='Padding' Value='10,6'/>
    <Setter Property='FontSize' Value='12'/>
  </Style>

  <Style TargetType='ScrollBar'>
    <Setter Property='Width' Value='8'/>
    <Setter Property='MinWidth' Value='8'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ScrollBar'>
          <Track x:Name='PART_Track' IsDirectionReversed='True'>
            <Track.Thumb>
              <Thumb>
                <Thumb.Template>
                  <ControlTemplate TargetType='Thumb'>
                    <Border Background='#353A48' CornerRadius='4' Margin='1,2'/>
                  </ControlTemplate>
                </Thumb.Template>
              </Thumb>
            </Track.Thumb>
          </Track>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
</ResourceDictionary>";
    }
}
