using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1;
/**
 * @category   SeeddmsAutoFileUploader
 * @license    GPL 2
 * @author     Serge Sorli <sergej@sorli.org>
 * @copyright  Copyright (C) 2020-2023 Sorli.org,
 */
namespace SeeddmsAutoFileUploader
{
    class Class1
    {
        private static IProgressInterface pi;
        public static string Login(ref CookieContainer cookieContainer, string url, string userName, string password, ref string host_url, ref string documentId, ref string version)
        {
            
            String errorMessage = "";
            try
            {
                Regex rd = new Regex(".*/op/op.Download.php\\?documentid=(\\d+)&version=(\\d+)");
                Match matchd = rd.Match(url);
                documentId = matchd.Groups[1].Value;
                version = matchd.Groups[2].Value;

                host_url = url.Substring(0, url.IndexOf("/op/"));

                string param = "login=" + userName + "&pwd=" + password + "&lang=1";
                url = host_url + "/op/op.Login.php";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Referer = host_url + "/out/out.Login.php";
                request.ContentLength = param.Length;
                request.ContentType = "application/x-www-form-urlencoded";
                cookieContainer = new CookieContainer();
                request.CookieContainer = cookieContainer;

                using (Stream stream = request.GetRequestStream())
                {
                    byte[] paramAsBytes = Encoding.Default.GetBytes(param);
                    stream.Write(paramAsBytes, 0, paramAsBytes.Count());
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                String contentLogin = new StreamReader(response.GetResponseStream(), Encoding.Default).ReadToEnd();
                Regex rl = new Regex(".*<div class=\"alert alert-error\">\n?(.*?)</div>"); // "<div class=\"alert alert-error\">\nError signing in. User ID or password incorrect.</div>"
                errorMessage = rl.Match(contentLogin).Groups[1].Value;//<div class=\"alert alert-error\">\nError signing in. User ID or password incorrect.</div>
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (errorMessage != "")
            {
                return errorMessage.Replace("<h4>", "").Replace("</h4>", ": ");
            }
            return null;
        }

        public static string Download(CookieContainer cookieContainer, string host_url, string directoryPath, string documentId, string version, ref string fileNamePath, IProgressInterface pi2)
        {;
            pi = pi2;
            String errorMessage = "";
            try
            {
                string url = host_url + "/op/op.Download.php?documentid=" + documentId + "&version=" + version;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.CookieContainer = cookieContainer;
                WebResponse resp = req.GetResponse();
                string fileNameDownloadContent = resp.Headers.Get("Content-Disposition");//attachment; filename=\"VirtualBoxGuestDebian2.txt\"; filename*=UTF-8''VirtualBoxGuestDebian2.txt"
                if (fileNameDownloadContent == null)
                {
                    return "File content for this version does not exist!";
                }
                Regex rf = new Regex(".*filename=\"(.*?)\";");
                String fileNameDownload = rf.Match(fileNameDownloadContent).Groups[1].Value;
                fileNamePath = directoryPath + fileNameDownload;
                //Pass the file path and file name to the StreamReader constructor

               
                
                //byte[] data = GetContentWithProgressReporting(resp.GetResponseStream(), resp.ContentLength);
                
                
                if (File.Exists(fileNamePath))
                {
                    DialogResult dialogResult = MessageBox.Show( "File already exist on the disk. Override?", "Exist", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        try
                        {
                            var fileStream = new FileStream(fileNamePath, FileMode.Create, FileAccess.Write);
                            CopyFrom(fileStream, resp.GetResponseStream(), resp.ContentLength);
                        }
                        catch (Exception ex)
                        {
                            FileAttributes attributes = File.GetAttributes(fileNamePath);
                            
                            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            {
                                DialogResult dialogResult1 = MessageBox.Show("File " + fileNamePath + " on the disk is read only. Override anyway?", "Exist", MessageBoxButtons.YesNo);
                                if (dialogResult1 == DialogResult.Yes)
                                {
                                    attributes = attributes & ~FileAttributes.ReadOnly;
                                    File.SetAttributes(fileNamePath, attributes);
                                    var fileStream = new FileStream(fileNamePath, FileMode.Create, FileAccess.Write);
                                    CopyFrom(fileStream, resp.GetResponseStream(), resp.ContentLength);
                                }
                                else if (dialogResult1 == DialogResult.No)
                                {
                                    errorMessage = "Application will exit!";
                                    return errorMessage;
                                }
                            }

                        }
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                   
                    }
                }
                else
                {
                    var fileStream = new FileStream(fileNamePath, FileMode.Create, FileAccess.Write);
                    CopyFrom(fileStream, resp.GetResponseStream(), resp.ContentLength, 4096);
                }

                //using (BinaryReader inStream = new BinaryReader(resp.GetResponseStream(), Encoding.Default))
                /*using (FileStream outStream = new FileStream(fileNamePath, FileMode.Create, FileAccess.Write))
                {
                    inStream.BaseStream.CopyTo(outStream);
                }*/
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            if (errorMessage != "")
            {
                return errorMessage;
            }
            return null;
        }
        public static string Upload(CookieContainer cookieContainer, string uploadFilePath, string host_url, string documentId, IProgressInterface pi2)
        {
            pi = pi2;
            
            String errorMessage = "";
            try
            {

                String url = host_url + "/out/out.UpdateDocument.php?documentid=" + documentId;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.CookieContainer = cookieContainer;
                WebResponse response = req.GetResponse();

                String content = new StreamReader(response.GetResponseStream(), Encoding.Default).ReadToEnd();
                //Regex r = new Regex(".*formtoken=\"(.*?)\">DMS</a>");
                Regex r = new Regex(".*name=\"formtoken\" value=\"(.*?)\"");
                String formtoken = r.Match(content).Groups[1].Value;

                // Read file data
                string uploadFileName = Path.GetFileName(uploadFilePath);
                string extension = Path.GetExtension(uploadFilePath);
                FileStream fs = new FileStream(uploadFilePath, FileMode.Open, FileAccess.Read);
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
                fs.Close();

                // Generate post objects
                Dictionary<string, object> postParameters = new Dictionary<string, object>();
                postParameters.Add("formtoken", formtoken);
                postParameters.Add("documentid", documentId);
                postParameters.Add("userfile", new FileParameter(data, uploadFileName, MimeTypeMap.GetMimeType(extension)));
                postParameters.Add("comment", "");
                postParameters.Add("presetexpdate", "never");
                postParameters.Add("expdate", "");
                postParameters.Add("workflow", "");

                // Create request and receive response
                string postURL = host_url + "/op/op.UpdateDocument.php";
                string userAgent = "file";
                HttpWebResponse webResponse = MultipartFormDataPost(cookieContainer, postURL, userAgent, postParameters);

                // Process response
                StreamReader responseReader = new StreamReader(webResponse.GetResponseStream());
                string uploadResponseContent = responseReader.ReadToEnd();
                webResponse.Close();

                Regex ru = new Regex(".*<div class=\"alert alert-error\">\n?(.*?)</div>"); // "<div class=\"alert alert-error\"><h4>Error!</h4>New version is identical to current version.</div>"
                errorMessage = ru.Match(uploadResponseContent).Groups[1].Value;
                if (errorMessage != "")
                {
                    return errorMessage.Replace("<h4>", "").Replace("</h4>", ": ");
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            if (errorMessage != "")
            {
                return errorMessage;
            }
            return null;
        }
        private static readonly Encoding encoding = Encoding.UTF8;
        private static HttpWebResponse MultipartFormDataPost(CookieContainer cookieContainer, string postUrl, string userAgent, Dictionary<string, object> postParameters)
        {
            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

            return PostForm(cookieContainer, postUrl, userAgent, contentType, formData);
        }
        private static HttpWebResponse PostForm(CookieContainer cookieContainer, string postUrl, string userAgent, string contentType, byte[] formData)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

            if (request == null)
            {
                throw new NullReferenceException("request is not a http request");
            }

            // Set up the request properties.
            request.Method = "POST";
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.CookieContainer = cookieContainer;
            request.ContentLength = formData.Length;

            // You could add authentication here as well if needed:
            // request.PreAuthenticate = true;
            // request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
            // request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("username" + ":" + "password")));

            // Send the form data to the request.
            using (Stream requestStream = request.GetRequestStream())
            {

                MemoryStream stream = new MemoryStream(formData);
                CopyFrom(requestStream, stream, formData.Length);
                //requestStream.Write(formData, 0, formData.Length);
                //requestStream.Close();
            }

            return request.GetResponse() as HttpWebResponse;
        }

        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Flush();
            formDataStream.Close();

            return formData;
        }

        private class FileParameter
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }


        public static long CopyFrom(Stream stream, Stream source,long contentLength, int bufferSize = 4096)
        {
            
            pi.UpdateProgressBar(0);
            int count = 0;
            byte[] buffer = new byte[bufferSize];
            long length = 0;

            while ((count = source.Read(buffer, 0, bufferSize)) != 0)
            {
                length += count;
                stream.Write(buffer, 0, count);
                double percentage = (double)length / contentLength;
                pi.UpdateProgressBar((int)(percentage * 100));
            }
            pi.UpdateProgressBar(100);
            stream.Flush();
            stream.Close();
            return length;
        }

        public interface IProgressInterface
        {
            void UpdateProgressBar(int percentage);
        }
    }
}
