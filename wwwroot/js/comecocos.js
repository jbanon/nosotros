// Adviento · mini comecocos del súper: un carrito recorre los pasillos recogiendo la compra (puntitos) mientras le persiguen
// el ticket kilométrico y la oferta 3x2. Briskanguro aparece dando saltitos por el laberinto y, si el
// carrito la alcanza, suelta una palabra de la frase. Con toda la compra hecha toca escribir la frase (adv-juego.js).
// Estética de laberinto arcade clásico: fondo negro, paredes de neón azul con doble contorno, puntitos y bolas grandes.
(() => {
  const A = window.adv, J = window.advJuego;
  if (!A || !J || A.datos.tipo !== 'Comecocos') return;
  const D = A.datos;

  // ---------- Ajustes (afinar probando en el móvil) ----------
  const AJ = {
    vidas: 3,                // veces que te pueden pillar; sin vidas, vuelta a empezar
    velJug: 1.1,             // el carrito va siempre un 10% más rápido que los perseguidores
    velPers: 3.7,            // velocidad de los perseguidores al empezar
    velPersSube: 0.19,       // fracción que suben los perseguidores al acabar la compra (lineal con lo comido)
    velTunel: 0.6,           // los perseguidores van más lentos por el túnel (fuera del laberinto)
    salidas: [600, 3500],   // ms tras el primer deslizamiento en que sale cada perseguidor del almacén
    salidasTrasVida: [600, 2400],   // ms tras perder una vida
    modo: [9000, 2000],      // ms persiguiendo / ms dispersándose a su esquina (alterna)
    choque: 0.62,            // distancia (casillas) a la que te pillan
    tolGiro: 0.4,            // casillas pasado un cruce en las que un deslizamiento tardío aún gira (vuelve al cruce)
    swipe: 16,               // px de deslizamiento para contar como dirección
    cafeDur: 4500,           // ms de chute de velocidad por café
    cafeVel: 1.4,            // multiplicador de velocidad con café
    techo: 0.075,            // fracción de la altura reservada al marcador
    suelo: 0.02,             // fracción de la altura libre abajo
    briskaVeces: 16,         // visitas de Briskanguro como mucho (hay 11 palabras: que se escape alguna es normal)
    briskaDur: 5200,         // ms que se queda dando saltitos si no la pillan
    briskaPausa: 1200,       // ms mínimos entre que se va y vuelve
    briskaEspera: 5500,      // ms tras los que vuelve aunque no se haya comido lo suficiente
    briskaLejos: 4,          // casillas (en línea) de distancia mínima al carrito al aparecer
    briskaHuye: 0.4,         // probabilidad de que cada salto sea alejándose del carrito (el resto, al azar)
    briskaTam: 1.35,         // ancho de Briskanguro, en casillas
    briskaAlcance: 0.75,     // distancia (casillas) a la que la pillas
    salto: 420,              // ms de cada salto
    saltoPausa: 90,          // ms agachada entre salto y salto
    saltoAlto: 0.75,         // altura del salto, en casillas
    entradilla: 3200,        // ms de la presentación de Briskanguro al empezar (un toque la salta)
    muerte: 1400,            // ms de pausa al pillarte (los primeros 800 con el carrito patas arriba)
    esfumaEspera: 1250,      // ms que se ven las palabras al acabar antes de empezar a esfumarse (igual que en globos)
    esfumaCada: 3000,        // ms entre palabra y palabra que se esfuma (igual que en globos)
  };
  // Laberinto propio: # pared · . compra · o café (bola grande) · ' ' pasillo vacío · c almacén de los perseguidores · - su puerta · P salida del carrito.
  // La fila 7 es un túnel: por los extremos se sale por un lado y se entra por el otro.
  const MAPA = [
    '###########',
    '#o...#...o#',
    '#.##.#.##.#',
    '#.........#',
    '#.##...##.#',
    '#.##ccc##.#',
    '#.###-###.#',
    ' ......... ',
    '#.##.#.##.#',
    '#....#....#',
    '#.#.###.#.#',
    '#.........#',
    '#.###.###.#',
    '#o...P...o#',
    '###########',
  ];
  const ROWS = MAPA.length, COLS = MAPA[0].length, TUNEL = 7, PUERTA = { x: 5, y: 7 };
  const DX = [1, 0, -1, 0], DY = [0, 1, 0, -1];   // 0 derecha · 1 abajo · 2 izquierda · 3 arriba
  const MELODIA = [523, 659, 784, 1047, 784, 659, 587, 698, 880, 698, 587, 494], BAJO = [131, 196, 165, 196];
  const bbImg = new Image(); bbImg.src = '/img/briska/briskanguro.png';

  const $ = id => document.getElementById(id);
  const inicio = $('gl-inicio'), escribir = $('gl-escribir'), pantalla = $('cc-pantalla'), cv = $('cc-canvas');
  const hudVidas = $('cc-vidas'), hudPal = $('cc-npal'), avisoEl = $('cc-aviso');
  if ($('cc-vidas-max')) $('cc-vidas-max').textContent = AJ.vidas;
  const S = J.sonido({ boton: $('cc-mudo'), activa: () => !pantalla.hidden });
  const F = J.frase({ frase: (D.vista && D.vista.frase) || '', inicio, escribir, jugar, esfumaEspera: AJ.esfumaEspera, esfumaCada: AJ.esfumaCada });
  const { visibles, n, baraja } = F;

  const ctx = cv.getContext('2d');
  let W = 0, H = 0, DPR = 1, G = { cs: 30, ox: 0, oy: 0 };
  const sprites = new Map();
  const medir = () => {
    DPR = Math.min(window.devicePixelRatio || 1, 1.5); sprites.clear();
    W = window.innerWidth; H = window.innerHeight;
    cv.width = Math.round(W * DPR); cv.height = Math.round(H * DPR);
    cv.style.width = W + 'px'; cv.style.height = H + 'px';
    ctx.setTransform(DPR, 0, 0, DPR, 0, 0);
    const top = H * AJ.techo, libre = H - top - H * AJ.suelo;
    const cs = Math.floor(Math.min(W / COLS, libre / ROWS));
    G = { cs, ox: (W - cs * COLS) / 2, oy: top + (libre - cs * ROWS) / 2 };
  };
  const px = x => G.ox + (x + 0.5) * G.cs, py = y => G.oy + (y + 0.5) * G.cs;
  const azar = (a, b) => a + Math.random() * (b - a);
  const elige = a => a[Math.floor(Math.random() * a.length)];

  // ---------- Laberinto ----------
  function celda(x, y) {
    if (y < 0 || y >= ROWS) return '#';
    if (x < 0) x += COLS; else if (x >= COLS) x -= COLS;
    return MAPA[y][x];
  }
  // quien: 'jug' (carrito), 'pers' (perseguidor suelto), 'salir' (perseguidor saliendo del almacén), 'bb' (Briskanguro)
  function abierta(x, y, quien) {
    const c = celda(x, y);
    if (c === '#') return false;
    if (quien === 'salir') return true;
    if (c === 'c' || c === '-') return false;
    if (quien === 'bb' && y === TUNEL && (x <= 0 || x >= COLS - 1)) return false;
    return true;
  }
  const distX = (a, b) => { const d = Math.abs(a - b); return Math.min(d, COLS - d); };
  const dist = (a, b) => Math.hypot(distX(a.x, b.x), a.y - b.y);
  const opuesto = d => (d + 2) % 4;

  // Avanza `d` casillas en la dirección de `e`; en cada centro de casilla llama a decide(e) que devuelve la dirección nueva (o -1 para parar)
  function avanza(e, d, decide) {
    for (let g = 0; g < 8 && d > 0; g++) {
      if (e.dir < 0) { const nd = decide(e); if (nd < 0) return; e.dir = nd; }
      const cx = Math.round(e.x), cy = Math.round(e.y);
      const pasado = (e.x - cx) * DX[e.dir] + (e.y - cy) * DY[e.dir];
      let tx = cx, ty = cy;
      if (pasado > -1e-6) { tx += DX[e.dir]; ty += DY[e.dir]; }
      const falta = Math.abs(tx - e.x) + Math.abs(ty - e.y);
      if (d < falta) { e.x += DX[e.dir] * d; e.y += DY[e.dir] * d; return; }
      e.x = tx; e.y = ty; d -= falta;
      if (e.x < -0.5) e.x += COLS; else if (e.x > COLS - 0.5) e.x -= COLS;
      const nd = decide(e);
      e.dir = nd;
      if (nd < 0) return;
    }
  }

  // ---------- Partida ----------
  let P = null, raf = 0, ultimo = 0;
  function nuevosPers() {
    // Dos perseguidores, cada uno con su carácter: el ticket va a por ti y la oferta se te adelanta para cortarte el paso
    return [
      { nombre: 'ticket', x: 4, y: 5, dir: -1, estado: 'casa', azar: 0.03, adelanta: 0, esquina: { x: 1, y: 1 }, sale: Infinity, fase: Math.random() * 7 },
      { nombre: 'oferta', x: 6, y: 5, dir: -1, estado: 'casa', azar: 0.07, adelanta: 3, esquina: { x: COLS - 2, y: ROWS - 2 }, sale: Infinity, fase: Math.random() * 7 },
    ];
  }
  function nueva() {
    const orden = [...Array(n).keys()];   // las palabras salen en el orden de la frase (al escribir se ven desordenadas)
    const cosas = new Map();
    let ini = { x: 5, y: ROWS - 2 };
    for (let y = 0; y < ROWS; y++) for (let x = 0; x < COLS; x++) {
      const c = MAPA[y][x];
      if (c === '.' || c === 'o') cosas.set(y * COLS + x, c);
      if (c === 'P') ini = { x, y };
    }
    const bbPend = Array.from({ length: AJ.briskaVeces }, (_, k) => Math.floor((k + 0.5) * cosas.size / AJ.briskaVeces));
    P = { t: 0, real: 0, vidas: AJ.vidas, cosas, total: cosas.size, comidos: 0, ini,
          jug: { x: ini.x, y: ini.y, dir: -1, want: -1, cara: 1, rueda: 0 }, pers: nuevosPers(), arrancado: false, cafe: 0, pausa: 0, repuesto: true,
          cola: orden, cazadas: [], perdidas: 0, bb: null, bbPend, bbFin: -AJ.briskaEspera + 2500, saltos: 0, efectos: [], fin: false, intro: AJ.entradilla, waka: 0 };
    pintaHud();
  }
  function reponer() {
    const j = P.jug; j.x = P.ini.x; j.y = P.ini.y; j.dir = -1; j.want = -1;
    P.pers = nuevosPers(); P.pers.forEach((e, i) => e.sale = P.t + AJ.salidasTrasVida[i]); P.cafe = 0;
  }
  function arrancar() {
    P.arrancado = true;
    P.pers.forEach((e, i) => e.sale = P.t + AJ.salidas[i]);
  }

  // El carrito: en cada centro de casilla gira si la dirección pedida está libre; si no, sigue; si no puede, se para
  function decideJug(e) {
    const cx = Math.round(e.x), cy = Math.round(e.y);
    come(cx, cy);
    if (e.want >= 0 && abierta(cx + DX[e.want], cy + DY[e.want], 'jug')) { const d = e.want; e.want = -1; return d; }
    if (e.dir >= 0 && abierta(cx + DX[e.dir], cy + DY[e.dir], 'jug')) return e.dir;
    return -1;
  }
  function come(cx, cy) {
    if (cx < 0) cx += COLS; else if (cx >= COLS) cx -= COLS;
    const k = cy * COLS + cx, c = P.cosas.get(k);
    if (!c) return;
    P.cosas.delete(k); P.comidos++;
    if (c === 'o') {
      P.cafe = AJ.cafeDur;
      if (S.AC) { const t = S.AC.currentTime; S.boing(t, 660, 0.12, 0.3); S.boing(t + 0.1, 880, 0.12, 0.3); S.boing(t + 0.2, 1320, 0.2, 0.3); }
      P.efectos.push({ tipo: 'texto', x: px(cx), y: py(cy) - G.cs * 0.6, texto: '¡Café!', t0: P.real, dur: 900 });
    } else {
      P.waka = 1 - P.waka; S.tic(P.waka ? 470 : 560, 0.09, 0.045);
    }
  }
  function pasoJug(dt) {
    const e = P.jug;
    if (e.want >= 0 && e.dir >= 0 && e.want === opuesto(e.dir)) { e.dir = e.want; e.want = -1; }
    // Giro con margen: si acaba de pasar un cruce donde se podía girar hacia donde pide, vuelve al cruce y gira
    if (e.want >= 0 && e.dir >= 0 && e.want !== e.dir) {
      const cx = Math.round(e.x), cy = Math.round(e.y), pasado = (e.x - cx) * DX[e.dir] + (e.y - cy) * DY[e.dir];
      if (pasado > 0 && pasado < AJ.tolGiro && abierta(cx + DX[e.want], cy + DY[e.want], 'jug')) { e.x = cx; e.y = cy; e.dir = e.want; e.want = -1; }
    }
    const antes = e.dir;
    avanza(e, AJ.velJug * velPers() * (P.cafe > 0 ? AJ.cafeVel : 1) * dt / 1000, decideJug);
    if (e.dir === 0) e.cara = 1; else if (e.dir === 2) e.cara = -1;
    if (e.dir >= 0 || antes >= 0) e.rueda += dt;
  }

  // Perseguidores: en cada cruce eligen la salida que más les acerca a su objetivo (sin dar media vuelta), con algo de azar
  function objetivo(e) {
    const ciclo = P.t % (AJ.modo[0] + AJ.modo[1]);
    if (ciclo >= AJ.modo[0]) return e.esquina;
    const j = P.jug;
    if (e.adelanta && j.dir >= 0) return { x: ((j.x + DX[j.dir] * e.adelanta) % COLS + COLS) % COLS, y: Math.max(0, Math.min(ROWS - 1, j.y + DY[j.dir] * e.adelanta)) };
    return { x: j.x, y: j.y };
  }
  function decidePers(e) {
    const cx = Math.round(e.x), cy = Math.round(e.y), quien = e.estado === 'salir' ? 'salir' : 'pers';
    let ops = [0, 1, 2, 3].filter(d => abierta(cx + DX[d], cy + DY[d], quien));
    if (ops.length > 1 && e.dir >= 0) { const sin = ops.filter(d => d !== opuesto(e.dir)); if (sin.length) ops = sin; }
    if (!ops.length) return -1;
    if (e.estado === 'fuera' && Math.random() < e.azar) return elige(ops);
    const tg = e.estado === 'salir' ? PUERTA : objetivo(e);
    let mejor = ops[0], md = Infinity;
    for (const d of ops) {
      const nx = cx + DX[d], ny = cy + DY[d], ddx = distX(nx, tg.x), dd = ddx * ddx + (ny - tg.y) * (ny - tg.y);
      if (dd < md) { md = dd; mejor = d; }
    }
    return mejor;
  }
  // Velocidad actual de los perseguidores (sube con lo comido); el carrito va en proporción
  function velPers() { return AJ.velPers * (1 + AJ.velPersSube * P.comidos / P.total); }
  function pasoPers(dt) {
    const v = velPers();
    for (const e of P.pers) {
      if (e.estado === 'casa') { if (P.t >= e.sale) { e.estado = 'salir'; e.dir = -1; } else continue; }
      const enTunel = e.y === TUNEL && (e.x < 0.5 || e.x > COLS - 1.5);
      avanza(e, v * (enTunel ? AJ.velTunel : 1) * dt / 1000, decidePers);
      if (e.estado === 'salir' && e.y >= PUERTA.y - 0.01) e.estado = 'fuera';
    }
  }
  function pillado() {
    P.vidas--; pintaHud(); S.gemido(0, false);
    if (navigator.vibrate) navigator.vibrate(80);
    hudVidas.classList.remove('sacude'); void hudVidas.offsetWidth; hudVidas.classList.add('sacude');
    P.pausa = AJ.muerte; P.repuesto = false; P.jug.want = -1;
    for (let i = 0; i < 10; i++) {
      const a = Math.random() * 6.3, vv = 50 + Math.random() * 120;
      P.efectos.push({ tipo: 'trozo', x: px(P.jug.x), y: py(P.jug.y), vx: Math.cos(a) * vv, vy: Math.sin(a) * vv - 60, col: elige(['#ff3b4e', '#5dff7a', '#ffd23e', '#fff']), t0: P.real, dur: 700 });
    }
    if (P.vidas <= 0) perder();
  }

  // ---------- Briskanguro ----------
  function empiezaBriska() {
    P.bbPend.shift();
    const libres = [];
    for (let y = 0; y < ROWS; y++) for (let x = 0; x < COLS; x++) if (abierta(x, y, 'bb')) libres.push({ x, y });
    const lejos = libres.filter(c => distX(c.x, P.jug.x) + Math.abs(c.y - P.jug.y) >= AJ.briskaLejos);
    const c = elige(lejos.length ? lejos : libres);
    P.bb = { x: c.x, y: c.y, de: c, a: c, dir: -1, cara: 1, t: 0, fase: 'entra', vida: 0, idx: P.cola.shift(), h: 0 };
    if (S.AC) S.boing(S.AC.currentTime, 330, 0.45, 0.3);
  }
  function siguienteSalto(B) {
    const ops = [0, 1, 2, 3].filter(d => abierta(B.a.x + DX[d], B.a.y + DY[d], 'bb'));
    let d;
    if (!ops.length) d = -1;
    else if (Math.random() < AJ.briskaHuye) {
      let md = -1;
      for (const o of ops) { const dd = dist({ x: B.a.x + DX[o], y: B.a.y + DY[o] }, P.jug); if (dd > md) { md = dd; d = o; } }
    } else { const sin = ops.filter(o => o !== opuesto(B.dir)); d = elige(sin.length ? sin : ops); }
    B.de = B.a; B.dir = d;
    B.a = d < 0 ? B.de : { x: B.de.x + DX[d], y: B.de.y + DY[d] };
    if (d === 0) B.cara = -1; else if (d === 2) B.cara = 1;
    B.fase = 'salta'; B.t = 0;
    // Música de saltitos: melodía con glissando en cada bote, bajo que alterna y un toquecito en el aire
    if (S.AC) {
      const t = S.AC.currentTime, k = P.saltos++;
      S.boing(t, MELODIA[k % MELODIA.length], 0.22, 0.26);
      S.boing(t, BAJO[k % BAJO.length], 0.18, 0.2, 'sine');
      S.boing(t + AJ.salto / 2000, MELODIA[(k + 4) % MELODIA.length] * 2, 0.07, 0.05);
    }
  }
  function pasoBriska(dt) {
    const B = P.bb; B.t += dt; B.vida += dt;
    if (B.fase === 'entra') {
      const p = Math.min(1, B.t / 500); B.h = (1 - p * p) * 4; B.x = B.a.x; B.y = B.a.y;
      if (p >= 1) { B.fase = 'agachada'; B.t = 0; S.pum(false, 0.6); }
    } else if (B.fase === 'agachada') {
      B.h = 0; if (B.t >= AJ.saltoPausa) siguienteSalto(B);
    } else if (B.fase === 'salta') {
      const p = Math.min(1, B.t / AJ.salto);
      B.x = B.de.x + (B.a.x - B.de.x) * p; B.y = B.de.y + (B.a.y - B.de.y) * p; B.h = Math.sin(p * Math.PI) * AJ.saltoAlto;
      if (p >= 1) { B.fase = 'agachada'; B.t = 0; }
    } else if (B.fase === 'sale') {
      const p = Math.min(1, B.t / 650); B.h = p * p * 7;
      if (p >= 1) { P.bb = null; P.bbFin = P.t; P.perdidas++; P.cola.push(B.idx); P.cola.sort((a, b) => a - b); return; }
    }
    if (B.fase !== 'sale' && B.vida >= AJ.briskaDur) {
      B.fase = 'sale'; B.t = 0;
      if (S.AC) { const t = S.AC.currentTime; S.boing(t, 440, 0.25, 0.25); S.boing(t + 0.2, 660, 0.25, 0.25); S.boing(t + 0.4, 990, 0.45, 0.25); }
      return;
    }
    if (B.fase !== 'sale' && B.fase !== 'entra' && dist(B, P.jug) < AJ.briskaAlcance) cazaBriska(B);
  }
  function cazaBriska(B) {
    P.bb = null; P.bbFin = P.t; P.cazadas.push(B.idx); pintaHud();
    S.guau(0, 1.3); S.guau(0.15, 1.45); S.guau(0.3, 1.6); S.pum(true);
    if (S.AC) { const t = S.AC.currentTime; S.boing(t + 0.05, 784, 0.15, 0.25); S.boing(t + 0.18, 1047, 0.15, 0.25); S.boing(t + 0.31, 1568, 0.35, 0.25); }
    if (navigator.vibrate) navigator.vibrate([30, 40, 30]);
    const x = px(B.x), y = py(B.y) - B.h * G.cs;
    P.efectos.push({ tipo: 'bbfuera', x, y, cara: B.cara, t0: P.real, dur: 750 });
    P.efectos.push({ tipo: 'palabra', x: W / 2, y: Math.min(Math.max(y, H * 0.3), H * 0.6), texto: visibles[B.idx], t0: P.real, dur: 1700 });
  }
  // p: { x, y } en píxeles (suelo), h altura en casillas, cara 1 mira a la izquierda (como la imagen) / -1 a la derecha
  function dibujaBriska(p, h, cara, sx = 1, sy = 1, rot = 0, alfa = 1, sombra = true) {
    if (!bbImg.complete || !bbImg.naturalWidth) return;
    const T = G.cs * AJ.briskaTam, al = T * bbImg.naturalHeight / bbImg.naturalWidth, hp = h * G.cs, hr = Math.min(1, h / AJ.saltoAlto);
    if (sombra) {
      ctx.save(); ctx.globalAlpha = alfa * 0.5 * (1 - 0.55 * hr); ctx.fillStyle = '#26348f';
      ctx.beginPath(); ctx.ellipse(p.x, p.y + G.cs * 0.32, T * 0.33 * (1 - 0.3 * hr), G.cs * 0.14 * (1 - 0.3 * hr), 0, 0, 6.2832); ctx.fill(); ctx.restore();
    }
    ctx.save(); ctx.globalAlpha = alfa; ctx.translate(p.x, p.y + G.cs * 0.32 - hp); ctx.rotate(rot); ctx.scale(sx * cara, sy);
    ctx.drawImage(bbImg, -T / 2, -al, T, al); ctx.restore();
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
    else if (P.pausa > 0) {
      P.pausa -= dt;
      if (!P.repuesto && P.pausa <= AJ.muerte - 800) { reponer(); P.repuesto = true; }
    }
    else if (!P.fin) {
      if (P.arrancado) P.t += dt;
      const compraHecha = P.comidos >= P.total;
      if (P.cafe > 0) P.cafe -= dt;
      pasoJug(dt);
      if (!compraHecha) {
        pasoPers(dt);
        if (P.pers.some(e => e.estado !== 'casa' && dist(e, P.jug) < AJ.choque)) pillado();
      }
      if (!P.fin) {
        if (P.bb) pasoBriska(dt);
        else if (!compraHecha && P.arrancado && P.cola.length && P.bbPend.length && P.t - P.bbFin >= AJ.briskaPausa && (P.comidos >= P.bbPend[0] || P.t - P.bbFin >= AJ.briskaEspera)) empiezaBriska();
        if (P.comidos >= P.total && !P.bb) ganar();
      }
    }
    dibujar();
    raf = requestAnimationFrame(paso);
  }

  // ---------- Dibujo ----------
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
  // Paredes de neón: se trazan uniendo los centros de las casillas de pared con una línea gruesa azul y encima la misma
  // más fina en negro, así queda solo el contorno (doble línea con esquinas redondeadas). Se pinta una vez por tamaño.
  function dibujaMapa() {
    const cs = G.cs;
    const s = sprite('mapa' + cs, cs * COLS, cs * ROWS, c => {
      const esPared = (x, y) => x >= 0 && x < COLS && y >= 0 && y < ROWS && MAPA[y][x] === '#';
      const ancho = cs * 0.56, grosor = Math.max(2, cs * 0.075);
      const traza = (w, color) => {
        c.strokeStyle = c.fillStyle = color; c.lineWidth = w; c.lineCap = c.lineJoin = 'round';
        for (let y = 0; y < ROWS; y++) for (let x = 0; x < COLS; x++) {
          if (!esPared(x, y)) continue;
          const X = (x + 0.5) * cs, Y = (y + 0.5) * cs;
          c.beginPath(); c.arc(X, Y, w / 2, 0, 6.2832); c.fill();
          if (esPared(x + 1, y)) { c.beginPath(); c.moveTo(X, Y); c.lineTo(X + cs, Y); c.stroke(); }
          if (esPared(x, y + 1)) { c.beginPath(); c.moveTo(X, Y); c.lineTo(X, Y + cs); c.stroke(); }
          if (esPared(x + 1, y) && esPared(x, y + 1) && esPared(x + 1, y + 1)) c.fillRect(X, Y, cs, cs);   // bloques de 2 × 2: sin hueco en medio
        }
      };
      c.shadowColor = '#3d6bff'; c.shadowBlur = cs * 0.22; traza(ancho, '#2a4dff');
      c.shadowBlur = 0; traza(ancho, '#2a4dff'); traza(ancho - 2 * grosor, '#000');
      // Almacén de los perseguidores: una línea fina por arriba y la puerta rosa por abajo
      let x0 = COLS, x1 = -1, yc = 0;
      for (let y = 0; y < ROWS; y++) for (let x = 0; x < COLS; x++) if (MAPA[y][x] === 'c') { x0 = Math.min(x0, x); x1 = Math.max(x1, x); yc = y; }
      if (x1 >= 0) {
        c.lineCap = 'butt'; c.lineWidth = grosor; c.strokeStyle = '#2a4dff';
        const Y = (yc + 0.5) * cs - ancho / 2 + grosor / 2;
        c.beginPath(); c.moveTo((x0 - 0.5) * cs + ancho / 2 - grosor, Y); c.lineTo((x1 + 1.5) * cs - ancho / 2 + grosor, Y); c.stroke();
      }
      c.lineWidth = grosor * 1.4; c.strokeStyle = '#ffb3e0';
      c.beginPath(); c.moveTo((PUERTA.x - 0.5) * cs + ancho / 2 - grosor, (PUERTA.y - 0.5) * cs); c.lineTo((PUERTA.x + 1.5) * cs - ancho / 2 + grosor, (PUERTA.y - 0.5) * cs); c.stroke();
    });
    ctx.drawImage(s, G.ox, G.oy, G.cs * COLS, G.cs * ROWS);
  }
  // La compra son puntitos amarillentos; los cafés de las esquinas, bolas grandes que laten con su taza dentro
  function dibujaCosas() {
    const cs = G.cs, r = Math.max(1.5, cs * 0.075), late = 0.5 + 0.5 * Math.sin(P.real / 170);
    const taza = sprite('cafe' + cs, cs, cs, g => {
      const m = cs / 2;
      g.fillStyle = '#6b3214'; redondo(g, m - cs * 0.15, m - cs * 0.06, cs * 0.3, cs * 0.24, cs * 0.06); g.fill();
      g.strokeStyle = '#6b3214'; g.lineWidth = Math.max(1.5, cs * 0.05); g.beginPath(); g.arc(m + cs * 0.16, m + cs * 0.05, cs * 0.07, -1.4, 1.4); g.stroke();
      g.fillStyle = '#fff'; g.beginPath(); g.ellipse(m, m - cs * 0.06, cs * 0.13, cs * 0.045, 0, 0, 6.2832); g.fill();
    });
    ctx.fillStyle = '#ffd98a';
    for (const [k, c] of P.cosas) {
      const x = px(k % COLS), y = py(Math.floor(k / COLS));
      ctx.beginPath(); ctx.arc(x, y, c === '.' ? r : cs * (0.3 + 0.05 * late), 0, 6.2832); ctx.fill();
      if (c === 'o') ctx.drawImage(taza, x - cs / 2, y - cs / 2, cs, cs);
    }
  }
  // Dibuja la entidad en su sitio y, si asoma por el túnel, también por el otro lado
  function enSitio(e, fn) {
    fn(px(e.x), py(e.y));
    if (e.y === TUNEL || Math.abs(e.y - TUNEL) < 0.01) { if (e.x < 0.5) fn(px(e.x + COLS), py(e.y)); else if (e.x > COLS - 1.5) fn(px(e.x - COLS), py(e.y)); }
  }
  function dibujaCarrito(x, y, muerto) {
    const e = P.jug, u = G.cs * 0.95;
    ctx.save(); ctx.translate(x, y);
    if (muerto) { const f = Math.min(1, (AJ.muerte - P.pausa) / 800); ctx.rotate(f * 3.3); ctx.scale(1 - f * 0.6, 1 - f * 0.6); ctx.globalAlpha = 1 - f * 0.8; }
    else {
      const inclina = e.dir === 3 ? -0.22 : e.dir === 1 ? 0.22 : 0;
      ctx.rotate(inclina * e.cara); ctx.translate(0, Math.sin(e.rueda / 60) * (e.dir >= 0 ? 0.6 : 0));
    }
    ctx.scale(e.cara * u, u);
    if (P.cafe > 0 && !muerto) { ctx.shadowColor = 'rgba(255,220,120,.95)'; ctx.shadowBlur = 14; }
    // Cesta
    ctx.fillStyle = '#ff2f45'; ctx.beginPath(); ctx.moveTo(-0.36, -0.16); ctx.lineTo(0.42, -0.16); ctx.lineTo(0.3, 0.2); ctx.lineTo(-0.26, 0.2); ctx.closePath(); ctx.fill();
    ctx.shadowBlur = 0;
    ctx.strokeStyle = 'rgba(255,255,255,.5)'; ctx.lineWidth = 0.025;
    for (let i = 0; i < 4; i++) { ctx.beginPath(); ctx.moveTo(-0.3 + i * 0.19, -0.16); ctx.lineTo(-0.22 + i * 0.16, 0.2); ctx.stroke(); }
    ctx.beginPath(); ctx.moveTo(-0.32, 0.0); ctx.lineTo(0.36, 0.0); ctx.stroke();
    // Compra asomando: lechuga y barra de pan
    ctx.fillStyle = '#5dff7a'; ctx.beginPath(); ctx.arc(-0.12, -0.2, 0.11, 0, 6.2832); ctx.fill();
    ctx.fillStyle = '#ffd23e'; ctx.save(); ctx.translate(0.14, -0.24); ctx.rotate(-0.6); ctx.beginPath(); ctx.ellipse(0, 0, 0.17, 0.06, 0, 0, 6.2832); ctx.fill(); ctx.restore();
    // Corazoncito en el lateral
    ctx.fillStyle = '#fff'; ctx.beginPath(); ctx.moveTo(0.04, 0.12); ctx.bezierCurveTo(-0.08, 0.02, 0.0, -0.08, 0.04, -0.02); ctx.bezierCurveTo(0.08, -0.08, 0.16, 0.02, 0.04, 0.12); ctx.fill();
    // Asa y ruedas
    ctx.strokeStyle = '#eef2ff'; ctx.lineWidth = 0.05; ctx.lineCap = 'round'; ctx.beginPath(); ctx.moveTo(-0.36, -0.16); ctx.lineTo(-0.5, -0.42); ctx.stroke();
    for (const wx of [-0.2, 0.2]) {   // ruedas claras: las negras no se verían sobre el fondo
      ctx.fillStyle = '#8fd8ff'; ctx.beginPath(); ctx.arc(wx, 0.32, 0.09, 0, 6.2832); ctx.fill();
      ctx.fillStyle = '#12306b'; ctx.beginPath(); ctx.arc(wx + Math.cos(e.rueda / 50) * 0.04, 0.32 + Math.sin(e.rueda / 50) * 0.04, 0.028, 0, 6.2832); ctx.fill();
    }
    ctx.restore();
  }
  function ojos(c, x, y, sep, r, dir, enfado) {
    const ox = dir >= 0 ? DX[dir] * r * 0.4 : 0, oy = dir >= 0 ? DY[dir] * r * 0.4 : 0;
    for (const s of [-1, 1]) {
      c.fillStyle = '#fff'; c.beginPath(); c.arc(x + s * sep, y, r, 0, 6.2832); c.fill();
      c.fillStyle = '#1b2bd6'; c.beginPath(); c.arc(x + s * sep + ox, y + oy, r * 0.55, 0, 6.2832); c.fill();
      if (enfado) { c.strokeStyle = '#222'; c.lineWidth = r * 0.45; c.beginPath(); c.moveTo(x + s * sep - s * r * 1.1, y - r * 1.5); c.lineTo(x + s * sep + s * r * 0.4, y - r * 0.9); c.stroke(); }
    }
  }
  function dibujaPers(e, x, y) {
    const u = G.cs * 0.9, t = P.real / 1000 + e.fase, enCasa = e.estado === 'casa';
    ctx.save(); ctx.translate(x, y + (enCasa ? Math.sin(t * 5) * 2 : 0));
    if (e.nombre === 'ticket') {
      ctx.rotate(Math.sin(t * 7) * 0.12); ctx.scale(u, u);
      ctx.fillStyle = '#fff'; ctx.beginPath(); ctx.moveTo(-0.27, -0.42); ctx.lineTo(0.27, -0.42); ctx.lineTo(0.27, 0.3);
      for (let i = 0; i < 6; i++) ctx.lineTo(0.27 - (i + 0.5) * 0.09, 0.3 + (i % 2 ? 0 : 0.08)); ctx.lineTo(-0.27, 0.3); ctx.closePath(); ctx.fill();
      ctx.fillStyle = '#19c8f5'; for (let i = 0; i < 4; i++) ctx.fillRect(-0.18, 0.02 + i * 0.07, 0.36 - (i % 2) * 0.12, 0.025);
      ctx.fillStyle = '#19c8f5'; ctx.fillRect(-0.18, -0.36, 0.36, 0.05);
      ojos(ctx, 0, -0.18, 0.1, 0.08, e.dir, false);
      ctx.strokeStyle = '#222'; ctx.lineWidth = 0.025; ctx.beginPath(); ctx.arc(0.02, -0.06, 0.06, 0.2, 2.9); ctx.stroke();
    } else {
      ctx.rotate(t * 0.9); ctx.scale(u, u);
      ctx.fillStyle = '#ff4fb0'; ctx.beginPath();
      for (let i = 0; i < 24; i++) { const a = i * Math.PI / 12, r = i % 2 ? 0.34 : 0.46; ctx.lineTo(Math.cos(a) * r, Math.sin(a) * r); }
      ctx.closePath(); ctx.fill();
      ctx.rotate(-t * 0.9);
      ctx.save(); ctx.scale(1 / u, 1 / u); ctx.fillStyle = '#fff'; ctx.font = `900 ${Math.round(u * 0.27)}px system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.fillText('3x2', 0, 0.13 * u); ctx.restore();
      ojos(ctx, 0, -0.1, 0.1, 0.08, e.dir, false);
    }
    ctx.restore();
  }
  function dibujar() {
    ctx.clearRect(0, 0, W, H);
    dibujaMapa(); dibujaCosas();
    const muriendo = P.pausa > AJ.muerte - 800;
    for (const e of P.pers) enSitio(e, (x, y) => dibujaPers(e, x, y));
    enSitio(P.jug, (x, y) => dibujaCarrito(x, y, muriendo));
    if (P.bb) {
      const B = P.bb, p = { x: px(B.x), y: py(B.y) };
      let sx = 1, sy = 1, rot = 0, alfa = 1;
      if (B.fase === 'agachada') { sx = 1.18; sy = 0.84; }
      else if (B.fase === 'salta') {
        const f = B.t / AJ.salto, s = Math.sin(f * Math.PI), q = f < 0.15 ? 1 - f / 0.15 : f > 0.85 ? (f - 0.85) / 0.15 : 0;
        sx = 1 + 0.18 * q - 0.08 * s; sy = 1 - 0.16 * q + 0.14 * s; rot = (B.dir === 0 ? 1 : B.dir === 2 ? -1 : 0) * s * 0.12;
      } else if (B.fase === 'entra') { const f = Math.min(1, B.t / 500); sx = 0.9 + 0.1 * f; sy = 1.1 - 0.1 * f; alfa = Math.min(1, f * 3); }
      else if (B.fase === 'sale') { const f = Math.min(1, B.t / 650); sx = 0.92; sy = 1.15; alfa = 1 - f * f; }
      if (B.fase !== 'sale' && B.vida > AJ.briskaDur - 1200 && Math.floor(B.vida / 150) % 2) alfa *= 0.55;   // parpadea cuando está a punto de irse
      dibujaBriska(p, B.h, B.cara, sx, sy, rot, alfa);
    }
    if (!P.arrancado && P.intro <= 0 && !P.fin) {
      const tam = Math.round(Math.min(W, 480) * 0.05);
      ctx.save(); ctx.globalAlpha = 0.55 + 0.45 * Math.sin(P.real / 300); ctx.fillStyle = '#fff'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.font = `700 ${tam}px system-ui, sans-serif`; ctx.fillText('Desliza el dedo para moverte', W / 2, py(P.ini.y) - G.cs * 1.6); ctx.restore();
    }
    for (let i = P.efectos.length - 1; i >= 0; i--) {
      const e = P.efectos[i], f = (P.real - e.t0) / e.dur;
      if (f >= 1) { P.efectos.splice(i, 1); continue; }
      ctx.save(); ctx.globalAlpha = 1 - f;
      if (e.tipo === 'trozo') {
        const s = (P.real - e.t0) / 1000;
        ctx.fillStyle = e.col; ctx.fillRect(e.x + e.vx * s - 3, e.y + e.vy * s + 300 * s * s - 3, 6, 6);
      } else if (e.tipo === 'texto') {
        ctx.fillStyle = '#ffd98a'; ctx.font = `800 ${Math.round(G.cs * 0.6)}px system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.shadowColor = 'rgba(0,0,0,.6)'; ctx.shadowBlur = 6; ctx.fillText(e.texto, e.x, e.y - f * 24);
      } else if (e.tipo === 'bbfuera') {
        ctx.restore(); dibujaBriska({ x: e.x, y: e.y - f * 90 }, 0, e.cara, 1 + f * 0.3, 1 + f * 0.3, f * 7, 1 - f, false); continue;
      } else if (e.tipo === 'palabra') {
        // La palabra entra de golpe, se queda grande y se va al final (igual que en Arkanoid)
        const tam = Math.round(Math.min(W, 480) * 0.13), esc = Math.min(1, f * 7) * (1 + 0.04 * Math.sin(f * 20));
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
    avisar(`<img class="gl-bb-img" src="${bbImg.src}" alt="" /><p class="gl-titulo">¡Ahí viene Briskanguro!</p><p>Va dando saltitos por los pasillos del súper (dice que así quema las croquetas). Si la alcanzas con el carrito, suelta una palabra: memorízala.</p><p class="gl-bb-premio">Desliza el dedo para girar · recoge toda la compra</p>`);
    if (S.AC) { const t = S.AC.currentTime; S.boing(t + 0.1, 392, 0.25, 0.3); S.boing(t + 0.4, 523, 0.25, 0.3); S.boing(t + 0.7, 659, 0.4, 0.3); }
    ultimo = 0; cancelAnimationFrame(raf); raf = requestAnimationFrame(paso);
  }
  function cerrar() {
    cancelAnimationFrame(raf); raf = 0; pantalla.hidden = true; avisoEl.hidden = true;
    document.body.classList.remove('gl-jugando');
  }
  function perder() {
    P.fin = true; P.pausa = 0; S.gemido(0.1, false);
    avisar(`<p class="gl-titulo">¡Te han pillado!</p><p>Llevabas ${P.comidos} de ${P.total} cosas de la compra y ${P.cazadas.length} de ${n} palabras.${D.diario ? ' Mañana, la revancha.' : ' Vuelta a empezar.'}</p>`,
      D.diario ? [['Salir', cerrar, 'primario']] : [['Otra vez', () => jugar(alAcabar), 'primario'], ['Salir', cerrar]]);
    if (D.diario && A.fin) A.fin({ gano: false, palabras: P.cazadas.length });
  }
  function ganar() {
    P.fin = true; S.guau(0.05, 1.3); S.guau(0.2, 1.45); S.guau(0.35, 1.75);
    if (S.AC) { const t = S.AC.currentTime + 0.5; [523, 659, 784, 1047].forEach((f, i) => S.boing(t + i * 0.13, f, 0.3, 0.25)); }
    A.celebrar();
    const cazadas = P.cazadas.slice(), faltan = n - cazadas.length;
    avisar(`<p class="gl-titulo">¡Compra hecha!</p><p>Has cazado <b>${cazadas.length}</b> de ${n} palabras${faltan ? `; ${faltan} se te ${faltan === 1 ? 'escapó' : 'escaparon'} con Briskanguro` : ''}.</p>`,
      [[D.resuelta ? 'Salir' : 'Escribir la frase', () => { cerrar(); alAcabar && alAcabar(cazadas); }, 'primario']]);
  }

  // ---------- Toques: deslizar el dedo pide una dirección (se puede seguir arrastrando para encadenar giros) ----------
  let dedo = null, ancla = null;
  avisoEl.addEventListener('click', () => { if (P && P.intro > 0 && P.intro < AJ.entradilla - 500) P.intro = 0.001; });
  function pide(d) {
    if (!P || P.fin || P.intro > 0) return;
    P.jug.want = d;
    if (!P.arrancado) arrancar();
  }
  cv.addEventListener('pointerdown', e => {
    e.preventDefault();
    dedo = e.pointerId; ancla = { x: e.clientX, y: e.clientY }; try { cv.setPointerCapture(e.pointerId); } catch { }
  }, { passive: false });
  cv.addEventListener('pointermove', e => {
    if (e.pointerId !== dedo || !ancla) return;
    e.preventDefault();
    const dx = e.clientX - ancla.x, dy = e.clientY - ancla.y;
    if (Math.abs(dx) < AJ.swipe && Math.abs(dy) < AJ.swipe) return;
    pide(Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? 0 : 2) : (dy > 0 ? 1 : 3));
    ancla = { x: e.clientX, y: e.clientY };
  }, { passive: false });
  const suelta = e => { if (e.pointerId === dedo) { dedo = null; ancla = null; } };
  cv.addEventListener('pointerup', suelta); cv.addEventListener('pointercancel', suelta);
  // Teclado (por si se prueba en un ordenador)
  document.addEventListener('keydown', e => {
    if (pantalla.hidden) return;
    const d = { ArrowRight: 0, ArrowDown: 1, ArrowLeft: 2, ArrowUp: 3 }[e.key];
    if (d !== undefined) { e.preventDefault(); pide(d); }
  });
  $('cc-salir').addEventListener('click', cerrar);
  window.addEventListener('resize', () => { if (!pantalla.hidden) medir(); });

  $('gl-empezar').addEventListener('click', () => jugar(F.empezar));
})();
