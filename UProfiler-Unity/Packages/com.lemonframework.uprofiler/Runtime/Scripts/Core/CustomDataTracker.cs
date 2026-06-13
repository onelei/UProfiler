using System;
using System.Collections.Generic;
using System.Linq;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>Optional custom dashboard / function group / variable / code segment recording.</summary>
    public static class CustomDataTracker
    {
        static readonly List<CustomDashboardPanelRow> DashboardPanels = new List<CustomDashboardPanelRow>();
        static readonly List<CustomFuncGroupRow> FuncGroups = new List<CustomFuncGroupRow>();
        static readonly Dictionary<string, HashSet<string>> VarNames = new Dictionary<string, HashSet<string>>();
        static readonly List<CustomVarSampleRow> VarSamples = new List<CustomVarSampleRow>();
        static readonly List<CustomCodeSegmentRow> CodeSegments = new List<CustomCodeSegmentRow>();

        public static void Clear()
        {
            DashboardPanels.Clear();
            FuncGroups.Clear();
            VarNames.Clear();
            VarSamples.Clear();
            CodeSegments.Clear();
        }

        public static CustomDashboardPanelRow GetOrCreatePanel(string panelName)
        {
            var panel = DashboardPanels.FirstOrDefault(item => item.name == panelName);
            if (panel != null)
            {
                return panel;
            }

            panel = new CustomDashboardPanelRow { name = panelName };
            DashboardPanels.Add(panel);
            return panel;
        }

        public static void RecordDashboardMetric(string panelName, string label, string unit, int frameIndex, double value)
        {
            var panel = GetOrCreatePanel(panelName);
            var metric = panel.metrics.FirstOrDefault(item => item.label == label);
            if (metric == null)
            {
                metric = new CustomDashboardMetricRow { label = label, unit = unit };
                panel.metrics.Add(metric);
            }

            metric.frames.Add(frameIndex);
            metric.values.Add(value);
        }

        public static void RecordFuncGroup(string groupName, ModuleFuncStackFunctionRow function)
        {
            var group = FuncGroups.FirstOrDefault(item => item.groupName == groupName);
            if (group == null)
            {
                group = new CustomFuncGroupRow { groupName = groupName };
                FuncGroups.Add(group);
            }

            group.functions.Add(function);
        }

        public static void RecordVar(int frameIndex, string varName, string value)
        {
            if (!VarNames.TryGetValue(varName, out var set))
            {
                set = new HashSet<string>();
                VarNames[varName] = set;
            }

            VarSamples.Add(new CustomVarSampleRow
            {
                frameIndex = frameIndex,
                varName = varName,
                value = value
            });
        }

        public static void RecordCodeSegment(string name, int startFrame, int endFrame, double totalMs)
        {
            CodeSegments.Add(new CustomCodeSegmentRow
            {
                name = name,
                startFrame = startFrame,
                endFrame = endFrame,
                totalMs = Math.Round(totalMs, 2)
            });
        }

        public static CustomDashboardUploadData BuildDashboardPayload()
        {
            return new CustomDashboardUploadData { panels = DashboardPanels.ToList() };
        }

        public static CustomFuncsUploadData BuildFuncsPayload()
        {
            return new CustomFuncsUploadData { groups = FuncGroups.ToList() };
        }

        public static CustomVarsUploadData BuildVarsPayload()
        {
            return new CustomVarsUploadData
            {
                varNames = VarNames.Keys.OrderBy(item => item).ToList(),
                samples = VarSamples.ToList()
            };
        }

        public static CustomCodeUploadData BuildCodePayload()
        {
            return new CustomCodeUploadData { segments = CodeSegments.ToList() };
        }

        public static bool HasDashboardData => DashboardPanels.Count > 0;
        public static bool HasFuncsData => FuncGroups.Count > 0;
        public static bool HasVarsData => VarSamples.Count > 0;
        public static bool HasCodeData => CodeSegments.Count > 0;
    }
}
