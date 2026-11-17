// Adviento · juego de los globos: explotarlos, cazar las palabras de los corazones dorados y escribir la frase.
(() => {
  const A = window.adv;
  if (!A || A.datos.tipo !== 'Globos') return;
  const D = A.datos;

  // ---------- Ajustes (afinar probando en el móvil) ----------
  const AJ = {
    fallosMax: 10,           // globos normales que pueden escaparse antes de perder
    globosPorPalabra: 8,     // globos normales entre corazón y corazón (± 2)
    globosFinales: 6,        // globos de propina tras el último corazón
    perdidasObjetivo: 3.5,
    maxGlobos: 12,           // tope de globos a la vez en pantalla (si hay más, espera para soltar otro)   // palabras que debería perder de media: la vida del corazón se adapta a esto
    // De la primera palabra (0) a la última (último tramo); se interpola entre tramos.
    // intervalo ms entre globos · vel en alturas de pantalla por segundo · radio en fracción del ancho (máx. 480)
    // zigzag px · vidaPalabra ms que dura un corazón en pantalla
    niveles: [
      { intervalo: 900, vel: 0.20, radio: 0.11, zigzag: 0, vidaPalabra: 2000 },
      { intervalo: 680, vel: 0.27, radio: 0.095, zigzag: 14, vidaPalabra: 1600 },
      { intervalo: 520, vel: 0.34, radio: 0.085, zigzag: 24, vidaPalabra: 1300 },
      { intervalo: 420, vel: 0.42, radio: 0.075, zigzag: 34, vidaPalabra: 1050 },
    ],
  };
  const COLORES = ['#ff5d73', '#4d96ff', '#6bcb77', '#ffb84d', '#b07cff', '#ff8fab', '#38c6d9'];

  // ---------- Frase ----------
  const frase = (D.vista && D.vista.frase) || '';
  const limpia = w => A.norm(w).replace(/[^A-ZÑ]/g, '');
  const visibles = frase.split(/\s+/).map(w => w.replace(/[^\p{L}]/gu, '')).filter(Boolean);   // "vida," → "vida"
  const objetivo = visibles.map(limpia);
  const n = objetivo.length;
  const baraja = a => { for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; };

  const $ = id => document.getElementById(id);
  const inicio = $('gl-inicio'), escribir = $('gl-escribir'), pantalla = $('gl-pantalla'), cv = $('gl-canvas');
  const hudFallos = $('gl-fallos'), hudPal = $('gl-npal'), bandeja = $('gl-bandeja'), avisoEl = $('gl-aviso');
  if ($('gl-max')) $('gl-max').textContent = AJ.fallosMax;
  const ctx = cv.getContext('2d');
  let W = 0, H = 0, DPR = 1;
  const sprites = new Map();   // dibujos ya hechos de cada globo (pintarlos cada fotograma con degradados es lento en el móvil)
  const medir = () => {
    DPR = Math.min(window.devicePixelRatio || 1, 1.5); const dpr = DPR; sprites.clear();
    W = window.innerWidth; H = window.innerHeight;
    cv.width = Math.round(W * dpr); cv.height = Math.round(H * dpr);
    cv.style.width = W + 'px'; cv.style.height = H + 'px';
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  };

  // ---------- Partida ----------
  let P = null, raf = 0, ultimo = 0;
  const lerp = (a, b, f) => a + (b - a) * f;
  function nivel() {
    const L = AJ.niveles, pos = Math.min(1, P.salidas / Math.max(1, n - 1)) * (L.length - 1);
    const i = Math.min(L.length - 2, Math.floor(pos)), f = pos - i, a = L[i], b = L[i + 1];
    const r = {}; for (const k in a) r[k] = lerp(a[k], b[k], f); return r;
  }

  function nueva() {
    const mitad = Math.ceil(n / 2);
    const orden = baraja([...Array(mitad).keys()]).concat(baraja([...Array(n - mitad).keys()].map(i => i + mitad)));
    P = { t: 0, globos: [], efectos: [], orden, salidas: 0, cazadas: [], perdidas: 0, fallos: 0, factor: 1,
          proximo: 600, hastaPalabra: AJ.globosPorPalabra, finales: AJ.globosFinales, fin: false, cuenta: 3.2 };
    bandeja.innerHTML = ''; pintaHud();
  }

  function globo(x, y, nv, tipo) {
    const r = nv.radio * Math.min(W, 480) * (tipo === 'p' ? 1.05 : 0.9 + Math.random() * 0.2);
    return { x0: x, x, y, r, tipo, vy: nv.vel * H * (tipo === 'p' ? 0.22 : 0.85 + Math.random() * 0.3),
             amp: nv.zigzag * (0.5 + Math.random() * 0.5), fr: 1.4 + Math.random() * 1.2, fase: Math.random() * 6.3,
             col: COLORES[Math.floor(Math.random() * COLORES.length)], nace: P.t, vida: nv.vidaPalabra * P.factor };
  }

  function soltar() {
    const nv = nivel();
    if (P.salidas < n && --P.hastaPalabra <= 0) {
      // Corazón dorado escondido entre un racimo de globos
      const r = nv.radio * Math.min(W, 480);
      const gx = r * 2.4 + Math.random() * Math.max(1, W - r * 4.8), gy = H * (0.32 + Math.random() * 0.36);
      const c = globo(gx, gy, nv, 'p'); c.idx = P.orden[P.salidas++]; P.globos.push(c);
      for (const [dx, dy] of [[-2, 0.6], [2, 0.4], [-0.9, 2], [1.1, -1.6]].slice(0, 3 + Math.round(Math.random())))
        P.globos.push(globo(Math.max(r, Math.min(W - r, gx + dx * r * (0.85 + Math.random() * 0.3))), gy + dy * r, nv, 'n'));
      P.hastaPalabra = AJ.globosPorPalabra + Math.round(Math.random() * 4 - 2);
    } else if (P.salidas < n || P.finales-- > 0) {
      const r = nv.radio * Math.min(W, 480);
      P.globos.push(globo(r + Math.random() * (W - 2 * r), H + r * 1.2, nv, 'n'));
    }
    P.proximo = P.t + nv.intervalo * (0.75 + Math.random() * 0.5);
  }

  // Dificultad adaptable: si caza demasiadas, los corazones duran menos; si pierde muchas, más.
  function adaptar() {
    const hechas = P.cazadas.length + P.perdidas, meta = AJ.perdidasObjetivo * hechas / n;
    if (P.perdidas < meta - 0.75) P.factor = Math.max(0.55, P.factor * 0.82);
    else if (P.perdidas > meta + 0.5) P.factor = Math.min(1.6, P.factor * 1.25);
  }

  function explota(g) {
    const m = P.efectos.length > 60 ? 4 : 8;
    for (let i = 0; i < m; i++) {
      const a = Math.random() * 6.3, v = 80 + Math.random() * 160;
      P.efectos.push({ tipo: 'trozo', x: g.x, y: g.y, vx: Math.cos(a) * v, vy: Math.sin(a) * v, col: g.tipo === 'p' ? '#ffd45e' : g.col, t0: P.t, dur: 500 });
    }
    if (navigator.vibrate) navigator.vibrate(g.tipo === 'p' ? 25 : 8);
  }

  function toque(e) {
    if (!P || P.fin || P.cuenta > 0) return;
    e.preventDefault();
    const rc = cv.getBoundingClientRect(), x = e.clientX - rc.left, y = e.clientY - rc.top;
    // El globo más cercano al dedo. Se mira también dónde estaba hace 150 ms: el toque llega con retraso
    // y a velocidad alta el globo ya se ha movido.
    let g = null, mejor = Infinity;
    for (const b of P.globos) {
      const atras = b.vy * 0.15, dy = y < b.y ? b.y - y : y > b.y + atras ? y - b.y - atras : 0;
      const d = Math.hypot(b.x - x, dy) / b.r - (b.tipo === 'p' ? 0.25 : 0);
      if (d < mejor) { mejor = d; g = b; }
    }
    if (mejor > 1.4) g = null;
    if (!g) return;
    P.globos.splice(P.globos.indexOf(g), 1);
    explota(g);
    if (g.tipo === 'p') {
      P.cazadas.push(g.idx); adaptar();
      P.efectos.push({ tipo: 'palabra', x: g.x, y: g.y, texto: visibles[g.idx], t0: P.t, dur: 1300 });
      const ch = document.createElement('span'); ch.className = 'gl-chip'; ch.textContent = visibles[g.idx]; bandeja.appendChild(ch);
      pintaHud();
    }
  }

  function pintaHud() {
    hudFallos.innerHTML = '';
    for (let i = 0; i < AJ.fallosMax; i++) { const s = document.createElement('i'); if (i < P.fallos) s.className = 'fuera'; hudFallos.appendChild(s); }
    hudPal.textContent = `💛 ${P.cazadas.length}/${n}`;
  }

  function avisar(html, botones) {
    avisoEl.innerHTML = html; avisoEl.hidden = false;
    for (const [texto, fn, clase] of botones || []) {
      const b = document.createElement('button'); b.type = 'button'; b.className = 'btn adv-btn ' + (clase || ''); b.textContent = texto;
      b.addEventListener('click', fn); avisoEl.appendChild(b);
    }
  }

  function paso(ahora) {
    const dt = Math.min(50, ahora - (ultimo || ahora)); ultimo = ahora;
    if (P.cuenta > 0) { P.cuenta -= dt / 1000; if (P.cuenta <= 0) avisoEl.hidden = true; else avisoEl.innerHTML = `<b class="gl-num">${Math.ceil(P.cuenta - 0.2) || '¡Ya!'}</b>`; }
    else if (!P.fin) {
      P.t += dt;
      if (P.t >= P.proximo) { if (P.globos.length < AJ.maxGlobos) soltar(); else P.proximo = P.t + 120; }
      const s = dt / 1000;
      for (let i = P.globos.length - 1; i >= 0; i--) {
        const g = P.globos[i];
        g.y -= g.vy * s;
        g.x = g.x0 + Math.sin((P.t / 1000) * g.fr + g.fase) * g.amp;
        if (g.tipo === 'n' && g.y + g.r * 2.2 < 0) {
          P.globos.splice(i, 1); P.fallos++; pintaHud();
          hudFallos.classList.remove('sacude'); void hudFallos.offsetWidth; hudFallos.classList.add('sacude');
          if (P.fallos >= AJ.fallosMax) { perder(); break; }
        } else if (g.tipo === 'p' && (P.t - g.nace > g.vida || g.y + g.r < 0)) {
          P.globos.splice(i, 1); P.perdidas++; adaptar();
          P.efectos.push({ tipo: 'puf', x: g.x, y: g.y, r: g.r, t0: P.t, dur: 450 });
        }
      }
      if (!P.fin && P.salidas >= n && P.finales <= 0 && !P.globos.some(g => g.tipo === 'n' || g.tipo === 'p')) ganar();
    }
    dibujar();
    raf = requestAnimationFrame(paso);
  }

  // ---------- Dibujo ----------
  function aclara(hex, k) {
    const v = parseInt(hex.slice(1), 16), c = [v >> 16, (v >> 8) & 255, v & 255].map(x => Math.round(x + (255 - x) * k));
    return `rgb(${c[0]},${c[1]},${c[2]})`;
  }
  function oscurece(hex, k) {
    const v = parseInt(hex.slice(1), 16), c = [v >> 16, (v >> 8) & 255, v & 255].map(x => Math.round(x * (1 - k)));
    return `rgb(${c[0]},${c[1]},${c[2]})`;
  }
  function sprite(clave, ancho, alto, pinta) {
    let s = sprites.get(clave);
    if (!s) {
      s = document.createElement('canvas'); s.width = Math.ceil(ancho * DPR); s.height = Math.ceil(alto * DPR);
      const c = s.getContext('2d'); c.scale(DPR, DPR); pinta(c); sprites.set(clave, s);
    }
    return s;
  }
  function dibujaGlobo(g) {
    const r = Math.round(g.r / 2) * 2, an = r * 2 + 4, al = r * 3.4;
    const s = sprite(g.col + r, an, al, c => {
      const x = an / 2, y = r + 2;
      c.strokeStyle = 'rgba(255,255,255,.55)'; c.lineWidth = 1.2;
      c.beginPath(); c.moveTo(x, y + r);
      c.bezierCurveTo(x - r * 0.3, y + r * 1.5, x + r * 0.3, y + r * 1.8, x - r * 0.1, y + r * 2.2); c.stroke();
      const gr = c.createRadialGradient(x - r * 0.35, y - r * 0.4, r * 0.1, x, y, r * 1.1);
      gr.addColorStop(0, aclara(g.col, 0.55)); gr.addColorStop(0.55, g.col); gr.addColorStop(1, oscurece(g.col, 0.3));
      c.fillStyle = gr; c.beginPath(); c.ellipse(x, y, r * 0.86, r, 0, 0, 6.2832); c.fill();
      c.fillStyle = g.col; c.beginPath(); c.moveTo(x, y + r * 0.95); c.lineTo(x - r * 0.13, y + r * 1.13); c.lineTo(x + r * 0.13, y + r * 1.13); c.fill();
      c.fillStyle = 'rgba(255,255,255,.45)'; c.beginPath(); c.ellipse(x - r * 0.33, y - r * 0.38, r * 0.14, r * 0.26, -0.5, 0, 6.2832); c.fill();
    });
    ctx.drawImage(s, g.x - an / 2, g.y - r - 2, an, al);
  }
  function dibujaCorazon(g) {
    const R = 60, lado = R * 3.2;   // se dibuja una vez grande y se escala
    const s = sprite('corazon', lado, lado, c => {
      c.translate(lado / 2, lado / 2);
      c.shadowColor = 'rgba(255,210,90,.9)'; c.shadowBlur = 30;
      const gr = c.createLinearGradient(0, -R, 0, R);
      gr.addColorStop(0, '#fff2b0'); gr.addColorStop(0.45, '#ffd04a'); gr.addColorStop(1, '#d99a12');
      c.fillStyle = gr; c.beginPath(); c.moveTo(0, R * 0.95);
      c.bezierCurveTo(-R * 1.25, R * 0.1, -R * 1.0, -R * 1.05, 0, -R * 0.45);
      c.bezierCurveTo(R * 1.0, -R * 1.05, R * 1.25, R * 0.1, 0, R * 0.95); c.fill();
      c.shadowBlur = 0; c.fillStyle = 'rgba(255,255,255,.6)';
      c.beginPath(); c.ellipse(-R * 0.45, -R * 0.4, R * 0.12, R * 0.22, -0.6, 0, 6.2832); c.fill();
    });
    const edad = P.t - g.nace, entra = Math.min(1, edad / 160), queda = g.vida - edad;
    const k = g.r * (0.6 + 0.4 * entra) * (1 + 0.05 * Math.sin(edad / 70)) / R, l = lado * k;
    ctx.globalAlpha = Math.max(0, Math.min(1, queda / 250));
    ctx.drawImage(s, g.x - l / 2, g.y - l / 2, l, l);
    ctx.globalAlpha = 1;
  }
  function dibujar() {
    ctx.clearRect(0, 0, W, H);
    for (const g of P.globos) g.tipo === 'p' ? dibujaCorazon(g) : dibujaGlobo(g);
    for (let i = P.efectos.length - 1; i >= 0; i--) {
      const e = P.efectos[i], f = (P.t - e.t0) / e.dur;
      if (f >= 1) { P.efectos.splice(i, 1); continue; }
      ctx.save(); ctx.globalAlpha = 1 - f;
      if (e.tipo === 'trozo') {
        const s = (P.t - e.t0) / 1000;
        ctx.fillStyle = e.col; ctx.fillRect(e.x + e.vx * s - 3, e.y + e.vy * s + 300 * s * s - 3, 6, 6);
      } else if (e.tipo === 'puf') {
        ctx.strokeStyle = 'rgba(255,220,120,.8)'; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.arc(e.x, e.y, e.r * (0.6 + f * 0.8), 0, 6.2832); ctx.stroke();
      } else if (e.tipo === 'palabra') {
        const tam = Math.round(Math.min(W, 480) * 0.11);
        ctx.font = `800 ${tam}px system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.shadowColor = 'rgba(255,190,40,.95)'; ctx.shadowBlur = 16; ctx.fillStyle = '#fff';
        const x = Math.max(tam * 1.5, Math.min(W - tam * 1.5, e.x));
        ctx.fillText(e.texto, x, e.y - f * 60);
      }
      ctx.restore();
    }
  }

  // ---------- Pantalla ----------
  let alAcabar = null;
  function jugar(fn) {
    alAcabar = fn;
    pantalla.hidden = false; document.body.classList.add('gl-jugando');
    medir(); nueva();
    avisoEl.hidden = false; avisoEl.innerHTML = '<b class="gl-num">3</b>';
    ultimo = 0; cancelAnimationFrame(raf); raf = requestAnimationFrame(paso);
  }
  function cerrar() {
    cancelAnimationFrame(raf); raf = 0; pantalla.hidden = true; avisoEl.hidden = true;
    document.body.classList.remove('gl-jugando');
  }
  function perder() {
    P.fin = true;
    avisar(`<p class="gl-titulo">¡Uy! Se han escapado ${AJ.fallosMax} globos</p><p>Has cazado ${P.cazadas.length} de ${n} palabras, pero hay que llegar al final.</p>`,
      [['Otra vez', () => jugar(alAcabar), 'primario'], ['Salir', cerrar]]);
  }
  function ganar() {
    P.fin = true;
    A.celebrar();
    const cazadas = P.cazadas.slice();
    avisar(`<p class="gl-titulo">¡Lo conseguiste!</p><p>Has cazado <b>${cazadas.length}</b> de ${n} palabras.</p>`,
      [[D.resuelta ? 'Salir' : 'Escribir la frase', () => { cerrar(); alAcabar && alAcabar(cazadas); }, 'primario']]);
  }
  $('gl-salir').addEventListener('click', cerrar);
  cv.addEventListener('pointerdown', toque, { passive: false });
  window.addEventListener('resize', () => { if (!pantalla.hidden) medir(); });

  // ---------- Escribir la frase (intentos con pistas) ----------
  let E = A.estado();
  if (!E || E.fase !== 'escribir') E = null;

  function huecos(i) {
    const w = objetivo[i];
    if (E.sabidas.includes(i)) return `<span class="gl-hueco bien">${visibles[i]}</span>`;
    const ini = E.intentos >= 2;
    return `<span class="gl-hueco">${[...w].map((ch, k) => `<i>${ini && k === 0 ? visibles[i][0] : ''}</i>`).join('')}</span>`;
  }
  function pintaEscribir() {
    inicio.hidden = true; escribir.hidden = false;
    const chips = baraja(E.palabras.slice()).map(i => `<span class="gl-chip">${visibles[i]}</span>`).join('');
    const perdidas = n - E.palabras.length;
    escribir.innerHTML = `
      <p class="adv-sobre">Tus palabras${perdidas ? ` · se te escaparon ${perdidas}` : ''}</p>
      <div class="gl-chips">${chips || '<span class="suave2">Ninguna… ¡tendrás que imaginarla!</span>'}</div>
      ${E.intentos > 0 ? `<div class="gl-plantilla">${objetivo.map((_, i) => huecos(i)).join('')}</div>` : ''}
      <p class="gl-ayuda">${ayuda()}</p>
      <textarea id="gl-frase" rows="2" autocomplete="off" autocapitalize="sentences" placeholder="Escribe la frase completa…"></textarea>
      <button class="btn adv-btn primario ancho" id="gl-comprobar" type="button">Comprobar</button>
      <button class="btn adv-btn fantasma pequeno ancho" id="gl-otra" type="button">Volver a jugar para cazar más</button>`;
    const ta = $('gl-frase'); ta.value = E.texto || '';
    $('gl-comprobar').addEventListener('click', comprobar);
    ta.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); comprobar(); } });
    $('gl-otra').addEventListener('click', () => jugar(cazadas => { E.palabras = [...new Set(E.palabras.concat(cazadas))]; guardarE(); pintaEscribir(); }));
  }
  function ayuda() {
    if (E.intentos === 0) return 'Ordénalas y completa lo que falte. Sin preocuparte de tildes ni comas.';
    if (E.intentos === 1) return 'Casi. Las verdes ya están en su sitio; los huecos te dicen cuántas letras tiene cada palabra.';
    if (E.intentos === 2) return 'Te doy la primera letra de cada palabra que falta.';
    return 'Te regalo una palabra más. ¡Ya casi!';
  }
  function guardarE() { A.guardarEstado(E); }
  async function comprobar() {
    const ta = $('gl-frase'), txt = ta.value.trim();
    if (!txt) return;
    const toks = txt.split(/\s+/).map(limpia).filter(Boolean);
    if (toks.length === n && toks.every((w, i) => w === objetivo[i])) {
      try {
        const r = await A.post('Resolver', { respuesta: txt, estado: JSON.stringify(E) });
        if (r.ok) { escribir.innerHTML = `<p class="adv-sobre centrado">La frase</p><p class="gl-frase-ok">${frase}</p>`; A.ganado(r); return; }
        A.aviso(r.mensaje, true);
      } catch { A.aviso('No se pudo comprobar', true); }
      return;
    }
    E.intentos++; E.texto = txt;
    toks.forEach((w, i) => { if (w === objetivo[i] && !E.sabidas.includes(i)) E.sabidas.push(i); });
    if (E.intentos >= 3) { const f = objetivo.findIndex((_, i) => !E.sabidas.includes(i)); if (f >= 0) E.sabidas.push(f); }
    guardarE(); pintaEscribir();
    A.aviso('Todavía no…', true);
  }

  $('gl-empezar').addEventListener('click', () => jugar(cazadas => {
    if (D.resuelta) return;
    E = { fase: 'escribir', palabras: cazadas, intentos: 0, sabidas: [], texto: '' };
    guardarE(); pintaEscribir();
    escribir.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }));
  if (E && !D.resuelta) pintaEscribir();
})();
