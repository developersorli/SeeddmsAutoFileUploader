using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
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
    public partial class FormLogin : Form
    {
        private static string url = "";
        private static CookieContainer cookieContainer = null;
        private ILoginInterface loginInterface;

        public FormLogin(ref CookieContainer cookieContainer, string url, string userName, string password, ILoginInterface loginInterface1)
        {
            FormLogin.cookieContainer = cookieContainer;
            FormLogin.url = url;
            InitializeComponent();

            textBoxUsername.Text = userName;
            textBoxPassword.Text = password;
            loginInterface = loginInterface1;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            string host_url = "";
            string documentId = ""; 
            string version = "";
            string message = Class1.Login(ref cookieContainer, url, textBoxUsername.Text, textBoxPassword.Text, ref host_url, ref documentId, ref version);
            if (message != null)
            {
                MessageBox.Show(message);
                return;
            }
            loginInterface.saveCredetials(textBoxUsername.Text, textBoxPassword.Text);
            Close();
            loginInterface.loginEvent(cookieContainer, labelUsername.Text, textBoxPassword.Text, host_url, documentId, version);
        }
    }

    public interface ILoginInterface
    {
        void saveCredetials(string username, string password);
        void loginEvent(CookieContainer cookieContainer, string username, string password, string host_url, string documentId, string version);
    }
}
