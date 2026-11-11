// Lista de la compra: sugerencias al escribir ("fi" → Fideos) y zona automática ("manzanas" → Frutería).
// La lógica de palabras clave es la misma que ZonaAuto.Adivinar en el servidor.
(() => {
  const datosEl = document.getElementById('datos-compra'), inp = document.getElementById('a-nombre');
  if (!datosEl || !inp) return;
  const D = JSON.parse(datosEl.textContent);
  const sel = document.getElementById('a-zona'), sug = document.getElementById('a-sug'), pista = document.getElementById('a-pista');
  const cant = inp.form.querySelector('[name=cantidad]');
  const clave = s => (s || '').trim().toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').split(/\s+/).filter(Boolean).join(' ');
  const singulares = w => { const r = [w]; if (w.length > 3 && w.endsWith('s')) r.push(w.slice(0, -1)); if (w.length > 4 && w.endsWith('es')) r.push(w.slice(0, -2)); return r; };
  const enLista = new Set(D.enLista);
  const conocidos = D.conocidos.map(([n, z, v]) => ({ n, z, v, k: clave(n) }));
  const porClave = new Map();
  conocidos.forEach(c => { porClave.set(c.k, c); const w = c.k.split(' '), u = w.pop(); singulares(u).forEach(s => { const k2 = [...w, s].join(' '); if (!porClave.has(k2)) porClave.set(k2, c); }); });
  const nombreZona = z => { const o = [...sel.options].find(o => o.value === z); return o ? o.textContent.trim() : z; };

  function adivinar(nombre) {
    const n = clave(nombre); if (!n) return null;
    const ws = n.split(' ');
    if (ws.some(p => D.envasado.some(e => p.startsWith(e))) || D.envasado.some(e => e.includes(' ') && n.includes(e))) return null;
    for (const [z, ps] of Object.entries(D.palabras)) for (const p of ps) if (p.includes(' ') && n.startsWith(p)) return z;
    for (const w of ws) { const ss = singulares(w); for (const [z, ps] of Object.entries(D.palabras)) if (ps.some(p => !p.includes(' ') && ss.includes(p))) return z; }
    return null;
  }
  function buscar(q) {
    const n = clave(q); if (!n) return [];
    const res = [];
    for (const c of conocidos) {
      let r = c.k.startsWith(n) ? 0 : c.k.split(' ').some(w => w.startsWith(n)) ? 1 : (n.length >= 3 && c.k.includes(n)) ? 2 : -1;
      if (r < 0 || c.k === n) continue;
      res.push({ c, r: r + (enLista.has(c.k) ? 3 : 0) });
    }
    return res.sort((a, b) => a.r - b.r || b.c.v - a.c.v || a.c.n.localeCompare(b.c.n)).slice(0, 6).map(x => x.c);
  }

  let aMano = false;
  function ponZona(z, motivo) {
    if (aMano) return;
    sel.value = z || 'Otros';
    pista.textContent = inp.value.trim() ? `Va a ${nombreZona(sel.value)} · ${motivo}` : 'La zona se pone sola al escribir; puedes cambiarla.';
  }
  function clasificar() {
    const k = clave(inp.value), c = porClave.get(k);
    if (c) ponZona(c.z, enLista.has(c.k) ? 'ya está en la lista' : c.v ? `ya lo habéis comprado${c.v > 1 ? ' ' + c.v + ' veces' : ''}` : 'como en las recetas');
    else { const z = adivinar(inp.value); ponZona(z, z ? 'automático' : buscar(inp.value).length ? 'toca una sugerencia o sigue escribiendo' : 'no lo conozco, cámbiala si no es'); }
  }
  function resalta(nombre, q) {
    const i = clave(nombre).indexOf(clave(q)), esc = s => s.replace(/[&<>"]/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[ch]));
    if (i < 0) return esc(nombre);
    const L = clave(q).length; // clave() conserva la longitud salvo espacios dobles: vale para resaltar
    return esc(nombre.slice(0, i)) + '<b>' + esc(nombre.slice(i, i + L)) + '</b>' + esc(nombre.slice(i + L));
  }
  function pintaSugerencias() {
    const lst = buscar(inp.value);
    sug.innerHTML = '';
    lst.forEach(c => {
      const b = document.createElement('button');
      b.type = 'button'; b.className = 'chip predice-op';
      b.innerHTML = `<span class="predice-ico" aria-hidden="true">${nombreZona(c.z).split(' ')[0]}</span><span>${resalta(c.n, inp.value)}</span>` + (enLista.has(c.k) ? '<span class="suave peq">· ya está</span>' : '');
      b.addEventListener('pointerdown', e => e.preventDefault()); // que no se cierre el teclado
      b.addEventListener('click', () => { inp.value = c.n; sug.hidden = true; clasificar(); inp.focus(); });
      sug.appendChild(b);
    });
    sug.hidden = !lst.length;
  }
  inp.addEventListener('input', () => { clasificar(); pintaSugerencias(); });
  inp.addEventListener('keydown', e => { if (e.key === 'Tab' && !sug.hidden && sug.firstChild && !e.shiftKey) { e.preventDefault(); sug.firstChild.click(); } });
  sel.addEventListener('change', () => { aMano = true; pista.textContent = `Va a ${nombreZona(sel.value)} · elegida a mano`; });
  inp.form.addEventListener('reset', () => { aMano = false; });
  if (cant) cant.addEventListener('focus', () => { sug.hidden = true; });
})();
