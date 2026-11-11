// Vídeo de una casilla del adviento: se abre a pantalla completa con guirnalda de luces, nieve y acebo.
(() => {
  const portada = document.getElementById('foto'), cine = document.getElementById('cine');
  if (!portada || !cine) return;
  const video = document.getElementById('cine-video'), fin = document.getElementById('cine-fin');
  const nieve = cine.querySelector('.cine-nieve'), guirnalda = cine.querySelector('.cine-guirnalda');
  const r = (a, b) => a + Math.random() * (b - a);
  const colores = ['#ff5a5f', '#ffd166', '#5ee08a', '#5aa9ff', '#ff8fc7', '#fff1a8'];
  let decorado = false, anchoGuirnalda = 0;

  // Guirnalda: un cable que cuelga en ondas con tres bombillas por onda.
  function pintarGuirnalda() {
    const w = guirnalda.clientWidth; if (!w || w === anchoGuirnalda) return;
    anchoGuirnalda = w;
    const ondas = Math.max(3, Math.round(w / 78)), p = w / ondas, caida = 16, y0 = 4;
    let d = `M0 ${y0}`, bombillas = '';
    for (let k = 0; k < ondas; k++) {
      d += ` Q${k * p + p / 2} ${y0 + 2 * caida} ${(k + 1) * p} ${y0}`;
      for (const t of [.25, .5, .75]) {
        const x = k * p + t * p, y = y0 + 2 * (1 - t) * t * 2 * caida, n = k * 3 + t * 4 - 1;
        const c = colores[n % colores.length];
        bombillas += `<i style="left:${x}px;top:${y}px;color:${c};background:${c};animation-delay:${r(0, 1.8).toFixed(2)}s"></i>`;
      }
    }
    guirnalda.innerHTML = `<svg width="${w}" height="${y0 + 2 * caida}"><path d="${d}" fill="none" stroke="#2c3b2f" stroke-width="2" /></svg>` + bombillas;
  }

  function decorar() {
    pintarGuirnalda();
    if (decorado) return;
    decorado = true;
    for (let i = 0; i < 45; i++) {
      const s = document.createElement('span'); s.className = 'p-nieve';
      const d = r(9, 18);
      s.style.left = r(0, 100) + 'vw';
      s.style.animationDuration = d + 's, ' + r(2, 5) + 's';
      s.style.animationDelay = -r(0, d) + 's, ' + -r(0, 3) + 's';
      s.style.scale = r(.5, 1.2); s.style.opacity = r(.4, .95);
      nieve.appendChild(s);
    }
  }

  function corazones() {
    for (let i = 0; i < 18; i++) {
      const h = document.createElement('span'); h.className = 'p-corazon'; h.textContent = '♥';
      h.style.left = r(5, 95) + 'vw'; h.style.color = i % 3 ? '#ff5a7a' : '#ffd166';
      h.style.animationDuration = r(4, 7) + 's, ' + r(2, 4) + 's';
      h.style.animationDelay = r(0, 1.5) + 's, 0s';
      h.style.animationIterationCount = '1, infinite';
      h.style.scale = r(.8, 1.6);
      nieve.appendChild(h); setTimeout(() => h.remove(), 9000);
    }
  }

  // Proporción real del vídeo para ajustar el marco; si es apaisado y estamos a pantalla completa, gira la pantalla.
  function orientar() {
    if (!video.videoWidth) return;
    cine.style.setProperty('--ar', video.videoWidth + ' / ' + video.videoHeight);
    if (document.fullscreenElement && video.videoWidth > video.videoHeight)
      screen.orientation?.lock?.('landscape').catch(() => {});
  }
  video.addEventListener('loadedmetadata', orientar);

  function pantallaCompleta() {
    const pedir = cine.requestFullscreen || cine.webkitRequestFullscreen;
    if (!pedir) return;
    try { Promise.resolve(pedir.call(cine, { navigationUI: 'hide' })).then(orientar, () => {}); } catch { }
  }

  function abrir() {
    if (portada.classList.contains('velada')) {
      const resp = document.getElementById('respuesta');
      resp?.scrollIntoView({ behavior: 'smooth', block: 'center' }); resp?.focus({ preventScroll: true });
      return;
    }
    if (!video.getAttribute('src')) { video.src = video.dataset.src; video.preload = 'auto'; }
    cine.hidden = false; fin.hidden = true;
    document.documentElement.classList.add('con-cine');
    decorar();
    history.pushState({ cine: true }, '');
    pantallaCompleta();
    video.play().catch(() => {});   // el toque del usuario permite que suene
  }

  function cerrar() {
    video.pause();
    cine.hidden = true;
    document.documentElement.classList.remove('con-cine');
    if (document.fullscreenElement) document.exitFullscreen?.().catch(() => {});
    try { screen.orientation?.unlock?.(); } catch { }
  }
  // Cerrar = volver atrás en el historial, así el botón Atrás del móvil también cierra el vídeo.
  const pedirCierre = () => { if (history.state?.cine) history.back(); else cerrar(); };

  portada.addEventListener('click', abrir);
  cine.addEventListener('click', e => { if (e.target.closest('[data-cerrar]')) pedirCierre(); });
  window.addEventListener('popstate', () => { if (!cine.hidden) cerrar(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !cine.hidden && !document.fullscreenElement) pedirCierre(); });
  window.addEventListener('resize', () => { if (!cine.hidden) pintarGuirnalda(); });
  video.addEventListener('play', () => { fin.hidden = true; });
  video.addEventListener('ended', () => { fin.hidden = false; corazones(); if (navigator.vibrate) navigator.vibrate([30, 60, 30]); });
  document.getElementById('cine-otra').addEventListener('click', () => { video.currentTime = 0; video.play().catch(() => {}); });
})();
