# TV Cleaner Pro 3.0 — código fuente

Programa para limpiar, optimizar y administrar **Android TV, Google TV, Fire TV, NVIDIA Shield y TV Box Android**
por WiFi, cable de red o USB. Escrito en **C# (.NET Framework 4.8, Windows Forms)**: un `.exe` nativo y pequeño
que no se descomprime ni lleva un intérprete escondido, así que los antivirus no lo bloquean como al de Python.

## Ejecutar desde el código

- **`EJECUTAR_DESDE_CODIGO.bat`**: compila lo que haya en `src\` y abre el programa al instante (queda en `bin\`).
  Si hay un error de compilación, muestra archivo y línea, y conserva la versión anterior.
- El `.exe` ya compilado está en la sección **Releases** del repositorio (no se guarda dentro del código).

## Compilar (sin instalar nada)

Doble clic en **`COMPILAR.bat`**. Usa `csc.exe`, el compilador de C# que ya trae Windows 10/11, y deja el
programa en `..\TV_Cleaner_Pro.exe`. La versión anterior queda como `TV_Cleaner_Pro_anterior.exe`.

Si compilas en otra máquina (por ejemplo, con Mono en Linux), hazlo contra las referencias de **.NET Framework 4.8**
(`-nostdlib` + `4.8-api`). Si no, el programa puede usar funciones que Windows no tiene y fallar con `MissingMethodException`.

El código usa solo C# 5 para que ese compilador lo entienda. **No uses** `$"texto {x}"`, `?.`, `nameof`,
`=>` en propiedades/métodos, ni `out var`. Usa `string.Format`, `if (x != null)` y métodos con llaves.

## Probar sin arriesgar el TV

**`EJECUTAR_PRUEBAS.bat`** compila y ejecuta 88 pruebas contra un **TV simulado** (`pruebas\fakeadb.py`, necesita Python).
El simulador puede hacerse pasar por Google TV, Fire TV (`FAKEADB_DEVICE=firetv`) o TV Box USB (`FAKEADB_DEVICE=tvbox`).
`pruebas\PruebaGUI.cs` maneja la ventana real por código (se usó en Linux con Mono; opcional).

## Dónde está cada cosa (`src\`)

| Archivo | Qué hace |
|---|---|
| `TablasPaquetes.cs` | **Base de datos de paquetes**: aquí se agregan o corrigen apps (categoría, nivel, descripción) y los patrones de seguridad. |
| `BaseDatos.cs` | Reglas de clasificación (orden: protección automática → lista propia → patrones críticos → prefijos → lista UAD → apps de usuario → sistema desconocido). |
| `Adb.cs` | Todas las órdenes a ADB. Cada nombre de paquete, ajuste o tecla se **valida** antes de enviarse (no se puede inyectar texto al shell del TV). |
| `Motor.cs` | Lógica: detectar tipo de dispositivo, escanear, aplicar, restaurar, verificación de seguridad y cambio de launcher. |
| `Historial.cs` | Guarda cada cambio por dispositivo (JSON) y genera el `_RESTAURAR.bat` de emergencia. |
| `Instalador.cs` | Instalación de APK, APK divididos, `.apks`/`.xapk`/`.apkm` (con OBB) y explicación de errores de Android. |
| `EscanerRed.cs` | Escáner de red: todas las interfaces, puertos de TV, mDNS, SSDP/UPnP, ARP, DNS. |
| `VentanaPrincipal.cs` | Ventana, conexión (IP o USB), tareas en segundo plano y registro. |
| `Pestana*.cs` | Una pestaña por archivo: Limpieza, Mis apps, Red, Optimizar/Launcher/Control remoto, Respaldos/Registro. |
| `Tema.cs` / `Dialogos.cs` | Colores, fuentes, botones y ventanas de diálogo. |
| `Program.cs` | Arranque y captura de errores inesperados (se guardan en `Documentos\TV Cleaner Pro\registros`). |

`recursos\` contiene el ícono, el manifiesto, la lista comunitaria `uad_lists.json.gz` y ADB oficial de Google
(`adb.exe` 36.0.0 con sus DLL). Todo eso va **dentro** del `.exe`.

## Agregar un paquete nuevo

En `TablasPaquetes.cs`, dentro de `CargarCurados()`:

```csharp
C("com.marca.appmolesta", "PUBLICIDAD", 1, "Lo que hace, en español.");
```

Niveles: `0` Bloqueado (nunca se toca) · `1` Seguro (entra en "Marcar recomendadas") · `2` Precaución · `3` Experto.
Si dudas, usa 2 o 3. Para que se vea un nombre amable: `Nombres["com.marca.appmolesta"] = "App Molesta";`

## Actualizar la lista comunitaria

`ACTUALIZAR_LISTA_UAD.bat` descarga la lista más reciente de Universal Android Debloater (GPL-3.0). Después,
compila de nuevo, o copia `recursos\uad_lists.json.gz` junto al `.exe`: el programa usa ese archivo si lo encuentra.
Tus reglas de `TablasPaquetes.cs` siempre tienen prioridad.

## Reglas de seguridad que no conviene quitar

- Por defecto solo se **desactiva** (`pm disable-user --user 0`), y se restaura con `pm enable`.
- "Eliminar" en apps del sistema usa `pm uninstall -k --user 0`, que se restaura con `install-existing`.
  A las apps instaladas se les respalda el APK antes de desinstalarlas.
- Se protegen automáticamente el **launcher activo**, el **teclado activo**, la accesibilidad activa y, en Fire TV, la Appstore.
- Después de aplicar cambios se verifica que el dispositivo responda y tenga pantalla de inicio. Si no la tiene, se reactivan los launchers.
- Los ajustes guardan su valor **original** la primera vez que se cambian.

Datos del usuario: `Documentos\TV Cleaner Pro\` (historial por TV, `.bat` de restauración, APK respaldados,
capturas y registros). ADB se copia una vez a `%LOCALAPPDATA%\TVCleanerPro\adb\`.
