using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ModuleDetailBuilder
{
    static readonly string[] FuncPalette =
    {
        "#1677ff", "#69b1ff", "#13c2c2", "#52c41a", "#faad14",
        "#eb2f96", "#722ed1", "#9254de", "#ff7875", "#ffc069",
        "#95de64", "#5cdbd3"
    };

    public static Dictionary<string, ModuleDetailPayload> Build(ReportDataContext data)
    {
        var result = new Dictionary<string, ModuleDetailPayload>(StringComparer.OrdinalIgnoreCase);
        if (data.ModuleTime.X.Count == 0)
        {
            return result;
        }

        foreach (var meta in data.ModuleTime.Modules)
        {
            result[meta.Key] = meta.Key switch
            {
                "logic" => BuildLogicDetail(data, meta),
                "rendering" => BuildRenderingDetail(data, meta),
                _ => BuildGenericDetail(data, meta)
            };
        }

        return result;
    }

    static ModuleDetailPayload BuildLogicDetail(ReportDataContext data, ModuleMeta meta)
    {
        var funcs = data.FuncAnalysis
            .Where(item => item.AverageTime > 0)
            .OrderByDescending(item => item.AverageTime)
            .Take(12)
            .ToList();

        if (funcs.Count == 0)
        {
            var generic = BuildGenericDetail(data, meta);
            return new ModuleDetailPayload
            {
                Key = generic.Key,
                Label = generic.Label,
                Title = "逻辑代码模块 CPU 耗时",
                DetailTitle = "逻辑代码模块 CPU 耗时",
                PieTitle = "函数占比预览",
                ChartTitle = "逻辑代码模块 CPU 耗时",
                Color = generic.Color,
                HasDrillDown = false,
                EmptyHint = "暂无函数性能数据。请在 Unity 中执行 Hook 注入并启用函数分析后重新测试。",
                PieSlices = generic.PieSlices,
                Metrics = generic.Metrics,
                X = generic.X,
                Series = generic.Series
            };
        }

        var totalAvg = funcs.Sum(item => item.AverageTime);
        var pieSlices = funcs.Select((item, index) => new ModuleDetailPieSlice
        {
            Name = item.Name,
            Value = Math.Round(item.AverageTime, 2),
            Color = FuncPalette[index % FuncPalette.Length]
        }).ToList();

        var metrics = funcs.Select(item => new ModuleDetailMetricRow
        {
            Name = item.Name,
            AverageMs = Math.Round(item.AverageTime, 2),
            Ratio = totalAvg > 0 ? Math.Round(item.AverageTime / totalAvg * 100, 2) : 0,
            LinkTarget = "#func"
        }).ToList();

        var logicSeries = data.ModuleTime.Series.TryGetValue("logic", out var logicValues)
            ? logicValues
            : new List<double>();
        var series = BuildFunctionTrendSeries(funcs, logicSeries, totalAvg);

        return new ModuleDetailPayload
        {
            Key = meta.Key,
            Label = meta.Label,
            Title = "逻辑代码模块 CPU 耗时",
            DetailTitle = "逻辑代码模块函数堆栈 CPU 耗时",
            PieTitle = "函数占比预览",
            ChartTitle = "逻辑代码模块函数堆栈 CPU 耗时",
            Color = meta.Color,
            HasDrillDown = true,
            PieSlices = pieSlices,
            Metrics = metrics,
            X = data.ModuleTime.X,
            Series = series
        };
    }

    static ModuleDetailPayload BuildRenderingDetail(ReportDataContext data, ModuleMeta meta)
    {
        var renderList = data.RenderInfos?.RenderInfoList ?? new List<RenderInfoDto>();
        var metrics = new List<ModuleDetailMetricRow>();
        if (renderList.Count > 0)
        {
            metrics.Add(new ModuleDetailMetricRow
            {
                Name = "DrawCall",
                AverageMs = Math.Round(renderList.Average(item => item.DrawCall), 0),
                Ratio = 0,
                Unit = "次"
            });
            metrics.Add(new ModuleDetailMetricRow
            {
                Name = "SetPassCall",
                AverageMs = Math.Round(renderList.Average(item => item.SetPassCall), 0),
                Ratio = 0,
                Unit = "次"
            });
            metrics.Add(new ModuleDetailMetricRow
            {
                Name = "三角面",
                AverageMs = Math.Round(renderList.Average(item => item.Triangles), 0),
                Ratio = 0,
                Unit = "个"
            });
            metrics.Add(new ModuleDetailMetricRow
            {
                Name = "顶点",
                AverageMs = Math.Round(renderList.Average(item => item.Vertices), 0),
                Ratio = 0,
                Unit = "个"
            });
        }

        var pieSlices = metrics.Select((item, index) => new ModuleDetailPieSlice
        {
            Name = item.Name,
            Value = Math.Max(1, item.AverageMs),
            Color = FuncPalette[index % FuncPalette.Length]
        }).ToList();

        var series = new List<ModuleDetailSeries>();
        if (data.ModuleTime.Series.TryGetValue("rendering", out var renderMs))
        {
            series.Add(new ModuleDetailSeries
            {
                Key = "rendering",
                Label = "渲染 CPU (ms)",
                Color = meta.Color,
                Data = renderMs,
                YAxisIndex = 0
            });
        }

        foreach (var metric in new[] { ("drawCall", "DrawCall", renderList.Select(item => (double)item.DrawCall).ToList()),
                         ("setPass", "SetPassCall", renderList.Select(item => (double)item.SetPassCall).ToList()) })
        {
            if (metric.Item3.Count == 0)
            {
                continue;
            }

            series.Add(new ModuleDetailSeries
            {
                Key = metric.Item1,
                Label = metric.Item2,
                Color = metric.Item1 == "drawCall" ? "#1677ff" : "#ff4d4f",
                Data = AlignToSampleFrames(data.ModuleTime.X, renderList, metric.Item3),
                YAxisIndex = 1,
                Unit = "次"
            });
        }

        return new ModuleDetailPayload
        {
            Key = meta.Key,
            Label = meta.Label,
            Title = "渲染模块 CPU 耗时",
            DetailTitle = "渲染模块 CPU 耗时",
            PieTitle = "渲染指标占比",
            ChartTitle = "渲染模块 CPU 耗时",
            Color = meta.Color,
            HasDrillDown = true,
            PieSlices = pieSlices,
            Metrics = metrics,
            X = data.ModuleTime.X,
            Series = series,
            DualAxis = series.Any(item => item.YAxisIndex == 1)
        };
    }

    static ModuleDetailPayload BuildGenericDetail(ReportDataContext data, ModuleMeta meta)
    {
        data.ModuleTime.Series.TryGetValue(meta.Key, out var moduleSeries);
        moduleSeries ??= new List<double>();
        var avg = moduleSeries.Count > 0 ? moduleSeries.Average() : 0;

        return new ModuleDetailPayload
        {
            Key = meta.Key,
            Label = meta.Label,
            Title = meta.Label + "模块 CPU 耗时",
            DetailTitle = meta.Label + "模块 CPU 耗时",
            PieTitle = "模块占比预览",
            ChartTitle = meta.Label + "模块 CPU 耗时",
            Color = meta.Color,
            HasDrillDown = true,
            PieSlices = new List<ModuleDetailPieSlice>
            {
                new() { Name = meta.Label, Value = Math.Round(avg, 2), Color = meta.Color }
            },
            Metrics = new List<ModuleDetailMetricRow>
            {
                new()
                {
                    Name = meta.Label,
                    AverageMs = Math.Round(avg, 2),
                    Ratio = 100,
                    Unit = "ms"
                }
            },
            X = data.ModuleTime.X,
            Series = new List<ModuleDetailSeries>
            {
                new()
                {
                    Key = meta.Key,
                    Label = meta.Label + " (ms)",
                    Color = meta.Color,
                    Data = moduleSeries,
                    YAxisIndex = 0
                }
            }
        };
    }

    static List<ModuleDetailSeries> BuildFunctionTrendSeries(
        List<FuncAnalysisInfoDto> funcs,
        List<double> logicSeries,
        float totalAvg)
    {
        var result = new List<ModuleDetailSeries>();
        for (var i = 0; i < funcs.Count; i++)
        {
            var func = funcs[i];
            var ratio = totalAvg > 0 ? func.AverageTime / totalAvg : 0;
            var data = logicSeries.Select(value => Math.Round(value * ratio, 2)).ToList();
            result.Add(new ModuleDetailSeries
            {
                Key = "func_" + i,
                Label = func.Name + " (ms)",
                Color = FuncPalette[i % FuncPalette.Length],
                Data = data,
                YAxisIndex = 0
            });
        }

        return result;
    }

    static List<double> AlignToSampleFrames(
        List<int> sampleFrames,
        List<RenderInfoDto> renderList,
        List<double> values)
    {
        if (renderList.Count == 0 || values.Count == 0)
        {
            return sampleFrames.Select(_ => 0d).ToList();
        }

        return sampleFrames.Select(frame =>
        {
            var index = renderList.FindIndex(item => item.FrameIndex == frame);
            if (index < 0)
            {
                index = renderList
                    .Select((item, i) => new { item.FrameIndex, i })
                    .OrderBy(item => Math.Abs(item.FrameIndex - frame))
                    .First().i;
            }

            return index < values.Count ? values[index] : 0;
        }).ToList();
    }
}
