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
- Inventar un juego nuevo para la casilla del día 13 del adviento (el Arkanoid ya está abierto en Juegos desde el 21-11).
- **Arte por temas: trabajarlo la semana del 16 de noviembre de 2026** (Javier). El plan y el orden están en
  decisiones → "Arte por temas". Falta implementar:
  - Entidad Tema, con el capítulo de tipo "tour general" o "autor".
  - Calendario: capítulos los domingos, alternando temas; obras sueltas los miércoles.
  - Álbum agrupado por tema.
  - Contenido, empezando por los tours generales. Hecho: Japón (falta pasar las imágenes al servidor e importarlas).
  - Fuentes de imágenes: Javier mira mucho WikiArt; también valen Met Open Access y Commons (comprobar la licencia).
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
- **Comecocos (día 18 y Juegos): Javier lo ve en el móvil.** El estilo nuevo (neón sobre negro) no se ha visto en
  ningún navegador, solo simulado con node; la dificultad con 2 perseguidores se afina en `AJ` de `comecocos.js`
  (`velPers`, `velPersSube`, `salidas`, `modo`). La casilla 18 ya sembrada conserva en su texto "…ni la piña":
  se cambia en Admin → Calendario de adviento.
- **Adviento día 6 (globos): Javier lo prueba en el móvil con Vista previa y ajusta la dificultad** (`AJ` en
  `wwwroot/js/globos.js`: fallos permitidos, velocidades, vida de los corazones, volumen y tempo de la música).
  Falta oír el sonido en el móvil (música de ladridos, explosiones, botón de silenciar). El JS solo se ha
  simulado con node y un DOM de pega; el servidor (vista y comprobación de la frase) sí se ha probado con curl.
- **Adviento: Javier debe subir las fotos (días 1, 7, 11, 16, 20) y revisar textos desde Admin → Calendario de adviento antes del 1 de diciembre.** Opcional: pregunta/respuesta por foto (si no, la foto se ve directa y la letra se gana al abrir). Pendiente ver capturas en el móvil (diseño validado a ciegas) y confirmar que el teclado se abre bien en criptofrase y crucigrama (input oculto `.teclado-oculto`).
- **Lista de la compra: "elementos abajo que no se ven"** (captura en modo oscuro,
  2026-11-08). Se aumentó el relleno inferior y se versionó el CSS; Javier debe confirmar tras
  desplegar. Si sigue, pedir captura.
- **Listas: Javier completa la de Camping** (Compra → pestaña Listas).
- Javier aún no ha pegado el texto de "¡Oigan!" (Javier Marías) en Lecturas: lo hace él.

## Ideas apuntadas (sin empezar, 2026-11-10)
Javier las dejó apuntadas para hacer más adelante; también pueden entrar como casillas del adviento.
- **Arkanoid sencillo**: pala, pelota y ladrillos, pensado para el móvil.
- **La cocina de Julia** (Javier, 2026-11-15). Sección con unos 40 clásicos franceses propuestos y buscador por
  nombre o ingrediente principal.
  - **Generar:** al elegir una, el agente (reutilizando `TipoTareaAgente.Receta`) la escribe AL ESTILO de Julia
    Child (cálido, preciso, que anima), nunca copiando el libro.
  - **Ilustración:** 1-2 dibujos de línea SVG del gesto clave, o grabados de recetarios en dominio público (sin
    decidir). Las ilustraciones del libro tienen derechos.
  - **Guardar:** pasa a Recetas, marcada "de Julia", para no generarla otra vez.
  - **Realizar:** mete los ingredientes en la lista de la compra de la semana (zona automática) y abre un modo
    cocina con fichas paso a paso con humor, temporizadores y pantalla encendida.

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
| 2026-11-14 | Visor de arte: el círculo del detalle parpadea dos veces y desaparece 5 s (en bucle) para no tapar la obra | 3263034 |
| 2026-11-14 | Juegos: Cifras (45 s, solución al final) y crucigrama del día generado; enlace en la portada. Pendiente: Javier lo prueba en el móvil (JS sin probar en navegador aquí) | a136aa2 |
| 2026-11-15 | Cifras: al acabar los 45 s cuenta como no conseguido pero se sigue jugando; botón "Ver la solución" | 060a6b1 |
| 2026-11-15 | Duelo de Cifras a dos: reto, aviso en portada, rondas simultáneas, marcador de sesión e histórico. Probado con curl; falta probar en los dos móviles | 1d63de7 |
| 2026-11-15 | Crucigrama compartido: la misma rejilla para los dos, letras y cursor del otro en violeta, guardado en servidor. Probado con curl; falta probar en los dos móviles | 81e5311 |
| 2026-11-16 | Gastos: la compra que pasa del objetivo se descuenta de la hipoteca del mes siguiente | c2f9eb1 |
| 2026-11-16 | Arte: tour general de Japón (texto ~420 palabras, 3 paseos: La gran ola, Dioses del viento y del trueno, Takiyasha; 4 reseñas: Kōrin, Jakuchū, Hiroshige, Kiyochika). Imágenes en `App_Data/entrada/arte/japon/` (Met Open Access y Commons, dominio público) | 5db4c10 |
| 2026-11-16 | Arte: el índice del capítulo se tragaba el texto si un `## ` no llevaba línea en blanco detrás (ahora el título ocupa solo su línea) | 1a0f705 |
| 2026-11-17 | Adviento: juego de los globos en el día 7 (corazones dorados con las palabras de la frase, dificultad adaptable, intentos con pistas) | 526d5ac |
| 2026-11-17 | Adviento: los globos pasan al día 6 (en lugar de su criptofrase); el día 7 vuelve a ser foto | dade167 |
| 2026-11-17 | Globos: con muchos en pantalla no se explotaban (globos dibujados una vez y copiados, tope de 12, toque al más cercano y con margen por el retraso) | b93bf6f |
| 2026-11-17 | Globos: más difícil y largo, BriskaBird (3 apariciones con entradilla, ladridos y +3 de margen si la caza) y palabras que se esfuman al acabar | 7134b5d |
| 2026-11-17 | Globos: música de ladridos y explosiones (Web Audio) con botón de silenciar; BriskaBird se presenta una vez y sale 10 veces sin pausa (+1 globo); hay que cazar todas las palabras (las perdidas vuelven a salir) | 1386c4c |
| 2026-11-17 | Globos: +15 % de dificultad, las palabras cazadas ya no se apuntan abajo durante la partida y al final se esfuman antes (1,25 s) y un 10 % más rápido | 877047b |
| 2026-11-17 | Adviento: murdokus con pistas indirectas (solución única comprobada por fuerza bruta) y criptofrases con más gracia; `CorregirAdviento` aplica el seed nuevo a esas 10 casillas si conservan el texto original | cf93c95 |
| 2026-11-17 | Criptofrases 19 y 23 retocadas por Javier ("tú, la lluvia y yo"; "galletas de chocolate a escondidas") | 2d5e58a |
| 2026-11-17 | Arkanoid: BriskaBird caga (la pala manchada va lenta 10 s), sin bloques se sigue hasta cazar todas las palabras y pala un 15 % más pequeña | b44c15b |
| 2026-11-17 | Juegos diarios: globos, bloques y comecocos también en la zona Juegos, una partida al día por juego (admin sin límite), frase del día y desbloqueo con su casilla del adviento | 1ee3837 |
| 2026-11-17 | Juegos con frase: comodín desde el 3º fallo (las palabras una a una y en orden, una vez por partida) en lugar de regalar palabras | a6071ac |
| 2026-11-17 | Juegos con frase: pista "caza palabras" (salen 0,6-0,4 s, con trampas) y cada partida tiene al azar comodín o caza | 8c4df97 |
| 2026-11-17 | Adviento: mini Arkanoid en el día 13 (pala, pelota, 42 bloques, 3 vidas; BriskaBird revolotea y suelta una palabra al darle; final con palabras que se esfuman y escritura de la frase como globos). Kit común `adv-juego.js` (sonido + escribir la frase). Briska asomando por el calendario y las casillas (`img/briska/*.png` listados por el servidor, bocadillos, ladridos) | 9850e62 |
| 2026-11-17 | Adviento: mini comecocos del súper en el día 18 (carrito por los pasillos, ticket/oferta/piña persiguiendo, cafés, control por deslizar; Briskanguro dando saltitos con música boing-boing suelta las palabras; final como Arkanoid). `S.boing` en el kit común | 51ea44b |
| 2026-11-17 | Comecocos: estética de laberinto arcade (fondo negro, paredes de neón azul, bolas grandes con la taza de café), solo 2 perseguidores (ticket y oferta, algo más rápidos) y Briskanguro un 23 % más pequeña | badb4db |
| 2026-11-17 | Comecocos: el carrito va siempre un 10% más rápido que los perseguidores | 82dcc29 |
| 2026-11-17 | Français: píldoras de francés divertidas (B2-C1) que escribe el agente los lunes, miércoles y viernes; Raquel responde, ♥ +1 al entregar y el agente corrige con cariño; Javi ve sus copias y puede encargar una a mano | 8d009b5 |
| 2026-11-17 | Juegos con frase: palabras en orden durante la partida (desordenadas al escribir) y botón de ayuda visible y bloqueado hasta el 3.er intento | 685b655 |
| 2026-11-17 | Criptofrase: cada casilla se rellena a mano, sin autocompletar las del mismo número | 5e4e233 |
| 2026-11-17 | Adviento día 24: el vale es por una serie (la navideña noruega), no por una película | 5bd282f |
| 2026-11-18 | Juego Línea del tiempo del arte: 5 obras a ordenar en el tiempo + autor, una partida por semana, presentado por BriskaBird pintora | bcbf2d1 |
| 2026-11-18 | Compra: importar la lista de Alexa (pegar o compartir hacia la app), con revisión previa de zona y cantidad | 0bf2f6a |
| 2026-11-18 | Juegos con frase: se quita el comodín de enseñar las palabras; solo queda la pista de cazarlas | cb06f05 |
| 2026-11-18 | Teaser del adviento: vídeo 1 s con «Something's cooking…», cada 3 días hasta el 1 de diciembre | 2721d76 |
| 2026-11-18 | Teaser del adviento: dura 2 s en vez de 1 | 4c321bc |
| 2026-11-18 | Teaser del adviento: cada 2 días en vez de 3 | 6ea41bd |
| 2026-11-18 | Arreglo: la pista de cazar palabras no se veía (una línea suelta del CSS del comodín anulaba sus estilos) | 2dc3d04 |
| 2026-11-18 | Línea del tiempo: álbum del museo con las obras jugadas (año, autor, texto breve de Wikipedia) | c7e33c5 |
| 2026-11-18 | Tal día como hoy: tres efemérides graciosas al día (esquema en portada, artículos al entrar), guardadas por mes y día | c7e33c5 |
| 2026-11-20 | Gastos: el mes siguiente muestra la previsión de fijos con la hipoteca ya descontada por la compra de más | 202f970 |
| 2026-11-21 | Tema Navidad desde el 21 de noviembre (antes el 25) | 12c0acf |
| 2026-11-21 | Navidad: Papá Noel que se asoma, guirnalda con bastones de caramelo y «Merry Christmas»; Bloques (Arkanoid) abierto en Juegos con tarjeta de estreno en la portada | 7171903 |
| 2026-11-21 | Juegos: Javi puede editar las frases del día (/Juegos/Frases); la de Bloques de hoy, la suya | e9c05eb |
| 2026-11-21 | Juegos: tras jugar la frase de Javi, partidas extra con frases sorpresa | 299773f |
| 2026-11-21 | Juegos: al acabar, las palabras se esfuman una cada 3 s (antes 0,91 s) en globos, comecocos y bloques | 3e27a93 |
| 2026-11-21 | Efemérides: cada día, una cuarta de 1980 con la etiqueta «🎂 El año que naciste»; las ya guardadas sin ella se rehacen una vez al tocarles | 1d7086d |
| 2026-11-21 | Miranda: usuario propio con su frase del día (agente), Cifras fácil sin reloj, crucigrama infantil y frases de juegos para su edad | b98f9c5 |
| 2026-11-22 | Aroa (10 años, guerreras k-pop) con su versión como Miranda (perfiles en `Ninas.cs`, página `/Hola`) y juego de las tablas de multiplicar para todos | 35fc699 |
| 2026-11-22 | Elsa (12, deportista) y Marcos (13, que estudiar renta) como Miranda; todos los niños con acceso ilimitado a todos los juegos | dc196fe |
| 2026-11-22 | Naia (14/09/2012) con su versión como los demás niños; perfil genérico de adolescente | 8d2dbb1 |
