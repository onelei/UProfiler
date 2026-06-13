using System;
using System.IO;
using System.Text;

namespace LemonFramework.UProfiler.Core
{
    public static class FileManager
    {
        public static void CreateDir(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
                return;
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, true);
            }

            Directory.CreateDirectory(dirPath);
        }

        public static bool WriteBinaryDataToFile(string filePath, IBinarySerializable data)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using var fileStream = new FileStream(filePath, FileMode.Create);
            using (var bw = new BinaryWriter(fileStream))
            {
                data.Serialize(bw);
                bw.Flush();
                bw.Close();
            }

            fileStream.Close();
            return true;
        }

        public static bool ReadBinaryDataFromFile(string filePath, ref IBinarySerializable data)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            using var fileStream = new FileStream(filePath, FileMode.Open);
            using (var br = new BinaryReader(fileStream))
            {
                data.DeSerialize(br);
                br.Close();
            }

            fileStream.Close();
            return true;
        }

        public static bool WriteBytesToFile(string filePath, byte[] data)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var file = new FileInfo(filePath);
            using Stream sw = file.Create();
            sw.Write(data, 0, data.Length);
            sw.Flush();
            sw.Close();
            return true;
        }

        public static bool WriteToFile(string filePath, string context)
        {
            return WriteToFile(filePath, context, Encoding.Default);
        }

        public static void TryDelete(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[UProfiler] Failed to delete file: {path}\n{ex.Message}");
            }
        }

        public static void TryDeleteDirectory(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
            {
                return;
            }

            try
            {
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[UProfiler] Failed to delete directory: {dirPath}\n{ex.Message}");
            }
        }

        private static bool WriteToFile(string filePath, string context, Encoding encoding)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using FileStream fs = new FileStream(filePath, FileMode.Create);
            var data = encoding.GetBytes(context);
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
            return true;
        }

        public static string ReadAllByLine(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            using StreamReader sr = new StreamReader(path, Encoding.Default);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                sb.AppendLine(line);
            }

            sr.Close();
            return sb.ToString();
        }

        public static byte[] ReadAllBytes(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            return File.ReadAllBytes(path);
        }

        public static void ReplaceContent(string path, string normalStr, string newStr)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            string strContent = File.ReadAllText(path);
            strContent = strContent.Replace(normalStr, newStr);
            File.WriteAllText(path, strContent);
        }

        public static void ReplaceContent(string path, string newStr, params string[] normalStrs)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            string strContent = File.ReadAllText(path);
            for (int i = 0; i < normalStrs.Length; i++)
            {
                strContent = strContent.Replace(normalStrs[i], newStr);
            }

            File.WriteAllText(path, strContent);
        }
    }
}