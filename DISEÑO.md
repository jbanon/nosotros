# Nosotros — diseño de la aplicación

Aplicación privada para dos personas (Javier = Admin, Raquel), pensada para móvil.
Tres bloques: **Para Raquel** (página de amor con mensaje diario), **Menú semanal**
(recetas → lista de la compra) y **Gastos** (control mensual con objetivo de compra).

---

## 1. Stack y decisiones técnicas

| Tema | Decisión |
|---|---|
| Framework | ASP.NET Core **Razor Pages**, .NET 10 (SDK ya instalado en el servidor) |
| Datos | **SQLite** vía EF Core (`App_Data/love.db`). Un único fichero; copia de seguridad desde Admin (usa la API de backup de SQLite) |
| Adjuntos | Carpeta local `App_Data/adjuntos/gastos/{año}/{mes}/{guid}.{ext}`. Nunca en wwwroot (requieren login). Las fotos se reducen en el móvil (canvas, máx. 1600 px) antes de subir |
| Auth | Cookie auth. Login por PIN. Dos usuarios con rol `Admin` / `Raquel`. PINs guardados **hasheados** (BCrypt) en la tabla `Usuario`, sembrados al arrancar |
| Front | CSS propio (sin Bootstrap), variables CSS por tema, fuentes Google (Cormorant Garamond + Nunito). JS mínimo (vanilla) |
| Móvil | Mobile-first, **PWA** (manifest + icono) para "Añadir a pantalla de inicio" y abrir a pantalla completa |
| Zona horaria | `Europe/Madrid` para calcular "hoy" (mensaje del día, temporada, mes de gastos) |
| Despliegue | Kestrel como servicio systemd en este Linux, detrás de nginx con HTTPS. Puerto interno 5100 |
| Semilla | `Seed/mensajes.json` (366 mensajes + días especiales), `Seed/recetas.json` (recetas iniciales), `Seed/ingredientes.json` |

Estructura del proyecto:

```
Nosotros/
  Program.cs
  Pages/
    Login.cshtml                 PIN
    Index.cshtml                 redirige según rol
    Amor/Index.cshtml            página de Raquel
    Menu/Recetas/Index.cshtml    listado + buscador
    Menu/Recetas/Ver.cshtml      detalle
    Menu/Recetas/Editar.cshtml   alta / edición (ingredientes dinámicos)
    Menu/Semana.cshtml           selección de recetas de la semana
    Menu/Lista.cshtml            lista de la compra
    Gastos/Index.cshtml          mes actual, resumen y movimientos
    Gastos/Editar.cshtml         alta / edición con adjunto
    Gastos/Adjunto.cshtml        descarga segura del fichero
    Admin/Mensajes/Index.cshtml  calendario de frases/poemas
    Admin/Mensajes/Editar.cshtml
    Admin/Config.cshtml          importes por defecto, objetivo, firma
    Shared/_Layout.cshtml        barra inferior de navegación
  Models/   Data/   Services/   Seed/   wwwroot/
```

---

## 2. Usuarios y navegación

| Usuario | PIN | Página de inicio | Ve |
|---|---|---|---|
| Admin | (en `appsettings.Local.json`) | Gastos | Todo: Gastos, Admin (mensajes, fechas especiales, config) y "ver página de Raquel" |
| Raquel | (en `appsettings.Local.json`) | Para Raquel | Para Raquel, Recetas, Semana y Lista de la compra. **Sin Gastos ni Admin** |

- Login: pantalla limpia, teclado numérico grande, sin usuario: el PIN identifica a la persona.
- Sesión persistente 90 días (que no tenga que meter el PIN cada vez en el móvil).
- 5 intentos fallidos → espera de 1 minuto (protección mínima).
- Navegación: **barra inferior** fija con 3-4 iconos: ♥ Raquel · 🍲 Menú · € Gastos · ⚙ Admin (solo admin).

---

## 3. Página "Para Raquel"

### 3.1 Contenido
- Saludo: "Buenos días / Buenas tardes / Buenas noches, Raquel".
- **Mensaje del día**: frase corta o poema breve (4-8 versos). 366 mensajes, uno por día del año (29 feb incluido). Se repiten cada año.
- **Fechas especiales** (tabla propia, gestionable desde Admin: el admin puede añadir nuevas) con mensaje que **prevalece** sobre el del calendario y tema visual opcional:
  - 20 may — aniversario (2024). Muestra "2 años juntos" (contador con `{n}`).
  - 3 sep — cumpleaños (1980). Muestra "Hoy cumples 46" y tema de fiesta.
  - 14 feb, 1 ene, 24 dic, 25 dic, 31 dic.
- Contador discreto al pie: "Llevamos N días juntos" (desde 20/05/2024).
- Cuenta atrás cuando queden ≤ 30 días para aniversario o cumpleaños: "Faltan 12 días para nuestro aniversario".
- Firma configurable (por defecto "Javier").
- Gesto: tocar el corazón lanza una pequeña lluvia de corazones (detalle, no funcionalidad).

### 3.2 Temas por época del año

El tema cambia solo por fecha. Cada tema = paleta + fondo + animación suave + icono.

| Tema | Fechas | Paleta | Detalle visual |
|---|---|---|---|
| Navidad | 1 dic – 6 ene | granate, verde abeto, dorado | luces cálidas parpadeando, nieve lenta |
| Invierno | 7 ene – 19 mar | azul hielo, blanco, lila suave | copos de nieve, niebla |
| San Valentín | 14 feb | rojo, rosa | corazones flotando (sustituye a Invierno ese día) |
| Primavera | 20 mar – 20 jun | rosa cerezo, verde tierno, crema | pétalos cayendo |
| Aniversario | 20 may | dorado, rosa | pétalos + brillo, tipografía mayor |
| Verano | 21 jun – 21 sep | coral, turquesa, arena, atardecer | luciérnagas / reflejo de sol |
| Cumpleaños | 3 sep | fiesta: confeti multicolor sobre fondo verano | confeti, tarta |
| Otoño | 22 sep – 30 nov | ocre, naranja quemado, burdeos | hojas cayendo |
| Halloween | 25 oct – 1 nov | morado noche, naranja calabaza | calabazas y murciélagos simpáticos, luna |

Implementación: `TemaService.Resolver(fecha)` devuelve un `Tema` (nombre, clase CSS, animación). El CSS usa `data-tema="otono"` en `<body>` y variables `--fondo`, `--acento`, `--texto`. Las animaciones son CSS puro (sin librerías), con `prefers-reduced-motion` respetado.

### 3.3 Maqueta (móvil)

```
┌──────────────────────────────┐
│  (fondo degradado del tema,  │
│   partículas suaves)         │
│                              │
│      Buenas tardes, Raquel   │
│      miércoles 5 noviembre   │
│                              │
│   ╭────────────────────────╮ │
│   │   "Hay días que no     │ │
│   │    necesitan nada más  │ │
│   │    que tu risa en      │ │
│   │    casa."              │ │
│   │                        │ │
│   │            — Javier    │ │
│   ╰────────────────────────╯ │
│                              │
│            ♥                 │
│   Llevamos 899 días juntos   │
│                              │
├──────────────────────────────┤
│   ♥ Raquel   🍲 Menú   € Gastos│
└──────────────────────────────┘
```

Tipografía del mensaje: Cormorant Garamond itálica, tamaño grande (26-30 px), interlineado amplio, tarjeta semitransparente con desenfoque de fondo.

### 3.4 Datos: tabla `Mensaje`

| Campo | Tipo | Notas |
|---|---|---|
| Id | int | |
| Mes | int 1-12 | |
| Dia | int 1-31 | |
| Tipo | enum | `Frase` / `Poema` |
| Texto | string | saltos de línea = versos |
| Titulo | string? | opcional en poemas |
| Especial | bool | true en aniversario, cumple, etc. (no editable la fecha) |
| Activo | bool | |

Regla de selección: mensaje de (mes, día) actual; si no existe (borrado), se usa uno aleatorio del mismo mes. 29 feb tiene el suyo.

Contenido: lo escribiré yo íntegro: 366 mensajes originales en castellano, tono íntimo y sencillo, mezcla ~60 % frases y ~40 % poemas cortos, en sintonía con la estación (nieve en enero, mar en agosto, hojas en octubre, luces en diciembre) y con guiños a lo cotidiano (café, casa, domingos). Se entregan en `Seed/mensajes.json` y se cargan la primera vez; después se gestionan desde Admin.

---

## 4. Menú semanal

### 4.1 Recetas
- Listado en tarjetas: nombre, foto (opcional), nº ingredientes, etiquetas (comida/cena/postre), buscador por nombre o ingrediente.
- Detalle: ingredientes con cantidad y zona (chip de color), raciones, preparación (texto libre, opcional).
- Alta/edición: formulario con filas de ingrediente dinámicas (nombre con autocompletado del maestro, cantidad, unidad, zona). Al escribir un ingrediente que ya existe, la zona se rellena sola.
- Ambos usuarios pueden crear y editar recetas.

### 4.2 Semana
- Pantalla "Esta semana": lista de todas las recetas con un **check** para seleccionarlas (y botón +/− para repetir una receta dos veces). No se asigna día concreto. También se puede añadir desde el detalle de la receta.
- Una semana = lunes de inicio. Se puede navegar a la siguiente para planificar con antelación.
- Botón grande: **"Generar lista de la compra"**.

### 4.3 Lista de la compra
- Se genera sumando ingredientes de las recetas seleccionadas: mismo ingrediente + misma unidad → se suman cantidades; unidad distinta → dos líneas.
- Se muestra **completa, ordenada por zona**, cada zona con cabecera y color de fondo suave en sus filas:

| Zona | Color |
|---|---|
| Frutería | verde claro |
| Charcutería | rosa salmón |
| Carnicería | rojo suave |
| Pescadería | azul claro |
| Otros | amarillo arena |

- Cada línea: check "comprado" (se guarda; tachado al marcar), nombre, cantidad, debajo en pequeño "para: Lentejas, Arroz al horno".
- **Ampliable**: líneas manuales ("papel de cocina") con su zona, marcadas como "extra".
- **Desmarcable**: cualquier línea se puede quitar con × ("no comprar esta vez"); pasa al bloque "No se compra esta vez" desde donde se vuelve a incluir con un toque. Los ingredientes de **despensa** (sal, aceite, pimentón…) nacen desmarcados: aparecen en ese bloque y no ensucian la lista.
- Regenerar (automático al cambiar recetas) no borra lo manual, lo marcado ni lo desmarcado mientras el ingrediente siga en alguna receta.
- Botón **"Compartir"**: copia la lista como texto por zonas (para WhatsApp) usando Web Share API.

Maqueta:

```
┌──────────────────────────────┐
│ ← Lista de la compra   ⤴     │
│ Semana 3-9 nov · 5 recetas   │
│                              │
│ ■ FRUTERÍA                   │
│ ☐ Cebolla          3 ud      │
│ ☐ Tomate           1 kg      │
│ ☑ Limón            2 ud      │
│ ■ CARNICERÍA                 │
│ ☐ Pollo (muslos)   1 kg      │
│ ■ PESCADERÍA                 │
│ ☐ Merluza          600 g     │
│ ■ CHARCUTERÍA                │
│ ☐ Chorizo          150 g     │
│ ■ OTROS                      │
│ ☐ Lentejas         500 g     │
│ ☐ Arroz            400 g     │
│                              │
│ + Añadir algo                │
└──────────────────────────────┘
```

### 4.4 Datos

```
Ingrediente        (Id, Nombre único, Zona enum, UnidadHabitual)
Receta             (Id, Nombre, Descripcion, Raciones, Etiquetas, Preparacion, FotoPath, Creada, Modificada)
RecetaIngrediente  (RecetaId, IngredienteId, Cantidad decimal?, Unidad, Nota)
Semana             (Id, LunesInicio date único, Notas)
SemanaReceta       (SemanaId, RecetaId, Veces int)
ListaItem          (Id, SemanaId, IngredienteId?, Nombre, Cantidad, Unidad, Zona, Incluido bool, Comprado bool, Manual bool, Origen texto)
Ingrediente.Despensa bool   → sal, aceite…: no entra en la lista por defecto
Zona enum          Fruteria, Charcuteria, Carniceria, Pescaderia, Otros
```

Las recetas que vayas pasando las cargo en `Seed/recetas.json` con sus ingredientes y zonas; el formato será: nombre, raciones, etiquetas, lista de `{ingrediente, cantidad, unidad, zona}`, preparación.

---

## 5. Gastos

### 5.1 Pantalla principal (mes)
- Cabecera con navegación de mes: `‹  Noviembre 2026  ›`.
- **Tarjeta objetivo de compra**: gastado / objetivo (350 €), barra de progreso con color (verde < 70 %, ámbar 70-100 %, rojo > 100 %) y texto "Te quedan 128,40 €" o "Te has pasado 20 €".
- Resumen por tipo: Veva · Hipoteca · Compra · **Total del mes**.
- Lista de movimientos agrupada por día, con chip de color por tipo y clip 📎 si tiene adjunto. Tocar → editar.
- Botón flotante **+** para nuevo gasto.

### 5.2 Alta de gasto
- Tipo como 3 chips grandes: **Veva · Compra · Hipoteca**.
- Al elegir Veva → importe se rellena con 300; Hipoteca → 900; Compra → vacío con foco y teclado numérico.
- Fecha (hoy por defecto), importe, nota opcional.
- Adjunto: foto o fichero (`accept="image/*,application/pdf"`, `capture="environment"` para abrir cámara en móvil). Las fotos se reducen a ≤1600 px al guardar para no llenar disco. Vista previa en miniatura.
- **Fijos automáticos**: la primera vez que se abre un mes (hasta el actual) se crean solos Veva 300 e Hipoteca 900 con fecha día 1 y marca "fijo". Si el admin borra uno para un mes concreto, **no se vuelve a crear** (tabla `MesInicializado`).

### 5.3 Datos

```
Gasto           (Id, Fecha date, Tipo enum {Veva, Compra, Hipoteca}, Importe decimal, Nota,
                 AdjuntoPath, AdjuntoNombre, AdjuntoMime, EsFijo bool, Creado)
MesInicializado (Anio, Mes)   → meses cuyos fijos ya se generaron
Config  (Clave, Valor)  →  ObjetivoCompra=350, ImporteVeva=300, ImporteHipoteca=900, Firma=Javier
```

Importes y objetivo editables en Admin → Config sin tocar código. Descarga de adjuntos solo con sesión (página `Gastos/Adjunto?id=`).

---

## 6. Admin

- **Mensajes**: vista por mes (12 pestañas) con los días en lista: día, tipo, primeras palabras. Edición, buscador por texto, "Ver cómo lo verá Raquel el día X" (previsualiza con el tema de esa fecha).
- **Fechas especiales**: alta, edición y borrado. Campos: nombre, día/mes, emoji, tema visual, año de origen y texto del contador, mensaje, cuenta atrás.
- **Config**: objetivo compra, importes fijos, firma, nombre, cambio de PIN de cada usuario, importación de recetas en JSON.
- **Ver página de Raquel** (previsualización de hoy o de cualquier fecha).
- Copia de seguridad: botón que descarga `nosotros.db` + zip de adjuntos.

---

## 7. Estilo general (fuera de la página de amor)

- Fondo crema muy claro, tarjetas blancas con sombra suave, bordes redondeados 16 px.
- Acento: un color cálido (terracota suave `#c96b5a`) para botones y elementos activos.
- Tipografía Nunito para la interfaz; Cormorant solo en la página de Raquel.
- Botones grandes (≥ 48 px alto), formularios de una columna, inputs numéricos con `inputmode="decimal"`.
- Modo oscuro automático según el móvil (menos en la página de amor, que manda el tema).

---

## 8. Fases de desarrollo

1. **Base**: proyecto, SQLite, login por PIN, layout móvil, barra inferior, PWA.
2. **Para Raquel**: temas por época, animaciones, 366 mensajes + especiales, contador.
3. **Menú**: ingredientes, recetas, semana, lista de la compra por zonas, compartir.
4. **Gastos**: mes, objetivo, tipos con importe por defecto, adjuntos.
5. **Admin**: gestión de mensajes, config, previsualización, copia de seguridad.
6. **Despliegue**: servicio systemd + nginx + HTTPS; prueba en móvil.

---

## 9. Decisiones cerradas

- Raquel no tiene Admin. Sí puede crear y editar recetas, usar la lista de la compra y registrar gastos (cambio de criterio 8/11/2026); solo el admin borra los fijos.
- Histórico de gastos por meses en /Gastos/Historico (resumen compra/objetivo, Veva, Hipoteca, total; agrupado por año).
- Los fijos del mes (Veva, Hipoteca) se crean solos; el admin puede borrarlos para un mes concreto y no se regeneran.
- El admin puede añadir fechas especiales nuevas.
- La lista de la compra es ampliable (extras) y desmarcable (× por línea; despensa desmarcada por defecto).
- La selección semanal no asigna día concreto.
- La lista se calcula siempre para 12 raciones (somos una gran familia; configurable en Admin): cada receta guarda sus cantidades para sus raciones y se escalan.
- Las cantidades se suman por ingrediente y unidad (singular/plural se unifican); no hay conversión g↔kg.
- Los mensajes van firmados como "Javier" (cambiable en Config).
- Despliegue previsto con systemd + nginx + HTTPS (ficheros en `deploy/`).
- Recetas siempre a 12 raciones (migración única al arrancar); sugerencias de recetas por historial en Semana; líneas de ticket en compras → compras recurrentes (mediana de intervalos) y aviso "toca reponer" en la lista; Lecturas con lector de tamaño ajustable, gestionadas por el admin y anunciadas en la página de amor.
- Páginas compartidas "Ideas" (cosas que automatizar, primera: pago basket) y "Pelis" (películas que ver, primera: Full Monty). Raquel las tiene en la barra; el admin las ve desde su menú.
