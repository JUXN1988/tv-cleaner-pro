// Historial por dispositivo: cada cambio queda guardado para poder deshacerlo.
// Formato JSON compatible con la versión anterior (Python) del programa.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TVCleanerPro
{
    public class Accion
    {
        public string action;            // "desactivada", "eliminada_usuario", "desinstalada"
        public string time;
        public List<string> apks = new List<string>();
    }

    public class DatosHistorial
    {
        public string device;
        public Dictionary<string, Accion> actions = new Dictionary<string, Accion>();
        public Dictionary<string, string> settings = new Dictionary<string, string>();
        public List<string> log = new List<string>();
    }

    public class Historial
    {
        public readonly string Archivo;
        public DatosHistorial Datos = new DatosHistorial();
        readonly object candado = new object();

        public Historial(string carpeta, string claveDispositivo)
        {
            string seguro = Regex.Replace(claveDispositivo, @"[^A-Za-z0-9_.\-]", "_");
            if (seguro.Length > 80) seguro = seguro.Substring(0, 80);
            if (seguro.Length == 0) seguro = "tv";
            Archivo = Path.Combine(carpeta, seguro + ".json");
            Datos.device = claveDispositivo;
            if (File.Exists(Archivo))
            {
                try
                {
                    var d = Utilidades.Json().Deserialize<DatosHistorial>(File.ReadAllText(Archivo, Encoding.UTF8));
                    if (d != null)
                    {
                        if (d.actions == null) d.actions = new Dictionary<string, Accion>();
                        if (d.settings == null) d.settings = new Dictionary<string, string>();
                        if (d.log == null) d.log = new List<string>();
                        foreach (var a in d.actions.Values) if (a.apks == null) a.apks = new List<string>();
                        Datos = d;
                    }
                }
                catch (Exception)
                {
                    File.Copy(Archivo, Path.ChangeExtension(Archivo, ".corrupto.json"), true);
                }
            }
        }

        public void Guardar()
        {
            lock (candado)
                Utilidades.EscribirSeguro(Archivo, Utilidades.Indentar(Utilidades.Json().Serialize(Datos)));
        }

        static string Ahora()
        {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        public void AgregarAccion(string pkg, string accion, List<string> apks)
        {
            lock (candado)
            {
                Datos.actions[pkg] = new Accion { action = accion, time = Ahora(), apks = apks ?? new List<string>() };
                Datos.log.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " " + accion + " " + pkg);
            }
            Guardar();
        }

        public void QuitarAccion(string pkg)
        {
            bool cambio;
            lock (candado)
            {
                cambio = Datos.actions.Remove(pkg);
                if (cambio) Datos.log.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " restaurado " + pkg);
            }
            if (cambio) Guardar();
        }

        public Accion AccionDe(string pkg)
        {
            Accion a;
            lock (candado) return Datos.actions.TryGetValue(pkg, out a) ? a : null;
        }

        /// <summary>Guarda el valor ORIGINAL de un ajuste (solo la primera vez).</summary>
        public void RecordarAjuste(string ns, string clave, string valor)
        {
            string k = ns + "/" + clave;
            lock (candado)
            {
                if (Datos.settings.ContainsKey(k)) return;
                Datos.settings[k] = valor;
            }
            Guardar();
        }

        public void OlvidarAjuste(string ns, string clave)
        {
            lock (candado) Datos.settings.Remove(ns + "/" + clave);
            Guardar();
        }

        /// <summary>Script .bat de emergencia para deshacer todo sin abrir el programa.</summary>
        public string EscribirBatRestauracion(string rutaAdb, string serial)
        {
            string bat = Path.Combine(Path.GetDirectoryName(Archivo), Path.GetFileNameWithoutExtension(Archivo) + "_RESTAURAR.bat");
            var l = new List<string>();
            l.Add("@echo off");
            l.Add("chcp 65001 >nul");
            l.Add("REM Restauracion generada por " + Utilidades.NombreApp + " " + Utilidades.Version);
            l.Add("set ADB=\"" + rutaAdb + "\"");
            l.Add("if not exist %ADB% set ADB=adb");
            if (serial != null && serial.Contains(":")) l.Add("%ADB% connect " + serial);
            l.Add("");
            lock (candado)
            {
                foreach (var kv in Datos.actions)
                {
                    var a = kv.Value;
                    if (a.action == "desactivada") l.Add("%ADB% -s " + serial + " shell pm enable " + kv.Key);
                    else if (a.action == "eliminada_usuario") l.Add("%ADB% -s " + serial + " shell cmd package install-existing " + kv.Key);
                    else if (a.action == "desinstalada" && a.apks.Count > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (var p in a.apks) sb.Append(" \"").Append(p).Append('"');
                        l.Add("%ADB% -s " + serial + (a.apks.Count == 1 ? " install -r" : " install-multiple -r") + sb);
                    }
                }
                foreach (var kv in Datos.settings)
                {
                    var p = kv.Key.Split(new[] { '/' }, 2);
                    if (kv.Value == "null" || kv.Value.Length == 0) l.Add("%ADB% -s " + serial + " shell settings delete " + p[0] + " " + p[1]);
                    else l.Add("%ADB% -s " + serial + " shell settings put " + p[0] + " " + p[1] + " " + kv.Value);
                }
            }
            l.Add("");
            l.Add("echo Restauracion terminada.");
            l.Add("pause");
            File.WriteAllText(bat, string.Join("\r\n", l), new UTF8Encoding(false));
            return bat;
        }
    }
}
