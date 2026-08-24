using System;

namespace ZARI.Application.Helpers;
    public class Base64Converter
    {
        public string StringToBase64(string text)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(plainTextBytes);
        }

        public string Base64ToString(string base64Data)
        {
            byte[] base64EncodedBytes = System.Convert.FromBase64String(base64Data);

            string result = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

            return result;
        }

    }

