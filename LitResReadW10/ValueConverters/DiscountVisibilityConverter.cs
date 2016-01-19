using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
    public class DiscountVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool visible = false;
            string type = "(null)";
            string annotation = string.Empty;

            if (value != null)
            {
                Type valueType = value.GetType();

                type = valueType.Name;

                if (valueType == typeof (Models.Book))
                {
                    Models.Book book = value as Models.Book;
                    visible = book.Price < book.BasePrice && !book.IsMyBook;
                }
            }


            if (parameter is string)
            {
                annotation = (string)parameter;

                if (annotation == "inverse")
                    visible = !visible;
            }

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
