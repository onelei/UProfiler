using LemonFramework.UProfiler.Core;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

//public class FileUpload : MonoBehaviour
//{
//    private void Start()
//    {
//        var uploadManager = gameObject.AddComponent<FileUploadManager>();
//        uploadManager.UploadFiles(Config.PostFileHeaders, new System.Collections.Generic.Dictionary<string, string>() { { "testcase_name", "testaladdin1" } }, "folder", new System.Collections.Generic.List<string>() { { "D:/captureFrame_2022_5_1_23_10_50.zip" } }, (res, errorInfo) =>
//        {
//            Debug.Log($"������:{res}  error:{errorInfo}");
//        });
//    }
//}

namespace LemonFramework.UProfiler.Samples
{
public class FileUploadSample : MonoBehaviour
{
    void Start()
    {
        To_FileUpload();
    }

    private void To_FileUpload()
    {
        WWWForm wWWForm = new WWWForm();
        wWWForm.AddField("testcaseName", "testaladdin1");
        wWWForm.AddField("fileName", "captureFrame_2022_5_1_23_10_51.rar");
        wWWForm.AddBinaryData("folder", GetStreamBytes("D:/captureFrame_2022_5_1_23_10_51.rar"));

        StartCoroutine(UploadResult(Config.PostFileUrl, wWWForm));
    }

    //byte[] GetStreamBytes(string filePath)
    //{
    //    return File.ReadAllBytes(filePath);
    //}

    //��ȡ�ļ�תbyte[]
    public byte[] GetStreamBytes(string path)
    {
        try
        {
            FileStream stream = new FileInfo(path).OpenRead();
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, Convert.ToInt32(stream.Length));
            stream.Close();
            stream.Dispose();
            return buffer;
        }
        catch (Exception ex)
        {
            Debug.LogError("����" + ex.Message);
        }
        return null;
    }
    //�ļ��ϴ�
    public IEnumerator UploadResult(string URL, WWWForm wWWForm)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Post(URL, wWWForm))
        {
            webRequest.SetRequestHeader("X-HW-ID", "com.huawei.xr.cyberverse.cybersim");
            webRequest.SetRequestHeader("X-HW-APPKEY", "bA2J8D1u9djyOVtS8efNTQ==");
            webRequest.SetRequestHeader("Content-Encoding", "gzip");
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.LogError("�ϴ��ɹ�");
                //text.text = "�ϴ��ɹ�";
            }
            else
            {
                Debug.LogError("�ϴ�ʧ��:" + " error:" + webRequest.error + " result:" + webRequest.result.ToString());
                //text.text = "�ϴ�ʧ��:" + " error:" + webRequest.error + " result:" + webRequest.result.ToString();
            }
        }
    }
}
}