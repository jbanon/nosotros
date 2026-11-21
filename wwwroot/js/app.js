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
      if (k === 'luces') { guirnalda(amor.querySelector('.cielo')); return; }
      if (k === 'santa') { papaNoel(); return; }
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
    // Guirnalda navideña: cable que cuelga, bombillas que parpadean, bastones de caramelo y el cartel "Merry Christmas"
    function guirnalda(cielo) {
      if (!cielo) return;
      const NS = 'http://www.w3.org/2000/svg';
      const caja = document.createElement('div'); caja.className = 'luces';
      cielo.appendChild(caja);
      const pinta = () => {
        const w = caja.clientWidth || innerWidth, n = Math.max(5, Math.round(w / 52)), paso = w / n, sag = 30, top = 6;
        let d = `M0 ${top}`, html = '';
        for (let i = 0; i < n; i++) {
          const x0 = i * paso, xm = x0 + paso / 2, ym = top + sag / 2;
          d += ` Q${xm} ${sag + top} ${x0 + paso} ${top}`;
          const c = colores[i % colores.length];
          html += `<g class="bombilla" style="color:${c};animation-delay:${(i * 0.37 % 1.8).toFixed(2)}s" transform="translate(${xm} ${ym}) rotate(${i % 2 ? 8 : -8})">` +
            `<rect x="-3.5" y="0" width="7" height="6" rx="1.5" fill="#2c3a2e"/><ellipse cx="0" cy="13" rx="6" ry="8.5" fill="${c}"/><ellipse cx="-2" cy="10" rx="1.6" ry="3" fill="#fff" opacity=".6"/></g>`;
          if (i % 2 === 1) html += `<g class="baston" transform="translate(${x0} ${top - 1}) rotate(${i % 4 === 1 ? -10 : 10})">` +
            `<path d="M0 30 V8 a6 6 0 0 1 12 0" fill="none" stroke="#fff" stroke-width="5" stroke-linecap="round"/>` +
            `<path d="M0 30 V8 a6 6 0 0 1 12 0" fill="none" stroke="#e23a3a" stroke-width="5" stroke-linecap="round" stroke-dasharray="4 4"/></g>`;
        }
        caja.innerHTML = `<svg xmlns="${NS}" width="${w}" height="64" viewBox="0 0 ${w} 64"><path d="${d}" fill="none" stroke="#7d9a6f" stroke-width="2"/>${html}</svg>` +
          '<div class="feliz"><span>✦</span> Merry Christmas <span>✦</span></div>';
      };
      pinta();
      let rt = 0; addEventListener('resize', () => { clearTimeout(rt); rt = setTimeout(pinta, 200); });
    }

    // Papá Noel: se asoma de vez en cuando por abajo o por un lado, saluda y, si lo tocas, suelta regalos
    function papaNoel() {
      const FRASES = ['¡Jo, jo, jo!', '¿Te has portado bien este año? 😏', 'Briska ya está en la lista de las buenas 🐾', 'Merry Christmas! 🎁',
        'Javi me ha encargado algo para ti… 🤫', 'Shhh… no le digas a nadie que me has visto', '¡Este año me paso por tu casa el primero!', 'He traído caramelos 🍬'];
      let svg = null, fuera = true, visto = 0;
      fetch('/img/papanoel.svg').then(x => x.text()).then(t => { svg = t; setTimeout(sale, r(3000, 6000)); }).catch(() => { });
      function sale() {
        if (document.hidden || !fuera) { setTimeout(sale, 4000); return; }
        fuera = false;
        const lados = ['abajo-izq', 'abajo-der', 'izq', 'der'], lado = lados[Math.floor(r(0, lados.length))];
        const pn = document.createElement('button'); pn.type = 'button'; pn.className = 'papanoel ' + lado; pn.setAttribute('aria-label', 'Papá Noel');
        pn.innerHTML = svg; document.body.appendChild(pn);
        requestAnimationFrame(() => requestAnimationFrame(() => pn.classList.add('dentro')));
        let t = setTimeout(vete, 4200);
        pn.addEventListener('click', () => {
          clearTimeout(t); jingle(); regalos(pn);
          const b = document.createElement('div'); b.className = 'papanoel-dice'; b.textContent = FRASES[visto++ % FRASES.length];
          pn.appendChild(b); pn.classList.add('rie');
          t = setTimeout(vete, 2600);
        }, { once: true });
        function vete() { pn.classList.remove('dentro'); setTimeout(() => { pn.remove(); fuera = true; }, 700); setTimeout(sale, r(22000, 40000)); }
      }
      function regalos(pn) {
        const q = pn.getBoundingClientRect(), cx = q.left + q.width / 2, cy = q.top + q.height / 3;
        for (let i = 0; i < 12; i++) {
          const h = document.createElement('span'); h.className = 'p-estela'; h.textContent = ['🎁', '🍬', '⭐', '❄️'][i % 4];
          h.style.left = cx + 'px'; h.style.top = cy + 'px';
          h.style.setProperty('--dx', r(-140, 140) + 'px'); h.style.setProperty('--dy', r(-240, -80) + 'px'); h.style.fontSize = r(16, 28) + 'px';
          document.body.appendChild(h); setTimeout(() => h.remove(), 1300);
        }
      }
      // Cascabeles sintetizados (sin ficheros)
      function jingle() {
        try {
          const AC = new (window.AudioContext || window.webkitAudioContext)(), t0 = AC.currentTime;
          [[0, 1319], [0.12, 1319], [0.24, 1319], [0.48, 1568], [0.6, 1047]].forEach(([dt, f]) => {
            [1, 2.76].forEach((m, j) => {
              const o = AC.createOscillator(), g = AC.createGain(); o.type = 'sine'; o.frequency.value = f * m;
              g.gain.setValueAtTime(0.0001, t0 + dt); g.gain.exponentialRampToValueAtTime(j ? 0.04 : 0.12, t0 + dt + 0.01); g.gain.exponentialRampToValueAtTime(0.0001, t0 + dt + 0.35);
              o.connect(g); g.connect(AC.destination); o.start(t0 + dt); o.stop(t0 + dt + 0.4);
            });
          });
          setTimeout(() => AC.close(), 1500);
        } catch { }
      }
    }

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
