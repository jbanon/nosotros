// Juegos de la viñeta del día: rompecabezas, parejas, revelar (pregunta o rascar) y ordena la frase.
(() => {
  const datos = JSON.parse(document.getElementById('datos')?.textContent || '{}');
  if (!datos.tipo) return;
  const juego = document.getElementById('juego'), msg = document.getElementById('msg');
  const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
  const q = datos.fecha ? `&fecha=${datos.fecha}` : '';
  const post = async (h, body) => {
    const r = await fetch(`${location.pathname}?handler=${h}${q}`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify(body || {}) });
    if (!r.ok) throw new Error(r.status);
    return r.json();
  };
  const aviso = (t, mal) => { if (!msg) return; msg.textContent = t; msg.className = 'adv-msg ' + (mal ? 'mal' : 'bien'); if (t) setTimeout(() => { if (msg.textContent === t) msg.textContent = ''; }, 4000); };
  let estado = {}; try { estado = JSON.parse(datos.estado || '{}') || {}; } catch { }
  let guardando;
  const guardar = () => { if (datos.resuelta || datos.admin) return; clearTimeout(guardando); guardando = setTimeout(() => post('Estado', { estado: JSON.stringify(estado) }).catch(() => {}), 700); };
  const celebrar = () => {
    const colores = ['#e8b84a', '#ff6b6b', '#6bcb77', '#4d96ff', '#ff8fab', '#c77dff'];
    for (let i = 0; i < 70; i++) {
      const s = document.createElement('span'); s.className = 'adv-confeti';
      s.style.left = Math.random() * 100 + 'vw'; s.style.background = colores[i % colores.length];
      s.style.animationDuration = 2.5 + Math.random() * 2.5 + 's'; s.style.animationDelay = Math.random() * .8 + 's';
      s.style.width = 6 + Math.random() * 6 + 'px'; s.style.height = 10 + Math.random() * 8 + 'px';
      document.body.appendChild(s); setTimeout(() => s.remove(), 6000);
    }
    if (navigator.vibrate) navigator.vibrate([20, 40, 20, 40, 60]);
  };
  const ganado = () => {
    datos.resuelta = true; celebrar();
    document.getElementById('acciones')?.classList.add('oculto');
    const premio = document.getElementById('premio');
    if (premio) { premio.classList.remove('oculto'); setTimeout(() => premio.scrollIntoView({ behavior: 'smooth', block: 'center' }), 700); }
  };
  const resolver = async cuerpo => {
    try { const r = await post('Resolver', { ...cuerpo, estado: JSON.stringify(estado) }); if (r.ok) ganado(); else aviso(r.mensaje, true); return r.ok; }
    catch { aviso('No se pudo comprobar', true); return false; }
  };
  const pedirPista = async cuerpo => {
    try { const r = await post('Pista', cuerpo); if (!r.pista) aviso('No hay más pistas: ¡ya casi lo tienes!'); return r.pista; }
    catch { aviso('No se pudo pedir la pista', true); return null; }
  };
  const btnPista = document.getElementById('pista');

  // ------------------------------------------------------------ Rompecabezas (tocar dos piezas para intercambiarlas)
  if (datos.tipo === 'Rompecabezas') {
    const n = datos.vista.n; estado.orden = estado.orden || datos.vista.permutacion.slice();
    let activa = null;
    const img = new Image(); img.src = datos.imagen;
    const render = () => {
      juego.innerHTML = '';
      const g = document.createElement('div'); g.className = 'rompe' + (datos.resuelta ? ' resuelto' : ''); g.style.setProperty('--n', n);
      if (img.naturalWidth) g.style.aspectRatio = img.naturalWidth + ' / ' + img.naturalHeight;
      estado.orden.forEach((pieza, pos) => {
        const d = document.createElement('div'); d.className = 'pieza' + (activa === pos ? ' activa' : '') + (pieza === pos ? ' bien' : '');
        d.style.backgroundImage = `url("${datos.imagen}")`;
        d.style.backgroundPosition = `${(pieza % n) * 100 / (n - 1)}% ${Math.floor(pieza / n) * 100 / (n - 1)}%`;
        d.addEventListener('click', () => {
          if (datos.resuelta) return;
          if (activa === null) { activa = pos; render(); return; }
          if (activa !== pos) { [estado.orden[activa], estado.orden[pos]] = [estado.orden[pos], estado.orden[activa]]; }
          activa = null; guardar(); render();
          if (estado.orden.every((x, i) => x === i)) resolver({ orden: estado.orden });
        });
        g.appendChild(d);
      });
      juego.appendChild(g);
      const p = document.createElement('p'); p.className = 'suave peq centrado'; p.style.margin = '10px 0 0';
      p.textContent = `${estado.orden.filter((x, i) => x === i).length} de ${n * n} piezas en su sitio`; juego.appendChild(p);
    };
    img.onload = render; render();
    btnPista?.addEventListener('click', async () => {
      const p = await pedirPista({ orden: estado.orden });
      if (p) { [estado.orden[p.pos], estado.orden[p.con]] = [estado.orden[p.con], estado.orden[p.pos]]; activa = null; guardar(); render(); if (estado.orden.every((x, i) => x === i)) resolver({ orden: estado.orden }); }
    });
  }

  // ------------------------------------------------------------ Parejas (memoria con viñetas ya vistas)
  if (datos.tipo === 'Parejas') {
    const ids = datos.vista.ids; const cartas = datos.vista.orden.map(k => ids[k % ids.length]);
    estado.encontradas = estado.encontradas || []; estado.intentos = estado.intentos || 0;
    let abiertas = [], bloqueado = false;
    const render = () => {
      juego.innerHTML = '';
      const g = document.createElement('div'); g.className = 'parejas'; g.style.setProperty('--cols', cartas.length <= 8 ? 4 : 4);
      cartas.forEach((id, i) => {
        const c = document.createElement('button'); c.type = 'button';
        const vista = abiertas.includes(i) || estado.encontradas.includes(i);
        c.className = 'carta' + (vista ? ' vista' : '') + (estado.encontradas.includes(i) ? ' hecha' : '');
        c.innerHTML = `<span class="dorso">♥</span><span class="cara" style="background-image:url('/Vinetas/Imagen/${id}')"></span>`;
        c.addEventListener('click', () => {
          if (bloqueado || vista || datos.resuelta) return;
          abiertas.push(i); render();
          if (abiertas.length === 2) {
            estado.intentos++; bloqueado = true;
            const [a, b] = abiertas;
            setTimeout(async () => {
              if (cartas[a] === cartas[b]) estado.encontradas.push(a, b);
              abiertas = []; bloqueado = false; guardar(); render();
              if (estado.encontradas.length === cartas.length) await resolver({ parejas: true });
            }, cartas[a] === cartas[b] ? 350 : 900);
          }
        });
        g.appendChild(c);
      });
      juego.appendChild(g);
      const p = document.createElement('p'); p.className = 'suave peq centrado'; p.style.margin = '10px 0 0';
      p.textContent = `${estado.encontradas.length / 2} de ${cartas.length / 2} parejas · ${estado.intentos} intentos`; juego.appendChild(p);
    };
    render();
  }

  // ------------------------------------------------------------ Revelar: pregunta o rascar
  if (datos.tipo === 'Revelar') {
    const img = document.getElementById('revelar-img');
    if (datos.vista.pregunta) {
      const inp = document.getElementById('respuesta');
      const comprobar = async () => { if (!inp.value.trim()) return; if (await resolver({ respuesta: inp.value })) { img.classList.remove('velada'); inp.closest('.fila')?.remove(); btnPista?.remove(); } };
      document.getElementById('comprobar')?.addEventListener('click', comprobar);
      inp?.addEventListener('keydown', e => { if (e.key === 'Enter') comprobar(); });
      btnPista?.addEventListener('click', async () => { const p = await pedirPista({}); if (p) aviso(p.texto); });
    } else {
      const canvas = document.getElementById('rasca'), cont = document.getElementById('revelar');
      const ctx = canvas.getContext('2d');
      let listo = false, trazos = 0;
      const preparar = () => {
        const r = img.getBoundingClientRect(); if (!r.width) return;
        canvas.width = Math.round(r.width); canvas.height = Math.round(r.height);
        ctx.globalCompositeOperation = 'source-over';
        const grad = ctx.createLinearGradient(0, 0, canvas.width, canvas.height); grad.addColorStop(0, '#c9b8a8'); grad.addColorStop(1, '#9e8878');
        ctx.fillStyle = grad; ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = 'rgba(255,255,255,.75)'; ctx.font = '700 18px Nunito, sans-serif'; ctx.textAlign = 'center';
        ctx.fillText('rasca aquí ✦', canvas.width / 2, canvas.height / 2);
        ctx.globalCompositeOperation = 'destination-out'; listo = true;
      };
      if (img.complete) preparar(); else img.addEventListener('load', preparar);
      window.addEventListener('load', () => { if (!listo) preparar(); });
      setTimeout(() => { if (!listo) preparar(); }, 400); setTimeout(() => { if (!listo) preparar(); }, 1500);
      const punto = e => { const r = canvas.getBoundingClientRect(); const t = e.touches ? e.touches[0] : e; return { x: (t.clientX - r.left) * canvas.width / r.width, y: (t.clientY - r.top) * canvas.height / r.height }; };
      let pintando = false;
      const rascar = e => {
        if (!listo || !pintando || datos.resuelta) return; e.preventDefault();
        const { x, y } = punto(e); ctx.beginPath(); ctx.arc(x, y, Math.max(22, canvas.width / 14), 0, Math.PI * 2); ctx.fill();
        if (++trazos % 12 === 0) medir();
      };
      const medir = async () => {
        const d = ctx.getImageData(0, 0, canvas.width, canvas.height).data; let vacios = 0, total = 0;
        for (let i = 3; i < d.length; i += 4 * 7) { total++; if (d[i] < 40) vacios++; }
        const pct = Math.round(vacios * 100 / total);
        if (pct >= 60 && !datos.resuelta) { datos.resuelta = true; canvas.style.transition = 'opacity .8s'; canvas.style.opacity = '0'; if (navigator.vibrate) navigator.vibrate(30); await resolver({ porcentaje: pct }); }
      };
      ['touchstart', 'mousedown'].forEach(ev => canvas.addEventListener(ev, e => { pintando = true; rascar(e); }, { passive: false }));
      ['touchmove', 'mousemove'].forEach(ev => canvas.addEventListener(ev, rascar, { passive: false }));
      ['touchend', 'mouseup', 'mouseleave'].forEach(ev => canvas.addEventListener(ev, () => { pintando = false; medir(); }));
      window.addEventListener('resize', () => { if (!datos.resuelta) preparar(); });
    }
  }

  // ------------------------------------------------------------ Ordena la frase
  if (datos.tipo === 'OrdenaFrase') {
    const palabras = datos.vista.palabras; estado.orden = estado.orden || [];
    const ordenCorrecto = palabras.map(p => p.i).sort((a, b) => a - b);
    const render = () => {
      juego.innerHTML = '';
      const frase = document.createElement('div'); frase.className = 'ordena-frase';
      if (!estado.orden.length) { const s = document.createElement('span'); s.className = 'suave'; s.textContent = 'Toca la primera palabra…'; frase.appendChild(s); }
      estado.orden.forEach((i, k) => {
        const w = document.createElement('button'); w.type = 'button'; w.className = 'chip on'; w.textContent = palabras.find(p => p.i === i).t;
        w.addEventListener('click', () => { if (datos.resuelta || k !== estado.orden.length - 1) return; estado.orden.pop(); guardar(); render(); });
        frase.appendChild(w);
      });
      juego.appendChild(frase);
      const banco = document.createElement('div'); banco.className = 'chips ordena-banco';
      palabras.forEach(p => {
        if (estado.orden.includes(p.i)) return;
        const w = document.createElement('button'); w.type = 'button'; w.className = 'chip'; w.textContent = p.t;
        w.addEventListener('click', async () => {
          if (datos.resuelta) return;
          const esperado = ordenCorrecto[estado.orden.length];
          const textoEsperado = palabras.find(x => x.i === esperado).t.toLowerCase().replace(/[^\p{L}]/gu, '');
          if (p.t.toLowerCase().replace(/[^\p{L}]/gu, '') !== textoEsperado) { w.classList.add('mal'); if (navigator.vibrate) navigator.vibrate(60); setTimeout(() => w.classList.remove('mal'), 500); return; }
          estado.orden.push(p.i); guardar(); render();
          if (estado.orden.length === palabras.length) { frase.classList.add('completa'); await resolver({ orden: estado.orden }); }
        });
        banco.appendChild(w);
      });
      juego.appendChild(banco);
    };
    render();
    btnPista?.addEventListener('click', async () => {
      const p = await pedirPista({ orden: estado.orden });
      if (p) { aviso('La siguiente es «' + p.texto + '»'); document.querySelectorAll('.ordena-banco .chip').forEach(c => { if (c.textContent === p.texto) c.classList.add('sugerida'); }); }
    });
  }
})();
