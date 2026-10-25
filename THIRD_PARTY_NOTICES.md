# Avisos de terceros

TV Cleaner Pro incluye los componentes siguientes. Cada uno conserva su propia licencia.

| Componente | Archivo en el proyecto | Autor | Licencia |
|---|---|---|---|
| Android Debug Bridge (ADB) 36.0.0 | `codigo_fuente/recursos/adb/adb.exe` | Google (Android platform-tools) | Apache License 2.0 |
| AdbWinApi | `codigo_fuente/recursos/adb/AdbWinApi.dll` | Google (Android platform-tools) | Apache License 2.0 |
| AdbWinUsbApi | `codigo_fuente/recursos/adb/AdbWinUsbApi.dll` | Google (Android platform-tools) | Apache License 2.0 |
| Lista de apps `uad_lists.json.gz` | `codigo_fuente/recursos/uad_lists.json.gz` | Universal Android Debloater (Universal-Debloater-Alliance) | GPL-3.0 |

La lista `uad_lists.json.gz` se puede actualizar con `ACTUALIZAR_LISTA_UAD.bat`, que la descarga de
`https://raw.githubusercontent.com/Universal-Debloater-Alliance/universal-android-debloater-next-generation/main/resources/assets/uad_lists.json`.

El texto completo de la licencia GPL-3.0 está en [`LICENSE`](LICENSE). El texto de la Apache License 2.0 está en
[`LICENSES/Apache-2.0.txt`](LICENSES/Apache-2.0.txt).

El código propio de TV Cleaner Pro usa únicamente bibliotecas que trae .NET Framework 4.8 (Windows).
