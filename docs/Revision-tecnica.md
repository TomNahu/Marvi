# Revisión técnica del prototipo 0.2.0

## Compatibilidad con equipos modestos

Marvi es un candidato viable para Windows 11 con un procesador de entrada y 4 GB de RAM. La prueba se hizo en Windows 11 Pro, con un Intel Core i3-8100 y 16 GB instalados. Para el ensayo de las actividades, Windows restringió el proceso a **un procesador lógico y 256 MiB de memoria privada comprometida**. Las cinco actividades terminaron dentro de ese límite. Queda por confirmar la experiencia de un estudiante con 4 GB físicos, su navegador, un PDF abierto y la presión de memoria de todo el sistema.

| Actividad en orden de ejecución | Tiempo observado | RAM residente al final | Memoria privada al final | CPU media, % de un núcleo |
| --- | ---: | ---: | ---: | ---: |
| Mascota visible | 15 s | 75,8 MiB | 58,4 MiB | 0,42 % |
| Rodar | 2 s | 85,0 MiB | 68,0 MiB | 2,32 % |
| Pomodoro con el reloj visible | 12 s | 92,9 MiB | 70,8 MiB | 0,39 % |
| Ejercicio de matemática con Pomodoro en marcha | 10 s | 93,5 MiB | 72,4 MiB | 0,63 % |
| En la casita después de las actividades | 15 s | 94,0 MiB | 69,5 MiB | 0,00 % al redondear |

El ensayo abrió las ventanas WPF del ejecutable de distribución desde un proceso de prueba con su propio perfil. El reloj de estudio usó la hora real. El porcentaje de CPU divide el tiempo de procesador del proceso por el tiempo transcurrido. El valor de RAM residente incluye páginas compartidas de Windows y bibliotecas. La columna de memoria privada incluye reservas comprometidas para el proceso. Son medidas distintas.

Medimos también el **ejecutable portátil iniciado directamente desde el ZIP extraído**, desde otro proceso que muestreó una vez por segundo. Durante 15,2 segundos con Marvi visible, los máximos observados fueron **84,3 MiB residentes y 66,7 MiB privados**, con una media de **0,21 % de un núcleo**. En la casita se detiene la animación y se conservan los recursos ya cargados para el regreso. Las muestras cortas no establecen un máximo para todas las combinaciones de pantallas, pelajes, duración de uso y aplicaciones abiertas.

## Recorrido del código

| Parte revisada | Decisiones y cambios |
| --- | --- |
| Inicio, ventana, entrada y bandeja | `Program` establece una sola instancia por escritorio y el cierre al salir de la mascota. `PetWindow` monta las capas y el ícono de la bandeja. `PetActions` concentra clics, teclas y acciones del menú. `PetMenu` se posiciona en el monitor correspondiente y recibe el foco de teclado. |
| Animaciones y arte | `PetAnimation`, `MealAnimation` y `DeliveryAnimation` controlan fotogramas y duraciones. Se carga a demanda el pelaje elegido. Los accesorios comparten imágenes inmutables. Los scripts y generadores de `art` preparan el arte fuera de la aplicación, incluidos los dibujos base y las nueve resoluciones del ícono. |
| Perfiles, entregas y datos | El perfil, las sesiones, los ejercicios y las entregas usan escritura temporal y reemplazo del archivo terminado. El lector conserva la compatibilidad con el perfil previo. El texto personal se carga desde el perfil local. Los accesos a disco tienen mensajes o alternativas adecuados a la operación. |
| Sesiones y reloj | `StudySession` guarda plazos UTC y acumula solo el tiempo de cada tramo. `StudyUI` coordina postura y avisos. El reloj de pantalla se detiene mientras espera una decisión. Las pulsaciones de la nota se agrupan durante 450 ms y el cierre normal o un cambio de etapa guarda la última edición. |
| Herramientas de estudio | `PracticeBook` guarda preguntas, fechas y ejercicios. La mezcla equilibra los tipos y evita ejercicios repetidos y dos tipos iguales consecutivos. `PracticeWindow` mantiene la pista oculta hasta que el estudiante la pide. `PracticeUI` confirma el guardado antes de sustituir el cuaderno activo. Las ventanas de estudio comparten controles y tema mediante `StudySurface`. |
| Compilación y distribución | El ejecutable usa el .NET Framework que trae Windows 11. Pasó de **174,85 MiB** en la distribución anterior a **10,03 MiB**. El MSI y el ZIP contienen el mismo ejecutable. El MSI instala por usuario y crea el acceso de Inicio. El marcador `Marvi.portable` dirige la edición portátil a su propia carpeta de datos. |

Las preguntas y notas se guardan localmente. La autoexplicación usa una postura y consignas, sin capturar audio. El cuaderno serializa el conjunto de preguntas y ejercicios cuando se guarda un cambio. Si crecen mucho los cuadernos, conviene medir la latencia de guardado con datos representativos antes de cambiar el almacenamiento.

## Verificación

- Se probaron las ventanas de Windows con un perfil temporal, desde el menú de la mascota. Pomodoro cubrió validación de campos, preferencias nuevas, extensión, descanso, cambio durante una pausa, objetivo y totales. Se reabrió antes y después del vencimiento y se comprobó que el aviso pendiente aparecía una sola vez.
- En Flowtime se verificaron dos tramos, sugerencia proporcional, pausa libre, salida anticipada, recuperación de la nota y tiempos de estudio y descanso por separado.
- Se guardó una pregunta desde el resumen, se volvió a abrir Marvi, se mostró la pista a pedido y se reprogramó el repaso. Una duda de la autoexplicación reapareció entre las preguntas. Se completó una mezcla de tres tipos con un ejercicio propio.
- Se ensayaron **100 mezclas reproducibles** con dos y tres tipos, y se decodificaron los **85 fotogramas de cada uno de los siete pelajes** desde los recursos de la aplicación. Se verificó la lectura de sesiones anteriores y de una carta de prueba local.
- Se extrajo y abrió la edición portátil y se verificó su carpeta de datos. Se instaló y desinstaló el MSI en un usuario sin Marvi instalado. Se comprobó el destino del acceso de Inicio, la coincidencia del ejecutable por SHA-256 y la conservación de la carpeta del perfil.

Para repetir la medición, ejecutá `tests/Check.ps1 -Measure`. La prueba de los paquetes se realiza en una cuenta sin otra instancia ni instalación de Marvi.

```powershell
.\tests\Check-Packages.ps1 -Installer .\dist\Marvi-0.2.0-Instalador.msi -Portable .\dist\Marvi-0.2.0-Portable.zip
```
