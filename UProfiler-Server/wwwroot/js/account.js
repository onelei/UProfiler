(function () {
  var menu = document.querySelector('.user-menu');
  if (menu) {
    var trigger = menu.querySelector('.user-trigger');
    if (trigger) {
      trigger.addEventListener('click', function (e) {
        e.stopPropagation();
        menu.classList.toggle('open');
      });
      document.addEventListener('click', function () {
        menu.classList.remove('open');
      });
    }
  }

  var form = document.getElementById('profileForm');
  if (!form) return;

  form.addEventListener('submit', function (e) {
    e.preventDefault();
    var status = document.getElementById('saveStatus');
    if (status) {
      status.textContent = '保存中…';
      status.classList.remove('error');
    }

    var formData = new FormData(form);
    fetch(form.action, {
      method: 'POST',
      body: formData,
      credentials: 'same-origin'
    })
      .then(function (res) { return res.json(); })
      .then(function (data) {
        if (!status) return;
        if (data.ok) {
          status.textContent = '已保存';
          status.classList.remove('error');
        } else {
          status.textContent = data.message || '保存失败';
          status.classList.add('error');
        }
      })
      .catch(function () {
        if (status) {
          status.textContent = '网络错误，请重试';
          status.classList.add('error');
        }
      });
  });
})();
