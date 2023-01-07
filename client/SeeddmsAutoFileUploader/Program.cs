using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
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
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 1)
            {
                string url = args[0].Replace("sorli://", "");

                if (url.StartsWith("about"))
                {
                    MessageBox.Show("Application " + Application.ProductName + " via webbrowser works!" );
                }
                else
                {
                    if (url.StartsWith("https://"))
                    {
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        // Skip validation of SSL/TLS certificate
                        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1(url));
                    return 0;
                }
                
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AboutBox1());


            return 0;
        }
    }
}
