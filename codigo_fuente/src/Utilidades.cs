// Funciones de apoyo: rutas, validaciones de seguridad y JSON.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace TVCleanerPro
{
    public static class Utilidades
    {
        public const string NombreApp = "TV Cleaner Pro";
        public const string Version = "3.0.0";

        static readonly Regex RxPaquete = new Regex(@"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z0-9_]+)+$");
        public static readonly Regex RxComponente = new Regex(@"^([A-Za-z][A-Za-z0-9_.]*)/([A-Za-z0-9_.$]+)$");
        static readonly Regex RxHost = new Regex(@"^[A-Za-z0-9][A-Za-z0-9.\-]{0,252}$");

        public static bool EsWindows
        {
            get { return Environment.OSVersion.Platform == PlatformID.Win32NT; }
        }

        public static string CarpetaPrograma()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>Documentos\TV Cleaner Pro (respeta la redirección a OneDrive).</summary>
        public static string CarpetaDatos()
        {
            string d = Environment.GetEnvironmentVariable("TVCLEANER_DATA");
            if (string.IsNullOrEmpty(d))
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (string.IsNullOrEmpty(docs)) docs = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                d = Path.Combine(docs, NombreApp);
            }
            foreach (var sub in new[] { "", "historial", "respaldos_apk", "capturas", "registros" })
                Directory.CreateDirectory(Path.Combine(d, sub));
            return d;
        }

        public static string CarpetaLocal()
        {
            string d = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(d)) d = Path.GetTempPath();
            return Path.Combine(d, "TVCleanerPro");
        }

        public static bool PaqueteValido(string p)
        {
            return !string.IsNullOrEmpty(p) && p.Length < 256 && RxPaquete.IsMatch(p);
        }

        public static bool HostValido(string h)
        {
            if (string.IsNullOrEmpty(h)) return false;
            IPAddress ip;
            if (IPAddress.TryParse(h, out ip)) return true;
            return RxHost.IsMatch(h);
        }

        public static bool PuertoValido(string p)
        {
            int n;
            return int.TryParse(p, out n) && n >= 1 && n <= 65535;
        }

        /// <summary>Entrecomilla un argumento según las reglas de Windows (sin pasar por cmd.exe).</summary>
        public static string Comillas(string a)
        {
            if (a.Length > 0 && a.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return a;
            var sb = new StringBuilder("\"");
            int bs = 0;
            foreach (char c in a)
            {
                if (c == '\\') { bs++; continue; }
                if (c == '"') { sb.Append('\\', bs * 2 + 1); sb.Append('"'); bs = 0; continue; }
                sb.Append('\\', bs); bs = 0; sb.Append(c);
            }
            sb.Append('\\', bs * 2);
            sb.Append('"');
            return sb.ToString();
        }

        public static JavaScriptSerializer Json()
        {
            var j = new JavaScriptSerializer();
            j.MaxJsonLength = int.MaxValue;
            return j;
        }

        /// <summary>JSON con sangría para que los archivos se puedan leer y editar a mano.</summary>
        public static string Indentar(string json)
        {
            var sb = new StringBuilder();
            int nivel = 0;
            bool enTexto = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (enTexto)
                {
                    sb.Append(c);
                    if (c == '\\' && i + 1 < json.Length) { sb.Append(json[i + 1]); i++; }
                    else if (c == '"') enTexto = false;
                    continue;
                }
                if (c == '"') { enTexto = true; sb.Append(c); continue; }
                switch (c)
                {
                    case '{':
                    case '[':
                        if (i + 1 < json.Length && (json[i + 1] == '}' || json[i + 1] == ']'))
                        {
                            sb.Append(c); sb.Append(json[i + 1]); i++; break;
                        }
                        sb.Append(c); sb.Append('\n'); nivel++; sb.Append(' ', nivel * 2); break;
                    case '}':
                    case ']':
                        sb.Append('\n'); nivel--; sb.Append(' ', nivel * 2); sb.Append(c); break;
                    case ',':
                        sb.Append(c); sb.Append('\n'); sb.Append(' ', nivel * 2); break;
                    case ':':
                        sb.Append(": "); break;
                    default:
                        sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        public static void EscribirSeguro(string ruta, string contenido)
        {
            string tmp = ruta + ".tmp";
            File.WriteAllText(tmp, contenido, new UTF8Encoding(false));
            if (File.Exists(ruta)) File.Delete(ruta);
            File.Move(tmp, ruta);
        }

        public static string Recortar(string s, int max)
        {
            if (s == null) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        public static List<string> Lineas(string s)
        {
            var res = new List<string>();
            if (string.IsNullOrEmpty(s)) return res;
            foreach (var l in s.Replace("\r\n", "\n").Split('\n')) res.Add(l);
            return res;
        }
    }
}
