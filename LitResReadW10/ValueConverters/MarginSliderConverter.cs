using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
    class MarginSliderConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var val = (double)value;
            {
                if (val == 1)
                {
                    return "3%";
                }
                if (val == 2)
                {
                    return "6%";
                }
                if (val == 3)
                {
                    return "9%";
                }
                if (val == 4)
                {
                    return "12%";
                }
                if (val == 5)
                {
                    return "15%";
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
