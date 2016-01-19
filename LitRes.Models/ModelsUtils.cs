using System.Linq;

using Autofac;
using System.Text;
using System.Collections.Generic;


namespace LitRes
{
    public class ModelsUtils 
	{
        public static string DictionaryToString(Dictionary<string, object> parameters)
        {
            var paramDictionaryStringBuilder = new StringBuilder();
            foreach (var item in parameters)
                paramDictionaryStringBuilder.AppendFormat("{0}:{1}|", item.Key, item.Value);
            var paramDictionaryString = paramDictionaryStringBuilder.ToString();
            if (paramDictionaryString.Length > 1) paramDictionaryString = paramDictionaryString.Substring(0, paramDictionaryString.Length - 1);

            return paramDictionaryString;
        }
	}
}
