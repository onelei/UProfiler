using LemonFramework.UProfiler.Core;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace LemonFramework.UProfiler.Samples
{
    public class Normal
    {
        public static int GetMax(int a, int b)
        {
            Debug.LogFormat("a = {0}, b = {1}", a, b);
            return a > b ? a : b;
        }
    }

    [TestInject]
    public class Inject
    {
        public static int GetMax(int a, int b)
        {
            return a;
        }
    }

    public class MethodInjectSample : MonoBehaviour
    {
        public Button btn_ShowFuncAnalysicClick;

        [ProfilerSample]
        void Start()
        {
            Debug.LogFormat("Normal Max: {0}", Normal.GetMax(6, 9));
            Debug.LogFormat("Inject Max: {0}", Inject.GetMax(6, 9));
            //for (int i = 0; i < 3; i++)
            Test();
            for (int i = 0; i < 3; i++)
                TestDefine();

            if (btn_ShowFuncAnalysicClick != null)
            {
                btn_ShowFuncAnalysicClick.onClick.AddListener(() =>
                {
                    //#if ENABLE_ANALYSIS
                    HookUtil.PrintMethodDatas();

                    //var datas = HookUtil.GetFunctionMonitorFileDatas();
                    //if (datas != null && datas.Count > 0)
                    //{
                    //    Debug.Log("--------������к�������������?------");
                    //    foreach (var data in datas)
                    //    {
                    //        Debug.Log(data);
                    //    }

                    //    var datasJsonStr = JsonUtility.ToJson();
                    //    Debug.Log(datasJsonStr);
                    //    MonitorLib.Core.Tools.EmailSend(datasJsonStr);
                    //    var funcAnalysisFile = FileManager.WriteToFile($"{Application.persistentDataPath}/a.txt", datasJsonStr);
                    //    if (funcAnalysisFile)
                    //    {
                    //        //UploadFile(funcAnalysisFilePath);
                    //    }
                    //}
                    //else
                    //{
                    //    Debug.Log("--------û�к������ܼ������?--------");
                    //}
                });
            }
        }

        [FunctionAnalysis]
        [ProfilerSample]
        public void Test()
        {
            Debug.Log("��ʼѭ��100��");
            for (int i = 0; i < 100; i++)
            {
                Debug.Log(i);
            }
            Debug.Log("����ѭ��100��");
        }
        //[ProfilerSampleWithDefineName("-------�Զ���Sample����,��ʱ��û֧��")]
        [FunctionAnalysis]
        [ProfilerSample]
        public void TestDefine()
        {
            Profiler.BeginSample("*************");
            Debug.Log("���������Եķ���");
            Profiler.EndSample();
        }

        [HideAnalysis] //����Ҫ�����ĺ���
        private void OnGUI()
        {

        }
    }
}