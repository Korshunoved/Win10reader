using System;
using System.Reflection;
using Windows.UI.Xaml.Data;

namespace LitRes.ValueConverters
{
    public class DeclensionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "";
            Type valueType = value.GetType();
            if (!valueType.GetTypeInfo().IsValueType) return "";
            if (valueType != typeof (int)) return "";
            var s = (parameter as string)?.Split('-')[0];
            var s1 = (parameter as string)?.Split('-')[1];
            var v = (int) value;
            if (s == null || s1 == null) return "";
            switch (s)
            {
                case "book":
                {
                    if (v == 0)
                        return "книг";
                    var lastTwoDigit = v%100;
                    var lastDigit = v%10;
                    string bookText;
                    switch (lastDigit)
                    {
                        case 1:
                        {
                            bookText = "книга";
                            break;
                        }
                        case 2:
                        {
                            bookText = lastTwoDigit != 12 ? "книги" : "книг";
                            break;
                        }
                        case 3:
                        {
                            bookText = lastTwoDigit != 12 ? "книги" : "книг";
                            break;
                        }
                        case 4:
                        {
                            bookText = lastTwoDigit != 12 ? "книги" : "книг";
                            break;
                        }
                        default:
                        {
                            bookText = "книг";
                            break;
                        }
                    }
                    return s1.Equals("u") ? bookText.ToUpper() : bookText;
                }
                case "author":
                {
                    if (v == 0)
                        return "автор";
                    var lastTwoDigit = v%100;
                    var lastDigit = v%10;
                    string authorText;
                    switch (lastDigit)
                    {
                        case 1:
                        {
                            authorText = "автор";
                            break;
                        }
                        case 2:
                        {
                            authorText = lastTwoDigit != 12 ? "автора" : "авторов";
                            break;
                        }
                        case 3:
                        {
                            authorText = lastTwoDigit != 12 ? "автора" : "авторов";
                            break;
                        }
                        case 4:
                        {
                            authorText = lastTwoDigit != 12 ? "автора" : "авторов";
                            break;
                        }
                        default:
                        {
                            authorText = "авторов";
                            break;
                        }
                    }
                    return s1.Equals("u") ? authorText.ToUpper() : authorText;
                }
                case "series":
                {
                    if (v == 0)
                        return "серия";
                    var lastTwoDigit = v % 100;
                    var lastDigit = v % 10;
                    string seriesText;
                    switch (lastDigit)
                    {
                        case 1:
                        {
                            seriesText = "серия";
                            break;
                        }
                        case 2:
                        {
                            seriesText = lastTwoDigit != 12 ? "серии" : "серий";
                            break;
                        }
                        case 3:
                        {
                            seriesText = lastTwoDigit != 12 ? "серии" : "серий";
                            break;
                        }
                        case 4:
                        {
                            seriesText = lastTwoDigit != 12 ? "серии" : "серий";
                            break;
                        }
                        default:
                        {
                            seriesText = "серий";
                            break;
                        }
                    }
                    return s1.Equals("u") ? seriesText.ToUpper() : seriesText;
                }
            }
            return "";
        }


        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
