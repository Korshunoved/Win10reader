using System;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
    class InterlineageSliderConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var val = (double) value;
            {
                if (val == 1)
                {
                    return 0.7f;
                }
                if (val == 2)
                {
                    return 0.85f;
                }
                if (val == 3)
                {
                    return 1.0f;
                }
                if (val == 4)
                {
                    return 1.15f;
                }
                if (val == 5)
                {
                    return 1.5f;
                }
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
