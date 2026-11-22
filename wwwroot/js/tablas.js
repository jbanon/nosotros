// Tablas de multiplicar: aprender (con trucos), reto de 15 preguntas que da estrellas por tabla y relámpago de 60 s.
// Las preguntas falladas salen más a menudo hasta que se aciertan. Todo se guarda en el móvil (por usuario).
(() => {
  const $ = id => document.getElementById(id);
  const raiz = $('tb'); if (!raiz) return;
  const CLAVE = 'tablas:' + (raiz.dataset.uid || '0');
  const lee = () => { try { return JSON.parse(localStorage.getItem(CLAVE) || 'null'); } catch { return null; } };
  const E = Object.assign({ dom: {}, fallos: {}, relampago: 0, sonido: true }, lee() || {});
  const guarda = () => { try { localStorage.setItem(CLAVE, JSON.stringify(E)); } catch { } };
  const zumba = ms => { try { navigator.vibrate && navigator.vibrate(ms); } catch { } };
  const azar = a => a[Math.floor(Math.random() * a.length)];

  // ---------- Estrellas y niveles ----------
  const UMBRALES = [8, 20, 40];   // puntos de dominio para 1, 2 y 3 estrellas
  const estrellas = t => UMBRALES.filter(u => (E.dom[t] || 0) >= u).length;
  const NIVELES = [[0, 'Aprendiz'], [3, 'Corista'], [8, 'Bailarín de fondo'], [14, 'Solista'], [20, 'Estrella'], [26, 'Superestrella'], [30, 'Leyenda de las tablas 👑']];
  const total = () => Array.from({ length: 10 }, (_, i) => estrellas(i + 1)).reduce((a, b) => a + b, 0);

  // ---------- Sonido (sin ficheros) ----------
  let ac = null;
  function tono(f, tipo, dur, ret = 0, vol = .1) {
    if (!E.sonido) return;
    try {
      ac = ac || new (window.AudioContext || window.webkitAudioContext)();
      if (ac.state === 'suspended') ac.resume();
      const o = ac.createOscillator(), g = ac.createGain(), t0 = ac.currentTime + ret;
      o.type = tipo; o.frequency.setValueAtTime(f, t0);
      g.gain.setValueAtTime(vol, t0); g.gain.exponentialRampToValueAtTime(.001, t0 + dur);
      o.connect(g); g.connect(ac.destination); o.start(t0); o.stop(t0 + dur);
    } catch { }
  }
  const S = {
    clic: () => tono(760, 'sine', .04, 0, .03),
    bien: r => { const s = Math.min(r * 30, 300); tono(523 + s, 'sine', .13); tono(659 + s, 'triangle', .2, .07); },
    mal: () => { tono(220, 'sawtooth', .16, 0, .07); tono(170, 'sawtooth', .22, .1, .07); },
    fanfarria: () => [523, 659, 784, 1047].forEach((n, i) => tono(n, 'triangle', .25, i * .1)),
  };
  const btnSonido = $('tb-sonido');
  const pintaSonido = () => btnSonido.textContent = E.sonido ? '🔊' : '🔇';
  btnSonido.addEventListener('click', () => { E.sonido = !E.sonido; guarda(); pintaSonido(); S.clic(); });
  pintaSonido();

  // ---------- Pantallas ----------
  const pantallas = ['tb-menu', 'tb-aprender', 'tb-juego', 'tb-fin'];
  function ver(id) { pantallas.forEach(p => $(p).hidden = p !== id); window.scrollTo({ top: 0 }); }
  raiz.querySelectorAll('[data-volver]').forEach(b => b.addEventListener('click', () => { S.clic(); menu(); }));

  // ---------- Menú ----------
  const elegidas = new Set();
  function menu() {
    const n = total(), nivel = NIVELES.filter(x => n >= x[0]).pop(), sig = NIVELES.find(x => x[0] > n);
    $('tb-total').textContent = n; $('tb-nivel').textContent = nivel[1];
    $('tb-barra').style.width = (n / 30 * 100) + '%';
    $('tb-sig').textContent = sig ? `${sig[0] - n} ⭐ más para «${sig[1]}»` : '¡Lo tienes todo! Ahora, a por el récord del relámpago.';
    $('tb-record').textContent = E.relampago ? `récord: ${E.relampago} aciertos` : '¿cuántas aciertas?';
    const mapa = $('tb-mapa'); mapa.innerHTML = '';
    for (let t = 1; t <= 10; t++) {
      const b = document.createElement('button'); b.type = 'button';
      const e = estrellas(t);
      b.className = 'tb-t' + (elegidas.has(t) ? ' sel' : '') + (e === 3 ? ' oro' : '');
      b.style.setProperty('--h', (t * 36 + 280) % 360);
      b.innerHTML = `<b>${t}</b><span>${'★'.repeat(e)}${'☆'.repeat(3 - e)}</span>`;
      b.addEventListener('click', () => { S.clic(); elegidas.has(t) ? elegidas.delete(t) : elegidas.add(t); menu(); });
      mapa.appendChild(b);
    }
    ver('tb-menu');
  }
  raiz.querySelectorAll('.tb-modo').forEach(b => b.addEventListener('click', () => {
    S.clic();
    if (b.dataset.modo === 'aprender') aprender(elegidas.size ? Math.min(...elegidas) : 2);
    else jugar(b.dataset.modo);
  }));

  // ---------- Aprender ----------
  let tablaVista = 2, tapar = false;
  function truco(a, b) {
    const r = a * b, o = (x) => (a === x ? b : a);
    if (a === b) return ['Número cuadrado', `${a} × ${a} = ${r}: es un cuadrado perfecto. Si dibujas un cuadrado de ${a} cuadritos de lado, tiene ${r} cuadritos.`];
    if (a === 9 || b === 9) { const x = o(9); return ['Truco del 9', `Las decenas son uno menos que ${x} (${x - 1}) y las dos cifras suman 9: ${x - 1} + ${9 - (x - 1)} = 9 → ${r}. También: abre las 10 manos, baja el dedo ${x} y cuenta los de cada lado.`]; }
    if (a === 10 || b === 10) return ['Truco del 10', `Pon un cero detrás del ${o(10)}: ${r}. Facilísimo.`];
    if (a === 5 || b === 5) { const x = o(5); return ['Truco del 5', `Multiplica por 10 y haz la mitad: ${x} × 10 = ${x * 10}, la mitad es ${r}. Además, la del 5 siempre acaba en 0 o en 5.`]; }
    if (a === 2 || b === 2) return ['Truco del 2', `Por 2 es el doble: ${o(2)} + ${o(2)} = ${r}.`];
    if (a === 4 || b === 4) { const x = o(4); return ['Doble del doble', `El doble de ${x} es ${x * 2}, y el doble de ${x * 2} es ${r}.`]; }
    if (a === 8 || b === 8) { const x = o(8); return ['Doble, doble y doble', `${x} → ${x * 2} → ${x * 4} → ${r}. Tres veces el doble.`]; }
    if (a === 1 || b === 1) return ['El 1 no cambia nada', `Cualquier número por 1 se queda igual: ${r}.`];
    if (a === 6 || b === 6) { const x = o(6); return ['Truco del 6', x % 2 === 0 ? `Si multiplicas 6 por un número par, el resultado acaba igual que ese número: 6 × ${x} = ${r}.` : `6 × ${x} es 5 × ${x} (${5 * x}) más otro ${x}: ${r}.`]; }
    if (a === 3 || b === 3) { const x = o(3); return ['Truco del 3', `El doble más uno más: ${x} + ${x} + ${x} = ${r}.`]; }
    if (r === 56) return ['La rebelde', '7 × 8 = 56… ¡5, 6, 7, 8! Los números van en orden. Ya no se te olvida.'];
    return ['Da la vuelta', `${a} × ${b} es lo mismo que ${b} × ${a} = ${r}. Si te sabes una, te sabes las dos.`];
  }
  function aprender(t) {
    tablaVista = t;
    const chips = $('tb-chips'); chips.innerHTML = '';
    for (let i = 1; i <= 10; i++) {
      const c = document.createElement('button'); c.type = 'button'; c.textContent = i;
      c.className = 'tb-chip' + (i === t ? ' sel' : ''); c.style.setProperty('--h', (i * 36 + 280) % 360);
      c.addEventListener('click', () => { S.clic(); aprender(i); });
      chips.appendChild(c);
    }
    const tabla = $('tb-tabla'); tabla.innerHTML = ''; tabla.style.setProperty('--h', (t * 36 + 280) % 360);
    for (let m = 1; m <= 10; m++) {
      const f = document.createElement('button'); f.type = 'button'; f.className = 'tb-fila' + (tapar ? ' tapada' : '');
      f.innerHTML = `<span>${t} × ${m}</span><b>${t * m}</b>`;
      f.addEventListener('click', () => {
        S.clic(); f.classList.toggle('vista');
        tabla.querySelectorAll('.tb-fila').forEach(x => x.classList.toggle('activa', x === f));
        const [tit, txt] = truco(t, m);
        $('tb-truco').innerHTML = `<b>💡 ${tit}</b><p>${txt}</p>`;
      });
      tabla.appendChild(f);
    }
    $('tb-tapar').textContent = tapar ? '👀 Destapar' : '🙈 Tapar resultados';
    $('tb-retar').textContent = `🎯 Reto del ${t}`;
    $('tb-truco').innerHTML = '<b>💡 Toca una multiplicación</b><p>y te cuento un truco para no olvidarla.</p>';
    ver('tb-aprender');
  }
  $('tb-tapar').addEventListener('click', () => { tapar = !tapar; S.clic(); aprender(tablaVista); });
  $('tb-retar').addEventListener('click', () => { S.clic(); jugar('reto', [tablaVista]); });

  // ---------- Juego ----------
  const RETO = 15, RELAMPAGO = 60;
  let J = null;
  const COMBOS = { 3: '¡Combo!', 5: '¡En racha! 🔥', 8: '¡Imparable!', 10: '¡Ovación! 👏', 15: '¡Modo leyenda!', 20: '¡Esto es un concierto! 🎤', 30: '¡Récord de fans! 🌟' };
  const BRISKA = ['¡BriskaBird está flipando!', 'Pío, pío… ¡qué crack!', '¡Calculadora con patas!', 'BriskaBird pide un autógrafo', '¡Eso ha sonado a éxito!'];

  function pool(tablas) {
    const ts = tablas && tablas.length ? tablas : [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    const p = [];
    ts.forEach(a => { for (let b = 1; b <= 10; b++) p.push([a, b]); });
    return p;
  }
  function siguienteHecho() {
    if (J.repaso.length && J.repaso[0].n <= J.hechas) return J.repaso.shift().h;
    let tot = 0;
    const pesos = J.pool.map(([a, b]) => {
      const w = (1 + 2 * (E.fallos[a + 'x' + b] || 0)) * (b === 1 || b === 10 || a === 1 || a === 10 ? .4 : 1) * (J.ultimo === a + 'x' + b ? 0 : 1);
      tot += w; return w;
    });
    let r = Math.random() * tot;
    for (let i = 0; i < pesos.length; i++) { r -= pesos[i]; if (r <= 0) return J.pool[i]; }
    return J.pool[0];
  }
  function jugar(modo, tablas, lista) {
    const ts = tablas || [...elegidas];
    J = { modo, tablas: ts, pool: lista ? lista : pool(ts), lista: !!lista, hechas: 0, bien: 0, puntos: 0, racha: 0, mejor: 0, falladas: [], repaso: [],
          ganado: {}, antes: Array.from({ length: 10 }, (_, i) => estrellas(i + 1)), fin: false, entrada: '', ocupado: false, ultimo: '' };
    if (lista) J.cola = [...lista];
    $('tb-puntos').textContent = 0; pintaRacha();
    ver('tb-juego');
    if (modo === 'relampago') { J.t0 = performance.now(); reloj(); }
    pregunta();
  }
  function reloj() {
    if (!J || J.fin) return;
    const q = Math.max(0, RELAMPAGO - (performance.now() - J.t0) / 1000);
    $('tb-prog').style.width = (q / RELAMPAGO * 100) + '%';
    $('tb-prog-txt').textContent = `⚡ ${Math.ceil(q)} s`;
    raiz.classList.toggle('tb-apura', q < 10);
    if (q <= 0) return terminar();
    requestAnimationFrame(reloj);
  }
  function pregunta() {
    if (J.modo !== 'relampago' && J.hechas >= (J.lista ? J.pool.length : RETO)) return terminar();
    let h = J.lista ? J.cola.shift() : siguienteHecho();
    J.ultimo = h[0] + 'x' + h[1];
    J.q = Math.random() < .5 ? [h[0], h[1]] : [h[1], h[0]];
    J.hecho = h; J.entrada = ''; J.ocupado = false;
    $('tb-a').textContent = J.q[0]; $('tb-b').textContent = J.q[1];
    pintaResp();
    $('tb-resp').className = 'tb-resp';
    $('tb-aviso').textContent = '¿Cuánto es?'; $('tb-correccion').textContent = '';
    if (J.modo !== 'relampago') {
      const n = J.lista ? J.pool.length : RETO;
      $('tb-prog-txt').textContent = `${J.hechas + 1} de ${n}`;
      $('tb-prog').style.width = (J.hechas / n * 100) + '%';
    }
  }
  const pintaResp = () => $('tb-resp').textContent = J.entrada || '?';
  function pintaRacha() {
    $('tb-racha').textContent = J ? J.racha : 0;
    $('tb-fuego').hidden = !J || J.racha < 3;
    $('tb-racha-caja').classList.toggle('caliente', !!J && J.racha >= 3);
  }
  $('tb-teclado').addEventListener('click', e => {
    const b = e.target.closest('button'); if (!b || !J || J.ocupado || J.fin) return;
    S.clic();
    const k = b.dataset.k;
    if (k === 'borrar') J.entrada = J.entrada.slice(0, -1);
    else if (k === 'ok') return comprobar();
    else if (J.entrada.length < 3) J.entrada += b.textContent;
    pintaResp();
    // Se comprueba sola al escribir tantas cifras como tiene el resultado
    if (J.entrada.length >= String(J.q[0] * J.q[1]).length) setTimeout(comprobar, 120);
  });
  document.addEventListener('keydown', e => {
    if ($('tb-juego').hidden || !J) return;
    if (/^[0-9]$/.test(e.key)) [...$('tb-teclado').children].find(b => b.textContent === e.key)?.click();
    else if (e.key === 'Backspace') $('tb-teclado').querySelector('[data-k=borrar]').click();
    else if (e.key === 'Enter') comprobar();
  });
  function comprobar() {
    if (!J || J.ocupado || J.fin || !J.entrada) return;
    J.ocupado = true;
    const [a, b] = J.q, r = a * b, clave = J.hecho[0] + 'x' + J.hecho[1], ok = parseInt(J.entrada, 10) === r;
    J.hechas++;
    const t = J.hecho[0];
    if (ok) {
      J.bien++; J.racha++; J.mejor = Math.max(J.mejor, J.racha);
      const pts = 100 + Math.min(J.racha * 10, 100); J.puntos += pts;
      if (E.fallos[clave]) E.fallos[clave] = Math.max(0, E.fallos[clave] - 1);
      sumaDominio(a, b, 1);
      S.bien(J.racha); zumba(15);
      $('tb-resp').className = 'tb-resp bien';
      $('tb-aviso').textContent = COMBOS[J.racha] || `+${pts}`;
      $('tb-pregunta').classList.remove('pop'); void $('tb-pregunta').offsetWidth; $('tb-pregunta').classList.add('pop');
      if (J.racha % 5 === 0) { briska(); confeti(30); }
    } else {
      J.racha = 0;
      E.fallos[clave] = (E.fallos[clave] || 0) + 1;
      sumaDominio(a, b, -2);
      if (!J.falladas.some(f => f[0] === J.hecho[0] && f[1] === J.hecho[1])) J.falladas.push(J.hecho);
      if (J.modo === 'reto' && !J.lista) J.repaso.push({ h: J.hecho, n: J.hechas + 3 });   // vuelve a salir dentro de 3
      S.mal(); zumba([50, 40, 50]);
      $('tb-resp').className = 'tb-resp mal';
      $('tb-aviso').textContent = '¡Uy! Casi';
      $('tb-correccion').innerHTML = `${a} × ${b} = <b>${r}</b> · ${truco(a, b)[1]}`;
    }
    guarda();
    $('tb-puntos').textContent = J.puntos; pintaRacha();
    setTimeout(() => { if (!J.fin) pregunta(); }, ok ? (J.modo === 'relampago' ? 350 : 650) : 3200);
  }
  function sumaDominio(a, b, d) {
    if (J.modo === 'aprender') return;
    [...new Set([a, b])].forEach(t => {
      if (J.tablas.length && !J.tablas.includes(t) && !J.lista) return;
      E.dom[t] = Math.max(0, (E.dom[t] || 0) + d);
    });
  }
  function briska() {
    const bb = $('tb-bb'); bb.hidden = false; bb.title = azar(BRISKA);
    bb.classList.remove('sale'); void bb.offsetWidth; bb.classList.add('sale');
    const d = document.createElement('div'); d.className = 'tb-bb-dice'; d.textContent = bb.title; raiz.appendChild(d);
    setTimeout(() => { bb.hidden = true; d.remove(); }, 1900);
  }
  $('tb-salir').addEventListener('click', () => { S.clic(); J && J.hechas ? terminar() : menu(); });

  function terminar() {
    if (!J || J.fin) return;
    J.fin = true; raiz.classList.remove('tb-apura');
    const pct = J.hechas ? Math.round(J.bien / J.hechas * 100) : 0;
    $('tb-f-puntos').textContent = J.puntos; $('tb-f-acierto').textContent = pct + '%'; $('tb-f-racha').textContent = J.mejor;
    let ico, tit, sub;
    if (J.modo === 'relampago') {
      const record = J.bien > (E.relampago || 0);
      if (record) { E.relampago = J.bien; guarda(); }
      ico = record ? '🏆' : '⚡'; tit = `${J.bien} aciertos en 60 s`; sub = record ? '¡Nuevo récord! Las tablas tiemblan.' : `Tu récord está en ${E.relampago}. ¡A por él!`;
    } else if (pct >= 90) { ico = '🏆'; tit = '¡Nivel maestro!'; sub = 'Eso ha sido un concierto de multiplicaciones.'; }
    else if (pct >= 70) { ico = '🚀'; tit = '¡Muy bien!'; sub = 'Repasa las que se escaparon y la próxima es tuya.'; }
    else { ico = '💪'; tit = '¡Buen entrenamiento!'; sub = 'Cada partida te hace más rápida. Las falladas saldrán más, hasta que caigan.'; }
    $('tb-fin-ico').textContent = ico; $('tb-fin-tit').textContent = tit; $('tb-fin-sub').textContent = sub;
    const subidas = [];
    for (let t = 1; t <= 10; t++) { const ahora = estrellas(t); if (ahora > J.antes[t - 1]) subidas.push(`La del ${t}: ${'★'.repeat(ahora)}${'☆'.repeat(3 - ahora)}`); }
    $('tb-ganadas').innerHTML = subidas.length ? `<p><b>⭐ ¡Estrellas nuevas!</b></p><p>${subidas.join('<br>')}</p>` : '';
    $('tb-falladas').hidden = !J.falladas.length;
    $('tb-falladas-lista').innerHTML = J.falladas.map(([a, b]) => `<span class="tb-chipfallo">${a} × ${b} = <b>${a * b}</b></span>`).join('');
    if (pct >= 70 || subidas.length) { S.fanfarria(); confeti(subidas.length ? 70 : 40); }
    ver('tb-fin');
  }
  $('tb-otra').addEventListener('click', () => { S.clic(); jugar(J.modo === 'relampago' ? 'relampago' : 'reto', J.tablas); });
  $('tb-practicar').addEventListener('click', () => { S.clic(); jugar('reto', J.tablas, J.falladas.map(h => [...h])); });

  function confeti(n) {
    const cols = ['#ff5fa2', '#b36bff', '#4dd6ff', '#ffd84d', '#5dffa8'];
    for (let i = 0; i < n; i++) {
      const s = document.createElement('i'); s.className = 'cf-confeti';
      s.style.cssText = `--x:${(Math.random() * 2 - 1) * 46}vw;--y:${-30 - Math.random() * 50}vh;--r:${Math.random() * 720 - 360}deg;background:${cols[i % cols.length]};left:50%;animation-delay:${Math.random() * .15}s`;
      document.body.appendChild(s); setTimeout(() => s.remove(), 1700);
    }
  }

  menu();
})();
