#if UPROFILER
using LemonFramework.UProfiler.Components;
using LemonFramework.UProfiler.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace LemonFramework.UProfiler.Editor
{
    [InitializeOnLoad]
    static class UProfilerHookCompilationInjector
    {
        internal static volatile bool AutoInjectOnCompile = true;

        static UProfilerHookCompilationInjector()
        {
            var settings = UProfilerSettings.Instance;
            AutoInjectOnCompile = settings == null || settings.autoInjectFunctionAnalysisOnCompile;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        internal static void SetAutoInject(bool enabled)
        {
            AutoInjectOnCompile = enabled;
        }

        static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (!AutoInjectOnCompile)
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
                Debug.Log($"[UProfiler] Injected [FunctionAnalysis] into {Path.GetFileName(assemblyPath)} ({patchedCount} methods).");
        }
    }

    public static class UProfilerHookEditor
    {
        private static string AssemblyPath = Application.dataPath + "/../Library/ScriptAssemblies/Assembly-CSharp.dll";

        public static void HookInject()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorRun();
        }

        public static void HookProfilerSampleInject()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorRun(EAnalyzeType.PROFILESAMPLE);
        }

        public static void HookFunctionAnalysisInject()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorRun(EAnalyzeType.DEFINEFUNC);
        }

        public static void HookLogAllFunction()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorHookLogRun();
        }

        public static void HookLogAllFunctionExceptUpdate()
        {
            if (!EnsureFunctionHookEnabled())
                return;

            AssemblyPostProcessorHookLogRun("Update", "OnGUI");
        }

        public static void ShowFuncAnaysics()
        {
            HookUtil.PrintMethodDatas();
        }

        public static void HookUtilsReport()
        {
            HookUtil.MethodAnalysisReport(PlayerPrefs.GetString("TestTime", ""));
        }

        static bool EnsureFunctionHookEnabled()
        {
            if (UProfilerHost.IsFunctionAnalysisEnabled)
                return true;

            Debug.LogWarning("[UProfiler] Function analysis is disabled. Enable it on UProfilerHost (enableFunctionAnalysis).");
            return false;
        }

        internal static int PatchFunctionAnalysis(AssemblyDefinition assembly)
        {
            return ProcessAssemblyByAttributeWithoutParam(assembly, typeof(FunctionAnalysisAttribute));
        }

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
                using var resolver = UProfilerAssemblyResolver.CreateForAssembly(assemblyPath);
                using var assembly = ReadAssemblyInMemory(assemblyPath, resolver);
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

        static AssemblyDefinition ReadAssemblyInMemory(string assemblyPath, BaseAssemblyResolver resolver)
        {
            var parameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadSymbols = false,
                ReadingMode = ReadingMode.Immediate,
                InMemory = true
            };
            return AssemblyDefinition.ReadAssembly(new MemoryStream(File.ReadAllBytes(assemblyPath)), parameters);
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

        static MethodReference ResolveHookMethod(ModuleDefinition module, string methodName)
        {
            foreach (var type in module.Types)
            {
                if (type.FullName != "LemonFramework.UProfiler.Core.HookUtil")
                    continue;

                foreach (var method in type.Methods)
                {
                    if (method.Name == methodName && method.Parameters.Count == 1)
                        return method;
                }
            }

            return module.ImportReference(typeof(HookUtil).GetMethod(methodName, new[] { typeof(string) }));
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

                        MethodReference begin;
                        MethodReference end;
                        if (attributeType == functionAnalysisType)
                        {
                            begin = ResolveHookMethod(module, "Begin");
                            end = ResolveHookMethod(module, "End");
                        }
                        else
                        {
                            begin = module.ImportReference(
                                typeof(UnityEngine.Profiling.Profiler).GetMethod("BeginSample", new[] { typeof(string) }));
                            end = module.ImportReference(typeof(UnityEngine.Profiling.Profiler).GetMethod("EndSample"));
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

    static class UProfilerAssemblyResolver
    {
        public static DefaultAssemblyResolver CreateForAssembly(string assemblyPath)
        {
            var hints = new List<string>();
            if (!string.IsNullOrEmpty(assemblyPath))
                hints.Add(assemblyPath);

            var contents = EditorApplication.applicationContentsPath;
            if (!string.IsNullOrEmpty(contents))
                hints.Add(Path.Combine(contents, "Managed", "UnityEngine.dll"));

            return Create(hints);
        }

        public static DefaultAssemblyResolver Create(IEnumerable<string> references)
        {
            var resolver = new DefaultAssemblyResolver();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (references == null)
                return resolver;

            foreach (var reference in references)
                AddReference(resolver, seen, reference);

            return resolver;
        }

        static void AddReference(DefaultAssemblyResolver resolver, HashSet<string> seen, string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return;

            var normalized = reference.Replace('\\', '/');
            AddDirectory(resolver, seen, Directory.Exists(reference) ? reference : Path.GetDirectoryName(reference));

            const string marker = "/Editor/Data/";
            var index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return;

            var data = normalized.Substring(0, index) + "/Editor/Data";
            AddDirectory(resolver, seen, Path.Combine(data, "NetStandard", "ref", "2.1.0"));
            AddDirectory(resolver, seen, Path.Combine(data, "Managed"));
            AddDirectory(resolver, seen, Path.Combine(data, "Managed", "UnityEngine"));
            AddDirectory(resolver, seen, Path.Combine(data, "UnityReferenceAssemblies", "unity-4.8-api", "Facades"));
        }

        static void AddDirectory(DefaultAssemblyResolver resolver, HashSet<string> seen, string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            var full = Path.GetFullPath(directory);
            if (!seen.Add(full))
                return;

            resolver.AddSearchDirectory(full);
        }
    }
}
#endif
