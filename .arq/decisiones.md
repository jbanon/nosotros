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
- Fijos: Veva 300, Hipoteca 900, objetivo de compra 350 (Config). Si la compra de un mes pasa del objetivo,
  el exceso se descuenta de la hipoteca del mes siguiente (540 de compra → 900 − 190 = 710). Nunca baja de 0.
  `GastoService.AjustarHipoteca` lo recalcula cada vez que se abre Gastos o el Histórico (un ticket tardío
  también cuenta); por eso, si se edita a mano el importe del fijo de hipoteca, se sobrescribe.

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
- 24 casillas del 1 al 24 de diciembre (año en `AdvientoCasilla.Anio`; en enero sigue valiendo el de diciembre). Una casilla se abre cuando llega su día (hora de Madrid); las pasadas siguen abiertas; las futuras muestran un mensaje gracioso. Reparto: 5 criptofrases, 5 murdokus, 3 crucigramas, 5 fotos, 1 juego de globos (día 6), 1 mini Arkanoid (día 13), 1 mini comecocos (día 18), 2 vales y la carta final del 24.
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
- Juego de los globos (`TipoCasilla.Globos`, día 6 en lugar de su criptofrase; `js/globos.js`, canvas
  a pantalla completa). Se explotan tocándolos y la dificultad sube con cada palabra (intervalo, velocidad,
  tamaño, zigzag; ~7 globos entre palabra y palabra). Si se escapan 12 globos normales, se pierde y vuelve a empezar.
  - **Hay que cazar todas las palabras de la frase** (Javier, 2026-11-17; las que tenga `datos.frase`, no un número
    fijo): la que se escapa vuelve a la cola y sale otra vez, durando un 30 % más cada vez. La partida solo acaba
    al cazar la última (~75-90 s) o al perder.
  - **Palabras:** cada ~7 globos sale un corazón dorado entre un racimo de globos; dura poco y lleva una
    palabra de `datos.frase` ("Eres el amor de mi vida, no puedo vivir sin ti"). Salen desordenadas: primero la
    mitad fácil y luego la otra.
  - **+15 % de dificultad** (Javier, 2026-11-17, tras probarlo): `AJ.dificultad = 1.15` multiplica la velocidad y
    divide el intervalo entre globos y la vida del corazón en todos los niveles. Tamaño, zigzag y los 12 fallos
    de margen no cambian.
  - **Las palabras cazadas no se apuntan abajo durante la partida** (Javier, 2026-11-17): solo se ve la palabra
    un instante al explotar el corazón y el contador 💛; la lista sale al final, en la fase de escribir.
  - **Dificultad adaptable:** cada palabra cazada acorta un 7 % la vida de los corazones y cada una perdida la
    alarga un 20 %. La dificultad sube con las palabras cazadas, no con las que han salido.
  - **Escribir la frase:** en el 2º intento las bien colocadas salen en verde y los huecos con sus letras; en el
    3º, además, la inicial. Desde el 3º fallo, **comodín** (una vez por partida): enseña las palabras una a una y
    en orden, a pantalla completa (0,75 s cada una). Solo tras usarlo se regala una palabra por fallo. Vale para
    todos los juegos con frase (`adv-juego.js`).
  - **Ayuda al azar por partida** (Javier: "a cada juego uno de los dos aleatorio"): o el comodín de arriba o la
    **caza de palabras**. En la caza salen palabras un instante (de 0,6 s a 0,4 s) en sitios al azar, mezcladas con
    trampas (croqueta, manta, sofá…). Tocar una buena la fija en verde en su hueco; tocar una trampa quita 1 s y
    Briska ladra. Se puede repetir, pero cada ronda dura menos: 15, 10, 7 y 5 s. Las dos ayudas salen desde el
    3º fallo. Solo después de usarlas (comodín, o 2 rondas de caza) se regala una palabra por fallo. Puede volver a jugar para cazar más.
  - Sin tildes, mayúsculas ni comas; la comprobación final la hace el servidor, que da la letra "I".
  - Todos los ajustes, en la constante `AJ` de `globos.js`.
  - **BriskaBird** (`wwwroot/img/briska/briskabird.png`, recortada de la imagen de Javier) sale **10 veces**
    (Javier, 2026-11-17), repartidas según las palabras cazadas (con 11: al llevar 0, 1, 2, 3, 4, 6, 7, 8, 9 y 10;
    mínimo 2,5 s entre visitas). Se presenta **una sola vez, al empezar la partida** (3,2 s, un toque la salta);
    después sale sin aviso y sin parar los globos. Hace uno de tres números: cruza volando de lado a lado (3
    pasadas rápidas), aparece 0,5 s en sitios distintos moviéndose (5 veces) o asoma por los bordes (4 veces).
    Cazarla da **1 globo** más de margen, que sale en dorado en el marcador.
  - **Sonido**, todo sintetizado con Web Audio (sin ficheros ni CDN); arranca con el toque de "Empezar". Música de
    fondo: el estribillo de «Jingle Bells» ladrado (notas cortas ladridos, largas aullidos, bajo de ladridos
    graves), en bucle y acelerando con las palabras cazadas; se para al ganar, perder, salir o esconder la página.
    Explosión en cada globo (con campanitas en el corazón). Botón 🔊/🔇 junto a la × (se recuerda en
    `localStorage['gl-mudo']`).
  - Al acabar, las palabras cazadas se ven 1,25 s y luego se esfuma una cada 0,91 s (antes 2,5 s y 1 s; Javier,
    2026-11-17: la mitad de espera y un 10 % más rápido); ya no vuelven a salir
    (`estado.ocultas`). Si vuelve a jugar para cazar más, se enseñan de nuevo y se esfuman otra vez.
  - Rendimiento en el móvil: cada globo se dibuja una vez en un canvas aparte y luego solo se copia; como mucho
    12 globos a la vez. El toque elige el globo más cercano y cuenta también dónde estaba hace 150 ms.
  - Lo común con el Arkanoid vive en `js/adv-juego.js` (`window.advJuego`): el kit de sonido (`sonido()`: ladridos,
    aullidos, explosiones, tics, botón 🔊/🔇 con `localStorage['gl-mudo']`) y la fase de escribir la frase (`frase()`:
    chips que se esfuman, intentos con pistas, `Resolver`). La música de «Jingle Bells» sigue siendo solo de globos.
- Mini Arkanoid (`TipoCasilla.Arkanoid`, día 13 en lugar de su crucigrama "Junto al fuego", misma letra U; `js/arkanoid.js`,
  canvas a pantalla completa como los globos; Javier, 2026-11-17). Pala que se arrastra con el dedo (soltar con la pelota
  pegada la lanza), pelota que acelera un poco con cada bloque, 42 bloques de colores (6 × 7), 3 vidas; sin vidas, vuelta
  a empezar (también se pierden las palabras cazadas, como en globos).
  - **BriskaBird** revolotea en pasadas de lado a lado (2 por visita) por debajo de los bloques al principio y entre
    ellos según van cayendo; hasta 14 visitas para 11 palabras (que se escape alguna es normal; la palabra que se escapa
    vuelve a la cola). Una visita sale cuando se han tirado bloques suficientes o han pasado 8 s desde la anterior, con
    1,8 s mínimo entre visitas. Si la pelota le da: ladridos, rebote redondo y la **palabra en grande** con brillo.
    Las palabras salen desordenadas (primero la mitad fácil), como en globos.
  - Frase (`datos.frase`): "Quien fuera tu ombligo para estar a la sombra de tus tetas". El juego acaba con los bloques
    fuera **y** todas las palabras cazadas: sin bloques se sigue jugando, con Briska volviendo enseguida (0,7 s) y
    soltando 2 cacas por visita; perder las 3 pelotas sigue siendo perder. Después las palabras cazadas se enseñan y se
    esfuman **al mismo ritmo que en globos** (1,25 s de espera, una cada 0,91 s) y toca escribir la frase con el mismo
    sistema de intentos y pistas. La comprobación final la hace el servidor (`ComprobarArkanoid`), sin tildes ni comas.
  - **Cacas** (Javier, 2026-11-17): con bloques, 3 o 4 visitas al azar sueltan una 💩 que cae. Si cae en la pala, esta
    se mancha de marrón durante 10 s (parpadea los 2 últimos, con cuenta atrás) y deja de seguir al dedo al momento:
    va detrás a 0,75 anchos de pantalla por segundo. La pala mide un 15 % menos (0,255 del ancho).
  - Todos los ajustes en la constante `AJ` de `arkanoid.js` (vidas, rejilla, pala, velocidades, visitas de Briska).
- Mini comecocos (`TipoCasilla.Comecocos`, día 18 en lugar de su crucigrama "Cómo empezó", misma letra R; `js/comecocos.js`,
  canvas a pantalla completa como los otros; Javier, 2026-11-17). Tema "hacer la compra": el jugador es un **carrito** rojo con
  un corazoncito que recorre los pasillos de un súper (laberinto propio de 11 × 15, túnel lateral en la fila central)
  recogiendo la compra (71 puntitos + 4 cafés en las esquinas que dan 4,5 s de velocidad). Le persiguen **solo dos**
  (Javier, 2026-11-17; la piña se quita): **el ticket kilométrico** (va a por ti) y **la oferta 3x2** (se te adelanta 3
  casillas); salen del almacén central a los 0,6 y 3,5 s, alternan 9 s de persecución y 2 s de dispersión a su esquina y van
  más lentos por el túnel. Para compensar que son dos van más rápidos (3,7 casillas/s, hasta 4,4 al
  acabar la compra; el carrito, siempre un 10 % más) y dudan menos. 3 vidas; sin vidas, vuelta a empezar (también se pierden las palabras).
  - **Estética de laberinto arcade clásico** (Javier, 2026-11-17, con captura de referencia): fondo negro, paredes de neón
    azul con doble contorno y esquinas redondeadas, puntitos amarillentos y bolas grandes en las esquinas, personajes de
    colores vivos. Se toma el estilo del género, **no** el laberinto ni los sprites del juego comercial, y **la temática
    sigue siendo hacer la compra**: carrito, ticket, oferta, cafés (la bola grande lleva la taza dentro) y Briskanguro.
  - **Control por deslizar**: el dedo pide una dirección y el carrito gira en el siguiente cruce donde se pueda (se puede
    seguir arrastrando para encadenar giros); la media vuelta es inmediata y un deslizamiento justo pasado un cruce aún gira
    (vuelve al cruce). Flechas del teclado en ordenador. Nada se mueve hasta el primer deslizamiento.
  - **Briskanguro** (`img/briska/briskanguro.png`, 1,35 casillas de ancho: un 23 % menos que al principio, con el alcance
    subido a 0,75 casillas para que no cueste más pillarla) aparece hasta 16 veces (11 palabras) en una casilla lejana y va dando
    **saltitos** de casilla en casilla (bote con estiramiento en el aire, aplastamiento al caer y sombra que encoge; el 40 % de
    los saltos se aleja del carrito, el resto al azar). Mientras está en pantalla suena la **música de saltitos**: en cada bote
    una nota con glissando ascendente (`S.boing`, nuevo en el kit de `adv-juego.js`), un bajo que alterna y un toquecito en el
    aire. Se queda 5,2 s (parpadea antes de irse); si el carrito la alcanza, ladridos + la **palabra en grande** como en
    Arkanoid; si se va, la palabra vuelve a la cola. Entradilla con su imagen al empezar (3,2 s, un toque la salta).
  - Frase (`datos.frase`): "Contigo hasta hacer la compra parece una cita. Te quiero". El juego acaba al recoger toda la
    compra (los perseguidores se paran; si Briskanguro está en pantalla, aún se la puede pillar); después palabras que se
    esfuman al ritmo de globos y escribir la frase con el mismo sistema. Comprobación en el servidor (`ComprobarComecocos`).
  - Todos los ajustes en la constante `AJ` de `comecocos.js` (velocidades, salidas, vidas, cafés, visitas y saltos de Briska);
    el laberinto, en la constante `MAPA` del mismo fichero.
  - El carrito va siempre un 10% más rápido que los perseguidores (`AJ.velJug` = 1.1 × su velocidad del momento, que sube con lo comido) (Javier, 2026-11-17).
- **Juegos diarios** (Javier, 2026-11-17): globos, bloques (Arkanoid) y comecocos se quedan también en la zona Juegos
  (`/Juegos/Partida/{globos|arkanoid|comecocos}`), igual que en su casilla pero con una **frase del día** distinta por
  juego (lista de 24 en `Services/JuegosDiarios.cs`).
  - **Una partida al día** por juego y persona (`JuegoPartida`): se gasta al pulsar Empezar, sin "Otra vez" ni
    "Volver a jugar"; al acabar se apunta si acertó la frase y cuántas palabras cazó. El admin juega sin límite
    para probar. Cifras, el duelo y el crucigrama siguen sin límite. **Todo juego nuevo de este tipo entra aquí
    con el mismo límite.**
  - Cada juego se desbloquea en Juegos el día de su casilla del adviento, para no destriparla.
  - La tarjeta, la pantalla y los scripts de cada juego son parciales compartidos con la casilla
    (`Pages/Shared/_JuegoTarjeta`, `_JuegoPantalla`, `_JuegoScripts`). En Juegos, `js/juego-diario.js` hace de
    `window.adv`, comprueba la frase en el navegador y avisa al servidor (`Empezar`/`Terminar`). Los juegos miran
    `datos.diario` para no ofrecer otra partida.
- **Briska por el calendario** (Javier, 2026-11-17: que BriskaBird y las demás Briskas tengan protagonismo). En `/Adviento`
  y en cada casilla sale una Briska asomando por la esquina inferior derecha con un bocadillo de frase graciosa que cambia
  cada 16-30 s; tocarla: ladra (Web Audio), bota y suelta otra frase. De vez en cuando (18-45 s) otra cruza la pantalla
  volando o asoma por un lado y se esconde; tocarla también ladra. Se esconde mientras hay un campo con el foco (teclado del
  móvil en criptofrase, crucigrama, fotos…) y con los juegos a pantalla completa; con `prefers-reduced-motion` solo queda
  la del rincón, sin animaciones. Todo en `Pages/Shared/_Briska.cshtml`, `js/briska.js` y CSS `.briska-*`; las frases, en
  el JS.
  - **Imágenes en `wwwroot/img/briska/*.png`**: el servidor (`BriskaImagenes`, singleton, relee la carpeta cada minuto)
    las lista sin base de datos y las manda a la página en el JSON `#briska-imgs`; cada aparición elige una al azar. Para
    añadir una Briska nueva basta con copiar ahí un PNG **recortado y con fondo transparente** (unos 300 px de alto
    sobran) y desplegar. Recorte: en la sesión anterior se hizo con un programita .NET + ImageSharp que hace flood fill
    desde los bordes (los píxeles conectados al borde y parecidos al color de fondo pasan a transparentes) y recorta al
    rectángulo con contenido; cualquier editor que exporte PNG con transparencia vale igual.

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
- Capítulos (2026-11-13): un artista explicado + obras (paseo si tienen paradas, si no reseña). Se alternan con
  las obras sueltas: el capítulo sale cuando Javier lo marca listo (próximo domingo libre). Textos desenfadados.
  Para móvil vale ~2000-2600 px; si Commons no llega, WikiArt.
- Solo dominio público, con URL de origen y licencia en cada obra; nada del Prado directamente (Commons). No
  usar capturas de baja resolución. Descartadas: Matisse (DP en España en 2035) y fotos de esculturas con
  derechos del fotógrafo.
- Contenido inicial: `Seed/arte.json` (12 obras, 78 paradas, textos en el tono de la app con guiños a la
  familia) + imágenes en `App_Data/entrada/arte/` (fuera del repo); se importan desde Admin → Disfruta la vista.


## Arte por temas (Javier, 2026-11-15; sin implementar)
- **Domingos: capítulo completo de un artista**, como el de Vallotton. **Miércoles: una obra suelta** (más rápida).
- Tono siempre desenfadado, con pinceladas cómicas. Texto de contexto ~60 % del de Vallotton (~500 palabras).
- Los capítulos se agrupan en **temas (movimientos)**. Cada tema tiene un **tour general** (obras de varios
  autores que cuentan el movimiento) y **4-5 autores**, o más si el movimiento lo pide. Se intercalan los temas
  (un domingo de uno, el siguiente de otro) y se van completando poco a poco.
- Temas elegidos (todos de dominio público; 2026-11-15 Javier pidió cambiar holandeses, españoles y
  Renacimiento por algo más llamativo y exótico, con Japón):
  1. **Postimpresionismo y nabis**: Vallotton (ya hecho), Bonnard, Vuillard, Gauguin, Van Gogh, Cézanne.
  2. **Impresionismo**: Monet, Renoir, Degas, Berthe Morisot, Pissarro, Caillebotte.
  3. **Japón: del oro al grabado** (2026-11-15, Javier quiere lo más vistoso y no solo ukiyo-e): Hokusai,
     Rimpa (Ogata Kōrin y Sōtatsu: biombos de oro, *Lirios*, *Dioses del viento y del trueno*), Kuniyoshi,
     Itō Jakuchū (gallos y pavos reales en seda), Hiroshige, Kobayashi Kiyochika (luz nocturna de la era
     Meiji) y Ohara Koson (pájaros shin-hanga, de dominio público en España desde 2026). En el tour, el
     japonismo: cómo estos les volaron la cabeza a Monet, Van Gogh y los nabis.
     Fuera por derechos en España (80 años desde la muerte): Hasui Kawase (2038), Hiroshi Yoshida (2031),
     Kayama Matazō y Higashiyama Kaii.
  4. **Simbolismo y Art Nouveau**: Klimt, Mucha, Gustave Moreau, Odilon Redon, Böcklin, Franz von Stuck.
  5. **Lo fantástico**: El Bosco, Bruegel el Viejo, Arcimboldo, Füssli, William Blake.
  6. **Luz del Norte** (en lugar de Romanticismo, 2026-11-15): Munch, Hammershøi, Hilma af Klint,
     Gallen-Kallela, Krøyer, Zorn (DP en España: muertos antes de 1945).
- Orden de los domingos (vueltas: un capítulo de cada tema; primero los tours generales):
  1. Tours: Japón · Impresionismo · Lo fantástico · Luz del Norte (cae en diciembre) · Simbolismo · Nabis.
  2. Hokusai · Monet · El Bosco · Munch · Klimt · Van Gogh.
  3. Rimpa (Kōrin) · Degas · Arcimboldo · Hammershøi · Mucha · Bonnard.
  4. Kuniyoshi · Morisot · Bruegel · Gallen-Kallela · Redon · Gauguin.
  5. Jakuchū · Renoir · Füssli · Hilma af Klint · Böcklin · Vuillard.
  6. Hiroshige · Caillebotte · Blake · Krøyer · Moreau · Cézanne.
  7. Kiyochika · Pissarro · Zorn · Von Stuck.  8. Ohara Koson.
  (41 domingos, unos 9 meses.)
- Las obras sueltas ya cargadas (Sorolla, Arnolfini…) pasan a los miércoles.

## Juegos (`/Juegos`)
- Sección propia, enlazada desde la portada (🎲 Juegos). Todo se juega en el navegador; récords y partidas en
  `localStorage` (`cifras-stats`, `cruci-stats`, `cruci-<semilla>`), sin corazones ni base de datos.
- Cifras: 6 números de {1-10 ×2, 25, 50, 75, 100}, objetivo 101-999, + − × ÷ con resultados enteros positivos,
  cada número una vez, 45 s. Pasado el tiempo cuenta como no conseguido pero se puede seguir jugando sin
  reloj; la solución (buscador en JS) sale al pulsar "Ver la solución" o al clavarlo.
- Crucigrama: banco `Seed/crucigrama.json` ([palabra, pista], pistas con guasa y algún guiño a la familia).
  `CrucigramaGenerador` arma hasta 12 palabras en 11×11 como mucho; semilla = fecha (+ n para "otro"), así el
  del día es el mismo para los dos. Se comprueba en el cliente (lleva la solución; no hay premio que proteger).
- Crucigrama compartido (siempre, sin invitación): una fila `CrucigramaPartidas` por semilla con las letras y
  quién puso cada una (JSON); `CruciCompartido` la cachea en memoria. Una sola llamada `Sync` (cursor +
  letras pendientes → estado): cada 1 s si el otro está dentro, 3 s si no, nada con la app en segundo plano.
  Lo del otro se ve en violeta (`--otro`): sus letras, su palabra y su casilla. Gana la última letra escrita.
- Duelo de Cifras (`/Juegos/Duelo`, `DueloCifras` singleton, una sala en memoria): uno reta, al otro le sale el
  aviso en la portada y en Juegos; al aceptar empieza la ronda (cuenta atrás de 4 s). Mismos números, el
  servidor cronometra y decide: gana el más cercano, a igualdad el que llegó antes; si alguien lo clava se
  acaba la ronda. "Nueva partida" (cualquiera de los dos) lanza otra sin volver a retar. Durante la ronda solo
  se ve lo cerca que va el otro. Consulta cada ~1 s (sin websockets: nginx no los pasa). Las rondas
  terminadas se guardan en `DueloRondas` (marcador histórico); si se reinicia el servidor se pierde la sala.

- **Orden de las palabras en los juegos con frase** (Javier, 2026-11-17): durante la partida salen en el orden de la frase
  (la que se escapa vuelve a la cola en su sitio y sale la siguiente vez); al escribir se muestran desordenadas.
  El botón de la ayuda se ve desde el principio, bloqueado («se abre tras N intentos»), para que se sepa que existe.

- **Criptofrase sin autocompletar** (Javier, 2026-11-17): cada casilla se rellena por separado; escribir una letra no rellena
  las demás con el mismo número (es trabajo de quien juega). Solo la pista rellena todas las de su número. Afecta a adviento y viñetas.

## Français (píldoras de francés)
- Javier, 2026-11-17. Sección propia `/Frances` (enlace "🥐 Français" en Para ti y aviso de la píldora nueva de la última semana).
- **Lo primero es que sean divertidas**: ironía amable, finales con giro, algún guiño a la casa (niños, Briska, la compra); nada escolar.
- Salen **lunes, miércoles y viernes** (`FrancesService.Dias`); el programador (`FrancesProgramador`, cada hora) las encarga al agente
  un día antes. La primera sale el mismo día del despliegue. Temas en turno: texto, receta, noticia (con WebSearch), curiosidad, expresión.
- Cada píldora: lectura de 180-280 palabras con glosario (palabras subrayadas, al tocar se ve la traducción), 2-3 preguntas,
  4 huecos, 3 preguntas de vocabulario tipo test y una consigna para escribir. Foto de Wikipedia solo con licencia libre.
- Raquel entrega una vez (borrador guardado en el móvil); **♥ +1 al entregar**, sin depender de la nota (suma en `VinetaService.Corazones`).
  El vocabulario se corrige al momento; el resto lo corrige el agente (`TipoTareaAgente.PildoraCorreccion`) con humor y explicando los fallos.
- Javi (admin) ve las copias de Raquel con su corrección, puede encargar una a mano (sale hoy, con tema e idea), borrar las futuras y reintentar fallos.

## Línea del tiempo del arte (juego)
- Javier, 2026-11-18. Cinco obras famosas que se colocan **una a una** en la línea del tiempo (la primera se cuelga sola);
  acertar el autor entre 4 da +1. Máximo 9 puntos (4 colocaciones + 5 autores). Al fallar, la obra se coloca en su sitio igualmente.
- **Catálogo propio** `Seed/linea-arte.json` (86 obras de dominio público, s. XV-XX, imagen de Wikipedia con licencia libre),
  separado de las obras de los domingos. Imágenes bajadas y guardadas en `adjuntos/linea` la primera vez.
- **Una partida por semana** (lunes a domingo), las mismas 5 obras para los dos para poder comparar (`JuegoPartida` con
  Juego = "linea" y Fecha = lunes). Javi juega sin límite. Enlace en Juegos y en Arte.
- Presenta **BriskaBird pintora** (`img/briska-pintora.png`: BriskaBird con boina granate y pincel) con frases graciosas y ladridos.

## Importar la lista de Alexa
- Javier, 2026-11-18. Amazon no deja a apps externas leer las listas de Alexa (desde 2024), así que se **pega** el texto
  (copiado de la app de Alexa o de amazon.es) en `/Menu/Importar`, o se **comparte** hacia la app (share target del manifest).
- Antes de añadir se revisa: zona automática (`ZonaAuto`), cantidad leída («2 docenas de…», «x2», «(4)»), lo que ya está
  en la lista sale desmarcado; se ignoran cabeceras y botones copiados y todo lo que va tras «Completados».
- Se añade a la compra de la semana como productos extra (Manual).

