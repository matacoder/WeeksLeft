/* Settings UI for WeeksLeft. Talks to the C# host over chrome.webview messages. */
(function () {
  'use strict';

  var I18N = {
    ru: {
      sub: 'Обои с картой прожитых недель. Обновляются сами, в фоне ничего не висит.',
      who: 'Кто ты', birth: 'Дата рождения', sex: 'Пол', country: 'Страна',
      male: 'Муж', female: 'Жен', avg: 'Среднее',
      auto: 'Определять автоматически',
      model: 'Ожидаемая продолжительность', modelLabel: 'Модель расчёта',
      mRemaining: 'Остаток по текущему возрасту',
      mBirth: 'Ожидаемая при рождении',
      mCustom: 'Свой целевой возраст',
      customAge: 'Целевой возраст',
      hintRemaining: 'Правильный вариант для взрослого: ты уже пережил детскую и раннюю смертность, поэтому ожидаемый финальный возраст выше, чем «продолжительность жизни при рождении».',
      hintBirth: 'Классическая цифра из статистики. Для взрослого занижает результат — он уже пережил ту смертность, которая в неё заложена.',
      hintCustom: 'Просто считаем от возраста, который ты сам назначил.',
      design: 'Дизайн', rotation: 'Смена дизайна',
      rFixed: 'Фиксированный', rWeekly: 'Каждую неделю', rMonthly: 'Каждый месяц',
      galleryFixed: 'Выбери дизайн — он будет стоять, пока не поменяешь.',
      galleryRotating: 'Выбранный дизайн встанет сейчас и продержится до конца недели, дальше пойдёт следующий по кругу.',
      tone: 'Тон', tonePositive: 'Позитивный', toneNeutral: 'Нейтральный',
      toneHintPos: 'Считаем то, что впереди: «1814 недель впереди», а не «осталось».',
      toneHintNeu: 'Нейтральные формулировки: «осталось недель», memento mori.',
      install: 'Установка в систему',
      installHint: 'Копирует приложение в %LOCALAPPDATA%\\Programs\\WeeksLeft, добавляет ярлык в меню «Пуск», ставит задачу в планировщик и регистрирует запись в «Приложениях и возможностях». Права администратора не нужны.',
      installBtn: 'Установить в систему', uninstallBtn: 'Удалить',
      installing: 'Устанавливаю…', installedOk: 'Установлено',
      installedAt: 'Установлено в ', notInstalled: 'Не установлено — работает из текущей папки.',
      uninstalledOk: 'Удалено', installFail: 'Не удалось: ',
      theme: 'Тема', tAuto: 'Как в Windows', tDark: 'Тёмная', tLight: 'Светлая',
      accent: 'Акцент', accentSys: 'системный',
      unit: 'Единицы', uWeeks: 'Недели', uMonths: 'Месяцы', uDays: 'Дни', uYears: 'Годы',
      lang: 'Язык обоев', lAuto: 'Авто', lRu: 'Рус', lEn: 'Eng',
      content: 'Что показывать',
      showNumbers: 'Показывать числа',
      showEnd: 'Показывать предполагаемый год окончания',
      endHint: 'Выключено по умолчанию — многим некомфортно видеть эту дату каждый день.',
      custom: 'Своя строка',
      safeLeft: 'Отступ под иконки', safeBottom: 'Отступ под панель задач',
      ms: 'Вехи жизни',
      msHint: 'Отмечаются точками на сетке недель. Дата + подпись + цвет.',
      msAdd: '+ Добавить веху', msLabel: 'Событие',
      behave: 'Поведение',
      monitors: 'Мониторы',
      monAll: 'Одинаково на всех', monPrimary: 'Только основной', monPer: 'Свой дизайн на каждый',
      update: 'Обновление',
      uWeekly: 'Раз в неделю', uDaily: 'Каждый день', uLogon: 'Только при входе',
      autostart: 'Обновлять автоматически (задача в планировщике)',
      history: 'Хранить архив прошлых обоев',
      behaveHint: 'Резидентного процесса нет. Задача запускается по расписанию, за ~30 мс проверяет, изменилось ли что-то, и выходит.',
      openFolder: 'Папка с обоями', openLog: 'Лог',
      apply: 'Применить сейчас', save: 'Сохранить',
      applying: 'Рендерю…', applied: 'Готово — обои установлены', failed: 'Ошибка, смотри лог',
      saved: 'Сохранено', needBirth: 'Укажи дату рождения',
      sLived: 'прожито недель', sLeft: 'осталось недель', sAhead: 'впереди недель', sPct: 'пройдено',
      sFinal: 'ожидаемый финал', sCountry: 'страна', years: 'лет'
    },
    en: {
      sub: 'A wallpaper of the weeks you have lived. Updates itself, nothing runs in the background.',
      who: 'Who you are', birth: 'Date of birth', sex: 'Sex', country: 'Country',
      male: 'Male', female: 'Female', avg: 'Average',
      auto: 'Detect automatically',
      model: 'Life expectancy', modelLabel: 'Model',
      mRemaining: 'Remaining at current age',
      mBirth: 'At birth',
      mCustom: 'Custom target age',
      customAge: 'Target age',
      hintRemaining: 'The right choice for an adult: you already survived infant and early-adult mortality, so your expected final age is higher than life expectancy at birth.',
      hintBirth: 'The classic headline number. It understates things for an adult, who already outlived the mortality baked into it.',
      hintCustom: 'Counts down from an age you pick yourself.',
      design: 'Design', rotation: 'Rotation',
      rFixed: 'Fixed', rWeekly: 'Every week', rMonthly: 'Every month',
      galleryFixed: 'Pick a design; it stays until you change it.',
      galleryRotating: 'The design you pick goes up now and holds until the end of the week, then the cycle moves on.',
      tone: 'Tone', tonePositive: 'Positive', toneNeutral: 'Neutral',
      toneHintPos: 'Counts what is still coming: "1,814 weeks ahead" rather than "left".',
      toneHintNeu: 'Plain wording: "weeks left", memento mori.',
      install: 'Install',
      installHint: 'Copies the app to %LOCALAPPDATA%\\Programs\\WeeksLeft, adds a Start menu shortcut, registers the scheduled task, and lists it in Apps & features. No administrator rights needed.',
      installBtn: 'Install to system', uninstallBtn: 'Remove',
      installing: 'Installing…', installedOk: 'Installed',
      installedAt: 'Installed in ', notInstalled: 'Not installed — running from the current folder.',
      uninstalledOk: 'Removed', installFail: 'Failed: ',
      theme: 'Theme', tAuto: 'Follow Windows', tDark: 'Dark', tLight: 'Light',
      accent: 'Accent', accentSys: 'system',
      unit: 'Units', uWeeks: 'Weeks', uMonths: 'Months', uDays: 'Days', uYears: 'Years',
      lang: 'Wallpaper language', lAuto: 'Auto', lRu: 'Rus', lEn: 'Eng',
      content: 'What to show',
      showNumbers: 'Show numbers',
      showEnd: 'Show projected final year',
      endHint: 'Off by default — plenty of people would rather not see that date every day.',
      custom: 'Custom line',
      safeLeft: 'Desktop icon margin', safeBottom: 'Taskbar margin',
      ms: 'Life milestones',
      msHint: 'Marked as coloured dots on the week grid. Date + label + colour.',
      msAdd: '+ Add milestone', msLabel: 'Event',
      behave: 'Behaviour',
      monitors: 'Monitors',
      monAll: 'Same on all', monPrimary: 'Primary only', monPer: 'Different per monitor',
      update: 'Update',
      uWeekly: 'Weekly', uDaily: 'Daily', uLogon: 'On logon only',
      autostart: 'Update automatically (scheduled task)',
      history: 'Keep an archive of past wallpapers',
      behaveHint: 'No resident process. The task wakes on schedule, spends ~30 ms checking whether anything changed, and exits.',
      openFolder: 'Wallpaper folder', openLog: 'Log',
      apply: 'Apply now', save: 'Save',
      applying: 'Rendering…', applied: 'Done — wallpaper set', failed: 'Failed, check the log',
      saved: 'Saved', needBirth: 'Set your date of birth',
      sLived: 'weeks lived', sLeft: 'weeks left', sAhead: 'weeks ahead', sPct: 'complete',
      sFinal: 'projected final age', sCountry: 'country', years: 'yrs'
    }
  };

  var L = I18N.ru, S = null, cfg = null, lastPreview = null, tmr = null;
  var gotState = false, demo = false;
  var $ = function (id) { return document.getElementById(id); };

  /* Fallback used when the host never answers — and when settings.html is opened
     directly in a browser to work on the UI. Keeps the window from ever being blank. */
  var DEMO = {
    lang: 'ru', detectedCountry: 'RU', systemAccent: '#E8552D', systemDark: true,
    taskInstalled: false,
    countries: [{ Iso2: 'RU', NameRu: 'Россия', NameEn: 'Russia', E0Male: 68, E0Female: 78 },
                { Iso2: 'US', NameRu: 'США', NameEn: 'United States', E0Male: 75.8, E0Female: 81.1 }],
    templates: [
                { Id: '01-grid', NameRu: 'Сетка недель', NameEn: 'Life in Weeks' },
                { Id: '02-minimal', NameRu: 'Минимализм', NameEn: 'Minimal' },
                { Id: '03-rings', NameRu: 'Кольца', NameEn: 'Rings' },
                { Id: '04-squares', NameRu: 'Квадраты', NameEn: 'Squares' },
                { Id: '05-heatmap', NameRu: 'Тепловая карта', NameEn: 'Heatmap' },
                { Id: '06-fullbleed', NameRu: 'Во весь экран', NameEn: 'Full Bleed' },
                { Id: '07-months', NameRu: 'Месяцы', NameEn: 'Months' },
                { Id: '08-years', NameRu: 'Годы', NameEn: 'Years' },
                { Id: '09-timeline', NameRu: 'Лента', NameEn: 'Timeline' },
                { Id: '10-bars', NameRu: 'Полосы по годам', NameEn: 'Year Bars' },
                { Id: '11-spiral', NameRu: 'Спираль', NameEn: 'Spiral' },
                { Id: '12-terminal', NameRu: 'Терминал', NameEn: 'Terminal' },
                { Id: '13-blueprint', NameRu: 'Чертёж', NameEn: 'Blueprint' },
                { Id: '14-cards', NameRu: 'Карточки', NameEn: 'Cards' },
                { Id: '15-typo', NameRu: 'Типографика', NameEn: 'Typography' },
                { Id: '16-horizon', NameRu: 'Горизонт', NameEn: 'Horizon' },
                { Id: '17-thisyear', NameRu: 'Этот год', NameEn: 'This Year' },
                { Id: '18-mosaic', NameRu: 'Мозаика', NameEn: 'Mosaic' },
                { Id: '19-orbit', NameRu: 'Орбиты', NameEn: 'Orbit' },
                { Id: '20-pixel', NameRu: 'Пиксели', NameEn: 'Pixel' }
    ],
    monitors: [{ Width: 3840, Height: 2160, IsPrimary: true }],
    installed: false, runningFromInstall: false, installDir: '',
    currentTemplate: '01-grid',
    config: {
      BirthDate: '1988-05-12', Sex: 'male', Country: null, Model: 'remaining',
      CustomTargetAge: 90, Template: '01-grid', Rotation: 'weekly',
      Tone: 'positive', Theme: 'dark',
      Accent: 'system', Granularity: 'weeks', Lang: 'auto', ShowNumbers: true,
      ShowEndDate: false, CustomText: '', SafeLeftPercent: 0, SafeBottomPercent: 0,
      Milestones: [{ Date: '2005-09-01', Label: 'Университет', Color: '#4C9EEB' }],
      MonitorMode: 'all-same', UpdateMode: 'weekly', AutoStart: true, KeepHistory: false
    }
  };

  var DEMO_PREVIEW = {
    birthDate: '1988-05-12', lang: 'ru', ageYears: 38.3, daysLived: 13983, daysLeft: 12700,
    monthsLived: 459, monthsTotal: 876, weeksLived: 1997, weeksTotal: 3811, weeksLeft: 1814,
    yearsTotal: 73, yearsLeft: 34.7, percent: 52.4, gridRows: 74, gridCols: 52,
    curRow: 38, curCol: 14, endDate: '2061-06-01', endYear: 2061,
    country: 'RU', countryName: 'Россия', sex: 'male', model: 'remaining',
    granularity: 'weeks', tone: 'positive', showNumbers: true, showEndDate: false, customText: '',
    theme: 'dark', accent: '#E8552D', safeLeftPct: 0, safeBottomPct: 0,
    width: 1920, height: 1080,
    milestones: [{ date: '2005-09-01', label: 'Университет', color: '#4C9EEB', row: 17, col: 15 }]
  };

  function post(o) {
    try { if (window.chrome && window.chrome.webview) window.chrome.webview.postMessage(o); }
    catch (e) { console.error(e); }
  }

  post({ cmd: 'mark', what: 'script parsed' });
  document.addEventListener('DOMContentLoaded', function () {
    post({ cmd: 'mark', what: 'dom ready' });
  });
  window.addEventListener('load', function () { post({ cmd: 'mark', what: 'window load' }); });

  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', function (e) {
      var m = e.data;
      if (!m) return;
      if (m.type === 'state') onState(m.state);
      else if (m.type === 'preview') onPreview(m.data);
      else if (m.type === 'saved') status(L.saved);
      else if (m.type === 'applied') status(m.ok ? L.applied : L.failed);
      else if (m.type === 'installed' || m.type === 'uninstalled') {
        if (m.state) { S = m.state; paintInstallState(); }
        status(m.ok ? (m.type === 'installed' ? L.installedOk : L.uninstalledOk)
                    : L.installFail + m.message);
      }
      else if (m.type === 'error') status(m.message);
    });
  }

  function status(t) {
    $('status').textContent = t;
    clearTimeout(status._t);
    status._t = setTimeout(function () { $('status').textContent = ''; }, 4000);
  }

  /* ---------------- segmented controls ---------------- */

  function seg(id, options, get, set) {
    var host = $(id);
    host.innerHTML = '';
    options.forEach(function (o) {
      var b = document.createElement('button');
      b.textContent = o[1];
      b.onclick = function () { set(o[0]); paintSegs(); changed(); };
      b.dataset.v = o[0];
      host.appendChild(b);
    });
    host._get = get;
  }

  function paintSegs() {
    ['sex', 'theme', 'granularity', 'lang', 'tone'].forEach(function (id) {
      var host = $(id);
      if (!host || !host._get) return;
      var v = host._get();
      Array.prototype.forEach.call(host.children, function (b) {
        b.classList.toggle('on', b.dataset.v === v);
      });
    });
  }

  function fillSelect(id, options, value) {
    var s = $(id);
    s.innerHTML = '';
    options.forEach(function (o) {
      var el = document.createElement('option');
      el.value = o[0]; el.textContent = o[1];
      s.appendChild(el);
    });
    s.value = value;
  }

  /* ---------------- state ---------------- */

  function onState(state) {
    gotState = true;
    S = state;
    cfg = state.config;
    L = I18N[state.lang] || I18N.en;
    /* Show what is actually on the desktop, which under rotation is not cfg.Template. */
    if (state.currentTemplate) cfg.Template = state.currentTemplate;
    document.documentElement.lang = state.lang;
    if (state.systemAccent) document.documentElement.style.setProperty('--accent', state.systemAccent);
    post({ cmd: 'mark', what: 'state received' });
    buildUi();
    post({ cmd: 'mark', what: 'ui built' });
    changed(true);

    requestAnimationFrame(function () {
      requestAnimationFrame(function () { post({ cmd: 'mark', what: 'first paint' }); });
    });
  }

  function buildUi() {
    var txt = {
      'subttl': L.sub, 'h-who': L.who, 'l-birth': L.birth, 'l-sex': L.sex, 'l-country': L.country,
      'h-model': L.model, 'l-model': L.modelLabel, 'l-customage': L.customAge,
      'h-design': L.design, 'l-rotation': L.rotation, 'l-theme': L.theme,
      'l-accent': L.accent, 'l-accentsys': L.accentSys, 'l-unit': L.unit, 'l-lang': L.lang,
      'h-content': L.content, 'l-tone': L.tone,
      'l-shownumbers': L.showNumbers, 'l-showend': L.showEnd,
      'h-install': L.install, 'installHint': L.installHint,
      'install': L.installBtn, 'uninstall': L.uninstallBtn,
      'endHint': L.endHint, 'l-custom': L.custom,
      'l-safeleft': L.safeLeft, 'l-safebottom': L.safeBottom,
      'h-ms': L.ms, 'msHint': L.msHint, 'msAdd': L.msAdd,
      'h-behave': L.behave, 'l-monitors': L.monitors, 'l-update': L.update,
      'l-autostart': L.autostart, 'l-history': L.history, 'behaveHint': L.behaveHint,
      'openFolder': L.openFolder, 'openLog': L.openLog,
      'apply': L.apply, 'save': L.save
    };
    for (var k in txt) if ($(k)) $(k).textContent = txt[k];

    $('birthDate').value = cfg.BirthDate || '';
    $('birthDate').max = new Date().toISOString().slice(0, 10);
    $('birthDate').oninput = function () { cfg.BirthDate = this.value || null; changed(); };

    seg('sex', [['male', L.male], ['female', L.female], ['average', L.avg]],
        function () { return cfg.Sex; }, function (v) { cfg.Sex = v; });

    var det = S.countries.filter(function (c) { return c.Iso2 === S.detectedCountry; })[0];
    var cname = function (c) { return S.lang === 'ru' ? c.NameRu : c.NameEn; };
    var opts = [['auto', L.auto + (det ? ' — ' + cname(det) : '')]];
    S.countries.forEach(function (c) {
      opts.push([c.Iso2, cname(c) + '  ·  ' + c.E0Male + ' / ' + c.E0Female]);
    });
    fillSelect('country', opts, cfg.Country || 'auto');
    $('country').onchange = function () {
      cfg.Country = this.value === 'auto' ? null : this.value; changed();
    };

    fillSelect('model', [['remaining', L.mRemaining], ['birth', L.mBirth], ['custom', L.mCustom]], cfg.Model);
    $('model').onchange = function () { cfg.Model = this.value; syncModel(); changed(); };
    $('customTargetAge').value = cfg.CustomTargetAge;
    $('customTargetAge').oninput = function () {
      cfg.CustomTargetAge = Number(this.value) || 90; changed();
    };
    syncModel();

    buildGallery();

    fillSelect('rotation', [['weekly', L.rWeekly], ['monthly', L.rMonthly], ['fixed', L.rFixed]], cfg.Rotation);
    $('rotation').onchange = function () {
      cfg.Rotation = this.value;
      paintCards();
      changed();
    };

    seg('tone', [['positive', L.tonePositive], ['neutral', L.toneNeutral]],
        function () { return cfg.Tone; },
        function (v) { cfg.Tone = v; $('toneHint').textContent = v === 'neutral' ? L.toneHintNeu : L.toneHintPos; });
    $('toneHint').textContent = cfg.Tone === 'neutral' ? L.toneHintNeu : L.toneHintPos;

    seg('theme', [['auto', L.tAuto], ['dark', L.tDark], ['light', L.tLight]],
        function () { return cfg.Theme; }, function (v) { cfg.Theme = v; });

    var sysAccent = cfg.Accent === 'system';
    $('accentSystem').checked = sysAccent;
    $('accentColor').value = sysAccent ? (S.systemAccent || '#E8552D') : cfg.Accent;
    $('accentColor').disabled = sysAccent;
    $('accentSystem').onchange = function () {
      cfg.Accent = this.checked ? 'system' : $('accentColor').value;
      $('accentColor').disabled = this.checked;
      changed();
    };
    $('accentColor').oninput = function () {
      if ($('accentSystem').checked) return;
      cfg.Accent = this.value; changed();
    };

    seg('granularity', [['weeks', L.uWeeks], ['months', L.uMonths], ['days', L.uDays], ['years', L.uYears]],
        function () { return cfg.Granularity; }, function (v) { cfg.Granularity = v; });

    seg('lang', [['auto', L.lAuto], ['ru', L.lRu], ['en', L.lEn]],
        function () { return cfg.Lang; }, function (v) { cfg.Lang = v; });

    bindCheck('showNumbers', 'ShowNumbers');
    bindCheck('showEndDate', 'ShowEndDate');
    bindCheck('autoStart', 'AutoStart');
    bindCheck('keepHistory', 'KeepHistory');

    $('customText').value = cfg.CustomText || '';
    $('customText').oninput = function () { cfg.CustomText = this.value; changed(); };

    bindRange('safeLeftPercent', 'SafeLeftPercent', 'safeLeftVal');
    bindRange('safeBottomPercent', 'SafeBottomPercent', 'safeBottomVal');

    buildMilestones();
    $('msAdd').onclick = function () {
      cfg.Milestones = cfg.Milestones || [];
      cfg.Milestones.push({ Date: new Date().toISOString().slice(0, 10), Label: L.msLabel, Color: '#4C9EEB' });
      buildMilestones(); changed();
    };

    fillSelect('monitorMode',
      [['all-same', L.monAll], ['primary-only', L.monPrimary], ['per-monitor', L.monPer]], cfg.MonitorMode);
    $('monitorMode').onchange = function () { cfg.MonitorMode = this.value; changed(); };

    fillSelect('updateMode',
      [['weekly', L.uWeekly], ['daily', L.uDaily], ['logon', L.uLogon]], cfg.UpdateMode);
    $('updateMode').onchange = function () { cfg.UpdateMode = this.value; changed(); };

    paintInstallState();
    $('install').onclick = function () {
      status(L.installing);
      post({ cmd: 'install', config: cfg });
    };
    $('uninstall').onclick = function () { post({ cmd: 'uninstall' }); };

    $('openFolder').onclick = function () { post({ cmd: 'openFolder' }); };
    $('openLog').onclick = function () { post({ cmd: 'openLog' }); };
    $('save').onclick = function () { post({ cmd: 'save', config: cfg }); };
    $('apply').onclick = function () {
      if (!cfg.BirthDate) { status(L.needBirth); return; }
      status(L.applying);
      post({ cmd: 'apply', config: cfg });
    };

    paintSegs();
  }

  function paintInstallState() {
    $('installState').textContent = S.installed ? L.installedAt + S.installDir : L.notInstalled;
    $('uninstall').disabled = !S.installed;
  }

  function bindCheck(id, key) {
    var el = $(id);
    el.checked = !!cfg[key];
    el.onchange = function () { cfg[key] = this.checked; changed(); };
  }

  function bindRange(id, key, outId) {
    var el = $(id);
    el.value = cfg[key] || 0;
    $(outId).textContent = el.value + '%';
    el.oninput = function () {
      cfg[key] = Number(this.value);
      $(outId).textContent = this.value + '%';
      changed();
    };
  }

  function syncModel() {
    $('customAgeRow').style.display = cfg.Model === 'custom' ? '' : 'none';
    $('modelHint').textContent =
      cfg.Model === 'birth' ? L.hintBirth : cfg.Model === 'custom' ? L.hintCustom : L.hintRemaining;
  }

  /* ---------------- milestones ---------------- */

  function buildMilestones() {
    var list = $('msList');
    list.innerHTML = '';
    (cfg.Milestones || []).forEach(function (m, i) {
      var row = document.createElement('div');
      row.className = 'ms';

      var d = document.createElement('input');
      d.type = 'date'; d.value = m.Date || '';
      d.oninput = function () { m.Date = this.value; changed(); };

      var lab = document.createElement('input');
      lab.type = 'text'; lab.value = m.Label || ''; lab.maxLength = 30;
      lab.oninput = function () { m.Label = this.value; changed(); };

      var col = document.createElement('input');
      col.type = 'color'; col.value = m.Color || '#4C9EEB';
      col.oninput = function () { m.Color = this.value; changed(); };

      var x = document.createElement('button');
      x.className = 'x'; x.textContent = '×';
      x.onclick = function () { cfg.Milestones.splice(i, 1); buildMilestones(); changed(); };

      row.appendChild(d); row.appendChild(lab); row.appendChild(col); row.appendChild(x);
      list.appendChild(row);
    });
  }

  /* ---------------- gallery + preview ---------------- */

  function paintCards() {
    Array.prototype.forEach.call($('gallery').children, function (card) {
      card.classList.toggle('on', card.dataset.id === cfg.Template);
    });
    $('galleryHint').textContent =
      cfg.Rotation === 'fixed' ? L.galleryFixed : L.galleryRotating;
  }

  var thumbWatcher = null;

  /* Thumbnails load only once their card scrolls into view — twenty templates rendering
     at once on open is what made the window feel slow. */
  function watchThumb(card) {
    if (!window.IntersectionObserver) { loadThumb(card); return; }
    if (!thumbWatcher) {
      thumbWatcher = new IntersectionObserver(function (entries) {
        entries.forEach(function (e) {
          if (!e.isIntersecting) return;
          loadThumb(e.target);
          thumbWatcher.unobserve(e.target);
        });
      }, { root: $('left'), rootMargin: '300px' });
    }
    thumbWatcher.observe(card);
  }

  function loadThumb(card) {
    var f = card.querySelector('iframe');
    if (!f || f.src) return;
    f.onload = function () { f.dataset.live = '1'; pushPreview(); };
    f.src = 'templates/' + card.dataset.id + '.html';
  }

  function buildGallery() {
    var g = $('gallery');
    if (thumbWatcher) { thumbWatcher.disconnect(); thumbWatcher = null; }
    g.innerHTML = '';

    S.templates.forEach(function (t) {
      var card = document.createElement('div');
      card.className = 'card';
      card.dataset.id = t.Id;
      card.innerHTML =
        '<div class="thumb"><iframe data-tpl="' + t.Id + '"></iframe></div>' +
        '<div class="name">' + (S.lang === 'ru' ? t.NameRu : t.NameEn) + '</div>';

      card.onclick = function () {
        cfg.Template = t.Id;
        paintCards();
        loadPreview();
        changed();
      };

      g.appendChild(card);
      watchThumb(card);
    });

    paintCards();
    scaleThumbs();
    loadPreview();
  }

  /*
    The big preview is laid out at 1920 wide with the real monitor's aspect ratio, so what
    you see is a true reduction of the wallpaper. Thumbnails use a much smaller internal
    size: at 1920 each of the twenty cards would allocate a 1920x1080 canvas, which is
    hundreds of megabytes of raster for pictures shown 200 px wide.
  */
  var PREVIEW_W = 1920, THUMB_W = 640;

  function aspect() {
    var m = S && S.monitors && S.monitors.filter(function (x) { return x.IsPrimary; })[0];
    return m ? m.Height / m.Width : 1080 / 1920;
  }

  function scaleThumbs() {
    var a = aspect();
    var ph = Math.round(PREVIEW_W * a), th = Math.round(THUMB_W * a);
    var ratio = PREVIEW_W + ' / ' + ph;

    Array.prototype.forEach.call(document.querySelectorAll('.card .thumb'), function (box) {
      box.style.aspectRatio = ratio;
      var f = box.querySelector('iframe');
      if (!f) return;
      f.style.width = THUMB_W + 'px';
      f.style.height = th + 'px';
      f.style.transform = 'scale(' + (box.clientWidth / THUMB_W) + ')';
    });

    var wrap = $('previewWrap'), pv = $('preview');
    wrap.style.aspectRatio = ratio;
    pv.style.width = PREVIEW_W + 'px';
    pv.style.height = ph + 'px';
    var s = Math.min(wrap.clientWidth / PREVIEW_W, wrap.clientHeight / ph);
    pv.style.transform = 'translate(' + ((wrap.clientWidth - PREVIEW_W * s) / 2) + 'px,' +
                         ((wrap.clientHeight - ph * s) / 2) + 'px) scale(' + s + ')';
  }

  function loadPreview() {
    var pv = $('preview');
    var want = 'templates/' + cfg.Template + '.html';
    if (pv.dataset.tpl !== cfg.Template) {
      pv.dataset.tpl = cfg.Template;
      pv.onload = function () { pushPreview(); };
      pv.src = want;
    } else {
      pushPreview();
    }
  }

  function pushPreview() {
    if (!lastPreview) return;
    var msg = { type: 'weeks-data', data: lastPreview };
    var pv = $('preview');
    try { pv.contentWindow.postMessage(msg, '*'); } catch (e) { }
    /* only the thumbnails that have actually loaded */
    Array.prototype.forEach.call(document.querySelectorAll('.card iframe[data-live]'), function (f) {
      try { f.contentWindow.postMessage(msg, '*'); } catch (e) { }
    });
  }

  function onPreview(data) {
    lastPreview = data;
    pushPreview();
    renderStats(data);
  }

  function renderStats(d) {
    var box = $('stats');
    if (!d) { box.innerHTML = '<span>' + L.needBirth + '</span>'; return; }
    var n = function (x) { return Number(x).toLocaleString(S.lang === 'ru' ? 'ru-RU' : 'en-US'); };
    box.innerHTML =
      '<div><b>' + n(d.weeksLived) + '</b>' + L.sLived + '</div>' +
      '<div><b>' + n(d.weeksLeft) + '</b>' + (cfg.Tone === 'neutral' ? L.sLeft : L.sAhead) + '</div>' +
      '<div><b>' + Math.round(d.percent) + '%</b>' + L.sPct + '</div>' +
      '<div><b>' + d.yearsTotal + ' ' + L.years + '</b>' + L.sFinal + '</div>' +
      '<div><b>' + d.countryName + '</b>' + L.sCountry + '</div>';
  }

  /* Settings are written to disk as you go, so closing the window never loses anything. */
  var persistTmr = null;

  function persist(now) {
    if (demo) return;
    clearTimeout(persistTmr);
    if (now) post({ cmd: 'persist', config: cfg });
    else persistTmr = setTimeout(function () { post({ cmd: 'persist', config: cfg }); }, 600);
  }

  /* Any change: ask the host to recompute, then repaint the live previews. */
  function changed(immediate) {
    clearTimeout(tmr);
    persist(false);
    var go = function () {
      if (!cfg.BirthDate) { renderStats(null); return; }
      if (demo) { onPreview(DEMO_PREVIEW); return; }
      post({ cmd: 'preview', config: cfg });
    };
    if (immediate) go(); else tmr = setTimeout(go, 180);
  }

  function start() {
    /* settings.html?demo=en opens the whole UI standalone in a browser, for design work
       and for taking screenshots without a running host. */
    var forced = (location.search.match(/[?&]demo=(ru|en)/) || [])[1];
    if (forced) {
      demo = true;
      DEMO.lang = forced;
      DEMO.config.Lang = forced;
      DEMO_PREVIEW.lang = forced;
      DEMO_PREVIEW.countryName = forced === 'ru' ? 'Россия' : 'United States';
      DEMO_PREVIEW.milestones = [{
        date: '2008-09-01', row: 17, col: 15, color: '#4C9EEB',
        label: forced === 'ru' ? 'Университет' : 'University'
      }];
      onState(DEMO);
      return;
    }

    post({ cmd: 'init' });
    setTimeout(function () {
      if (gotState) return;
      demo = true;
      onState(DEMO);
    }, 1200);
  }

  /* Closing the window or clicking away flushes the pending write immediately. */
  window.addEventListener('blur', function () { if (cfg) persist(true); });
  window.addEventListener('beforeunload', function () { if (cfg) persist(true); });
  document.addEventListener('visibilitychange', function () {
    if (document.visibilityState === 'hidden' && cfg) persist(true);
  });

  window.addEventListener('resize', scaleThumbs);
  if (document.readyState === 'complete') start();
  else window.addEventListener('load', start);
})();
