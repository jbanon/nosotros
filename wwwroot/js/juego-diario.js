// Zona Juegos · partida diaria de un juego del adviento. Hace de window.adv (lo que en el calendario da adviento.js):
// la frase se comprueba aquí, y el servidor solo apunta que se ha empezado y cómo ha acabado (una partida al día).
(() => {
  const datos = JSON.parse(document.getElementById('datos')?.textContent || '{}');
  const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
  const enviar = async (h, body) => {
    const r = await fetch(`${location.pathname}?handler=${h}`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify(body || {}) });
    if (!r.ok) throw new Error(r.status);
    return r.json();
  };
  const norm = s => (s || '').toUpperCase().normalize('NFD').replace(/Ñ/g, 'Ñ').replace(/[̀-ͯ]/g, '');
  const letras = s => norm(s).replace(/[^A-ZÑ]/g, '');
  const msg = document.getElementById('msg');
  const aviso = (t, mal) => { if (!msg) return; msg.textContent = t; msg.className = 'adv-msg ' + (mal ? 'mal' : 'bien'); if (t) setTimeout(() => { if (msg.textContent === t) msg.textContent = ''; }, 4000); };
  const celebrar = () => {
    const colores = ['#e8b84a', '#ff6b6b', '#6bcb77', '#4d96ff', '#ff8fab', '#fff'];
    for (let i = 0; i < 70; i++) {
      const s = document.createElement('span'); s.className = 'adv-confeti';
      s.style.left = Math.random() * 100 + 'vw'; s.style.background = colores[i % colores.length];
      s.style.animationDuration = 2.5 + Math.random() * 2.5 + 's'; s.style.animationDelay = Math.random() * .8 + 's';
      s.style.width = 6 + Math.random() * 6 + 'px'; s.style.height = 10 + Math.random() * 8 + 'px';
      document.body.appendChild(s); setTimeout(() => s.remove(), 6000);
    }
    if (navigator.vibrate) navigator.vibrate([20, 40, 20, 40, 60]);
  };
  // Tras la partida, la tarjeta deja de ofrecer jugar (salvo al admin)
  const cierraTarjeta = html => {
    if (datos.admin) return;
    const ini = document.getElementById('gl-inicio');
    if (ini) ini.innerHTML = html;
  };

  window.adv = {
    datos, norm, aviso, celebrar,
    estado: () => null, guardarEstado: () => { },
    post: async (h, body) => {
      if (h !== 'Resolver') return enviar(h, body);
      if (letras(body.respuesta) !== letras(datos.vista.frase)) return { ok: false, mensaje: 'Todavía no…' };
      let palabras = 0;
      try { palabras = (JSON.parse(body.estado || '{}').palabras || []).length; } catch { }
      enviar('Terminar', { gano: true, palabras }).catch(() => { });
      return { ok: true, solucion: datos.vista.frase };
    },
    ganado: () => { datos.resuelta = true; celebrar(); aviso('¡Frase acertada! Mañana, otra.'); },
    fin: r => {
      enviar('Terminar', r).catch(() => { });
      cierraTarjeta('<p class="msg centrado">La partida de hoy ya está jugada 🐾</p><p class="suave2 centrado">Mañana, otra partida y otra frase.</p><a class="btn adv-btn ancho" href="/Juegos">Volver a Juegos</a>');
    },
  };

  // Empezar gasta la partida del día. El juego arranca en el mismo toque (el sonido lo necesita); el aviso al
  // servidor va a la vez y, si dice que ya se jugó (otra pestaña), se recarga.
  let empezada = false;
  document.addEventListener('click', e => {
    const b = e.target.closest && e.target.closest('#gl-empezar');
    if (!b) return;
    if (empezada && !datos.admin) { e.stopPropagation(); e.preventDefault(); window.adv.fin({ gano: false, palabras: 0 }); return; }
    empezada = true;
    enviar('Empezar').then(r => { if (!r.ok) { alert(r.mensaje); location.reload(); } }).catch(() => { });
  }, true);
})();
