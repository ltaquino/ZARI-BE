using System;
using System.Collections.Generic;

namespace ZARI.Application.Helpers;

public class HexConverter
    {
        public string StringToHex(string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);

            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public string HexToString(string hex)
        {
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex string must have an even length");
            }
            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return System.Text.Encoding.UTF8.GetString(bytes);
        }

    }

