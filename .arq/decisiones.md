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

## Calendario de adviento
- 24 casillas del 1 al 24 de diciembre (año en `AdvientoCasilla.Anio`; en enero sigue valiendo el de diciembre). Una casilla se abre cuando llega su día (hora de Madrid); las pasadas siguen abiertas; las futuras muestran un mensaje gracioso. Reparto: 6 criptofrases, 5 murdokus, 5 crucigramas, 5 fotos, 2 vales y la carta final del 24.
- Cada enigma (y cada foto) da una letra; las 21 letras en orden de día forman la frase final (`Config.AdvientoFrase`, "Gracias por quererme así"). Admin avisa si letras y frase no coinciden.
- Fotos: con pregunta → borrosa hasta acertar (la letra se gana al acertar); sin pregunta → se ve directa y la letra se gana al abrir. Fotos en `App_Data/adjuntos/adviento`, fuera del repo. Cualquier casilla con foto sale en el collage del 24.
- El contenido inicial vive en `Seed/adviento.json` (se carga una vez por año si no hay casillas); después se edita en Admin → Calendario de adviento (JSON del pasatiempo plegado). Las comprobaciones se hacen en el servidor (handlers JSON `Resolver`, `Pista`, `Estado`, `Canjear`); el admin puede jugar en vista previa sin que se guarde nada.
- Vales: Raquel pulsa "Canjear" y queda la fecha; el tercer vale (peli noruega) va dentro del día 24.
- Datos de la familia para los pasatiempos: Naia (14, guitarra, "en la parra"), Marcos (14, basket, sobradillo), Elsa (12, corre como un gamo), Miranda (11, la graciosa), Aroa (10, achuchable, baile), Briska (labrador negro, futura perra guía). Se conocieron chateando; cada verano una semana los dos en el norte (Irlanda del Norte, Gales, quizá Finlandia); "big project": casita en el campo con despachito y barracón para toda la familia.

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

