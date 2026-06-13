(function () {
  'use strict';

  const chartInstances = [];
  const chartRegistry = [];
  let echartsPromise = null;
  let selectedSampleFrame = null;
  let currentSampleIndex = -1;
  let moduleChartInstance = null;
  let modulePieInstance = null;
  let moduleDetailPieInstance = null;
  let moduleDetailChartInstance = null;
  let currentModuleKey = null;
  let currentPanelId = null;
  let activePanelEl = null;
  const panelElements = {};
  let sidebarNavLinks = [];
  let panelChartInitToken = 0;
  let panelChartTimer = null;
  let panelChartIdle = null;

  function escapeHtml(text) {
    return String(text).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  function loadEcharts() {
    if (window.echarts) return Promise.resolve(window.echarts);
    if (echartsPromise) return echartsPromise;
    echartsPromise = new Promise(function (resolve, reject) {
      var script = document.createElement('script');
      script.src = '/js/vendor/echarts.min.js?v=9';
      script.async = true;
      script.onload = function () { resolve(window.echarts); };
      script.onerror = function () {
        var fallback = document.createElement('script');
        fallback.src = 'https://cdn.jsdelivr.net/npm/echarts@5.5.1/dist/echarts.min.js';
        fallback.async = true;
        fallback.onload = function () { resolve(window.echarts); };
        fallback.onerror = reject;
        document.head.appendChild(fallback);
      };
      document.head.appendChild(script);
    });
    return echartsPromise;
  }

  function baseZoom() {
    return [{ type: 'inside' }, { type: 'slider' }];
  }

  function axisPointerLine() {
    return { type: 'line', triggerOn: 'mousemove', lineStyle: { color: '#91caff', width: 1, type: 'dashed' } };
  }

  function getSampleFrames() {
    if (window.capturePayload && capturePayload.frames && capturePayload.frames.length) {
      return capturePayload.frames.slice().sort(function (a, b) { return a - b; });
    }
    if (window.modulePayload && modulePayload.x && modulePayload.x.length) {
      return modulePayload.x.slice();
    }
    return [];
  }

  function findNearestDataIndex(xValues, frameIndex) {
    if (!xValues || !xValues.length) return -1;
    return xValues.reduce(function (best, value, index) {
      return Math.abs(value - frameIndex) < Math.abs(xValues[best] - frameIndex) ? index : best;
    }, 0);
  }

  function findSampleIndex(frameIndex) {
    var frames = getSampleFrames();
    if (!frames.length) return -1;
    return frames.reduce(function (best, value, index) {
      return Math.abs(value - frameIndex) < Math.abs(frames[best] - frameIndex) ? index : best;
    }, 0);
  }

  function toFrameNumber(value) {
    if (value == null) return null;
    var n = typeof value === 'number' ? value : parseInt(String(value), 10);
    return isNaN(n) ? null : n;
  }

  function captureUrl(frameIndex) {
    if (!window.reportSession || frameIndex == null) return '';
    return '/capture/' + encodeURIComponent(reportSession) + '/' + frameIndex + '.png';
  }

  function updateCaptureNavState() {
    var frames = getSampleFrames();
    var prevBtn = document.getElementById('capturePrev');
    var nextBtn = document.getElementById('captureNext');
    var navInfo = document.getElementById('captureNavInfo');
    if (navInfo) {
      navInfo.textContent = frames.length
        ? (currentSampleIndex + 1) + ' / ' + frames.length
        : '- / -';
    }
    if (prevBtn) prevBtn.disabled = currentSampleIndex <= 0;
    if (nextBtn) nextBtn.disabled = currentSampleIndex < 0 || currentSampleIndex >= frames.length - 1;
  }

  function renderCaptureImage(captureFrame) {
    var image = document.getElementById('captureImage');
    var placeholder = document.getElementById('capturePlaceholder');
    if (!image || !placeholder) return;

    if (!window.capturePayload || !capturePayload.hasCaptures) {
      image.style.display = 'none';
      placeholder.style.display = 'flex';
      placeholder.textContent = '该会话未上传截图';
      return;
    }

    var url = captureUrl(captureFrame) + '?t=' + captureFrame;
    if (image.dataset.currentSrc === url && image.complete && image.naturalWidth > 0) {
      image.style.display = 'block';
      placeholder.style.display = 'none';
      return;
    }
    image.dataset.currentSrc = url;
    image.onload = function () {
      image.style.display = 'block';
      placeholder.style.display = 'none';
    };
    image.onerror = function () {
      image.style.display = 'none';
      placeholder.style.display = 'flex';
      placeholder.textContent = '截图加载失败';
    };
    image.src = url;
  }

  function updateCapturePanelMeta(captureFrame) {
    var panel = document.getElementById('capturePanel');
    var scene = document.getElementById('captureScene');
    var frameLabel = document.getElementById('captureFrameLabel');
    var device = document.getElementById('captureDevice');
    if (!panel) return;

    panel.classList.remove('hidden');
    if (scene && window.capturePayload) {
      scene.textContent = capturePayload.productName || '当前场景';
    }
    if (frameLabel) {
      frameLabel.textContent = '第 ' + captureFrame + ' 帧';
    }
    if (device && window.capturePayload) {
      var parts = [];
      if (capturePayload.deviceModel) parts.push(capturePayload.deviceModel);
      if (capturePayload.platform) parts.push(capturePayload.platform);
      if (capturePayload.version) parts.push('v' + capturePayload.version);
      device.textContent = parts.join(' · ') || '-';
    }
    updateCaptureNavState();
  }

  function buildFrameMarkerSeries(xValues) {
    return {
      id: 'frame-marker',
      type: 'line',
      data: (xValues || []).map(function () { return 0; }),
      symbol: 'none',
      lineStyle: { width: 0, opacity: 0 },
      itemStyle: { opacity: 0 },
      emphasis: { disabled: true },
      silent: true,
      legendHoverLink: false,
      tooltip: { show: false },
      z: 20,
      animation: false,
      markLine: {
        silent: true,
        symbol: ['none', 'none'],
        animation: false,
        lineStyle: { color: '#1677ff', width: 3, type: 'solid' },
        label: { show: false },
        data: []
      }
    };
  }

  function ensureFrameMarkerSeries(chart, xValues) {
    var option = chart.getOption();
    var series = option.series || [];
    for (var i = 0; i < series.length; i++) {
      if (series[i].id === 'frame-marker') {
        if (series[i].name) {
          chart.setOption({
            series: [{
              id: 'frame-marker',
              name: '',
              tooltip: { show: false },
              legendHoverLink: false
            }]
          });
        }
        return;
      }
    }
    chart.setOption({
      series: series.concat([buildFrameMarkerSeries(xValues)])
    });
  }

  function applySelectionLine(chart, xValue, xValues) {
    if (!chart || xValue == null || !xValues || !xValues.length) return;
    ensureFrameMarkerSeries(chart, xValues);

    var dataIndex = findNearestDataIndex(xValues, xValue);
    var catLabel = String(xValues[dataIndex]);
    var markLineOpt = {
      silent: true,
      symbol: ['none', 'none'],
      animation: false,
      lineStyle: { color: '#1677ff', width: 3, type: 'solid' },
      label: { show: false },
      data: [[
        { xAxis: catLabel, yAxis: 'min' },
        { xAxis: catLabel, yAxis: 'max' }
      ]]
    };

    function drawLine() {
      chart.setOption({
        series: [{
          id: 'frame-marker',
          markLine: markLineOpt
        }]
      });
    }

    drawLine();
    requestAnimationFrame(drawLine);
  }

  function syncChartEntry(entry, frameIndex) {
    if (!entry || !entry.chart || !entry.xValues || !entry.xValues.length) return;
    var dataIndex = findNearestDataIndex(entry.xValues, frameIndex);
    if (dataIndex < 0) return;
    var xValue = entry.xValues[dataIndex];

    entry.chart.dispatchAction({ type: 'showTip', seriesIndex: 0, dataIndex: dataIndex });
    applySelectionLine(entry.chart, xValue, entry.xValues);
  }

  function syncAllCharts(frameIndex) {
    chartRegistry.forEach(function (entry) {
      syncChartEntry(entry, frameIndex);
    });
  }

  function updateModulePieForFrame(frameIndex) {
    if (!modulePieInstance || !window.modulePayload) return;
    var xIndex = modulePayload.x.indexOf(frameIndex);
    if (xIndex < 0) {
      xIndex = findNearestDataIndex(modulePayload.x, frameIndex);
    }
    if (xIndex < 0) return;

    var data = modulePayload.modules.map(function (module) {
      var values = modulePayload.series[module.key] || [];
      return {
        name: module.label,
        value: values[xIndex] || 0,
        itemStyle: { color: module.color }
      };
    });
    modulePieInstance.setOption({ series: [{ data: data }] });
  }

  function selectSampleFrame(frameIndex) {
    frameIndex = toFrameNumber(frameIndex);
    if (frameIndex == null) return;

    var frames = getSampleFrames();
    if (!frames.length) {
      selectedSampleFrame = frameIndex;
      currentSampleIndex = -1;
      updateCapturePanelMeta(frameIndex);
      renderCaptureImage(frameIndex);
      syncAllCharts(frameIndex);
      updateModulePieForFrame(frameIndex);
      return;
    }

    var sampleIndex = -1;
    for (var i = 0; i < frames.length; i++) {
      if (frames[i] === frameIndex) {
        sampleIndex = i;
        break;
      }
    }
    if (sampleIndex < 0) {
      sampleIndex = findSampleIndex(frameIndex);
      frameIndex = frames[sampleIndex];
    }

    selectedSampleFrame = frameIndex;
    currentSampleIndex = sampleIndex;

    updateCapturePanelMeta(frameIndex);
    renderCaptureImage(frameIndex);
    syncAllCharts(frameIndex);
    updateModulePieForFrame(frameIndex);
    updateSceneFrameHint(frameIndex);
  }

  function navigateSample(delta) {
    var frames = getSampleFrames();
    if (!frames.length) return;
    var nextIndex = currentSampleIndex + delta;
    if (nextIndex < 0 || nextIndex >= frames.length) return;
    selectSampleFrame(frames[nextIndex]);
  }

  function registerChart(chart, xValues, chartType) {
    if (!chart || !xValues || !xValues.length) return;
    var entry = { chart: chart, xValues: xValues, type: chartType };
    chartRegistry.push(entry);
    chart.on('datazoom', function () {
      if (selectedSampleFrame != null) {
        applySelectionLine(chart, selectedSampleFrame, xValues);
      }
    });
    chart.on('finished', function () {
      if (selectedSampleFrame != null) {
        applySelectionLine(chart, selectedSampleFrame, xValues);
      }
    });
    if (selectedSampleFrame != null) {
      syncChartEntry(entry, selectedSampleFrame);
    }
  }

  function resolveFrameFromChartClick(chart, xValues, params, pointer) {
    if (params && params.componentType === 'series' && params.dataIndex != null) {
      return toFrameNumber(xValues[params.dataIndex]);
    }
    var point = pointer || (params && params.event ? [params.event.offsetX, params.event.offsetY] : null);
    if (!point || !chart.containPixel({ gridIndex: 0 }, point)) return null;
    var coord = chart.convertFromPixel({ gridIndex: 0 }, point);
    if (!coord || coord[0] == null || isNaN(coord[0])) return null;
    var dataIndex = Math.round(coord[0]);
    if (dataIndex < 0) dataIndex = 0;
    if (dataIndex >= xValues.length) dataIndex = xValues.length - 1;
    return toFrameNumber(xValues[dataIndex]);
  }

  function bindChartFrameSelection(chart, xValues) {
    if (!chart || !xValues || !xValues.length) return;

    var lastPickAt = 0;
    function pickFrame(pointer, params) {
      var now = Date.now();
      if (now - lastPickAt < 80) return;
      var frameIndex = resolveFrameFromChartClick(chart, xValues, params, pointer);
      if (frameIndex == null) return;
      lastPickAt = now;
      selectSampleFrame(frameIndex);
    }

    chart.on('click', function (params) {
      if (!params.event) return;
      pickFrame([params.event.offsetX, params.event.offsetY], params);
    });

    chart.getZr().on('click', function (event) {
      pickFrame([event.offsetX, event.offsetY], null);
    });
  }

  function initCapturePanel() {
    var expandBtn = document.getElementById('captureExpand');
    var prevBtn = document.getElementById('capturePrev');
    var nextBtn = document.getElementById('captureNext');
    var modal = document.getElementById('captureModal');
    var modalImage = document.getElementById('captureModalImage');
    var modalClose = document.getElementById('captureModalClose');
    var image = document.getElementById('captureImage');

    if (prevBtn) prevBtn.onclick = function () { navigateSample(-1); };
    if (nextBtn) nextBtn.onclick = function () { navigateSample(1); };

    if (expandBtn && modal && modalImage && image) {
      expandBtn.onclick = function () {
        if (!image.src) return;
        modalImage.src = image.src;
        modal.classList.remove('hidden');
      };
    }
    if (modal && modalClose) {
      modalClose.onclick = function () { modal.classList.add('hidden'); };
      modal.onclick = function (event) {
        if (event.target === modal) modal.classList.add('hidden');
      };
    }

    document.addEventListener('keydown', function (event) {
      var panel = document.getElementById('capturePanel');
      if (!panel || panel.classList.contains('hidden')) return;
      if (event.key === 'ArrowLeft') navigateSample(-1);
      if (event.key === 'ArrowRight') navigateSample(1);
    });
  }

  function buildOption(type) {
    if (!window.chartPayload) return null;
    var p = window.chartPayload;
    var tooltipBase = { trigger: 'axis', axisPointer: axisPointerLine() };
    if (type === 'fps') {
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.fps.x, name: '帧' },
        yAxis: { type: 'value', name: 'FPS' },
        dataZoom: baseZoom(),
        series: [{ name: 'FPS', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, data: p.fps.y, itemStyle: { color: '#1677ff' } }]
      };
    }
    if (type === 'frametime' && p.frametime && p.frametime.x) {
      var markArea = buildSceneMarkAreas();
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.frametime.x, name: '帧' },
        yAxis: { type: 'value', name: 'ms' },
        dataZoom: baseZoom(),
        series: [{
          name: '每帧耗时',
          type: 'line',
          smooth: true,
          showSymbol: false,
          triggerLineEvent: true,
          data: p.frametime.y,
          itemStyle: { color: '#fa8c16' },
          markArea: markArea
        }]
      };
    }
    if (type === 'memory') {
      return {
        tooltip: tooltipBase,
        legend: { top: 0 },
        grid: { left: 50, right: 20, top: 40, bottom: 50 },
        xAxis: { type: 'category', data: p.memory.x, name: '帧' },
        yAxis: { type: 'value', name: 'MB' },
        dataZoom: baseZoom(),
        series: [
          { name: 'MonoUsed', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, data: p.memory.monoUsed, itemStyle: { color: '#722ed1' }, markLine: buildLowMemoryMarkLine() },
          { name: 'TotalAllocated', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, data: p.memory.totalAllocated, itemStyle: { color: '#1677ff' } },
          { name: 'UnityReserved', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, data: p.memory.unityReserved, itemStyle: { color: '#13c2c2' } }
        ]
      };
    }
    if (type === 'render') {
      return {
        tooltip: tooltipBase,
        legend: { top: 0 },
        grid: { left: 50, right: 20, top: 40, bottom: 50 },
        xAxis: { type: 'category', data: p.render.x, name: '帧' },
        yAxis: { type: 'value' },
        dataZoom: baseZoom(),
        series: [
          { name: 'SetPassCall', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, data: p.render.setPass, itemStyle: { color: '#ff4d4f' } },
          { name: 'DrawCall', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, data: p.render.drawCall, itemStyle: { color: '#1677ff' } },
          { name: '顶点', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, data: p.render.vertices, itemStyle: { color: '#fa8c16' } },
          { name: '三角面', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, data: p.render.triangles, itemStyle: { color: '#52c41a' } }
        ]
      };
    }
    if (type === 'power') {
      return {
        tooltip: tooltipBase,
        legend: { top: 0 },
        grid: { left: 50, right: 20, top: 40, bottom: 50 },
        xAxis: { type: 'category', data: p.power.x, name: '帧' },
        yAxis: [{ type: 'value', name: 'W' }, { type: 'value', name: '℃' }],
        dataZoom: baseZoom(),
        series: [
          { name: '瞬时功耗', type: 'line', smooth: true, showSymbol: false, data: p.power.batteryPower, itemStyle: { color: '#1677ff' } },
          { name: 'CPU温度', type: 'line', smooth: true, showSymbol: false, yAxisIndex: 1, data: p.power.cpuTemp, itemStyle: { color: '#ff4d4f' } }
        ]
      };
    }
    if (type === 'pss') {
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.pss.x, name: '帧' },
        yAxis: { type: 'value', name: 'MB' },
        dataZoom: baseZoom(),
        series: [{ name: 'PSS', type: 'line', smooth: true, showSymbol: false, triggerLineEvent: true, areaStyle: {}, data: p.pss.y, itemStyle: { color: '#1677ff' }, markLine: buildLowMemoryMarkLine() }]
      };
    }
    if (type === 'cpufreq' && p.hardware && p.hardware.x && p.hardware.x.length) {
      return {
        tooltip: tooltipBase,
        grid: { left: 60, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.hardware.x, name: '帧' },
        yAxis: { type: 'value', name: 'MHz' },
        dataZoom: baseZoom(),
        series: [{ name: 'CPU频率', type: 'line', smooth: true, showSymbol: false, data: p.hardware.cpuFreq, itemStyle: { color: '#722ed1' } }]
      };
    }
    if (type === 'netrecv' && p.hardware && p.hardware.x && p.hardware.x.length) {
      return {
        tooltip: tooltipBase,
        grid: { left: 60, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.hardware.x, name: '帧' },
        yAxis: { type: 'value', name: 'KB' },
        dataZoom: baseZoom(),
        series: [{ name: '网络下载', type: 'line', smooth: true, showSymbol: false, areaStyle: { opacity: 0.2 }, data: p.hardware.netRecv, itemStyle: { color: '#13c2c2' } }]
      };
    }
    if (type === 'netsent' && p.hardware && p.hardware.x && p.hardware.x.length) {
      return {
        tooltip: tooltipBase,
        grid: { left: 60, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.hardware.x, name: '帧' },
        yAxis: { type: 'value', name: 'KB' },
        dataZoom: baseZoom(),
        series: [{ name: '网络上传', type: 'line', smooth: true, showSymbol: false, areaStyle: { opacity: 0.2 }, data: p.hardware.netSent, itemStyle: { color: '#fa8c16' } }]
      };
    }
    if (type === 'render-dc' && p.render && p.render.x) {
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.render.x, name: '帧' },
        yAxis: { type: 'value', name: 'DrawCall' },
        dataZoom: baseZoom(),
        series: [{ name: 'DrawCall', type: 'line', smooth: true, showSymbol: false, data: p.render.drawCall, itemStyle: { color: '#1677ff' } }]
      };
    }
    if (type === 'render-tri' && p.render && p.render.x) {
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.render.x, name: '帧' },
        yAxis: { type: 'value', name: '三角面' },
        dataZoom: baseZoom(),
        series: [{ name: '三角面', type: 'line', smooth: true, showSymbol: false, data: p.render.triangles, itemStyle: { color: '#52c41a' } }]
      };
    }
    if (type === 'gpu-bandwidth-real' && window.gpuBandwidthPayload && gpuBandwidthPayload.samples && gpuBandwidthPayload.samples.length) {
      var samples = gpuBandwidthPayload.samples;
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: samples.map(function (s) { return s.frameIndex; }), name: '帧' },
        yAxis: { type: 'value', name: 'MB/s' },
        dataZoom: baseZoom(),
        series: [{ name: 'GPU Total Bandwidth', type: 'line', smooth: true, showSymbol: false, areaStyle: {}, data: samples.map(function (s) { return Math.round(s.totalBytes / 1024 / 1024); }), itemStyle: { color: '#1677ff' } }]
      };
    }
    if (type === 'gpu-bandwidth' && p.render && p.render.x) {
      var maxDc = Math.max.apply(null, p.render.drawCall) || 1;
      var maxTri = Math.max.apply(null, p.render.triangles) || 1;
      var pressure = p.render.drawCall.map(function (dc, i) {
        return Math.round((dc / maxDc) * (p.render.triangles[i] / maxTri) * 100);
      });
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.render.x, name: '帧' },
        yAxis: { type: 'value', name: '压力指数' },
        dataZoom: baseZoom(),
        series: [{ name: '渲染压力', type: 'line', smooth: true, showSymbol: false, areaStyle: {}, data: pressure, itemStyle: { color: '#fa8c16' } }]
      };
    }
    if (type === 'memory-mono' && p.memory && p.memory.x) {
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.memory.x, name: '帧' },
        yAxis: { type: 'value', name: 'MB' },
        dataZoom: baseZoom(),
        series: [{ name: 'MonoUsed', type: 'line', smooth: true, showSymbol: false, data: p.memory.monoUsed, itemStyle: { color: '#722ed1' } }]
      };
    }
    if (type === 'temperature' && p.power && p.power.x) {
      return {
        tooltip: tooltipBase,
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.power.x, name: '帧' },
        yAxis: { type: 'value', name: '℃' },
        dataZoom: baseZoom(),
        series: [{ name: 'CPU温度', type: 'line', smooth: true, showSymbol: false, data: p.power.cpuTemp, itemStyle: { color: '#ff4d4f' } }]
      };
    }
    if (type === 'resource-pie' && window.resourceSummary && resourceSummary.length) {
      return {
        tooltip: { trigger: 'item', formatter: '{b}: {c} bytes ({d}%)' },
        series: [{
          type: 'pie',
          radius: ['40%', '68%'],
          data: resourceSummary.filter(function (row) { return row.avgSizeBytes > 0; }).map(function (row) {
            return { name: row.label || row.type, value: row.avgSizeBytes };
          })
        }]
      };
    }
    if (type === 'resource-trend' && window.resourceSummary && resourceSummary.length) {
      return {
        tooltip: { trigger: 'axis' },
        grid: { left: 50, right: 20, top: 30, bottom: 40 },
        xAxis: { type: 'category', data: resourceSummary.map(function (row) { return row.label || row.type; }) },
        yAxis: { type: 'value', name: 'MB' },
        series: [{
          name: '平均占用',
          type: 'bar',
          data: resourceSummary.map(function (row) { return Math.round(row.avgSizeBytes / 1024 / 1024 * 100) / 100; }),
          itemStyle: { color: '#1677ff' }
        }]
      };
    }
    if (type === 'scene-cpu-bar' && window.scenePayload && scenePayload.overviewBars && scenePayload.overviewBars.length) {
      var modules = scenePayload.overviewModules || ['渲染', 'UI', '加载', '动画', '粒子系统', '物理', '同步等待', '逻辑代码'];
      var moduleKeys = ['rendering', 'ui', 'loading', 'animation', 'particles', 'physics', 'sync', 'logic'];
      var scenes = scenePayload.overviewBars.map(function (bar) { return bar.sceneName; });
      var series = moduleKeys.map(function (key, index) {
        return {
          name: modules[index] || key,
          type: 'bar',
          stack: 'cpu',
          emphasis: { focus: 'series' },
          data: scenePayload.overviewBars.map(function (bar) {
            return bar.moduleMs && bar.moduleMs[key] != null ? bar.moduleMs[key] : 0;
          })
        };
      });
      return {
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        legend: { type: 'scroll', top: 0 },
        grid: { left: 50, right: 20, top: 48, bottom: 80 },
        xAxis: { type: 'category', data: scenes, axisLabel: { rotate: scenes.length > 6 ? 30 : 0 } },
        yAxis: { type: 'value', name: 'ms' },
        series: series
      };
    }
    if (type === 'thread-stack' && window.modulePayload && modulePayload.summary && modulePayload.summary.length) {
      return {
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        grid: { left: 120, right: 20, top: 20, bottom: 30 },
        xAxis: { type: 'value', name: 'ms' },
        yAxis: {
          type: 'category',
          data: modulePayload.summary.map(function (row) { return row.label; }).concat(['Overhead'])
        },
        series: [{
          name: 'CPU耗时均值',
          type: 'bar',
          data: modulePayload.summary.map(function (row) { return row.averageMs; }).concat([
            Math.max(0, modulePayload.summary.reduce(function (sum, row) { return sum + row.averageMs; }, 0) * 0.15)
          ]),
          itemStyle: { color: '#1677ff' }
        }]
      };
    }
    if (type === 'module') {
      return buildModuleChartOption();
    }
    return null;
  }

  function buildModuleChartOption() {
    if (!window.modulePayload || !modulePayload.x || !modulePayload.x.length) return null;
    var series = modulePayload.modules.map(function (module) {
      return {
        name: module.label,
        type: 'line',
        stack: 'module',
        areaStyle: { opacity: 0.85 },
        showSymbol: true,
        symbolSize: 6,
        triggerLineEvent: true,
        emphasis: { focus: 'series' },
        data: modulePayload.series[module.key] || [],
        itemStyle: { color: module.color }
      };
    });
    return {
      tooltip: { trigger: 'axis', axisPointer: axisPointerLine() },
      legend: { type: 'scroll', top: 0, data: modulePayload.modules.map(function (m) { return m.label; }) },
      grid: { left: 50, right: 20, top: 48, bottom: 50 },
      xAxis: { type: 'category', data: modulePayload.x, name: '帧' },
      yAxis: { type: 'value', name: 'ms' },
      dataZoom: baseZoom(),
      series: series.concat([buildFrameMarkerSeries(modulePayload.x)])
    };
  }

  function buildModulePieOption() {
    if (!window.modulePayload || !modulePayload.summary) return null;
    return {
      tooltip: { trigger: 'item', formatter: '{b}: {c} ms ({d}%)' },
      series: [{
        type: 'pie',
        radius: ['42%', '68%'],
        avoidLabelOverlap: true,
        itemStyle: { borderRadius: 4, borderColor: '#fff', borderWidth: 2 },
        label: { formatter: '{b}\n{d}%' },
        data: modulePayload.summary.map(function (row) {
          return { name: row.label, value: row.averageMs, itemStyle: { color: row.color } };
        })
      }]
    };
  }

  function buildLowMemoryMarkLine() {
    var frames = window.chartPayload && chartPayload.lowMemoryFrames;
    if (!frames || !frames.length) return undefined;
    return {
      silent: true,
      symbol: 'none',
      label: { formatter: 'low memory', color: '#ff4d4f', fontSize: 10 },
      lineStyle: { color: '#ff4d4f', type: 'dashed' },
      data: frames.map(function (frame) { return { xAxis: String(frame) }; })
    };
  }

  function buildSceneMarkAreas() {
    if (!window.scenePayload || !scenePayload.scenes || !scenePayload.scenes.length) return undefined;
    var colors = ['rgba(22,119,255,0.08)', 'rgba(82,196,26,0.08)', 'rgba(250,140,22,0.08)', 'rgba(114,46,209,0.08)'];
    return {
      silent: true,
      data: scenePayload.scenes.map(function (scene, index) {
        return [{
          name: scene.sceneName,
          xAxis: String(scene.startFrame),
          itemStyle: { color: colors[index % colors.length] }
        }, {
          xAxis: String(scene.endFrame)
        }];
      })
    };
  }

  async function initChartElement(el) {
    if (el.dataset.initialized === '1') return;
    var type = el.dataset.chart;
    var option = buildOption(type);
    if (!option) {
      el.innerHTML = '<div class="chart-loading">暂无数据</div>';
      return;
    }
    el.dataset.initialized = '1';
    el.innerHTML = '';
    var echarts = await loadEcharts();
    var chart = echarts.init(el);
    el._chartInstance = chart;
    chart.setOption(option);
    chartInstances.push(chart);

    var xValues = null;
    if (type === 'module') {
      moduleChartInstance = chart;
      xValues = modulePayload.x;
      registerChart(chart, xValues, type);
      bindChartFrameSelection(chart, xValues);
      if (selectedSampleFrame == null && xValues.length) {
        selectSampleFrame(xValues[0]);
      }
    } else if (window.chartPayload) {
      var xMap = {
        fps: chartPayload.fps.x,
        frametime: chartPayload.frametime && chartPayload.frametime.x,
        memory: chartPayload.memory.x,
        render: chartPayload.render.x,
        'render-dc': chartPayload.render && chartPayload.render.x,
        'render-tri': chartPayload.render && chartPayload.render.x,
        'gpu-bandwidth': chartPayload.render && chartPayload.render.x,
        'memory-mono': chartPayload.memory && chartPayload.memory.x,
        power: chartPayload.power && chartPayload.power.x,
        temperature: chartPayload.power && chartPayload.power.x,
        pss: chartPayload.pss && chartPayload.pss.x
      };
      xValues = xMap[type];
      if (xValues && xValues.length) {
        registerChart(chart, xValues, type);
        bindChartFrameSelection(chart, xValues);
      }
    }

    var resize = function () {
      chart.resize();
      if (selectedSampleFrame != null && xValues && xValues.length) {
        applySelectionLine(chart, selectedSampleFrame, xValues);
      }
    };
    window.addEventListener('resize', resize);
  }

  async function initModulePieChart() {
    var el = document.getElementById('modulePieChart');
    if (!el || el.dataset.initialized === '1') return;
    var option = buildModulePieOption();
    if (!option) {
      el.innerHTML = '<div class="chart-loading">暂无数据</div>';
      return;
    }
    el.dataset.initialized = '1';
    el.innerHTML = '';
    var echarts = await loadEcharts();
    modulePieInstance = echarts.init(el);
    modulePieInstance.setOption(option);
    chartInstances.push(modulePieInstance);
    if (selectedSampleFrame != null) {
      updateModulePieForFrame(selectedSampleFrame);
    }
    window.addEventListener('resize', function () { modulePieInstance.resize(); });
    modulePieInstance.on('click', function (params) {
      if (!params.name || !window.modulePayload) return;
      var row = modulePayload.summary.find(function (item) { return item.label === params.name; });
      if (row) showModuleDetail(row.key);
    });
  }

  function removeChartsByType(type) {
    for (var i = chartRegistry.length - 1; i >= 0; i--) {
      if (chartRegistry[i].type === type) chartRegistry.splice(i, 1);
    }
  }

  function disposeDetailCharts() {
    if (moduleDetailPieInstance) {
      try { moduleDetailPieInstance.dispose(); } catch (e) { /* ignore */ }
      moduleDetailPieInstance = null;
    }
    if (moduleDetailChartInstance) {
      try { moduleDetailChartInstance.dispose(); } catch (e) { /* ignore */ }
      moduleDetailChartInstance = null;
    }
    removeChartsByType('module-detail');
  }

  function updateModuleSidebarNav(moduleKey) {
    document.querySelectorAll('.module-nav-link, [data-module-nav]').forEach(function (link) {
      link.classList.remove('active');
    });
    if (!moduleKey || moduleKey === 'overview') {
      var overviewLink = document.querySelector('[data-module-nav="overview"]');
      if (overviewLink) overviewLink.classList.add('active');
      return;
    }
    var active = document.querySelector('[data-module-nav="' + moduleKey + '"]');
    if (active) active.classList.add('active');
  }

  function showModuleOverview(options) {
    options = options || {};
    currentModuleKey = null;
    var overview = document.getElementById('moduleOverview');
    var detail = document.getElementById('moduleDetail');
    if (overview) overview.classList.remove('hidden');
    if (detail) detail.classList.add('hidden');
    if (!options.skipPanel) activatePanel('module-time');
    updateModuleSidebarNav('overview');
    if (location.hash.indexOf('module-time/') === 1) {
      history.replaceState(null, '', '#module-time');
    }
  }

  function renderModuleDetailTable(detail) {
    var head = document.getElementById('moduleDetailTableHead');
    var body = document.getElementById('moduleDetailTableBody');
    if (!head || !body) return;

    var isLogic = detail.key === 'logic' && detail.hasDrillDown;
    head.innerHTML = isLogic
      ? '<tr><th>函数名称</th><th>CPU 耗时均值(ms)</th><th>函数占比(%)</th><th>操作</th></tr>'
      : '<tr><th>指标</th><th>均值</th><th>占比</th><th>操作</th></tr>';

    body.innerHTML = (detail.metrics || []).map(function (row) {
      var unit = row.unit || 'ms';
      var value = unit === 'ms'
        ? Number(row.averageMs).toFixed(2) + ' ms'
        : Number(row.averageMs).toFixed(0) + ' ' + unit;
      var ratio = detail.key === 'rendering' ? '-' : Number(row.ratio).toFixed(2) + '%';
      var op = row.linkTarget
        ? '<a class="link-btn" href="' + row.linkTarget + '">查看详细堆栈</a>'
        : '-';
      return '<tr><td>' + escapeHtml(row.name) + '</td><td>' + value + '</td><td>' + ratio + '</td><td>' + op + '</td></tr>';
    }).join('') || '<tr><td colspan="4" class="muted">暂无数据</td></tr>';
  }

  async function renderModuleDetailCharts(detail) {
    disposeDetailCharts();
    var pieEl = document.getElementById('moduleDetailPie');
    var chartEl = document.getElementById('moduleDetailChart');
    if (!pieEl || !chartEl) return;

    var echarts = await loadEcharts();

    if (detail.pieSlices && detail.pieSlices.length) {
      moduleDetailPieInstance = echarts.init(pieEl);
      moduleDetailPieInstance.setOption({
        tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
        series: [{
          type: 'pie',
          radius: ['42%', '68%'],
          avoidLabelOverlap: true,
          itemStyle: { borderRadius: 4, borderColor: '#fff', borderWidth: 2 },
          label: { formatter: '{b}\n{d}%' },
          data: detail.pieSlices.map(function (slice) {
            return { name: slice.name, value: slice.value, itemStyle: { color: slice.color } };
          })
        }]
      });
      chartInstances.push(moduleDetailPieInstance);
    } else {
      pieEl.innerHTML = '<div class="chart-loading">暂无数据</div>';
    }

    if (!detail.x || !detail.x.length || !detail.series || !detail.series.length) {
      chartEl.innerHTML = '<div class="chart-loading">暂无趋势数据</div>';
      return;
    }

    chartEl.innerHTML = '';
    var isLogicStack = detail.key === 'logic' && detail.hasDrillDown;
    var series = detail.series.map(function (item) {
      return {
        name: item.label,
        type: 'line',
        stack: isLogicStack ? 'func-stack' : undefined,
        areaStyle: isLogicStack ? { opacity: 0.85 } : undefined,
        showSymbol: true,
        symbolSize: 5,
        triggerLineEvent: true,
        smooth: true,
        yAxisIndex: item.yAxisIndex || 0,
        data: item.data,
        itemStyle: { color: item.color }
      };
    });

    var option = {
      tooltip: { trigger: 'axis', axisPointer: axisPointerLine() },
      legend: { type: 'scroll', top: 0 },
      grid: { left: 50, right: detail.dualAxis ? 50 : 20, top: 48, bottom: 50 },
      xAxis: { type: 'category', data: detail.x, name: '帧' },
      yAxis: detail.dualAxis
        ? [{ type: 'value', name: 'ms' }, { type: 'value', name: '次数' }]
        : { type: 'value', name: 'ms' },
      dataZoom: baseZoom(),
      series: series
    };

    moduleDetailChartInstance = echarts.init(chartEl);
    moduleDetailChartInstance.setOption(option);
    chartInstances.push(moduleDetailChartInstance);
    registerChart(moduleDetailChartInstance, detail.x, 'module-detail');
    bindChartFrameSelection(moduleDetailChartInstance, detail.x);
    if (selectedSampleFrame != null) {
      syncChartEntry(chartRegistry[chartRegistry.length - 1], selectedSampleFrame);
    }
    window.addEventListener('resize', function () {
      if (moduleDetailChartInstance) moduleDetailChartInstance.resize();
      if (moduleDetailPieInstance) moduleDetailPieInstance.resize();
    });
  }

  function showModuleDetail(moduleKey, options) {
    options = options || {};
    if (!window.moduleDetails || !moduleDetails[moduleKey]) return;
    currentModuleKey = moduleKey;
    var detail = moduleDetails[moduleKey];
    var overview = document.getElementById('moduleOverview');
    var detailView = document.getElementById('moduleDetail');
    if (overview) overview.classList.add('hidden');
    if (detailView) detailView.classList.remove('hidden');

    var crumb = document.getElementById('moduleDetailCrumb');
    if (crumb) crumb.textContent = detail.title;
    var pieTitle = document.getElementById('moduleDetailPieTitle');
    if (pieTitle) pieTitle.textContent = detail.pieTitle || '占比预览';
    var chartTitle = document.getElementById('moduleDetailChartTitle');
    if (chartTitle) chartTitle.textContent = detail.chartTitle || detail.detailTitle;

    var hint = document.getElementById('moduleDetailHint');
    if (hint) {
      if (detail.emptyHint) {
        hint.textContent = detail.emptyHint;
        hint.classList.remove('hidden');
      } else {
        hint.classList.add('hidden');
        hint.textContent = '';
      }
    }

    renderModuleDetailTable(detail);
    renderModuleDetailCharts(detail);
    if (!options.skipPanel) activatePanel('module-time');
    updateModuleSidebarNav(moduleKey);
    if (location.hash !== '#module-time/' + moduleKey) {
      history.replaceState(null, '', '#module-time/' + moduleKey);
    }
  }

  function initModuleNavigation() {
    document.querySelectorAll('.module-row-clickable').forEach(function (row) {
      row.style.cursor = 'pointer';
      row.onclick = function () {
        if (row.dataset.module) showModuleDetail(row.dataset.module);
      };
    });

    var backLink = document.getElementById('moduleBackLink');
    if (backLink) {
      backLink.onclick = function (event) {
        event.preventDefault();
        showModuleOverview();
      };
    }

    document.querySelectorAll('.module-nav-link').forEach(function (link) {
      link.onclick = function (event) {
        var moduleKey = link.getAttribute('data-module-nav');
        if (moduleKey && moduleKey !== 'overview') {
          event.preventDefault();
          showModuleDetail(moduleKey);
        }
      };
    });

    updateModuleSidebarNav(parseModuleHash() || 'overview');
  }

  function parseModuleHash() {
    var hash = (location.hash || '').replace(/^#/, '');
    if (hash.indexOf('module-time/') !== 0) return null;
    return hash.split('/')[1] || null;
  }

  function initLazyCharts() {
    var nodes = document.querySelectorAll('.chart[data-chart]');
    if (!nodes.length) return;
    if (!('IntersectionObserver' in window)) {
      nodes.forEach(function (el) { initChartElement(el); });
      initModulePieChart();
      return;
    }
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          initChartElement(entry.target);
          observer.unobserve(entry.target);
        }
      });
    }, { rootMargin: '120px' });
    nodes.forEach(function (el) { observer.observe(el); });

    var pieEl = document.getElementById('modulePieChart');
    if (pieEl) {
      var pieObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            initModulePieChart();
            pieObserver.unobserve(entry.target);
          }
        });
      }, { rootMargin: '120px' });
      pieObserver.observe(pieEl);
    }
  }

  function initDiagnosis() {
    if (!window.diagnosisItems) return;
    var currentFilter = 'ALL';
    var activeItemId = null;

    function renderDiagnosisList() {
      var list = document.getElementById('diagList');
      if (!list) return;
      var items = diagnosisItems.filter(function (item) {
        return currentFilter === 'ALL' || item.severity === currentFilter;
      });
      list.innerHTML = items.map(function (item) {
        return '<div class="diag-item ' + (activeItemId === item.id ? 'active' : '') + '" data-id="' + item.id + '">' +
          '<div class="sev-bar ' + item.severity.toLowerCase() + '"></div>' +
          '<div><div>' + item.category + ' · ' + item.title + '</div>' +
          '<div class="diag-meta">行业水平 ' + item.industryText + ' · 推荐 ' + item.recommendText + '</div></div>' +
          '<div><span class="pill ' + item.severity.toLowerCase() + '">' + item.valueText + '</span></div></div>';
      }).join('') || '<div style="padding:16px;color:#999;">暂无诊断项</div>';
      list.querySelectorAll('.diag-item').forEach(function (el) {
        el.onclick = function () { showDiagnosisDetail(el.dataset.id); };
      });
      if (!activeItemId && items.length) showDiagnosisDetail(items[0].id);
    }

    function showDiagnosisDetail(id) {
      activeItemId = id;
      var item = diagnosisItems.find(function (x) { return x.id === id; });
      var detail = document.getElementById('diagDetail');
      if (!detail) return;
      if (!item) { detail.innerHTML = ''; return; }
      detail.innerHTML =
        '<div class="pill ' + item.severity.toLowerCase() + '">优化优先级: ' + item.severity + '</div>' +
        '<h3>' + item.title + '</h3><p>' + (item.summary || '') + '</p><p><b>UWA 建议</b></p><ol>' +
        (item.suggestions || []).map(function (s) { return '<li>' + s + '</li>'; }).join('') + '</ol>';
      renderDiagnosisList();
    }

    document.querySelectorAll('.filter-btn').forEach(function (btn) {
      btn.onclick = function () {
        document.querySelectorAll('.filter-btn').forEach(function (x) { x.classList.remove('active'); });
        btn.classList.add('active');
        currentFilter = btn.dataset.filter;
        activeItemId = null;
        renderDiagnosisList();
      };
    });
    renderDiagnosisList();
  }

  function initFuncTable() {
    var dataEl = document.getElementById('funcData');
    var tbody = document.getElementById('funcTbody');
    var pageInfo = document.getElementById('funcPageInfo');
    var prevBtn = document.getElementById('funcPrev');
    var nextBtn = document.getElementById('funcNext');
    if (!dataEl || !tbody) return;
    var rows = JSON.parse(dataEl.textContent || '[]');
    var pageSize = 25;
    var page = 0;
    var totalPages = Math.max(1, Math.ceil(rows.length / pageSize));

    function renderPage() {
      var start = page * pageSize;
      var slice = rows.slice(start, start + pageSize);
      tbody.innerHTML = slice.map(function (item) {
        return '<tr><td>' + item.name + '</td><td>' + item.calls + '</td><td>' + item.avgTime + '</td><td>' + item.useTime + '</td><td>' + item.avgMem + '</td>' +
          '<td><span class="pill ' + item.severity.toLowerCase() + '">' + item.severity + '</span></td></tr>';
      }).join('');
      if (pageInfo) pageInfo.textContent = '第 ' + (page + 1) + ' / ' + totalPages + ' 页，共 ' + rows.length + ' 条';
      if (prevBtn) prevBtn.disabled = page <= 0;
      if (nextBtn) nextBtn.disabled = page >= totalPages - 1;
    }

    if (prevBtn) prevBtn.onclick = function () { if (page > 0) { page--; renderPage(); } };
    if (nextBtn) nextBtn.onclick = function () { if (page < totalPages - 1) { page++; renderPage(); } };
    renderPage();
  }

  function initBriefFilter() {
    var checkbox = document.getElementById('briefOptimOnly');
    var list = document.getElementById('briefMetricsTable');
    if (!checkbox || !list) return;
    function apply() {
      list.querySelectorAll('.brief-collapse-item').forEach(function (row) {
        if (!checkbox.checked) {
          row.style.display = '';
          return;
        }
        row.style.display = row.classList.contains('optimizable') ? '' : 'none';
      });
    }
    checkbox.onchange = apply;
    document.querySelectorAll('.brief-task-jump').forEach(function (btn) {
      btn.onclick = function () {
        checkbox.checked = true;
        apply();
        list.scrollIntoView({ behavior: 'smooth', block: 'start' });
      };
    });
  }

  function initThreadTabs() {
    document.querySelectorAll('.thread-tab').forEach(function (tab) {
      tab.onclick = function () {
        var key = tab.getAttribute('data-thread-tab');
        document.querySelectorAll('.thread-tab').forEach(function (x) { x.classList.remove('active'); });
        document.querySelectorAll('.thread-tab-panel').forEach(function (x) { x.classList.remove('active'); });
        tab.classList.add('active');
        var panel = document.querySelector('.thread-tab-panel[data-thread-panel="' + key + '"]');
        if (panel) {
          panel.classList.add('active');
          initChartsInPanel(panel);
        }
      };
    });
    document.querySelectorAll('.thread-stack-export').forEach(function (btn) {
      btn.onclick = function () {
        var thread = btn.getAttribute('data-thread');
        var table = document.querySelector('.thread-func-table[data-thread-func="' + thread + '"]');
        if (!table) return;
        var lines = ['函数名,耗时均值,总耗时,总体占比,自身耗时,自身占比,总调用次数,单次耗时,调用帧数,每帧调用次数'];
        table.querySelectorAll('tbody tr').forEach(function (row) {
          var cells = row.querySelectorAll('td');
          if (cells.length < 10) return;
          lines.push(Array.prototype.map.call(cells, function (c) { return '"' + c.textContent.trim().replace(/"/g, '""') + '"'; }).join(','));
        });
        var blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
        var a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = (thread || 'thread') + '_stack.csv';
        a.click();
      };
    });
  }

  function initJankFuncTabs() {
    document.querySelectorAll('#jankFuncTabs .jank-tab').forEach(function (tab) {
      tab.onclick = function () {
        if (tab.classList.contains('muted')) return;
        var key = tab.getAttribute('data-jank-cat');
        document.querySelectorAll('#jankFuncTabs .jank-tab').forEach(function (x) { x.classList.remove('active'); });
        document.querySelectorAll('.jank-func-panel').forEach(function (x) { x.classList.remove('active'); });
        tab.classList.add('active');
        var panel = document.querySelector('.jank-func-panel[data-jank-panel="' + key + '"]');
        if (panel) panel.classList.add('active');
      };
    });
    document.querySelectorAll('.func-jump-btn').forEach(function (btn) {
      btn.onclick = function () {
        location.hash = '#func';
      };
    });
  }

  function initTextTabs(containerId, tabClass, panelClass, attrTab, attrPanel) {
    var root = document.getElementById(containerId);
    if (!root) return;
    root.querySelectorAll('.' + tabClass).forEach(function (tab) {
      tab.onclick = function () {
        var key = tab.getAttribute(attrTab);
        root.querySelectorAll('.' + tabClass).forEach(function (x) { x.classList.remove('active'); });
        document.querySelectorAll('.' + panelClass).forEach(function (x) { x.classList.remove('active'); });
        tab.classList.add('active');
        var panel = document.querySelector('.' + panelClass + '[' + attrPanel + '="' + key + '"]');
        if (panel) {
          panel.classList.add('active');
          initChartsInPanel(panel);
        }
      };
    });
  }

  function initScopeToolbar() {
    var globalRange = { start: 0, end: null, scene: null };
    if (window.scenePayload && scenePayload.scenes && scenePayload.scenes.length) {
      globalRange.end = scenePayload.scenes[scenePayload.scenes.length - 1].endFrame;
    }
    document.querySelectorAll('.scope-frame-btn').forEach(function (btn) {
      btn.onclick = function () {
        var val = window.prompt('输入帧号（留空取消）', globalRange.start || 0);
        if (val == null || val === '') return;
        var frame = parseInt(val, 10);
        if (isNaN(frame)) return;
        globalRange.start = frame;
        globalRange.end = frame;
        document.querySelectorAll('.scope-range-label').forEach(function (label) {
          label.textContent = label.textContent.replace(/（.*?）/, '（' + frame + '帧）');
        });
        selectSampleFrame(frame);
      };
    });
    document.querySelectorAll('.scope-scene-btn').forEach(function (btn) {
      btn.onclick = function () {
        location.hash = '#scene-management';
      };
    });
  }

  function initMetricDragSort() {
    document.querySelectorAll('[data-draggable-grid]').forEach(function (grid) {
      var dragEl = null;
      grid.querySelectorAll('.uwa-metric-card[draggable="true"]').forEach(function (card) {
        card.addEventListener('dragstart', function () { dragEl = card; card.classList.add('dragging'); });
        card.addEventListener('dragend', function () { card.classList.remove('dragging'); dragEl = null; });
        card.addEventListener('dragover', function (e) {
          e.preventDefault();
          if (!dragEl || dragEl === card) return;
          var rect = card.getBoundingClientRect();
          var after = (e.clientY - rect.top) / rect.height > 0.5;
          grid.insertBefore(dragEl, after ? card.nextSibling : card);
        });
      });
    });
  }

  function initFuncSearch() {
    document.querySelectorAll('.func-search-input').forEach(function (input) {
      input.oninput = function () {
        var q = input.value.trim().toLowerCase();
        var moduleKey = input.getAttribute('data-module');
        var threadKey = input.getAttribute('data-thread');
        var selector = moduleKey
          ? '.module-func-table[data-module-func="' + moduleKey + '"] tbody tr'
          : '.thread-func-table[data-thread-func="' + threadKey + '"] tbody tr';
        document.querySelectorAll(selector).forEach(function (row) {
          var text = row.textContent.toLowerCase();
          row.style.display = !q || text.indexOf(q) !== -1 ? '' : 'none';
        });
      };
    });
  }

  function initLogFilters() {
    var dataEl = document.getElementById('logData');
    var box = document.getElementById('logBox');
    if (!dataEl || !box) return;
    var lines = JSON.parse(dataEl.textContent || '[]');
    var filter = 'all';
    var chunk = 80;
    var shown = 0;
    var filtered = lines.slice();

    function classify(line) {
      if (/\[Exception\]/i.test(line)) return 'exception';
      if (/\[Assert\]/i.test(line)) return 'assert';
      if (/\[Error\]/i.test(line)) return 'error';
      if (/\[Warning\]/i.test(line)) return 'warning';
      if (/\[Log\]/i.test(line)) return 'log';
      return 'log';
    }

    function applyFilter() {
      filtered = filter === 'all' ? lines : lines.filter(function (line) { return classify(line) === filter; });
      shown = 0;
      box.innerHTML = '';
      appendLines();
    }

    function lineClass(line) {
      var kind = classify(line);
      if (kind === 'error' || kind === 'exception') return 'log-line log-error';
      if (kind === 'warning' || kind === 'assert') return 'log-line log-warning';
      return 'log-line log-info';
    }

    function appendLines() {
      var next = filtered.slice(shown, shown + chunk);
      box.insertAdjacentHTML('beforeend', next.map(function (line) {
        return '<div class="' + lineClass(line) + '">' + escapeHtml(line) + '</div>';
      }).join(''));
      shown += next.length;
      var moreBtn = document.getElementById('logMore');
      if (moreBtn) moreBtn.style.display = shown >= filtered.length ? 'none' : 'inline-block';
    }

    document.querySelectorAll('.log-filter-btn').forEach(function (btn) {
      btn.onclick = function () {
        document.querySelectorAll('.log-filter-btn').forEach(function (x) { x.classList.remove('active'); });
        btn.classList.add('active');
        filter = btn.dataset.logFilter || 'all';
        applyFilter();
      };
    });

    var moreBtn = document.getElementById('logMore');
    if (moreBtn) moreBtn.onclick = appendLines;

    var stackBox = document.getElementById('logStackBox');
    if (stackBox) {
      box.addEventListener('click', function (event) {
        var lineEl = event.target.closest('.log-line');
        if (!lineEl) return;
        box.querySelectorAll('.log-line.selected').forEach(function (x) { x.classList.remove('selected'); });
        lineEl.classList.add('selected');
        var text = lineEl.textContent || '';
        var stackStart = text.search(/\bat\s+\S+|\n\s*UnityEngine\./);
        stackBox.classList.remove('muted');
        if (stackStart > 0) {
          stackBox.textContent = text.slice(stackStart);
        } else {
          stackBox.textContent = text + '\n\n（该日志行未携带堆栈信息，需 Unity 端在上传时附带 StackTrace，详见 todo.md）';
        }
      });
    }

    var exportBtn = document.getElementById('logExport');
    if (exportBtn) {
      exportBtn.onclick = function () {
        downloadTextFile(filtered.join('\n'), 'uprofiler-log-' + filter + '.txt', 'text/plain');
      };
    }

    applyFilter();
  }

  function downloadTextFile(content, filename, mime) {
    var blob = new Blob(['\ufeff' + content], { type: (mime || 'text/plain') + ';charset=utf-8' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
  }

  var icicleColors = ['#1677ff', '#13c2c2', '#52c41a', '#fa8c16', '#722ed1', '#eb2f96', '#2f54eb', '#a0d911', '#fa541c', '#08979c'];

  function renderIcicle(container, title, functions) {
    if (!functions || !functions.length) {
      container.innerHTML = '<div class="chart-loading">暂无堆栈数据</div>';
      return;
    }
    var sorted = functions.slice().sort(function (a, b) { return (b.totalPct || 0) - (a.totalPct || 0); }).slice(0, 12);
    var totalPct = sorted.reduce(function (sum, f) { return sum + (f.totalPct || 0); }, 0);
    var scale = totalPct > 100 ? 100 / totalPct : 1;

    var html = '<div class="icicle-wrap">';
    html += '<div class="icicle-row"><div class="icicle-seg icicle-root" style="width:100%" title="' +
      escapeHtml(title) + ' 100%">' + escapeHtml(title) + ' · 100%</div></div>';
    html += '<div class="icicle-row">';
    var used = 0;
    sorted.forEach(function (func, index) {
      var pct = Math.max(0.5, (func.totalPct || 0) * scale);
      used += pct;
      var label = func.name + ' ' + (func.totalPct || 0).toFixed(2) + '%';
      html += '<div class="icicle-seg" style="width:' + pct.toFixed(2) + '%;background:' + icicleColors[index % icicleColors.length] +
        '" title="' + escapeHtml(label) + '" data-func-name="' + escapeHtml(func.name) + '"><span>' + escapeHtml(func.name) + '</span></div>';
    });
    if (used < 99.5) {
      html += '<div class="icicle-seg icicle-other" style="width:' + (100 - used).toFixed(2) + '%" title="其他 ' + (100 - used).toFixed(2) + '%"><span>其他</span></div>';
    }
    html += '</div>';
    html += '<div class="icicle-row icicle-self-row">';
    sorted.forEach(function (func, index) {
      var pct = Math.max(0.5, (func.totalPct || 0) * scale);
      var selfRatio = func.totalPct > 0 ? Math.min(1, (func.selfPct || 0) / func.totalPct) : 0;
      html += '<div class="icicle-seg icicle-self" style="width:' + pct.toFixed(2) + '%" title="' +
        escapeHtml(func.name + ' 自身 ' + (func.selfPct || 0).toFixed(2) + '%') + '">' +
        '<div class="icicle-self-fill" style="width:' + (selfRatio * 100).toFixed(1) + '%;background:' + icicleColors[index % icicleColors.length] + '"></div></div>';
    });
    html += '</div></div>';
    container.innerHTML = html;

    container.querySelectorAll('.icicle-seg[data-func-name]').forEach(function (seg) {
      seg.onclick = function () {
        var name = seg.dataset.funcName;
        var table = container.closest('.thread-tab-panel, .report-panel');
        if (!table) return;
        var row = Array.prototype.find.call(table.querySelectorAll('tbody tr'), function (tr) {
          return tr.textContent.indexOf(name) >= 0;
        });
        if (row) {
          row.scrollIntoView({ behavior: 'smooth', block: 'center' });
          row.classList.add('row-highlight');
          setTimeout(function () { row.classList.remove('row-highlight'); }, 1600);
        }
      };
    });
  }

  function initIcicleCharts() {
    document.querySelectorAll('[data-module-icicle]').forEach(function (el) {
      var key = el.dataset.moduleIcicle;
      var stack = window.moduleFuncStacks && moduleFuncStacks[key];
      renderIcicle(el, (stack && stack.title) || key, stack ? stack.functions : null);
    });
    document.querySelectorAll('[data-thread-icicle]').forEach(function (el) {
      var name = el.dataset.threadIcicle;
      var thread = window.threadStackPayload && threadStackPayload.threads
        ? threadStackPayload.threads.find(function (t) { return t.name === name; })
        : null;
      renderIcicle(el, name, thread ? thread.functions : null);
    });
  }

  function initResourceEventDetails() {
    document.querySelectorAll('.resource-events-btn').forEach(function (btn) {
      btn.onclick = function () {
        var section = btn.closest('.report-panel');
        if (!section) return;
        var details = section.querySelector('.resource-event-details');
        if (!details) return;
        details.open = true;
        var key = btn.dataset.resKey;
        var matched = null;
        details.querySelectorAll('tbody tr').forEach(function (tr) {
          var hit = tr.dataset.resRow === key;
          tr.classList.toggle('row-dim', !hit);
          if (hit && !matched) matched = tr;
        });
        if (matched) matched.scrollIntoView({ behavior: 'smooth', block: 'center' });
      };
    });
  }

  function initReportDownload() {
    var btn = document.getElementById('reportDownloadJson');
    if (!btn) return;
    btn.onclick = function () {
      var payload = {
        exportedAt: new Date().toISOString(),
        chartPayload: window.chartPayload || null,
        modulePayload: window.modulePayload || null,
        moduleDetails: window.moduleDetails || null,
        scenePayload: window.scenePayload || null,
        diagnosisItems: window.diagnosisItems || null,
        gpuBandwidthPayload: window.gpuBandwidthPayload || null,
        resourceSummary: window.resourceSummary || null,
        moduleFuncStacks: window.moduleFuncStacks || null
      };
      downloadTextFile(JSON.stringify(payload, null, 2), 'uprofiler-report.json', 'application/json');
    };
  }

  function initTableExports() {
    document.querySelectorAll('[data-export-table]').forEach(function (btn) {
      btn.onclick = function () {
        var section = btn.closest('.panel-section') || document;
        var table = section.querySelector('table');
        if (!table) return;
        var rows = [];
        table.querySelectorAll('tr').forEach(function (tr) {
          var cells = [];
          tr.querySelectorAll('th,td').forEach(function (cell) {
            var text = (cell.textContent || '').trim().replace(/\s+/g, ' ');
            cells.push('"' + text.replace(/"/g, '""') + '"');
          });
          if (cells.length) rows.push(cells.join(','));
        });
        downloadTextFile(rows.join('\n'), (btn.dataset.exportTable || 'table') + '.csv', 'text/csv');
      };
    });
  }

  function updateSceneFrameHint(frameIndex) {
    var hint = document.getElementById('sceneFrameHint');
    if (!hint || !window.scenePayload || !scenePayload.scenes) return;
    var scene = scenePayload.scenes.find(function (item) {
      return frameIndex >= item.startFrame && frameIndex <= item.endFrame;
    });
    var ms = window.chartPayload && chartPayload.frametime && chartPayload.frametime.y
      ? chartPayload.frametime.y[findNearestDataIndex(chartPayload.frametime.x, frameIndex)] : null;
    if (scene) {
      hint.textContent = '第' + frameIndex + '帧 ' + scene.sceneName + ' · 每帧耗时: ' + (ms != null ? ms + ' ms' : '-');
    } else {
      hint.textContent = ms != null ? '第' + frameIndex + '帧 · 每帧耗时: ' + ms + ' ms' : '';
    }
  }

  window.addEventListener('beforeunload', function () {
    chartInstances.forEach(function (chart) {
      try { chart.dispose(); } catch (e) { /* ignore */ }
    });
  });

  var panelTitles = {
    brief: '性能简报',
    basicinfo: '运行信息',
    'scene-overview': '场景概览 · 性能概览',
    'scene-management': '场景概览 · 场景管理',
    'gpu-render': 'GPU分析 · GPU 渲染分析',
    'gpu-bandwidth': 'GPU分析 · GPU 带宽分析',
    'gpu-summary': 'GPU分析 · 指标汇总',
    trend: '总体性能趋势',
    'module-time': '模块耗时统计',
    'thread-stack': '各线程 CPU 调用堆栈',
    diagnosis: '性能诊断',
    'jank-frames': '卡顿分析 · 卡顿点分析',
    'jank-func': '卡顿分析 · 重点函数分析',
    'memory-occupy': '内存分析 · 内存占用',
    'memory-resource': '内存分析 · 资源内存',
    'memory-lua': '内存分析 · Lua内存',
    'memory-mono': '内存分析 · Mono内存',
    battery: '耗电量',
    temperature: '温度变化量',
    'custom-dashboard': '自定义面板',
    'custom-funcs': '自定义函数组',
    'custom-vars': '自定义变量',
    'custom-code': '自定义代码段',
    'resource-summary': '资源管理汇总',
    'resource-ab': 'AssetBundle 加载&卸载',
    'resource-load': '资源加载&卸载',
    'resource-instantiate': '资源实例化&激活',
    'module-particles': '粒子系统性能',
    'module-rendering': '渲染模块性能',
    'module-sync': 'GPU同步模块性能',
    'module-logic': '逻辑代码模块性能',
    'module-ui': 'UI模块性能',
    'module-loading': '加载模块性能',
    'module-physics': '物理系统性能',
    'module-animation': '动画模块性能',
    func: '函数性能分析',
    log: '运行日志'
  };

  function resolvePanelFromHash() {
    var hash = (location.hash || '').replace(/^#/, '');
    if (!hash) return 'brief';
    if (hash.indexOf('module-time/') === 0) return 'module-time';
    if (hash.indexOf('module-') === 0) return hash.split('/')[0];
    return hash.split('/')[0] || 'brief';
  }

  function cacheNavigationElements() {
    document.querySelectorAll('.report-panel').forEach(function (panel) {
      if (panel.dataset.panel) panelElements[panel.dataset.panel] = panel;
    });
    sidebarNavLinks = Array.prototype.slice.call(document.querySelectorAll('.sidebar-menu a[href^="#"]'));
  }

  function resizePanelCharts(panelEl) {
    if (!panelEl) return;
    panelEl.querySelectorAll('.chart[data-initialized="1"]').forEach(function (el) {
      if (el._chartInstance) {
        try { el._chartInstance.resize(); } catch (e) { /* ignore */ }
      }
    });
  }

  function cancelPendingChartInit() {
    panelChartInitToken += 1;
    if (panelChartTimer) {
      clearTimeout(panelChartTimer);
      panelChartTimer = null;
    }
    if (panelChartIdle && window.cancelIdleCallback) {
      cancelIdleCallback(panelChartIdle);
      panelChartIdle = null;
    }
  }

  function schedulePanelCharts(panelEl) {
    if (!panelEl) return;
    cancelPendingChartInit();
    resizePanelCharts(panelEl);

    var pending = panelEl.querySelectorAll('.chart[data-chart]:not([data-initialized="1"])');
    if (!pending.length) return;

    var token = panelChartInitToken;
    function initBatch(startIdx) {
      if (token !== panelChartInitToken) return;
      var end = Math.min(startIdx + 2, pending.length);
      var batch = [];
      for (var i = startIdx; i < end; i++) batch.push(initChartElement(pending[i]));
      Promise.all(batch).then(function () {
        if (token !== panelChartInitToken) return;
        resizePanelCharts(panelEl);
        if (end < pending.length) {
          if (window.requestIdleCallback) {
            panelChartIdle = requestIdleCallback(function () { initBatch(end); }, { timeout: 400 });
          } else {
            panelChartTimer = setTimeout(function () { initBatch(end); }, 16);
          }
        }
      });
    }

    initChartElement(pending[0]).then(function () {
      if (token !== panelChartInitToken) return;
      resizePanelCharts(panelEl);
      if (pending.length > 1) initBatch(1);
    });
  }

  function initChartsInPanel(panelEl) {
    schedulePanelCharts(panelEl);
    initLuaMemoryCharts(panelEl);
  }

  async function initLuaMemoryCharts(root) {
    var payload = window.luaMemoryPayload;
    if (!payload || !payload.curves || !payload.curves.length) return;
    var scope = root || document;
    var nodes = scope.querySelectorAll('.lua-curve-chart[data-lua-curve]:not([data-initialized="1"])');
    if (!nodes.length) return;
    var echarts = await loadEcharts();
    nodes.forEach(function (el) {
      var label = el.dataset.luaCurve;
      var curve = payload.curves.find(function (item) { return item.label === label; });
      if (!curve || !curve.frames || !curve.frames.length) {
        el.innerHTML = '<div class="chart-loading">暂无数据</div>';
        el.dataset.initialized = '1';
        return;
      }
      el.dataset.initialized = '1';
      el.innerHTML = '';
      var chart = echarts.init(el);
      chart.setOption({
        animation: false,
        grid: { left: 42, right: 10, top: 10, bottom: 22 },
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'category', data: curve.frames.map(String), boundaryGap: false },
        yAxis: { type: 'value', name: curve.unit || '' },
        series: [{ type: 'line', data: curve.values, showSymbol: false, lineStyle: { width: 1.5 } }]
      });
      chartInstances.push(chart);
      window.addEventListener('resize', function () { chart.resize(); });
    });
  }

  function activatePanel(panelId, options) {
    options = options || {};
    if (!options.force && panelId === currentPanelId) return;

    var nextPanel = panelElements[panelId];
    if (!nextPanel) return;

    if (activePanelEl && activePanelEl !== nextPanel) {
      activePanelEl.classList.remove('active');
    }
    nextPanel.classList.add('active');
    activePanelEl = nextPanel;
    currentPanelId = panelId;

    sidebarNavLinks.forEach(function (link) {
      var href = (link.getAttribute('href') || '').replace(/^#/, '');
      var linkPanel = href.indexOf('module-time/') === 0 ? 'module-time' : href.split('/')[0];
      link.classList.toggle('active', linkPanel === panelId && !href.match(/module-time\/.+/));
    });

    var crumb = document.getElementById('breadcrumbPanel');
    if (crumb) crumb.textContent = panelTitles[panelId] || panelId;

    if (!options.skipCharts) {
      schedulePanelCharts(nextPanel);
    }

    if (!options.skipScroll) {
      window.scrollTo(0, 0);
    }
  }

  function initSceneManagement() {
    document.querySelectorAll('.scene-detail-btn').forEach(function (btn) {
      btn.onclick = function () {
        var row = btn.closest('.scene-row');
        if (!row) return;
        var start = parseInt(row.dataset.start, 10);
        if (!isNaN(start)) selectSampleFrame(start);
      };
    });
  }

  function initSidebarNavigation() {
    document.querySelectorAll('.sidebar-group-title').forEach(function (btn) {
      btn.onclick = function () {
        var group = btn.closest('.sidebar-group');
        if (!group) return;
        var expanded = btn.getAttribute('aria-expanded') !== 'false';
        btn.setAttribute('aria-expanded', expanded ? 'false' : 'true');
        group.classList.toggle('collapsed', expanded);
      };
    });

    document.querySelectorAll('.sidebar-menu a[href^="#"]').forEach(function (link) {
      link.onclick = function (event) {
        event.preventDefault();
        var href = link.getAttribute('href') || '';
        if (location.hash !== href) {
          location.hash = href;
        } else {
          var panelId = href.replace('#', '').split('/')[0];
          if (href.indexOf('module-time/') === 0) panelId = 'module-time';
          activatePanel(panelId);
        }
      };
    });

    function onHashChange() {
      var panelId = resolvePanelFromHash();
      activatePanel(panelId);
      if (panelId !== 'module-time') return;
      var moduleKey = parseModuleHash();
      if (moduleKey && window.moduleDetails && moduleDetails[moduleKey]) {
        showModuleDetail(moduleKey, { skipPanel: true });
      } else {
        showModuleOverview({ skipPanel: true });
      }
    }

    window.addEventListener('hashchange', onHashChange);
    if (!location.hash) {
      location.hash = '#brief';
    }
    onHashChange();
  }

  document.addEventListener('DOMContentLoaded', function () {
    cacheNavigationElements();
    loadEcharts();
    initCapturePanel();
    initSidebarNavigation();
    initSceneManagement();
    initLazyCharts();
    initDiagnosis();
    initFuncTable();
    initLogFilters();
    initBriefFilter();
    initThreadTabs();
    initJankFuncTabs();
    initTextTabs('luaMemoryTabs', 'text-tab', 'lua-tab-panel', 'data-lua-tab', 'data-lua-panel');
    initTextTabs('memResourceTabs', 'text-tab', 'res-tab-panel', 'data-res-tab', 'data-res-panel');
    initScopeToolbar();
    initMetricDragSort();
    initFuncSearch();
    initModuleNavigation();
    initIcicleCharts();
    initReportDownload();
    initTableExports();
    initResourceEventDetails();
  });
})();
