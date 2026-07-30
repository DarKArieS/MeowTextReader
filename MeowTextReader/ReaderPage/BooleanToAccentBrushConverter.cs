using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MeowTextReader.ReaderPage
{
    /// <summary>true（目前章節）轉成主題強調色，其餘維持預設文字色，讓目前章節在清單中一眼可辨。</summary>
    public class BooleanToAccentBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isCurrent = value is bool b && b;
            string key = isCurrent ? "AccentTextFillColorPrimaryBrush" : "TextFillColorPrimaryBrush";
            return Application.Current.Resources[key];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
