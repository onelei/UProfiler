using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    public static class FileHttpUploadManager
    {
        public static IEnumerator UploadFile(string filePath, Dictionary<string, string> headers, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                callback?.Invoke(false, "file not found");
                yield break;
            }

            var fileName = Path.GetFileName(filePath);
            var form = new WWWForm();
            form.AddField("fileName", fileName);
            form.AddBinaryData("file", File.ReadAllBytes(filePath), fileName);

            yield return InsecureHttpUtil.Post(
                Config.PostFileUrl,
                form,
                headers ?? Config.PostFileHeaders,
                callback);
        }
    }
}
