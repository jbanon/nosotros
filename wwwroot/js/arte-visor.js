// Visor de Raquel: paso 0 = obra entera con la intro; 1..n = paradas; n+1 = vuelta a la vista completa con la frase.
(() => {
  const d = JSON.parse(document.getElementById('datos').textContent);
  const escena = document.getElementById('escena'), img = document.getElementById('obra'), tarjeta = document.getElementById('tarjeta');
  const contenido = document.getElementById('contenido'), puntos = document.getElementById('puntos');
  const btnSig = document.getElementById('sig'), btnAtras = document.getElementById('atras'), btnExplorar = document.getElementById('explorar');
  const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
  const post = (h, body) => fetch(`${location.pathname}?handler=${h}`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify(body || {}) }).then(r => r.ok ? r.json() : null).catch(() => null);
  const esc = s => (s || '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
  const parrafos = s => (s || '').split(/\n+/).filter(Boolean).map(p => `<p>${esc(p)}</p>`).join('');

  const visor = VisorArte(escena, img, { ancho: d.ancho, alto: d.alto, marcador: document.getElementById('marca'), margenInferior: () => tarjeta.offsetHeight });
  const total = d.paradas.length + 2; // intro + paradas + final
  let paso = 0, vista = false;

  // Primero la versión reducida; la grande se descarga por detrás y la sustituye sin salto.
  // Solo la primera carga encuadra la obra entera; al cambiar a la grande se mantiene la parada en curso.
  let primeraCarga = true;
  img.onload = () => { if (!primeraCarga) return; primeraCarga = false; document.getElementById('cargando')?.remove(); visor.recolocar(); };
  img.src = d.reducida;
  const grande = new Image();
  grande.src = d.grande;
  (grande.decode ? grande.decode() : new Promise(r => grande.onload = r)).then(() => { img.src = d.grande; }).catch(() => {});

  for (let i = 0; i < total; i++) { const b = document.createElement('i'); puntos.appendChild(b); }

  function pintar() {
    [...puntos.children].forEach((b, i) => b.className = i === paso ? 'on' : i < paso ? 'hecho' : '');
    btnAtras.disabled = paso === 0;
    if (paso === 0) {
      contenido.innerHTML = `<p class="visor-sobre">${esc([d.autor, d.anio].filter(Boolean).join(' · '))}${d.museo ? ' · ' + esc(d.museo) : ''}</p><h2>${esc(d.titulo)}</h2>${parrafos(d.intro)}`;
      btnSig.textContent = d.paradas.length ? `Empezar · ${d.paradas.length} detalles` : 'Ver la obra';
      visor.irA({ zoom: 1 });
    } else if (paso <= d.paradas.length) {
      const p = d.paradas[paso - 1];
      contenido.innerHTML = `<p class="visor-sobre">${paso} de ${d.paradas.length}</p><h2>${esc(p.titulo)}</h2>${parrafos(p.texto)}`;
      btnSig.textContent = paso === d.paradas.length ? 'Ver la obra entera' : 'Siguiente';
      visor.irA(p);
    } else {
      contenido.innerHTML = (d.frase ? `<div class="visor-frase">${parrafos(d.frase)}<span>— ${esc(d.firma)}</span></div>` : `<h2>${esc(d.titulo)}</h2>`)
        + `<label class="visor-respuesta"><span>¿Me dices algo?</span><textarea id="resp" rows="2" placeholder="Una línea basta…">${esc(d.respuesta)}</textarea></label>`;
      btnSig.textContent = 'Volver a empezar';
      visor.irA({ zoom: 1 });
      if (!vista) { vista = true; post('Vista'); }
      const resp = document.getElementById('resp');
      let t; resp.addEventListener('input', () => { clearTimeout(t); t = setTimeout(() => { d.respuesta = resp.value; post('Responder', { texto: resp.value }); }, 900); });
    }
    // La tarjeta cambia de alto con el texto: recolocar el encuadre cuando se haya pintado.
    requestAnimationFrame(() => { if (!visor.libre) visor.irA(paso === 0 || paso > d.paradas.length ? { zoom: 1 } : d.paradas[paso - 1]); });
  }

  const siguiente = () => { if (visor.libre) return; paso = paso >= total - 1 ? 0 : paso + 1; pintar(); };
  const anterior = () => { if (visor.libre || paso === 0) return; paso--; pintar(); };
  btnSig.addEventListener('click', siguiente);
  btnAtras.addEventListener('click', anterior);
  document.addEventListener('keydown', e => { if (e.target.tagName === 'TEXTAREA') return; if (e.key === 'ArrowRight' || e.key === ' ') { e.preventDefault(); siguiente(); } if (e.key === 'ArrowLeft') anterior(); });

  // Deslizar en la obra para avanzar o retroceder (fuera del modo explorar).
  let toque = null;
  escena.addEventListener('touchstart', e => { if (!visor.libre && e.touches.length === 1) toque = { x: e.touches[0].clientX, y: e.touches[0].clientY, t: Date.now() }; }, { passive: true });
  escena.addEventListener('touchend', e => {
    if (!toque || visor.libre) return;
    const dx = e.changedTouches[0].clientX - toque.x, dy = e.changedTouches[0].clientY - toque.y;
    if (Math.abs(dx) > 50 && Math.abs(dx) > Math.abs(dy) * 1.5 && Date.now() - toque.t < 700) dx < 0 ? siguiente() : anterior();
    toque = null;
  });

  // Explorar a mi aire: arrastrar y pellizcar; doble toque en la obra también lo activa.
  const explorar = si => {
    visor.exploracion(si);
    document.getElementById('visor').classList.toggle('explorando', si);
    btnExplorar.textContent = si ? '↩ Volver al recorrido' : '🔍 Explorar a mi aire';
    if (!si) pintar();
  };
  btnExplorar.addEventListener('click', () => explorar(!visor.libre));
  let ultimoToque = 0;
  escena.addEventListener('click', () => { const ahora = Date.now(); if (ahora - ultimoToque < 320 && !visor.libre) explorar(true); ultimoToque = ahora; });

  const fav = document.getElementById('fav');
  fav.addEventListener('click', async () => { const r = await post('Favorita'); if (r && !d.admin) fav.classList.toggle('on', r.favorita); if (navigator.vibrate) navigator.vibrate(15); });

  pintar();
})();
