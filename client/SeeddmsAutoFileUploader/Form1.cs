using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
    public partial class Form1 : Form, ILoginInterface
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowText(IntPtr hWnd, string text);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private static FileSystemWatcher watcher = null;
        public string url = "";
        public static string host_url = "";
        public static string documentId = "";
        public static string fileHash = "";
        public bool isFileChange = false;
        public string version = "";
        public string username = "";
        public string password = "";

        private static CookieContainer cookieContainer = null;
        private static string fileNamePath = "";


        private static ProcessStartInfo pi;
        private static Process process = null;
        private static IntPtr hWnd = IntPtr.Zero;
        private static bool disableUpload = false;

        private TripleDESStringEncryptor te = null;
        private static ProgressBar progressBar;
        private static bool canExit = true;

        private long processStartTime = -1;
        private bool processNotWork = false;
        public Form1(string url)
        {
            this.url = url;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            te = new TripleDESStringEncryptor();
            //progressBar = progressBar1;

            Point point = new Point();
            point.X = 0;
            point.Y = 0;
            this.Location = point;


            string us = ""; string ps = "";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\SeeddmsAutoFileUploader");
            if (key != null)
            {
                us = (string)key.GetValue("us");
                ps = (string)key.GetValue("ps");

            }


            if (us == null || ps == null || us.Equals("") || ps.Equals(""))
            {
                FormLogin formLogin = new FormLogin(ref cookieContainer, url, username, password, this);
                formLogin.ShowDialog(this);
                return;
            }

            username = te.DecryptString(us);
            password = te.DecryptString(ps);

            string message = Class1.Login(ref cookieContainer, url, username, password, ref host_url, ref documentId, ref version);
            if (message != null)
            {
                MessageBox.Show(message);
                FormLogin formLogin = new FormLogin(ref cookieContainer, url, username, password, this);
                formLogin.ShowDialog(this);
                return;
            }
            loginEvent(cookieContainer, username, password, host_url, documentId, version);
        }

        // Handle Exited event and display process information.
        private void process_Exited(object sender, System.EventArgs e)
        {

            long timeprocesselapse = DateTime.Now.Ticks - processStartTime;
            if (timeprocesselapse != -1 && timeprocesselapse < 3000000)
            {
                processNotWork = true;
                return;
            }


            if (isFileChange)
            {
                FileChange();
            }


            int i = 0;
            Thread.Sleep(1000);
            while (!Form1.canExit)
            {
                i++;
                if (i> 1200)
                {
                    break;
                }
                Thread.Sleep(250);
            }

            if (i>1200)
            {
                DialogResult dialogResul = MessageBox.Show("Application is waiting to long to transfer file. Application will exit now!");
            }

            Application.Exit();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void Run()
        {


            // Create a new FileSystemWatcher and set its properties.
            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(fileNamePath);

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName;

            // Only watch text files.
            watcher.Filter = Path.GetFileName(fileNamePath);

            // Add event handlers.
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            isFileChange = true;
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            isFileChange = true;
            if (processNotWork)
            {
                FileChange();
            }

            if (watcher != null)
            {
                watcher.Dispose();
                watcher = null;
            }
            Run();
        }

        private void FileChange()
        {
            // Specify what is done when a file is renamed.
            //Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
            if (!disableUpload)
            {
                canExit = false;
                Thread.Sleep(250);
                byte[] fileByte = null;

                // Loop allow multiple attempts
                while (true)
                {
                    try
                    {
                        fileByte = System.IO.File.ReadAllBytes(fileNamePath);

                        //If we get here, the File.Open succeeded, so break out of the loop and return the FileStream
                        break;
                    }
                    catch (IOException ioEx)
                    {
                        // IOExcception is thrown if the file is in use by another process.

                        // Check the numbere of attempts to ensure no infinite loop

                        // Too many attempts,cannot Open File, break and return null 
                        DialogResult dialogResult = MessageBox.Show("Unable to upload file, because file " + Path.GetFileName(fileNamePath) + " is locked. Please close application which is lock the file and click retray!", "File locked by appliction", MessageBoxButtons.RetryCancel);
                        if (dialogResult == DialogResult.Cancel)
                        {
                            return;
                        }
                    }

                }



                string fileHashNew = Util.ComputeSha256Hash(fileByte);

                if (fileHash.Equals(fileHashNew))
                {
                    File.Delete(fileNamePath);
                    return;
                }

                if (processNotWork)
                {
                    DialogResult dialogResult = MessageBox.Show("File has changed. Upload it and exit?", "Upload and exit", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        string message1 = Class1.Upload(cookieContainer, fileNamePath, host_url, documentId, new Progress(progressBar1));
                        if (message1 != null)
                        {
                            MessageBox.Show(message1);
                            return;
                        }

                        File.Delete(fileNamePath);
                        fileHash = fileHashNew;
                        canExit = true;
                        Application.Exit();
                        return;
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        //do something else
                        return;
                    }
                }
                string message = Class1.Upload(cookieContainer, fileNamePath, host_url, documentId, new Progress(progressBar1));
                if (message != null)
                {
                    MessageBox.Show(message);
                    return;
                }

                File.Delete(fileNamePath);
                fileHash = fileHashNew;
                canExit = true;                
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*if (process != null)
            {
                string content = "";
                int length = GetWindowTextLength(hWnd);

                if (length != 0)
                {
                    StringBuilder builder = new StringBuilder(length);
                    GetWindowText(hWnd, builder, length + 1);
                    content = builder.ToString();
                }

                content = content.Replace("(for upload to Seeddms)", "");
                SetWindowText(process.MainWindowHandle, content);
            }*/
            disableUpload = true;

            if (!Form1.canExit)
            {
                DialogResult dialogResult = MessageBox.Show("Operation is stil in progress. Are you sure?", "Progress", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    Application.Exit();
                }
                else if (dialogResult == DialogResult.No)
                {
                    //do something else
                }
            } else
            {

                if (File.Exists(fileNamePath))
                {
                    try
                    {
                        File.Delete(fileNamePath);
                    } catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                Application.Exit();
            }

            
        }

        private void buttonUser_Click(object sender, EventArgs e)
        {
            FormLogin formLogin = new FormLogin(ref cookieContainer, url, username, password, this);
            formLogin.ShowDialog(this);

        }

        public void loginEvent(CookieContainer cookieContainer, string username, string password, string host_url, string documentId, string version)
        {
            canExit = false;
            Form1.cookieContainer = cookieContainer;
            this.username = username;
            this.password = password;
            Form1.host_url = host_url;
            Form1.documentId = documentId;
            this.version = version;

            if (pi == null)
            {
                if (watcher != null)
                {
                    watcher.Dispose();
                    watcher = null;
                }
                disableUpload = true;
                string directoryPath = System.IO.Path.GetTempPath();
                string errorMessage = Class1.Download(cookieContainer, host_url, directoryPath, documentId, version, ref fileNamePath, new Progress(progressBar1));
                if (errorMessage != null)
                {
                    MessageBox.Show(errorMessage);
                    Application.Exit();
                    return;
                }
                byte[] fileByte = null;
                try
                {
                    fileByte = System.IO.File.ReadAllBytes(fileNamePath);
                } catch (Exception ex)
                {
                    MessageBox.Show("File " + Path.GetFileName(fileNamePath) + " is locketd. Please close application which is open the file! Application will exit now!", "File locked");
                    Application.Exit();
                    return;
                }
                fileHash = Util.ComputeSha256Hash(fileByte);

                string fileName = Path.GetFileName(fileNamePath);
                label1.Text = "File: " + fileName;
                //this.Text = fileName;
                pi = new ProcessStartInfo(fileNamePath);
                pi.Arguments = Path.GetFileName(fileNamePath);
                pi.UseShellExecute = true;
                pi.WorkingDirectory = Path.GetDirectoryName(fileNamePath);
                pi.FileName = fileNamePath;
                pi.Verb = "OPEN";
                ProcessStartInfo MyStartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true
                };

                process = new Process { StartInfo = pi };
                process.EnableRaisingEvents = true;
                process.Exited += new EventHandler(process_Exited);


                process.Start();


                /*hWnd = process.MainWindowHandle;
                string content = "";

                do
                {

                    Thread.Sleep(100);
                    hWnd = process.MainWindowHandle;
                    if (process.HasExited)
                        return;
                } while (hWnd == IntPtr.Zero);
                
                RECT rct = new RECT();
                do
                {

                    Thread.Sleep(100);
                    GetWindowRect(hWnd, ref rct);
                    if (process.HasExited)
                        return;
                } while (rct.Right == 0 && rct.Bottom == 0);

                int length = GetWindowTextLength(hWnd);

                if (length != 0)
                {
                    StringBuilder builder = new StringBuilder(length);
                    GetWindowText(hWnd, builder, length + 1);
                    content = builder.ToString();
                }

                content = content + "(for upload to Seeddms)";
                SetWindowText(process.MainWindowHandle, content);
                */
                Run();
                disableUpload = false;
                canExit = true;
                processStartTime = DateTime.Now.Ticks;
            }
        }

        public void saveCredetials(string username, string password)
        {
            //accessing the CurrentUser root element  
            //and adding "OurSettings" subkey to the "SOFTWARE" subkey  
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\SeeddmsAutoFileUploader");

            //storing the values  
            key.SetValue("us", te.EncryptString(username));
            key.SetValue("ps", te.EncryptString(password));
            key.Close();
        }

        public delegate void InvokeDelegate();
        private class Progress : Class1.IProgressInterface
        {
            private ProgressBar progressBar = null;
            public Progress(ProgressBar progressBar2)
            {
                progressBar = progressBar2;
            }
            private int percentage = 0;
            public void UpdateProgressBar(int percentage2)
            {
                percentage = percentage2;
                progressBar.BeginInvoke(new InvokeDelegate(InvokeMethod));
            }

            public void InvokeMethod()
            {
                progressBar.Value = percentage;
            }

        }


    }

}
