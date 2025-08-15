using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;

namespace MeowTextReader
{
    public class FolderBoldConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            if (value is bool isFolder && isFolder)
                return FontWeights.Bold;
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new System.NotImplementedException();
        }
    }
}
