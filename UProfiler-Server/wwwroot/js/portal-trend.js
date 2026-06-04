(function () {
  'use strict';

  if (!window.trendData) return;

  var chartInstance = null;
  var echartsPromise = null;

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

  async function renderTrend() {
    var dom = document.getElementById('trendChart');
    if (!dom || dom.dataset.initialized === '1') return;
    dom.dataset.initialized = '1';
    var echarts = await loadEcharts();
    chartInstance = echarts.init(dom);
    chartInstance.setOption({
      tooltip: { trigger: 'axis' },
      legend: { data: ['FPS均值', 'DrawCall/10'] },
      grid: { left: 50, right: 40, top: 40, bottom: 60 },
      xAxis: { type: 'category', data: trendData.labels },
      yAxis: [{ type: 'value', name: 'FPS' }, { type: 'value', name: 'DrawCall/10' }],
      dataZoom: [{ type: 'inside' }, { type: 'slider' }],
      series: [
        { name: 'FPS均值', type: 'line', smooth: true, showSymbol: false, data: trendData.fps, itemStyle: { color: '#1677ff' } },
        { name: 'DrawCall/10', type: 'line', smooth: true, showSymbol: false, yAxisIndex: 1, data: trendData.dc, itemStyle: { color: '#722ed1' } }
      ]
    });
    window.addEventListener('resize', function () { chartInstance && chartInstance.resize(); });
  }

  document.addEventListener('DOMContentLoaded', function () {
    var dom = document.getElementById('trendChart');
    if (!dom) return;
    if ('IntersectionObserver' in window) {
      var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            renderTrend();
            observer.disconnect();
          }
        });
      }, { rootMargin: '80px' });
      observer.observe(dom);
    } else {
      renderTrend();
    }
  });

  window.addEventListener('beforeunload', function () {
    if (chartInstance) {
      try { chartInstance.dispose(); } catch (e) { /* ignore */ }
    }
  });
})();
