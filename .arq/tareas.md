# Tareas

## Para la próxima sesión (Javier, 2026-11-11: "déjalo anotado para la próxima")
- **Postgres en producción:** en `/srv/nosotros`, `git pull`, `sudo ./deploy/instalar-postgres.sh` y
  `./deploy/publicar.sh`. Al arrancar importa `/opt/love/App_Data/love.db` (queda `love.db.migrado-…` como copia);
  comprobar en el log "Importado de SQLite: N filas". SQL Server se puede quitar: la app nunca lo usó.
- **Vallotton en producción:** copiar `App_Data/entrada/arte/vallotton/` (8 jpg) al servidor en
  `/opt/love/App_Data/entrada/arte/vallotton/`, desplegar, Admin → Arte → Importar, revisar el capítulo y pulsar
  "Está listo" (sale el próximo domingo libre). Javier revisa en el móvil textos, paradas y diseño; puede escribir
  su frase en cada obra.
- **Botón 🎤 en la lista de la compra** (Web Speech en el navegador): dictar "fideos, manzanas y leche" y que
  se añadan separados y clasificados con `ZonaAuto`. Alexa y Google ya no dejan leer sus listas (ver
  decisiones → Comida); una skill propia de Alexa queda como opción si la piden.

## Hilos abiertos
- **Arte en producción:** copiar `App_Data/entrada/arte/` (12 jpg, ~52 MB) al servidor en
  `/opt/love/App_Data/entrada/arte/` y pulsar Importar en Admin → Disfruta la vista antes del domingo 15/11.
  Javier revisa los textos y escribe/ajusta su frase final en cada obra. Visor sin probar en navegador real.
- **Viñetas: Javier debe escribir su frase en cada viñeta (Admin → Viñetas → filtro "Sin mi frase") y subir
  las carpetas `App_Data/entrada/libro-amor` y `las-vidas-que-dibujamos` al servidor (`/opt/love/App_Data/entrada/`)
  para importarlas allí.** Pendiente ver los juegos en el móvil (capturas): rompecabezas, parejas, rascar,
  criptofrase y ordena la frase se han probado solo contra el servidor. Javier también puede escribir la carta
  de los 10 ♥ y el texto del vale en Admin → Viñetas → Desbloqueos.

- **Adviento día 1: Javier sube el vídeo del villancico (Admin → Calendario de adviento → casilla 1 → Vídeo) y
  cambia en el texto "Hoy, para empezar, una foto."** Probar en su móvil con Vista previa (pantalla completa,
  sonido, botón Atrás); el reproductor solo se ha visto en Chromium sin interfaz con un WebM de prueba.
- **Adviento: Javier debe subir las fotos (días 1, 7, 11, 16, 20) y revisar textos desde Admin → Calendario de adviento antes del 1 de diciembre.** Opcional: pregunta/respuesta por foto (si no, la foto se ve directa y la letra se gana al abrir). Pendiente ver capturas en el móvil (diseño validado a ciegas) y confirmar que el teclado se abre bien en criptofrase y crucigrama (input oculto `.teclado-oculto`).
- **Lista de la compra: "elementos abajo que no se ven"** (captura en modo oscuro,
  2026-11-08). Se aumentó el relleno inferior y se versionó el CSS; Javier debe confirmar tras
  desplegar. Si sigue, pedir captura.
- **Listas: Javier completa la de Camping** (Compra → pestaña Listas).
- Javier aún no ha pegado el texto de "¡Oigan!" (Javier Marías) en Lecturas: lo hace él.

## Ideas apuntadas (sin empezar, 2026-11-10)
Javier las dejó apuntadas para hacer más adelante; también pueden entrar como casillas del adviento.
- **Cifras (de "Cifras y letras")**: le encanta. Seis números y un objetivo, llegar con + − × ÷.
- **Arkanoid sencillo**: pala, pelota y ladrillos, pensado para el móvil.
- **Recordatorio de francés**: Raquel habla bien francés; algo gracioso para repasar (frase o expresión del
  día, falsos amigos, mini quiz).

## Resueltas
| Fecha | Tarea | Commit |
|---|---|---|
| 2026-11-05 | App inicial: página de amor con 366 mensajes, menú semanal + lista por zonas, gastos, Admin, PIN | 1921252 |
| 2026-11-08 | Lista siempre para 12 raciones (configurable) y plural de unidades | d1d21a2 |
| 2026-11-08 | Gasto inicial de noviembre: 60 € filetes, magro y croquetas (seed idempotente) | 2927f75 |
| 2026-11-08 | Páginas Ideas ("Pago basket") y Pelis ("Full Monty"); tema otoño más suave | 36d37c2 |
| 2026-11-08 | Declaración permanente en la página principal, editable en Admin | 07950f1 |
| 2026-11-08 | Pie "N días juntos" sin "Llevamos" | b56a798 |
| 2026-11-08 | Raquel registra gastos (fijos solo los borra el admin); histórico por meses | 188eb10 |
| 2026-11-08 | Admin → "Javi"; `/Login` permite cambiar de usuario sin enlace visible | 1e9fa35 |
| 2026-11-08 | Ideas editables con resumen y página Hechas; CSS/JS por hash; relleno inferior y modo oscuro de la lista | 9028016 |
| 2026-11-08 | Recetas a 12 raciones de verdad; sugerencias por historial; líneas de ticket, recurrentes y chips de reponer | 2052b19 |
| 2026-11-08 | Lecturas para Raquel: lector con A−/A+, gestión en Admin, aviso en la página de amor | b7d31e2 |
| 2026-11-09 | Prompt para el agente de los presupuestadores sobre el patrón `claude -p` a demanda (sin código) | — |
| 2026-11-09 | Creado `.arq/` y `CLAUDE.md` | — |
| 2026-11-10 | "Disfruta la vista": obra semanal con recorrido de zoom por detalles, álbum, favoritas, respuesta, editor de paradas en Admin y 12 obras iniciales | fd996c5 |
| 2026-11-10 | Repaso de bugs en los juegos de viñetas: Parejas con pocas conocidas, tablero que cambiaba entre sesiones, rascado que se borraba al hacer scroll, estados guardados incoherentes, medianoche, asignación simultánea | 5868d80 |
| 2026-11-10 | Viñetas: los días sin abrir se completan solos y los juegos no caducan (lista "Por jugar" en el álbum, aviso en la portada) | 4b83c83 |
| 2026-11-10 | Viñetas fase 3: desbloqueos por corazones (rompecabezas grande, vale, carta), viñeta de regalo en casillas del adviento, avisos en Admin | 3e5d828 |
| 2026-11-10 | Viñetas fase 2: los cinco juegos (rompecabezas, parejas, revelar con pregunta o rascando, criptofrase, ordena la frase) en /Vinetas/Hoy, con pistas, progreso y corazón al resolver | 43200b7 |
| 2026-11-10 | Viñetas fase 1: colección en Admin (importación de catálogos, análisis por el agente), viñeta del día en la portada con frase de Javi, álbum con favoritas y respuestas | ca7f47a |
| 2026-11-10 | Sin recolocación automática (Javier lo corrigió a mano); la app empieza en noviembre 2026 y los meses anteriores quedan en blanco (sin fijos) | 7fdac7f |
| 2026-11-10 | CAUSA de las compras "borradas": el enlace de gasto nuevo pasaba la fecha como 10/11/2026 y se leía como 11 de octubre. Arreglado (ISO en URL), fecha plegada en letras con atajos y validación de fechas | 2726482 |
| 2026-11-10 | El seed deja de sembrar la compra de 60 € (se apunta a mano) y la edición de un gasto ofrece "Guardar como compra nueva" | 62bd10c |
| 2026-11-10 | Cronómetro de la compra: arranca solo al marcar el primer producto comprado, se ve en la lista y se guarda en el gasto (minutos) al apuntar el ticket; total y media del mes en Gastos | d53f778 |
| 2026-11-10 | Gastos: aviso claro al editar un gasto existente + botón "Apuntar una compra"; la lista de la compra se cierra con importe y foto del ticket y crea el gasto con el detalle de lo comprado | cef162b |
| 2026-11-09 | Agente con `claude -p` (Sonnet, esfuerzo medio): mini fichas de películas automáticas con póster, recetas por nombre o desde una foto (traduce y pasa a métrico), conversación de cambios y guardado | cf399aa |
| 2026-11-09 | Tema Navidad adelantado al 25 de noviembre | 49af518 |
| 2026-11-09 | Calendario de adviento 2026: 24 casillas (fotos, criptofrases, murdokus, crucigramas, vales, carta final), letras → frase final, Admin para fotos/contenidos/seguimiento, aviso en la página de Raquel | e05af01 |
| 2026-11-09 | Agente `claude -p` en producción: CLI de dev (`/home/dev/.local/bin/claude`) usado por www-data, token de suscripción en `/etc/love.env`; probado con Paddington 2 (ficha y póster en 13 s) | — |
| 2026-11-11 | Página de amor: fondo de otoño mucho más claro (sin el marrón de abajo) y sin las franjas de 16px a los lados en los temas | 4af0c51 |
| 2026-11-11 | Icono del álbum de viñetas: 🖼️ → 💌 (página de amor y álbum; en Admin, 📔) | 172127f |
| 2026-11-11 | Repaso de contraste de toda la web (claro y oscuro, todos los temas): botones negros en modo oscuro (Ordena la frase), login, gris suave, acento de texto separado, paletas de verano/San Valentín/cumple/aniversario/invierno/primavera | 6a19fb9 |
| 2026-11-11 | Criptofrases: las celdas se encogen para que la palabra más larga quepa en móviles pequeños (320 px); adviento día 3: la sospechosa es Aroa, no Miranda (también en producción vía `CorregirAdviento`) | 057b914 |
| 2026-11-11 | Vale del adviento: sello "Canjeado" con día y hora al canjearlo, aviso para Javier arriba en Admin (también el vale de los corazones), y el admin ve cómo queda al pulsar sin guardar nada | 003d726 |
| 2026-11-11 | Crucigrama y criptofrase: tocar una casilla ya no desplaza la página (campo oculto del teclado arriba y `focus({ preventScroll })`) | 71e6713 |
| 2026-11-11 | Adviento: vídeo en una casilla (día 1, villancico) a pantalla completa con guirnalda, nieve y acebo; subida por trozos desde Admin y servido con rangos | cbc74c1 |
| 2026-11-11 | Listas reutilizables (pestaña junto a La compra): Viaje y Camping de inicio, marcar sin recargar, añadir varias líneas, desmarcar todo, compartir, renombrar y borrar | 2c492f8 |
| 2026-11-11 | Compra: al añadir, sugerencias de lo ya comprado ("fi" → Fideos) y zona automática ("manzanas" → Frutería), también en "Quizá toca reponer" | 131d78f |
| 2026-11-13 | PostgreSQL en lugar de SQLite: importación automática de love.db al primer arranque, copia de seguridad como love.db, `deploy/instalar-postgres.sh`; probado en local con Postgres 16 (todas las páginas, escrituras, reinicio, reimportación) | fb15984 |
| 2026-11-13 | Arte por capítulos: Vallotton (texto, 3 paseos: Perseo, Coquetería, Mujer con cántaro; 5 obras con reseña), diseño del agente Fable; imágenes de WikiArt (Commons solo tenía baja resolución) | e395df9 |
| 2026-11-14 | Visor de arte: el círculo del detalle parpadea dos veces y desaparece 5 s (en bucle) para no tapar la obra | (este) |
