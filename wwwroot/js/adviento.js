// Pasatiempos del calendario de adviento: criptofrase, crucigrama, murdoku y foto con pregunta.
(() => {
  const datos = JSON.parse(document.getElementById('datos')?.textContent || '{}');
  if (!datos.tipo) return;
  const juego = document.getElementById('juego'), msg = document.getElementById('msg');
  const btnComprobar = document.getElementById('comprobar'), btnPista = document.getElementById('pista');
  const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
  const q = datos.fecha ? `&fecha=${datos.fecha}` : '';
  const url = h => `${location.pathname}?handler=${h}${q}`;
  const post = async (h, body) => {
    const r = await fetch(url(h), { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify(body || {}) });
    if (!r.ok) throw new Error(r.status);
    return r.json();
  };
  const norm = s => (s || '').toUpperCase().normalize('NFD').replace(/N\u0303/g, 'Ñ').replace(/[\u0300-\u036f]/g, '');
  const letraOk = ch => /^[A-ZÑ]$/.test(ch);
  const aviso = (t, mal) => { if (!msg) return; msg.textContent = t; msg.className = 'adv-msg ' + (mal ? 'mal' : 'bien'); if (t) setTimeout(() => { if (msg.textContent === t) msg.textContent = ''; }, 4000); };

  let estado = {};
  try { estado = JSON.parse(datos.estado || '{}') || {}; } catch { }
  let guardando;
  const guardar = () => { if (datos.resuelta || datos.admin) return; clearTimeout(guardando); guardando = setTimeout(() => post('Estado', { estado: JSON.stringify(estado) }).catch(() => {}), 700); };

  const celebrar = () => {
    const colores = ['#e8b84a', '#ff6b6b', '#6bcb77', '#4d96ff', '#ff8fab', '#fff'];
    for (let i = 0; i < 70; i++) {
      const s = document.createElement('span'); s.className = 'adv-confeti';
      s.style.left = Math.random() * 100 + 'vw'; s.style.background = colores[i % colores.length];
      s.style.animationDuration = 2.5 + Math.random() * 2.5 + 's'; s.style.animationDelay = Math.random() * .8 + 's';
      s.style.width = 6 + Math.random() * 6 + 'px'; s.style.height = 10 + Math.random() * 8 + 'px';
      document.body.appendChild(s); setTimeout(() => s.remove(), 6000);
    }
    if (navigator.vibrate) navigator.vibrate([20, 40, 20, 40, 60]);
  };

  const ganado = res => {
    datos.resuelta = true;
    celebrar();
    document.getElementById('acciones')?.classList.add('oculto');
    const premio = document.getElementById('premio');
    if (premio) { premio.classList.remove('oculto'); const n = document.getElementById('n-letras'); if (n && res.letras != null) n.textContent = res.letras; setTimeout(() => premio.scrollIntoView({ behavior: 'smooth', block: 'center' }), 600); }
    juego?.classList.add('resuelto');
  };

  const pedirPista = async cuerpo => {
    try {
      const r = await post('Pista', cuerpo);
      if (!r.pista) { aviso('No hay más pistas: ¡ya casi lo tienes!'); return null; }
      return r.pista;
    } catch { aviso('No se pudo pedir la pista', true); return null; }
  };

  // ------------------------------------------------------------ Criptofrase
  if (datos.tipo === 'Criptofrase') {
    const v = datos.vista; estado.letras = estado.letras || {};
    let activo = null;
    const render = () => {
      juego.innerHTML = '';
      if (v.pista) { const p = document.createElement('p'); p.className = 'cripto-pista'; p.textContent = 'Pista: ' + v.pista; juego.appendChild(p); }
      const cont = document.createElement('div'); cont.className = 'cripto';
      v.palabras.forEach(pal => {
        const w = document.createElement('span'); w.className = 'cripto-palabra';
        pal.forEach(c => {
          const celda = document.createElement('span');
          if (c.n == null) { celda.className = 'signo'; celda.textContent = c.s; }
          else {
            celda.className = 'celda' + (activo === c.n ? ' activa' : '') + (estado.letras[c.n] ? ' llena' : '');
            celda.dataset.n = c.n;
            celda.innerHTML = `<b>${estado.letras[c.n] || ''}</b><i>${c.n}</i>`;
            celda.addEventListener('click', () => { activo = c.n; render(); teclado.focus(); });
          }
          w.appendChild(celda);
        });
        cont.appendChild(w);
      });
      juego.appendChild(cont);
      if (datos.resuelta && datos.solucion) { const s = document.createElement('p'); s.className = 'cripto-solucion'; s.textContent = '«' + datos.solucion + '»'; juego.appendChild(s); }
    };
    const teclado = document.createElement('input'); teclado.className = 'teclado-oculto'; teclado.autocapitalize = 'characters'; teclado.autocomplete = 'off'; teclado.setAttribute('aria-label', 'Letra');
    document.body.appendChild(teclado);
    const siguiente = () => {
      const nums = []; v.palabras.forEach(p => p.forEach(c => { if (c.n != null && !nums.includes(c.n)) nums.push(c.n); }));
      const i = nums.indexOf(activo);
      for (let k = 1; k <= nums.length; k++) { const n = nums[(i + k) % nums.length]; if (!estado.letras[n]) return n; }
      return activo;
    };
    teclado.addEventListener('input', () => {
      const ch = norm(teclado.value).slice(-1); teclado.value = '';
      if (activo == null || datos.resuelta) return;
      if (letraOk(ch)) { estado.letras[activo] = ch; activo = siguiente(); }
      guardar(); render();
    });
    teclado.addEventListener('keydown', e => {
      if (e.key === 'Backspace' && activo != null) { delete estado.letras[activo]; guardar(); render(); e.preventDefault(); }
    });
    render();
    if (datos.resuelta) btnComprobar?.closest('#acciones')?.classList.add('oculto');
    btnComprobar?.addEventListener('click', async () => {
      try {
        const r = await post('Resolver', { asignacion: estado.letras, estado: JSON.stringify(estado) });
        if (r.ok) { datos.solucion = r.solucion; render(); ganado(r); } else aviso(r.mensaje, true);
      } catch { aviso('No se pudo comprobar', true); }
    });
    btnPista?.addEventListener('click', async () => {
      const p = await pedirPista({ asignacion: estado.letras });
      if (p) { estado.letras[p.n] = p.letra; activo = p.n; guardar(); render(); aviso(`El ${p.n} es la ${p.letra}`); }
    });
  }

  // ------------------------------------------------------------ Crucigrama
  if (datos.tipo === 'Crucigrama') {
    const v = datos.vista; estado.celdas = estado.celdas || {};
    let activa = null, dir = 'H';
    const key = (f, c) => `${f},${c}`;
    const blanca = (f, c) => f >= 0 && c >= 0 && f < v.filas && c < v.cols && v.celdas[f][c] != null;
    const defEn = (f, c, d) => v.definiciones.find(x => x.dir === d && (d === 'H' ? x.fila === f && c >= x.col && c < x.col + x.largo : x.col === c && f >= x.fila && f < x.fila + x.largo));
    const render = () => {
      juego.innerHTML = '';
      const rej = document.createElement('div'); rej.className = 'cruci'; rej.style.setProperty('--cols', v.cols);
      const actDef = activa ? defEn(activa[0], activa[1], dir) : null;
      for (let f = 0; f < v.filas; f++) for (let c = 0; c < v.cols; c++) {
        const cel = document.createElement('span');
        const info = v.celdas[f][c];
        if (info == null) { cel.className = 'negra'; rej.appendChild(cel); continue; }
        const enPal = actDef && (actDef.dir === 'H' ? actDef.fila === f && c >= actDef.col && c < actDef.col + actDef.largo : actDef.col === c && f >= actDef.fila && f < actDef.fila + actDef.largo);
        cel.className = 'celda' + (activa && activa[0] === f && activa[1] === c ? ' activa' : '') + (enPal ? ' en-palabra' : '') + (v.sombreada && v.sombreada.fila === f && v.sombreada.col === c ? ' dorada' : '');
        cel.innerHTML = `${info.num ? `<i>${info.num}</i>` : ''}<b>${estado.celdas[key(f, c)] || ''}</b>`;
        cel.addEventListener('click', () => {
          if (activa && activa[0] === f && activa[1] === c) dir = dir === 'H' ? 'V' : 'H';
          else { activa = [f, c]; if (!defEn(f, c, dir)) dir = dir === 'H' ? 'V' : 'H'; }
          render(); teclado.focus();
        });
        rej.appendChild(cel);
      }
      juego.appendChild(rej);
      const defs = document.createElement('div'); defs.className = 'cruci-defs';
      ['H', 'V'].forEach(d => {
        const grupo = v.definiciones.filter(x => x.dir === d); if (!grupo.length) return;
        const h = document.createElement('h3'); h.textContent = d === 'H' ? 'Horizontales' : 'Verticales'; defs.appendChild(h);
        grupo.forEach(x => {
          const p = document.createElement('p'); p.className = 'def' + (actDef === x ? ' activa' : ''); p.innerHTML = `<b>${x.num}.</b> ${x.pista}`;
          p.addEventListener('click', () => { activa = [x.fila, x.col]; dir = x.dir; render(); teclado.focus(); });
          defs.appendChild(p);
        });
      });
      juego.appendChild(defs);
    };
    const teclado = document.createElement('input'); teclado.className = 'teclado-oculto'; teclado.autocapitalize = 'characters'; teclado.autocomplete = 'off'; teclado.setAttribute('aria-label', 'Letra');
    document.body.appendChild(teclado);
    const mover = paso => {
      if (!activa) return;
      let [f, c] = activa;
      const d = dir === 'H' ? [0, paso] : [paso, 0];
      if (blanca(f + d[0], c + d[1])) activa = [f + d[0], c + d[1]];
    };
    teclado.addEventListener('input', () => {
      const ch = norm(teclado.value).slice(-1); teclado.value = '';
      if (!activa || datos.resuelta) return;
      if (letraOk(ch)) { estado.celdas[key(...activa)] = ch; mover(1); guardar(); render(); }
    });
    teclado.addEventListener('keydown', e => {
      if (!activa || datos.resuelta) return;
      if (e.key === 'Backspace') { const k = key(...activa); if (estado.celdas[k]) delete estado.celdas[k]; else { mover(-1); delete estado.celdas[key(...activa)]; } guardar(); render(); e.preventDefault(); }
    });
    if (datos.resuelta && Array.isArray(datos.solucion)) datos.solucion.forEach(s => estado.celdas[key(s.fila, s.col)] = s.letra);
    render();
    if (datos.resuelta) document.getElementById('acciones')?.classList.add('oculto');
    btnComprobar?.addEventListener('click', async () => {
      try {
        const r = await post('Resolver', { celdas: estado.celdas, estado: JSON.stringify(estado) });
        if (r.ok) { activa = null; render(); ganado(r); } else aviso(r.mensaje, true);
      } catch { aviso('No se pudo comprobar', true); }
    });
    btnPista?.addEventListener('click', async () => {
      const p = await pedirPista({ celdas: estado.celdas });
      if (p) { estado.celdas[key(p.fila, p.col)] = p.letra; activa = [p.fila, p.col]; guardar(); render(); }
    });
  }

  // ------------------------------------------------------------ Murdoku
  if (datos.tipo === 'Murdoku') {
    const v = datos.vista; const cats = v.categorias;
    estado.marcas = estado.marcas || {}; estado.eleccion = estado.eleccion || [null, null, null];
    const pares = [[0, 1], [0, 2], [1, 2]];
    const ciclo = { '': 'x', 'x': 'v', 'v': '' };
    const render = () => {
      juego.innerHTML = '';
      if (v.caso) { const p = document.createElement('p'); p.className = 'murdoku-caso'; p.textContent = v.caso; juego.appendChild(p); }
      const ul = document.createElement('ol'); ul.className = 'murdoku-pistas';
      (v.pistas || []).forEach(t => { const li = document.createElement('li'); li.textContent = t; ul.appendChild(li); });
      juego.appendChild(ul);
      if (v.remate) { const p = document.createElement('p'); p.className = 'murdoku-remate'; p.textContent = v.remate; juego.appendChild(p); }

      const ayuda = document.createElement('p'); ayuda.className = 'murdoku-ayuda'; ayuda.textContent = 'Rejillas de notas: toca una casilla para marcar ✗ (imposible) o ✓ (seguro).'; juego.appendChild(ayuda);
      const grids = document.createElement('div'); grids.className = 'murdoku-grids';
      pares.forEach(([a, b]) => {
        const g = document.createElement('div'); g.className = 'mgrid';
        const t = document.createElement('div'); t.className = 'mtit'; t.textContent = `${cats[a].nombre} × ${cats[b].nombre}`; g.appendChild(t);
        const tabla = document.createElement('table');
        const cab = document.createElement('tr'); cab.appendChild(document.createElement('th'));
        cats[b].items.forEach(it => { const th = document.createElement('th'); th.textContent = corto(it); cab.appendChild(th); });
        tabla.appendChild(cab);
        cats[a].items.forEach((ia, i) => {
          const tr = document.createElement('tr'); const th = document.createElement('th'); th.textContent = corto(ia); tr.appendChild(th);
          cats[b].items.forEach((ib, j) => {
            const td = document.createElement('td'); const k = `${a}${b}-${i}${j}`; const m = estado.marcas[k] || '';
            td.className = m; td.textContent = m === 'x' ? '✗' : m === 'v' ? '✓' : '';
            td.addEventListener('click', () => { if (datos.resuelta) return; estado.marcas[k] = ciclo[m]; guardar(); render(); });
            tr.appendChild(td);
          });
          tabla.appendChild(tr);
        });
        g.appendChild(tabla); grids.appendChild(g);
      });
      juego.appendChild(grids);

      const acus = document.createElement('div'); acus.className = 'murdoku-acusacion';
      const h = document.createElement('h3'); h.textContent = datos.resuelta ? 'Caso resuelto' : 'Tu acusación'; acus.appendChild(h);
      cats.forEach((cat, ci) => {
        const fila = document.createElement('div'); fila.className = 'chips';
        cat.items.forEach((it, i) => {
          const ch = document.createElement('button'); ch.type = 'button'; ch.className = 'chip' + (estado.eleccion[ci] === i ? ' on' : ''); ch.textContent = it;
          ch.addEventListener('click', () => { if (datos.resuelta) return; estado.eleccion[ci] = estado.eleccion[ci] === i ? null : i; guardar(); render(); });
          fila.appendChild(ch);
        });
        acus.appendChild(fila);
      });
      juego.appendChild(acus);
      if (datos.resuelta && datos.solucion?.explicacion) { const p = document.createElement('p'); p.className = 'murdoku-explica'; p.textContent = datos.solucion.explicacion; juego.appendChild(p); }
    };
    const corto = s => s.replace(/^(En |Junto al |Un |Una |Las |Los |La |El )/i, '').replace(/^(el |la |su |al |a )/i, '').split(' ').slice(0, 2).join(' ');
    if (datos.resuelta && datos.solucion?.eleccion) estado.eleccion = datos.solucion.eleccion;
    render();
    if (datos.resuelta) document.getElementById('acciones')?.classList.add('oculto');
    btnComprobar?.addEventListener('click', async () => {
      if (estado.eleccion.some(x => x == null)) { aviso('Elige quién, dónde y qué.', true); return; }
      try {
        const r = await post('Resolver', { eleccion: estado.eleccion, estado: JSON.stringify(estado) });
        if (r.ok) { datos.solucion = r.solucion; render(); ganado(r); } else aviso(r.mensaje, true);
      } catch { aviso('No se pudo comprobar', true); }
    });
    btnPista?.addEventListener('click', async () => {
      const p = await pedirPista({ eleccion: estado.eleccion.map(x => x ?? -1) });
      if (p) { estado.eleccion[p.categoria] = p.indice; guardar(); render(); aviso('Pista: ' + p.texto); }
    });
  }

  // ------------------------------------------------------------ Foto con pregunta
  if (datos.tipo === 'Foto' && !datos.resuelta) {
    const inp = document.getElementById('respuesta');
    const comprobar = async () => {
      if (!inp.value.trim()) return;
      try {
        const r = await post('Resolver', { respuesta: inp.value });
        if (r.ok) {
          document.getElementById('foto')?.classList.remove('velada');
          inp.closest('.fila')?.replaceWith(Object.assign(document.createElement('p'), { className: 'respuesta-ok', textContent: r.solucion }));
          btnPista?.remove(); ganado(r);
        } else aviso(r.mensaje, true);
      } catch { aviso('No se pudo comprobar', true); }
    };
    btnComprobar?.addEventListener('click', comprobar);
    inp?.addEventListener('keydown', e => { if (e.key === 'Enter') comprobar(); });
    btnPista?.addEventListener('click', async () => { const p = await pedirPista({}); if (p) aviso(p.texto); });
  }
})();
