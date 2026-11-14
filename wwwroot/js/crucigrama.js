// Crucigrama del día: rejilla generada en el servidor; se juega y se guarda en el navegador.
(() => {
  const $ = id => document.getElementById(id);
  const D = JSON.parse($('cr-datos').textContent);
  const CLAVE = 'cruci-' + D.semilla;
  const lee = (k, d) => { try { return JSON.parse(localStorage.getItem(k) || 'null') ?? d; } catch { return d; } };
  const guarda = (k, v) => { try { localStorage.setItem(k, JSON.stringify(v)); } catch { } };
  const norm = s => (s || '').toUpperCase().normalize('NFD').replace(/Ñ/g, 'Ñ').replace(/[̀-ͯ]/g, '').replace(/[^A-ZÑ]/g, '');
  const key = (f, c) => f + ',' + c;
  const celdasDe = d => Array.from({ length: d.largo }, (_, i) => d.dir === 'H' ? [d.fila, d.col + i] : [d.fila + i, d.col]);

  // Solución por celda y número de inicio
  const sol = {}, nums = {};
  D.defs.forEach(d => { celdasDe(d).forEach(([f, c], i) => sol[key(f, c)] = d.sol[i]); nums[key(d.fila, d.col)] = d.num; });
  const orden = [...D.defs.filter(d => d.dir === 'H'), ...D.defs.filter(d => d.dir === 'V')];

  const est = lee(CLAVE, { letras: {}, ayudas: [], resuelto: false, rendido: false });
  let activa = null, def = null, mal = new Set();

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

  // Teclado del móvil: input oculto que recibe las letras
  const tec = document.createElement('input');
  tec.className = 'teclado-oculto'; tec.autocapitalize = 'characters'; tec.autocomplete = 'off'; tec.spellcheck = false; tec.setAttribute('aria-label', 'Letra');
  document.body.appendChild(tec);
  const foco = () => { if (!est.resuelto) tec.focus({ preventScroll: true }); };

  const defsEn = (f, c) => D.defs.filter(d => celdasDe(d).some(([a, b]) => a === f && b === c));
  const primeraVacia = d => celdasDe(d).find(([f, c]) => !est.letras[key(f, c)]) || celdasDe(d)[0];
  const llena = d => celdasDe(d).every(([f, c]) => est.letras[key(f, c)]);

  function elegir(d, celda) { def = d; activa = celda; pinta(); foco(); }

  function tocaCelda(f, c) {
    const ds = defsEn(f, c);
    if (activa && activa[0] === f && activa[1] === c && ds.length > 1) def = ds.find(d => d !== def);
    else if (!def || !ds.includes(def)) def = ds.find(d => d.dir === (def?.dir || 'H')) || ds[0];
    activa = [f, c]; pinta(); foco();
  }

  function pinta() {
    const enDef = new Set(def ? celdasDe(def).map(([f, c]) => key(f, c)) : []);
    for (const [k, el] of Object.entries(celdaEl)) {
      el.querySelector('b').textContent = est.letras[k] || '';
      el.classList.toggle('activa', !!activa && key(...activa) === k);
      el.classList.toggle('en-def', enDef.has(k));
      el.classList.toggle('mal', mal.has(k));
      el.classList.toggle('ayuda', est.ayudas.includes(k));
    }
    defEl.forEach((el, d) => { el.classList.toggle('activa', d === def); el.classList.toggle('llena', llena(d)); });
    $('cr-pista-txt').innerHTML = def ? `<b>${def.num} ${def.dir === 'H' ? '→' : '↓'}</b> ${def.pista} <span>(${def.largo})</span>` : (est.resuelto ? '¡Resuelto! 🎉' : 'Toca una casilla para empezar');
    const hechas = D.defs.filter(llena).length;
    $('cr-progreso').textContent = `${hechas}/${D.defs.length} completas`;
    document.body.classList.toggle('cr-resuelto', est.resuelto);
    $('cr-hecho').hidden = !est.resuelto;
    if (est.resuelto) $('cr-hecho-txt').textContent = est.rendido ? 'Bueno… con un empujoncito. Mañana, otro.' : est.ayudas.length ? `Con ${est.ayudas.length} letra${est.ayudas.length === 1 ? '' : 's'} de ayuda. ¡Muy bien!` : 'Sin ninguna ayuda. ¡Qué cabeza!';
    ['cr-letra', 'cr-comprobar', 'cr-rendirse'].forEach(id => $(id).disabled = est.resuelto);
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
    const resto = cs.slice(i + 1).find(([f, c]) => !est.letras[key(f, c)]);
    if (resto) activa = resto;
    else if (!llena(def)) activa = primeraVacia(def);
    else { const d = siguienteDef(1); def = d; activa = primeraVacia(d); }
  }

  function escribe(ch) {
    if (!activa || est.resuelto) return;
    const k = key(...activa);
    est.letras[k] = ch; mal.delete(k);
    avanza(); guarda(CLAVE, est); comprueba(); pinta();
  }

  function borra() {
    if (!activa || est.resuelto) return;
    const k = key(...activa);
    if (est.letras[k]) delete est.letras[k];
    else {
      const cs = celdasDe(def), i = cs.findIndex(([f, c]) => f === activa[0] && c === activa[1]);
      if (i > 0) { activa = cs[i - 1]; delete est.letras[key(...activa)]; }
    }
    mal.delete(key(...activa)); guarda(CLAVE, est); pinta();
  }

  tec.addEventListener('input', () => { const ch = norm(tec.value).slice(-1); tec.value = ''; if (ch) escribe(ch); });
  tec.addEventListener('keydown', e => {
    if (e.key === 'Backspace') { e.preventDefault(); borra(); }
    else if (e.key === 'Enter') { e.preventDefault(); if (def) { const d = siguienteDef(1); elegir(d, primeraVacia(d)); } }
  });

  function comprueba() {
    if (est.resuelto) return;
    if (Object.keys(sol).every(k => est.letras[k] === sol[k])) {
      est.resuelto = true; def = null; activa = null; tec.blur(); guarda(CLAVE, est);
      if (!est.rendido) {
        const st = lee('cruci-stats', { hechos: 0 }); st.hechos++; guarda('cruci-stats', st);
        fiesta();
      }
      setTimeout(() => $('cr-hecho').scrollIntoView({ behavior: 'smooth', block: 'center' }), 250);
    }
  }

  $('cr-ant').addEventListener('click', () => { const d = def ? siguienteDef(-1) : orden[orden.length - 1]; elegir(d, primeraVacia(d)); });
  $('cr-sig').addEventListener('click', () => { const d = def ? siguienteDef(1) : orden[0]; elegir(d, primeraVacia(d)); });
  $('cr-letra').addEventListener('click', () => {
    let k = activa && est.letras[key(...activa)] !== sol[key(...activa)] ? key(...activa) : null;
    if (!k && def) { const c = celdasDe(def).find(([f, c]) => est.letras[key(f, c)] !== sol[key(f, c)]); if (c) k = key(...c); }
    if (!k) { const ks = Object.keys(sol).filter(x => est.letras[x] !== sol[x]); k = ks[Math.floor(Math.random() * ks.length)]; }
    if (!k) return;
    est.letras[k] = sol[k]; mal.delete(k); if (!est.ayudas.includes(k)) est.ayudas.push(k);
    const [f, c] = k.split(',').map(Number); activa = [f, c];
    if (!def || !defsEn(f, c).includes(def)) def = defsEn(f, c)[0];
    guarda(CLAVE, est); comprueba(); pinta(); foco();
  });
  $('cr-comprobar').addEventListener('click', () => {
    mal = new Set(Object.keys(est.letras).filter(k => est.letras[k] !== sol[k]));
    pinta();
    const b = $('cr-comprobar'), txt = b.textContent;
    b.textContent = mal.size ? `${mal.size} por revisar` : (Object.keys(est.letras).length ? 'Todo bien de momento ✓' : 'Aún no hay letras');
    setTimeout(() => b.textContent = txt, 1800);
    foco();
  });
  $('cr-rendirse').addEventListener('click', () => {
    if (!confirm('¿Ver la solución? Se rellena todo.')) return;
    Object.assign(est.letras, sol); est.rendido = true; mal.clear(); guarda(CLAVE, est); comprueba(); pinta();
  });

  function fiesta() {
    const cs = Object.values(celdaEl);
    cs.forEach((el, i) => { el.style.animationDelay = (i * 18) + 'ms'; el.classList.add('ola'); });
    setTimeout(() => cs.forEach(el => { el.classList.remove('ola'); el.style.animationDelay = ''; }), cs.length * 18 + 700);
  }

  pinta();
})();
