using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
//SeeddmsAutoFileUploader
//Copyright(C) 2022-2023  Sergej Sorli, developer.sorli@gmail.com

//    This program is free software; you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation; either version 2 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License along
//    with this program; if not, write to the Free Software Foundation, Inc.,
//    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
namespace SeeddmsAutoFileUploader
{
    public class TripleDESStringEncryptor : IStringEncryptor
    {
        private byte[] _key;
        private byte[] _iv;
        private TripleDESCryptoServiceProvider _provider;

        public TripleDESStringEncryptor()
        {
            _key = System.Text.ASCIIEncoding.ASCII.GetBytes("FJOTDWBLPLJFDFBNMJAOISFE");
            _iv = System.Text.ASCIIEncoding.ASCII.GetBytes("DSASAKCAK");
            _provider = new TripleDESCryptoServiceProvider();
        }

        #region IStringEncryptor Members

        public string EncryptString(string plainText)
        {
            return Transform(plainText, _provider.CreateEncryptor(_key, _iv));
        }

        public string DecryptString(string encryptedText)
        {
            return Transform(encryptedText, _provider.CreateDecryptor(_key, _iv));
        }

        #endregion

        private string Transform(string text, ICryptoTransform transform)
        {
            if (text == null)
            {
                return null;
            }
            using (MemoryStream stream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Write))
                {
                    byte[] input = Encoding.Default.GetBytes(text);
                    cryptoStream.Write(input, 0, input.Length);
                    cryptoStream.FlushFinalBlock();

                    return Encoding.Default.GetString(stream.ToArray());
                }
            }
        }
    }

    public interface IStringEncryptor
    {
        string EncryptString(string plainText);
        string DecryptString(string encryptedText);
    }
}
