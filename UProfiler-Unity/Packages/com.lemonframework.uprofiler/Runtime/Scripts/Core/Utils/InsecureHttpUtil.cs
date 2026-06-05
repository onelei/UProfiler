using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace LemonFramework.UProfiler.Core
{
    public static class InsecureHttpUtil
    {
        private static bool IsInsecureHttpUrl(string url)
        {
            return !string.IsNullOrEmpty(url)
                   && url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerator Get(string url, Action<bool, string> callback)
        {
            if (!IsInsecureHttpUrl(url))
            {
                using (var request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        callback?.Invoke(true, request.downloadHandler.text);
                    }
                    else
                    {
                        callback?.Invoke(false, request.error);
                    }
#else
                    if (string.IsNullOrEmpty(request.error))
                    {
                        callback?.Invoke(true, request.downloadHandler.text);
                    }
                    else
                    {
                        callback?.Invoke(false, request.error);
                    }
#endif
                }

                yield break;
            }

            string response = null;
            string error = null;
            var finished = false;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var request = (HttpWebRequest) WebRequest.Create(url);
                    request.Method = "GET";
                    request.Timeout = 10000;
                    using (var webResponse = (HttpWebResponse) request.GetResponse())
                    using (var reader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        response = reader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    finished = true;
                }
            });

            while (!finished)
            {
                yield return null;
            }

            callback?.Invoke(error == null, error ?? response);
        }

        public static IEnumerator Post(string url, WWWForm form, Dictionary<string, string> headers,
            Action<bool, string> callback)
        {
            if (!IsInsecureHttpUrl(url))
            {
                using (var request = UnityWebRequest.Post(url, form))
                {
                    ApplyHeaders(request, headers);
                    yield return request.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        callback?.Invoke(true, request.downloadHandler.text);
                    }
                    else
                    {
                        callback?.Invoke(false, $"{request.error} result:{request.result}");
                    }
#else
                    if (string.IsNullOrEmpty(request.error))
                    {
                        callback?.Invoke(true, request.downloadHandler.text);
                    }
                    else
                    {
                        callback?.Invoke(false, request.error);
                    }
#endif
                }

                yield break;
            }

            string response = null;
            string error = null;
            var finished = false;
            var body = form.data;
            var formHeaders = form.headers;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var request = (HttpWebRequest) WebRequest.Create(url);
                    request.Method = "POST";
                    request.Timeout = 30000;
                    request.ContentType = formHeaders["Content-Type"];
                    request.ContentLength = body.Length;
                    if (headers != null)
                    {
                        foreach (var kv in headers)
                        {
                            if (string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            request.Headers[kv.Key] = kv.Value;
                        }
                    }

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(body, 0, body.Length);
                    }

                    using (var webResponse = (HttpWebResponse) request.GetResponse())
                    using (var reader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        response = reader.ReadToEnd();
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response is HttpWebResponse httpResponse)
                    {
                        using (var reader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            response = reader.ReadToEnd();
                        }

                        error =
                            $"The remote server returned an error: ({(int) httpResponse.StatusCode}) {httpResponse.StatusDescription}. {response}";
                    }
                    else
                    {
                        error = ex.Message;
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    finished = true;
                }
            });

            while (!finished)
            {
                yield return null;
            }

            callback?.Invoke(error == null, error ?? response);
        }

        static void ApplyHeaders(UnityWebRequest request, Dictionary<string, string> headers)
        {
            if (headers == null)
            {
                return;
            }

            foreach (var kv in headers)
            {
                request.SetRequestHeader(kv.Key, kv.Value);
            }
        }
    }
}