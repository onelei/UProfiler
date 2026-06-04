(function () {
  'use strict';

  const chartInstances = [];
  let echartsPromise = null;

  function loadEcharts() {
    if (window.echarts) return Promise.resolve(window.echarts);
    if (echartsPromise) return echartsPromise;
    echartsPromise = new Promise(function (resolve, reject) {
      var script = document.createElement('script');
      script.src = '/js/vendor/echarts.min.js?v=2';
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

  function buildOption(type) {
    if (!window.chartPayload) return null;
    var p = window.chartPayload;
    if (type === 'fps') {
      return {
        tooltip: { trigger: 'axis' },
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.fps.x, name: '帧' },
        yAxis: { type: 'value', name: 'FPS' },
        dataZoom: baseZoom(),
        series: [{ name: 'FPS', type: 'line', smooth: true, showSymbol: false, data: p.fps.y, itemStyle: { color: '#1677ff' } }]
      };
    }
    if (type === 'memory') {
      return {
        tooltip: { trigger: 'axis' },
        legend: { top: 0 },
        grid: { left: 50, right: 20, top: 40, bottom: 50 },
        xAxis: { type: 'category', data: p.memory.x, name: '帧' },
        yAxis: { type: 'value', name: 'MB' },
        dataZoom: baseZoom(),
        series: [
          { name: 'MonoUsed', type: 'line', smooth: true, showSymbol: false, data: p.memory.monoUsed, itemStyle: { color: '#722ed1' } },
          { name: 'TotalAllocated', type: 'line', smooth: true, showSymbol: false, data: p.memory.totalAllocated, itemStyle: { color: '#1677ff' } },
          { name: 'UnityReserved', type: 'line', smooth: true, showSymbol: false, data: p.memory.unityReserved, itemStyle: { color: '#13c2c2' } }
        ]
      };
    }
    if (type === 'render') {
      return {
        tooltip: { trigger: 'axis' },
        legend: { top: 0 },
        grid: { left: 50, right: 20, top: 40, bottom: 50 },
        xAxis: { type: 'category', data: p.render.x, name: '帧' },
        yAxis: { type: 'value' },
        dataZoom: baseZoom(),
        series: [
          { name: 'SetPassCall', type: 'line', smooth: true, showSymbol: false, data: p.render.setPass, itemStyle: { color: '#ff4d4f' } },
          { name: 'DrawCall', type: 'line', smooth: true, showSymbol: false, data: p.render.drawCall, itemStyle: { color: '#1677ff' } },
          { name: '顶点', type: 'line', smooth: true, showSymbol: false, data: p.render.vertices, itemStyle: { color: '#fa8c16' } },
          { name: '三角面', type: 'line', smooth: true, showSymbol: false, data: p.render.triangles, itemStyle: { color: '#52c41a' } }
        ]
      };
    }
    if (type === 'power') {
      return {
        tooltip: { trigger: 'axis' },
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
        tooltip: { trigger: 'axis' },
        grid: { left: 50, right: 20, top: 30, bottom: 50 },
        xAxis: { type: 'category', data: p.pss.x, name: '帧' },
        yAxis: { type: 'value', name: 'MB' },
        dataZoom: baseZoom(),
        series: [{ name: 'PSS', type: 'line', smooth: true, showSymbol: false, areaStyle: {}, data: p.pss.y, itemStyle: { color: '#1677ff' } }]
      };
    }
    return null;
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
    chart.setOption(option);
    chartInstances.push(chart);
    var resize = function () { chart.resize(); };
    window.addEventListener('resize', resize);
  }

  function initLazyCharts() {
    var nodes = document.querySelectorAll('.chart[data-chart]');
    if (!nodes.length) return;
    if (!('IntersectionObserver' in window)) {
      nodes.forEach(function (el) { initChartElement(el); });
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

  function initLogViewer() {
    var dataEl = document.getElementById('logData');
    var box = document.getElementById('logBox');
    var moreBtn = document.getElementById('logMore');
    if (!dataEl || !box) return;
    var lines = JSON.parse(dataEl.textContent || '[]');
    var chunk = 80;
    var shown = 0;

    function lineClass(line) {
      if (/(\[Error\]|\[Exception\]|\[Assert\])/i.test(line)) return 'log-line log-error';
      if (/\[Warning\]/i.test(line)) return 'log-line log-warning';
      return 'log-line log-info';
    }

    function appendLines() {
      var next = lines.slice(shown, shown + chunk);
      box.insertAdjacentHTML('beforeend', next.map(function (line) {
        return '<div class="' + lineClass(line) + '"></div>'.replace('></div>', '>' + escapeHtml(line) + '</div>');
      }).join(''));
      shown += next.length;
      if (moreBtn) moreBtn.style.display = shown >= lines.length ? 'none' : 'inline-block';
    }

    function escapeHtml(text) {
      return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    appendLines();
    if (moreBtn) moreBtn.onclick = appendLines;
  }

  window.addEventListener('beforeunload', function () {
    chartInstances.forEach(function (chart) {
      try { chart.dispose(); } catch (e) { /* ignore */ }
    });
  });

  document.addEventListener('DOMContentLoaded', function () {
    initLazyCharts();
    initDiagnosis();
    initFuncTable();
    initLogViewer();
  });
})();
