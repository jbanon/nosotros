# Decisiones cerradas

Criterios fijados con Javier. Si una tarea nueva choca con alguno, avisar antes de cambiarlo.

## Personas y permisos
- Raquel no ve Admin ni ningún enlace para cambiar de usuario. Para cambiar de Javi a Raquel
  (o al revés) se escribe `/Login` a mano y se mete el otro PIN; Javier borra el historial.
- El usuario administrador se llama "Javi" (antes "Admin"). El rename se hace solo al arrancar.
- Raquel registra y borra sus gastos y crea recetas. Los gastos fijos (Veva, Hipoteca) solo los
  borra el admin; se crean solos cada mes y, si el admin borra uno, no se regenera ese mes.
- Los PIN nunca van al repo ni a la documentación: `appsettings.Local.json` en local,
  `/etc/love.env` en el servidor.

## Gastos
- La app empieza en noviembre de 2026 (`GastoService.InicioApp`): los meses anteriores quedan en blanco,
  sin fijos automáticos; al arrancar se borran los fijos que se hubieran creado antes. No hay recolocación
  automática de gastos: lo que haya mal de fecha lo corrige Javier a mano.
- Cada compra es un gasto independiente; el total del mes es la suma. Tocar un movimiento lo EDITA (la
  pantalla lo avisa y ofrece "+ Otra compra"); para añadir, botón "Apuntar una compra" o el cierre de
  la lista de la compra (importe + foto del ticket + detalle con lo marcado como comprado; opcional
  desmarcar al apuntar). Desde la lista también se puede ir al formulario completo con el ticket
  prerrellenado (`/Gastos/Editar?lunes=`).
- Tiempo en el súper: `Semana.CompraInicio` se fija solo al marcar el primer producto como comprado (sin
  botón de empezar; hay "Cancelar" por si se marca algo en casa). Al apuntar el ticket (desde la lista o
  desde el formulario con `lunes`) se guarda `Gasto.DuracionMin` y se vacía el inicio. Si pasaron más de
  5 h se considera un olvido y no se guarda. Gastos muestra 🕒 por compra y total/media del mes.

## Comida
- La lista de la compra es SIEMPRE para 12 raciones ("somos una gran familia"), configurable en
  Admin → Configuración (`RacionesLista`). Todas las recetas se escalaron una vez a 12 al
  arrancar (`SeedService.EscalarRecetas`, guardado con `RecetasEscaladas`); las nuevas nacen
  con 12. Si una receta tiene otras raciones, la lista la escala y la ficha lo avisa.
- Sugerencias de recetas en Semana según historial (hace tiempo que no, favoritas, nunca
  probadas), estables toda la semana, ocultas cuando ya hay 7 recetas.
- Tickets: líneas producto+importe opcionales en los gastos de tipo Compra. De ahí salen los
  productos recurrentes (mediana de intervalos) y los chips "Quizá toca reponer" en la lista.
  No hay OCR de la foto del ticket (requeriría API de visión).
- Añadir a la compra es predictivo y se clasifica solo (`ZonaAuto`): sugiere lo ya conocido (tickets,
  ingredientes, lo añadido a mano otras semanas, ordenado por veces) y pone la zona: primero la conocida
  (la última vez que se añadió a mano manda, así aprende si se corrige), si no por palabras clave
  ("manzanas" → frutería) salvo envasados ("tomate frito", "atún en lata" → otros). La misma tabla de
  palabras va al navegador (`predictivo.js`). Si se elige la zona a mano, se respeta.
- Voz (2026-11-11): no se integra con Alexa ni con Google. Amazon cerró la API de listas a terceros el
  1/7/2024 y Google no tiene API para Keep ni acciones de Assistant (cerradas en 2023). Lo viable, si se
  quiere: botón 🎤 en el navegador (Web Speech) o una skill propia de Alexa ("Alexa, dile a Nosotros…").

## Página de Raquel
- Declaración permanente bajo la tarjeta del día, editable en Admin → Configuración.
- Pie: "N días juntos" (sin "Llevamos").
- Tema Navidad desde el 25 de noviembre (coincide con el aviso "Algo se está preparando…" del adviento) hasta el 6 de enero.
- Tema otoño: tonos suaves (`--fondo-1:#f9ead4` … `--acento-amor:#a9442a`), hojas 🍂🍁 sin
  la verde. Modo oscuro vía `prefers-color-scheme` en las páginas normales.
- Lecturas: Javier pega el texto (o sube .txt) en Admin → Lecturas; lector a pantalla completa
  con A−/A+ recordado por dispositivo; aviso en la página de Raquel hasta que marca "leída".
  NO incluir en el repo textos con derechos (artículos de prensa, libros): los pega Javier.

## Interfaz
- Fechas en URL (query/route) SIEMPRE con `ToString("yyyy-MM-dd")`: el enlace a un gasto nuevo llevaba
  `10/11/2026` y el servidor (cultura invariante en query) lo leía como 11 de octubre; así se perdieron
  compras en otros meses (2026-11-10). El formulario de gasto además rechaza fechas futuras o de hace
  más de 120 días, y la fecha va plegada, en letras y con atajos Hoy/Ayer/Anteayer.
- Todo mobile-first, sin liar la interfaz: lo opcional va plegado (`<details>`) o en páginas
  secundarias enlazadas desde la cabecera. Barra inferior: ♥, Recetas, Compra, € Gastos; Raquel
  además ✨ Ideas y 🎬 Pelis; el admin en su lugar ⚙ Admin.
- Ideas: editables, con casilla "ya está automatizada" y resumen de lo hecho, visible en
  `/Ideas/Hechas`. Películas: mismo patrón (por ver / vistas).
- CSS y JS con versión por hash para que los móviles no se queden con estilos viejos.
- Contraste (repaso 2026-11-11, modo claro y oscuro): texto normal ≥ 4.5:1, texto grande e iconos ≥ 3:1.
  El acento va en tres variables: `--acento` (rellenos con texto blanco y adornos), `--acento-texto`
  (enlaces y texto; más claro en modo oscuro) y `--acento-claro` (fondos suaves); igual `--ok/--mal/--aviso`
  con su `-texto`. Nunca `color: var(--acento)` ni `var(--ok)` para texto. En las páginas con tema,
  `--acento-texto` es el `--acento-amor` del tema, y cada tema debe leerse sobre su `--fondo-3`.
  `button { color: inherit }` en la base: sin eso los botones salían negros en modo oscuro.
  Las páginas sin tema siguen el modo oscuro del móvil; si un color va fijo (`#fff`, `#fde7df`…),
  hay que mirarlo también en oscuro.

## Listas
- Listas reutilizables para marcar (viaje, camping…), compartidas entre los dos (también lo que marca el admin).
  Están en una pestaña junto a "La compra" (el icono Compra de la barra) para no añadir otro icono a la barra
  en móviles pequeños. Lo marcado se queda en su sitio (tachado) y "Desmarcar todo" la deja lista para la
  próxima vez. Viaje y Camping se crean una sola vez (`Config.ListasSembradas`); si se borran no vuelven.

## Calendario de adviento
- 24 casillas del 1 al 24 de diciembre (año en `AdvientoCasilla.Anio`; en enero sigue valiendo el de diciembre). Una casilla se abre cuando llega su día (hora de Madrid); las pasadas siguen abiertas; las futuras muestran un mensaje gracioso. Reparto: 6 criptofrases, 5 murdokus, 5 crucigramas, 5 fotos, 2 vales y la carta final del 24.
- Cada enigma (y cada foto) da una letra; las 21 letras en orden de día forman la frase final (`Config.AdvientoFrase`, "Gracias por quererme así"). Admin avisa si letras y frase no coinciden.
- Fotos: con pregunta → borrosa hasta acertar (la letra se gana al acertar); sin pregunta → se ve directa y la letra se gana al abrir. Fotos en `App_Data/adjuntos/adviento`, fuera del repo. Cualquier casilla con foto sale en el collage del 24.
- El contenido inicial vive en `Seed/adviento.json` (se carga una vez por año si no hay casillas); después se edita en Admin → Calendario de adviento (JSON del pasatiempo plegado). Las comprobaciones se hacen en el servidor (handlers JSON `Resolver`, `Pista`, `Estado`, `Canjear`); el admin puede jugar en vista previa sin que se guarde nada.
- Vales: Raquel pulsa "Canjear" y queda la fecha; el tercer vale (peli noruega) va dentro del día 24.
- Datos de la familia para los pasatiempos: Naia (14, guitarra, "en la parra"), Marcos (14, basket, sobradillo), Elsa (12, corre como un gamo), Miranda (11, la graciosa), Aroa (10, achuchable, baile), Briska (labrador negro, futura perra guía). Se conocieron chateando; cada verano una semana los dos en el norte (Irlanda del Norte, Gales, quizá Finlandia); "big project": casita en el campo con despachito y barracón para toda la familia.
- `Seed/adviento.json` solo se carga una vez (si no hay casillas del año). Para cambiar una casilla ya
  creada en producción: cambiar el JSON y añadir el arreglo en `SeedService.CorregirAdviento`, que solo
  actúa si la casilla conserva el texto original (no pisa lo editado en Admin).
- Vales (adviento y desbloqueo de viñetas): al canjear, Raquel ve el sello "Canjeado" con día y hora, y Javier
  tiene el aviso arriba en Admin durante 30 días (no hay notificaciones push). Si el admin pulsa "Canjear",
  ve el vale canjeado sin que se guarde nada (`?simularCanje=true`). Horas mostradas con `Reloj.Local`
  (hora de Madrid), no `ToLocalTime`, que depende de la zona del servidor (esta máquina está en UTC).
- Las criptofrases ajustan el tamaño de celda (`--celda`, de 30 a 16 px) para que la palabra más larga quepa
  en el ancho del móvil; si ni así cabe, la palabra se parte.
- Vídeo en una casilla (`AdvientoCasilla.VideoPath`, primero el villancico de Javier el día 1): en una casilla
  Foto sustituye a la foto, que pasa a ser su portada; la letra se gana igual que con la foto (al abrir, o al
  acertar si hay pregunta). Se abre a pantalla completa con guirnalda de luces, nieve y acebo (`js/cine.js`);
  Atrás cierra el vídeo. Se sube desde Admin por trozos de 8 MB (nginx limita a 30 MB por petición; así no hay
  que tocarlo), máx. 1 GB, y se sirve con rangos en `/Adviento/Video/{id}`. Sin transcodificar (no hay ffmpeg):
  mejor MP4 grabado en 1080p y sin HEVC; los .mov se sirven como video/mp4.

## Base de datos
- PostgreSQL desde 2026-11-13 (Javier quita SQL Server del servidor y quiere Postgres; la app venía de SQLite).
  `DateTime` como timestamp sin zona (`Npgsql.EnableLegacyTimestampBehavior`), igual que hacía SQLite; decimales
  como numeric (en SQLite eran double). Búsquedas con `ILike` para no distinguir mayúsculas.
- Si Postgres está vacío y existe `App_Data/love.db`, se importa al arrancar (mismos Id, secuencias ajustadas) y el
  fichero se renombra `love.db.migrado-…`. La copia de seguridad de Admin sigue siendo un ZIP con un love.db
  (SQLite portable): restaurar = ponerlo en App_Data con Postgres vacío.

## Agente (claude -p)
- Opción B elegida (2026-11-09): la app lanza el CLI `claude -p` en segundo plano (`AgenteService`,
  cola en memoria, tareas en la tabla `TareasAgente`, se retoman al arrancar). Modelo **Sonnet 5.5
  con esfuerzo medio** (`Agente:Modelo=sonnet`, `Agente:Esfuerzo=medium`), salida estructurada con
  `--json-schema`, `--no-session-persistence`, `--permission-mode dontAsk`, timeout 240 s. Al
  lanzar el proceso se quitan `CLAUDECODE` y `CLAUDE_CODE_ENTRYPOINT`.
- Películas: al apuntar una, la ficha se crea sola y se aplica directamente (título en España,
  original, año, director, reparto, género, duración, sinopsis sin spoilers, curiosidad). El agente
  puede usar WebSearch/WebFetch. El póster lo busca la app en Wikipedia (`pageimages` con
  `pilicense=any`, en y luego es) y lo guarda en `adjuntos/posters`. En `/Peliculas/Ficha/{id}`
  se le piden cambios en conversación.
- Recetas: el usuario pide por nombre (+ indicaciones) o sube una foto (la foto puede estar en
  inglés: el agente la lee con `Read`, traduce, pasa a métrico y escala a las raciones de la lista).
  La propuesta NO se guarda hasta que el usuario pulsa "Guardar receta"; antes puede pedir cambios
  (cada ronda manda la propuesta completa + conversación y recibe la receta entera actualizada).
  Al guardar se reutilizan los ingredientes existentes por nombre (el prompt incluye la lista).
- Coste orientativo medido: ficha 0,05–0,13 $, receta 0,06–0,15 $ por ronda; 10–25 s.
- El agente usa SIEMPRE la suscripción de Javier (sesión del CLI en local; en el servidor el token OAuth de
  `claude setup-token` en `CLAUDE_CODE_OAUTH_TOKEN`), nunca la API de pago: el proceso hijo arranca sin
  `ANTHROPIC_API_KEY` ni `ANTHROPIC_AUTH_TOKEN`. El `total_cost_usd` que se guarda es solo la estimación del CLI.
- Si no se encuentra el binario, la app sigue funcionando sin los botones ✨.

## Viñetas (ilustraciones de 72 Kilos y otros)
- Colección en `Vinetas` (imagen en `adjuntos/vinetas`, fuera del repo) con frase del autor, FRASE DE JAVI,
  intenciones (taxonomía en `VinetaService.Taxonomia`), tono, momentos, juego sugerido, conocida, fecha fija.
  Se importan de `App_Data/entrada/<carpeta>/catalogo.json` (Admin → Viñetas → Importar; idempotente por
  `Origen`) o se suben una a una y el agente las cataloga leyendo la imagen (`TipoTareaAgente.Vineta`).
- Reparto: `VinetasPorSemana` (3) días fijos por semana elegidos con semilla; nunca repite hasta agotar; las
  "conocidas" (libro del amor) no salen antes de `VinetasConocidasDesde` (2027-02-01); `FechaFija` manda.
  La asignación (`VinetaDia`) se guarda la primera vez que se carga la portada o el álbum; al entrar se
  completan los días pasados que quedaron sin abrir (`Completar`, desde la primera asignación), así nada se
  pierde si Raquel no entra unos días. Los juegos NO caducan: quedan en "Por jugar" en el álbum y se abren
  con `/Vinetas/Hoy?fecha=`. La vista previa del admin no persiste, así que puede variar.
- De cada 10, `VinetasJuegoDe10` (5) llegan como juego: Rompecabezas, Parejas, Revelar, Criptofrase, Ordena la
  frase (se respeta el sugerido si es viable). La viñeta pasa al álbum al resolver; cada juego da 1 corazón.
- Álbum `/Vinetas`: solo las desveladas; favoritas y respuesta de Raquel (Admin las ve y marca como vistas).
- Desbloqueos por corazones (Admin → Viñetas → Desbloqueos): rompecabezas grande 4×4 con cualquier viñeta del
  álbum (3 ♥), vale sorpresa canjeable (6 ♥, texto configurable) y carta (10 ♥, texto configurable).
- Una casilla del adviento puede regalar una viñeta además de la letra (`AdvientoCasilla.VinetaId`); esa
  viñeta queda fuera del reparto semanal y entra en el álbum al resolver la casilla (`MostradaEl`).
- Catálogos de las dos primeras tandas (48 viñetas) en `App_Data/entrada/*/catalogo.json` de la máquina de
  desarrollo; para producción hay que copiar esas carpetas a `/opt/love/App_Data/entrada/` e importar.

## Disfruta la vista (arte)
- Sección propia `/Arte`, una obra cada domingo desde el 15/11/2026 (`ArteService.Estreno`). La fecha es la
  fijada en la obra o el siguiente domingo libre según el orden de la cola (Admin → flechas). Las obras
  pasadas no caducan. Aviso en la portada la semana de la obra mientras no la haya visto; enlace "🎨 Arte" en
  el pie. No ocupa botón en la barra inferior.
- Visor: una sola `<img>` con `translate3d + scale` (transform-origin 0 0) y transición de 1,3 s; `will-change`
  solo durante la animación para que el navegador repinte nítido al terminar. Se carga la reducida (1600 px)
  y se cambia por la grande (4096 px máx., límite elegido por Safari en iPhone) cuando está decodificada.
  Paso 0 = obra entera con intro; paradas; último = obra entera con la frase de Javi y respuesta de Raquel.
  Deslizar o botones; doble toque o botón para explorar con arrastre y pellizco (zoom máx. ×12).
- Imágenes con ImageSharp **3.1.12** (la 4.x exige clave de licencia para compilar en Release). Se guardan en
  `adjuntos/arte` (-g y -r). El reductor de fotos de `app.js` se salta inputs con `data-sin-reducir`.
- Solo dominio público, con URL de origen y licencia en cada obra; nada del Prado directamente (Commons). No
  usar capturas de baja resolución. Descartadas: Matisse (DP en España en 2035) y fotos de esculturas con
  derechos del fotógrafo.
- Contenido inicial: `Seed/arte.json` (12 obras, 78 paradas, textos en el tono de la app con guiños a la
  familia) + imágenes en `App_Data/entrada/arte/` (fuera del repo); se importan desde Admin → Disfruta la vista.

