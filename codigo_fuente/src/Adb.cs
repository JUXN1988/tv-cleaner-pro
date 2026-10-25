// Comunicación con el dispositivo a través de ADB (Android Debug Bridge).
// Todas las órdenes se validan antes de enviarse: nunca se pasa texto libre al shell del TV.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TVCleanerPro
{
    public class AdbException : Exception
    {
        public AdbException(string m) : base(m) { }
    }

    public class Resultado
    {
        public int Codigo;
        public string Salida = "";
        public string Error = "";
        public byte[] Binario;

        public string Todo
        {
            get { return (Salida + " " + Error).Trim(); }
        }
    }

    public class DispositivoAdb
    {
        public string Serial;
        public string Estado;
        public string Modelo;
        public bool EsUsb;

        public override string ToString()
        {
            string tipo = EsUsb ? "USB" : "Red";
            return string.Format("{0}  [{1}]  {2}{3}", Serial, tipo, Modelo ?? "",
                Estado == "device" ? "" : "  (" + Estado + ")");
        }
    }

    public class EstadoPaquete
    {
        public bool Sistema;
        public string Estado;
    }

    public class Adb
    {
        public string Ruta;
        public volatile string Serial;
        static readonly string[] ArchivosAdb = { "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" };

        public Adb()
        {
            Ruta = Localizar();
        }

        // ───────── localizar adb ─────────
        static string Localizar()
        {
            string env = Environment.GetEnvironmentVariable("TVCLEANER_ADB");
            if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;

            string exe = Utilidades.EsWindows ? "adb.exe" : "adb";
            string junto = Path.Combine(Utilidades.CarpetaPrograma(), exe);
            if (File.Exists(junto)) return junto;
            junto = Path.Combine(Path.Combine(Utilidades.CarpetaPrograma(), "adb"), exe);
            if (File.Exists(junto)) return junto;

            if (Utilidades.EsWindows)
            {
                // ADB incluido dentro del programa: se copia una sola vez a %LOCALAPPDATA%\TVCleanerPro\adb
                try
                {
                    string destino = Path.Combine(Utilidades.CarpetaLocal(), "adb");
                    Directory.CreateDirectory(destino);
                    var asm = Assembly.GetExecutingAssembly();
                    foreach (var f in ArchivosAdb)
                    {
                        using (var s = asm.GetManifestResourceStream("adb/" + f))
                        {
                            if (s == null) continue;
                            string dst = Path.Combine(destino, f);
                            if (File.Exists(dst) && new FileInfo(dst).Length == s.Length) continue;
                            try
                            {
                                string tmp = dst + ".part";
                                using (var o = File.Create(tmp)) s.CopyTo(o);
                                if (File.Exists(dst)) File.Delete(dst);
                                File.Move(tmp, dst);
                            }
                            catch (IOException) { }   // en uso por un servidor adb ya abierto: sirve igual
                            catch (UnauthorizedAccessException) { }
                        }
                    }
                    string r = Path.Combine(destino, "adb.exe");
                    if (File.Exists(r)) return r;
                }
                catch (Exception) { }
            }

            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in path.Split(Path.PathSeparator))
            {
                try
                {
                    string c = Path.Combine(dir.Trim('"'), exe);
                    if (File.Exists(c)) return c;
                }
                catch (ArgumentException) { }
            }
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            foreach (var c in new[] {
                Path.Combine(home, @"AppData\Local\Android\Sdk\platform-tools\adb.exe"),
                @"C:\platform-tools\adb.exe", @"C:\Android\platform-tools\adb.exe", @"C:\adb\adb.exe" })
                if (File.Exists(c)) return c;
            return null;
        }

        // ───────── ejecución ─────────
        public Resultado Ejecutar(string[] args, int segundos, bool conSerial, bool binario)
        {
            if (Ruta == null) throw new AdbException("No se encontró ADB.");
            var todos = new List<string>();
            if (conSerial)
            {
                string s = Serial;
                if (string.IsNullOrEmpty(s)) throw new AdbException("No hay un dispositivo conectado.");
                todos.Add("-s"); todos.Add(s);
            }
            todos.AddRange(args);
            var sb = new StringBuilder();
            foreach (var a in todos)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(Utilidades.Comillas(a));
            }
            var psi = new ProcessStartInfo(Ruta, sb.ToString());
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            if (!binario) psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            var res = new Resultado();
            using (var p = new Process())
            {
                p.StartInfo = psi;
                try { p.Start(); }
                catch (Exception e) { throw new AdbException("No se pudo ejecutar ADB: " + e.Message); }
                try { p.StandardInput.Close(); } catch (Exception) { }
                var ms = new MemoryStream();
                Task<string> tOut = null;
                Task tBin = null;
                if (binario) tBin = p.StandardOutput.BaseStream.CopyToAsync(ms);
                else tOut = p.StandardOutput.ReadToEndAsync();
                Task<string> tErr = p.StandardError.ReadToEndAsync();

                if (!p.WaitForExit(segundos * 1000))
                {
                    try { p.Kill(); } catch (Exception) { }
                    string cmd = Utilidades.Recortar(string.Join(" ", args), 80);
                    throw new AdbException(string.Format("Tiempo agotado ({0}s) en: adb {1}", segundos, cmd));
                }
                // si un proceso hijo (servidor adb) heredó la salida, no esperar indefinidamente
                if (binario) tBin.Wait(5000); else tOut.Wait(5000);
                tErr.Wait(3000);
                res.Codigo = p.ExitCode;
                if (binario) res.Binario = ms.ToArray();
                else if (tOut.IsCompleted) res.Salida = tOut.Result.Replace("\r\n", "\n").Trim();
                if (tErr.IsCompleted) res.Error = tErr.Result.Replace("\r\n", "\n").Trim();
            }
            return res;
        }

        public Resultado Ejecutar(params string[] args)
        {
            return Ejecutar(args, 30, true, false);
        }

        public Resultado Shell(string comando, int segundos = 30)
        {
            return Ejecutar(new[] { "shell", comando }, segundos, true, false);
        }

        public string Sh(string comando, int segundos = 30)
        {
            return Shell(comando, segundos).Salida;
        }

        void ValidarPaquete(string pkg)
        {
            if (!Utilidades.PaqueteValido(pkg)) throw new AdbException("Nombre de paquete no válido: " + pkg);
        }

        // ───────── conexión ─────────
        public string VersionAdb()
        {
            var r = Ejecutar(new[] { "version" }, 15, false, false);
            var l = Utilidades.Lineas(r.Salida);
            return l.Count > 0 ? l[0] : r.Error;
        }

        public List<DispositivoAdb> Dispositivos()
        {
            var r = Ejecutar(new[] { "devices", "-l" }, 15, false, false);
            var res = new List<DispositivoAdb>();
            foreach (var linea in Utilidades.Lineas(r.Salida))
            {
                if (linea.StartsWith("List of") || linea.StartsWith("*") || linea.Trim().Length == 0) continue;
                var p = linea.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 2) continue;
                var d = new DispositivoAdb { Serial = p[0], Estado = p[1] };
                foreach (var t in p)
                    if (t.StartsWith("model:")) d.Modelo = t.Substring(6).Replace('_', ' ');
                d.EsUsb = !p[0].Contains(":") && !p[0].Contains("._adb");
                res.Add(d);
            }
            return res;
        }

        static readonly string Ayuda =
            "\n\nRevisa:\n• Que el dispositivo esté encendido y en la misma red que esta PC.\n" +
            "• Que la IP sea la correcta (en el TV: Ajustes > Red > Estado de la red).\n" +
            "• Que 'Depuración ADB' / 'Depuración USB' esté activada en Opciones de desarrollador.\n" +
            "• Fire TV: Ajustes > Mi Fire TV > Opciones para desarrolladores > Depuración ADB.\n" +
            "• Android 11+ con 'Depuración inalámbrica': usa el botón Emparejar.\n" +
            "• Usa la pestaña 'Red' para escanear y ver qué equipos responden.";

        /// <summary>Conecta por red. esperaAutorizacion(segundos) se llama mientras el TV pide permiso.</summary>
        public string Conectar(string host, string puerto, Action<int> esperaAutorizacion)
        {
            if (!Utilidades.HostValido(host) || !Utilidades.PuertoValido(puerto))
                throw new AdbException("IP o puerto no válidos.");
            string destino = host + ":" + int.Parse(puerto);
            Ejecutar(new[] { "start-server" }, 30, false, false);
            Resultado r;
            try { r = Ejecutar(new[] { "connect", destino }, 20, false, false); }
            catch (AdbException) { throw new AdbException("El dispositivo no respondió en " + destino + "." + Ayuda); }
            string msg = r.Todo;
            string low = msg.ToLowerInvariant();
            if (low.Contains("cannot") || low.Contains("failed to connect") || low.Contains("unable") ||
                low.Contains("refused") || low.Contains("no route"))
                throw new AdbException("No se pudo conectar a " + destino + ": " + msg + Ayuda);
            Esperar(destino, low.Contains("authenticate"), esperaAutorizacion, true);
            return msg;
        }

        /// <summary>Usa un dispositivo ya visible para ADB (USB o red).</summary>
        public void Usar(string serial, Action<int> esperaAutorizacion)
        {
            Esperar(serial, false, esperaAutorizacion, false);
        }

        void Esperar(string destino, bool pideAuth, Action<int> esperaAutorizacion, bool esRed)
        {
            DateTime limite = DateTime.Now.AddSeconds(90);
            bool reintento = false;
            while (DateTime.Now < limite)
            {
                string estado = EstadoDe(destino);
                if (estado == "device") { Serial = destino; return; }
                if (estado == "unauthorized" || pideAuth)
                {
                    if (esperaAutorizacion != null) esperaAutorizacion((int)(limite - DateTime.Now).TotalSeconds);
                }
                else if ((estado == "offline" || estado == null) && esRed && !reintento)
                {
                    Ejecutar(new[] { "disconnect", destino }, 10, false, false);
                    Thread.Sleep(1000);
                    Ejecutar(new[] { "connect", destino }, 20, false, false);
                    reintento = true;
                }
                else if (estado == null && !esRed)
                {
                    throw new AdbException("El dispositivo " + destino + " ya no aparece. ¿Se desconectó el cable?");
                }
                Thread.Sleep(2000);
            }
            if (EstadoDe(destino) == "unauthorized")
                throw new AdbException("El dispositivo no autorizó la conexión. En su pantalla acepta " +
                                       "'¿Permitir la depuración?' y marca 'Permitir siempre'.");
            throw new AdbException("El dispositivo no respondió (estado: " + (EstadoDe(destino) ?? "desconocido") + ")." + Ayuda);
        }

        string EstadoDe(string serial)
        {
            foreach (var d in Dispositivos())
                if (d.Serial == serial) return d.Estado;
            return null;
        }

        public string Emparejar(string host, string puerto, string codigo)
        {
            if (!Utilidades.HostValido(host) || !Utilidades.PuertoValido(puerto) || !Regex.IsMatch(codigo ?? "", @"^\d{6}$"))
                throw new AdbException("Datos de emparejamiento no válidos (el código tiene 6 dígitos).");
            var r = Ejecutar(new[] { "pair", host + ":" + int.Parse(puerto), codigo }, 30, false, false);
            if (!r.Todo.ToLowerInvariant().Contains("success")) throw new AdbException(r.Todo.Length > 0 ? r.Todo : "Falló el emparejamiento.");
            return r.Todo;
        }

        /// <summary>Dispositivos anunciados por mDNS que ADB conoce (depuración inalámbrica).</summary>
        public List<string[]> ServiciosMdns()
        {
            var res = new List<string[]>();
            try
            {
                var r = Ejecutar(new[] { "mdns", "services" }, 10, false, false);
                foreach (var l in Utilidades.Lineas(r.Salida))
                {
                    var m = Regex.Match(l, @"^(\S+)\s+(_adb[\w\-]*\._tcp)\.?\s+([\d.]+):(\d+)");
                    if (m.Success) res.Add(new[] { m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value });
                }
            }
            catch (AdbException) { }
            return res;
        }

        public void Desconectar()
        {
            string s = Serial;
            if (!string.IsNullOrEmpty(s) && s.Contains(":"))
            {
                try { Ejecutar(new[] { "disconnect", s }, 10, false, false); } catch (AdbException) { }
            }
            Serial = null;
        }

        public void CerrarServidor()
        {
            try { Ejecutar(new[] { "kill-server" }, 10, false, false); } catch (AdbException) { }
        }

        public bool Responde()
        {
            try { return Sh("echo ok", 10).Trim() == "ok"; }
            catch (AdbException) { return false; }
        }

        // ───────── información ─────────
        public Dictionary<string, string> Propiedades()
        {
            var claves = new Dictionary<string, string> {
                { "marca", "ro.product.brand" }, { "fabricante", "ro.product.manufacturer" },
                { "modelo", "ro.product.model" }, { "android", "ro.build.version.release" },
                { "sdk", "ro.build.version.sdk" }, { "serie", "ro.serialno" },
                { "compilacion", "ro.build.display.id" }, { "nombre", "ro.product.name" },
                { "fireos", "ro.build.mktg.fireos" }, { "plataforma", "ro.board.platform" },
                { "abi", "ro.product.cpu.abi" } };
            var res = new Dictionary<string, string>();
            foreach (var kv in claves)
            {
                string v = "";
                try { v = Sh("getprop " + kv.Value, 10).Trim(); } catch (AdbException) { }
                if (v.Length > 0) res[kv.Key] = v;
            }
            return res;
        }

        public HashSet<string> Caracteristicas()
        {
            var res = new HashSet<string>();
            foreach (var l in Utilidades.Lineas(Sh("pm list features")))
                if (l.StartsWith("feature:")) res.Add(l.Substring(8).Trim().Split('=')[0]);
            return res;
        }

        public Dictionary<string, int> Memoria()
        {
            var res = new Dictionary<string, int>();
            foreach (var l in Utilidades.Lineas(Sh("cat /proc/meminfo")))
            {
                var m = Regex.Match(l, @"^(\w+):\s+(\d+)");
                if (m.Success) res[m.Groups[1].Value] = (int)(long.Parse(m.Groups[2].Value) / 1024);
            }
            return res;
        }

        /// <summary>[total, usado, libre] en MB de /data.</summary>
        public long[] Almacenamiento()
        {
            var lineas = Utilidades.Lineas(Sh("df /data"));
            for (int i = lineas.Count - 1; i >= 0; i--)
            {
                var p = lineas[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                long t, u, a;
                if (p.Length >= 4 && long.TryParse(p[1], out t) && long.TryParse(p[2], out u) && long.TryParse(p[3], out a))
                    return new[] { t / 1024, u / 1024, a / 1024 };
            }
            return new long[] { 0, 0, 0 };
        }

        // ───────── paquetes ─────────
        HashSet<string> ListaPm(string banderas)
        {
            var r = Shell(("pm list packages " + banderas).Trim(), 60);
            if (r.Codigo != 0 && r.Salida.Length == 0) throw new AdbException(r.Error.Length > 0 ? r.Error : "pm list packages falló");
            var res = new HashSet<string>();
            foreach (var l in Utilidades.Lineas(r.Salida))
            {
                string t = l.Trim();
                if (!t.StartsWith("package:")) continue;
                string n = t.Substring(8).Trim();
                int sp = n.IndexOf(' ');
                if (sp > 0) n = n.Substring(0, sp);
                if (Utilidades.PaqueteValido(n) || n == "android") res.Add(n);
            }
            return res;
        }

        public Dictionary<string, EstadoPaquete> EstadosPaquetes()
        {
            var instalados = ListaPm("");
            var sistema = ListaPm("-s");
            var desactivados = ListaPm("-d");
            HashSet<string> todos;
            try { todos = ListaPm("-u"); }
            catch (AdbException) { todos = new HashSet<string>(instalados); }
            var res = new Dictionary<string, EstadoPaquete>();
            foreach (var p in todos)
            {
                bool quitado = !instalados.Contains(p);
                string est = quitado ? "eliminada" : (desactivados.Contains(p) ? "desactivada" : "activa");
                res[p] = new EstadoPaquete { Sistema = sistema.Contains(p) || quitado, Estado = est };
            }
            return res;
        }

        /// <summary>Apps instaladas por el usuario con su versionCode (si el sistema lo informa).</summary>
        public Dictionary<string, string> AppsUsuario()
        {
            var res = new Dictionary<string, string>();
            var r = Shell("pm list packages -3 --show-versioncode", 60);
            if (r.Salida.Length == 0) r = Shell("pm list packages -3", 60);
            foreach (var l in Utilidades.Lineas(r.Salida))
            {
                var m = Regex.Match(l.Trim(), @"^package:(\S+)(?:\s+versionCode:(\d+))?");
                if (m.Success && Utilidades.PaqueteValido(m.Groups[1].Value))
                    res[m.Groups[1].Value] = m.Groups[2].Success ? m.Groups[2].Value : "";
            }
            return res;
        }

        public Dictionary<string, string> DetallesApp(string pkg)
        {
            ValidarPaquete(pkg);
            var res = new Dictionary<string, string>();
            string salida = Sh("dumpsys package " + pkg, 30);
            foreach (var clave in new[] { "versionName", "versionCode", "targetSdk", "minSdk", "firstInstallTime", "lastUpdateTime", "installerPackageName" })
            {
                var m = Regex.Match(salida, @"\b" + clave + @"=([^\s]+)");
                if (m.Success && !res.ContainsKey(clave)) res[clave] = m.Groups[1].Value;
            }
            if (salida.Contains("TEST_ONLY")) res["testOnly"] = "sí";
            if (salida.Contains("DEBUGGABLE")) res["debuggable"] = "sí";
            return res;
        }

        public string InicioActual()
        {
            string salida = Sh("cmd package resolve-activity --brief -a android.intent.action.MAIN -c android.intent.category.HOME");
            string comp = null;
            foreach (var l in Utilidades.Lineas(salida))
                if (Utilidades.RxComponente.IsMatch(l.Trim())) comp = l.Trim();
            if (comp == null || comp.StartsWith("android/") || comp.Contains("ResolverActivity")) return null;
            return comp;
        }

        public List<string> CandidatosInicio()
        {
            var res = new List<string>();
            foreach (var l in Utilidades.Lineas(Sh("cmd package query-activities --brief -a android.intent.action.MAIN -c android.intent.category.HOME")))
            {
                string t = l.Trim();
                if (!Utilidades.RxComponente.IsMatch(t) || res.Contains(t)) continue;
                string pkg = t.Split('/')[0];
                if (pkg.ToLowerInvariant().Contains("settings") || pkg == "android") continue;
                res.Add(t);
            }
            return res;
        }

        public string TecladoActual()
        {
            string v = Sh("settings get secure default_input_method").Trim();
            return v.Length > 0 && v != "null" ? v.Split('/')[0] : null;
        }

        public HashSet<string> PaquetesAccesibilidad()
        {
            var res = new HashSet<string>();
            string v = Sh("settings get secure enabled_accessibility_services").Trim();
            if (v.Length == 0 || v == "null") return res;
            foreach (var c in v.Split(':'))
                if (c.Contains("/")) res.Add(c.Split('/')[0]);
            return res;
        }

        static bool Contiene(string t, string s)
        {
            return t.ToLowerInvariant().Contains(s);
        }

        public bool Desactivar(string pkg, out string texto)
        {
            ValidarPaquete(pkg);
            texto = Shell("pm disable-user --user 0 " + pkg).Todo;
            return Contiene(texto, "disabled") && !Contiene(texto, "error");
        }

        public bool Activar(string pkg, out string texto)
        {
            ValidarPaquete(pkg);
            texto = Shell("pm enable " + pkg).Todo;
            return Contiene(texto, "enabled") && !Contiene(texto, "error");
        }

        /// <summary>Quita una app del sistema solo para el usuario (se restaura con install-existing).</summary>
        public bool EliminarParaUsuario(string pkg, out string texto)
        {
            ValidarPaquete(pkg);
            texto = Shell("pm uninstall -k --user 0 " + pkg).Todo;
            return texto.ToLowerInvariant().StartsWith("success") || texto.ToLowerInvariant().Contains("\nsuccess");
        }

        /// <summary>Desinstala una app que NO es del sistema.</summary>
        public bool Desinstalar(string pkg, out string texto)
        {
            ValidarPaquete(pkg);
            texto = Shell("pm uninstall " + pkg).Todo;
            return Contiene(texto, "success");
        }

        public bool Reinstalar(string pkg, out string texto)
        {
            ValidarPaquete(pkg);
            texto = Shell("cmd package install-existing " + pkg).Todo;
            if (Contiene(texto, "installed") && !Contiene(texto, "fail")) return true;
            string t2 = Shell("pm install-existing " + pkg).Todo;
            if (t2.Length > 0) texto = t2;
            return Contiene(t2, "installed") && !Contiene(t2, "fail");
        }

        public List<string> RutasApk(string pkg)
        {
            ValidarPaquete(pkg);
            var res = new List<string>();
            foreach (var l in Utilidades.Lineas(Sh("pm path " + pkg)))
                if (l.StartsWith("package:")) res.Add(l.Substring(8).Trim());
            return res;
        }

        public bool Descargar(string remoto, string local)
        {
            if (!Regex.IsMatch(remoto, @"^/[A-Za-z0-9_./\-=~+]+\.apk$")) throw new AdbException("Ruta de APK inesperada: " + remoto);
            var r = Ejecutar(new[] { "pull", remoto, local }, 300, true, false);
            return r.Codigo == 0 && File.Exists(local);
        }

        public Resultado Enviar(string local, string remoto)
        {
            if (!Regex.IsMatch(remoto, @"^/(sdcard|storage|data/local/tmp)/[A-Za-z0-9_./\- ()\[\]]*$")) throw new AdbException("Destino no permitido: " + remoto);
            return Ejecutar(new[] { "push", local, remoto }, 1800, true, false);
        }

        /// <summary>Instala uno o varios APK de la MISMA app (varios = APK divididos).</summary>
        public Resultado Instalar(IList<string> apks, string[] opciones)
        {
            var args = new List<string>();
            args.Add(apks.Count == 1 ? "install" : "install-multiple");
            foreach (var o in opciones)
            {
                if (o != "-r" && o != "-d" && o != "-g" && o != "-t") throw new AdbException("Opción no válida: " + o);
                args.Add(o);
            }
            args.AddRange(apks);
            return Ejecutar(args.ToArray(), 900, true, false);
        }

        // ───────── ajustes ─────────
        static readonly Regex RxAjuste = new Regex(@"^[a-z_][a-z0-9_.]*$");

        void ValidarAjuste(string ns, string clave)
        {
            if ((ns != "global" && ns != "secure" && ns != "system") || !RxAjuste.IsMatch(clave))
                throw new AdbException("Ajuste no válido");
        }

        public string LeerAjuste(string ns, string clave)
        {
            ValidarAjuste(ns, clave);
            return Sh("settings get " + ns + " " + clave).Trim();
        }

        public void PonerAjuste(string ns, string clave, string valor)
        {
            ValidarAjuste(ns, clave);
            valor = valor ?? "null";
            if (valor == "null" || valor.Length == 0) { Shell("settings delete " + ns + " " + clave); return; }
            if (!Regex.IsMatch(valor, @"^[A-Za-z0-9_.:/\-]+$")) throw new AdbException("Valor de ajuste no válido");
            Shell("settings put " + ns + " " + clave + " " + valor);
        }

        public void Tecla(string codigo)
        {
            if (!Regex.IsMatch(codigo, @"^KEYCODE_[A-Z0-9_]+$")) throw new AdbException("Tecla no válida");
            Shell("input keyevent " + codigo, 10);
        }

        public void EscribirTexto(string t)
        {
            if (!Regex.IsMatch(t ?? "", @"^[A-Za-z0-9@._\-+ ]{1,200}$"))
                throw new AdbException("Solo letras, números, espacios y @ . _ - +");
            Shell("input text " + t.Replace(" ", "%s"), 15);
        }

        public byte[] Captura()
        {
            var r = Ejecutar(new[] { "exec-out", "screencap", "-p" }, 30, true, true);
            var d = r.Binario;
            if (d == null || d.Length < 8 || d[0] != 0x89 || d[1] != (byte)'P')
                throw new AdbException(r.Error.Length > 0 ? r.Error : "No se pudo capturar la pantalla");
            return d;
        }

        public void AbrirTienda(string pkg)
        {
            ValidarPaquete(pkg);
            Shell("am start -a android.intent.action.VIEW -d market://details?id=" + pkg);
        }

        public bool AbrirApp(string pkg, out string texto)
        {
            ValidarPaquete(pkg);
            texto = Shell("monkey -p " + pkg + " -c android.intent.category.LEANBACK_LAUNCHER 1").Todo;
            if (!Contiene(texto, "no activities") && !Contiene(texto, "aborted")) return true;
            texto = Shell("monkey -p " + pkg + " -c android.intent.category.LAUNCHER 1").Todo;
            return !Contiene(texto, "no activities") && !Contiene(texto, "aborted");
        }

        public void ForzarCierre(string pkg)
        {
            ValidarPaquete(pkg);
            Shell("am force-stop " + pkg);
        }

        public bool BorrarDatos(string pkg, out string texto)
        {
            ValidarPaquete(pkg);
            texto = Shell("pm clear " + pkg).Todo;
            return Contiene(texto, "success");
        }

        public void AbrirAjustesApp(string pkg)
        {
            ValidarPaquete(pkg);
            Shell("am start -a android.settings.APPLICATION_DETAILS_SETTINGS -d package:" + pkg);
        }

        public string Logcat(string pkg, int lineas)
        {
            string filtro = "";
            if (pkg != null)
            {
                ValidarPaquete(pkg);
                string pid = Sh("pidof " + pkg).Trim().Split(' ')[0];
                if (Regex.IsMatch(pid, @"^\d+$")) filtro = " --pid=" + pid;
                else return "(La app " + pkg + " no está en ejecución. Ábrela y vuelve a intentar.)\n\n" +
                            Sh("logcat -d -t " + lineas + " -v time", 60);
            }
            return Sh("logcat -d -t " + lineas + " -v time" + filtro, 60);
        }

        public bool PonerInicio(string componente, out string texto)
        {
            if (!Utilidades.RxComponente.IsMatch(componente)) throw new AdbException("Componente no válido");
            var r = Shell("cmd package set-home-activity " + componente);
            texto = r.Todo;
            string low = texto.ToLowerInvariant();
            return low.Contains("success") || (r.Codigo == 0 && !low.Contains("error") && !low.Contains("exception"));
        }

        public void Reiniciar()
        {
            try { Ejecutar(new[] { "reboot" }, 15, true, false); } catch (AdbException) { }
        }
    }
}
