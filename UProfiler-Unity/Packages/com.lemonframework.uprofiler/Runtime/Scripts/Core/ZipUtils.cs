using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;

namespace LemonFramework.UProfiler.Core
{
    public static class ZipUtils
    {
        /// <summary>
        /// 缓存字节
        /// </summary>
        private const int BufferSize = 4096;

        /// <summary>
        /// 压缩最小等级
        /// </summary>
        public const int CompressionLevelMin = 0;

        /// <summary>
        /// 压缩最大等级
        /// </summary>
        public const int CompressionLevelMax = 9;

        private static Dictionary<string, string> GetAllFileSystemEntities(string source, string topDirectory)
        {
            Dictionary<string, string> entitiesDictionary = new Dictionary<string, string>
            {
                {source, source.Replace(topDirectory, "")}
            };

            if (!Directory.Exists(source)) return entitiesDictionary;
            //一次性获取下级所有目录，避免递归
            string[] directories = Directory.GetDirectories(source, "*.*", SearchOption.AllDirectories);
            foreach (string directory in directories)
            {
                entitiesDictionary.Add(directory, directory.Replace(topDirectory, ""));
            }

            string[] files = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                entitiesDictionary.Add(file, file.Replace(topDirectory, ""));
            }

            return entitiesDictionary;
        }

        /// <summary>
        /// 校验压缩等级
        /// </summary>
        /// <param name="compressionLevel"></param>
        /// <returns></returns>
        private static int CheckCompressionLevel(int compressionLevel)
        {
            compressionLevel = compressionLevel < CompressionLevelMin ? CompressionLevelMin : compressionLevel;
            compressionLevel = compressionLevel > CompressionLevelMax ? CompressionLevelMax : compressionLevel;
            return compressionLevel;
        }

        #region 字节压缩与解压缩

        public static byte[] CompressBytes(byte[] sourceBytes, string password = null, int compressionLevel = 6)
        {
            byte[] result = new byte[] { };

            if (sourceBytes.Length > 0)
            {
                try
                {
                    using (MemoryStream tempStream = new MemoryStream())
                    {
                        using (MemoryStream readStream = new MemoryStream(sourceBytes))
                        {
                            using (ZipOutputStream zipStream = new ZipOutputStream(tempStream))
                            {
                                zipStream.Password = password; //设置密码
                                zipStream.SetLevel(CheckCompressionLevel(compressionLevel)); //设置压缩等级

                                ZipEntry zipEntry = new ZipEntry("ZipBytes")
                                {
                                    DateTime = DateTime.Now, Size = sourceBytes.Length
                                };
                                zipStream.PutNextEntry(zipEntry);
                                int readLength = 0;
                                byte[] buffer = new byte[BufferSize];

                                do
                                {
                                    readLength = readStream.Read(buffer, 0, BufferSize);
                                    zipStream.Write(buffer, 0, readLength);
                                } while (readLength == BufferSize);

                                readStream.Close();
                                zipStream.Flush();
                                zipStream.Finish();
                                result = tempStream.ToArray();
                                zipStream.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("压缩字节数组发生错误", ex);
                }
            }

            return result;
        }

        public static byte[] DecompressBytes(byte[] sourceBytes, string password = null)
        {
            byte[] result = new byte[] { };

            if (sourceBytes.Length > 0)
            {
                try
                {
                    using (MemoryStream tempStream = new MemoryStream(sourceBytes))
                    {
                        using (MemoryStream writeStream = new MemoryStream())
                        {
                            using (ZipInputStream zipStream = new ZipInputStream(tempStream))
                            {
                                zipStream.Password = password;
                                ZipEntry zipEntry = zipStream.GetNextEntry();

                                if (zipEntry != null)
                                {
                                    byte[] buffer = new byte[BufferSize];
                                    int readLength = 0;

                                    do
                                    {
                                        readLength = zipStream.Read(buffer, 0, BufferSize);
                                        writeStream.Write(buffer, 0, readLength);
                                    } while (readLength == BufferSize);

                                    writeStream.Flush();
                                    result = writeStream.ToArray();
                                    writeStream.Close();
                                }

                                zipStream.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("解压字节数组发生错误", ex);
                }
            }

            return result;
        }

        #endregion

        #region 文件压缩与解压缩

        private static Dictionary<string, string> PrepareFileSystementities(
            IEnumerable<string> sourceFileEntityPathList)
        {
            Dictionary<string, string> fileEntityDictionary = new Dictionary<string, string>(); //文件字典
            foreach (string fileEntityPath in sourceFileEntityPathList)
            {
                string path = fileEntityPath;
                //保证传入的文件夹也被压缩进文�?
                if (path.EndsWith(@"\"))
                {
                    path = path.Remove(path.LastIndexOf(@"\"));
                }

                var parentDirectoryPath = Path.GetDirectoryName(path) + @"\";

                if (parentDirectoryPath.EndsWith(@":\\")) //防止根目录下把盘符压入的错误
                {
                    parentDirectoryPath = parentDirectoryPath.Replace(@"\\", @"\");
                }

                parentDirectoryPath = parentDirectoryPath.Replace('\\', '/');
                //获取目录中所有的文件系统对象
                Dictionary<string, string> subDictionary = GetAllFileSystemEntities(path, parentDirectoryPath);

                //将文件系统对象添加到总的文件字典�?
                foreach (string key in subDictionary.Keys)
                {
                    if (!fileEntityDictionary.ContainsKey(key)) //检测重复项
                    {
                        fileEntityDictionary.Add(key, subDictionary[key]);
                    }
                }
            }

            return fileEntityDictionary;
        }

        /// <summary>
        /// 压缩单个文件
        /// </summary>
        /// <param name="path">源文件夹路径/param>
        /// <param name="zipFilePath">压缩文件路径</param>
        /// <param name="comment">注释信息</param>
        /// <param name="password">压缩密码</param>
        /// <returns></returns>
        public static bool ZipFile(string path, string zipFilePath,
            string comment = null, string password = null, int compressionLevel = 6)
        {
            return ZipFiles(new string[] {path}, zipFilePath, comment, password, compressionLevel);
        }

        /// <summary>
        /// 压缩多个文件
        /// </summary>
        public static bool ZipFiles(IEnumerable<string> sourceList, string zipFilePath,
            string comment = null, string password = null, int compressionLevel = 0)
        {
            bool result = false;

            try
            {
                //检测目标文件所属的文件夹是否存在，如果不存在则建立
                string zipFileDirectory = Path.GetDirectoryName(zipFilePath);
                if (!Directory.Exists(zipFileDirectory))
                {
                    Directory.CreateDirectory(zipFileDirectory);
                }

                Dictionary<string, string> dictionaryList = PrepareFileSystementities(sourceList);

                using (ZipOutputStream zipStream = new ZipOutputStream(File.Create(zipFilePath)))
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        zipStream.Password = password; //设置密码
                    }

                    if (!string.IsNullOrEmpty(comment))
                    {
                        zipStream.SetComment(comment); //添加注释
                    }

                    zipStream.SetLevel(CheckCompressionLevel(compressionLevel)); //设置压缩等级

                    foreach (string key in dictionaryList.Keys) //从字典取文件添加到压缩文�?
                    {
                        if (File.Exists(key)) //判断是文件还是文件夹
                        {
                            FileInfo fileItem = new FileInfo(key);
                            string parentDir = Path.GetDirectoryName(fileItem.FullName);
                            using (FileStream readStream = fileItem.Open(FileMode.Open,
                                FileAccess.Read, FileShare.Read))
                            {
                                ZipEntry zipEntry = new ZipEntry(dictionaryList[key].Replace(parentDir, ""));
                                zipEntry.DateTime = fileItem.LastWriteTime;
                                zipEntry.Size = readStream.Length;
                                zipStream.PutNextEntry(zipEntry);
                                int readLength = 0;
                                byte[] buffer = new byte[BufferSize];

                                do
                                {
                                    readLength = readStream.Read(buffer, 0, BufferSize);
                                    zipStream.Write(buffer, 0, readLength);
                                } while (readLength == BufferSize);

                                readStream.Close();
                            }
                        }
                        else
                        {
                            ZipEntry zipEntry = new ZipEntry(dictionaryList[key] + "/");
                            zipStream.PutNextEntry(zipEntry);
                        }
                    }

                    zipStream.Flush();
                    zipStream.Finish();
                    zipStream.Close();
                }

                result = true;
            }
            catch (Exception ex)
            {
                throw new Exception("压缩文件失败", ex);
            }

            return result;
        }

        /// <summary>
        /// ZIP:解压一个zip文件
        /// </summary>
        /// <param name="zipFile">需要解压的Zip文件（绝对路径）</param>
        /// <param name="targetDirectory">解压到的目录</param>
        /// <param name="password">解压密码</param>
        /// <param name="overWrite">是否覆盖已存在的文件</param>
        public static void UnZip(string zipFile, string targetDirectory, string password = "", bool overWrite = true)
        {
            //如果解压到的目录不存在，则报�?
            if (!System.IO.Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            //目录结尾
            if (!targetDirectory.EndsWith("\\"))
            {
                targetDirectory = targetDirectory + "\\";
            }

            using (ZipInputStream ziplines = new ZipInputStream(File.OpenRead(zipFile)))
            {
                ziplines.Password = password;
                ZipEntry theEntry;

                while ((theEntry = ziplines.GetNextEntry()) != null)
                {
                    string directoryName = "";
                    string pathToZip = "";
                    pathToZip = theEntry.Name;

                    if (pathToZip != "")
                        directoryName = Path.GetDirectoryName(pathToZip) + "\\";

                    string fileName = Path.GetFileName(pathToZip);

                    Directory.CreateDirectory(targetDirectory + directoryName);

                    if (fileName != "")
                    {
                        if ((File.Exists(targetDirectory + directoryName + fileName) && overWrite) ||
                            (!File.Exists(targetDirectory + directoryName + fileName)))
                        {
                            using (FileStream streamWriter = File.Create(targetDirectory + directoryName + fileName))
                            {
                                int size = 2048;
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    size = ziplines.Read(data, 0, data.Length);

                                    if (size > 0)
                                        streamWriter.Write(data, 0, size);
                                    else
                                        break;
                                }

                                streamWriter.Close();
                            }
                        }
                    }
                }

                ziplines.Close();
            }
        }

        #endregion
    }
}