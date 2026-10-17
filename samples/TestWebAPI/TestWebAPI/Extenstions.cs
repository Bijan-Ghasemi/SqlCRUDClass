using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestWebAPI
{
    internal static class Extenstions
    {
        public static T ConvertTo<T>(this object toSerialize)
        {
            return toSerialize.ToJson().ToObject<T>();

        }
        public static string ToJson(this object toSerialize)
        {
            bool Inputobjetisjson = false;
            if (toSerialize is string)
            {
                if (IsValidJson(toSerialize.ToString()))
                {
                    Inputobjetisjson = true;
                }

            }
            if (Inputobjetisjson)
            {
                return toSerialize.ToString();
            }
            else
            {
                return JsonConvert.SerializeObject(toSerialize);
            }

        }
        private static T ToObject<T>(this string json)
        {
            if (json=="null")
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(json);
        }
        private static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
