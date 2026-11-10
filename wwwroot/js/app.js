(() => {
  // --- Partículas de la página de amor ---
  const amor = document.querySelector('.amor, .adviento');
  if (amor) {
    const cont = document.getElementById('particulas');
    const r = (a, b) => a + Math.random() * (b - a);
    const colores = ['#ff6b6b', '#ffd93d', '#6bcB77', '#4d96ff', '#ff8fab', '#c77dff'];
    const cfg = {
      nieve:       { n: 40, clase: 'p-nieve', dur: [9, 18] },
      petalos:     { n: 24, clase: 'p-petalo', dur: [10, 18] },
      hojas:       { n: 16, clase: 'p-hoja', dur: [10, 20], txt: ['🍂', '🍁', '🍂'] },
      corazones:   { n: 18, clase: 'p-corazon', dur: [8, 16], txt: ['♥', '♥', '❤'] },
      luciernagas: { n: 26, clase: 'p-luciernaga', dur: [3, 7], fijo: true },
      confeti:     { n: 60, clase: 'p-confeti', dur: [5, 10], color: true },
      murcielagos: { n: 6,  clase: 'p-murcielago', dur: [6, 12], txt: ['🦇'], fijo: true },
      brillos:     { n: 22, clase: 'p-brillo', dur: [2, 5], txt: ['✦', '✧'], fijo: true },
    };
    (amor.dataset.particulas || '').split(' ').forEach(k => {
      const c = cfg[k];
      if (k === 'luces') {
        const l = document.createElement('div'); l.className = 'luces';
        for (let i = 0; i < 14; i++) { const b = document.createElement('i'); b.style.background = b.style.color = colores[i % colores.length]; b.style.animationDelay = r(0, 2) + 's'; l.appendChild(b); }
        amor.querySelector('.cielo').appendChild(l);
        return;
      }
      if (!c) return;
      for (let i = 0; i < c.n; i++) {
        const s = document.createElement('span');
        s.className = c.clase;
        s.style.left = r(0, 100) + 'vw';
        if (c.fijo) s.style.top = r(5, 90) + 'vh';
        const d = r(c.dur[0], c.dur[1]);
        s.style.animationDuration = d + 's, ' + r(2, 5) + 's';
        s.style.animationDelay = -r(0, d) + 's, ' + -r(0, 3) + 's';
        const esc = r(.6, 1.3); s.style.scale = esc; s.style.opacity = r(.5, 1);
        if (c.txt) s.textContent = c.txt[Math.floor(r(0, c.txt.length))];
        if (c.color) s.style.background = colores[i % colores.length];
        cont.appendChild(s);
      }
    });
    // corazón: lluvia de corazones al tocar
    const corazon = document.getElementById('corazon');
    corazon?.addEventListener('click', e => {
      const rect = corazon.getBoundingClientRect();
      for (let i = 0; i < 14; i++) {
        const h = document.createElement('span'); h.className = 'p-estela'; h.textContent = '♥';
        h.style.left = rect.left + rect.width / 2 + 'px'; h.style.top = rect.top + rect.height / 2 + 'px';
        h.style.setProperty('--dx', r(-140, 140) + 'px'); h.style.setProperty('--dy', r(-220, -60) + 'px');
        h.style.fontSize = r(14, 30) + 'px';
        document.body.appendChild(h); setTimeout(() => h.remove(), 1300);
      }
      if (navigator.vibrate) navigator.vibrate([10, 30, 10]);
    });
  }

  // --- Confirmaciones ---
  document.querySelectorAll('[data-confirm]').forEach(el => el.addEventListener('submit', e => { if (!confirm(el.dataset.confirm)) e.preventDefault(); }));
  document.querySelectorAll('a[data-confirm], button[data-confirm]').forEach(el => el.addEventListener('click', e => { if (!confirm(el.dataset.confirm)) e.preventDefault(); }));

  // --- Compartir ---
  document.querySelectorAll('[data-share]').forEach(el => el.addEventListener('click', async () => {
    const texto = document.getElementById(el.dataset.share)?.textContent || '';
    if (navigator.share) { try { await navigator.share({ text: texto }); } catch { } }
    else { await navigator.clipboard.writeText(texto); alert('Lista copiada'); }
  }));
})();

// --- Reducir fotos en el móvil antes de subirlas (máx. 1600 px, JPEG) ---
document.querySelectorAll('input[type=file][accept*="image"]:not([data-sin-reducir])').forEach(inp => inp.addEventListener('change', async () => {
  const f = inp.files?.[0];
  if (!f || !f.type.startsWith('image/') || f.type === 'image/gif') return;
  try {
    const bmp = await createImageBitmap(f, { imageOrientation: 'from-image' });
    const max = 1600, esc = Math.min(1, max / Math.max(bmp.width, bmp.height));
    if (esc === 1 && f.size < 600 * 1024) return;
    const c = document.createElement('canvas');
    c.width = Math.round(bmp.width * esc); c.height = Math.round(bmp.height * esc);
    c.getContext('2d').drawImage(bmp, 0, 0, c.width, c.height);
    const blob = await new Promise(r => c.toBlob(r, 'image/jpeg', 0.82));
    if (!blob) return;
    const dt = new DataTransfer();
    dt.items.add(new File([blob], f.name.replace(/\.[^.]+$/, '') + '.jpg', { type: 'image/jpeg' }));
    inp.files = dt.files;
  } catch (e) { console.warn('No se pudo reducir la imagen', e); }
}));
