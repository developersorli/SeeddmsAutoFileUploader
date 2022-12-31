using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
/**
 * @category   SeeddmsAutoFileUploader
 * @license    GPL 2
 * @author     Serge Sorli <sergej@sorli.org>
 * @copyright  Copyright (C) 2020-2023 Sorli.org,
 */
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
