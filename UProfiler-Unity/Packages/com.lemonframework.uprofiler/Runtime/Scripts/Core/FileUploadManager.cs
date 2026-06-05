using System;
using System.IO;
using System.Net;

namespace LemonFramework.UProfiler.Core
{
    public static class FileFtpUploadManager
    {
        private static string FtpHost => $"ftp://{Config.IP}:2121/";

        public static void UploadFile(string filePath,
            Action<object, UploadProgressChangedEventArgs> onFileUploadProgressChanged,
            Action<object, UploadFileCompletedEventArgs> onFileUploadCompleted)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            using var client = new WebClient();
            var uri = new Uri(FtpHost + new FileInfo(filePath).Name);
            client.UploadProgressChanged += new UploadProgressChangedEventHandler(onFileUploadProgressChanged);
            client.UploadFileCompleted += new UploadFileCompletedEventHandler(onFileUploadCompleted);
            client.UploadFileAsync(uri, "STOR", filePath);
        }
    }
}