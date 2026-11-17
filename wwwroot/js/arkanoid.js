// Adviento · mini Arkanoid: pala, pelota y bloques de colores. BriskaBird revolotea por la zona de bloques y,
// si la pelota le da, suelta una palabra de la frase. Al tirar todos los bloques toca escribir la frase (adv-juego.js).
(() => {
  const A = window.adv, J = window.advJuego;
  if (!A || !J || A.datos.tipo !== 'Arkanoid') return;
  const D = A.datos;

  // ---------- Ajustes (afinar probando en el móvil) ----------
  const AJ = {
    vidas: 3,                // pelotas que se pueden perder; sin vidas, vuelta a empezar (también sin bloques)
    filas: 6, cols: 7,       // rejilla de bloques (42)
    bloquesArriba: 0.14,     // fracción de la altura donde empieza la zona de bloques
    bloquesAlto: 0.30,       // fracción de la altura que ocupa la zona de bloques
    hueco: 4,                // px entre bloques
    palaAncho: 0.255,        // ancho de la pala, fracción del ancho (máx. 480)
    palaAlto: 14,            // px
    palaY: 0.86,             // fracción de la altura donde está la pala
    pelotaR: 0.02,           // radio de la pelota, fracción del ancho (máx. 480)
    velInicio: 0.55,         // velocidad de salida, en alturas de pantalla por segundo
    velMax: 0.95,            // tope de velocidad
    velPorBloque: 1.012,     // la pelota acelera un poco con cada bloque que tira
    anguloMax: 1.05,         // rad (60°): desvío máximo al rebotar en la punta de la pala
    techo: 0.07,             // fracción de la altura que ocupa el marcador (la pelota rebota ahí)
    briskaVeces: 14,         // visitas de BriskaBird como mucho (hay 11 palabras: que se escape alguna es normal)
    briskaPausa: 1800,       // ms mínimos entre que se va y vuelve
    briskaEspera: 8000,      // ms tras los que vuelve aunque no se hayan tirado bloques suficientes
    briskaPasada: 2600,      // ms que tarda en cruzar la pantalla de lado a lado
    briskaPasadas: 2,        // pasadas por visita (ida y vuelta)
    briskaTam: 0.24,         // tamaño de BriskaBird, fracción del ancho (máx. 480)
    briskaPausaSinBloques: 700, // sin bloques, vuelve enseguida hasta cazar todas las palabras
    cacas: [3, 4],           // cacas de BriskaBird mientras quedan bloques (al azar entre estos dos)
    cacasSinBloques: 2,      // cacas por visita cuando ya no hay bloques
    cacaVel: 0.42,           // velocidad de caída, en alturas por segundo
    cacaTam: 0.075,          // tamaño, fracción del ancho (máx. 480)
    manchaMs: 10000,         // lo que dura la pala manchada
    palaVelManchada: 0.75,   // con la pala manchada, la pala sigue al dedo a esta velocidad (anchos de pantalla por segundo)
    entradilla: 3200,        // ms de la presentación de BriskaBird al empezar (un toque la salta)
    esfumaEspera: 1250,      // ms que se ven las palabras al acabar antes de empezar a esfumarse (igual que en globos)
    esfumaCada: 910,         // ms entre palabra y palabra que se esfuma (igual que en globos)
  };
  const COLORES = ['#ff5d73', '#ffb84d', '#6bcb77', '#38c6d9', '#4d96ff', '#b07cff', '#ff8fab'];
  const bbImg = new Image(); bbImg.src = '/img/briska/briskabird.png';

  const $ = id => document.getElementById(id);
  const inicio = $('gl-inicio'), escribir = $('gl-escribir'), pantalla = $('ak-pantalla'), cv = $('ak-canvas');
  const hudVidas = $('ak-vidas'), hudPal = $('ak-npal'), avisoEl = $('ak-aviso');
  if ($('ak-vidas-max')) $('ak-vidas-max').textContent = AJ.vidas;
  const S = J.sonido({ boton: $('ak-mudo'), activa: () => !pantalla.hidden });
  const F = J.frase({ frase: (D.vista && D.vista.frase) || '', inicio, escribir, jugar, esfumaEspera: AJ.esfumaEspera, esfumaCada: AJ.esfumaCada });
  const { visibles, n, baraja } = F;

  const ctx = cv.getContext('2d');
  let W = 0, H = 0, DPR = 1;
  const sprites = new Map();
  const medir = () => {
    DPR = Math.min(window.devicePixelRatio || 1, 1.5); const dpr = DPR; sprites.clear();
    W = window.innerWidth; H = window.innerHeight;
    cv.width = Math.round(W * dpr); cv.height = Math.round(H * dpr);
    cv.style.width = W + 'px'; cv.style.height = H + 'px';
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    if (P) muevePala(P.pala.x);
  };

  // ---------- Geometría (se calcula del tamaño de pantalla, así aguanta un giro o un cambio de tamaño) ----------
  const anchoRef = () => Math.min(W, 480);
  const palaW = () => anchoRef() * AJ.palaAncho;
  const palaTop = () => H * AJ.palaY;
  const tamBB = () => anchoRef() * AJ.briskaTam;
  const azar = (a, b) => a + Math.random() * (b - a);
  function geo() {
    const g = AJ.hueco, mx = 6, top = H * AJ.bloquesArriba;
    const bw = (W - 2 * mx - (AJ.cols - 1) * g) / AJ.cols, bh = (H * AJ.bloquesAlto - (AJ.filas - 1) * g) / AJ.filas;
    return { mx, top, bw, bh, g, fondo: top + AJ.filas * (bh + g) - g };
  }
  const rectDe = (b, G) => ({ x: G.mx + b.c * (G.bw + G.g), y: G.top + b.f * (G.bh + G.g), w: G.bw, h: G.bh });

  // ---------- Partida ----------
  let P = null, raf = 0, ultimo = 0;
  function nueva() {
    const orden = [...Array(n).keys()];   // las palabras salen en el orden de la frase (al escribir se ven desordenadas)
    const bloques = [];
    for (let f = 0; f < AJ.filas; f++) for (let c = 0; c < AJ.cols; c++) bloques.push({ f, c, col: COLORES[f % COLORES.length] });
    // Visita k de BriskaBird: cuando se han tirado ⌊(k + ½)·bloques / visitas⌋ bloques (o pasado briskaEspera desde la anterior)
    const bbPend = Array.from({ length: AJ.briskaVeces }, (_, k) => Math.floor((k + 0.5) * bloques.length / AJ.briskaVeces));
    P = { t: 0, real: 0, vidas: AJ.vidas, bloques, total: bloques.length, rotos: 0, pala: { x: W / 2 }, bola: null,
          cola: orden, cazadas: [], perdidas: 0, bb: null, bbPend, cacas: [], mancha: 0, visitas: 0,
          visitasCaca: cacasEn(),  bbFin: -AJ.briskaEspera + 2500, efectos: [], fin: false, intro: AJ.entradilla };
    reponer(); pintaHud();
  }
  // Visitas (de las primeras) en las que BriskaBird suelta una caca mientras quedan bloques
  function cacasEn() {
    const k = AJ.cacas[0] + Math.floor(Math.random() * (AJ.cacas[1] - AJ.cacas[0] + 1));
    return baraja([...Array(Math.max(k, AJ.briskaVeces - 3)).keys()]).slice(0, k);
  }
  const sinBloques = () => P.rotos >= P.total;
  function reponer() { P.bola = { x: P.pala.x, y: palaTop() - anchoRef() * AJ.pelotaR - 1, vx: 0, vy: 0, r: anchoRef() * AJ.pelotaR, pegada: true }; }
  function lanzar() {
    const b = P.bola, v = AJ.velInicio * H, ang = azar(-0.3, 0.3);
    b.vx = Math.sin(ang) * v; b.vy = -Math.cos(ang) * v; b.pegada = false;
    S.tic(520, 0.3);
  }
  function muevePala(x) {
    const w = palaW(); P.pala.obj = Math.max(w / 2, Math.min(W - w / 2, x));
    if (P.mancha <= P.t) { P.pala.x = P.pala.obj; if (P.bola && P.bola.pegada) P.bola.x = P.pala.x; }
  }
  // Con la pala manchada, va detrás del dedo despacito
  function siguePala(dt) {
    if (P.pala.obj == null || P.mancha <= P.t) return;
    const max = AJ.palaVelManchada * W * dt / 1000, d = P.pala.obj - P.pala.x;
    P.pala.x += Math.max(-max, Math.min(max, d));
  }
  function acelera() {
    const b = P.bola, v = Math.hypot(b.vx, b.vy), nv = Math.min(AJ.velMax * H, v * AJ.velPorBloque);
    if (v > 0) { b.vx *= nv / v; b.vy *= nv / v; }
  }
  function rompe(j, r) {
    const b = P.bloques[j]; P.bloques.splice(j, 1); P.rotos++;
    for (let i = 0; i < 6; i++) {
      const a = Math.random() * 6.3, v = 60 + Math.random() * 140;
      P.efectos.push({ tipo: 'trozo', x: r.x + r.w / 2, y: r.y + r.h / 2, vx: Math.cos(a) * v, vy: Math.sin(a) * v, col: b.col, t0: P.real, dur: 450 });
    }
    S.tic(330 + (AJ.filas - b.f) * 55, 0.28, 0.08);
    if (navigator.vibrate) navigator.vibrate(6);
    acelera();
  }
  function pierdeVida() {
    P.vidas--; pintaHud(); S.gemido(0, false);
    if (navigator.vibrate) navigator.vibrate(60);
    hudVidas.classList.remove('sacude'); void hudVidas.offsetWidth; hudVidas.classList.add('sacude');
    if (P.vidas <= 0) { perder(); return; }
    reponer();
  }

  // La pelota avanza en pasitos de menos de un radio para no atravesar nada aunque vaya rápida
  function mueve(dt) {
    const b = P.bola;
    if (b.pegada) { b.x = P.pala.x; b.y = palaTop() - b.r - 1; return; }
    const s = dt / 1000, G = geo(), pt = palaTop(), pw = palaW(), techo = H * AJ.techo;
    const dist = Math.hypot(b.vx, b.vy) * s, pasos = Math.max(1, Math.ceil(dist / (b.r * 0.8)));
    for (let i = 0; i < pasos; i++) {
      b.x += b.vx * s / pasos; b.y += b.vy * s / pasos;
      if (b.x < b.r) { b.x = b.r; b.vx = Math.abs(b.vx); S.tic(900, 0.08, 0.04); }
      else if (b.x > W - b.r) { b.x = W - b.r; b.vx = -Math.abs(b.vx); S.tic(900, 0.08, 0.04); }
      if (b.y < techo + b.r) { b.y = techo + b.r; b.vy = Math.abs(b.vy); S.tic(900, 0.08, 0.04); }
      // Pala: solo cuando baja y pasa por su altura; el punto de la pala decide el ángulo
      if (b.vy > 0 && b.y + b.r >= pt && b.y - b.r <= pt + AJ.palaAlto && Math.abs(b.x - P.pala.x) <= pw / 2 + b.r * 0.7) {
        const k = Math.max(-1, Math.min(1, (b.x - P.pala.x) / (pw / 2))), v = Math.hypot(b.vx, b.vy), ang = k * AJ.anguloMax;
        b.vx = Math.sin(ang) * v; b.vy = -Math.cos(ang) * v; b.y = pt - b.r;
        S.tic(220, 0.25); P.efectos.push({ tipo: 'pala', t0: P.real, dur: 220 });
      }
      // Bloques: el más cercano al punto de contacto; el lado por el que entra decide qué componente se invierte
      if (b.y - b.r < G.fondo + 2) {
        for (let j = P.bloques.length - 1; j >= 0; j--) {
          const r = rectDe(P.bloques[j], G);
          const cx = Math.max(r.x, Math.min(r.x + r.w, b.x)), cy = Math.max(r.y, Math.min(r.y + r.h, b.y));
          const dx = b.x - cx, dy = b.y - cy;
          if (dx * dx + dy * dy > b.r * b.r) continue;
          if (Math.abs(dx) > Math.abs(dy)) b.vx = (dx > 0 ? 1 : -1) * Math.abs(b.vx); else b.vy = (dy > 0 ? 1 : -1) * Math.abs(b.vy);
          rompe(j, r); break;
        }
      }
      // BriskaBird: rebote redondo y palabra
      if (P.bb) {
        const p = posBriska(P.bb, P.bb.t), T = tamBB();
        if (p) { const dx = b.x - p.x, dy = b.y - p.y, d = Math.hypot(dx, dy) || 1; if (d < T * 0.45 + b.r) cazaBriska(p, dx / d, dy / d); }
      }
      if (b.y - b.r > H) { pierdeVida(); return; }
    }
  }

  // ---------- BriskaBird ----------
  function empiezaBriska() {
    P.bbPend.shift();
    const G = geo(), T = tamBB(), pasadas = [];
    let dir = Math.random() < 0.5 ? 1 : -1;
    for (let i = 0; i < AJ.briskaPasadas; i++, dir = -dir)
      pasadas.push({ dir, y: azar(G.fondo - G.bh * 3 * (P.rotos / P.total), G.fondo + H * 0.12), amp: azar(0.3, 0.8) * T, fr: azar(1.5, 3) });   // al principio por debajo de los bloques; según caen, se mete entre ellos
    // Cacas: instantes de la visita en que se le escapa una (dentro de pantalla)
    const total = AJ.briskaPasadas * AJ.briskaPasada, nc = sinBloques() ? AJ.cacasSinBloques : (P.visitasCaca.includes(P.visitas) ? 1 : 0);
    const cuando = Array.from({ length: nc }, (_, k) => (k + azar(0.3, 0.7)) * total / nc).sort((a, b) => a - b);
    P.visitas++;
    P.bb = { t: 0, pasadas, idx: P.cola.shift(), sonado: -1, cuando };
  }
  // Dónde está en el instante t de su visita (null si ya se fue)
  function posBriska(B, t) {
    const T = tamBB(), i = Math.floor(t / AJ.briskaPasada);
    if (t < 0 || i >= B.pasadas.length) return null;
    const s = B.pasadas[i], p = (t - i * AJ.briskaPasada) / AJ.briskaPasada, rec = p * (W + 2 * T);
    return { seg: i, x: s.dir > 0 ? -T + rec : W + T - rec, y: s.y + Math.sin(p * Math.PI * s.fr) * s.amp, flip: s.dir < 0, rot: Math.sin(p * Math.PI * 8) * 0.12 };
  }
  function pasoBriska(dt) {
    const B = P.bb; B.t += dt;
    const p = posBriska(B, B.t);
    if (!p) {   // se va sin que le hayan dado: la palabra vuelve a la cola para otra visita
      P.bb = null; P.bbFin = P.t; P.perdidas++; P.cola.push(B.idx); P.cola.sort((a, b) => a - b); S.gemido(0, false); return;
    }
    if (p.seg !== B.sonado) { B.sonado = p.seg; S.guau(0, p.seg ? 1.3 : 1.15); }
    if (B.cuando.length && B.t >= B.cuando[0] && p.x > tamBB() * 0.3 && p.x < W - tamBB() * 0.3) { B.cuando.shift(); caga(p); }
  }

  // ---------- Cacas ----------
  function prrt() {
    if (!S.AC || !S.ruido) return;
    const t0 = S.AC.currentTime;
    for (let i = 0; i < 5; i++) S.soplo(t0 + i * 0.05, 0.06, 'lowpass', 300 - i * 35, 0.4);
  }
  function caga(p) {
    P.cacas.push({ x: p.x + (p.flip ? 1 : -1) * tamBB() * 0.25, y: p.y + tamBB() * 0.2, r: anchoRef() * AJ.cacaTam / 2, giro: azar(-1, 1) });
    prrt();
  }
  function mueveCacas(dt) {
    const s = dt / 1000, pt = palaTop(), pw = palaW();
    for (let i = P.cacas.length - 1; i >= 0; i--) {
      const c = P.cacas[i]; c.y += AJ.cacaVel * H * s;
      if (c.y + c.r >= pt && c.y - c.r <= pt + AJ.palaAlto && Math.abs(c.x - P.pala.x) <= pw / 2 + c.r * 0.6) {
        P.cacas.splice(i, 1);
        const ya = P.mancha > P.t; P.mancha = P.t + AJ.manchaMs; if (!ya) P.pala.obj = P.pala.x;
        S.pum(false, 0.6); S.gemido(0.05, true);
        if (navigator.vibrate) navigator.vibrate([50, 30, 50]);
        P.efectos.push({ tipo: 'palabra', x: P.pala.x, y: pt - anchoRef() * 0.12, texto: '¡Puaj! 💩', t0: P.real, dur: 1300, peq: true });
        for (let k = 0; k < 8; k++) { const a = azar(3.3, 6.1), v = azar(60, 160); P.efectos.push({ tipo: 'trozo', x: c.x, y: pt, vx: Math.cos(a) * v, vy: Math.sin(a) * v, col: '#7a4a1e', t0: P.real, dur: 500 }); }
      } else if (c.y - c.r > H) P.cacas.splice(i, 1);
    }
  }
  function cazaBriska(p, nx, ny) {
    const B = P.bb, b = P.bola, v = Math.hypot(b.vx, b.vy);
    // La pelota sale del centro de Briska hacia fuera, con algo de componente vertical para que no se quede plana
    let vx = nx * v, vy = ny * v;
    if (Math.abs(vy) < v * 0.35) { vy = (vy < 0 ? -1 : 1) * v * 0.35; vx = (vx < 0 ? -1 : 1) * Math.sqrt(Math.max(0, v * v - vy * vy)); }
    b.vx = vx; b.vy = vy; b.x = p.x + nx * (tamBB() * 0.45 + b.r + 1); b.y = p.y + ny * (tamBB() * 0.45 + b.r + 1);
    P.bb = null; P.bbFin = P.t; P.cazadas.push(B.idx); pintaHud();
    S.guau(0, 1.3); S.guau(0.15, 1.45); S.guau(0.3, 1.6); S.pum(true);
    if (navigator.vibrate) navigator.vibrate([30, 40, 30]);
    P.efectos.push({ tipo: 'bbfuera', x: p.x, y: p.y, flip: p.flip, t0: P.real, dur: 700 });
    P.efectos.push({ tipo: 'palabra', x: W / 2, y: Math.min(Math.max(p.y, H * 0.3), H * 0.6), texto: visibles[B.idx], t0: P.real, dur: 1700 });
  }
  function dibujaBriska(p, extraRot = 0, extraEsc = 1, alfa = 1) {
    if (!bbImg.complete || !bbImg.naturalWidth) return;
    const T = tamBB() * extraEsc, al = T * bbImg.naturalHeight / bbImg.naturalWidth;
    ctx.save(); ctx.globalAlpha = alfa; ctx.translate(p.x, p.y); ctx.rotate(p.rot + extraRot); if (p.flip) ctx.scale(-1, 1);
    ctx.drawImage(bbImg, -T / 2, -al / 2, T, al); ctx.restore();
  }

  // ---------- HUD y avisos ----------
  function pintaHud() {
    hudVidas.innerHTML = '';
    for (let i = 0; i < AJ.vidas; i++) { const s = document.createElement('i'); s.textContent = '♥'; if (i >= P.vidas) s.className = 'fuera'; hudVidas.appendChild(s); }
    hudPal.textContent = `🐾 ${P.cazadas.length}/${n}`;
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
    if (P.intro > 0) { P.intro -= dt; if (P.intro <= 0) avisoEl.hidden = true; }
    else if (!P.fin) {
      P.t += dt;
      if (P.bb) pasoBriska(dt);
      else if (P.cola.length && !P.bola.pegada && (sinBloques()
        ? P.t - P.bbFin >= AJ.briskaPausaSinBloques
        : P.bbPend.length && P.t - P.bbFin >= AJ.briskaPausa && (P.rotos >= P.bbPend[0] || P.t - P.bbFin >= AJ.briskaEspera))) empiezaBriska();
      siguePala(dt); mueveCacas(dt);
      mueve(dt);
      // Se gana con los bloques fuera y todas las palabras cazadas (sin bloques se sigue jugando hasta cazarlas)
      if (!P.fin && sinBloques() && !P.bb && !P.cola.length) ganar();
      else if (!P.fin && sinBloques() && !P.avisoSin) { P.avisoSin = true; P.efectos.push({ tipo: 'palabra', x: W / 2, y: H * 0.45, texto: `¡Faltan ${P.cola.length + (P.bb ? 1 : 0)} palabras!`, t0: P.real, dur: 1800, peq: true }); }
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
  function redondo(c, x, y, w, h, r) {
    r = Math.min(r, w / 2, h / 2);
    c.beginPath(); c.moveTo(x + r, y); c.lineTo(x + w - r, y); c.quadraticCurveTo(x + w, y, x + w, y + r); c.lineTo(x + w, y + h - r);
    c.quadraticCurveTo(x + w, y + h, x + w - r, y + h); c.lineTo(x + r, y + h); c.quadraticCurveTo(x, y + h, x, y + h - r); c.lineTo(x, y + r);
    c.quadraticCurveTo(x, y, x + r, y); c.closePath();
  }
  function sprite(clave, ancho, alto, pinta) {
    let s = sprites.get(clave);
    if (!s) {
      s = document.createElement('canvas'); s.width = Math.ceil(ancho * DPR); s.height = Math.ceil(alto * DPR);
      const c = s.getContext('2d'); c.scale(DPR, DPR); pinta(c); sprites.set(clave, s);
    }
    return s;
  }
  function dibujaBloque(b, r) {
    const w = Math.round(r.w), h = Math.round(r.h);
    const s = sprite(b.col + w + 'x' + h, w, h, c => {
      const gr = c.createLinearGradient(0, 0, 0, h);
      gr.addColorStop(0, aclara(b.col, 0.35)); gr.addColorStop(0.5, b.col); gr.addColorStop(1, oscurece(b.col, 0.25));
      c.fillStyle = gr; redondo(c, 0, 0, w, h, 6); c.fill();
      c.fillStyle = 'rgba(255,255,255,.35)'; redondo(c, 3, 2, w - 6, h * 0.3, 4); c.fill();
    });
    ctx.drawImage(s, r.x, r.y, w, h);
  }
  function dibujaPala() {
    const w = palaW(), h = AJ.palaAlto, x = P.pala.x - w / 2, y = palaTop();
    const brillo = P.efectos.some(e => e.tipo === 'pala'), queda = P.mancha - P.t;
    // Manchada: marrón con grumos; parpadea los 2 últimos segundos
    const sucia = queda > 0 && (queda > 2000 || Math.floor(queda / 200) % 2 === 0);
    const s = sprite('pala' + Math.round(w) + (brillo ? 'b' : '') + (sucia ? 'm' : ''), w + 8, h + 8, c => {
      c.shadowColor = brillo ? 'rgba(255,230,120,.95)' : 'rgba(255,255,255,.5)'; c.shadowBlur = brillo ? 14 : 8;
      const gr = c.createLinearGradient(0, 4, 0, h + 4);
      if (sucia) { gr.addColorStop(0, '#b07a45'); gr.addColorStop(0.5, '#7a4a1e'); gr.addColorStop(1, '#4e2c0f'); }
      else { gr.addColorStop(0, '#fff6d6'); gr.addColorStop(0.5, '#ffd45e'); gr.addColorStop(1, '#d99a12'); }
      c.fillStyle = gr; redondo(c, 4, 4, w, h, h / 2); c.fill();
      if (sucia) { c.shadowBlur = 0; c.fillStyle = '#5a3412'; for (let i = 0; i < 6; i++) { c.beginPath(); c.arc(4 + w * (0.1 + 0.16 * i), 4 + h * (i % 2 ? 0.35 : 0.7), h * 0.22, 0, 6.2832); c.fill(); } }
    });
    ctx.drawImage(s, x - 4, y - 4, w + 8, h + 8);
  }
  function dibujaBola() {
    const b = P.bola, s = sprite('bola' + Math.round(b.r), b.r * 2 + 12, b.r * 2 + 12, c => {
      c.shadowColor = 'rgba(255,255,255,.9)'; c.shadowBlur = 10;
      const gr = c.createRadialGradient(b.r * 0.7 + 6, b.r * 0.7 + 6, 1, b.r + 6, b.r + 6, b.r);
      gr.addColorStop(0, '#fff'); gr.addColorStop(1, '#ffd8e4');
      c.fillStyle = gr; c.beginPath(); c.arc(b.r + 6, b.r + 6, b.r, 0, 6.2832); c.fill();
    });
    ctx.drawImage(s, b.x - b.r - 6, b.y - b.r - 6, b.r * 2 + 12, b.r * 2 + 12);
  }
  function dibujar() {
    ctx.clearRect(0, 0, W, H);
    const G = geo();
    for (const b of P.bloques) dibujaBloque(b, rectDe(b, G));
    if (P.bb) { const p = posBriska(P.bb, P.bb.t); if (p) dibujaBriska(p); }
    if (P.cacas.length) {
      ctx.save(); ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      for (const c of P.cacas) { ctx.save(); ctx.translate(c.x, c.y); ctx.rotate(Math.sin(c.y / 30) * 0.3 * c.giro); ctx.font = `${Math.round(c.r * 2)}px system-ui, sans-serif`; ctx.fillText('💩', 0, 0); ctx.restore(); }
      ctx.restore();
    }
    dibujaPala(); dibujaBola();
    if (P.mancha > P.t && !P.fin) {
      const tam = Math.round(anchoRef() * 0.04);
      ctx.save(); ctx.fillStyle = '#e8c39e'; ctx.textAlign = 'center'; ctx.font = `700 ${tam}px system-ui, sans-serif`;
      ctx.fillText(`💩 ${Math.ceil((P.mancha - P.t) / 1000)} s`, P.pala.x, palaTop() + AJ.palaAlto + tam * 1.4); ctx.restore();
    }
    if (P.bola.pegada && P.intro <= 0 && !P.fin) {
      const tam = Math.round(anchoRef() * 0.05);
      ctx.save(); ctx.globalAlpha = 0.55 + 0.45 * Math.sin(P.real / 300); ctx.fillStyle = '#fff'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.font = `700 ${tam}px system-ui, sans-serif`; ctx.fillText('Toca para lanzar', W / 2, palaTop() - tam * 2.2); ctx.restore();
    }
    for (let i = P.efectos.length - 1; i >= 0; i--) {
      const e = P.efectos[i], f = (P.real - e.t0) / e.dur;
      if (f >= 1) { P.efectos.splice(i, 1); continue; }
      if (e.tipo === 'pala') continue;
      ctx.save(); ctx.globalAlpha = 1 - f;
      if (e.tipo === 'trozo') {
        const s = (P.real - e.t0) / 1000;
        ctx.fillStyle = e.col; ctx.fillRect(e.x + e.vx * s - 3, e.y + e.vy * s + 300 * s * s - 3, 6, 6);
      } else if (e.tipo === 'bbfuera') {
        ctx.restore(); dibujaBriska({ x: e.x, y: e.y - f * 80, flip: e.flip, rot: 0 }, f * 9, 1 - f * 0.8, 1 - f); continue;
      } else if (e.tipo === 'palabra') {
        // La palabra entra de golpe, se queda grande y se va al final
        const tam = Math.round(anchoRef() * (e.peq ? 0.075 : 0.13)), esc = Math.min(1, f * 7) * (1 + 0.04 * Math.sin(f * 20));
        ctx.globalAlpha = f < 0.7 ? 1 : (1 - f) / 0.3;
        ctx.translate(e.x, e.y - f * 40); ctx.scale(esc, esc);
        ctx.font = `800 ${tam}px system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.shadowColor = 'rgba(255,190,40,.95)'; ctx.shadowBlur = 18; ctx.fillStyle = '#fff';
        ctx.fillText(e.texto, 0, 0);
      }
      ctx.restore();
    }
  }

  // ---------- Pantalla ----------
  let alAcabar = null;
  function jugar(fn) {
    alAcabar = fn; S.audio();
    pantalla.hidden = false; document.body.classList.add('gl-jugando');
    medir(); nueva();
    avisar(`<img class="gl-bb-img" src="${bbImg.src}" alt="" /><p class="gl-titulo">¡Ahí anda BriskaBird!</p><p>Revolotea entre los bloques. Si le das con la pelota, suelta una palabra. Y ojo, que a veces se le escapa algo: si te cae en la pala, irá más lenta un rato 💩. Sin bloques se sigue hasta cazar todas las palabras.</p><p class="gl-bb-premio">Arrastra para mover la pala · toca para lanzar</p>`);
    S.guau(0.1); S.guau(0.32, 0.95);
    ultimo = 0; cancelAnimationFrame(raf); raf = requestAnimationFrame(paso);
  }
  function cerrar() {
    cancelAnimationFrame(raf); raf = 0; pantalla.hidden = true; avisoEl.hidden = true;
    document.body.classList.remove('gl-jugando');
  }
  function perder() {
    P.fin = true; S.gemido(0.1, false);
    avisar(`<p class="gl-titulo">¡Uy! Se te ha caído la última pelota</p><p>Llevabas ${P.rotos} de ${P.total} bloques y ${P.cazadas.length} de ${n} palabras.${D.diario ? ' Mañana, la revancha.' : ' Vuelta a empezar.'}</p>`,
      D.diario ? [['Salir', cerrar, 'primario']] : [['Otra vez', () => jugar(alAcabar), 'primario'], ['Salir', cerrar]]);
    if (D.diario && A.fin) A.fin({ gano: false, palabras: P.cazadas.length });
  }
  function ganar() {
    P.fin = true; S.guau(0.05, 1.3); S.guau(0.2, 1.45); S.guau(0.35, 1.75);
    A.celebrar();
    const cazadas = P.cazadas.slice(), faltan = n - cazadas.length;
    avisar(`<p class="gl-titulo">¡Bloques fuera y palabras cazadas!</p><p>Tienes las <b>${cazadas.length}</b> palabras${faltan ? ` (faltan ${faltan})` : ''}. Ahora, a memorizarlas.</p>`,
      [[D.resuelta ? 'Salir' : 'Escribir la frase', () => { cerrar(); alAcabar && alAcabar(cazadas); }, 'primario']]);
  }

  // ---------- Toques: arrastrar mueve la pala; soltar con la pelota pegada la lanza ----------
  let dedo = null;
  const dedoX = e => e.clientX - cv.getBoundingClientRect().left;
  avisoEl.addEventListener('click', () => { if (P && P.intro > 0 && P.intro < AJ.entradilla - 500) P.intro = 0.001; });
  cv.addEventListener('pointerdown', e => {
    if (!P || P.fin || P.intro > 0) return;
    e.preventDefault();
    dedo = e.pointerId; try { cv.setPointerCapture(e.pointerId); } catch { }
    muevePala(dedoX(e));
  }, { passive: false });
  cv.addEventListener('pointermove', e => {
    if (!P || P.fin || P.intro > 0 || e.pointerId !== dedo) return;
    e.preventDefault(); muevePala(dedoX(e));
  }, { passive: false });
  const suelta = e => {
    if (e.pointerId !== dedo) return;
    dedo = null;
    if (P && !P.fin && P.intro <= 0 && P.bola.pegada) lanzar();
  };
  cv.addEventListener('pointerup', suelta); cv.addEventListener('pointercancel', suelta);
  $('ak-salir').addEventListener('click', cerrar);
  window.addEventListener('resize', () => { if (!pantalla.hidden) medir(); });

  $('gl-empezar').addEventListener('click', () => jugar(F.empezar));
})();
