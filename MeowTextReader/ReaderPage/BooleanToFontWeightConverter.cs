using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;

namespace MeowTextReader.ReaderPage
{
    /// <summary>true（目前章節）轉成粗體，其餘維持一般字重。</summary>
    public class BooleanToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isCurrent = value is bool b && b;
            return isCurrent ? FontWeights.SemiBold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
