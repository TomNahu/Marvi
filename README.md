# Marvi

<img src="art/Marvi-casita.png" width="100" alt="Marvi en su casita">

Marvi es un pitbull de escritorio para Windows 11. Acompaña tus sesiones de estudio, te ayuda a retomar después de una pausa y te deja practicar con tus propias preguntas. Funciona sin conexión. Esta publicación es el prototipo 0.3.0.

## Descargar

| Edición | Cómo usarla |
| --- | --- |
| [Instalador para Windows](https://github.com/TomNahu/Marvi/releases/download/v0.3.0/Marvi-0.3.0-Instalador.msi) | Abrí el MSI, elegí **Instalar** y buscá **Marvi** en Inicio. Se instala para tu usuario. Podés desinstalarlo desde Configuración → Aplicaciones. |
| [Portátil para Windows](https://github.com/TomNahu/Marvi/releases/download/v0.3.0/Marvi-0.3.0-Portable.zip) | Extraé todo el ZIP en una carpeta con permiso de escritura y abrí **Marvi.exe**. Para llevarte tu progreso a otra computadora, copiá toda la carpeta, incluidos **Marvi.portable** y **Datos**. |

Las dos ediciones usan el .NET Framework incluido en Windows 11. Una sola copia de Marvi puede estar abierta por escritorio. [Compatibilidad de .NET Framework en Windows](https://learn.microsoft.com/es-es/dotnet/framework/install/)

## Estudiá conmigo

Hacé clic derecho sobre Marvi y elegí **Estudiá conmigo**. También podés abrir el menú desde su casita en el área de notificaciones de Windows.

- **Pomodoro**. Podés empezar con 25 minutos de estudio, 5 de descanso y 5 de extensión. En **Ajustar tiempos del Pomodoro** podés elegir entre 1 y 1440 minutos para cada valor. Marvi recuerda tus ajustes. Si los editás con un tramo en curso, se aplican desde el siguiente.
- **Flowtime**. Empezá el cronómetro y decidí cuándo parar. La pausa sugerida es de un minuto por cada cinco estudiados, con un mínimo de uno. Podés cambiarla o elegir **Sin límite**.
- **Repasá con Marvi**. Guardá una pregunta y, si querés, la respuesta o la página de tu apunte. El día elegido, intentá responder antes de ver la pista. Podés programarla de nuevo para la fecha que prefieras. También podés guardar una pregunta al cerrar la sesión.
- **Explicáselo a Marvi**. Elegí un paso de un ejercicio o un concepto y explicalo con tus palabras mientras Marvi se sienta a escucharte. La actividad funciona sin micrófono. Si te queda una duda, podés convertirla en una pregunta de repaso.
- **Taller de ejercicios**. Elegí una materia y una consigna de Física, Química, Cálculo I o II, Probabilidad y Estadística, Matemática Superior o Análisis Numérico. Escribí tu desarrollo y consultá la solución cuando quieras. Podés guardar ejercicios propios, importar una guía PDF o vincular un ejercicio a su página. Marvi copia el PDF a `Guias` dentro de tus datos y lo abre en tu lector habitual.

En la ventana de estudio, antes de empezar o mientras estudiás, **Ctrl + Shift + F1** abre el repaso, **Ctrl + Shift + F2** abre la autoexplicación y **Ctrl + Shift + F3** abre el taller. Durante una pausa podés anotar por dónde querés seguir. La nota vuelve a aparecer al retomar.

## Datos y desarrollo

La edición instalada guarda el perfil, la sesión y el cuaderno de práctica en `%LOCALAPPDATA%\Marvi`. La portátil usa `Datos` junto al ejecutable. La desinstalación conserva los datos del estudiante.

| Carpeta | Contenido |
| --- | --- |
| `src/Marvi` | Aplicación WPF, lógica de sesiones y repaso, controles de escritorio y arte listo para usar. |
| `art` | Dibujos originales y herramientas para generar sprites y el ícono. |
| `tests` | Recorridos de la interfaz, compatibilidad de datos, prueba de paquetes y medición de recursos. |
| `packaging` | Definición del instalador por usuario y generación de las dos descargas. |

Desde la carpeta del proyecto podés compilar y probar con PowerShell.

```powershell
.\build.ps1
.\tests\Check.ps1
.\tests\Check.ps1 -Measure
```

`build.ps1` genera `dist\Marvi.exe` con las herramientas de Windows. También podés abrir `src\Marvi\Marvi.csproj` en Visual Studio. Para volver a generar el instalador y el ZIP, indicá la carpeta del compilador WiX 3.14.1. [Descarga oficial de WiX](https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm)

```powershell
.\packaging\Build-Release.ps1 -WixBin C:\Herramientas\wix3141
```

La [revisión técnica y las mediciones](docs/Revision-tecnica.md) detallan la evaluación para equipos con 4 GB de RAM.

Si probaste Marvi, podés contar en qué momento te ayudó, cuándo te interrumpió o qué intentaste hacer y no encontraste. [Enviar una sugerencia](https://github.com/TomNahu/Marvi/issues/new)
