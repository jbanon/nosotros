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
- Tema otoño: tonos suaves (`--fondo-1:#f9ead4` … `--acento-amor:#a9442a`), hojas 🍂🍁 sin
  la verde. Modo oscuro vía `prefers-color-scheme` en las páginas normales.
- Lecturas: Javier pega el texto (o sube .txt) en Admin → Lecturas; lector a pantalla completa
  con A−/A+ recordado por dispositivo; aviso en la página de Raquel hasta que marca "leída".
  NO incluir en el repo textos con derechos (artículos de prensa, libros): los pega Javier.

## Interfaz
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

