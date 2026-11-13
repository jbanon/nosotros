// Capítulo de "Disfruta la vista": tamaño de letra (misma preferencia que las Lecturas), barra que se
// esconde sobre la portada y chips de sección que siguen la lectura.
(() => {
  const cap = document.getElementById('cap'), barra = document.getElementById('cap-barra');
  if (!cap || !barra) return;

  // Tamaño de letra: comparte la clave de las Lecturas para que Raquel no tenga que ajustarlo dos veces.
  const k = 'lectura-tam', min = 1.0, max = 2.2, paso = 0.1;
  let t = 1.3; try { t = parseFloat(localStorage.getItem(k)) || t; } catch (e) {}
  const aplicar = () => { cap.style.setProperty('--lect', t.toFixed(2) + 'rem'); try { localStorage.setItem(k, t); } catch (e) {} };
  document.getElementById('tam-menos')?.addEventListener('click', () => { t = Math.max(min, t - paso); aplicar(); });
  document.getElementById('tam-mas')?.addEventListener('click', () => { t = Math.min(max, t + paso); aplicar(); });
  aplicar();

  if (!('IntersectionObserver' in window)) return;

  // Sobre la portada la barra es transparente y sin chips; al entrar en el texto se vuelve sólida.
  const portada = document.getElementById('cap-portada');
  if (portada) {
    new IntersectionObserver(([e]) => barra.classList.toggle('sobre-portada', e.isIntersecting && e.intersectionRatio > 0.25),
      { threshold: [0, 0.25, 0.5], rootMargin: '-56px 0px 0px 0px' }).observe(portada);
  }

  // Chip activo = la última sección cuyo título ya ha pasado por debajo de la barra.
  const chips = [...document.querySelectorAll('#cap-chips a[data-seccion]')];
  if (!chips.length) return;
  const objetivos = chips.map(a => document.getElementById(a.dataset.seccion)).filter(Boolean);
  let actual = null;
  const marcar = () => {
    const limite = barra.offsetHeight + 24;
    let el = null;
    for (const o of objetivos) { if (o.getBoundingClientRect().top <= limite) el = o; else break; }
    if (el === actual) return;
    actual = el;
    chips.forEach(a => {
      const on = el && a.dataset.seccion === el.id;
      a.classList.toggle('on', !!on);
      if (on) a.scrollIntoView({ block: 'nearest', inline: 'center', behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
    });
  };
  let pendiente = false;
  addEventListener('scroll', () => { if (pendiente) return; pendiente = true; requestAnimationFrame(() => { pendiente = false; marcar(); }); }, { passive: true });
  marcar();
})();
