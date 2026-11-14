// Cifras: seis números, un objetivo (101-999), + − × ÷ con resultados enteros positivos y 45 segundos.
(() => {
  const $ = id => document.getElementById(id);
  const SEG = 45, OPS = { '+': '+', '-': '−', '*': '×', '/': '÷' };
  const elObj = $('cf-objetivo'), elFichas = $('cf-fichas'), elPasos = $('cf-pasos'), elFinal = $('cf-final'),
        elBarra = $('cf-barra'), elSeg = $('cf-seg'), elInicio = $('cf-inicio'), raiz = $('cf');
  const lee = (k, d) => { try { return JSON.parse(localStorage.getItem(k) || 'null') ?? d; } catch { return d; } };
  const guarda = (k, v) => { try { localStorage.setItem(k, JSON.stringify(v)); } catch { } };
  const zumba = ms => { try { navigator.vibrate && navigator.vibrate(ms); } catch { } };

  let objetivo, fichas, pasos, sel, op, fin, inicio, tick, vistos;

  function nueva() {
    const bolsa = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 25, 50, 75, 100];
    const nums = [];
    for (let i = 0; i < 6; i++) nums.push(bolsa.splice(Math.floor(Math.random() * bolsa.length), 1)[0]);
    objetivo = 101 + Math.floor(Math.random() * 899);
    fichas = nums.map((v, i) => ({ v, usado: false, k: i }));
    vistos = new Set(nums); pasos = []; sel = null; op = null; fin = false;
  }

  function pintaObjetivo(txt) { elObj.innerHTML = String(txt).split('').map(d => `<span>${d}</span>`).join(''); }

  function rodar(cb) {
    raiz.classList.add('rodando');
    let n = 0;
    const t = setInterval(() => {
      pintaObjetivo(String(101 + Math.floor(Math.random() * 899)));
      if (++n > 12) { clearInterval(t); pintaObjetivo(objetivo); raiz.classList.remove('rodando'); elObj.classList.add('fijado'); cb(); }
    }, 55);
  }

  function pinta(nuevaK) {
    elFichas.innerHTML = '';
    fichas.forEach((f, i) => {
      const b = document.createElement('button'); b.type = 'button';
      b.className = 'cf-ficha' + (f.usado ? ' usada' : '') + (sel === i ? ' sel' : '') + (f.v > 100 || f.v === 100 ? ' grande' : '') + (f.k === nuevaK ? ' nueva' : '') + (f.v === objetivo ? ' exacta' : '');
      b.textContent = f.v; b.disabled = f.usado || fin;
      b.style.setProperty('--i', i);
      b.addEventListener('click', () => toca(i));
      elFichas.appendChild(b);
    });
    document.querySelectorAll('#cf-ops button').forEach(b => { b.classList.toggle('sel', b.dataset.op === op); b.disabled = fin; });
    elPasos.innerHTML = pasos.map(p => `<li>${p.a} ${OPS[p.op]} ${p.b} = <b>${p.r}</b></li>`).join('');
    $('cf-deshacer').disabled = fin || !pasos.length;
    $('cf-reiniciar').disabled = fin || !pasos.length;
  }

  function sacude(i, msg) {
    zumba(40);
    const b = elFichas.children[i]; if (b) { b.classList.remove('mal'); void b.offsetWidth; b.classList.add('mal'); }
    if (msg) aviso(msg);
  }

  let tAviso;
  function aviso(msg) {
    let a = document.querySelector('.cf-aviso');
    if (!a) { a = document.createElement('div'); a.className = 'cf-aviso'; raiz.appendChild(a); }
    a.textContent = msg; a.classList.add('ver'); clearTimeout(tAviso); tAviso = setTimeout(() => a.classList.remove('ver'), 1600);
  }

  function toca(i) {
    if (fin || fichas[i].usado) return;
    if (sel === null || (op === null && sel !== i)) { sel = i; pinta(); return; }
    if (sel === i) { sel = null; op = null; pinta(); return; }
    const a = fichas[sel].v, b = fichas[i].v;
    let r;
    if (op === '+') r = a + b;
    else if (op === '*') r = a * b;
    else if (op === '-') { r = a - b; if (r <= 0) return sacude(i, 'Solo valen resultados positivos'); }
    else { if (a % b) return sacude(i, `${a} ÷ ${b} no es exacta`); r = a / b; }
    pasos.push({ a, b, op, r, antes: fichas.map(f => ({ ...f })) });
    const k = Date.now();
    fichas[sel].usado = true; fichas[i] = { v: r, usado: false, k };
    vistos.add(r); sel = null; op = null;
    pinta(k);
    if (r === objetivo) terminar();
  }

  document.querySelectorAll('#cf-ops button').forEach(b => b.addEventListener('click', () => {
    if (fin) return;
    if (sel === null) { aviso('Primero toca un número'); return; }
    op = op === b.dataset.op ? null : b.dataset.op; pinta();
  }));
  $('cf-deshacer').addEventListener('click', () => { const p = pasos.pop(); if (p) { fichas = p.antes; sel = null; op = null; pinta(); } });
  $('cf-reiniciar').addEventListener('click', () => { if (pasos.length) { fichas = pasos[0].antes; pasos = []; sel = null; op = null; pinta(); } });

  function reloj() {
    const t = (performance.now() - inicio) / 1000, queda = Math.max(0, SEG - t);
    elBarra.style.transform = `scaleX(${queda / SEG})`;
    elSeg.textContent = Math.ceil(queda);
    raiz.classList.toggle('apurando', queda <= 10 && queda > 0);
    if (queda <= 0) return terminar();
    tick = requestAnimationFrame(reloj);
  }

  function resolver(nums, obj) {
    let mejor = { d: Infinity, v: null, pasos: [] }; const visto = new Set();
    const rec = (arr, ps) => {
      for (const v of arr) { const d = Math.abs(v - obj); if (d < mejor.d) mejor = { d, v, pasos: ps.slice() }; }
      if (mejor.d === 0) return true;
      const clave = arr.slice().sort((x, y) => x - y).join(',');
      if (visto.has(clave)) return false; visto.add(clave);
      for (let i = 0; i < arr.length; i++) for (let j = i + 1; j < arr.length; j++) {
        const hi = Math.max(arr[i], arr[j]), lo = Math.min(arr[i], arr[j]);
        const resto = arr.filter((_, k) => k !== i && k !== j);
        const cands = [['+', hi + lo]];
        if (lo > 1) cands.push(['*', hi * lo]);
        if (hi > lo && hi - lo !== lo) cands.push(['-', hi - lo]);
        if (lo > 1 && hi % lo === 0 && hi / lo !== lo) cands.push(['/', hi / lo]);
        for (const [o, r] of cands) { ps.push({ a: hi, op: o, b: lo, r }); if (rec([...resto, r], ps)) return true; ps.pop(); }
      }
      return false;
    };
    rec(nums, []);
    // quedarse solo con los pasos que llevan al resultado
    const falta = [mejor.v], usados = [];
    for (let i = mejor.pasos.length - 1; i >= 0; i--) {
      const p = mejor.pasos[i], k = falta.indexOf(p.r);
      if (k >= 0) { falta.splice(k, 1, p.a, p.b); usados.unshift(p); }
    }
    return { ...mejor, pasos: usados };
  }

  function terminar() {
    if (fin) return;
    fin = true; cancelAnimationFrame(tick); raiz.classList.remove('apurando');
    const segs = Math.round((performance.now() - inicio) / 1000);
    let mio = null; vistos.forEach(v => { if (mio === null || Math.abs(v - objetivo) < Math.abs(mio - objetivo)) mio = v; });
    const d = Math.abs(mio - objetivo), exacta = d === 0;
    const st = lee('cifras-stats', { jugadas: 0, exactas: 0, racha: 0, mejorSeg: null });
    st.jugadas++; if (exacta) { st.exactas++; st.racha++; if (st.mejorSeg == null || segs < st.mejorSeg) st.mejorSeg = segs; } else st.racha = 0;
    guarda('cifras-stats', st);
    pinta();
    const titulo = exacta ? `¡Exacto! <small>en ${segs} s</small>` : d <= 5 ? `¡Casi! A ${d} <small>(${mio})</small>` : d <= 10 ? `A ${d} <small>(${mio})</small>` : `Uy… a ${d} <small>(${mio})</small>`;
    const frase = exacta ? (segs <= 15 ? 'Eso ha sido de calculadora humana.' : 'Clavado. Ni el del concurso.') :
                  d <= 10 ? 'En el concurso eso puntúa. Aquí también.' : 'Los números hoy estaban rebeldes.';
    elFinal.innerHTML = `<div class="cf-res ${exacta ? 'ok' : ''}"><h2>${titulo}</h2><p>${frase}</p><div id="cf-sol" class="cf-sol"><span class="suave peq">Buscando la mejor solución…</span></div>
      <button type="button" class="btn primario ancho" id="cf-otra">Otra partida</button></div>`;
    elFinal.hidden = false;
    $('cf-otra').addEventListener('click', empezar);
    if (exacta) confeti();
    pintaRecord();
    setTimeout(() => {
      const s = resolver(fichasIniciales(), objetivo);
      const lista = s.pasos.map(p => `<li>${p.a} ${OPS[p.op]} ${p.b} = <b>${p.r}</b></li>`).join('');
      $('cf-sol').innerHTML = s.d === 0
        ? (exacta ? `<p class="suave peq">Otra forma de llegar:</p><ol>${lista}</ol>` : `<p class="suave peq">Se podía clavar así:</p><ol>${lista}</ol>`)
        : `<p class="suave peq">Imposible clavarlo: lo más cerca era <b>${s.v}</b>${lista ? ':' : '.'}</p>${lista ? `<ol>${lista}</ol>` : ''}`;
    }, 350);
    setTimeout(() => elFinal.scrollIntoView({ behavior: 'smooth', block: 'center' }), 200);
  }

  function fichasIniciales() { return (pasos.length ? pasos[0].antes : fichas).map(f => f.v); }

  function confeti() {
    const cols = ['#b05540', '#e6b85c', '#4f9d69', '#7262bd', '#e8917f'];
    for (let i = 0; i < 36; i++) {
      const s = document.createElement('i'); s.className = 'cf-confeti';
      s.style.cssText = `--x:${(Math.random() * 2 - 1) * 46}vw;--y:${-30 - Math.random() * 50}vh;--r:${Math.random() * 720 - 360}deg;background:${cols[i % cols.length]};left:50%;animation-delay:${Math.random() * .15}s`;
      document.body.appendChild(s); setTimeout(() => s.remove(), 1700);
    }
  }

  function pintaRecord() {
    const st = lee('cifras-stats', null);
    $('cf-record').textContent = st && st.jugadas ? `${st.exactas}/${st.jugadas} exactas` + (st.racha > 1 ? ` · 🔥${st.racha}` : '') : '';
  }

  function empezar() {
    nueva(); elFinal.hidden = true; elInicio.hidden = true; elObj.classList.remove('fijado');
    raiz.classList.add('jugando');
    elBarra.style.transform = 'scaleX(1)'; elSeg.textContent = SEG;
    fin = true; pinta(); fin = false;
    raiz.classList.add('repartiendo');
    rodar(() => { raiz.classList.remove('repartiendo'); raiz.classList.add('reparte'); setTimeout(() => raiz.classList.remove('reparte'), 900); pinta(); inicio = performance.now(); tick = requestAnimationFrame(reloj); });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  pintaObjetivo('???'); pintaRecord();
  $('cf-empezar').addEventListener('click', empezar);
})();
