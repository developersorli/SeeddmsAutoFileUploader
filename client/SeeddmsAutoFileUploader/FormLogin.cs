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
