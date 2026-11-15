// Duelo de Cifras: consulta el estado al servidor cada segundo y maneja el tablero de cifras.js.
(() => {
  const $ = id => document.getElementById(id);
  const token = document.querySelector('input[name=__RequestVerificationToken]')?.value;
  const url = (h, q) => `${location.pathname}?handler=${h}${q || ''}`;
  let estado = null, rondaN = null, finPintado = null, ocupado = false, ultimoAviso = null;

  const post = (h, q) => fetch(url(h, q), { method: 'POST', headers: { 'RequestVerificationToken': token } })
    .then(r => r.ok ? r.json() : null).catch(() => null).then(e => { if (e) pinta(e); });
  const consulta = () => {
    if (ocupado) return; ocupado = true;
    fetch(url('Estado'), { cache: 'no-store' }).then(r => r.ok ? r.json() : null).catch(() => null)
      .then(e => { ocupado = false; if (e) pinta(e); });
  };

  // cifras.js avisa de cada número nuevo
  window.cifrasDuelo = { valor: v => post('Jugada', '&valor=' + v) };

  document.addEventListener('click', e => {
    const b = e.target.closest('[data-accion]'); if (!b) return;
    if (b.dataset.accion === 'Salir' && estado?.estado !== 'esperando' && !confirm('¿Salir del duelo?')) return;
    b.disabled = true; post(b.dataset.accion).finally(() => b.disabled = false);
  });

  const vista = v => document.querySelectorAll('.dl-vista').forEach(x => x.hidden = x.dataset.v !== v);
  const txtD = (d, seg) => d == null ? 'pensando…' : d === 0 ? `¡clavado! · ${seg} s` : `a ${d}`;

  function pinta(e) {
    estado = e;
    document.querySelectorAll('[data-rival]').forEach(x => x.textContent = e.rival?.nombre || '');
    if (e.aviso && e.aviso !== ultimoAviso) avisa(e.aviso); ultimoAviso = e.aviso || null;
    const h = e.historico; $('dl-hist').textContent = h && (h.yo + h.rival + h.empates) ? `Total: ${h.yo}–${h.rival}` : '';
    $('dl-salir-p').hidden = !(e.estado === 'ronda' || e.estado === 'fin');
    if (e.estado === 'nada' || e.estado === 'reto' || e.estado === 'esperando') { vista(e.estado); rondaN = null; return; }
    vista('juego');
    const r = e.ronda;
    $('dl-s-yo').textContent = e.sesion.yo; $('dl-s-rival').textContent = e.sesion.rival;
    $('dl-yo-txt').textContent = txtD(r.yo.d, r.yo.seg);
    $('dl-rival-txt').textContent = (e.rival.conectado ? '' : '📴 ') + txtD(r.rival.d, r.rival.seg);
    document.querySelector('.dl-jug.rival').classList.toggle('clavado', r.rival.d === 0);
    document.querySelector('.dl-jug.yo').classList.toggle('clavado', r.yo.d === 0);
    if (r.n !== rondaN) {
      rondaN = r.n; finPintado = null; $('dl-final').hidden = true;
      const inicio = performance.now() + r.msParaInicio;
      window.cifras.empezar({ nums: r.nums, objetivo: r.objetivo, inicio });
      if (r.msParaInicio > 300) cuentaAtras(inicio);
    }
    if (e.estado === 'fin' && finPintado !== r.n) { finPintado = r.n; final(e); }
  }

  function cuentaAtras(inicio) {
    const el = $('dl-cuenta'); el.hidden = false;
    const t = setInterval(() => {
      const q = inicio - performance.now();
      const txt = q > 0 ? String(Math.ceil(q / 1000)) : '¡Ya!';
      if (el.textContent !== txt) { el.textContent = txt; el.classList.remove('late'); void el.offsetWidth; el.classList.add('late'); }
      if (q < -600) { clearInterval(t); el.hidden = true; }
    }, 80);
  }

  function final(e) {
    const r = e.ronda;
    window.cifras.parar();
    const tit = r.ganador === 'yo' ? '🏆 ¡Ganas tú!' : r.ganador === 'rival' ? `Gana ${e.rival.nombre}` : '¡Empate!';
    const linea = (quien, x) => `<div class="dl-fila ${x.d === 0 ? 'ok' : ''}"><b>${quien}</b><span>${x.valor ?? '—'}</span><span>${x.d == null ? 'sin número' : x.d === 0 ? 'exacto' : 'a ' + x.d}</span><span>${x.seg != null ? x.seg + ' s' : ''}</span></div>`;
    const el = $('dl-final');
    el.innerHTML = `<div class="cf-res ${r.ganador === 'yo' ? 'ok' : r.ganador === 'rival' ? 'tiempo' : ''}"><h2>${tit}</h2>
      <p class="suave peq">Objetivo <b>${r.objetivo}</b></p>
      <div class="dl-tabla">${linea('Tú', r.yo)}${linea(e.rival.nombre, r.rival)}</div>
      <div class="cf-sol" id="dl-sol"><span class="suave peq">Buscando la mejor solución…</span></div>
      <button type="button" class="btn primario ancho" data-accion="Nueva">Nueva partida</button></div>`;
    el.hidden = false;
    if (r.ganador === 'yo') window.cifras.confeti();
    setTimeout(() => {
      const s = window.cifras.resolver(r.nums, r.objetivo);
      const OPS = { '+': '+', '-': '−', '*': '×', '/': '÷' };
      const lista = s.pasos.map(p => `<li>${p.a} ${OPS[p.op]} ${p.b} = <b>${p.r}</b></li>`).join('');
      $('dl-sol').innerHTML = s.d === 0 ? `<p class="suave peq">Se podía clavar así:</p><ol>${lista}</ol>` : `<p class="suave peq">Imposible clavarlo: lo más cerca era <b>${s.v}</b></p><ol>${lista}</ol>`;
    }, 300);
    setTimeout(() => el.scrollIntoView({ behavior: 'smooth', block: 'center' }), 200);
  }

  let tAviso;
  function avisa(msg) {
    let a = document.querySelector('.cf-aviso');
    if (!a) { a = document.createElement('div'); a.className = 'cf-aviso'; document.body.appendChild(a); }
    a.textContent = msg; a.classList.add('ver'); clearTimeout(tAviso); tAviso = setTimeout(() => a.classList.remove('ver'), 2500);
  }

  // Más rápido mientras se juega
  const bucle = () => { consulta(); setTimeout(bucle, estado && (estado.estado === 'ronda') ? 900 : 2000); };
  document.addEventListener('visibilitychange', () => { if (!document.hidden) consulta(); });
  bucle();
})();
