// Briska por el calendario de adviento: una asoma en la esquina con un bocadillo que cambia y, de vez en cuando,
// otra cruza la pantalla volando o asoma por un lado y se esconde. Tocarla: ladra y suelta una frase.
// Las imágenes vienen del servidor (wwwroot/img/briska/*.png) en el JSON #briska-imgs; sin JS de terceros ni sonidos grabados.
(() => {
  const raiz = document.getElementById('briska');
  if (!raiz) return;
  let imgs = [];
  try { imgs = JSON.parse(document.getElementById('briska-imgs')?.textContent || '[]'); } catch { }
  if (!Array.isArray(imgs) || !imgs.length) return;
  const quieto = window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches;
  const rincon = document.getElementById('briska-rincon'), rImg = rincon.querySelector('img');
  const boc = document.getElementById('briska-bocadillo'), suelta = document.getElementById('briska-suelta');
  const azar = (a, b) => a + Math.random() * (b - a), elige = a => a[Math.floor(Math.random() * a.length)];

  const FRASES = [
    '¿Eso que huele es turrón?',
    'Guau. Traducción: te quiere un montón.',
    'Yo también quiero una casilla.',
    'Perra guía en prácticas: te llevo hasta el sofá.',
    'Si te atascas, pide una pista. Yo pediría una galleta.',
    'Mitad labrador, mitad pájaro. Cien por cien hambre.',
    'He olido la frase final. Huele a bonito.',
    'Elsa corre como un gamo, pero yo tengo más orejas.',
    'Hoy toca mimos. Lo dice el calendario (y yo).',
    'No miro. Bueno, un poco.',
    'Vigilo esta casilla desde las siete. Sin desayunar.',
    'Si fallas, no pasa nada: yo me como los errores.',
    'Hay 24 sorpresas y ninguna es un hueso. Lo he comprobado.',
    'Aroa me achucha. Yo me dejo.',
    'Ladro bajito para no despertar a las clones.',
    'En la casita del campo habrá un hueco para mí, ¿verdad?',
    'Las letras de los días forman una frase. Yo solo sé ladrarla.',
    'Nieve en Finlandia, sofá en casa. Gano yo.',
    'Si ves un globo dorado, no es mío. Lo juro.',
    '¿Marcos sigue en el sobradillo? Es que ahí no llego.',
    'Naia toca la guitarra y yo aúllo la segunda voz.',
    'Miranda dice que la graciosa es ella. Ja.',
    'Resuelve rápido, que a las nueve tengo paseo.',
    'Te quiere. Lo sé porque me lo cuenta todo cuando salimos.',
    'Hoy el calendario pone: querer mucho. Fácil.',
    'No era yo la que roncaba. Era el pájaro.',
    'Si pudiera, te traería la pelota. Y la respuesta.',
    'Tengo una pista: te la cambio por una caricia.',
  ];
  let ultima = -1;
  const frase = () => { let i; do i = Math.floor(Math.random() * FRASES.length); while (i === ultima && FRASES.length > 1); ultima = i; return FRASES[i]; };

  // Ladrido corto sintetizado con Web Audio (sin ficheros)
  let AC = null;
  function guau(tono = 1, retraso = 0) {
    try {
      AC = AC || new (window.AudioContext || window.webkitAudioContext)();
      if (AC.state === 'suspended') AC.resume();
      const t = AC.currentTime + retraso, frec = 250 * tono, o = AC.createOscillator(), f = AC.createBiquadFilter(), g = AC.createGain();
      o.type = 'sawtooth'; o.frequency.setValueAtTime(frec * 1.7, t); o.frequency.exponentialRampToValueAtTime(frec, t + 0.05); o.frequency.exponentialRampToValueAtTime(frec * 0.75, t + 0.17);
      f.type = 'lowpass'; f.frequency.value = Math.min(2400, frec * 5); f.Q.value = 6;
      g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(0.35, t + 0.012); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.18);
      o.connect(f); f.connect(g); g.connect(AC.destination); o.start(t); o.stop(t + 0.2);
    } catch { }
  }

  // Bocadillo
  let callar = 0;
  function habla(texto, dur = 5500) {
    boc.textContent = texto; boc.classList.add('visible');
    clearTimeout(callar); callar = setTimeout(() => boc.classList.remove('visible'), dur);
  }
  const escondida = () => document.hidden || raiz.classList.contains('escondida');

  // Rincón: asoma a los pocos segundos y dice algo cada cierto tiempo
  rImg.src = elige(imgs); raiz.hidden = false;
  setTimeout(() => { rincon.classList.add('asoma'); setTimeout(() => { if (!escondida()) habla(frase()); }, 900); }, quieto ? 300 : 1800);
  (function charla() { setTimeout(() => { if (!escondida()) habla(frase()); charla(); }, azar(16000, 30000)); })();
  rincon.addEventListener('click', () => {
    guau(1.2); guau(1.38, 0.14);
    habla(frase(), 5000);
    rincon.classList.remove('bota'); void rincon.offsetWidth; rincon.classList.add('bota');
    if (navigator.vibrate) navigator.vibrate(15);
  });

  // Se esconde mientras se escribe (teclado del móvil, criptofrase, crucigrama…) para no tapar nada
  const campo = el => el && el.matches && el.matches('input, textarea');
  document.addEventListener('focusin', e => { if (campo(e.target)) raiz.classList.add('escondida'); });
  document.addEventListener('focusout', e => { if (campo(e.target)) setTimeout(() => { if (!campo(document.activeElement)) raiz.classList.remove('escondida'); }, 150); });

  // Briska suelta: de vez en cuando cruza volando o asoma por un lado y se esconde (no con prefers-reduced-motion)
  if (quieto) return;
  function recoge() { suelta.onanimationend = null; suelta.hidden = true; suelta.className = 'briska-suelta'; }
  function programa() { setTimeout(suelto, azar(18000, 45000)); }
  function suelto() {
    if (escondida()) { programa(); return; }
    suelta.src = elige(imgs);
    const cruza = Math.random() < 0.55;
    suelta.className = 'briska-suelta ' + (cruza ? 'cruza' : 'asoma') + (Math.random() < 0.5 ? ' der' : '');
    suelta.style.top = azar(12, 58) + 'vh';
    suelta.style.animationDuration = (cruza ? azar(4.5, 7) : azar(3.5, 4.5)) + 's';
    suelta.onanimationend = () => { recoge(); programa(); };
    suelta.hidden = false;
  }
  suelta.addEventListener('click', () => { guau(1.3); guau(1.5, 0.12); habla(frase()); recoge(); programa(); });
  programa();
})();
