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
            Test();
            for (int i = 0; i < 3; i++)
                TestDefine();

            if (btn_ShowFuncAnalysicClick != null)
            {
                btn_ShowFuncAnalysicClick.onClick.AddListener(() =>
                {
                    HookUtil.PrintMethodDatas();
                });
            }
        }

        [FunctionAnalysis]
        [ProfilerSample]
        public void Test()
        {
            Debug.Log("开始循环100次");
            for (int i = 0; i < 100; i++)
            {
                Debug.Log(i);
            }
            Debug.Log("完成循环100次");
        }

        [FunctionAnalysis]
        [ProfilerSample]
        public void TestDefine()
        {
            Profiler.BeginSample("*************");
            Debug.Log("测试自定义的方法");
            Profiler.EndSample();
        }

        [HideAnalysis] //不需要分析的后函数
        private void OnGUI()
        {
        }
    }
}
