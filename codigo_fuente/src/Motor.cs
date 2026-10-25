// Motor: lógica de limpieza, protecciones, restauración y cambio de launcher (sin interfaz).
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace TVCleanerPro
{
    public class Motor
    {
        public readonly Adb Adb = new Adb();
        public readonly string Datos = Utilidades.CarpetaDatos();
        public Historial Historial;
        public Dictionary<string, string> Props = new Dictionary<string, string>();
        public string TipoDispositivo = "";
        public bool TienePlayStore;
        public Dictionary<string, InfoPaquete> Paquetes = new Dictionary<string, InfoPaquete>();
        public string Inicio;
        public Dictionary<string, string> Protegidos = new Dictionary<string, string>();
        public Action<string> Log = delegate { };

        public string Prop(string k)
        {
            string v;
            return Props.TryGetValue(k, out v) ? v : "";
        }

        public string ClaveDispositivo()
        {
            string serie = Prop("serie");
            string baseNombre = (Prop("marca").Length > 0 ? Prop("marca") : "TV") + "_" + Prop("modelo");
            if (serie.Length > 0 && serie.ToLowerInvariant() != "unknown" && serie != "0123456789ABCDEF")
                return baseNombre + "_" + serie;
            string s = Adb.Serial ?? "";
            return baseNombre + "_" + s.Split(':')[0];
        }

        // ───────── conexión ─────────
        public string ConectarRed(string host, string puerto, Action<int> auth)
        {
            Adb.Desconectar();
            string msg = Adb.Conectar(host, puerto, auth);
            DespuesDeConectar();
            return msg;
        }

        public void ConectarSerial(string serial, Action<int> auth)
        {
            Adb.Usar(serial, auth);
            DespuesDeConectar();
        }

        void DespuesDeConectar()
        {
            Props = Adb.Propiedades();
            Historial = new Historial(Path.Combine(Datos, "historial"), ClaveDispositivo());
            DetectarTipo();
        }

        /// <summary>Identifica Fire TV, Google TV, Android TV, NVIDIA Shield, TV Box genérico o teléfono/tablet.</summary>
        public void DetectarTipo()
        {
            HashSet<string> f;
            try { f = Adb.Caracteristicas(); } catch (AdbException) { f = new HashSet<string>(); }
            string fab = (Prop("fabricante") + " " + Prop("marca")).ToLowerInvariant();
            string modelo = (Prop("modelo") + " " + Prop("nombre") + " " + Prop("plataforma")).ToLowerInvariant();
            bool leanback = f.Contains("android.software.leanback") || f.Contains("android.software.leanback_only");
            bool tieneGms = false, launcherx = false;
            try
            {
                string pk = Adb.Sh("pm list packages com.google.android", 30);
                tieneGms = pk.Contains("package:com.google.android.gms\n") || pk.EndsWith("package:com.google.android.gms");
                launcherx = pk.Contains("com.google.android.apps.tv.launcherx");
                TienePlayStore = Adb.Sh("pm path com.android.vending", 15).Contains("package:");
            }
            catch (AdbException) { }

            if (fab.Contains("amazon") || f.Contains("amazon.hardware.fire_tv") || Prop("fireos").Length > 0)
                TipoDispositivo = "Fire TV (Fire OS " + (Prop("fireos").Length > 0 ? Prop("fireos") : "?") + ")";
            else if (fab.Contains("nvidia"))
                TipoDispositivo = "NVIDIA Shield";
            else if (launcherx)
                TipoDispositivo = "Google TV";
            else if (leanback && tieneGms)
                TipoDispositivo = "Android TV";
            else if (leanback || fab.Contains("amlogic") || fab.Contains("rockchip") || fab.Contains("allwinner") ||
                     fab.Contains("softwinner") || modelo.Contains("box") || modelo.Contains("amlogic") ||
                     modelo.Contains("rk3") || modelo.Contains("s905") || modelo.Contains("h6"))
                TipoDispositivo = "TV Box Android";
            else if (f.Contains("android.hardware.telephony"))
                TipoDispositivo = "Teléfono Android";
            else
                TipoDispositivo = "Dispositivo Android";
        }

        public bool EsFireTv
        {
            get { return TipoDispositivo.StartsWith("Fire TV"); }
        }

        // ───────── escaneo ─────────
        public Dictionary<string, string> CalcularProtegidos()
        {
            var p = new Dictionary<string, string>();
            try { Inicio = Adb.InicioActual(); } catch (AdbException) { Inicio = null; }
            if (Inicio != null)
                p[Inicio.Split('/')[0]] = "Es tu pantalla de inicio ACTUAL. Cámbiala primero desde la pestaña Launcher.";
            else
            {
                try
                {
                    foreach (var c in Adb.CandidatosInicio())
                        p[c.Split('/')[0]] = "Launcher del dispositivo (no hay uno predeterminado). Elige uno en la pestaña Launcher.";
                }
                catch (AdbException) { }
            }
            try
            {
                string t = Adb.TecladoActual();
                if (t != null) p[t] = "Es el teclado en pantalla que usa el dispositivo.";
            }
            catch (AdbException) { }
            try
            {
                foreach (var a in Adb.PaquetesAccesibilidad()) p[a] = "Servicio de accesibilidad activo.";
            }
            catch (AdbException) { }
            if (EsFireTv)
            {
                // En Fire TV la tienda de Amazon también es el cajón de apps
                p["com.amazon.venezia"] = "Fire TV: la tienda de Amazon maneja el menú 'Tus apps'. Quitarla lo rompe.";
            }
            Protegidos = p;
            return p;
        }

        public Dictionary<string, InfoPaquete> Escanear()
        {
            var estados = Adb.EstadosPaquetes();
            CalcularProtegidos();
            var res = new Dictionary<string, InfoPaquete>();
            foreach (var kv in estados)
            {
                string razon;
                Protegidos.TryGetValue(kv.Key, out razon);
                var c = BaseDatos.Clasificar(kv.Key, kv.Value.Sistema, razon);
                res[kv.Key] = new InfoPaquete
                {
                    Nombre = kv.Key, Cat = c.Cat, N = c.N, Desc = c.Desc, Fuente = c.Fuente,
                    Sistema = kv.Value.Sistema, Estado = kv.Value.Estado
                };
            }
            Paquetes = res;
            return res;
        }

        // ───────── aplicar ─────────
        public class ResultadoLote
        {
            public List<string> Ok = new List<string>();
            public List<string[]> Fallos = new List<string[]>();   // {paquete, motivo}
        }

        /// <summary>modo: "desactivar" o "eliminar".</summary>
        public ResultadoLote Aplicar(IList<string> nombres, string modo, Action<int, int, string> progreso)
        {
            var r = new ResultadoLote();
            var prot = CalcularProtegidos();
            int i = 0;
            foreach (var n in nombres)
            {
                i++;
                if (progreso != null) progreso(i, nombres.Count, n);
                InfoPaquete info;
                if (!Paquetes.TryGetValue(n, out info)) { r.Fallos.Add(new[] { n, "No existe en el dispositivo" }); continue; }
                if (info.N == Nivel.Bloqueado || prot.ContainsKey(n)) { r.Fallos.Add(new[] { n, "Protegido: no se modificó" }); continue; }
                bool bien = false;
                string texto = "", accion = "";
                List<string> apks = null;
                try
                {
                    if (modo == "eliminar" && !info.Sistema)
                    {
                        apks = RespaldarApk(n);
                        if (apks.Count == 0) { r.Fallos.Add(new[] { n, "No se pudo respaldar el APK; no se eliminó" }); continue; }
                        bien = Adb.Desinstalar(n, out texto);
                        accion = "desinstalada";
                    }
                    else if (modo == "eliminar")
                    {
                        bien = Adb.EliminarParaUsuario(n, out texto);
                        accion = "eliminada_usuario";
                    }
                    else
                    {
                        bien = Adb.Desactivar(n, out texto);
                        accion = "desactivada";
                    }
                }
                catch (AdbException e) { texto = e.Message; }
                if (bien)
                {
                    var previa = Historial.AccionDe(n);
                    if ((apks == null || apks.Count == 0) && previa != null) apks = previa.apks;
                    Historial.AgregarAccion(n, accion, apks);
                    info.Estado = accion == "desactivada" ? "desactivada" : "eliminada";
                    r.Ok.Add(n);
                    Log("OK  " + accion + ": " + n);
                }
                else
                {
                    if (EsFireTv && texto.Contains("SecurityException"))
                        texto += " (Fire OS no permite desactivar este paquete)";
                    r.Fallos.Add(new[] { n, texto });
                    Log("ERR " + n + ": " + texto);
                }
            }
            VerificarSeguridad();
            Historial.EscribirBatRestauracion(Adb.Ruta, Adb.Serial);
            return r;
        }

        public List<string> RespaldarApk(string pkg)
        {
            string carpeta = Path.Combine(Path.Combine(Datos, "respaldos_apk"), pkg);
            Directory.CreateDirectory(carpeta);
            var guardados = new List<string>();
            int i = 0;
            foreach (var remoto in Adb.RutasApk(pkg))
            {
                string local = Path.Combine(carpeta, i.ToString("00") + "_" + Path.GetFileName(remoto));
                if (!Adb.Descargar(remoto, local)) return new List<string>();
                guardados.Add(local);
                i++;
            }
            return guardados;
        }

        public bool Restaurar(string n, out string texto)
        {
            InfoPaquete info;
            Paquetes.TryGetValue(n, out info);
            var acc = Historial != null ? Historial.AccionDe(n) : null;
            string accion = acc != null ? acc.action : null;
            string estado = info != null ? info.Estado : null;
            bool bien;
            if (accion == "desinstalada" || (estado == null && acc != null && acc.apks.Count > 0))
            {
                var apks = new List<string>();
                foreach (var a in acc.apks) if (File.Exists(a)) apks.Add(a);
                if (apks.Count == 0) { texto = "No hay respaldo del APK. Reinstálala desde la tienda."; return false; }
                var r = Adb.Instalar(apks, new[] { "-r" });
                texto = r.Todo;
                bien = texto.ToLowerInvariant().Contains("success");
            }
            else if (estado == "eliminada" || accion == "eliminada_usuario")
            {
                bien = Adb.Reinstalar(n, out texto);
                string t2;
                if (bien) Adb.Activar(n, out t2);
            }
            else bien = Adb.Activar(n, out texto);

            if (bien)
            {
                if (Historial != null) Historial.QuitarAccion(n);
                if (info != null) info.Estado = "activa";
                Log("OK  restaurada: " + n);
            }
            else Log("ERR restaurar " + n + ": " + texto);
            if (Historial != null) Historial.EscribirBatRestauracion(Adb.Ruta, Adb.Serial);
            return bien;
        }

        public ResultadoLote RestaurarTodo(Action<int, int, string> progreso)
        {
            var r = new ResultadoLote();
            if (Historial == null) return r;
            var nombres = new List<string>(Historial.Datos.actions.Keys);
            int i = 0;
            foreach (var n in nombres)
            {
                i++;
                if (progreso != null) progreso(i, nombres.Count, n);
                string t;
                bool ok;
                try { ok = Restaurar(n, out t); }
                catch (AdbException e) { ok = false; t = e.Message; }
                if (ok) r.Ok.Add(n); else r.Fallos.Add(new[] { n, t });
            }
            foreach (var kv in new List<KeyValuePair<string, string>>(Historial.Datos.settings))
            {
                var p = kv.Key.Split(new[] { '/' }, 2);
                try
                {
                    Adb.PonerAjuste(p[0], p[1], kv.Value);
                    Historial.OlvidarAjuste(p[0], p[1]);
                    r.Ok.Add(kv.Key);
                }
                catch (AdbException e) { r.Fallos.Add(new[] { kv.Key, e.Message }); }
            }
            return r;
        }

        /// <summary>Después de cambios: el dispositivo responde y tiene pantalla de inicio. Si no, reactiva launchers.</summary>
        public bool VerificarSeguridad()
        {
            if (!Adb.Responde()) { Log("AVISO: el dispositivo no responde tras los cambios."); return false; }
            string inicio = null;
            var cands = new List<string>();
            try { inicio = Adb.InicioActual(); cands = Adb.CandidatosInicio(); }
            catch (AdbException) { }
            if (inicio != null || cands.Count > 0) return true;
            Log("AVISO: el dispositivo quedó sin pantalla de inicio. Reactivando launchers...");
            foreach (var n in new List<string>(Historial.Datos.actions.Keys))
            {
                string low = n.ToLowerInvariant();
                if (low.Contains("launcher") || low.Contains("tvhome") || low.Contains("home"))
                {
                    string t;
                    Restaurar(n, out t);
                }
            }
            return false;
        }

        // ───────── ajustes reversibles ─────────
        public void CambiarAjuste(string ns, string clave, string valor)
        {
            string anterior = Adb.LeerAjuste(ns, clave);
            Historial.RecordarAjuste(ns, clave, anterior);
            Adb.PonerAjuste(ns, clave, valor);
            Historial.EscribirBatRestauracion(Adb.Ruta, Adb.Serial);
        }

        public void RestaurarAjuste(string ns, string clave)
        {
            string orig;
            if (!Historial.Datos.settings.TryGetValue(ns + "/" + clave, out orig)) return;
            Adb.PonerAjuste(ns, clave, orig);
            Historial.OlvidarAjuste(ns, clave);
        }

        // ───────── launcher ─────────
        public bool CambiarLauncher(string componente, bool desactivarAnterior, out string mensaje)
        {
            string anterior = Adb.InicioActual();
            string t;
            if (!Adb.PonerInicio(componente, out t)) { mensaje = "El dispositivo no aceptó el cambio de inicio: " + t; return false; }
            Adb.Tecla("KEYCODE_HOME");
            Thread.Sleep(2000);
            string ahora = Adb.InicioActual();
            string nuevoPkg = componente.Split('/')[0];
            if (ahora == null || ahora.Split('/')[0] != nuevoPkg)
            {
                mensaje = "El dispositivo no confirmó el nuevo launcher. No se desactivó nada.";
                if (EsFireTv) mensaje += "\nFire OS 7.6 o superior no permite cambiar la pantalla de inicio.";
                return false;
            }
            mensaje = "Nuevo inicio: " + componente;
            if (desactivarAnterior && anterior != null && anterior.Split('/')[0] != nuevoPkg)
            {
                string viejo = anterior.Split('/')[0];
                var c = BaseDatos.Clasificar(viejo, true, null);
                if (c.N == Nivel.Bloqueado && !BaseDatos.Curados.ContainsKey(viejo))
                {
                    mensaje += " (el anterior " + viejo + " es crítico; se dejó activo)";
                }
                else if (Adb.Desactivar(viejo, out t))
                {
                    Historial.AgregarAccion(viejo, "desactivada", null);
                    Adb.Tecla("KEYCODE_HOME");
                    Thread.Sleep(2000);
                    if (Adb.InicioActual() == null)
                    {
                        Adb.Activar(viejo, out t);
                        Historial.QuitarAccion(viejo);
                        mensaje = "Sin el launcher anterior el dispositivo no tenía inicio; se reactivó.";
                        return false;
                    }
                    mensaje += "\nLauncher anterior desactivado: " + viejo;
                }
                else mensaje += "\nNo se pudo desactivar " + viejo + ": " + t;
            }
            Historial.EscribirBatRestauracion(Adb.Ruta, Adb.Serial);
            return true;
        }
    }
}
