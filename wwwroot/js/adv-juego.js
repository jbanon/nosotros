// Adviento · trozos comunes de los juegos en canvas (globos, arkanoid, comecocos): sonido sintetizado con Web Audio
// y la fase final de escribir la frase (palabras que se esfuman, intentos con pistas, comprobación en el servidor).
// Se carga después de adviento.js (usa window.adv) y antes del juego, que llama a window.advJuego.
(() => {
  const A = window.adv;
  if (!A) return;

  // ---------- Sonido (todo sintetizado, sin ficheros) ----------
  // opc.boton: botón 🔊/🔇 · opc.activa(): si la pantalla del juego está abierta (para reanudar al volver a la pestaña)
  function sonido(opc) {
    opc = opc || {};
    const S = { AC: null, salida: null, ruido: null, mudo: false };
    try { S.mudo = localStorage.getItem('gl-mudo') === '1'; } catch { }
    S.audio = () => {
      try {
        if (!S.AC) {
          S.AC = new (window.AudioContext || window.webkitAudioContext)();
          S.salida = S.AC.createGain(); S.salida.gain.value = S.mudo ? 0 : 1; S.salida.connect(S.AC.destination);
          S.ruido = S.AC.createBuffer(1, Math.floor(S.AC.sampleRate * 0.15), S.AC.sampleRate);
          const d = S.ruido.getChannelData(0); for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
        }
        if (S.AC.state === 'suspended') S.AC.resume();
      } catch { S.AC = null; }
    };
    // Soplido de ruido filtrado (para ladridos y explosiones)
    S.soplo = (t, dur, tipo, frec, vol, dest) => {
      const AC = S.AC, s = AC.createBufferSource(), f = AC.createBiquadFilter(), g = AC.createGain();
      s.buffer = S.ruido; f.type = tipo; f.frequency.value = frec; f.Q.value = 1.2;
      g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
      s.connect(f); f.connect(g); g.connect(dest || S.salida); s.start(t); s.stop(t + dur + 0.02);
    };
    // Ladrido afinado: cae hasta la nota `frec`
    S.ladra = (t, frec, vol, dest) => {
      const AC = S.AC, o = AC.createOscillator(), f = AC.createBiquadFilter(), g = AC.createGain();
      o.type = 'sawtooth'; o.frequency.setValueAtTime(frec * 1.7, t); o.frequency.exponentialRampToValueAtTime(frec, t + 0.05); o.frequency.exponentialRampToValueAtTime(frec * 0.75, t + 0.17);
      f.type = 'lowpass'; f.frequency.value = Math.min(2400, frec * 5); f.Q.value = 6;
      g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.012); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.18);
      o.connect(f); f.connect(g); g.connect(dest || S.salida); o.start(t); o.stop(t + 0.2);
      S.soplo(t, 0.09, 'bandpass', frec * 2.6, vol * 0.5, dest);
    };
    // Aullido afinado para las notas largas
    S.aulla = (t, frec, dur, vol, dest) => {
      const AC = S.AC, o = AC.createOscillator(), g = AC.createGain(), lfo = AC.createOscillator(), lg = AC.createGain();
      o.type = 'triangle'; o.frequency.setValueAtTime(frec * 0.8, t); o.frequency.linearRampToValueAtTime(frec, t + 0.09); o.frequency.setValueAtTime(frec, t + dur * 0.8); o.frequency.linearRampToValueAtTime(frec * 0.9, t + dur);
      lfo.frequency.value = 7; lg.gain.value = frec * 0.03; lfo.connect(lg); lg.connect(o.frequency);
      g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.05); g.gain.setValueAtTime(vol, t + dur * 0.75); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
      o.connect(g); g.connect(dest || S.salida); o.start(t); lfo.start(t); o.stop(t + dur + 0.02); lfo.stop(t + dur + 0.02);
    };
    S.guau = (retraso = 0, tono = 1) => { if (S.AC) S.ladra(S.AC.currentTime + retraso, 250 * tono, 0.5); };
    S.gemido = (retraso = 0, sube = true) => {
      if (!S.AC) return;
      const c = S.AC, t = c.currentTime + retraso;
      const o = c.createOscillator(), g = c.createGain(), lfo = c.createOscillator(), lg = c.createGain();
      o.type = 'triangle';
      o.frequency.setValueAtTime(sube ? 650 : 1100, t); o.frequency.linearRampToValueAtTime(sube ? 1150 : 1250, t + 0.25); o.frequency.linearRampToValueAtTime(sube ? 900 : 480, t + 0.65);
      lfo.frequency.value = 9; lg.gain.value = 35; lfo.connect(lg); lg.connect(o.frequency);
      g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(0.25, t + 0.05); g.gain.setValueAtTime(0.25, t + 0.5); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.7);
      o.connect(g); g.connect(S.salida); o.start(t); lfo.start(t); o.stop(t + 0.72); lfo.stop(t + 0.72);
    };
    // Explosión: chasquido de ruido + golpe grave que cae. Con `dorado`, dos campanitas.
    S.pum = (dorado, tono = 1) => {
      if (!S.AC) return;
      const AC = S.AC, t = AC.currentTime, k = (0.85 + Math.random() * 0.4) * tono;
      S.soplo(t, 0.1, 'highpass', 700 * k, 0.7);
      const o = AC.createOscillator(), g = AC.createGain();
      o.type = 'sine'; o.frequency.setValueAtTime(380 * k, t); o.frequency.exponentialRampToValueAtTime(60, t + 0.1);
      g.gain.setValueAtTime(0.6, t); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.12);
      o.connect(g); g.connect(S.salida); o.start(t); o.stop(t + 0.14);
      if (dorado) for (const [f, r] of [[1319, 0.03], [1760, 0.13]]) {
        const c = AC.createOscillator(), cg = AC.createGain();
        c.type = 'sine'; c.frequency.value = f;
        cg.gain.setValueAtTime(0.0001, t + r); cg.gain.exponentialRampToValueAtTime(0.3, t + r + 0.01); cg.gain.exponentialRampToValueAtTime(0.0001, t + r + 0.4);
        c.connect(cg); cg.connect(S.salida); c.start(t + r); c.stop(t + r + 0.42);
      }
    };
    // Toque corto afinado (rebotes de la pelota)
    S.tic = (frec, vol = 0.25, dur = 0.07) => {
      if (!S.AC) return;
      const AC = S.AC, t = AC.currentTime, o = AC.createOscillator(), g = AC.createGain();
      o.type = 'triangle'; o.frequency.setValueAtTime(frec, t); o.frequency.exponentialRampToValueAtTime(frec * 0.7, t + dur);
      g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
      o.connect(g); g.connect(S.salida); o.start(t); o.stop(t + dur + 0.02);
    };
    // Boing de saltito: nota corta con glissando ascendente (sube desde media octava por debajo) y un pelín de caída al final.
    // `t` es tiempo absoluto del AudioContext, para poder encadenar varios (música de saltitos del comecocos).
    S.boing = (t, frec, dur = 0.2, vol = 0.3, tipo = 'triangle', dest) => {
      const AC = S.AC, o = AC.createOscillator(), g = AC.createGain();
      o.type = tipo; o.frequency.setValueAtTime(frec * 0.55, t); o.frequency.exponentialRampToValueAtTime(frec, t + dur * 0.5); o.frequency.exponentialRampToValueAtTime(frec * 0.94, t + dur);
      g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.012); g.gain.setValueAtTime(vol, t + dur * 0.45); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
      o.connect(g); g.connect(dest || S.salida); o.start(t); o.stop(t + dur + 0.02);
    };
    const pinta = () => { const b = opc.boton; if (b) { b.textContent = S.mudo ? '🔇' : '🔊'; b.setAttribute('aria-label', S.mudo ? 'Activar el sonido' : 'Silenciar'); b.setAttribute('aria-pressed', S.mudo ? 'true' : 'false'); } };
    S.silenciar = v => {
      S.mudo = v;
      try { localStorage.setItem('gl-mudo', v ? '1' : '0'); } catch { }
      if (S.salida) S.salida.gain.setTargetAtTime(v ? 0 : 1, S.AC.currentTime, 0.02);
      pinta();
    };
    if (opc.boton) { pinta(); opc.boton.addEventListener('click', () => { S.audio(); S.silenciar(!S.mudo); }); }
    // Al esconder la página (cambio de app, pantalla apagada), el sonido se calla
    document.addEventListener('visibilitychange', () => {
      if (!S.AC) return;
      try { if (document.hidden) S.AC.suspend(); else if (!opc.activa || opc.activa()) S.AC.resume(); } catch { }
    });
    return S;
  }

  // ---------- Escribir la frase (intentos con pistas) ----------
  // opc: { frase, inicio, escribir, jugar(fn), esfumaEspera, esfumaCada }
  //   inicio/escribir: elementos de la tarjeta (el de las reglas y el de escribir)
  //   jugar(fn): arranca otra partida; al ganar llama a fn(cazadas) con los índices de las palabras cazadas
  // Devuelve { limpia, visibles, objetivo, n, baraja, empezar(cazadas), estado() }.
  function frase(opc) {
    const D = A.datos, $ = id => document.getElementById(id);
    const limpia = w => A.norm(w).replace(/[^A-ZÑ]/g, '');
    const visibles = (opc.frase || '').split(/\s+/).map(w => w.replace(/[^\p{L}]/gu, '')).filter(Boolean);   // "vida," → "vida"
    const objetivo = visibles.map(limpia);
    const n = objetivo.length;
    const baraja = a => { for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; };

    let E = A.estado();
    if (!E || E.fase !== 'escribir') E = null;

    function huecos(i) {
      const w = objetivo[i];
      if (E.sabidas.includes(i)) return `<span class="gl-hueco bien">${visibles[i]}</span>`;
      const ini = E.intentos >= 2;
      return `<span class="gl-hueco">${[...w].map((ch, k) => `<i>${ini && k === 0 ? visibles[i][0] : ''}</i>`).join('')}</span>`;
    }
    function pintaEscribir() {
      opc.inicio.hidden = true; opc.escribir.hidden = false;
      const ver = !E.ocultas;   // las palabras se ven una sola vez y se van esfumando
      const chips = ver ? baraja(E.palabras.slice()).map(i => `<span class="gl-chip">${visibles[i]}</span>`).join('') : '';
      const perdidas = n - E.palabras.length;
      const sinChips = ver ? '<span class="suave2">Ninguna… ¡tendrás que imaginarla!</span>' : `<span class="suave2">Tus ${E.palabras.length} palabras se esfumaron. Tira de memoria 🐾</span>`;
      opc.escribir.innerHTML = `
        <p class="adv-sobre">Tus palabras${perdidas ? ` · se te escaparon ${perdidas}` : ''}${ver && chips ? ' · ¡memorízalas!' : ''}</p>
        <div class="gl-chips" id="gl-chips">${chips || sinChips}</div>
        ${E.intentos > 0 ? `<div class="gl-plantilla">${objetivo.map((_, i) => huecos(i)).join('')}</div>` : ''}
        <p class="gl-ayuda">${ayuda()}</p>
        <textarea id="gl-frase" rows="2" autocomplete="off" autocapitalize="sentences" placeholder="Escribe la frase completa…"></textarea>
        <button class="btn adv-btn primario ancho" id="gl-comprobar" type="button">Comprobar</button>
        ${E.intentos >= 3 && ayudaDe() === 'comodin' && !E.comodin ? '<button class="btn adv-btn ancho gl-btn-comodin" id="gl-comodin" type="button">🃏 Comodín: ver las palabras en orden</button>' : ''}
        ${E.intentos >= 3 && ayudaDe() === 'caza' && E.sabidas.length < n ? `<button class="btn adv-btn ancho gl-btn-comodin" id="gl-caza" type="button">🎯 Pista: caza palabras (${Math.round(rondaMs() / 1000)} s)</button>` : ''}
        ${D.diario ? '' : '<button class="btn adv-btn fantasma pequeno ancho" id="gl-otra" type="button">Volver a jugar para verlas otra vez</button>'}`;
      const ta = $('gl-frase'); ta.value = E.texto || '';
      $('gl-comprobar').addEventListener('click', comprobar);
      if ($('gl-comodin')) $('gl-comodin').addEventListener('click', comodin);
      if ($('gl-caza')) $('gl-caza').addEventListener('click', caza);
      ta.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); comprobar(); } });
      if ($('gl-otra')) $('gl-otra').addEventListener('click', () => opc.jugar(cazadas => { E.palabras = [...new Set(E.palabras.concat(cazadas))]; E.ocultas = false; guardarE(); pintaEscribir(); }));
      if (ver && chips) { E.ocultas = true; guardarE(); esfumar(); }
    }
    // Tras la espera, se va esfumando una palabra cada poco (opc.esfumaEspera y opc.esfumaCada)
    let esfumando = 0;
    function esfumar() {
      clearTimeout(esfumando);
      const quita = () => {
        const caja = $('gl-chips'), vivos = caja ? [...caja.querySelectorAll('.gl-chip:not(.esfuma)')] : [];
        if (!vivos.length) { if (caja) caja.innerHTML = `<span class="suave2">Tus ${E.palabras.length} palabras se esfumaron. Tira de memoria 🐾</span>`; return; }
        const c = vivos[Math.floor(Math.random() * vivos.length)]; c.classList.add('esfuma');
        esfumando = setTimeout(quita, opc.esfumaCada);
      };
      esfumando = setTimeout(quita, opc.esfumaEspera);
    }
    function ayuda() {
      if (E.intentos === 0) return 'Ordénalas y completa lo que falte. Sin preocuparte de tildes ni comas.';
      if (E.intentos === 1) return 'Casi. Las verdes ya están en su sitio; los huecos te dicen cuántas letras tiene cada palabra.';
      if (E.intentos === 2) return 'Te doy la primera letra de cada palabra que falta.';
      if (ayudaDe() === 'comodin' && !E.comodin) return '¿Atascada? Tienes un comodín: verás las palabras una a una y en orden, solo una vez.';
      if (ayudaDe() === 'caza' && !E.rondas) return '¿Atascada? Pide pista: las palabras salen un instante y, si las tocas, se quedan fijas. ¡Cuidado con las trampas!';
      if (ayudaDe() === 'caza') return 'Puedes cazar más palabras, pero cada ronda dura menos.';
      return 'Te regalo una palabra más. ¡Ya casi!';
    }
    // Comodín (una vez por partida): las palabras de la frase, una a una y en orden, a pantalla completa
    function comodin() {
      if (E.comodin) return;
      E.comodin = true; guardarE();
      const capa = document.createElement('div'); capa.className = 'gl-comodin';
      capa.innerHTML = '<p class="gl-comodin-num"></p><p class="gl-comodin-pal"></p>';
      document.body.appendChild(capa);
      const num = capa.firstChild, pal = capa.lastChild;
      let i = -1;
      const sig = () => {
        i++;
        if (i >= n) { capa.classList.add('fuera'); setTimeout(() => { capa.remove(); pintaEscribir(); $('gl-frase')?.focus(); }, 350); return; }
        num.textContent = `${i + 1} / ${n}`; pal.textContent = visibles[i];
        pal.classList.remove('entra'); void pal.offsetWidth; pal.classList.add('entra');
        setTimeout(sig, opc.comodinCada || 750);
      };
      setTimeout(sig, 400);
    }
    // Cada partida tiene al azar una de las dos ayudas (las partidas guardadas de antes, el comodín)
    const ayudaDe = () => E.ayuda || 'comodin';
    const RONDAS = [15000, 10000, 7000, 5000];
    const rondaMs = () => RONDAS[Math.min(E.rondas || 0, RONDAS.length - 1)];
    const TRAMPAS = ['croqueta', 'manta', 'sofá', 'galleta', 'pijama', 'lunes', 'paraguas', 'calcetín', 'mando', 'siesta', 'churros', 'tortilla', 'Briska', 'ticket', 'piña', 'lluvia'];
    let snd = null;
    // Pista "caza palabras": salen palabras un instante en sitios al azar; tocar una buena la fija en su hueco,
    // tocar una trampa quita un segundo. Cada ronda dura menos que la anterior.
    function caza() {
      if (!snd) snd = sonido({}); snd.audio();
      const dur = rondaMs(); E.rondas = (E.rondas || 0) + 1; guardarE();
      const enFrase = new Set(objetivo);
      const trampas = TRAMPAS.filter(w => !enFrase.has(limpia(w)));
      const capa = document.createElement('div'); capa.className = 'gl-caza';
      capa.innerHTML = '<div class="gl-caza-barra"><i></i></div><p class="gl-caza-titulo">Toca las palabras de la frase</p><div class="gl-caza-campo"></div><div class="gl-plantilla gl-caza-plantilla"></div>';
      document.body.appendChild(capa);
      const barra = capa.querySelector('.gl-caza-barra i'), campo = capa.querySelector('.gl-caza-campo'), plant = capa.querySelector('.gl-caza-plantilla');
      const pintaPlant = () => { plant.innerHTML = objetivo.map((_, i) => huecos(i)).join(''); };
      pintaPlant();
      let queda = dur, ultimo = performance.now(), proximo = 300, vivas = 0, acabado = false;
      const faltan = () => objetivo.map((_, i) => i).filter(i => !E.sabidas.includes(i));
      function suelta(t) {
        const f = faltan(); if (!f.length) return;
        const buena = !trampas.length || Math.random() < 0.6;
        const idx = buena ? f[Math.floor(Math.random() * f.length)] : -1;
        const b = document.createElement('button'); b.type = 'button'; b.className = 'gl-caza-pal';
        b.textContent = buena ? visibles[idx] : trampas[Math.floor(Math.random() * trampas.length)];
        b.style.left = (12 + Math.random() * 76) + '%'; b.style.top = (8 + Math.random() * 80) + '%';
        const prog = 1 - queda / dur, vis = 600 - 200 * prog;   // de 0,6 s a 0,4 s
        campo.appendChild(b); vivas++;
        const quita = () => { if (b.parentNode) { b.remove(); vivas--; } };
        b.addEventListener('pointerdown', e => {
          e.preventDefault(); if (acabado || !b.parentNode) return;
          if (idx >= 0) {
            if (!E.sabidas.includes(idx)) E.sabidas.push(idx);
            b.classList.add('bien'); snd.tic(880, 0.25); if (navigator.vibrate) navigator.vibrate(15);
            pintaPlant(); guardarE();
            if (!faltan().length) setTimeout(fin, 500);
          } else {
            b.classList.add('mal'); queda -= 1000; snd.guau(0, 1.3); if (navigator.vibrate) navigator.vibrate([40, 30, 40]);
            capa.classList.remove('sacude'); void capa.offsetWidth; capa.classList.add('sacude');
          }
          setTimeout(quita, 250);
        });
        setTimeout(quita, vis);
      }
      function fin() {
        if (acabado) return; acabado = true;
        capa.classList.add('fuera');
        setTimeout(() => { capa.remove(); pintaEscribir(); $('gl-frase')?.focus(); }, 350);
      }
      function paso(ahora) {
        if (acabado) return;
        const dt = Math.min(100, ahora - ultimo); ultimo = ahora; queda -= dt; proximo -= dt;
        barra.style.width = Math.max(0, queda / dur * 100) + '%';
        if (queda <= 0) { fin(); return; }
        if (proximo <= 0 && vivas < 2) { suelta(); proximo = 420 + Math.random() * 280; }
        requestAnimationFrame(paso);
      }
      requestAnimationFrame(paso);
    }
    function guardarE() { A.guardarEstado(E); }
    async function comprobar() {
      const ta = $('gl-frase'), txt = ta.value.trim();
      if (!txt) return;
      const toks = txt.split(/\s+/).map(limpia).filter(Boolean);
      if (toks.length === n && toks.every((w, i) => w === objetivo[i])) {
        try {
          const r = await A.post('Resolver', { respuesta: txt, estado: JSON.stringify(E) });
          if (r.ok) { opc.escribir.innerHTML = `<p class="adv-sobre centrado">La frase</p><p class="gl-frase-ok">${opc.frase}</p>`; A.ganado(r); return; }
          A.aviso(r.mensaje, true);
        } catch { A.aviso('No se pudo comprobar', true); }
        return;
      }
      E.intentos++; E.texto = txt;
      toks.forEach((w, i) => { if (w === objetivo[i] && !E.sabidas.includes(i)) E.sabidas.push(i); });
      if ((E.comodin || E.rondas >= 2) && E.intentos >= 4) { const f = objetivo.findIndex((_, i) => !E.sabidas.includes(i)); if (f >= 0) E.sabidas.push(f); }
      guardarE(); pintaEscribir();
      A.aviso('Todavía no…', true);
    }
    // Al ganar la partida: se apuntan las palabras cazadas y empieza la fase de escribir
    function empezar(cazadas) {
      if (D.resuelta) return;
      E = { fase: 'escribir', palabras: cazadas, intentos: 0, sabidas: [], texto: '', ayuda: Math.random() < 0.5 ? 'comodin' : 'caza', rondas: 0 };
      guardarE(); pintaEscribir();
      opc.escribir.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    if (E && !D.resuelta) pintaEscribir();
    return { limpia, visibles, objetivo, n, baraja, empezar, estado: () => E };
  }

  window.advJuego = { sonido, frase };
})();
