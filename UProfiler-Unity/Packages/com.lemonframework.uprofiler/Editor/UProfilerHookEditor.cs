using LemonFramework.UProfiler.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace LemonFramework.UProfiler.Editor
{
public static class UProfilerHookEditor
{
    private static string AssemblyPath = Application.dataPath + "/../Library/ScriptAssemblies/Assembly-CSharp.dll";

#if ENABLE_ANALYSIS
    [MenuItem("Hook/Inject All Methods (Profiler)")]
    public static void HookInject()
    {
        AssemblyPostProcessorRun();
    }

    [MenuItem("Hook/Inject [ProfilerSample] Methods")]
    public static void HookProfilerSampleInject()
    {
        AssemblyPostProcessorRun(EAnalyzeType.PROFILESAMPLE);
    }

    [MenuItem("Hook/Inject [FunctionAnalysis] Methods")]
    public static void HookFunctionAnalysisInject()
    {
        AssemblyPostProcessorRun(EAnalyzeType.DEFINEFUNC);
    }

    [MenuItem("Hook/Log All Method Entry Exit")]
    public static void HookLogAllFunction()
    {
        AssemblyPostProcessorHookLogRun();
    }

    [MenuItem("Hook/Log All Except Update OnGUI")]
    public static void HookLogAllFunctionExceptUpdate()
    {
        AssemblyPostProcessorHookLogRun("Update", "OnGUI");
    }

    [MenuItem("Hook/Print Method Timings")]
    public static void ShowFuncAnaysics()
    {
        HookUtil.PrintMethodDatas();
    }

    [MenuItem("Hook/Export Function Performance Report")]
    public static void HookUtilsReport()
    {
        var lastTestTime = PlayerPrefs.GetString("TestTime", "");
        HookUtil.MethodAnalysisReport(lastTestTime);
    }

    public static void AssemblyPostProcessorRun()
    {
        try
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
            {
                Debug.Log("Stop Play mode or wait until compilation finishes.");
                return;
            }
            EditorApplication.LockReloadAssemblies();
            var readerParameters = new ReaderParameters
            {
                ReadSymbols = true,
                SymbolReaderProvider = new Mono.Cecil.Pdb.PdbReaderProvider()
            };
            var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath, readerParameters);
            if (assembly == null)
            {
                Debug.LogError($"Inject failed to load assembly: {AssemblyPath}");
                return;
            }
            if (ProcessAssembly(assembly))
            {
                assembly.Write(AssemblyPath, new WriterParameters
                {
                    WriteSymbols = true,
                    SymbolWriterProvider = new Mono.Cecil.Pdb.PdbWriterProvider()
                });
            }
            else
            {
                Debug.LogError($"{Path.GetFileName(AssemblyPath)} inject failed: no methods were patched.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        EditorApplication.UnlockReloadAssemblies();
        Debug.Log("Inject succeeded.");
    }

    public static void AssemblyPostProcessorHookLogRun(params string[] methodsName)
    {
        try
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
            {
                Debug.Log("Stop Play mode or wait until compilation finishes.");
                return;
            }
            EditorApplication.LockReloadAssemblies();
            var readerParameters = new ReaderParameters { ReadSymbols = false };
            var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath, readerParameters);
            if (assembly == null)
            {
                Debug.LogError($"Inject failed to load assembly: {AssemblyPath}");
                return;
            }
            if (ProcessAssemblyHookLogExceptFunctions(assembly, methodsName))
            {
                assembly.Write(AssemblyPath, new WriterParameters { WriteSymbols = true });
            }
            else
            {
                Debug.LogError($"{Path.GetFileName(AssemblyPath)} inject failed: no methods were patched.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        EditorApplication.UnlockReloadAssemblies();
        Debug.Log("Inject succeeded.");
    }

    private static bool ProcessAssembly(AssemblyDefinition assembly)
    {
        bool hasProcessed = false;
        var hideAnalysisType = typeof(HideAnalysisAttribute).FullName;
        foreach (var module in assembly.Modules)
        {
            foreach (var type in module.Types)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                foreach (var method in type.Methods)
                {
                    if (method.Name == ".ctor" || method.Name == ".cctor")
                        continue;
                    if (method.IsAbstract || method.IsVirtual || method.IsGetter || method.IsSetter)
                        continue;
                    if (method.CustomAttributes.Any(a => a.AttributeType.FullName == hideAnalysisType))
                        continue;
                    if (method.Body == null)
                        continue;

                    var hookUtilBegin = module.ImportReference(typeof(HookUtil).GetMethod("Begin", new[] { typeof(string) }));
                    var hookUtilEnd = module.ImportReference(typeof(HookUtil).GetMethod("End", new[] { typeof(string) }));
                    ILProcessor ilProcessor = method.Body.GetILProcessor();

                    Instruction first = method.Body.Instructions[0];
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name));
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, hookUtilBegin));

                    Instruction last = method.Body.Instructions[method.Body.Instructions.Count - 1];
                    Instruction lastInstruction = Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name);
                    ilProcessor.InsertBefore(last, lastInstruction);
                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, hookUtilEnd));

                    foreach (var jump in method.Body.Instructions.Cast<Instruction>().Where(i => i.Operand == lastInstruction))
                    {
                        jump.Operand = lastInstruction;
                    }
                    hasProcessed = true;
                }
            }
        }
        return hasProcessed;
    }
#endif

    static void AssemblyPostProcessorRun(EAnalyzeType analyzeType)
    {
        try
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
            {
                Debug.Log("Stop Play mode or wait until compilation finishes.");
                return;
            }
            EditorApplication.LockReloadAssemblies();
            var readerParameters = new ReaderParameters { ReadSymbols = false };
            var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath, readerParameters);
            if (assembly == null)
            {
                Debug.LogError($"Inject failed to load assembly: {AssemblyPath}");
                return;
            }
            switch (analyzeType)
            {
                case EAnalyzeType.PROFILESAMPLE:
                    if (ProcessAssemblyByAttributeWithoutParam(assembly, typeof(ProfilerSampleAttribute)))
                        assembly.Write(AssemblyPath, new WriterParameters { WriteSymbols = true });
                    else
                        Debug.Log($"{Path.GetFileName(AssemblyPath)} inject: no [ProfilerSample] methods found.");
                    break;
                case EAnalyzeType.DEFINEFUNC:
                    if (ProcessAssemblyByAttributeWithoutParam(assembly, typeof(FunctionAnalysisAttribute)))
                        assembly.Write(AssemblyPath, new WriterParameters { WriteSymbols = true });
                    else
                        Debug.Log($"{Path.GetFileName(AssemblyPath)} inject: no [FunctionAnalysis] methods found.");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        EditorApplication.UnlockReloadAssemblies();
        Debug.Log("Inject succeeded.");
    }

    private static bool ProcessAssemblyByAttributeWithoutParam(AssemblyDefinition assembly, Type attributeType)
    {
        var hideAnalysisType = typeof(HideAnalysisAttribute).FullName;
        bool hasProcessed = false;
        var profilerSampleType = typeof(ProfilerSampleAttribute);
        var functionAnalysisType = typeof(FunctionAnalysisAttribute);
        if (attributeType != profilerSampleType && attributeType != functionAnalysisType)
            return hasProcessed;

        var needInjectAttr = attributeType.FullName;
        foreach (var module in assembly.Modules)
        {
            foreach (var type in module.Types)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                foreach (var method in type.Methods)
                {
                    if (method.Name == ".ctor" || method.Name == ".cctor")
                        continue;
                    if (method.IsAbstract || method.IsVirtual || method.IsGetter || method.IsSetter)
                        continue;
                    if (method.CustomAttributes.Any(a => a.AttributeType.FullName == hideAnalysisType))
                        continue;
                    if (method.Body == null)
                        continue;

                    bool needInject = method.CustomAttributes.Any(a => a.AttributeType.FullName == needInjectAttr);
                    if (!needInject)
                        continue;

                    var begin = module.ImportReference(typeof(UnityEngine.Profiling.Profiler).GetMethod("BeginSample", new[] { typeof(string) }));
                    var end = module.ImportReference(typeof(UnityEngine.Profiling.Profiler).GetMethod("EndSample"));

                    if (attributeType == functionAnalysisType)
                    {
                        begin = module.ImportReference(typeof(HookUtil).GetMethod("Begin", new[] { typeof(string) }));
                        end = module.ImportReference(typeof(HookUtil).GetMethod("End", new[] { typeof(string) }));
                    }

                    ILProcessor ilProcessor = method.Body.GetILProcessor();
                    Instruction first = method.Body.Instructions[0];
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name));
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, begin));

                    Instruction last = method.Body.Instructions[method.Body.Instructions.Count - 1];
                    Instruction lastInstruction = Instruction.Create(OpCodes.Call, end);
                    if (attributeType == functionAnalysisType)
                        ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name));
                    ilProcessor.InsertBefore(last, lastInstruction);

                    foreach (var jump in method.Body.Instructions.Cast<Instruction>().Where(i => i.Operand == lastInstruction))
                        jump.Operand = lastInstruction;

                    hasProcessed = true;
                }
            }
        }
        return hasProcessed;
    }

    /// <summary>Inject Debug.Log at method entry/exit, optionally skipping method names.</summary>
    private static bool ProcessAssemblyHookLogExceptFunctions(AssemblyDefinition assembly, params string[] methodsName)
    {
        bool hasProcessed = false;
        foreach (var module in assembly.Modules)
        {
            foreach (var type in module.Types)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                foreach (var method in type.Methods)
                {
                    if (method.Name == ".ctor" || method.Name == ".cctor")
                        continue;
                    if (method.IsAbstract || method.IsVirtual || method.IsGetter || method.IsSetter)
                        continue;
                    if (method.Body == null)
                        continue;

                    if (methodsName != null && methodsName.Contains(method.Name))
                    {
                        Debug.Log($"Skipped excluded method: {method.FullName}");
                        continue;
                    }

                    var begin = module.ImportReference(typeof(HookUtil).GetMethod("BeginDebugLog", new[] { typeof(string) }));
                    var end = module.ImportReference(typeof(HookUtil).GetMethod("EndDebugLog", new[] { typeof(string) }));
                    ILProcessor ilProcessor = method.Body.GetILProcessor();

                    Instruction first = method.Body.Instructions[0];
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name));
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, begin));

                    Instruction last = method.Body.Instructions[method.Body.Instructions.Count - 1];
                    Instruction lastInstruction = Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name);
                    ilProcessor.InsertBefore(last, lastInstruction);
                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, end));

                    foreach (var jump in method.Body.Instructions.Cast<Instruction>().Where(i => i.Operand == lastInstruction))
                        jump.Operand = lastInstruction;

                    hasProcessed = true;
                }
            }
        }
        return hasProcessed;
    }
}
}
