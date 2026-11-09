# Tareas

## Hilos abiertos
- **Adviento: Javier debe subir las fotos (días 1, 7, 11, 16, 20) y revisar textos desde Admin → Calendario de adviento antes del 1 de diciembre.** Opcional: pregunta/respuesta por foto (si no, la foto se ve directa y la letra se gana al abrir). Pendiente ver capturas en el móvil (diseño validado a ciegas) y confirmar que el teclado se abre bien en criptofrase y crucigrama (input oculto `.teclado-oculto`).
- **Fichas automáticas de películas y recetas con `claude -p` (opción B, explorada
  2026-11-09, pendiente de decisión).** Botón "✨ Completar ficha" → cola → BackgroundService
  lanza `claude -p --no-session-persistence --output-format json --json-schema … --allowedTools
  WebSearch,WebFetch --max-turns 6 --model haiku` → parsea `structured_output` → guarda y
  descarga el póster a `wwwroot/uploads`. Probado en local: 26 s, resultado correcto. En el
  servidor hace falta `claude setup-token` → `CLAUDE_CODE_OAUTH_TOKEN` en el entorno de
  `www-data` y ruta absoluta del binario. No usar `--bare` (no lee credenciales); limpiar
  `CLAUDECODE` y `CLAUDE_CODE_ENTRYPOINT` si se lanza desde otra sesión. Alternativa A
  descartada por ahora: API de Anthropic + OMDb. Falta: migración (sinopsis, año, director,
  imdb, póster, curiosidad en Película; ingredientes/pasos generados en Receta), servicio,
  botón, refresco de la tarjeta, token en el servidor.
- **Lista de la compra: "elementos abajo que no se ven"** (captura en modo oscuro,
  2026-11-08). Se aumentó el relleno inferior y se versionó el CSS; Javier debe confirmar tras
  desplegar. Si sigue, pedir captura.
- Javier aún no ha pegado el texto de "¡Oigan!" (Javier Marías) en Lecturas: lo hace él.

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
| 2026-11-09 | Tema Navidad adelantado al 25 de noviembre | 49af518 |
| 2026-11-09 | Calendario de adviento 2026: 24 casillas (fotos, criptofrases, murdokus, crucigramas, vales, carta final), letras → frase final, Admin para fotos/contenidos/seguimiento, aviso en la página de Raquel | e05af01 |
