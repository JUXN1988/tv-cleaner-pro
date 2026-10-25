// Pruebas automáticas del motor contra un dispositivo SIMULADO (fakeadb.py). No tocan ningún TV real.
// Compilar y ejecutar: ver EJECUTAR_PRUEBAS.bat
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace TVCleanerPro
{
    static class PruebasMotor
    {
        static int ok, fallos;
        static string carpeta;

        static void Afirmar(bool cond, string que)
        {
            if (cond) { ok++; Console.WriteLine("  ok   " + que); }
            else { fallos++; Console.WriteLine("  FALLA " + que); }
        }

        static Dictionary<string, object> Estado()
        {
            var j = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            return j.Deserialize<Dictionary<string, object>>(File.ReadAllText(Environment.GetEnvironmentVariable("FAKEADB_STATE")));
        }

        static Dictionary<string, object> Pkg(string n)
        {
            var p = (Dictionary<string, object>)Estado()["packages"];
            return p.ContainsKey(n) ? (Dictionary<string, object>)p[n] : null;
        }

        static Motor Nuevo(string dispositivo)
        {
            string tmp = Path.Combine(carpeta, dispositivo + "_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            Directory.CreateDirectory(tmp);
            Environment.SetEnvironmentVariable("FAKEADB_STATE", Path.Combine(tmp, "estado.json"));
            Environment.SetEnvironmentVariable("FAKEADB_DEVICE", dispositivo);
            Environment.SetEnvironmentVariable("TVCLEANER_DATA", Path.Combine(tmp, "datos"));
            return new Motor();
        }

        static void Prueba(string nombre, Action a)
        {
            Console.WriteLine("\n▶ " + nombre);
            try { a(); }
            catch (Exception e) { fallos++; Console.WriteLine("  EXCEPCIÓN " + e); }
        }

        static int Main(string[] args)
        {
            carpeta = Path.Combine(Path.GetTempPath(), "tvc_pruebas_" + DateTime.Now.Ticks);
            Directory.CreateDirectory(carpeta);
            string fake = args.Length > 0 ? args[0] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Utilidades.EsWindows ? "fakeadb.bat" : "fakeadb");
            Environment.SetEnvironmentVariable("TVCLEANER_ADB", fake);
            Console.WriteLine("ADB simulado: " + fake);

            Prueba("Conexión: IP inválida e inyección rechazadas", () =>
            {
                var m = Nuevo("googletv");
                bool err = false;
                try { m.ConectarRed("192.168.1.99", "5555", null); } catch (AdbException) { err = true; }
                Afirmar(err, "IP sin TV da error claro");
                err = false;
                try { m.ConectarRed("1.2.3.4; rm -rf /", "5555", null); } catch (AdbException) { err = true; }
                Afirmar(err, "IP con comandos rechazada");
            });

            Prueba("Google TV: clasificación segura", () =>
            {
                var m = Nuevo("googletv");
                m.ConectarRed("192.168.1.2", "5555", null);
                Afirmar(m.TipoDispositivo == "Google TV", "tipo detectado: " + m.TipoDispositivo);
                var pk = m.Escanear();
                var esperado = new Dictionary<string, Nivel>
                {
                    { "com.google.android.gms", Nivel.Bloqueado }, { "com.android.location.fused", Nivel.Bloqueado },
                    { "com.google.android.tungsten.setupwraith", Nivel.Bloqueado }, { "com.google.android.ext.services", Nivel.Bloqueado },
                    { "com.google.android.webview", Nivel.Bloqueado }, { "com.google.android.leanback.ime", Nivel.Bloqueado },
                    { "com.sony.dtv.tvx", Nivel.Bloqueado }, { "com.mediatek.tvinput", Nivel.Bloqueado },
                    { "com.mtk.tvsystemservice", Nivel.Bloqueado }, { "com.sony.dtv.osdcoreservice", Nivel.Bloqueado },
                    { "com.google.android.apps.tv.launcherx", Nivel.Bloqueado },
                    { "com.google.android.tv.bugreportsender", Nivel.Seguro }, { "tv.alphonso.alphonso_eula", Nivel.Seguro },
                    { "com.sony.dtv.promos", Nivel.Seguro }, { "com.android.galaxy4", Nivel.Seguro },
                    { "com.google.android.katniss", Nivel.Precaucion }, { "com.netflix.ninja", Nivel.Precaucion },
                    { "com.example.myapp", Nivel.Precaucion }, { "com.vendor.unknownthing", Nivel.Experto },
                };
                foreach (var kv in esperado)
                    Afirmar(pk.ContainsKey(kv.Key) && pk[kv.Key].N == kv.Value, kv.Key + " = " + kv.Value + (pk.ContainsKey(kv.Key) ? " (es " + pk[kv.Key].N + ")" : " (no está)"));
                Afirmar(pk["com.google.android.feedback"].Estado == "desactivada", "detecta estado desactivado de fábrica");
                Afirmar(BaseDatos.Uad.Count > 5000, "lista UAD cargada desde recurso: " + BaseDatos.Uad.Count);
            });

            Prueba("Desactivar, historial, .bat y restaurar todo", () =>
            {
                var m = Nuevo("googletv");
                m.ConectarRed("192.168.1.2", "5555", null);
                m.Escanear();
                var nombres = new[] { "tv.alphonso.alphonso_eula", "com.sony.dtv.promos", "com.google.android.tv.bugreportsender" };
                var r = m.Aplicar(nombres, "desactivar", null);
                Afirmar(r.Ok.Count == 3 && r.Fallos.Count == 0, "3 desactivadas");
                Afirmar(nombres.All(n => !(bool)Pkg(n)["enabled"]), "el TV las tiene desactivadas");
                string bat = Path.Combine(Path.GetDirectoryName(m.Historial.Archivo), Path.GetFileNameWithoutExtension(m.Historial.Archivo) + "_RESTAURAR.bat");
                Afirmar(File.Exists(bat) && File.ReadAllText(bat).Contains("pm enable tv.alphonso.alphonso_eula"), ".bat de restauración correcto");
                Environment.SetEnvironmentVariable("TVCLEANER_DATA", Path.GetDirectoryName(Path.GetDirectoryName(m.Historial.Archivo)));
                Environment.SetEnvironmentVariable("FAKEADB_STATE", Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(m.Historial.Archivo))), "estado.json"));
                var m3 = new Motor();
                m3.ConectarRed("192.168.1.2", "5555", null);
                Afirmar(m3.Historial.Datos.actions.Count == 3, "historial persiste entre sesiones");
                var rr = m3.RestaurarTodo(null);
                Afirmar(rr.Fallos.Count == 0 && nombres.All(n => (bool)Pkg(n)["enabled"]), "restaurar todo reactiva");
                Afirmar(m3.Historial.Datos.actions.Count == 0, "historial vacío tras restaurar");
            });

            Prueba("Protegidos nunca se tocan", () =>
            {
                var m = Nuevo("googletv");
                m.ConectarRed("192.168.1.2", "5555", null);
                m.Escanear();
                var r = m.Aplicar(new[] { "com.google.android.gms", "com.google.android.apps.tv.launcherx", "com.google.android.leanback.ime", "no.existe.paquete" }, "desactivar", null);
                Afirmar(r.Ok.Count == 0 && r.Fallos.Count == 4, "4 rechazados");
                Afirmar((bool)Pkg("com.google.android.gms")["enabled"], "gms sigue activo");
                var calls = (System.Collections.ArrayList)Estado()["calls"];
                Afirmar(!calls.Cast<string>().Any(c => c.Contains("disable-user") && c.Contains("gms")), "nunca se envió la orden");
            });

            Prueba("Eliminar (sistema reversible y app con respaldo de APK)", () =>
            {
                var m = Nuevo("googletv");
                m.ConectarRed("192.168.1.2", "5555", null);
                m.Escanear();
                var r = m.Aplicar(new[] { "com.android.email", "com.example.myapp" }, "eliminar", null);
                Afirmar(r.Ok.Count == 2, "2 eliminadas");
                Afirmar(!(bool)Pkg("com.android.email")["installed"], "sistema: quitada para el usuario");
                Afirmar(Pkg("com.example.myapp") == null, "app: desinstalada");
                var apks = m.Historial.AccionDe("com.example.myapp").apks;
                Afirmar(apks.Count == 2 && apks.All(File.Exists), "APK respaldados en la PC (" + apks.Count + ")");
                m.Escanear();
                string t;
                Afirmar(m.Restaurar("com.android.email", out t) && (bool)Pkg("com.android.email")["installed"], "sistema restaurada");
                Afirmar(m.Restaurar("com.example.myapp", out t) && Pkg("com.example.myapp") != null, "app reinstalada desde respaldo");
            });

            Prueba("Ajustes reversibles", () =>
            {
                var m = Nuevo("googletv");
                m.ConectarRed("192.168.1.2", "5555", null);
                m.CambiarAjuste("global", "window_animation_scale", "0.5");
                m.CambiarAjuste("global", "window_animation_scale", "0");
                Afirmar(m.Historial.Datos.settings["global/window_animation_scale"] == "1.0", "guarda el valor ORIGINAL");
                m.CambiarAjuste("global", "ble_scan_always_enabled", "0");
                m.RestaurarTodo(null);
                var st = (Dictionary<string, object>)Estado()["settings"];
                Afirmar((string)st["global/window_animation_scale"] == "1.0", "animación restaurada");
                Afirmar(!st.ContainsKey("global/ble_scan_always_enabled"), "ajuste inexistente se borra al restaurar");
            });

            Prueba("Cambio de launcher con verificación", () =>
            {
                var m = Nuevo("googletv");
                m.ConectarRed("192.168.1.2", "5555", null);
                var inst = new Instalador(m.Adb);
                string apk = Path.Combine(carpeta, "flauncher.apk");
                File.WriteAllText(apk, "FAKEAPK:me.efesser.flauncher");
                var res = inst.Instalar(new[] { apk }, new[] { "-r" }, false, null);
                Afirmar(res[0].Ok, "FLauncher instalado");
                Afirmar(m.Adb.CandidatosInicio().Count == 2, "2 launchers (FallbackHome excluido)");
                string msg;
                bool ok2 = m.CambiarLauncher("me.efesser.flauncher/me.efesser.flauncher.MainActivity", true, out msg);
                Afirmar(ok2, "cambio confirmado: " + msg.Replace("\n", " "));
                Afirmar(!(bool)Pkg("com.google.android.apps.tv.launcherx")["enabled"], "launcher de Google desactivado");
                var pk = m.Escanear();
                Afirmar(pk["me.efesser.flauncher"].N == Nivel.Bloqueado, "FLauncher ahora protegido (es el inicio)");
            });

            Prueba("Instalador: debug (-t), split, .apks/.xapk con OBB y errores explicados", () =>
            {
                var m = Nuevo("googletv");
                m.ConectarRed("192.168.1.2", "5555", null);
                var inst = new Instalador(m.Adb);
                string debug = Path.Combine(carpeta, "mi_app-debug.apk");
                File.WriteAllText(debug, "FAKEAPK:com.gabriel.miapp:TEST_ONLY");
                var r1 = inst.Instalar(new[] { debug }, new[] { "-r" }, false, null);
                Afirmar(!r1[0].Ok && r1[0].Mensaje.Contains("-t"), "sin -t explica que falta la opción");
                var r2 = inst.Instalar(new[] { debug }, new[] { "-r", "-t" }, false, null);
                Afirmar(r2[0].Ok, "con -t se instala");
                Afirmar(m.Adb.DetallesApp("com.gabriel.miapp").ContainsKey("testOnly"), "detalles muestran que es de prueba");

                string xapk = Path.Combine(carpeta, "juego.xapk");
                using (var z = ZipFile.Open(xapk, ZipArchiveMode.Create))
                {
                    foreach (var n in new[] { "base.apk", "config.armeabi_v7a.apk", "config.es.apk" })
                        using (var w = new StreamWriter(z.CreateEntry(n).Open())) w.Write("FAKEAPK:com.juego.prueba");
                    using (var w = new StreamWriter(z.CreateEntry("Android/obb/com.juego.prueba/main.1.com.juego.prueba.obb").Open())) w.Write("datos");
                    using (var w = new StreamWriter(z.CreateEntry("../../malicioso.apk").Open())) w.Write("x");
                }
                var r3 = inst.Instalar(new[] { xapk }, new[] { "-r" }, false, null);
                Afirmar(r3[0].Ok && r3[0].Mensaje.Contains("3 APK") && r3[0].Mensaje.Contains("OBB"), ".xapk: 3 APK + OBB (ruta maliciosa ignorada): " + r3[0].Mensaje.Replace("\n", " "));
                var calls = ((System.Collections.ArrayList)Estado()["calls"]).Cast<string>().ToList();
                Afirmar(calls.Any(c => c.StartsWith("install-multiple -r 3")), "usó install-multiple con 3 partes");
                Afirmar(calls.Any(c => c.StartsWith("push ") && c.Contains("/sdcard/Android/obb/com.juego.prueba/")), "OBB enviado a su carpeta");

                string abi = Path.Combine(carpeta, "arm64.apk");
                File.WriteAllText(abi, "FAKEAPK:com.x.arm:ARM64");
                var r4 = inst.Instalar(new[] { abi }, new[] { "-r" }, false, null);
                Afirmar(!r4[0].Ok && r4[0].Mensaje.Contains("procesador"), "error de arquitectura explicado en español");

                string a1 = Path.Combine(carpeta, "base.apk"), a2 = Path.Combine(carpeta, "split_es.apk");
                File.WriteAllText(a1, "FAKEAPK:com.split.app");
                File.WriteAllText(a2, "FAKEAPK:com.split.app");
                var r5 = inst.Instalar(new[] { a1, a2 }, new[] { "-r" }, true, null);
                Afirmar(r5.Count == 1 && r5[0].Ok, "APK divididos instalados juntos");
                Afirmar(m.Adb.AppsUsuario().ContainsKey("com.gabriel.miapp"), "aparece en 'Mis apps' con versión");
                string t;
                Afirmar(m.Adb.AbrirApp("com.gabriel.miapp", out t), "abrir app");
                Afirmar(m.Adb.Logcat("com.gabriel.miapp", 100).Contains("hola desde la app"), "logcat de la app");
            });

            Prueba("Fire TV: detección y protecciones de Amazon", () =>
            {
                var m = Nuevo("firetv");
                m.ConectarRed("192.168.1.2", "5555", null);
                Afirmar(m.EsFireTv && m.TipoDispositivo.Contains("7.6.6.9"), "tipo: " + m.TipoDispositivo);
                Afirmar(!m.TienePlayStore, "sin Play Store");
                var pk = m.Escanear();
                Afirmar(pk["com.amazon.tv.launcher"].N == Nivel.Bloqueado, "inicio de Amazon protegido");
                Afirmar(pk["com.amazon.venezia"].N == Nivel.Bloqueado, "Appstore protegida");
                Afirmar(pk["com.amazon.tv.ime"].N == Nivel.Bloqueado, "teclado protegido");
                Afirmar(pk["com.amazon.tv.acr"].N == Nivel.Seguro && pk["com.amazon.tv.acr"].Cat == "TELEMETRÍA", "ACR de Amazon = telemetría segura");
                Afirmar(pk["com.amazon.hybridadidservice"].N == Nivel.Seguro, "ID publicidad Amazon seguro");
                Afirmar(pk["com.amazon.tv.settings.v2"].N == Nivel.Bloqueado, "ajustes de Fire TV bloqueados");
                Afirmar(pk["com.amazon.tv.inputpreference.service"].N == Nivel.Bloqueado, "servicio desconocido de entrada bloqueado");
            });

            Prueba("TV Box por USB (Amlogic)", () =>
            {
                var m = Nuevo("tvbox");
                var lista = m.Adb.Dispositivos();
                Afirmar(lista.Any(d => d.Serial == "BOX777" && d.EsUsb && d.Modelo == "X96 Max"), "aparece como USB con modelo");
                m.ConectarSerial("BOX777", null);
                Afirmar(m.TipoDispositivo == "TV Box Android", "tipo: " + m.TipoDispositivo);
                var pk = m.Escanear();
                Afirmar(pk["com.droidlogic"].N == Nivel.Bloqueado && pk["com.droidlogic.tvinput"].N == Nivel.Bloqueado, "Amlogic base protegido");
                Afirmar(pk["com.droidlogic.appinstall"].N == Nivel.Bloqueado, "droidlogic desconocido bloqueado por patrón");
                Afirmar(pk["com.android.launcher3"].N == Nivel.Bloqueado, "launcher actual protegido");
                Afirmar(pk["com.softwinner.explore"].N == Nivel.Seguro, "explorador de fábrica seguro");
                Afirmar(pk["com.mbox.weirdthing"].N == Nivel.Experto, "paquete raro = experto");
            });

            Prueba("Escáner de red: rangos, DNS, UPnP", () =>
            {
                var r = EscanerRed.InterpretarRangos("192.168.1.0/24, 10.0.0.1-10.0.0.20, 172.16.5.9, 192.168.2.10-50");
                Afirmar(r.Count == 4 && r[0].Cantidad == 254 && r[1].Cantidad == 20 && r[2].Cantidad == 1 && r[3].Cantidad == 41, "interpreta 4 formatos de rango");
                bool err = false;
                try { EscanerRed.InterpretarRangos("10.0.0.0/8"); } catch (ArgumentException) { err = true; }
                Afirmar(err, "rechaza rangos gigantes (/8)");
                var q = EscanerRed.ConsultaMdns(new[] { "_googlecast._tcp.local" });
                Afirmar(q.Length == 12 + 24 + 4 && q[5] == 1, "consulta mDNS bien formada");
                // respuesta mDNS real simulada: PTR + SRV + TXT + A con compresión de nombres
                var paquete = RespuestaMdns();
                var regs = EscanerRed.LeerPaqueteDns(paquete);
                var srv = regs.FirstOrDefault(x => x.Tipo == 33);
                var txt = regs.FirstOrDefault(x => x.Tipo == 16);
                var a = regs.FirstOrDefault(x => x.Tipo == 1);
                Afirmar(regs.Count == 4, "lee 4 registros DNS");
                Afirmar(srv != null && srv.Puerto == 8009 && srv.Texto == "sala.local", "SRV: puerto 8009 y host (con compresión)");
                Afirmar(txt != null && txt.Txt["fn"] == "TV Sala" && txt.Txt["md"] == "Google TV Streamer", "TXT: nombre y modelo");
                Afirmar(a != null && a.Texto == "192.168.1.2", "A: 192.168.1.2");
                var x2 = EscanerRed.DescripcionUpnp("<root><device><friendlyName>[TV] Samsung 7 Series (55)</friendlyName><manufacturer>Samsung Electronics</manufacturer><modelName>UN55TU7000</modelName></device></root>");
                Afirmar(x2[0] == "[TV] Samsung 7 Series (55)" && x2[1] == "Samsung Electronics" && x2[2] == "UN55TU7000", "lee descripción UPnP");
                var eq = new EquipoRed { IP = "1.1.1.1", Marca = "Samsung Electronics" };
                EscanerRed.Identificar(eq);
                Afirmar(eq.Tipo == "Samsung (Tizen)" && !eq.EsAndroid, "Samsung identificado como no Android");
                var eq2 = new EquipoRed { IP = "1.1.1.2" };
                eq2.Puertos.Add(6466); eq2.Puertos.Add(8009);
                EscanerRed.Identificar(eq2);
                Afirmar(eq2.EsAndroid && eq2.Compatibilidad.Contains("Depuración"), "Android TV sin ADB: pide activar depuración");
                var eq3 = new EquipoRed { IP = "1.1.1.3" };
                eq3.Puertos.Add(3000);
                EscanerRed.Identificar(eq3);
                Afirmar(eq3.Tipo == "Equipo de red", "PC con puerto 3000 NO se confunde con LG");
            });

            Prueba("Escáner de red: equipos reales en la red local de prueba", () =>
            {
                string rango = Environment.GetEnvironmentVariable("PRUEBA_RANGO_RED");
                if (string.IsNullOrEmpty(rango)) { Console.WriteLine("  (omitida: sin PRUEBA_RANGO_RED)"); return; }
                var esc = new EscanerRed { TiempoConexionMs = 300 };
                var res = esc.Escanear(EscanerRed.InterpretarRangos(rango), null, System.Threading.CancellationToken.None);
                foreach (var e in res) Console.WriteLine("     " + e.IP + " | " + e.Tipo + " | " + e.Compatibilidad + " | " + EscanerRed.DescribirPuertos(e));
                var adb = res.FirstOrDefault(e => e.PuertoAdb == 5555);
                var cast = res.FirstOrDefault(e => e.Tipo.StartsWith("Android TV"));
                var roku = res.FirstOrDefault(e => e.Tipo == "Roku");
                Afirmar(adb != null && adb.Compatibilidad.StartsWith("Listo"), "encuentra el equipo con ADB abierto");
                Afirmar(cast != null && cast.Compatibilidad.Contains("activa"), "encuentra Android TV sin ADB");
                Afirmar(roku != null && !roku.EsAndroid, "encuentra Roku (no Android)");
            });

            Console.WriteLine("\n══════════════════════════════════════");
            Console.WriteLine(" RESULTADO: " + ok + " correctas, " + fallos + " fallidas");
            Console.WriteLine("══════════════════════════════════════");
            return fallos == 0 ? 0 : 1;
        }

        static byte[] RespuestaMdns()
        {
            var ms = new MemoryStream();
            Action<byte[]> w = b => ms.Write(b, 0, b.Length);
            Func<string, byte[]> nombre = s =>
            {
                var m2 = new MemoryStream();
                foreach (var e in s.Split('.')) { var b = Encoding.ASCII.GetBytes(e); m2.WriteByte((byte)b.Length); m2.Write(b, 0, b.Length); }
                m2.WriteByte(0);
                return m2.ToArray();
            };
            w(new byte[] { 0, 0, 0x84, 0, 0, 0, 0, 1, 0, 0, 0, 3 });
            int posServicio = (int)ms.Position;
            w(nombre("_googlecast._tcp.local"));
            w(new byte[] { 0, 12, 0, 1, 0, 0, 0, 120 });
            var instancia = new MemoryStream();
            var etq = Encoding.ASCII.GetBytes("TV-Sala-abc");
            instancia.WriteByte((byte)etq.Length); instancia.Write(etq, 0, etq.Length);
            instancia.WriteByte((byte)(0xC0 | (posServicio >> 8))); instancia.WriteByte((byte)posServicio);
            var inst = instancia.ToArray();
            w(new byte[] { 0, (byte)inst.Length });
            int posInstancia = (int)ms.Position;
            w(inst);
            // SRV
            w(new byte[] { (byte)(0xC0 | (posInstancia >> 8)), (byte)posInstancia, 0, 33, 0x80, 1, 0, 0, 0, 120 });
            var host = nombre("sala.local");
            w(new byte[] { 0, (byte)(6 + host.Length), 0, 0, 0, 0, 0x1F, 0x49 });
            int posHost = (int)ms.Position;
            w(host);
            // TXT
            w(new byte[] { (byte)(0xC0 | (posInstancia >> 8)), (byte)posInstancia, 0, 16, 0x80, 1, 0, 0, 0, 120 });
            var t1 = Encoding.UTF8.GetBytes("fn=TV Sala");
            var t2 = Encoding.UTF8.GetBytes("md=Google TV Streamer");
            w(new byte[] { 0, (byte)(2 + t1.Length + t2.Length), (byte)t1.Length });
            w(t1);
            ms.WriteByte((byte)t2.Length);
            w(t2);
            // A
            w(new byte[] { (byte)(0xC0 | (posHost >> 8)), (byte)posHost, 0, 1, 0x80, 1, 0, 0, 0, 120, 0, 4, 192, 168, 1, 2 });
            return ms.ToArray();
        }
    }
}
