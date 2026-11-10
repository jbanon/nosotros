// "Disfruta la vista": motor de zoom sobre una sola imagen con transform CSS (translate + scale).
// Lo usan el visor de Raquel (/Arte/Ver) y la vista previa del editor de paradas en Admin.
(function () {
  const reducirMovimiento = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /**
   * escenario: contenedor con overflow hidden; img: la imagen (position absolute, top/left 0, transform-origin 0 0).
   * opciones: { ancho, alto, margenInferior: () => px tapados abajo (tarjeta), marcador: elemento opcional, duracion }
   */
  function Visor(escenario, img, opciones) {
    const o = Object.assign({ duracion: reducirMovimiento ? 350 : 1300, margenInferior: () => 0, margen: 12 }, opciones);
    let actual = { x: .5, y: .5, zoom: 1 }, base = 1, t = { x: 0, y: 0, s: 1 }, animando = false;

    function medir() {
      const cw = escenario.clientWidth, ch = escenario.clientHeight - o.margenInferior();
      base = Math.min((cw - o.margen * 2) / o.ancho, (ch - o.margen * 2) / o.alto);
      img.style.width = o.ancho * base + 'px'; img.style.height = o.alto * base + 'px';
      return { cw, ch };
    }

    function calcular(p) {
      const { cw, ch } = medir();
      const w = o.ancho * base * p.zoom, h = o.alto * base * p.zoom;
      let tx = cw / 2 - p.x * w, ty = ch / 2 - p.y * h;
      // Sin mostrar fondo vacío cuando la imagen ampliada es mayor que la vista; centrada si es menor.
      tx = w <= cw ? (cw - w) / 2 : Math.min(0, Math.max(cw - w, tx));
      ty = h <= ch ? (ch - h) / 2 : Math.min(0, Math.max(ch - h, ty));
      return { x: tx, y: ty, s: p.zoom, w, h };
    }

    function aplicar(nt, animar) {
      const dur = animar ? o.duracion : 0;
      img.style.transition = dur ? `transform ${dur}ms cubic-bezier(.45,.05,.25,1)` : 'none';
      // will-change solo durante la animación: al terminar el navegador vuelve a pintar la imagen nítida a su nueva escala.
      img.style.willChange = dur ? 'transform' : 'auto';
      img.style.transform = `translate3d(${nt.x}px, ${nt.y}px, 0) scale(${nt.s})`;
      t = nt;
      if (o.marcador) o.marcador.classList.remove('visible');
      animando = !!dur;
      clearTimeout(aplicar._fin);
      aplicar._fin = setTimeout(() => { animando = false; img.style.willChange = 'auto'; colocarMarcador(); }, dur + 30);
    }

    function colocarMarcador() {
      if (!o.marcador) return;
      if (actual.zoom <= 1.01 || actual.sinMarcador) { o.marcador.classList.remove('visible'); return; }
      o.marcador.style.left = (t.x + actual.x * o.ancho * base * t.s) + 'px';
      o.marcador.style.top = (t.y + actual.y * o.alto * base * t.s) + 'px';
      o.marcador.classList.add('visible');
    }

    function irA(p, animar = true) { actual = Object.assign({ x: .5, y: .5, zoom: 1 }, p); aplicar(calcular(actual), animar); }

    // ---------- Exploración libre: arrastrar y pellizcar ----------
    let libre = false, punteros = new Map(), inicio = null;
    function limitar(nt) {
      const { cw, ch } = medir();
      nt.s = Math.max(1, Math.min(12, nt.s));
      const w = o.ancho * base * nt.s, h = o.alto * base * nt.s;
      nt.x = w <= cw ? (cw - w) / 2 : Math.min(0, Math.max(cw - w, nt.x));
      nt.y = h <= ch ? (ch - h) / 2 : Math.min(0, Math.max(ch - h, nt.y));
      return nt;
    }
    function puntoMedio() { const ps = [...punteros.values()]; return { x: ps.reduce((a, p) => a + p.x, 0) / ps.length, y: ps.reduce((a, p) => a + p.y, 0) / ps.length }; }
    function distancia() { const [a, b] = [...punteros.values()]; return Math.hypot(a.x - b.x, a.y - b.y); }
    escenario.addEventListener('pointerdown', e => {
      if (!libre) return;
      escenario.setPointerCapture(e.pointerId);
      punteros.set(e.pointerId, { x: e.clientX, y: e.clientY });
      inicio = { t: { ...t }, m: puntoMedio(), d: punteros.size > 1 ? distancia() : 0 };
    });
    escenario.addEventListener('pointermove', e => {
      if (!libre || !punteros.has(e.pointerId)) return;
      punteros.set(e.pointerId, { x: e.clientX, y: e.clientY });
      const r = escenario.getBoundingClientRect(), m = puntoMedio();
      let s = inicio.t.s;
      if (punteros.size > 1 && inicio.d) s = inicio.t.s * distancia() / inicio.d;
      const k = s / inicio.t.s, mx = inicio.m.x - r.left, my = inicio.m.y - r.top;
      const nt = limitar({ x: mx - (mx - inicio.t.x) * k + (m.x - inicio.m.x), y: my - (my - inicio.t.y) * k + (m.y - inicio.m.y), s });
      img.style.transition = 'none'; img.style.transform = `translate3d(${nt.x}px, ${nt.y}px, 0) scale(${nt.s})`; t = nt;
    });
    const soltar = e => { punteros.delete(e.pointerId); inicio = punteros.size ? { t: { ...t }, m: puntoMedio(), d: punteros.size > 1 ? distancia() : 0 } : null; };
    ['pointerup', 'pointercancel'].forEach(ev => escenario.addEventListener(ev, soltar));
    escenario.addEventListener('wheel', e => {
      if (!libre) return; e.preventDefault();
      const r = escenario.getBoundingClientRect(), mx = e.clientX - r.left, my = e.clientY - r.top, k = Math.exp(-e.deltaY / 400);
      const nt = limitar({ x: mx - (mx - t.x) * k, y: my - (my - t.y) * k, s: t.s * k });
      img.style.transition = 'none'; img.style.transform = `translate3d(${nt.x}px, ${nt.y}px, 0) scale(${nt.s})`; t = nt;
    }, { passive: false });

    window.addEventListener('resize', () => { if (!libre) aplicar(calcular(actual), false); });

    return {
      irA, recolocar: () => aplicar(calcular(actual), false),
      exploracion(si) { libre = si; escenario.classList.toggle('libre', si); if (o.marcador) o.marcador.classList.remove('visible'); if (!si) irA(actual); },
      get libre() { return libre; }, get animando() { return animando; },
      /** Convierte un clic en el escenario en coordenadas de la obra (fracción 0–1). */
      aFraccion(clientX, clientY) {
        const r = escenario.getBoundingClientRect();
        return { x: (clientX - r.left - t.x) / (o.ancho * base * t.s), y: (clientY - r.top - t.y) / (o.alto * base * t.s) };
      },
    };
  }
  window.VisorArte = Visor;
})();
