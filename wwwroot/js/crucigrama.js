// Crucigrama compartido: los dos rellenan la misma rejilla. Una sola llamada (Sync) manda el cursor y las
// letras pendientes y trae el estado; cada 1 s si el otro está dentro, cada 3 s si no, nada en segundo plano.
(() => {
  const $ = id => document.getElementById(id);
  const D = JSON.parse($('cr-datos').textContent);
  const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
  const lee = (k, d) => { try { return JSON.parse(localStorage.getItem(k) || 'null') ?? d; } catch { return d; } };
  const guarda = (k, v) => { try { localStorage.setItem(k, JSON.stringify(v)); } catch { } };
  const norm = s => (s || '').toUpperCase().normalize('NFD').replace(/Ñ/g, 'Ñ').replace(/[̀-ͯ]/g, '').replace(/[^A-ZÑ]/g, '');
  const key = (f, c) => f + ',' + c;
  const celdasDe = d => Array.from({ length: d.largo }, (_, i) => d.dir === 'H' ? [d.fila, d.col + i] : [d.fila + i, d.col]);

  const sol = {}, nums = {};
  D.defs.forEach(d => { celdasDe(d).forEach(([f, c], i) => sol[key(f, c)] = d.sol[i]); nums[key(d.fila, d.col)] = d.num; });
  const orden = [...D.defs.filter(d => d.dir === 'H'), ...D.defs.filter(d => d.dir === 'V')];

  // Estado compartido (lo manda el servidor): letras k → [letra, mía 1/0]
  const est = { letras: {}, ayudas: [], resuelto: false, rendido: false, otro: null, cargado: false };
  const pendientes = {}, recientes = {};   // lo escrito aquí y aún no confirmado
  let activa = null, def = null, mal = new Set();
  const letra = k => est.letras[k]?.[0] || '';

  // ---------- Rejilla
  const rej = $('cr-rejilla'), celdaEl = {};
  for (let f = 0; f < D.filas; f++) for (let c = 0; c < D.cols; c++) {
    const k = key(f, c), el = document.createElement('div');
    if (!(k in sol)) { el.className = 'cr-v'; rej.appendChild(el); continue; }
    el.className = 'cr-c'; el.innerHTML = (nums[k] ? `<i>${nums[k]}</i>` : '') + '<b></b>';
    el.addEventListener('click', () => tocaCelda(f, c));
    celdaEl[k] = el; rej.appendChild(el);
  }
  // ---------- Definiciones
  const defsEl = $('cr-defs'), defEl = new Map();
  ['H', 'V'].forEach(dir => {
    const grupo = D.defs.filter(d => d.dir === dir); if (!grupo.length) return;
    const h = document.createElement('h3'); h.textContent = dir === 'H' ? 'Horizontales →' : 'Verticales ↓'; defsEl.appendChild(h);
    grupo.forEach(d => {
      const p = document.createElement('button'); p.type = 'button'; p.className = 'cr-def';
      p.innerHTML = `<b>${d.num}</b><span>${d.pista}</span>`;
      p.addEventListener('click', () => { elegir(d, primeraVacia(d)); window.scrollTo({ top: 0, behavior: 'smooth' }); });
      defsEl.appendChild(p); defEl.set(d, p);
    });
  });

  const tec = document.createElement('input');
  tec.className = 'teclado-oculto'; tec.autocapitalize = 'characters'; tec.autocomplete = 'off'; tec.spellcheck = false; tec.setAttribute('aria-label', 'Letra');
  document.body.appendChild(tec);
  const foco = () => { if (!est.resuelto) tec.focus({ preventScroll: true }); };

  const defsEn = (f, c) => D.defs.filter(d => celdasDe(d).some(([a, b]) => a === f && b === c));
  const primeraVacia = d => celdasDe(d).find(([f, c]) => !letra(key(f, c))) || celdasDe(d)[0];
  const llena = d => celdasDe(d).every(([f, c]) => letra(key(f, c)));

  function elegir(d, celda) { def = d; activa = celda; pinta(); foco(); pronto(); }

  function tocaCelda(f, c) {
    const ds = defsEn(f, c);
    if (activa && activa[0] === f && activa[1] === c && ds.length > 1) def = ds.find(d => d !== def);
    else if (!def || !ds.includes(def)) def = ds.find(d => d.dir === (def?.dir || 'H')) || ds[0];
    activa = [f, c]; pinta(); foco(); pronto();
  }

  function pinta() {
    const enDef = new Set(def ? celdasDe(def).map(([f, c]) => key(f, c)) : []);
    const o = est.otro, oDef = o?.k ? defsEn(...o.k.split(',').map(Number)).find(d => d.dir === o.dir) : null;
    const enOtro = new Set(oDef ? celdasDe(oDef).map(([f, c]) => key(f, c)) : []);
    for (const [k, el] of Object.entries(celdaEl)) {
      el.querySelector('b').textContent = letra(k);
      el.classList.toggle('activa', !!activa && key(...activa) === k);
      el.classList.toggle('en-def', enDef.has(k));
      el.classList.toggle('mal', mal.has(k));
      el.classList.toggle('ayuda', est.ayudas.includes(k));
      el.classList.toggle('del-otro', est.letras[k]?.[1] === 0);
      el.classList.toggle('otro-def', enOtro.has(k));
      el.classList.toggle('otro-cursor', o?.k === k);
    }
    defEl.forEach((el, d) => { el.classList.toggle('activa', d === def); el.classList.toggle('llena', llena(d)); el.classList.toggle('otro', d === oDef); });
    $('cr-pista-txt').innerHTML = def ? `<b>${def.num} ${def.dir === 'H' ? '→' : '↓'}</b> ${def.pista} <span>(${def.largo})</span>` : (est.resuelto ? '¡Resuelto! 🎉' : 'Toca una casilla para empezar');
    $('cr-progreso').textContent = `${D.defs.filter(llena).length}/${D.defs.length} completas`;
    const chip = $('cr-otro'); chip.hidden = !o; if (o) chip.textContent = `✏️ ${o.nombre} está aquí`;
    document.body.classList.toggle('cr-resuelto', est.resuelto);
    $('cr-hecho').hidden = !est.resuelto;
    if (est.resuelto) {
      const mias = Object.values(est.letras).filter(x => x[1] === 1).length, total = Object.keys(sol).length;
      $('cr-hecho-txt').textContent = est.rendido ? 'Bueno… con un empujoncito. Mañana, otro.'
        : mias === total ? 'Todo tuyo, sin ayuda de nadie. ¡Qué cabeza!'
        : mias === 0 ? 'Lo ha resuelto el otro… ¡pero mira qué bien ha quedado!'
        : `Entre los dos: ${mias} letras tuyas y ${total - mias} del otro.` + (est.ayudas.length ? ` Con ${est.ayudas.length} de ayuda.` : '');
    }
    ['cr-letra', 'cr-comprobar', 'cr-rendirse'].forEach(id => $(id).disabled = est.resuelto || !est.cargado);
  }

  function siguienteDef(paso) {
    const i = orden.indexOf(def);
    for (let n = 1; n <= orden.length; n++) {
      const d = orden[(i + paso * n + orden.length * 2) % orden.length];
      if (paso < 0 || !llena(d)) return d;
    }
    return orden[(i + paso + orden.length) % orden.length];
  }

  function avanza() {
    const cs = celdasDe(def), i = cs.findIndex(([f, c]) => f === activa[0] && c === activa[1]);
    const resto = cs.slice(i + 1).find(([f, c]) => !letra(key(f, c)));
    if (resto) activa = resto;
    else if (!llena(def)) activa = primeraVacia(def);
    else { const d = siguienteDef(1); def = d; activa = primeraVacia(d); }
  }

  function pon(k, l) {
    if (l) est.letras[k] = [l, 1]; else delete est.letras[k];
    pendientes[k] = l; recientes[k] = Date.now(); mal.delete(k);
  }

  function escribe(ch) {
    if (!activa || est.resuelto) return;
    pon(key(...activa), ch); avanza(); pinta(); pronto();
  }

  function borra() {
    if (!activa || est.resuelto) return;
    const k = key(...activa);
    if (letra(k)) pon(k, '');
    else {
      const cs = celdasDe(def), i = cs.findIndex(([f, c]) => f === activa[0] && c === activa[1]);
      if (i > 0) { activa = cs[i - 1]; pon(key(...activa), ''); }
    }
    pinta(); pronto();
  }

  tec.addEventListener('input', () => { const ch = norm(tec.value).slice(-1); tec.value = ''; if (ch) escribe(ch); });
  tec.addEventListener('keydown', e => {
    if (e.key === 'Backspace') { e.preventDefault(); borra(); }
    else if (e.key === 'Enter') { e.preventDefault(); if (def) { const d = siguienteDef(1); elegir(d, primeraVacia(d)); } }
  });

  // ---------- Sincronización
  let enCurso = false, tSync = null, extra = {};
  async function sync() {
    if (enCurso) return;
    enCurso = true; clearTimeout(tSync);
    const envio = { ...pendientes }, ex = extra; extra = {};
    try {
      const r = await fetch(location.pathname + '?handler=Sync', {
        method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ semilla: D.semilla, k: activa ? key(...activa) : null, dir: def?.dir || null, letras: envio, ...ex }),
      });
      if (!r.ok) throw 0;
      const s = await r.json();
      for (const [k, l] of Object.entries(envio)) if (pendientes[k] === l) delete pendientes[k];
      const ahora = Date.now(), locales = {};
      for (const k of Object.keys(recientes)) {
        if (ahora - recientes[k] > 2500 && !(k in pendientes)) delete recientes[k];
        else locales[k] = est.letras[k];
      }
      const yaResuelto = est.resuelto;
      est.letras = s.letras; est.ayudas = s.ayudas; est.resuelto = s.resuelto; est.rendido = s.rendido; est.otro = s.otro; est.cargado = true;
      for (const [k, v] of Object.entries(locales)) { if (v) est.letras[k] = v; else delete est.letras[k]; }
      if (est.resuelto) { def = null; activa = null; tec.blur(); }
      pinta();
      if (est.resuelto && !yaResuelto) resuelto(ahora);
    } catch { extra = { ...ex, ...extra }; }
    finally { enCurso = false; programa(); }
  }
  function programa(ms) {
    clearTimeout(tSync);
    if (document.hidden) return;
    tSync = setTimeout(sync, ms ?? (Object.keys(pendientes).length ? 200 : est.resuelto ? 15000 : est.otro ? 1000 : 3000));
  }
  const pronto = () => programa(150);
  document.addEventListener('visibilitychange', () => { if (!document.hidden) sync(); });

  function resuelto() {
    const contados = lee('cruci-contados', []);
    if (est.rendido || contados.includes(D.semilla)) return;
    contados.push(D.semilla); guarda('cruci-contados', contados.slice(-60));
    const st = lee('cruci-stats', { hechos: 0 }); st.hechos++; guarda('cruci-stats', st);
    const cs = Object.values(celdaEl);
    cs.forEach((el, i) => { el.style.animationDelay = (i * 18) + 'ms'; el.classList.add('ola'); });
    setTimeout(() => cs.forEach(el => { el.classList.remove('ola'); el.style.animationDelay = ''; }), cs.length * 18 + 700);
    setTimeout(() => $('cr-hecho').scrollIntoView({ behavior: 'smooth', block: 'center' }), 250);
  }

  // ---------- Botones
  $('cr-ant').addEventListener('click', () => { const d = def ? siguienteDef(-1) : orden[orden.length - 1]; elegir(d, primeraVacia(d)); });
  $('cr-sig').addEventListener('click', () => { const d = def ? siguienteDef(1) : orden[0]; elegir(d, primeraVacia(d)); });
  $('cr-letra').addEventListener('click', () => {
    const malK = k => letra(k) !== sol[k];
    let k = activa && malK(key(...activa)) ? key(...activa) : null;
    if (!k && def) { const c = celdasDe(def).find(([f, c]) => malK(key(f, c))); if (c) k = key(...c); }
    if (!k) { const ks = Object.keys(sol).filter(malK); k = ks[Math.floor(Math.random() * ks.length)]; }
    if (!k) return;
    est.letras[k] = [sol[k], 1]; est.ayudas.push(k); recientes[k] = Date.now(); mal.delete(k);
    const [f, c] = k.split(',').map(Number); activa = [f, c];
    if (!def || !defsEn(f, c).includes(def)) def = defsEn(f, c)[0];
    extra.ayuda = true; pinta(); foco(); sync();
  });
  $('cr-comprobar').addEventListener('click', () => {
    mal = new Set(Object.keys(est.letras).filter(k => letra(k) !== sol[k]));
    pinta();
    const b = $('cr-comprobar'), txt = b.textContent;
    b.textContent = mal.size ? `${mal.size} por revisar` : (Object.keys(est.letras).length ? 'Todo bien de momento ✓' : 'Aún no hay letras');
    setTimeout(() => b.textContent = txt, 1800);
    foco();
  });
  $('cr-rendirse').addEventListener('click', () => {
    if (!confirm('¿Ver la solución? Se rellena todo, también para el otro.')) return;
    extra.rendirse = true; sync();
  });

  pinta(); sync();
})();
