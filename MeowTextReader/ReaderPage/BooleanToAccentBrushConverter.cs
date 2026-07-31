using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MeowTextReader.ReaderPage
{
    /// <summary>
    /// true（目前章節）轉成主題強調色，其餘維持預設文字色，讓目前章節在清單中一眼可辨。
    /// AccentBrush/DefaultBrush 要在 XAML 用 ThemeResource 指定（見 ChapterChooseDialog.xaml），
    /// 靠 DependencyProperty 才能隨著 ActualTheme 即時換色；
    /// 改在 C# 用 Application.Current.Resources[key] 查表是靜態查詢，不會跟著切換主題，會查到錯的顏色。
    /// </summary>
    public class BooleanToAccentBrushConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty AccentBrushProperty =
            DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(BooleanToAccentBrushConverter), new PropertyMetadata(null));

        public Brush AccentBrush
        {
            get => (Brush)GetValue(AccentBrushProperty);
            set => SetValue(AccentBrushProperty, value);
        }

        public static readonly DependencyProperty DefaultBrushProperty =
            DependencyProperty.Register(nameof(DefaultBrush), typeof(Brush), typeof(BooleanToAccentBrushConverter), new PropertyMetadata(null));

        public Brush DefaultBrush
        {
            get => (Brush)GetValue(DefaultBrushProperty);
            set => SetValue(DefaultBrushProperty, value);
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isCurrent = value is bool b && b;
            return isCurrent ? AccentBrush : DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
