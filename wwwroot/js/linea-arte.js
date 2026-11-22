// Línea del tiempo del arte: 5 obras que se colocan una a una en el tiempo; acertar el autor da un punto extra.
// Presenta BriskaBird con boina de pintora. Una partida por semana (el servidor la apunta al empezar).
(() => {
  const D = JSON.parse(document.getElementById('la-datos').textContent);
  const $ = id => document.getElementById(id);
  const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
  const post = (h, body) => fetch(`${location.pathname}?handler=${h}`, { method: 'POST', headers: { 'Content-Type': 'application/json', RequestVerificationToken: token }, body: JSON.stringify(body || {}) }).then(r => r.json()).catch(() => ({ ok: true }));
  const img = id => `/Juegos/Linea?handler=Imagen&id=${encodeURIComponent(id)}`;
  const azar = a => a[Math.floor(Math.random() * a.length)];
  const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

  // ---------- Lo que dice BriskaBird ----------
  const DICE = {
    hola: [
      '¡Bienvenida a mi museo! Soy BriskaBird, crítica de arte: mitad labrador, mitad pájaro y cien por cien experta en marcos (los muerdo).',
      'Buenas. Esta boina me da autoridad artística. El pincel no lo uso, pero queda divino.',
      'Hoy te traigo cinco obras maestras. Las he olido todas: el Barroco huele a perro mojado.',
    ],
    reglas: 'Te enseño cinco cuadros, uno a uno. Me dices quién lo pintó (punto extra) y lo pones en su sitio en la línea del tiempo. ¿Lista?',
    autor: ['¿Quién pintó esto? Pista: no fui yo.', '¿De quién es? Yo diría que de alguien con pincel.', 'A ver ese ojo: ¿quién lo pintó?', '¿Autor? Yo sé quién es, pero tengo la boca llena de galleta.'],
    autorBien: ['¡Exacto! Has visto más cuadros que yo pelotas.', '¡Correcto! Me dan ganas de mover la cola… la de pájaro.', '¡Bravo! Te pondría un hueso dorado en el marco.', '¡Eso es! Tienes más ojo que yo olfato. Y mira que huelo.'],
    autorMal: ['Nooo. Era de {autor}. No se lo cuentes, que es muy sensible.', 'Casi… bueno, no. Era {autor}. Yo también los confundo cuando tengo hambre.', 'Era {autor}. Pero tu respuesta tenía estilo, eso no te lo quita nadie.'],
    primera: 'Esta la cuelgo yo, gratis: es de {anio}. Soy muy generosa con lo que no es mío.',
    donde: ['¿Dónde la cuelgo? Toca el hueco: antes, entre o después.', 'Ahora, en la línea del tiempo. ¿Más antigua o más moderna?', '¿En qué hueco va? Piensa como un historiador. O como yo: a ojo.'],
    sitioBien: ['¡En su sitio! Es de {anio}. Ni yo con mi olfato.', '¡Perfecto! {anio}. Eso es tener ojo… y boina.', '¡Clavado! {anio}. Me quito la boina (no, que se me despeina la oreja).'],
    sitioMal: ['Uy, es de {anio}. El tiempo vuela, como yo.', 'Se te ha colado algún siglo: es de {anio}. Pasa en las mejores familias.', 'Era de {anio}. Te la coloco yo, que para algo tengo alas.'],
    fin: [
      [9, '¡Matrícula de honor! Te nombro conservadora jefa de mi museo. El sueldo, en galletas.'],
      [7, '¡Tremendo! El Prado te va a llamar. Bueno, te llamo yo, ladrando.'],
      [4, 'Nada mal. Un par de domingos más de museo y me quitas el puesto.'],
      [0, 'Bueno… el arte es subjetivo. Y el tiempo, también. La semana que viene, revancha.'],
    ],
  };
  const fmt = (s, c) => s.replace('{autor}', c ? c.autor : '').replace('{anio}', c ? c.anio : '');
  function habla(txt, alegre) {
    const d = $('la-dice'); d.textContent = txt; d.classList.remove('nuevo'); void d.offsetWidth; d.classList.add('nuevo');
    const b = $('la-briska'); b.classList.remove('salta', 'triste'); void b.offsetWidth; if (alegre != null) b.classList.add(alegre ? 'salta' : 'triste');
  }

  // ---------- Sonidos sintetizados (guau, acierto, fallo) ----------
  let AC = null;
  const audio = () => { try { AC = AC || new (window.AudioContext || window.webkitAudioContext)(); if (AC.state === 'suspended') AC.resume(); } catch { AC = null; } };
  function tono(f, t, dur, tipo, vol) {
    if (!AC) return; const o = AC.createOscillator(), g = AC.createGain(); o.type = tipo || 'sine'; o.frequency.setValueAtTime(f, AC.currentTime + t);
    g.gain.setValueAtTime(vol || 0.2, AC.currentTime + t); g.gain.exponentialRampToValueAtTime(0.0001, AC.currentTime + t + dur);
    o.connect(g); g.connect(AC.destination); o.start(AC.currentTime + t); o.stop(AC.currentTime + t + dur + 0.02);
  }
  function guau(t) {
    if (!AC) return; const o = AC.createOscillator(), g = AC.createGain(), f = AC.createBiquadFilter(), t0 = AC.currentTime + (t || 0);
    o.type = 'sawtooth'; o.frequency.setValueAtTime(420, t0); o.frequency.exponentialRampToValueAtTime(190, t0 + 0.16);
    f.type = 'bandpass'; f.frequency.value = 900; f.Q.value = 1.4;
    g.gain.setValueAtTime(0.0001, t0); g.gain.exponentialRampToValueAtTime(0.35, t0 + 0.02); g.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.2);
    o.connect(f); f.connect(g); g.connect(AC.destination); o.start(t0); o.stop(t0 + 0.22);
  }
  const bien = () => { tono(660, 0, 0.12, 'triangle'); tono(880, 0.1, 0.18, 'triangle'); guau(0.3); };
  const mal = () => { tono(220, 0, 0.25, 'square', 0.08); tono(165, 0.18, 0.3, 'square', 0.08); };

  // ---------- Partida ----------
  const cartas = D.cartas;
  let i = 0, puntos = 0;
  const linea = [];   // cartas colgadas, ordenadas por año: { c, res: 'ok'|'mal'|'gratis' }

  function pintaLinea(huecos) {
    const ol = $('la-linea');
    if (!linea.length) { ol.innerHTML = ''; return; }
    const hueco = k => huecos ? `<li class="la-hueco"><button type="button" data-k="${k}">${k === 0 ? '⬆ Más antigua' : k === linea.length ? '⬇ Más moderna' : '↔ Aquí, entre medias'}</button></li>` : '';
    ol.innerHTML = linea.map((x, k) => hueco(k) + `<li class="la-item ${x.res} ${x.nueva ? 'nueva' : ''}"><img src="${img(x.c.id)}" alt="" /><span><b>${x.c.anio}</b> · ${esc(x.c.titulo)}<br /><span class="suave peq">${esc(x.c.autor)}</span></span></li>`).join('') + hueco(linea.length);
    linea.forEach(x => x.nueva = false);
    if (huecos) ol.querySelectorAll('.la-hueco button').forEach(b => b.addEventListener('click', () => coloca(+b.dataset.k)));
  }

  function escena(html) { $('la-escena').innerHTML = html; }

  function intro() {
    habla(azar(DICE.hola));
    escena(`<p class="la-reglas">${DICE.reglas}</p><button class="btn primario ancho" id="la-empezar" type="button">🎨 Abrir el museo</button>`);
    $('la-empezar').addEventListener('click', async () => {
      audio(); guau(0); guau(0.25);
      const r = await post('Empezar');
      if (r && r.ok === false) { habla(r.mensaje, false); return; }
      carta();
    });
  }

  function carta() {
    const c = cartas[i];
    habla(azar(DICE.autor));
    escena(`<div class="la-cuadro"><span class="la-num">${i + 1} / ${cartas.length}</span><img src="${img(c.id)}" alt="Obra ${i + 1}" /></div>
      <p class="la-pregunta">¿Quién la pintó?</p>
      <div class="la-autores">${c.autores.map(a => `<button type="button" class="chip" data-a="${esc(a)}">${esc(a)}</button>`).join('')}</div>`);
    $('la-escena').querySelectorAll('.la-autores button').forEach(b => b.addEventListener('click', () => autor(b)));
    pintaLinea(false);
  }

  function autor(b) {
    const c = cartas[i], ok = b.dataset.a === c.autor;
    $('la-escena').querySelectorAll('.la-autores button').forEach(x => { x.disabled = true; if (x.dataset.a === c.autor) x.classList.add('on'); else if (x === b) x.classList.add('mal'); });
    if (ok) { puntos++; bien(); habla(azar(DICE.autorBien), true); } else { mal(); habla(fmt(azar(DICE.autorMal), c), false); }
    const p = document.createElement('p'); p.className = 'la-titulo'; p.innerHTML = `«${esc(c.titulo)}»`;
    $('la-escena').querySelector('.la-cuadro').after(p);
    setTimeout(() => {
      if (i === 0) {   // la primera se cuelga sola
        linea.push({ c, res: 'gratis', nueva: true });
        habla(fmt(DICE.primera, c));
        siguienteBoton();
        pintaLinea(false);
      } else {
        habla(azar(DICE.donde));
        $('la-escena').querySelector('.la-autores').remove();
        $('la-escena').querySelector('.la-pregunta').textContent = '¿Dónde la cuelgo?';
        pintaLinea(true);
        $('la-linea').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
      }
    }, 1500);
  }

  function coloca(k) {
    const c = cartas[i];
    const antes = k > 0 ? linea[k - 1].c.anio : -Infinity, despues = k < linea.length ? linea[k].c.anio : Infinity;
    const ok = c.anio >= antes && c.anio <= despues;
    if (ok) { puntos++; bien(); habla(fmt(azar(DICE.sitioBien), c), true); } else { mal(); habla(fmt(azar(DICE.sitioMal), c), false); }
    linea.push({ c, res: ok ? 'ok' : 'mal', nueva: true });
    linea.sort((a, b) => a.c.anio - b.c.anio);
    pintaLinea(false);
    siguienteBoton();
  }

  function siguienteBoton() {
    const ultima = i === cartas.length - 1;
    const p = $('la-escena').querySelector('.la-pregunta'); if (p) p.remove();
    const a = $('la-escena').querySelector('.la-autores'); if (a) a.remove();
    const b = document.createElement('button'); b.type = 'button'; b.className = 'btn primario ancho'; b.textContent = ultima ? '🏁 Ver mi nota' : 'Siguiente cuadro →';
    b.addEventListener('click', () => { i++; if (ultima) fin(); else carta(); });
    $('la-escena').appendChild(b);
    $('la-escena').scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  async function fin() {
    const frase = DICE.fin.find(([m]) => puntos >= m)[1];
    habla(frase, puntos >= 4);
    if (puntos >= 7) { guau(0); guau(0.25); guau(0.5); }
    const otros = (D.otros || []).map(o => `<p class="centrado">${esc(o.nombre)}: <b>${o.puntos}</b> de ${D.maximo}</p>`).join('');
    escena(`<div class="la-nota"><span>${puntos}</span> de ${D.maximo}</div>
      <p class="suave peq centrado">1 punto por cada cuadro bien colgado y 1 por cada autor acertado.</p>${otros}
      ${D.admin ? '<button class="btn ancho" type="button" onclick="location.reload()">Otra vez</button>' : '<p class="centrado suave">El lunes, cinco obras nuevas.</p>'}`);
    pintaLinea(false);
    await post('Terminar', { puntos });
  }

  intro();
})();
