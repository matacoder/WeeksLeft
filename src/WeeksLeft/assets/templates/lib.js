/* Shared helpers for WeeksLeft templates.
   A template defines render(data) and calls WL.boot(render). */
(function (global) {
  'use strict';

  /* ------------------------------------------------------------------ i18n */

  var STRINGS = {
    ru: {
      /* neutral framing */
      left: 'осталось недель', leftD: 'осталось дней',
      leftM: 'осталось месяцев', leftY: 'осталось лет',
      /* positive framing — the default */
      ahead: 'недель впереди', aheadD: 'дней впереди',
      aheadM: 'месяцев впереди', aheadY: 'лет впереди',
      stillAhead: 'ещё впереди',

      lived: 'прожито недель', livedD: 'прожито дней',
      livedM: 'прожито месяцев', livedY: 'прожито лет',

      of: 'из', done: 'пройдено', until: 'ориентир',
      week: 'неделя', year: 'год', age: 'возраст',
      motto: 'CARPE DIEM', mottoNeutral: 'MEMENTO MORI',
      thisWeek: 'эта неделя', decade: 'десятилетие',
      months: ['янв', 'фев', 'мар', 'апр', 'май', 'июн', 'июл', 'авг', 'сен', 'окт', 'ноя', 'дек']
    },
    en: {
      left: 'weeks left', leftD: 'days left',
      leftM: 'months left', leftY: 'years left',
      ahead: 'weeks ahead', aheadD: 'days ahead',
      aheadM: 'months ahead', aheadY: 'years ahead',
      stillAhead: 'still ahead',

      lived: 'weeks lived', livedD: 'days lived',
      livedM: 'months lived', livedY: 'years lived',

      of: 'of', done: 'complete', until: 'projected',
      week: 'week', year: 'year', age: 'age',
      motto: 'CARPE DIEM', mottoNeutral: 'MEMENTO MORI',
      thisWeek: 'this week', decade: 'decade',
      months: ['jan', 'feb', 'mar', 'apr', 'may', 'jun', 'jul', 'aug', 'sep', 'oct', 'nov', 'dec']
    }
  };

  function t(lang) { return STRINGS[lang] || STRINGS.en; }

  function fmt(n, lang) {
    try { return Number(n).toLocaleString(lang === 'ru' ? 'ru-RU' : 'en-US'); }
    catch (e) { return String(n); }
  }

  /* --------------------------------------------------------------- colours */

  function hexToRgb(hex) {
    var h = String(hex || '#888').replace('#', '');
    if (h.length === 3) h = h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
    var v = parseInt(h, 16);
    return [(v >> 16) & 255, (v >> 8) & 255, v & 255];
  }

  function rgba(hex, a) {
    var c = hexToRgb(hex);
    return 'rgba(' + c[0] + ',' + c[1] + ',' + c[2] + ',' + a + ')';
  }

  function mix(a, b, k) {
    var x = hexToRgb(a), y = hexToRgb(b);
    var m = function (i) { return Math.round(x[i] + (y[i] - x[i]) * k); };
    return 'rgb(' + m(0) + ',' + m(1) + ',' + m(2) + ')';
  }

  /* Keeps the accent readable against the background. */
  function ensureContrast(accent, isDark) {
    var c = hexToRgb(accent);
    var lum = (0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]) / 255;
    var f = 1;
    if (isDark && lum < 0.30) f = 0.30 / Math.max(lum, 0.04);
    if (!isDark && lum > 0.72) f = 0.72 / lum;
    if (f === 1) return accent;
    var m = function (x) { return Math.max(0, Math.min(255, Math.round(x * f))); };
    return '#' + [m(c[0]), m(c[1]), m(c[2])].map(function (x) {
      return ('0' + x.toString(16)).slice(-2);
    }).join('');
  }

  function palette(d) {
    var dark = d.theme !== 'light';
    var accent = ensureContrast(d.accent || '#E8552D', dark);
    /* Pure black on dark: OLED panels switch those pixels off entirely. */
    var bg = dark ? '#000000' : '#F7F6F2';
    var fg = dark ? '#F2F2F0' : '#121214';
    return {
      dark: dark, bg: bg, fg: fg, accent: accent,
      bg2: dark ? '#0A0A0C' : '#EBE9E3',
      dim: function (a) { return rgba(fg, a); },
      acc: function (a) { return rgba(accent, a); },
      toward: function (k) { return mix(bg, accent, k); }
    };
  }

  /* ---------------------------------------------------------------- layout */

  /* Safe area in px for the current viewport. */
  function box(d) {
    var W = global.innerWidth, H = global.innerHeight;
    var left = Math.max(W * 0.055, W * (d.safeLeftPct || 0) / 100);
    var bottom = Math.max(H * 0.075, H * (d.safeBottomPct || 0) / 100);
    var right = W * 0.055, top = H * 0.075;
    return { W: W, H: H, left: left, right: right, top: top, bottom: bottom,
             w: W - left - right, h: H - top - bottom,
             u: Math.min(W / 1920, H / 1080) };
  }

  /* The headline triplet for the configured granularity and tone. */
  function counts(d) {
    var positive = d.tone !== 'neutral';
    var pick = function (aheadKey, leftKey) { return positive ? aheadKey : leftKey; };
    switch (d.granularity) {
      case 'days':
        return { lived: d.daysLived, left: d.daysLeft, total: d.daysLived + d.daysLeft,
                 kLived: 'livedD', kLeft: pick('aheadD', 'leftD') };
      case 'months':
        return { lived: d.monthsLived, left: Math.max(0, d.monthsTotal - d.monthsLived),
                 total: d.monthsTotal, kLived: 'livedM', kLeft: pick('aheadM', 'leftM') };
      case 'years':
        return { lived: Math.floor(d.ageYears), left: Math.max(0, Math.round(d.yearsLeft)),
                 total: Math.round(d.yearsTotal), kLived: 'livedY', kLeft: pick('aheadY', 'leftY') };
      default:
        return { lived: d.weeksLived, left: d.weeksLeft, total: d.weeksTotal,
                 kLived: 'lived', kLeft: pick('ahead', 'left') };
    }
  }

  function motto(d) {
    var T = t(d.lang);
    return d.tone === 'neutral' ? T.mottoNeutral : T.motto;
  }

  /* --------------------------------------------------------------- canvas */

  function canvas(b, p) {
    var cv = document.getElementById('c');
    cv.width = b.W; cv.height = b.H;
    var g = cv.getContext('2d');
    g.fillStyle = p.bg;
    g.fillRect(0, 0, b.W, b.H);
    return g;
  }

  /*
    Cell geometry for a rows x cols grid inside a max box.
    opt: { cols, rows, gap (share of cell), maxW, maxH, spread }

    A life grid is 52 wide and ~75 tall, so on a 16:9 screen the height always binds
    and the block ends up a narrow column. `spread` lets the columns breathe sideways
    up to N times the vertical step, filling the width without stretching the marks.
  */
  function geom(d, b, opt) {
    opt = opt || {};
    var cols = opt.cols || d.gridCols || 52;
    var rows = opt.rows || Math.max(1, d.gridRows);
    var gapR = opt.gap == null ? 0.36 : opt.gap;
    var maxW = opt.maxW == null ? b.w : opt.maxW;
    var maxH = opt.maxH == null ? b.h : opt.maxH;

    var spread = opt.spread || 1;
    var stepY = maxH / rows;
    var stepXmax = maxW / cols;
    var stepX;

    if (spread <= 1) {
      stepY = stepX = Math.min(stepY, stepXmax);       // square lattice
    } else {
      stepX = Math.min(stepXmax, stepY * spread);
      if (stepX < stepY) stepY = stepX;                // width binds after all
    }

    /* The mark is sized off the tighter axis, so it never merges into stripes. */
    var cell = Math.min(stepX, stepY) / (1 + gapR);

    return {
      cols: cols, rows: rows, cell: cell, gap: cell * gapR,
      stepX: stepX, stepY: stepY, step: stepY,
      w: stepX * (cols - 1) + cell,
      h: stepY * (rows - 1) + cell
    };
  }

  /*
    A 52-column life grid is roughly 52 x 75, which never fills a 16:9 frame. This picks
    how many weeks to put on a row — 52, 104, 156 ... — so the block covers as much of
    the available box as possible. Columns stay a whole number of years wide, so a
    column still means "the same time of year".
  */
  function wrapGeom(d, b, opt) {
    opt = opt || {};
    var total = Math.max(1, d.weeksTotal);
    var candidates = opt.cols ? [opt.cols] : [52, 104, 156, 208, 260, 312];
    var best = null;

    for (var i = 0; i < candidates.length; i++) {
      var o = {};
      for (var k in opt) o[k] = opt[k];
      o.cols = candidates[i];
      o.rows = Math.ceil(total / candidates[i]);
      var gm = geom(d, b, o);
      var area = gm.w * gm.h;
      if (!best || area > best.area) best = { area: area, gm: gm };
    }

    best.gm.total = total;
    return best.gm;
  }

  /* Walks the weeks in linear order for a wrapped grid. */
  function eachWeek(d, gm, ox, oy, fn) {
    var ms = {};
    (d.milestones || []).forEach(function (m) { ms[m.week] = m; });

    var total = gm.total || d.weeksTotal;
    for (var i = 0; i < total; i++) {
      var row = Math.floor(i / gm.cols), col = i % gm.cols;
      fn(ox + col * gm.stepX, oy + row * gm.stepY, {
        row: row, col: col, index: i,
        state: i < d.weeksLived ? 'past' : i === d.weeksLived ? 'now' : 'future',
        milestone: ms[i],
        progress: total > 1 ? i / (total - 1) : 0
      });
    }
  }

  /*
    Walks every cell of the life grid.
    fn(x, y, info) where info = { row, col, state: 'past'|'now'|'future',
                                 index, milestone, progress }
    x,y is the top-left corner of the cell.
  */
  function eachCell(d, gm, ox, oy, fn) {
    var ms = {};
    (d.milestones || []).forEach(function (m) { ms[m.row + ':' + m.col] = m; });

    for (var row = 0; row < gm.rows; row++) {
      for (var col = 0; col < gm.cols; col++) {
        var state = row < d.curRow || (row === d.curRow && col < d.curCol) ? 'past'
                  : (row === d.curRow && col === d.curCol) ? 'now' : 'future';
        fn(ox + col * gm.stepX, oy + row * gm.stepY, {
          row: row, col: col, state: state,
          index: row * gm.cols + col,
          milestone: ms[row + ':' + col],
          progress: gm.rows > 1 ? row / (gm.rows - 1) : 0
        });
      }
    }
  }

  function dot(g, x, y, size, fill) {
    g.beginPath();
    g.arc(x + size / 2, y + size / 2, size / 2, 0, 6.2832);
    g.fillStyle = fill;
    g.fill();
  }

  function square(g, x, y, size, fill, radius) {
    g.fillStyle = fill;
    if (!radius) { g.fillRect(x, y, size, size); return; }
    var r = Math.min(radius, size / 2);
    g.beginPath();
    g.moveTo(x + r, y);
    g.arcTo(x + size, y, x + size, y + size, r);
    g.arcTo(x + size, y + size, x, y + size, r);
    g.arcTo(x, y + size, x, y, r);
    g.arcTo(x, y, x + size, y, r);
    g.closePath();
    g.fill();
  }

  /* ------------------------------------------------------------------- dom */

  /* Returns an add(html, css) helper bound to a cleared #ui layer. */
  function layer() {
    var host = document.getElementById('ui');
    host.innerHTML = '';
    return function add(html, css) {
      var el = document.createElement('div');
      el.className = 'abs ' + (css.cls || '');
      el.innerHTML = html;
      for (var k in css) if (k !== 'cls') el.style[k] = css[k];
      host.appendChild(el);
      return el;
    };
  }

  /* Standard top strip: place / projection on the left, motto or percent right. */
  function header(add, d, p, b, rightHtml) {
    var T = t(d.lang);
    var meta = [d.countryName];
    if (d.showEndDate) meta.push(T.until + ' ' + d.endYear);

    add(meta.join(' &nbsp;&middot;&nbsp; '), {
      cls: 'eyebrow nw', left: b.left + 'px', top: b.top + 'px',
      fontSize: (16 * b.u) + 'px', letterSpacing: (3 * b.u) + 'px', color: p.dim(0.45)
    });

    if (rightHtml !== null) {
      add(rightHtml === undefined ? motto(d) : rightHtml, {
        cls: 'eyebrow nw', right: b.right + 'px', top: b.top + 'px', textAlign: 'right',
        fontSize: (16 * b.u) + 'px', letterSpacing: (4 * b.u) + 'px', color: p.dim(0.3)
      });
    }
  }

  /* ------------------------------------------------------------------ boot */

  function signalReady() {
    var fire = function () {
      requestAnimationFrame(function () {
        requestAnimationFrame(function () {
          try {
            if (global.chrome && global.chrome.webview) global.chrome.webview.postMessage('ready');
          } catch (e) { /* preview iframe, no host */ }
        });
      });
    };
    if (document.fonts && document.fonts.ready) document.fonts.ready.then(fire, fire);
    else fire();
  }

  function boot(render) {
    var last = null;
    var run = function (d) {
      last = d;
      document.documentElement.style.background = palette(d).bg;
      try { render(d); } catch (e) { console.error('[WeeksLeft] render failed', e); }
      signalReady();
    };

    /* The settings window posts data into the preview iframes. */
    global.addEventListener('message', function (ev) {
      if (ev.data && ev.data.type === 'weeks-data') run(ev.data.data);
    });

    var tmr = null;
    global.addEventListener('resize', function () {
      if (!last) return;
      clearTimeout(tmr);
      tmr = setTimeout(function () { try { render(last); } catch (e) { } }, 60);
    });

    var start = function () { if (global.WEEKS_DATA) run(global.WEEKS_DATA); };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
    else start();
  }

  global.WL = {
    t: t, fmt: fmt, palette: palette, box: box, counts: counts, motto: motto,
    rgba: rgba, mix: mix, canvas: canvas,
    geom: geom, wrapGeom: wrapGeom, eachCell: eachCell, eachWeek: eachWeek,
    dot: dot, square: square, layer: layer, header: header, boot: boot
  };
})(window);
