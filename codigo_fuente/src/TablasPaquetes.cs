// ARCHIVO DE DATOS: aquí se agregan o corrigen paquetes conocidos.
// Nivel: 0 = Bloqueado (nunca se toca), 1 = Seguro, 2 = Precaución, 3 = Experto.
// Categorías válidas: TELEMETRÍA, PUBLICIDAD, STREAMING, JUEGOS, GOOGLE, FABRICANTE, BLOATWARE, APPS INSTALADAS, SISTEMA
using System.Collections.Generic;

namespace TVCleanerPro
{
    static partial class BaseDatos
    {
        // paquete -> (categoría, nivel, descripción)
        static void CargarCurados()
        {
            C("android", "SISTEMA", 0, "El sistema operativo Android en sí.");
            C("com.android.systemui", "SISTEMA", 0, "Interfaz del sistema (barras, menús, volumen).");
            C("com.android.settings", "SISTEMA", 0, "Ajustes del sistema.");
            C("com.android.tv.settings", "SISTEMA", 0, "Ajustes del TV. Sin esto no podrás configurar nada.");
            C("com.google.android.gms", "SISTEMA", 0, "Servicios de Google Play. Casi todas las apps dependen de esto.");
            C("com.google.android.gsf", "SISTEMA", 0, "Google Services Framework, base de los servicios de Google.");
            C("com.android.vending", "SISTEMA", 0, "Google Play Store. Necesaria para instalar y actualizar apps.");
            C("com.android.location.fused", "SISTEMA", 0, "Ubicación del sistema. Desactivarla puede dejar el TV reiniciándose sin fin.");
            C("com.google.android.tv.remote.service", "SISTEMA", 0, "Servicio del control remoto y emparejamiento con el celular.");
            C("com.google.android.tungsten.setupwraith", "SISTEMA", 0, "Asistente de inicio del TV. En Sony y otros, quitarlo rompe el control de volumen.");
            C("com.google.android.tv.axel", "SISTEMA", 0, "Servicio infrarrojo del control (volumen del TV/barra de sonido).");
            C("com.google.android.ext.services", "SISTEMA", 0, "Módulo base del sistema. Desactivarlo puede causar bootloop.");
            C("com.google.android.overlay.modules.ext.services", "SISTEMA", 0, "Dependencia de ExtServices. Puede causar bootloop.");
            C("com.google.android.webview", "SISTEMA", 0, "Motor web usado por muchas apps (YouTube, Netflix, inicio de sesión).");
            C("com.android.webview", "SISTEMA", 0, "Motor web del sistema.");
            C("com.android.providers.tv", "SISTEMA", 0, "Guía de canales y entradas del TV.");
            C("com.android.providers.settings", "SISTEMA", 0, "Almacena toda la configuración del sistema.");
            C("com.android.providers.media", "SISTEMA", 0, "Biblioteca de archivos multimedia.");
            C("com.android.providers.downloads", "SISTEMA", 0, "Administrador de descargas (Play Store lo usa).");
            C("com.android.packageinstaller", "SISTEMA", 0, "Instalador de apps.");
            C("com.google.android.packageinstaller", "SISTEMA", 0, "Instalador de apps.");
            C("com.android.permissioncontroller", "SISTEMA", 0, "Control de permisos.");
            C("com.google.android.permissioncontroller", "SISTEMA", 0, "Control de permisos.");
            C("com.android.bluetooth", "SISTEMA", 0, "Bluetooth: control remoto, audífonos y mandos.");
            C("com.android.shell", "SISTEMA", 0, "Shell de ADB. Sin esto TV Cleaner no podría conectarse.");
            C("com.android.inputmethod.latin", "SISTEMA", 0, "Teclado en pantalla.");
            C("com.google.android.inputmethod.latin", "SISTEMA", 0, "Teclado Gboard.");
            C("com.google.android.leanback.ime", "SISTEMA", 0, "Teclado para Android TV.");
            C("com.android.externalstorage", "SISTEMA", 0, "Acceso a memorias USB.");
            C("com.android.keychain", "SISTEMA", 0, "Certificados de seguridad.");
            C("com.android.certinstaller", "SISTEMA", 0, "Instalador de certificados.");
            C("com.sony.dtv.tvx", "SISTEMA", 0, "Sony: maneja las entradas. Sin esto el HDMI deja de funcionar.");
            C("com.tcl.suspension", "SISTEMA", 0, "TCL: menú de Entradas/Fuente. Sin esto no cambias de HDMI.");
            C("com.tcl.ttvs", "SISTEMA", 0, "TCL: núcleo del sistema y actualizaciones.");
            C("com.tcl.tv", "SISTEMA", 0, "TCL: entrada de HDMI y antena.");
            C("com.tcl.tvinput", "SISTEMA", 0, "TCL: entradas de TV.");
            C("com.tcl.providers.config", "SISTEMA", 0, "TCL: configuración. Quitarlo desconecta el WiFi.");
            C("com.tcl.initsetup", "SISTEMA", 0, "TCL: configuración inicial. Quitarlo desconecta el WiFi.");
            C("com.tcl.tcl_bt_rcu_service", "SISTEMA", 0, "TCL: control remoto Bluetooth.");
            C("com.tcl.autopair", "SISTEMA", 0, "TCL: emparejamiento del control remoto.");
            C("com.xiaomi.mitv.res", "SISTEMA", 0, "Xiaomi: recursos del sistema Mi TV.");
            C("com.google.android.feedback", "TELEMETRÍA", 1, "Envía reportes de errores y de uso a Google.");
            C("com.google.android.tv.bugreportsender", "TELEMETRÍA", 1, "Envía reportes de fallos (bug reports) a Google.");
            C("com.google.android.apps.tv.netoscope", "TELEMETRÍA", 1, "Diagnóstico de conexión que reporta datos. No es necesario.");
            C("com.google.android.onetimeinitializer", "TELEMETRÍA", 1, "Inicialización de primer arranque; ya no se usa.");
            C("tv.alphonso.alphonso_eula", "TELEMETRÍA", 1, "Alphonso ACR: analiza lo que ves en pantalla para publicidad dirigida.");
            C("com.miui.tv.analytics", "TELEMETRÍA", 1, "Xiaomi: analítica y envío de datos de uso.");
            C("com.google.android.gms.policy_sidecar_aps", "TELEMETRÍA", 2, "Recolecta datos de red para Google. Raramente necesario.");
            C("com.google.android.backuptransport", "TELEMETRÍA", 2, "Copia datos de apps a servidores de Google. Perderás esos respaldos.");
            C("com.google.android.partnersetup", "TELEMETRÍA", 2, "Configuración de socios de Google (búsqueda y anuncios de socios).");
            C("com.sony.dtv.promos", "PUBLICIDAD", 1, "Sony: promociones y ofertas.");
            C("com.sony.dtv.sonyselect", "PUBLICIDAD", 1, "Sony Select: anuncios de apps y suscripciones.");
            C("com.sony.dtv.notificationcenter", "PUBLICIDAD", 1, "Sony: notificaciones promocionales de su ecosistema.");
            C("com.mitv.tvhome.michannel", "PUBLICIDAD", 1, "Xiaomi Mi Channel: contenido promocional.");
            C("com.xiaomi.android.tvsetup.partnercustomizer", "PUBLICIDAD", 1, "Xiaomi: instala apps de socios en el arranque.");
            C("com.android.providers.partnerbookmarks", "PUBLICIDAD", 1, "Favoritos de socios comerciales de Google.");
            C("com.google.android.apps.tv.dreamx", "PUBLICIDAD", 2, "Modo ambiente de Google TV (protector con fotos y sugerencias). Si lo quitas usa otro protector.");
            C("com.mitv.videoplayer", "PUBLICIDAD", 2, "Xiaomi: reproductor de video con anuncios.");
            C("com.google.android.apps.tachyon", "GOOGLE", 1, "Google Duo/Meet: videollamadas, inútil en un TV sin cámara.");
            C("com.google.android.music", "GOOGLE", 1, "Google Play Música: el servicio ya no existe.");
            C("com.google.android.play.games", "GOOGLE", 1, "Google Play Juegos.");
            C("com.google.android.apps.photos", "GOOGLE", 1, "Google Fotos.");
            C("com.google.android.videos", "GOOGLE", 2, "Google TV / Play Películas. En Google TV alimenta la pestaña de películas compradas.");
            C("com.google.android.katniss", "GOOGLE", 2, "Asistente y búsqueda por voz. Sin esto el botón de micrófono no funciona.");
            C("com.google.android.googlequicksearchbox", "GOOGLE", 2, "Búsqueda de Google / Asistente. Afecta la búsqueda por voz.");
            C("com.google.android.tts", "GOOGLE", 2, "Voz del sistema (texto a voz). Solo necesaria para accesibilidad.");
            C("com.google.android.marvin.talkback", "GOOGLE", 2, "Accesibilidad (lector de pantalla).");
            C("com.google.android.apps.mediashell", "GOOGLE", 2, "Chromecast integrado. Sin esto no podrás transmitir desde el celular.");
            C("com.google.android.gms.supervision", "GOOGLE", 2, "Control parental Family Link.");
            C("com.google.android.apps.youtube.kids", "GOOGLE", 2, "YouTube Kids.");
            C("com.google.android.youtube.tvmusic", "STREAMING", 2, "YouTube Music.");
            C("com.google.android.youtube.tv", "STREAMING", 2, "YouTube. Desactívalo solo si no lo usas.");
            C("com.google.android.youtube.tvunplugged", "STREAMING", 1, "YouTube TV (solo Estados Unidos).");
            C("com.google.android.apps.tv.launcherx", "SISTEMA", 3, "Pantalla de inicio de Google TV (con anuncios). Cámbiala desde la pestaña Launcher.");
            C("com.google.android.tvlauncher", "SISTEMA", 3, "Pantalla de inicio de Android TV. Cámbiala desde la pestaña Launcher.");
            C("com.google.android.leanbacklauncher", "SISTEMA", 3, "Pantalla de inicio clásica. Cámbiala desde la pestaña Launcher.");
            C("com.google.android.tvrecommendations", "SISTEMA", 3, "Servicios base de Android TV (recomendaciones). Puede afectar el inicio.");
            C("com.google.android.setupwizard", "SISTEMA", 3, "Asistente de configuración inicial.");
            C("com.google.android.apps.turbo", "SISTEMA", 3, "Salud del dispositivo. Puede romper ajustes de energía.");
            C("com.google.android.configupdater", "SISTEMA", 3, "Actualizaciones de datos del sistema (certificados, zonas horarias).");
            C("com.netflix.ninja", "STREAMING", 2, "Netflix. Desactívalo solo si no tienes suscripción.");
            C("com.amazon.amazonvideo.livingroom", "STREAMING", 2, "Prime Video. Desactívalo solo si no tienes suscripción.");
            C("com.disney.disneyplus", "STREAMING", 2, "Disney+. Desactívalo solo si no tienes suscripción.");
            C("com.wbd.stream", "STREAMING", 2, "Max (HBO). Desactívalo solo si no tienes suscripción.");
            C("com.apple.atve.androidtv.appletv", "STREAMING", 2, "Apple TV.");
            C("com.apple.atve.sony.appletv", "STREAMING", 2, "Apple TV (versión Sony).");
            C("com.cbs.ott", "STREAMING", 2, "Paramount+.");
            C("com.spotify.tv.android", "STREAMING", 2, "Spotify.");
            C("tv.pluto.android", "STREAMING", 2, "Pluto TV: canales gratis con anuncios.");
            C("com.tubitv", "STREAMING", 2, "Tubi: películas gratis con anuncios.");
            C("com.peacocktv.peacockandroid", "STREAMING", 1, "Peacock: solo disponible en Estados Unidos.");
            C("com.nousguide.android.rbtv", "STREAMING", 1, "Red Bull TV.");
            C("com.gameloft.android.ANMP.GloftA9HM", "JUEGOS", 1, "Asphalt 9 preinstalado.");
            C("com.kiloo.subwaysurf", "JUEGOS", 1, "Subway Surfers.");
            C("com.halfbrick.fruitninjafree", "JUEGOS", 1, "Fruit Ninja.");
            C("com.sony.dtv.smarthelp", "FABRICANTE", 1, "Sony: manual de ayuda. Quitarlo no tiene efecto.");
            C("com.sony.dtv.recapp", "FABRICANTE", 1, "Sony: visor de recordatorios.");
            C("com.sony.dtv.osat.music", "FABRICANTE", 1, "Sony: reproductor de música local.");
            C("com.sony.dtv.livingfit", "FABRICANTE", 1, "Sony: modo decorativo con imágenes.");
            C("com.sony.dtv.seeds.iot", "FABRICANTE", 2, "Sony: control por Alexa / altavoz inteligente.");
            C("com.sony.dtv.timers", "FABRICANTE", 2, "Sony: reloj y temporizadores (algunos launchers lo usan).");
            C("com.sony.dtv.ecodashboard", "FABRICANTE", 3, "Sony: ajustes ecológicos. Quitarlo rompe opciones en Ajustes.");
            C("com.sony.dtv.calibrationmonitor", "FABRICANTE", 2, "Sony: calibración de pantalla (Calman).");
            C("com.mitv.download.service", "FABRICANTE", 1, "Xiaomi: servicio de descargas propio.");
            C("com.mitv.tvhome.atv", "FABRICANTE", 3, "Xiaomi PatchWall (inicio). Cámbialo desde la pestaña Launcher.");
            C("com.mitv.milinkservice", "FABRICANTE", 2, "Xiaomi: duplicar pantalla desde el celular.");
            C("com.android.galaxy4", "BLOATWARE", 1, "Fondo animado, no se usa en TV.");
            C("com.android.magicsmoke", "BLOATWARE", 1, "Fondo animado, no se usa en TV.");
            C("com.android.noisefield", "BLOATWARE", 1, "Fondo animado, no se usa en TV.");
            C("com.android.phasebeam", "BLOATWARE", 1, "Fondo animado, no se usa en TV.");
            C("com.android.email", "BLOATWARE", 1, "Correo, no se usa en TV.");
            C("com.android.dreams.basic", "BLOATWARE", 2, "Protector de pantalla básico (colores).");
            C("com.android.wallpaper.livepicker", "BLOATWARE", 2, "Selector de fondos animados.");
            C("com.android.calculator2", "BLOATWARE", 2, "Calculadora.");
            C("com.android.soundrecorder", "BLOATWARE", 2, "Grabadora de sonido.");
            C("com.android.stk", "BLOATWARE", 2, "Herramientas de tarjeta SIM (no aplica a un TV).");
            C("com.android.bookmarkprovider", "BLOATWARE", 2, "Favoritos de navegador (compatibilidad).");
            C("com.android.printspooler", "BLOATWARE", 3, "Cola de impresión.");
            C("com.android.htmlviewer", "BLOATWARE", 3, "Visor HTML usado por algunas apps.");

            // ── Fire TV, TV Box y NVIDIA Shield ──
            C("com.amazon.tv.launcher", "SISTEMA", 3, "Fire TV: pantalla de inicio de Amazon. En Fire OS 7.6+ no se puede reemplazar; no la quites.");
            C("com.amazon.venezia", "SISTEMA", 0, "Fire TV: Amazon Appstore. Maneja el menú 'Tus apps'; quitarla lo rompe.");
            C("com.amazon.tv.oobe", "SISTEMA", 0, "Fire TV: configuración inicial del dispositivo.");
            C("com.amazon.device.software.ota", "SISTEMA", 3, "Fire TV: actualizaciones del sistema. Desactivarlo bloquea parches de seguridad.");
            C("com.amazon.tv.acr", "TELEMETRÍA", 1, "Fire TV: ACR, analiza lo que ves en pantalla para estadísticas y publicidad.");
            C("com.amazon.dp.logger", "TELEMETRÍA", 1, "Fire TV: registros de diagnóstico enviados a Amazon.");
            C("com.amazon.wirelessmetrics.service", "TELEMETRÍA", 1, "Fire TV: métricas de red y rendimiento enviadas a Amazon.");
            C("com.amazon.device.telemetry.emitter", "TELEMETRÍA", 1, "Fire TV: envío de telemetría.");
            C("com.amazon.perfcollection", "TELEMETRÍA", 1, "Fire TV: recolección de datos de rendimiento.");
            C("com.amazon.hybridadidservice", "PUBLICIDAD", 1, "Fire TV: ID de publicidad y motor de anuncios personalizados.");
            C("com.amazon.shoptv.client", "PUBLICIDAD", 1, "Fire TV: compras de Amazon dentro del TV.");
            C("com.amazon.tv.democontent.provider", "PUBLICIDAD", 1, "Fire TV: contenido de demostración de tienda.");
            C("com.amazon.minitv.android.app", "PUBLICIDAD", 1, "Fire TV: Amazon miniTV (contenido con anuncios).");
            C("com.amazon.media.recommendations", "PUBLICIDAD", 2, "Fire TV: recomendaciones y banners en el inicio. Quitarlo puede dejar filas vacías.");
            C("com.amazon.sneakpeek", "PUBLICIDAD", 2, "Fire TV: vistas previas que se reproducen solas en el inicio.");
            C("com.amazon.aa", "PUBLICIDAD", 1, "Amazon Assistant: recomendaciones de compras.");
            C("com.amazon.bueller.photos", "FABRICANTE", 1, "Fire TV: sincronización de Amazon Photos.");
            C("com.amazon.bueller.music", "FABRICANTE", 1, "Fire TV: sincronización de Amazon Music.");
            C("com.amazon.ods.kindleconnect", "FABRICANTE", 1, "Fire TV: integración con Kindle.");
            C("com.amazon.storm.lightning.tutorial", "FABRICANTE", 1, "Fire TV: tutorial de bienvenida.");
            C("com.amazon.gamehub", "FABRICANTE", 1, "Fire TV: centro de juegos de Amazon.");
            C("com.amazon.smarthomemapviewapp", "FABRICANTE", 1, "Fire TV: mapa de hogar inteligente.");
            C("com.amazon.tv.matter", "FABRICANTE", 2, "Fire TV: vincular dispositivos de hogar inteligente (Matter).");
            C("com.amazon.tv.alexanotifications", "FABRICANTE", 2, "Fire TV: avisos y notificaciones de Alexa.");
            C("com.amazon.tahoe", "FABRICANTE", 2, "Fire TV: Amazon Kids y control parental.");
            C("com.amazon.whisperplay.contracts", "FABRICANTE", 2, "Fire TV: transmisión desde el celular (WhisperPlay).");
            C("com.amazon.tv.easyupgrade", "FABRICANTE", 2, "Fire TV: avisos de actualización de dispositivo.");
            C("com.amazon.audiohome", "FABRICANTE", 2, "Fire TV: audio multi-habitación.");
            C("com.amazon.kso.blackbird", "FABRICANTE", 2, "Fire TV: componente de Amazon sin documentación pública.");
            C("com.amazon.hedwig", "FABRICANTE", 2, "Fire TV: servicio de notificaciones de Amazon.");
            C("com.amazon.spiderpork", "FABRICANTE", 2, "Fire TV: revisa apps instaladas por fuera de la tienda y puede bloquearlas.");
            C("com.droidlogic", "SISTEMA", 0, "TV Box Amlogic: emparejamiento Bluetooth y servicios base.");
            C("com.droidlogic.tvinput", "SISTEMA", 0, "TV Box Amlogic: entradas de video.");
            C("com.softwinner.explore", "BLOATWARE", 1, "TV Box Allwinner: explorador de archivos básico.");
            C("com.softwinner.dragonatt", "BLOATWARE", 1, "TV Box Allwinner: herramienta de pruebas de fábrica.");
            C("com.softwinner.service", "SISTEMA", 3, "TV Box Allwinner: posiblemente actualizador del firmware.");
            C("android.rockchip.update.service", "SISTEMA", 3, "TV Box Rockchip: actualizaciones del firmware.");
            C("com.nvidia.ota", "SISTEMA", 3, "NVIDIA Shield: actualizaciones del sistema.");
            C("com.nvidia.tegrazone3", "JUEGOS", 1, "NVIDIA Games / TegraZone.");
            C("com.nvidia.feedback", "TELEMETRÍA", 1, "NVIDIA Shield: envío de comentarios y reportes.");
        }

        // Nombres legibles que se muestran en la lista
        static void CargarNombres()
        {
            Nombres["com.google.android.gms"] = "Servicios de Google Play";
            Nombres["com.android.vending"] = "Google Play Store";
            Nombres["com.google.android.gsf"] = "Google Services Framework";
            Nombres["com.android.systemui"] = "Interfaz del sistema";
            Nombres["com.android.settings"] = "Ajustes";
            Nombres["com.android.tv.settings"] = "Ajustes del TV";
            Nombres["com.google.android.feedback"] = "Google Feedback";
            Nombres["com.google.android.tv.bugreportsender"] = "Envío de reportes de fallos";
            Nombres["com.google.android.apps.tv.netoscope"] = "Diagnóstico de conexión";
            Nombres["com.google.android.katniss"] = "Asistente / búsqueda por voz";
            Nombres["com.google.android.apps.tv.launcherx"] = "Inicio de Google TV";
            Nombres["com.google.android.tvlauncher"] = "Inicio de Android TV";
            Nombres["com.google.android.leanbacklauncher"] = "Inicio clásico (Leanback)";
            Nombres["com.google.android.apps.tv.dreamx"] = "Modo ambiente";
            Nombres["com.google.android.youtube.tv"] = "YouTube";
            Nombres["com.google.android.youtube.tvmusic"] = "YouTube Music";
            Nombres["com.google.android.youtube.tvunplugged"] = "YouTube TV";
            Nombres["com.google.android.videos"] = "Google TV / Play Películas";
            Nombres["com.google.android.apps.mediashell"] = "Chromecast integrado";
            Nombres["com.google.android.play.games"] = "Google Play Juegos";
            Nombres["com.google.android.music"] = "Google Play Música";
            Nombres["com.google.android.apps.tachyon"] = "Google Duo / Meet";
            Nombres["com.google.android.apps.photos"] = "Google Fotos";
            Nombres["com.google.android.tts"] = "Voz de Google (TTS)";
            Nombres["com.google.android.marvin.talkback"] = "Accesibilidad TalkBack";
            Nombres["com.google.android.webview"] = "WebView del sistema";
            Nombres["com.netflix.ninja"] = "Netflix";
            Nombres["com.amazon.amazonvideo.livingroom"] = "Prime Video";
            Nombres["com.disney.disneyplus"] = "Disney+";
            Nombres["com.wbd.stream"] = "Max";
            Nombres["com.apple.atve.androidtv.appletv"] = "Apple TV";
            Nombres["com.cbs.ott"] = "Paramount+";
            Nombres["com.spotify.tv.android"] = "Spotify";
            Nombres["tv.pluto.android"] = "Pluto TV";
            Nombres["com.tubitv"] = "Tubi";
            Nombres["com.peacocktv.peacockandroid"] = "Peacock";
            Nombres["me.efesser.flauncher"] = "FLauncher (sin anuncios)";
            Nombres["com.android.location.fused"] = "Ubicación del sistema";
            Nombres["com.android.bluetooth"] = "Bluetooth";
            Nombres["tv.alphonso.alphonso_eula"] = "Alphonso (rastreo ACR)";
            Nombres["com.sony.dtv.promos"] = "Sony Promociones";
            Nombres["com.sony.dtv.sonyselect"] = "Sony Select";
            Nombres["com.mitv.tvhome.atv"] = "PatchWall (Xiaomi)";
            Nombres["com.google.android.onetimeinitializer"] = "Inicializador de Google";
            Nombres["com.google.android.partnersetup"] = "Configuración de socios Google";
            Nombres["com.google.android.backuptransport"] = "Respaldo en la nube de Google";
            Nombres["com.android.providers.partnerbookmarks"] = "Favoritos de socios";
            Nombres["com.google.android.gms.policy_sidecar_aps"] = "Datos de red para Google";
            Nombres["com.google.android.gms.supervision"] = "Family Link";
            Nombres["com.google.android.configupdater"] = "Actualizador de configuración";
            Nombres["com.google.android.tvrecommendations"] = "Servicios de Android TV";
            Nombres["com.google.android.setupwizard"] = "Asistente de configuración";
            Nombres["com.google.android.apps.turbo"] = "Salud del dispositivo";
            Nombres["com.google.android.tungsten.setupwraith"] = "Asistente de inicio del TV";
            Nombres["com.google.android.tv.axel"] = "Infrarrojo del control";
            Nombres["com.google.android.ext.services"] = "ExtServices (núcleo)";
            Nombres["com.google.android.leanback.ime"] = "Teclado de Android TV";
            Nombres["com.google.android.tv.remote.service"] = "Servicio del control remoto";
            Nombres["com.google.android.googlequicksearchbox"] = "Búsqueda de Google";
            Nombres["com.google.android.apps.youtube.kids"] = "YouTube Kids";
            Nombres["com.google.android.inputmethod.latin"] = "Teclado Gboard";
            Nombres["com.android.email"] = "Correo";
            Nombres["com.android.galaxy4"] = "Fondo animado Galaxy";
            Nombres["com.android.magicsmoke"] = "Fondo animado Humo";
            Nombres["com.android.noisefield"] = "Fondo animado Partículas";
            Nombres["com.android.phasebeam"] = "Fondo animado Rayos";
            Nombres["com.android.dreams.basic"] = "Protector de pantalla básico";
            Nombres["com.android.wallpaper.livepicker"] = "Selector de fondos animados";
            Nombres["com.android.printspooler"] = "Cola de impresión";
            Nombres["com.android.htmlviewer"] = "Visor HTML";
            Nombres["com.android.calculator2"] = "Calculadora";
            Nombres["com.android.soundrecorder"] = "Grabadora";
            Nombres["com.android.stk"] = "Herramientas SIM";
            Nombres["com.android.bookmarkprovider"] = "Favoritos del navegador";
            Nombres["com.android.packageinstaller"] = "Instalador de apps";
            Nombres["com.google.android.packageinstaller"] = "Instalador de apps";
            Nombres["com.android.permissioncontroller"] = "Control de permisos";
            Nombres["com.google.android.permissioncontroller"] = "Control de permisos";
            Nombres["com.android.externalstorage"] = "Memorias USB";
            Nombres["com.android.keychain"] = "Certificados";
            Nombres["com.android.certinstaller"] = "Instalador de certificados";
            Nombres["com.android.providers.downloads"] = "Descargas";
            Nombres["com.android.providers.media"] = "Biblioteca multimedia";
            Nombres["com.android.providers.settings"] = "Base de datos de ajustes";
            Nombres["com.android.providers.tv"] = "Canales y entradas de TV";
            Nombres["com.android.shell"] = "Shell de ADB";
            Nombres["com.android.webview"] = "WebView";
            Nombres["com.android.inputmethod.latin"] = "Teclado";
            Nombres["com.sony.dtv.smarthelp"] = "Sony Ayuda";
            Nombres["com.sony.dtv.recapp"] = "Sony Recordatorios";
            Nombres["com.sony.dtv.osat.music"] = "Sony Música";
            Nombres["com.sony.dtv.livingfit"] = "Sony Modo decorativo";
            Nombres["com.sony.dtv.seeds.iot"] = "Sony Alexa";
            Nombres["com.sony.dtv.timers"] = "Sony Reloj";
            Nombres["com.sony.dtv.ecodashboard"] = "Sony Eco";
            Nombres["com.sony.dtv.calibrationmonitor"] = "Sony Calibración";
            Nombres["com.sony.dtv.tvx"] = "Sony Entradas HDMI";
            Nombres["com.sony.dtv.notificationcenter"] = "Sony Notificaciones";
            Nombres["com.apple.atve.sony.appletv"] = "Apple TV";
            Nombres["com.miui.tv.analytics"] = "Xiaomi Analítica";
            Nombres["com.mitv.tvhome.michannel"] = "Mi Channel";
            Nombres["com.mitv.videoplayer"] = "Mi Video";
            Nombres["com.mitv.download.service"] = "Mi Descargas";
            Nombres["com.mitv.milinkservice"] = "Mi Duplicar pantalla";
            Nombres["com.xiaomi.android.tvsetup.partnercustomizer"] = "Xiaomi Apps de socios";
            Nombres["com.xiaomi.mitv.res"] = "Xiaomi Recursos";
            Nombres["com.gameloft.android.ANMP.GloftA9HM"] = "Asphalt 9";
            Nombres["com.kiloo.subwaysurf"] = "Subway Surfers";
            Nombres["com.halfbrick.fruitninjafree"] = "Fruit Ninja";
            Nombres["com.nousguide.android.rbtv"] = "Red Bull TV";
            Nombres["com.tcl.suspension"] = "TCL Entradas";
            Nombres["com.tcl.ttvs"] = "TCL Núcleo";
            Nombres["com.tcl.tv"] = "TCL TV";
            Nombres["com.tcl.tvinput"] = "TCL Entradas TV";
            Nombres["com.tcl.providers.config"] = "TCL Configuración";
            Nombres["com.tcl.initsetup"] = "TCL Configuración inicial";
            Nombres["com.tcl.tcl_bt_rcu_service"] = "TCL Control Bluetooth";
            Nombres["com.tcl.autopair"] = "TCL Emparejar control";
            Nombres["com.amazon.tv.launcher"] = "Inicio de Fire TV";
            Nombres["com.amazon.venezia"] = "Amazon Appstore";
            Nombres["com.amazon.tv.acr"] = "Amazon ACR (rastreo)";
            Nombres["com.amazon.hybridadidservice"] = "Amazon ID de publicidad";
            Nombres["com.amazon.dp.logger"] = "Amazon registros";
            Nombres["com.amazon.device.software.ota"] = "Actualizaciones Fire OS";
            Nombres["com.amazon.media.recommendations"] = "Recomendaciones de Amazon";
            Nombres["com.amazon.sneakpeek"] = "Vistas previas automáticas";
            Nombres["com.amazon.tahoe"] = "Amazon Kids";
            Nombres["com.amazon.spiderpork"] = "Control de apps externas (Amazon)";
            Nombres["com.amazon.gamehub"] = "Amazon Juegos";
            Nombres["com.nvidia.tegrazone3"] = "NVIDIA Games";
        }

        // Paquetes no listados cuyo nombre EMPIEZA con esto: { prefijo, categoría, nivel, descripción }
        static readonly string[][] Prefijos = new string[][]
        {
            new string[] { "tv.alphonso.", "TELEMETRÍA", "1", "Alphonso ACR: rastrea lo que ves para publicidad." },
            new string[] { "com.samba.", "TELEMETRÍA", "2", "Samba TV: reconocimiento de contenido (ACR) para publicidad." },
            new string[] { "tv.samba.", "TELEMETRÍA", "2", "Samba TV: reconocimiento de contenido (ACR) para publicidad." },
            new string[] { "com.anoki.", "TELEMETRÍA", "2", "Anoki: publicidad y reconocimiento de contenido." },
            new string[] { "com.gameloft.", "JUEGOS", "1", "Juego de Gameloft preinstalado." },
        };

        // Si el nombre CONTIENE esto -> BLOQUEADO siempre (salvo que esté en los curados)
        static readonly string[] PatronesCriticos = new string[]
        {
            "inputmethod", ".ime", "webview", "permissioncontroller", "packageinstaller", "systemui", "bluetooth", "rcu", "autopair", "hdmi", ".cec", "tvinput", "tv.input", "setupwraith", "ext.services", "extservices", "networkstack", "providers.settings", "providers.tv", "providers.media", "mediaprovider", "tethering", "remotecontrol", "remote.service", "tv.settings", "tvsettings", "keyboard"
        };

        // Paquete del sistema desconocido que CONTIENE esto -> BLOQUEADO
        static readonly string[] PatronesSensibles = new string[]
        {
            "provider", "service", "framework", "overlay", "settings", "audio", "sound", "volume", "wifi", "network", "connectivity", "update", "ota", "fota", "factory", "mediatek", "mtk", "amlogic", "droidlogic", "realtek", "hisilicon", "launcher", "location", "resolv", "module", "telephony", "phone", "display", "picture", "power", "sensor", "input", "tvserver", "tvplayer", "middleware", "system", "core", "server", "daemon", "rockchip", "softwinner", "allwinner", "oobe"
        };

        static readonly string[] PalabrasTelemetria = new string[] { "analytic", "telemetry", "tracking", "tracker", "bug report", "crash report", "diagnos", "usage data", "collects", "spy" };
        static readonly string[] PalabrasPublicidad = new string[] { " ads", "advert", "promo", "sponsored", "adware" };
    }
}
