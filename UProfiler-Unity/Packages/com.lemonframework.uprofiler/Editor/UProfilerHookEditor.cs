using LemonFramework.UProfiler.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace LemonFramework.UProfiler.Editor
{
    [InitializeOnLoad]
    static class UProfilerHookCompilationInjector
    {
        static UProfilerHookCompilationInjector()
        {
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (!UProfilerSettings.IsFunctionHookEnabled)
                return;

            var settings = UProfilerSettings.Instance;
            if (settings == null || !settings.autoInjectFunctionAnalysisOnCompile)
                return;

            if (messages != null && messages.Any(message => message.type == CompilerMessageType.Error))
                return;

            if (!UProfilerHookEditor.ShouldProcessAssemblyPath(assemblyPath))
                return;

            if (UProfilerHookEditor.TryInjectFunctionAnalysis(
                    assemblyPath,
                    lockReload: false,
                    requestReloadOnSuccess: false,
                    out var patchedCount,
                    silentWhenNoMethods: true))
                Debug.Log($"[UProfiler] Auto-injected [FunctionAnalysis] into {Path.GetFileName(assemblyPath)} ({patchedCount} methods).");
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            if (!UProfilerSettings.IsFunctionHookEnabled)
                return;

            var injectedCount = UProfilerHookEditor.InjectFunctionAnalysisForPlayMode();
            if (injectedCount > 0)
                Debug.Log($"[UProfiler] Injected [FunctionAnalysis] before Play mode ({injectedCount} methods).");
        }
    }

    public static class UProfilerHookEditor
    {
        private static string AssemblyPath = Application.dataPath + "/../Library/ScriptAssemblies/Assembly-CSharp.dll";

        [MenuItem("Hook/Inject All Methods (Profiler)", false, 0)]
        public static void HookInject()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorRun();
        }

        [MenuItem("Hook/Inject [ProfilerSample] Methods", false, 1)]
        public static void HookProfilerSampleInject()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorRun(EAnalyzeType.PROFILESAMPLE);
        }

        [MenuItem("Hook/Inject [FunctionAnalysis] Methods", false, 2)]
        public static void HookFunctionAnalysisInject()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorRun(EAnalyzeType.DEFINEFUNC);
        }

        [MenuItem("Hook/Log All Method Entry Exit", false, 10)]
        public static void HookLogAllFunction()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorHookLogRun();
        }

        [MenuItem("Hook/Log All Except Update OnGUI", false, 11)]
        public static void HookLogAllFunctionExceptUpdate()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorHookLogRun("Update", "OnGUI");
        }

        [MenuItem("Hook/Print Method Timings", false, 20)]
        public static void ShowFuncAnaysics()
        {
            HookUtil.PrintMethodDatas();
        }

        [MenuItem("Hook/Export Function Performance Report", false, 21)]
        public static void HookUtilsReport()
        {
            HookUtil.MethodAnalysisReport(PlayerPrefs.GetString("TestTime", ""));
        }

        [MenuItem("Hook/Inject All Methods (Profiler)", true)]
        [MenuItem("Hook/Inject [ProfilerSample] Methods", true)]
        [MenuItem("Hook/Inject [FunctionAnalysis] Methods", true)]
        [MenuItem("Hook/Log All Method Entry Exit", true)]
        [MenuItem("Hook/Log All Except Update OnGUI", true)]
        [MenuItem("Hook/Print Method Timings", true)]
        [MenuItem("Hook/Export Function Performance Report", true)]
        static bool ValidateHookMenus()
        {
            return UProfilerSettings.IsFunctionHookEnabled;
        }

        static bool EnsureFunctionHookEnabled()
        {
            if (UProfilerSettings.IsFunctionHookEnabled)
                return true;

            Debug.LogWarning("[UProfiler] Function Hook is disabled. Enable it in UProfiler > Settings.");
            return false;
        }

        static readonly ReaderParameters DefaultReaderParameters = new ReaderParameters { ReadSymbols = false };

        public static void AssemblyPostProcessorRun()
        {
            RunInject(
                assembly => ProcessAssembly(assembly),
                $"{Path.GetFileName(AssemblyPath)} inject failed: no methods were patched.");
        }

        public static void AssemblyPostProcessorHookLogRun(params string[] methodsName)
        {
            RunInject(
                assembly => ProcessAssemblyHookLogExceptFunctions(assembly, methodsName),
                $"{Path.GetFileName(AssemblyPath)} inject failed: no methods were patched.");
        }

        static void AssemblyPostProcessorRun(EAnalyzeType analyzeType)
        {
            if (!CanInject())
                return;

            switch (analyzeType)
            {
                case EAnalyzeType.PROFILESAMPLE:
                    TryInjectAssembly(
                        AssemblyPath,
                        assembly => ProcessAssemblyByAttributeWithoutParam(assembly, typeof(ProfilerSampleAttribute)) > 0,
                        lockReload: true,
                        requestReloadOnSuccess: true,
                        $"{Path.GetFileName(AssemblyPath)} inject: no [ProfilerSample] methods found.",
                        noMethodsIsInfo: true);
                    break;
                case EAnalyzeType.DEFINEFUNC:
                    TryInjectFunctionAnalysis(AssemblyPath, lockReload: true, requestReloadOnSuccess: true);
                    break;
            }
        }

        public static bool ShouldProcessAssemblyPath(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                return false;

            var normalizedPath = assemblyPath.Replace('\\', '/');
            if (!normalizedPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return false;

            var fileName = Path.GetFileName(normalizedPath);
            if (fileName.IndexOf("Editor", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return normalizedPath.Contains("/Library/ScriptAssemblies/", StringComparison.OrdinalIgnoreCase);
        }

        public static int InjectFunctionAnalysisForPlayMode()
        {
            var scriptAssembliesDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/ScriptAssemblies"));
            if (!Directory.Exists(scriptAssembliesDir))
                return 0;

            var totalPatched = 0;
            foreach (var assemblyPath in Directory.GetFiles(scriptAssembliesDir, "*.dll"))
            {
                if (!ShouldProcessAssemblyPath(assemblyPath))
                    continue;

                if (TryInjectFunctionAnalysis(
                        assemblyPath,
                        lockReload: false,
                        requestReloadOnSuccess: false,
                        out var patchedCount,
                        silentWhenNoMethods: true))
                    totalPatched += patchedCount;
            }

            return totalPatched;
        }

        public static bool TryInjectFunctionAnalysis(
            string assemblyPath,
            bool lockReload,
            bool requestReloadOnSuccess,
            out int patchedCount,
            bool silentWhenNoMethods = false)
        {
            patchedCount = 0;
            var patched = 0;
            var success = TryInjectAssembly(
                assemblyPath,
                assembly =>
                {
                    patched = ProcessAssemblyByAttributeWithoutParam(assembly, typeof(FunctionAnalysisAttribute));
                    return patched > 0;
                },
                lockReload,
                requestReloadOnSuccess,
                silentWhenNoMethods
                    ? string.Empty
                    : $"{Path.GetFileName(assemblyPath)} inject: no [FunctionAnalysis] methods found.",
                out _,
                noMethodsIsInfo: true);

            if (success)
                patchedCount = patched;

            return success;
        }

        public static bool TryInjectFunctionAnalysis(
            string assemblyPath,
            bool lockReload,
            bool requestReloadOnSuccess,
            bool silentWhenNoMethods = false)
        {
            return TryInjectFunctionAnalysis(
                assemblyPath,
                lockReload,
                requestReloadOnSuccess,
                out _,
                silentWhenNoMethods);
        }

        static void RunInject(
            Func<AssemblyDefinition, bool> processAssembly,
            string noMethodsMessage,
            bool noMethodsIsInfo = false)
        {
            if (!CanInject())
                return;

            TryInjectAssembly(
                AssemblyPath,
                processAssembly,
                lockReload: true,
                requestReloadOnSuccess: true,
                noMethodsMessage,
                noMethodsIsInfo);
        }

        static bool TryInjectAssembly(
            string assemblyPath,
            Func<AssemblyDefinition, bool> processAssembly,
            bool lockReload,
            bool requestReloadOnSuccess,
            string noMethodsMessage,
            out int patchedCount,
            bool noMethodsIsInfo = false)
        {
            patchedCount = 0;
            if (lockReload && !CanInject())
                return false;

            if (!File.Exists(assemblyPath))
            {
                Debug.LogError($"Assembly not found: {assemblyPath}");
                return false;
            }

            if (lockReload)
                EditorApplication.LockReloadAssemblies();

            try
            {
                using var assembly = ReadAssemblyInMemory(assemblyPath, DefaultReaderParameters);
                if (assembly == null)
                {
                    Debug.LogError($"Inject failed to load assembly: {assemblyPath}");
                    return false;
                }

                if (!processAssembly(assembly))
                {
                    if (!string.IsNullOrEmpty(noMethodsMessage))
                    {
                        if (noMethodsIsInfo)
                            Debug.Log(noMethodsMessage);
                        else
                            Debug.LogError(noMethodsMessage);
                    }

                    return false;
                }

                if (!TryWriteAssembly(assembly, assemblyPath))
                {
                    Debug.LogError(
                        "Inject failed to write assembly. Ensure Play mode is stopped and retry. " +
                        "If the issue persists, restart the Unity Editor and inject again before entering Play mode.");
                    return false;
                }

                if (requestReloadOnSuccess)
                    Debug.Log($"Inject succeeded for {Path.GetFileName(assemblyPath)}. Enter Play mode to apply.");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
            finally
            {
                if (lockReload)
                    EditorApplication.UnlockReloadAssemblies();
            }
        }

        static bool TryInjectAssembly(
            string assemblyPath,
            Func<AssemblyDefinition, bool> processAssembly,
            bool lockReload,
            bool requestReloadOnSuccess,
            string noMethodsMessage,
            bool noMethodsIsInfo = false)
        {
            return TryInjectAssembly(
                assemblyPath,
                processAssembly,
                lockReload,
                requestReloadOnSuccess,
                noMethodsMessage,
                out _,
                noMethodsIsInfo);
        }

        static bool CanInject()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
            {
                Debug.Log("Stop Play mode or wait until compilation finishes.");
                return false;
            }

            if (!File.Exists(AssemblyPath))
            {
                Debug.LogError($"Assembly not found: {AssemblyPath}");
                return false;
            }

            return true;
        }

        static AssemblyDefinition ReadAssemblyInMemory(string assemblyPath, ReaderParameters readerParameters)
        {
            var bytes = File.ReadAllBytes(assemblyPath);
            var stream = new MemoryStream(bytes);
            return AssemblyDefinition.ReadAssembly(stream, readerParameters);
        }

        static bool TryWriteAssembly(AssemblyDefinition assembly, string assemblyPath)
        {
            // PDB writers require a file path; inject writes the DLL from memory without symbols.
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                assembly.Write(stream, new WriterParameters { WriteSymbols = false });
                bytes = stream.ToArray();
            }

            const int maxAttempts = 10;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    using (var stream = new FileStream(
                               assemblyPath,
                               FileMode.Create,
                               FileAccess.Write,
                               FileShare.ReadWrite))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    return true;
                }
                catch (IOException) when (attempt < maxAttempts - 1)
                {
                    Thread.Sleep(50 * (attempt + 1));
                }
            }

            return false;
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

                        var hookUtilBegin =
                            module.ImportReference(typeof(HookUtil).GetMethod("Begin", new[] { typeof(string) }));
                        var hookUtilEnd =
                            module.ImportReference(typeof(HookUtil).GetMethod("End", new[] { typeof(string) }));
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

        static bool HasCustomAttribute(ICustomAttributeProvider provider, Type attributeType)
        {
            return provider.CustomAttributes.Any(attribute =>
                attribute.AttributeType.FullName == attributeType.FullName ||
                attribute.AttributeType.Name == attributeType.Name);
        }

        static bool IsMethodAlreadyHooked(MethodDefinition method, Type attributeType)
        {
            if (method.Body == null || method.Body.Instructions.Count == 0)
                return false;

            var expectedMethodName = attributeType == typeof(FunctionAnalysisAttribute) ? "Begin" : "BeginSample";
            foreach (var instruction in method.Body.Instructions.Take(8))
            {
                if (instruction.OpCode != OpCodes.Call || instruction.Operand is not MethodReference calledMethod)
                    continue;

                if (calledMethod.Name == expectedMethodName)
                    return true;
            }

            return false;
        }

        private static int ProcessAssemblyByAttributeWithoutParam(AssemblyDefinition assembly, Type attributeType)
        {
            var profilerSampleType = typeof(ProfilerSampleAttribute);
            var functionAnalysisType = typeof(FunctionAnalysisAttribute);
            if (attributeType != profilerSampleType && attributeType != functionAnalysisType)
                return 0;

            var patchedCount = 0;
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
                        if (HasCustomAttribute(method, typeof(HideAnalysisAttribute)))
                            continue;
                        if (method.Body == null || method.Body.Instructions.Count == 0)
                            continue;
                        if (!HasCustomAttribute(method, attributeType))
                            continue;
                        if (IsMethodAlreadyHooked(method, attributeType))
                            continue;

                        var begin = module.ImportReference(
                            typeof(UnityEngine.Profiling.Profiler).GetMethod("BeginSample", new[] { typeof(string) }));
                        var end = module.ImportReference(typeof(UnityEngine.Profiling.Profiler).GetMethod("EndSample"));

                        if (attributeType == functionAnalysisType)
                        {
                            begin = module.ImportReference(typeof(HookUtil).GetMethod("Begin", new[] { typeof(string) }));
                            end = module.ImportReference(typeof(HookUtil).GetMethod("End", new[] { typeof(string) }));
                        }

                        ILProcessor ilProcessor = method.Body.GetILProcessor();
                        Instruction first = method.Body.Instructions[0];
                        ilProcessor.InsertBefore(first,
                            Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name));
                        ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, begin));

                        Instruction last = method.Body.Instructions[method.Body.Instructions.Count - 1];
                        Instruction lastInstruction = Instruction.Create(OpCodes.Call, end);
                        if (attributeType == functionAnalysisType)
                            ilProcessor.InsertBefore(last,
                                Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name));
                        ilProcessor.InsertBefore(last, lastInstruction);

                        foreach (var jump in method.Body.Instructions.Cast<Instruction>()
                            .Where(i => i.Operand == lastInstruction))
                            jump.Operand = lastInstruction;

                        patchedCount++;
                    }
                }
            }

            return patchedCount;
        }

        /// <summary>Inject Debug.Log at method entry/exit, optionally skipping method names.</summary>
        private static bool ProcessAssemblyHookLogExceptFunctions(AssemblyDefinition assembly,
            params string[] methodsName)
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

                        var begin = module.ImportReference(typeof(HookUtil).GetMethod("BeginDebugLog",
                            new[] { typeof(string) }));
                        var end = module.ImportReference(typeof(HookUtil).GetMethod("EndDebugLog",
                            new[] { typeof(string) }));
                        ILProcessor ilProcessor = method.Body.GetILProcessor();

                        Instruction first = method.Body.Instructions[0];
                        ilProcessor.InsertBefore(first,
                            Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name));
                        ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, begin));

                        Instruction last = method.Body.Instructions[method.Body.Instructions.Count - 1];
                        Instruction lastInstruction =
                            Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name);
                        ilProcessor.InsertBefore(last, lastInstruction);
                        ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, end));

                        foreach (var jump in method.Body.Instructions.Cast<Instruction>()
                            .Where(i => i.Operand == lastInstruction))
                            jump.Operand = lastInstruction;

                        hasProcessed = true;
                    }
                }
            }

            return hasProcessed;
        }
    }
}
