# Historial de cambios

Traducción de [CHANGELOG.md](../../CHANGELOG.md). El inglés es la fuente; si
ambos difieren, vale el texto inglés.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es/1.1.0/), y el
versionado [Semantic Versioning](https://semver.org/lang/es/).

## [Sin publicar]

## [0.9.0] – 2026-08-21

### Añadido
- **El paquete de macOS está firmado y notarizado.** Hasta ahora llevaba una
  firma ad hoc, que no vale nada para nadie salvo para la máquina que la creó:
  macOS se negaba a abrirlo, y conseguirlo significaba desdecir al sistema a
  mano. Ahora lleva un Developer ID y la notarización de Apple, grapada dentro
  del paquete para que valga también sin conexión. Se abre con un doble clic,
  como cualquier otra cosa.
- **Sustitución automática en macOS.** El botón que actualiza la versión de
  Windows desde la 0.4.0 hace ahora lo mismo aquí. La diferencia está en lo que
  se sustituye —un paquete entero en lugar de un archivo— y en lo que se
  comprueba antes: además de la suma SHA-256 publicada, la firma de la versión
  descargada, con la misma pregunta que haría Gatekeeper. Lo que macOS no
  ejecutaría tampoco se instala.

  Ambas cosas, porque ninguna sustituye a la otra. Una suma de verificación
  demuestra que el archivo es el que envió el servidor; una firma demuestra
  quién lo creó.

## [0.8.0] – 2026-08-20

### Añadido
- **macOS.** La aplicación vive ahora también en la barra de menús: un icono con
  los límites informados, las mismas ventanas, los mismos nueve idiomas. El
  inicio de sesión propio va al llavero, el arranque automático se hace con un
  agente de inicio del usuario, y el token de una instalación de Claude Code se
  lee del llavero como hasta ahora.

  Allí el menú es nativo: la decisión contraria a la de Windows por la misma
  razón. Un icono de la barra de menús abre un menú del sistema, y una ventana
  imitando uno sería justo lo que desentonaría.

  Se entrega como paquete de aplicación para Apple silicon, firmado ad hoc y no
  por un desarrollador registrado. La sustitución automática sigue desactivada
  en macOS por ahora: la nueva versión se descarga a mano.

- **Claro u oscuro, a elección.** La aplicación siempre ha seguido la apariencia
  del sistema; eso sirve en la mayoría de los casos y sigue siendo lo
  predeterminado. En «Apariencia» ahora puede fijarse en claro u oscuro. La
  elección surte efecto mientras se hace: el color es el único ajuste cuyo
  efecto no se deja describir en una frase.
- **Los dos inicios de sesión de un vistazo.** La configuración indica ahora,
  arriba del todo, si hay una instalación de Claude Code con sesión iniciada y
  si el inicio de sesión propio funciona: ambos, siempre, sea cual sea el que
  se esté usando. Qué vía se toma no dice nada sobre si la otra funcionaría, y
  esa es justo la pregunta cuando dejan de llegar las cifras.

### Corregido
- **Las versiones de prueba a partir de la décima no se ofrecían.** La etiqueta
  tras el guión se compara ba como texto, de modo que «beta.10» quedaba por
  debajo de «beta.9»: a quien probaba se le decía que estaba al día. Ahora las
  etiquetas se cuentan, parte por parte, como manda el versionado semántico.
## [0.7.2] – 2026-08-20

### Corregido
- **El resumen de novedades no aparecía al salir de una versión de prueba.** Lo
  que se ejecutó la última vez se anotaba con tres números y sin etiqueta, de
  modo que 0.7.1-beta.5 y la 0.7.1 definitiva dejaban el mismo rastro: el paso
  entre ambas era invisible y el resumen nunca llegaba. Ahora la etiqueta se
  guarda, y llegar a la versión definitiva cuenta como un paso adelante aunque
  el número no cambie. Entre dos versiones de prueba del mismo número no dice
  nada: el historial no tiene novedades que contar ahí.
- **El resumen indica cuando se ejecuta una versión de prueba.** El título
  nombra la versión de la entrada del historial, y el historial no conoce
  versiones de prueba: 0.7.2-beta.1 se leía como «Novedades de la versión
  0.7.2» sin que en ninguna parte constara que aún no se había llegado a la
  versión definitiva.
## [0.7.1] – 2026-08-20

### Añadido
- **El botón de actualizar busca de paso una versión nueva.** Una pulsación en
  lugar de dos. Se puede desactivar en la configuración, para quien prefiera no
  tocar la red más de lo necesario.
- **La configuración se presenta en dos columnas.** En una sola columna la
  ventana quedaba alta y estrecha, lo que obligaba a desplazarse en la pantalla
  de un portátil por ajustes que caben cómodamente uno al lado del otro.

### Cambiado
- Base para las versiones de prueba. En el uso diario no se nota nada de ello.

### Corregido
- **El menú del área de notificación crece con su contenido.** Tenía un ancho
  fijo y la línea del uso adicional no cabía: lleva dos importes y una moneda,
  así que se partía en dos líneas. En un menú cuyas demás líneas son un límite
  cada una, una línea partida parece dos.
- **Correcciones de traducción.** Seis idiomas seguían llamando «créditos» al
  uso adicional aunque la cifra es dinero, y todos afirmaban que un historial
  de cambios sin traducir se muestra en alemán: se muestra en inglés. Los
  mensajes de error del almacén de credenciales de Windows estaban fijados en
  alemán; ahora siguen el idioma de la interfaz.

### Seguridad
- La página de publicación que devuelve GitHub se exige ahora con el mismo
  criterio que las direcciones de descarga: solo https.
- Las acciones de los flujos de trabajo se fijan por hash de confirmación en
  lugar de por etiquetas móviles. Una etiqueta puede reapuntarla quien
  administre el repositorio de la acción; un hash no. Importa sobre todo en la
  acción de publicación ajena, que se ejecuta con permiso de escritura en el
  flujo que compila el ejecutable publicado.

## [0.7.0] – 2026-08-20

### Cambiado
- **El menú del área de notificación lo dibuja ahora la propia aplicación.**
  Windows dibuja los menús contextuales con la fuente del sistema y sin marco
  propio; junto a las ventanas de esta aplicación parecía otro programa. Ahora
  lleva el mismo marco, la misma fuente y los mismos espacios.
- **El menú indica la versión.** La entrada dice ahora «Acerca de Claude
  UsageChecker 0.7.0 …». Es lo primero que se pregunta a quien informa de un
  problema.

## [0.6.4] – 2026-08-20

### Corregido
- **Una ventana cuyo reinicio ya vencía no tenía una frase propia.** El bloque
  para una duración se insertaba en un hueco que espera una duración, y salía
  «Sesión: 39 % - ahora restante». Los cuatro lugares que hablan de tiempo
  restante tienen ahora una frase para este caso: «reinicio pendiente».

## [0.6.3] – 2026-08-20

### Corregido
- **La ventana de detalles quedaba por debajo del centro cuando había una
  actualización disponible.** Se crea una vez y se reutiliza, de modo que
  `CenterScreen` solo actuaba la primera vez; el aviso de actualización llega
  segundos después y la hace unos cien píxeles más alta, creciendo hacia
  abajo. Ahora se vuelve a centrar cada vez que su contenido cambia de tamaño.

## [0.6.2] – 2026-08-20

### Cambiado
- **El icono del área de notificación indica su estado.** Sin sesión: gris. Con
  sesión y todo en orden: una marca verde; a partir del umbral de aviso, un
  signo de interrogación ámbar; del crítico, una exclamación roja. Antes esto
  lo hacía solo el color. Un signo por estado: a dieciséis píxeles, dos no se
  distinguen.

### Corregido
- **El saldo adicional se mostraba cien veces mayor y en la unidad equivocada.**
  La API informa `used_credits: 2276`, y no son 2276 créditos sino 22,76 EUR:
  un importe en la unidad más pequeña de su moneda. La aplicación tomaba el
  número al pie de la letra. **La moneda procede de la cuenta** —USD, BRL, lo
  que corresponda—, igual que el número de decimales, porque no toda moneda
  tiene dos. Ahora se lee el campo `spend`, que dice qué significan sus cifras.

## [0.6.1] – 2026-08-20

### Cambiado
- Versión de mantenimiento.

## [0.6.0] – 2026-08-20

### Corregido
- **Los límites semanales por modelo no aparecían.** Quien tiene un límite de
  Fable no lo veía en ninguna parte – ni en la información sobre herramientas,
  ni en el menú contextual, ni en la ventana de detalles – aunque el propio
  Claude lo indica. El motivo: la aplicación leía los campos `seven_day_opus` y
  `seven_day_sonnet`, que llevan el nombre del modelo en el identificador. Ambos
  están ahora vacíos, y no existe ningún campo `seven_day_fable`.

  La API entrega los mismos valores además en una lista `limits`, que nombra el
  modelo en su contenido (`scope.model.display_name`). Esa lista tiene ahora
  preferencia; los campos antiguos quedan como respaldo. **Cualquier modelo
  futuro aparecerá por sí solo**, sin cambios aquí. Detalles en
  [docs/api-research.md](../api-research.md).

  El icono del área de notificación también tiene en cuenta esos límites – antes
  se quedaba verde mientras un cupo de modelo ya estaba agotado.

### Añadido
- **Nueve idiomas.** Alemán, inglés, español, francés, italiano, portugués
  (Brasil y Portugal por separado), ruso y chino simplificado. En el primer
  arranque la aplicación sigue el idioma del sistema; se puede cambiar en la
  ventana de instalación – donde la elección surte efecto de inmediato y la
  asumen **ambos** botones – y más tarde en cualquier momento en la
  configuración.

  Con el idioma cambia también la cultura de números, fechas y horas: quien pone
  la interfaz en francés no espera allí fechas alemanas.

  **El historial de cambios también está traducido.** El resumen que aparece
  tras una actualización sale, por tanto, en el mismo idioma que la interfaz. El
  inglés es la fuente y está en [CHANGELOG.md](../../CHANGELOG.md); las
  traducciones, el alemán entre ellas, se encuentran en [docs/changelog/](.).

  No se traducen los nombres de producto ni de modelo: «Claude UsageChecker»,
  «Claude Code» y el nombre del modelo que da la API: «Fable» se llama Fable en
  todos los idiomas.
- **Los umbrales de aviso y crítico son configurables.** A partir de qué uso el
  icono se vuelve amarillo, y a partir de cuál rojo, se ajusta ahora en la
  configuración en lugar de estar fijado en el código (valores por defecto sin
  cambios: 75 % y 90 %). Un umbral de aviso por encima del crítico se rechaza en
  lugar de corregirse en silencio: nunca llegaría a aplicarse.
- **Resumen de las novedades tras una actualización.** En el primer arranque de
  una versión nueva, la aplicación muestra qué ha cambiado desde la versión que
  se ejecutó antes. Las versiones intermedias omitidas se incluyen. La fuente es
  el historial que viaja con el programa, sin acceso a la red: el resumen está
  disponible sin conexión y muestra por fuerza el estado que corresponde a la
  versión en ejecución. En el primerísimo arranque se omite.
- **«Acerca de Claude UsageChecker» en el menú contextual.** Muestra el icono,
  la versión, una breve descripción y lleva a la página del proyecto. Desde allí
  también se llega al historial completo.

### Cambiado
- **El idioma del proyecto es el inglés.** Documentación, comentarios,
  identificadores y nombres de pruebas: todo en el repositorio salvo los textos
  alemanes de la interfaz y el historial de commits hasta ahora. El motivo es
  sencillo: es un repositorio público, y quien lo encuentre debería poder
  leerlo. La documentación en alemán se mantiene en paralelo bajo
  [docs/de/](../de/).
- La versión ejecutada por última vez queda anotada en el archivo de
  configuración (`lastRunVersion`). Es el único dato por el que la aplicación
  puede reconocer una actualización: el ejecutable en sí no sabe qué se ejecutó
  antes que él.

  Las versiones anteriores no conocían ese campo. Quien actualiza desde una de
  ellas no tiene nada anotado; en ese caso decide la existencia del archivo de
  configuración: demuestra que la aplicación ya se ha ejecutado, y se muestran
  las novedades de la versión en curso. Sin esa rama, justo la versión que
  introduce el resumen no mostraría ninguno.
- `MonitorOptions` ya no lleva los umbrales. El monitor nunca los leyó: obtiene
  valores, no los juzga. El juicio ocurre en un único sitio, en
  `TrayIconSeverityResolver`, a partir de la configuración del usuario. Dos
  sitios para el mismo dato serían una invitación a girar más tarde el mando
  equivocado.
- El `PollInterval` calculado ya no se escribe en el archivo de configuración.
  Allí nunca se leía; solo parecía un segundo dato sobre el intervalo de
  consulta, capaz de contradecir al primero.
- **La ventana de configuración se queda en la pantalla.** Crece con su
  contenido y no se puede redimensionar; en una pantalla baja se salía por abajo
  llevándose consigo el botón «Guardar». Ahora lo impiden dos cosas: la fila de
  botones queda anclada bajo el área desplazable y permanece visible por baja
  que sea la pantalla, y la ventana se mide una vez compuesta y se desplaza
  hacia arriba si aún sobresale. Limitar la altura no bastaba: Avalonia centra
  la ventana según la altura que tiene al abrirse, y el contenido crece después.

### Eliminado
- **La introducción manual de un token** ha desaparecido de la configuración.
  A nadie podía servirle: el único token que se podía pegar procede de
  `claude setup-token`, y carece del ámbito `user:profile` que exige el punto de
  acceso. Los tokens que sí funcionan —el de la instalación de Claude Code y el
  del inicio de sesión propio— no se escriben a mano. Un token guardado por una
  versión anterior se sigue leyendo; solo desaparece la manera de añadir uno.
  Justificación en [docs/api-research.md](../api-research.md).

### Documentación
- **Plantillas para informes de error y peticiones de función** en
  `.github/ISSUE_TEMPLATE/`, además de una plantilla para pull requests y
  [CONTRIBUTING.md](../../CONTRIBUTING.md) – en inglés, para que también se
  pueda informar desde fuera del ámbito germanohablante. Los formularios
  preguntan por versión, sistema operativo, suscripción y origen del token, y
  advierten expresamente de no pegar ningún token.
- Las notas sobre la API ([docs/api-research.md](../api-research.md)) recogen
  el nuevo formato de respuesta, incluidos los campos que quedan sin usar y por
  qué.

## [0.5.0] – 2026-08-19

### Cambiado
- El destino de la instalación es ahora
  `%LOCALAPPDATA%\Programs\ClaudeUsageChecker` en lugar de
  `%USERPROFILE%\ClaudeUsageChecker`. Ese es el lugar que Windows prevé para
  aplicaciones sin permisos de administrador: allí están también VS Code y
  Signal. La raíz del perfil de usuario queda así libre, donde nadie espera
  programas junto a documentos y descargas.

  **Las instalaciones ya existentes no se mudan solas.** Siguen ejecutándose
  desde el lugar antiguo. Para mudarlas basta con abrir la configuración y
  guardar: con la casilla de inicio automático marcada, se copia al nuevo
  destino. El directorio antiguo puede borrarse después a mano.

## [0.4.2] – 2026-08-19

### Corregido
- Quien se saltaba la instalación en el primer arranque y más tarde solo marcaba
  «Iniciar con Windows» obtenía una entrada de inicio automático que apuntaba a
  la carpeta de descargas: sin valor en cuanto se limpiara esa carpeta. La
  casilla provoca ahora también la mudanza, con aviso previo de la ruta de
  destino y del reinicio.
- **Desmarcarla**, en cambio, deja la aplicación donde está. Solo se elimina la
  entrada de inicio automático; una vez instalada, sigue instalada.

## [0.4.1] – 2026-08-19

### Corregido
- Las carpetas de extracción de versiones anteriores se quedaban en el
  directorio temporal. Un archivo único comprimido no puede cargar sus
  bibliotecas nativas desde el paquete: el runtime de .NET las extrae a
  `%TEMP%\.net\ClaudeUsageChecker\<identificador>`, y como el identificador
  depende del contenido, cada versión obtenía su propia carpeta. Unos 16 MB por
  actualización, acumulándose sin límite. La aplicación ahora las limpia por sí
  misma.

### Documentación
- [SECURITY.md](../../SECURITY.md) enumera por completo qué guarda la aplicación
  y dónde, y qué quedaría tras una desinstalación.

## [0.4.0] – 2026-08-19

### Añadido
- **Instalación permanente.** Si la aplicación se ejecuta fuera de su destino,
  ofrece una sola vez en el primer arranque copiarse a
  `%USERPROFILE%\ClaudeUsageChecker`, configurar el inicio automático y
  reiniciarse desde allí. El motivo no es el orden: el inicio automático, el
  anclaje en el área de notificación y la actualización automática dependen
  todos de la ruta del ejecutable; si está en la carpeta de descargas, los tres
  se rompen en cuanto se limpie esa carpeta.
- El inicio automático se activa junto con la instalación y apunta a la ruta de
  destino, no al lugar de arranque. Desactivable en la configuración.

### Cambiado
- La ventana de detalles aparece centrada en la pantalla y lleva un borde fino
  del color del icono en lugar del marco del sistema.

### Añadido
- Una prueba comprueba que el borde recibe realmente su color. Un
  `DynamicResource` no resoluble quedaría, si no, vacío en silencio.

## [0.3.3] – 2026-08-19

### Cambiado
- El archivo publicado se llama igual en cada versión:
  `ClaudeUsageChecker.exe` en lugar de `ClaudeUsageChecker-0.3.2-win-x64.exe`.
  La actualización automática escribe la nueva versión en la ruta del archivo en
  ejecución: un nombre con versión afirmaría después una versión equivocada. Y
  Windows recuerda el anclaje en el área de notificación por ruta: si el nombre
  no fuera siempre el mismo, el icono acabaría de nuevo en el área de
  desbordamiento tras cada actualización.

## [0.3.2] – 2026-08-19

### Corregido
- Los botones del aviso de actualización sobresalían de la ventana. Uno al lado
  del otro necesitaban unos 420 píxeles, y la ventana mide 380: «Abrir página de
  la versión» solo se leía a medias. Ahora están uno debajo del otro.

### Añadido
- Pruebas que descubren desbordamientos en la ventana de detalles. Miden la
  colocación real tras un ciclo de diseño completo y comparan el borde derecho
  de cada elemento con el ancho de la ventana. Ni el tamaño deseado de los
  controles ni el de la ventana sirven para eso: Avalonia limita ambos al valor
  indicado, de modo que un desbordamiento no puede aparecer ahí.

## [0.3.1] – 2026-08-19

### Cambiado
- La interfaz escribe las diéresis como diéresis. Antes ponía «Auf
  Aktualisierungen pruefen», «Gueltig bis» o «Der Browser liess sich nicht
  oeffnen»: esas transliteraciones venían del desarrollo y no tenían nada que
  hacer en pantalla. 36 cadenas afectadas.
- El mensaje sobre la falta de permisos de acceso remite a la configuración
  también allí donde antes exigía guardar un token.

### Añadido
- Una prueba comprueba la codificación de caracteres desde el archivo fuente
  hasta la interfaz. Un fallo de codificación se nota así en la ejecución de las
  pruebas en lugar de en el usuario.

## [0.3.0] – 2026-08-19

La primera versión capaz de actualizarse a sí misma. A partir de aquí basta un
clic: la descarga manual desaparece.

### Corregido
- Las versiones se muestran con tres componentes. La cuarta procede de la
  versión de ensamblado y no dice nada: «La versión 0.2.0.0 está actualizada»
  solo confundía.

### Añadido
- **Actualización con un clic.** «Instalar ahora y reiniciar» descarga la nueva
  versión, comprueba su suma SHA-256 contra la publicada, sustituye el archivo
  en ejecución y reinicia. Un aviso que hay que atender a mano acaba, en la
  práctica, quedándose sin atender.
  - Si la suma de comprobación no coincide o falta, no se instala ni se ejecuta
    nada.
  - La dirección procede de la respuesta de GitHub sobre este repositorio; las
    direcciones sin HTTPS se descartan.
  - Solo tras un clic expreso, nunca en silencio en segundo plano.
  - La sustitución aprovecha que Windows permite renombrar un archivo en
    ejecución. Si la colocación falla, el renombrado se deshace.

### Cambiado
- «Mostrar detalles» se ha retirado del menú contextual. El clic izquierdo sobre
  el icono abre la ventana de detalles, y las cifras están de todos modos en las
  líneas de estado de encima: la entrada solo ofrecía el mismo camino una
  segunda vez.
- El aviso sobre la falta de permisos de acceso menciona primero el inicio de
  sesión propio. Antes decía «Inicia sesión en Claude Code», un consejo que
  nadie podía seguir en un equipo sin Claude Code.

## [0.2.0] – 2026-08-19

Primera publicación. Archivo único autónomo para Windows x64, 21 MB, sin
necesidad de runtime de .NET.

### Visualización

- Límite de sesión de 5 horas y límites semanales (total, Opus, Sonnet) desde
  `GET /api/oauth/usage`: valores autorizados, no estimaciones.
- Información sobre herramientas con el uso, la hora de reinicio y el tiempo
  restante. Si el reinicio cae en otro día, delante va el día de la semana; a
  partir de una semana, la fecha: una hora a secas sería ambigua para el límite
  semanal.
- Menú contextual con **todos** los límites indicados.
- Ventana de detalles con barras de progreso, horas de reinicio, créditos
  adicionales (`extra_usage`) y el origen del token realmente utilizado.
- Icono del área de notificación con código de colores: normal, tenso, crítico.

### Inicio de sesión

- **Inicio de sesión propio mediante OAuth con PKCE** (RFC 7636, S256): hace la
  aplicación independiente de una instalación de Claude Code en ejecución. El
  único permiso solicitado es `user:profile`; expresamente **no**
  `user:inference` ni `org:create_api_key`.
- Sin servidor web local: el código se pega a mano en lugar de recibirse por una
  redirección a `localhost`. Ningún puerto abierto.
- El token propio se renueva automáticamente. Con el token leído de Claude Code
  eso se omite a propósito: un token de refresco rotatorio invalidaría su
  sesión. Entradas separadas en el almacén seguro.
- Si el inicio de sesión propio caduca, se elimina y se informa, en lugar de
  recaer en silencio en Claude Code. Una simple perturbación (red, 5xx, límite
  de frecuencia) lo deja, en cambio, intacto.
- Cadena de reserva: inicio de sesión propio → token guardado → variable de
  entorno → Claude Code. Si la API rechaza una fuente, la consulta pasa a la
  siguiente.

### Funcionamiento

- Intervalo de consulta de al menos 180 segundos, espera exponencial tras los
  fallos, el `Retry-After` del servidor tiene prioridad.
- Una sola instancia por sesión iniciada.
- Inicio automático con Windows, desactivable.
- Comprobación de actualizaciones a través de las publicaciones de GitHub. No se
  descarga ni se ejecuta nada: solo se informa y, si se desea, se abre la página
  de la versión.
- Los errores en las acciones del área de notificación ya no terminan la
  aplicación, sino que acaban con su contexto en `crash.log`.

### Hallazgos que marcaron el diseño

- **`claude setup-token` no sirve para este fin.** Esos tokens son válidos y
  funcionan contra `/v1/messages`, pero no llevan `user:profile`. El punto final
  de uso los rechaza con HTTP 403. Esa era la suposición inicial del proyecto, y
  queda refutada.
- **El punto final de tokens está en `platform.claude.com`**, ya no en
  `console.anthropic.com`, donde responde HTTP 404.
- **El `User-Agent` es obligatorio.** Sin un user agent de Claude Code, el punto
  final de uso limita de forma permanente con HTTP 429.
- Compilado con recorte y compresión: 21 MB en lugar de 93 MB, arranque en 2,3
  en lugar de 7,2 segundos, 87 en lugar de 136 MB de memoria. El recorte gana en
  los tres ejes: el código eliminado tampoco hay que cargarlo ni compilarlo.

### Limitaciones conocidas

- El paquete **no está firmado**. Windows SmartScreen informa de un editor
  desconocido en el primer arranque.
- Cuánto sobrevive el inicio de sesión propio a una pausa larga se desconoce:
  Anthropic no documenta la vida útil del token de refresco.
- El proceso de inicio de sesión utiliza el ID de cliente OAuth públicamente
  conocido de Claude Code, ya que Anthropic no ofrece registrar aplicaciones
  propias. No es una vía oficialmente admitida; puede cambiar en cualquier
  momento.
- macOS está preparado, pero no implementado.
