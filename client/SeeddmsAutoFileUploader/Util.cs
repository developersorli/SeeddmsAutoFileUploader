using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
/**
 * @category   SeeddmsAutoFileUploader
 * @license    GPL 2
 * @author     Serge Sorli <sergej@sorli.org>
 * @copyright  Copyright (C) 2020-2023 Sorli.org,
 */
namespace SeeddmsAutoFileUploader
{
    class Util
    {
        public static string ComputeSha256Hash(byte[] byteData)
        {
            if (byteData == null)
            {
                return null;
            }
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(byteData);

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
