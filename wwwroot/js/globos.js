// Adviento · juego de los globos: explotarlos, cazar las palabras de los corazones dorados y escribir la frase.
(() => {
  const A = window.adv;
  if (!A || A.datos.tipo !== 'Globos') return;
  const D = A.datos;

  // ---------- Ajustes (afinar probando en el móvil) ----------
  const AJ = {
    fallosMax: 12,           // globos normales que pueden escaparse antes de perder
    premioBriska: 1,         // globos de margen extra por cazar a BriskaBird
    briskaVeces: 10,         // apariciones de BriskaBird, repartidas según las palabras ya cazadas
    briskaPausa: 2500,       // ms mínimos entre que se va y vuelve a salir
    globosPorPalabra: 7,     // globos normales entre corazón y corazón (± 2)
    globosFinales: 4,        // globos de propina tras cazar la última palabra
    maxGlobos: 12,           // tope de globos a la vez en pantalla (si hay más, espera para soltar otro)
    entradilla: 3200,        // ms que dura la presentación de BriskaBird al empezar (un toque la salta)
    // De ninguna palabra cazada (0) a todas menos una (último tramo); se interpola entre tramos.
    // intervalo ms entre globos · vel en alturas de pantalla por segundo · radio en fracción del ancho (máx. 480)
    // zigzag px · vidaPalabra ms que dura un corazón en pantalla
    niveles: [
      { intervalo: 800, vel: 0.23, radio: 0.105, zigzag: 6, vidaPalabra: 1800 },
      { intervalo: 620, vel: 0.30, radio: 0.092, zigzag: 16, vidaPalabra: 1500 },
      { intervalo: 500, vel: 0.37, radio: 0.083, zigzag: 25, vidaPalabra: 1250 },
      { intervalo: 410, vel: 0.44, radio: 0.075, zigzag: 34, vidaPalabra: 1050 },
    ],
    volMusica: 0.22,         // volumen de la musiquita de ladridos (0-1)
    pulso: [0.3, 0.23],      // segundos por negra de la música: al empezar y con todas las palabras cazadas
  };
  const bbImg = new Image(); bbImg.src = '/img/briskabird.png';

  // ---------- Sonido (todo sintetizado con Web Audio, sin ficheros) ----------
  let AC = null, salida = null, ruido = null, mudo = false;
  try { mudo = localStorage.getItem('gl-mudo') === '1'; } catch { }
  const audio = () => {
    try {
      if (!AC) {
        AC = new (window.AudioContext || window.webkitAudioContext)();
        salida = AC.createGain(); salida.gain.value = mudo ? 0 : 1; salida.connect(AC.destination);
        ruido = AC.createBuffer(1, Math.floor(AC.sampleRate * 0.15), AC.sampleRate);
        const d = ruido.getChannelData(0); for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
      }
      if (AC.state === 'suspended') AC.resume();
    } catch { AC = null; }
  };
  // Soplido de ruido filtrado (para ladridos y explosiones)
  function soplo(t, dur, tipo, frec, vol, dest) {
    const s = AC.createBufferSource(), f = AC.createBiquadFilter(), g = AC.createGain();
    s.buffer = ruido; f.type = tipo; f.frequency.value = frec; f.Q.value = 1.2;
    g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    s.connect(f); f.connect(g); g.connect(dest || salida); s.start(t); s.stop(t + dur + 0.02);
  }
  // Ladrido afinado: cae hasta la nota `frec`
  function ladra(t, frec, vol, dest) {
    const o = AC.createOscillator(), f = AC.createBiquadFilter(), g = AC.createGain();
    o.type = 'sawtooth'; o.frequency.setValueAtTime(frec * 1.7, t); o.frequency.exponentialRampToValueAtTime(frec, t + 0.05); o.frequency.exponentialRampToValueAtTime(frec * 0.75, t + 0.17);
    f.type = 'lowpass'; f.frequency.value = Math.min(2400, frec * 5); f.Q.value = 6;
    g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.012); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.18);
    o.connect(f); f.connect(g); g.connect(dest || salida); o.start(t); o.stop(t + 0.2);
    soplo(t, 0.09, 'bandpass', frec * 2.6, vol * 0.5, dest);
  }
  // Aullido afinado para las notas largas
  function aulla(t, frec, dur, vol, dest) {
    const o = AC.createOscillator(), g = AC.createGain(), lfo = AC.createOscillator(), lg = AC.createGain();
    o.type = 'triangle'; o.frequency.setValueAtTime(frec * 0.8, t); o.frequency.linearRampToValueAtTime(frec, t + 0.09); o.frequency.setValueAtTime(frec, t + dur * 0.8); o.frequency.linearRampToValueAtTime(frec * 0.9, t + dur);
    lfo.frequency.value = 7; lg.gain.value = frec * 0.03; lfo.connect(lg); lg.connect(o.frequency);
    g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.05); g.gain.setValueAtTime(vol, t + dur * 0.75); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(g); g.connect(dest || salida); o.start(t); lfo.start(t); o.stop(t + dur + 0.02); lfo.stop(t + dur + 0.02);
  }
  function guau(retraso = 0, tono = 1) { if (AC) ladra(AC.currentTime + retraso, 250 * tono, 0.5); }
  function gemido(retraso = 0, sube = true) {
    if (!AC) return;
    const c = AC, t = c.currentTime + retraso;
    const o = c.createOscillator(), g = c.createGain(), lfo = c.createOscillator(), lg = c.createGain();
    o.type = 'triangle';
    o.frequency.setValueAtTime(sube ? 650 : 1100, t); o.frequency.linearRampToValueAtTime(sube ? 1150 : 1250, t + 0.25); o.frequency.linearRampToValueAtTime(sube ? 900 : 480, t + 0.65);
    lfo.frequency.value = 9; lg.gain.value = 35; lfo.connect(lg); lg.connect(o.frequency);
    g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(0.25, t + 0.05); g.gain.setValueAtTime(0.25, t + 0.5); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.7);
    o.connect(g); g.connect(salida); o.start(t); lfo.start(t); o.stop(t + 0.72); lfo.stop(t + 0.72);
  }
  // Explosión de un globo: chasquido de ruido + golpe grave que cae. El corazón añade dos campanitas.
  function pum(dorado) {
    if (!AC) return;
    const t = AC.currentTime, k = 0.85 + Math.random() * 0.4;
    soplo(t, 0.1, 'highpass', 700 * k, 0.7);
    const o = AC.createOscillator(), g = AC.createGain();
    o.type = 'sine'; o.frequency.setValueAtTime(380 * k, t); o.frequency.exponentialRampToValueAtTime(60, t + 0.1);
    g.gain.setValueAtTime(0.6, t); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.12);
    o.connect(g); g.connect(salida); o.start(t); o.stop(t + 0.14);
    if (dorado) for (const [f, r] of [[1319, 0.03], [1760, 0.13]]) {
      const c = AC.createOscillator(), cg = AC.createGain();
      c.type = 'sine'; c.frequency.value = f;
      cg.gain.setValueAtTime(0.0001, t + r); cg.gain.exponentialRampToValueAtTime(0.3, t + r + 0.01); cg.gain.exponentialRampToValueAtTime(0.0001, t + r + 0.4);
      c.connect(cg); cg.connect(salida); c.start(t + r); c.stop(t + r + 0.42);
    }
  }

  // Musiquita de fondo: el estribillo de «Jingle Bells» ladrado (notas cortas = ladridos, largas = aullidos)
  // sobre un chim-pum de ladridos graves. Va en bucle y se acelera según se cazan palabras.
  const MUSICA = (() => {
    const E = 64, F = 65, G = 67, C = 60, Dn = 62;
    const mel = [[E, 1], [E, 1], [E, 2], [E, 1], [E, 1], [E, 2], [E, 1], [G, 1], [C, 1.5], [Dn, 0.5], [E, 4],
      [F, 1], [F, 1], [F, 1.5], [F, 0.5], [F, 1], [E, 1], [E, 1], [E, 0.5], [E, 0.5], [E, 1], [Dn, 1], [Dn, 1], [E, 1], [Dn, 2], [G, 2],
      [E, 1], [E, 1], [E, 2], [E, 1], [E, 1], [E, 2], [E, 1], [G, 1], [C, 1.5], [Dn, 0.5], [E, 4],
      [F, 1], [F, 1], [F, 1.5], [F, 0.5], [F, 1], [E, 1], [E, 1], [E, 0.5], [E, 0.5], [G, 1], [G, 1], [F, 1], [Dn, 1], [C, 4]];
    const bajos = [48, 48, 48, 48, 53, 48, 50, 43, 48, 48, 48, 48, 53, 48, 43, 48];   // raíz de cada compás
    const ev = []; let b = 0;
    for (const [m, d] of mel) { ev.push({ b, m, d }); b += d; }
    bajos.forEach((m, i) => { ev.push({ b: i * 4, m, bajo: true }); ev.push({ b: i * 4 + 2, m: m + 7, bajo: true }); });
    ev.sort((x, y) => x.b - y.b);
    return { ev, total: b };
  })();
  const hz = m => 440 * Math.pow(2, (m - 69) / 12);
  let mus = null;   // { g, i, t, reloj }
  function programaMusica() {
    if (!mus || !AC || AC.state !== 'running') return;
    const ahora = AC.currentTime, pr = P ? Math.min(1, P.cazadas.length / Math.max(1, n)) : 0;
    const pulso = AJ.pulso[0] + (AJ.pulso[1] - AJ.pulso[0]) * pr;
    if (mus.t < ahora) mus.t = ahora + 0.05;   // si la pestaña estuvo parada, no se recupera lo atrasado
    while (mus.t < ahora + 0.35) {
      const e = MUSICA.ev[mus.i];
      if (e.bajo) ladra(mus.t, hz(e.m), 0.5, mus.g);
      else if (e.d >= 2) aulla(mus.t, hz(e.m), e.d * pulso * 0.9, 0.5, mus.g);
      else ladra(mus.t, hz(e.m), 0.8, mus.g);
      const sig = (mus.i + 1) % MUSICA.ev.length;
      const salto = sig ? MUSICA.ev[sig].b - e.b : MUSICA.total - e.b;
      mus.i = sig; mus.t += salto * pulso;
    }
  }
  function musica(on) {
    if (mus) {
      clearInterval(mus.reloj);
      const g = mus.g; mus = null;
      try { g.gain.cancelScheduledValues(AC.currentTime); g.gain.setTargetAtTime(0, AC.currentTime, 0.03); setTimeout(() => g.disconnect(), 600); } catch { }
    }
    if (!on || !AC) return;
    const g = AC.createGain(); g.gain.value = AJ.volMusica; g.connect(salida);
    mus = { g, i: 0, t: AC.currentTime + 0.15, reloj: setInterval(programaMusica, 100) };
  }
  function silenciar(v) {
    mudo = v;
    try { localStorage.setItem('gl-mudo', v ? '1' : '0'); } catch { }
    if (salida) salida.gain.setTargetAtTime(v ? 0 : 1, AC.currentTime, 0.02);
    pintaMudo();
  }
  function pintaMudo() { const b = document.getElementById('gl-mudo'); if (b) { b.textContent = mudo ? '🔇' : '🔊'; b.setAttribute('aria-label', mudo ? 'Activar el sonido' : 'Silenciar'); b.setAttribute('aria-pressed', mudo ? 'true' : 'false'); } }

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
    const L = AJ.niveles, pos = Math.min(1, P.cazadas.length / Math.max(1, n - 1)) * (L.length - 1);
    const i = Math.min(L.length - 2, Math.floor(pos)), f = pos - i, a = L[i], b = L[i + 1];
    const r = {}; for (const k in a) r[k] = lerp(a[k], b[k], f); return r;
  }

  function nueva() {
    const mitad = Math.ceil(n / 2);
    const orden = baraja([...Array(mitad).keys()]).concat(baraja([...Array(n - mitad).keys()].map(i => i + mitad)));
    const tipos = baraja(['vuelo', 'salto', 'asoma']);
    // BriskaBird: la aparición k sale cuando ya hay cazadas ⌊(k + ½)·n / veces⌋ palabras (con 11: 0, 1, 2, 3, 4, 6, 7, 8, 9 y 10)
    const bbPend = Array.from({ length: AJ.briskaVeces }, (_, k) => ({ en: Math.floor((k + 0.5) * n / AJ.briskaVeces), tipo: tipos[k % 3], k }));
    // cola: palabras que quedan por cazar; la que se escapa vuelve al final de la cola
    P = { t: 0, real: 0, maxF: AJ.fallosMax, bb: null, bbPend, bbFin: 0, globos: [], efectos: [], cola: orden, veces: {}, cazadas: [], perdidas: 0, fallos: 0, factor: 1,
          proximo: 600, hastaPalabra: AJ.globosPorPalabra, finales: AJ.globosFinales, fin: false, intro: AJ.entradilla, cuenta: 3.2 };
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
    if (P.cola.length && --P.hastaPalabra <= 0) {
      // Corazón dorado escondido entre un racimo de globos
      const r = nv.radio * Math.min(W, 480);
      const gx = r * 2.4 + Math.random() * Math.max(1, W - r * 4.8), gy = H * (0.32 + Math.random() * 0.36);
      const c = globo(gx, gy, nv, 'p'); c.idx = P.cola.shift();
      c.vida *= 1 + 0.3 * (P.veces[c.idx] || 0);   // cada vez que se escapa una palabra, la siguiente vez dura más
      P.globos.push(c);
      for (const [dx, dy] of [[-2, 0.6], [2, 0.4], [-0.9, 2], [1.1, -1.6]].slice(0, 3 + Math.round(Math.random())))
        P.globos.push(globo(Math.max(r, Math.min(W - r, gx + dx * r * (0.85 + Math.random() * 0.3))), gy + dy * r, nv, 'n'));
      P.hastaPalabra = AJ.globosPorPalabra + Math.round(Math.random() * 4 - 2);
    } else if (P.cazadas.length < n || P.finales-- > 0) {
      const r = nv.radio * Math.min(W, 480);
      P.globos.push(globo(r + Math.random() * (W - 2 * r), H + r * 1.2, nv, 'n'));
    }
    P.proximo = P.t + nv.intervalo * (0.75 + Math.random() * 0.5);
  }

  // Dificultad adaptable: cada palabra cazada acorta un poco la vida de los corazones; cada una que se escapa la alarga.
  function adaptar(cazada) {
    P.factor = cazada ? Math.max(0.7, P.factor * 0.93) : Math.min(2.2, P.factor * 1.2);
  }

  function explota(g) {
    const m = P.efectos.length > 60 ? 4 : 8;
    for (let i = 0; i < m; i++) {
      const a = Math.random() * 6.3, v = 80 + Math.random() * 160;
      P.efectos.push({ tipo: 'trozo', x: g.x, y: g.y, vx: Math.cos(a) * v, vy: Math.sin(a) * v, col: g.tipo === 'p' ? '#ffd45e' : g.col, t0: P.real, dur: 500 });
    }
    pum(g.tipo === 'p');
    if (navigator.vibrate) navigator.vibrate(g.tipo === 'p' ? 25 : 8);
  }

  function toque(e) {
    if (!P || P.fin || P.intro > 0 || P.cuenta > 0) return;
    e.preventDefault();
    const rc = cv.getBoundingClientRect(), x = e.clientX - rc.left, y = e.clientY - rc.top;
    if (P.bb && tocaBriska(x, y)) return;
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
      P.cazadas.push(g.idx); adaptar(true);
      P.efectos.push({ tipo: 'palabra', x: g.x, y: g.y, texto: visibles[g.idx], t0: P.real, dur: 1300 });
      const ch = document.createElement('span'); ch.className = 'gl-chip'; ch.textContent = visibles[g.idx]; bandeja.appendChild(ch);
      pintaHud();
    }
  }

  function pintaHud() {
    hudFallos.innerHTML = '';
    for (let i = 0; i < P.maxF; i++) { const s = document.createElement('i'); s.className = (i >= AJ.fallosMax ? 'extra ' : '') + (i < P.fallos ? 'fuera' : ''); hudFallos.appendChild(s); }
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
    const dt = Math.min(50, ahora - (ultimo || ahora)); ultimo = ahora; P.real += dt;
    if (P.intro > 0) { P.intro -= dt; if (P.intro <= 0) avisoEl.innerHTML = '<b class="gl-num">3</b>'; }
    else if (P.cuenta > 0) { P.cuenta -= dt / 1000; if (P.cuenta <= 0) avisoEl.hidden = true; else avisoEl.innerHTML = `<b class="gl-num">${Math.ceil(P.cuenta - 0.2) || '¡Ya!'}</b>`; }
    else if (!P.fin) {
      P.t += dt;
      // BriskaBird sale sin avisar y sin parar los globos
      if (P.bb) pasoBriska(dt);
      else if (P.bbPend.length && P.cazadas.length >= P.bbPend[0].en && P.t - P.bbFin >= AJ.briskaPausa) empiezaBriska(P.bbPend.shift());
      if (P.t >= P.proximo) { if (P.globos.length < AJ.maxGlobos) soltar(); else P.proximo = P.t + 120; }
      const s = dt / 1000;
      for (let i = P.globos.length - 1; i >= 0; i--) {
        const g = P.globos[i];
        g.y -= g.vy * s;
        g.x = g.x0 + Math.sin((P.t / 1000) * g.fr + g.fase) * g.amp;
        if (g.tipo === 'n' && g.y + g.r * 2.2 < 0) {
          P.globos.splice(i, 1); P.fallos++; pintaHud();
          hudFallos.classList.remove('sacude'); void hudFallos.offsetWidth; hudFallos.classList.add('sacude');
          if (P.fallos >= P.maxF) { perder(); break; }
        } else if (g.tipo === 'p' && (P.t - g.nace > g.vida || g.y + g.r < 0)) {
          P.globos.splice(i, 1); P.perdidas++; adaptar(false);
          P.veces[g.idx] = (P.veces[g.idx] || 0) + 1; P.cola.push(g.idx);   // volverá a salir
          P.efectos.push({ tipo: 'puf', x: g.x, y: g.y, r: g.r, t0: P.real, dur: 450 });
        }
      }
      // Solo se acaba con TODAS las palabras cazadas (y Briska ya ha hecho todas sus visitas)
      if (!P.fin && P.cazadas.length >= n && P.finales <= 0 && !P.globos.length && !P.bb && !P.bbPend.length) ganar();
    }
    dibujar();
    raf = requestAnimationFrame(paso);
  }

  // ---------- BriskaBird: se presenta una vez al empezar; luego sale 10 veces sin aviso ni pausa ----------
  const tamBB = () => Math.min(W, 480) * 0.28;
  const azar = (a, b) => a + Math.random() * (b - a);
  function empiezaBriska(p) {
    const S = tamBB(), segs = [];
    if (p.tipo === 'vuelo') for (let i = 0; i < 3; i++) segs.push({ dir: i % 2 ? -1 : 1, y: H * azar(0.2, 0.7) });
    if (p.tipo === 'salto') for (let i = 0; i < 5; i++) segs.push({ x: azar(S * 0.6, W - S * 0.6), y: azar(H * 0.15, H * 0.75), vx: azar(-1, 1) * S * 0.8, vy: azar(-1, 1) * S * 0.8 });
    if (p.tipo === 'asoma') for (let i = 0; i < 4; i++) segs.push({ borde: ['izq', 'der', 'abajo'][Math.floor(Math.random() * 3)], pos: azar(0.25, 0.75) });
    P.bb = { ...p, segs, t: 0, sonado: -1 };
  }
  // Dónde está en el instante t de su número (null si ya terminó)
  function posBriska(B, t) {
    const S = tamBB();
    if (B.tipo === 'vuelo') {
      const dur = 950, per = 1150, i = Math.floor(t / per); if (t < 0 || i >= B.segs.length) return null;
      const lt = t - i * per, s = B.segs[i]; if (lt > dur) return { visible: false, seg: i };
      const p = lt / dur, rec = p * (W + 2 * S);
      return { visible: true, seg: i, x: s.dir > 0 ? -S + rec : W + S - rec, y: s.y + Math.sin(p * Math.PI * 4) * S * 0.35, flip: s.dir < 0, rot: Math.sin(p * Math.PI * 8) * 0.12, esc: 1 };
    }
    if (B.tipo === 'salto') {
      const vis = 500, per = 680, i = Math.floor(t / per); if (t < 0 || i >= B.segs.length) return null;
      const lt = t - i * per, s = B.segs[i]; if (lt > vis) return { visible: false, seg: i };
      return { visible: true, seg: i, x: s.x + s.vx * lt / 1000, y: s.y + s.vy * lt / 1000, flip: s.vx < 0, rot: Math.sin(lt / 40) * 0.15, esc: Math.min(1, lt / 80) };
    }
    const per = 1000, i = Math.floor(t / per); if (t < 0 || i >= B.segs.length) return null;
    const lt = t - i * per, s = B.segs[i];
    const k = lt < 250 ? lt / 250 : lt < 600 ? 1 : lt < 850 ? 1 - (lt - 600) / 250 : 0;
    if (k <= 0) return { visible: false, seg: i };
    if (s.borde === 'izq') return { visible: true, seg: i, x: -S * 0.5 + k * S * 0.85, y: H * s.pos, flip: false, rot: 0.25, esc: 1 };
    if (s.borde === 'der') return { visible: true, seg: i, x: W + S * 0.5 - k * S * 0.85, y: H * s.pos, flip: true, rot: -0.25, esc: 1 };
    return { visible: true, seg: i, x: W * s.pos, y: H + S * 0.55 - k * S * 0.95, flip: s.pos > 0.5, rot: 0, esc: 1 };
  }
  function pasoBriska(dt) {
    const B = P.bb; B.t += dt;
    const p = posBriska(B, B.t);
    if (!p) { P.bb = null; P.bbFin = P.t; gemido(0, false); return; }
    if (p.visible && p.seg !== B.sonado) {
      B.sonado = p.seg;
      if (B.tipo === 'vuelo') guau(0, 1.1); else if (B.tipo === 'salto') guau(0, 1.35); else gemido(0, true);
    }
  }
  function tocaBriska(x, y) {
    const B = P.bb, S = tamBB();
    for (const atras of [0, 120]) {
      const p = posBriska(B, B.t - atras);
      if (!p || !p.visible || Math.hypot(p.x - x, p.y - y) > S * 0.55) continue;
      P.bb = null; P.bbFin = P.t; P.maxF += AJ.premioBriska; pintaHud();
      guau(0, 1.3); guau(0.15, 1.45); guau(0.3, 1.6);
      if (navigator.vibrate) navigator.vibrate([30, 40, 30]);
      P.efectos.push({ tipo: 'bbfuera', x: p.x, y: p.y, flip: p.flip, t0: P.real, dur: 700 });
      P.efectos.push({ tipo: 'palabra', x: p.x, y: p.y - S * 0.4, texto: `+${AJ.premioBriska} 🎈`, t0: P.real, dur: 1300 });
      return true;
    }
    return false;
  }
  function dibujaBriska(p, extraRot = 0, extraEsc = 1, alfa = 1) {
    if (!bbImg.complete || !bbImg.naturalWidth) return;
    const S = tamBB() * p.esc * extraEsc, al = S * bbImg.naturalHeight / bbImg.naturalWidth;
    ctx.save(); ctx.globalAlpha = alfa; ctx.translate(p.x, p.y); ctx.rotate(p.rot + extraRot); if (p.flip) ctx.scale(-1, 1);
    ctx.drawImage(bbImg, -S / 2, -al / 2, S, al); ctx.restore();
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
    if (P.bb) { const p = posBriska(P.bb, P.bb.t); if (p && p.visible) dibujaBriska(p); }
    for (let i = P.efectos.length - 1; i >= 0; i--) {
      const e = P.efectos[i], f = (P.real - e.t0) / e.dur;
      if (f >= 1) { P.efectos.splice(i, 1); continue; }
      ctx.save(); ctx.globalAlpha = 1 - f;
      if (e.tipo === 'trozo') {
        const s = (P.real - e.t0) / 1000;
        ctx.fillStyle = e.col; ctx.fillRect(e.x + e.vx * s - 3, e.y + e.vy * s + 300 * s * s - 3, 6, 6);
      } else if (e.tipo === 'puf') {
        ctx.strokeStyle = 'rgba(255,220,120,.8)'; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.arc(e.x, e.y, e.r * (0.6 + f * 0.8), 0, 6.2832); ctx.stroke();
      } else if (e.tipo === 'bbfuera') {
        ctx.restore(); dibujaBriska({ x: e.x, y: e.y - f * 80, flip: e.flip, rot: 0, esc: 1 }, f * 9, 1 - f * 0.8, 1 - f); continue;
      } else if (e.tipo === 'palabra') {
        const tam = Math.round(Math.min(W, 480) * (e.peq ? 0.07 : 0.11));
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
    alAcabar = fn; audio(); musica(true);
    pantalla.hidden = false; document.body.classList.add('gl-jugando');
    medir(); nueva();
    // Única presentación de BriskaBird en toda la partida
    avisar(`<img class="gl-bb-img" src="${bbImg.src}" alt="" /><p class="gl-titulo">¡Cuidado con BriskaBird!</p><p>Mitad labrador, mitad pájaro y cien por cien hambre. Saldrá ${AJ.briskaVeces} veces, sin avisar.</p><p class="gl-bb-premio">Cada vez que la caces: +${AJ.premioBriska} globo de margen</p>`);
    guau(0.1); guau(0.32, 0.95);
    ultimo = 0; cancelAnimationFrame(raf); raf = requestAnimationFrame(paso);
  }
  function cerrar() {
    musica(false);
    cancelAnimationFrame(raf); raf = 0; pantalla.hidden = true; avisoEl.hidden = true;
    document.body.classList.remove('gl-jugando');
  }
  function perder() {
    P.fin = true; musica(false); gemido(0.1, false);
    avisar(`<p class="gl-titulo">¡Uy! Se han escapado ${P.fallos} globos</p><p>Llevabas ${P.cazadas.length} de ${n} palabras, y hay que cazarlas todas.</p>`,
      [['Otra vez', () => jugar(alAcabar), 'primario'], ['Salir', cerrar]]);
  }
  function ganar() {
    P.fin = true; musica(false); guau(0.05, 1.3); guau(0.2, 1.45); guau(0.35, 1.75);
    A.celebrar();
    const cazadas = P.cazadas.slice();
    avisar(`<p class="gl-titulo">¡Lo conseguiste!</p><p>Has cazado las <b>${n}</b> palabras.</p>`,
      [[D.resuelta ? 'Salir' : 'Escribir la frase', () => { cerrar(); alAcabar && alAcabar(cazadas); }, 'primario']]);
  }
  // Un toque sobre la presentación la salta (con un margen para no saltarla sin querer)
  avisoEl.addEventListener('click', () => { if (P && P.intro > 0 && P.intro < AJ.entradilla - 500) P.intro = 0.001; });
  const btnMudo = $('gl-mudo');
  if (btnMudo) { pintaMudo(); btnMudo.addEventListener('click', () => { audio(); silenciar(!mudo); }); }
  // Al salir de la página o esconderla (cambio de app, pantalla apagada), el sonido se calla
  document.addEventListener('visibilitychange', () => {
    if (!AC) return;
    try { if (document.hidden) AC.suspend(); else if (!pantalla.hidden) AC.resume(); } catch { }
  });
  window.addEventListener('pagehide', () => musica(false));
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
    const ver = !E.ocultas;   // las palabras se ven una sola vez y se van esfumando
    const chips = ver ? baraja(E.palabras.slice()).map(i => `<span class="gl-chip">${visibles[i]}</span>`).join('') : '';
    const perdidas = n - E.palabras.length;
    const sinChips = ver ? '<span class="suave2">Ninguna… ¡tendrás que imaginarla!</span>' : `<span class="suave2">Tus ${E.palabras.length} palabras se esfumaron. Tira de memoria 🐾</span>`;
    escribir.innerHTML = `
      <p class="adv-sobre">Tus palabras${perdidas ? ` · se te escaparon ${perdidas}` : ''}${ver && chips ? ' · ¡memorízalas!' : ''}</p>
      <div class="gl-chips" id="gl-chips">${chips || sinChips}</div>
      ${E.intentos > 0 ? `<div class="gl-plantilla">${objetivo.map((_, i) => huecos(i)).join('')}</div>` : ''}
      <p class="gl-ayuda">${ayuda()}</p>
      <textarea id="gl-frase" rows="2" autocomplete="off" autocapitalize="sentences" placeholder="Escribe la frase completa…"></textarea>
      <button class="btn adv-btn primario ancho" id="gl-comprobar" type="button">Comprobar</button>
      <button class="btn adv-btn fantasma pequeno ancho" id="gl-otra" type="button">Volver a jugar para verlas otra vez</button>`;
    const ta = $('gl-frase'); ta.value = E.texto || '';
    $('gl-comprobar').addEventListener('click', comprobar);
    ta.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); comprobar(); } });
    $('gl-otra').addEventListener('click', () => jugar(cazadas => { E.palabras = [...new Set(E.palabras.concat(cazadas))]; E.ocultas = false; guardarE(); pintaEscribir(); }));
    if (ver && chips) { E.ocultas = true; guardarE(); esfumar(); }
  }
  // Tras 2,5 s, una palabra menos cada segundo
  let esfumando = 0;
  function esfumar() {
    clearTimeout(esfumando);
    const quita = () => {
      const caja = $('gl-chips'), vivos = caja ? [...caja.querySelectorAll('.gl-chip:not(.esfuma)')] : [];
      if (!vivos.length) { if (caja) caja.innerHTML = `<span class="suave2">Tus ${E.palabras.length} palabras se esfumaron. Tira de memoria 🐾</span>`; return; }
      const c = vivos[Math.floor(Math.random() * vivos.length)]; c.classList.add('esfuma');
      esfumando = setTimeout(quita, 1000);
    };
    esfumando = setTimeout(quita, 2500);
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
