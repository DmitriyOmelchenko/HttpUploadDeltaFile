using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Net;
using System.Collections.Specialized;

namespace ApptimizedTask
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }
        //creating delta file
        private void CreateAndUploadDeltaFile(FileInfo BaseFile, string url, NameValueCollection nvc)
        {
            var fileInfo = BaseFile.Name.Split('.');
            var paramName = fileInfo[0];
            var contentType=fileInfo[1];
            int deltaFileNumber = 0;
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webRequest.Method = "POST";
            webRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;
            webRequest.KeepAlive = true;

            Stream sendStream = webRequest.GetRequestStream();
            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";

            foreach (string key in nvc.Keys)
            {
                sendStream.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                sendStream.Write(formitembytes, 0, formitembytes.Length);
            }
            sendStream.Write(boundarybytes, 0, boundarybytes.Length);
            // Create the file.
            using (BinaryReader binaryReader = new BinaryReader(BaseFile.OpenRead()))
            {

                int bytesRead;
                byte[] buffer = new byte[50000000];
                //
              
                while ((bytesRead = binaryReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    deltaFileNumber++;

                    // Add some information to the file.
                    string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                    string header = string.Format(headerTemplate, paramName, BaseFile.Name+deltaFileNumber, contentType);
                    byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                    //write header
                    sendStream.Write(headerbytes, 0, headerbytes.Length);
                    //write data
                    sendStream.Write(buffer, 0, bytesRead);
                    //write trailer
                    byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                    sendStream.Write(trailer, 0, trailer.Length);
                }
            }
            //e
            
            sendStream.Close();
            WebResponse wresp = null;
            try
            {
                wresp = webRequest.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                MessageBox.Show(string.Format("File uploaded, server response is: {0}", reader2.ReadToEnd()));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error uploading file"+ex.Message);
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                webRequest = null;
            }
        }
       
        private async void OpendButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //create directory for delta file
             if (!Directory.Exists(Environment.CurrentDirectory + @"\Test"))
             Directory.CreateDirectory(Environment.CurrentDirectory + @"\Test");
            if (openFileDialog.ShowDialog() == true)
            {
               FileInfo BaseFile = new FileInfo(openFileDialog.FileName);
                // 
                if (BaseFile.Length==0)
                {
                    MessageBox.Show("File is Empty");
                    return;
                }
                NameValueCollection nvc = new NameValueCollection();
                nvc.Add("user", "user");
                nvc.Add("passwd", "passwd");
                await Dispatcher.InvokeAsync(() => CreateAndUploadDeltaFile(BaseFile, "http://", nvc), DispatcherPriority.Background);
            }
        }
    }
}
