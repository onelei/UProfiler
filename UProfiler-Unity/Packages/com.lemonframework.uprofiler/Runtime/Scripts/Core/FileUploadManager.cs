using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace LemonFramework.UProfiler.Core
{
    public class FileFTPUploadManager
    {
        //备注:远程服务器需要端口映�?
        public static string FTPHost => $"ftp://{Config.IP}:2121/";
        public static void UploadFile(string filePath, Action<object, UploadProgressChangedEventArgs> OnFileUploadProgressChanged, Action<object, UploadFileCompletedEventArgs> OnFileUploadCompleted)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            using (var client = new WebClient())
            {
                var uri = new Uri(FTPHost + new FileInfo(filePath).Name);
                client.UploadProgressChanged += new UploadProgressChangedEventHandler(OnFileUploadProgressChanged);
                client.UploadFileCompleted += new UploadFileCompletedEventHandler(OnFileUploadCompleted);
                //client.Credentials = new System.Net.NetworkCredential(FTPUserName, FTPPassword);
                client.UploadFileAsync(uri, "STOR", filePath);
            }
        }
    }

    public class FileUploadManager : MonoBehaviour
    {
        Dictionary<string, string> requestHeaders;
        Action<bool, string> callBack;

        public void UploadFiles(Dictionary<string, string> headers, Dictionary<string, string> fields, string filePathKey, List<string> filePaths, Action<bool, string> callback)
        {
            requestHeaders = headers;
            callBack = callback;
            WWWForm form = new WWWForm();
            if (fields != null)
            {
                foreach (var kv in fields)
                {
                    form.AddField(kv.Key, kv.Value);
                }
            }
            if (filePaths != null)
            {
                foreach (var filePath in filePaths)
                {
                    form.AddBinaryData(filePathKey, FileManager.ReadAllBytes(filePath));
                }
            }
            StartCoroutine(Post(form));
        }

        public void Upload(Dictionary<string, string> headers, Dictionary<string, string> fields, Dictionary<string, byte[]> binaryDatas, Action<bool, string> callback)
        {
            requestHeaders = headers;
            callBack = callback;
            WWWForm form = new WWWForm();
            if (fields != null)
            {
                foreach (var kv in fields)
                {
                    form.AddField(kv.Key, kv.Value);
                }
            }
            if (binaryDatas != null)
            {
                foreach (var kv in binaryDatas)
                {
                    form.AddBinaryData(kv.Key, kv.Value);
                }
            }
            StartCoroutine(Post(form));
        }

        IEnumerator Post(WWWForm form)
        {
            yield return InsecureHttpUtil.Post(Config.PostFileUrl, form, requestHeaders, (success, response) =>
            {
                if (success)
                {
                    Debug.Log("上传成功");
                    callBack?.Invoke(true, "");
                }
                else
                {
                    string errorInfo = $"error:{response}";
                    Debug.LogError($"上传失败:{errorInfo}");
                    callBack?.Invoke(false, errorInfo);
                }
            });
        }
    }
}
