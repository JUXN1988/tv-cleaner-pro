// Clasificación de paquetes: decide categoría y nivel de riesgo de cada app del TV.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace TVCleanerPro
{
    public enum Nivel { Bloqueado = 0, Seguro = 1, Precaucion = 2, Experto = 3 }

    public class Curado
    {
        public string Cat;
        public Nivel N;
        public string Desc;
    }

    public class Clasificacion
    {
        public string Cat;
        public Nivel N;
        public string Desc;
        public string Fuente;

        public Clasificacion(string cat, Nivel n, string desc, string fuente)
        {
            Cat = cat; N = n; Desc = desc; Fuente = fuente;
        }
    }

    public class InfoPaquete
    {
        public string Nombre;
        public string Cat;
        public Nivel N;
        public string Desc;
        public string Fuente;
        public bool Sistema;
        public string Estado;   // "activa", "desactivada", "eliminada"
    }

    static partial class BaseDatos
    {
        public static readonly Dictionary<string, Curado> Curados = new Dictionary<string, Curado>();
        public static readonly Dictionary<string, string> Nombres = new Dictionary<string, string>();
        static Dictionary<string, string[]> uad;

        public static readonly string[] OrdenCategorias = new string[]
        {
            "TELEMETRÍA", "PUBLICIDAD", "STREAMING", "JUEGOS", "GOOGLE", "FABRICANTE", "BLOATWARE", "APPS INSTALADAS", "SISTEMA"
        };

        public static readonly Dictionary<string, string> DescCategorias = new Dictionary<string, string>
        {
            { "TELEMETRÍA", "Rastreo, reportes y recopilación de datos" },
            { "PUBLICIDAD", "Anuncios, promociones y contenido patrocinado" },
            { "STREAMING", "Servicios de video y música (decide tú)" },
            { "JUEGOS", "Juegos preinstalados" },
            { "GOOGLE", "Apps opcionales de Google" },
            { "FABRICANTE", "Apps de la marca del TV" },
            { "BLOATWARE", "Relleno del sistema que no se usa en un TV" },
            { "APPS INSTALADAS", "Apps descargadas o agregadas al TV" },
            { "SISTEMA", "Componentes del sistema" },
        };

        public static string TextoNivel(Nivel n)
        {
            switch (n)
            {
                case Nivel.Bloqueado: return "Bloqueado";
                case Nivel.Seguro: return "Seguro";
                case Nivel.Precaucion: return "Precaución";
                default: return "Experto";
            }
        }

        public static string ExplicaNivel(Nivel n)
        {
            switch (n)
            {
                case Nivel.Bloqueado: return "Componente esencial. TV Cleaner no permite tocarlo porque podría dañar el TV.";
                case Nivel.Seguro: return "Se puede desactivar sin afectar el funcionamiento del TV.";
                case Nivel.Precaucion: return "Se puede desactivar, pero perderás la función que ofrece. Revisa la descripción.";
                default: return "Solo en Modo experto. Puede afectar funciones del sistema; investiga antes.";
            }
        }

        static BaseDatos()
        {
            CargarCurados();
            CargarNombres();
        }

        static void C(string pkg, string cat, int nivel, string desc)
        {
            Curados[pkg] = new Curado { Cat = cat, N = (Nivel)nivel, Desc = desc };
        }

        /// <summary>Lista comunitaria UAD (incluida dentro del .exe como recurso).</summary>
        public static Dictionary<string, string[]> Uad
        {
            get
            {
                if (uad == null) uad = CargarUad();
                return uad;
            }
        }

        static Dictionary<string, string[]> CargarUad()
        {
            var res = new Dictionary<string, string[]>();
            try
            {
                Stream s = null;
                string externo = Path.Combine(Utilidades.CarpetaPrograma(), "uad_lists.json.gz");
                if (File.Exists(externo)) s = File.OpenRead(externo);   // permite actualizar sin recompilar
                else s = Assembly.GetExecutingAssembly().GetManifestResourceStream("uad_lists.json.gz");
                if (s == null) return res;
                string json;
                using (s)
                using (var gz = new GZipStream(s, CompressionMode.Decompress))
                using (var rd = new StreamReader(gz, Encoding.UTF8))
                    json = rd.ReadToEnd();
                var ser = new JavaScriptSerializer();
                ser.MaxJsonLength = int.MaxValue;
                var obj = ser.Deserialize<Dictionary<string, object>>(json);
                foreach (var kv in obj)
                {
                    // formato compacto: ["Google","Recommended","desc"]  ·  formato original UAD: {"list":..,"removal":..,"description":..}
                    var arr = kv.Value as System.Collections.IList;
                    var dic = kv.Value as IDictionary<string, object>;
                    if (arr != null && arr.Count >= 3)
                        res[kv.Key] = new string[] { Convert.ToString(arr[0]), Convert.ToString(arr[1]), Convert.ToString(arr[2]) };
                    else if (dic != null && dic.ContainsKey("removal"))
                    {
                        string d = dic.ContainsKey("description") ? Convert.ToString(dic["description"]) : "";
                        if (d.Length > 400) d = d.Substring(0, 400);
                        res[kv.Key] = new string[] { dic.ContainsKey("list") ? Convert.ToString(dic["list"]) : "", Convert.ToString(dic["removal"]), d.Trim() };
                    }
                }
            }
            catch (Exception)
            {
            }
            return res;
        }

        static readonly string[] Ignorar = { "com", "android", "google", "tv", "app", "apps", "org", "net" };

        public static string NombreAmigable(string pkg)
        {
            string n;
            if (Nombres.TryGetValue(pkg, out n)) return n;
            var partes = new List<string>();
            foreach (var p in pkg.Split('.'))
                if (Array.IndexOf(Ignorar, p) < 0) partes.Add(p);
            if (partes.Count == 0) return pkg;
            int desde = Math.Max(0, partes.Count - 2);
            var sb = new StringBuilder();
            for (int i = desde; i < partes.Count; i++)
            {
                string w = partes[i].Replace("_", " ");
                if (w.Length > 0) w = char.ToUpper(w[0]) + w.Substring(1).ToLower();
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(w);
            }
            return sb.ToString();
        }

        static bool Contiene(string texto, string[] lista)
        {
            foreach (var p in lista)
                if (texto.Contains(p)) return true;
            return false;
        }

        /// <summary>Decide categoría y nivel. El orden de las reglas importa: lo primero que coincide gana.</summary>
        public static Clasificacion Clasificar(string nombre, bool sistema, string razonProteccion)
        {
            Curado cur;
            if (razonProteccion != null)
            {
                string cat = Curados.TryGetValue(nombre, out cur) ? cur.Cat : (sistema ? "SISTEMA" : "APPS INSTALADAS");
                return new Clasificacion(cat, Nivel.Bloqueado, razonProteccion, "protección automática");
            }
            if (Curados.TryGetValue(nombre, out cur))
                return new Clasificacion(cur.Cat, cur.N, cur.Desc, "TV Cleaner");

            string low = nombre.ToLowerInvariant();
            if (Contiene(low, PatronesCriticos))
                return new Clasificacion("SISTEMA", Nivel.Bloqueado,
                    "Componente crítico (teclado, control, entradas, red o ajustes).", "regla de seguridad");

            foreach (var p in Prefijos)
                if (nombre.StartsWith(p[0], StringComparison.Ordinal))
                    return new Clasificacion(p[1], (Nivel)int.Parse(p[2]), p[3], "TV Cleaner");

            string[] u;
            if (Uad.TryGetValue(nombre, out u))
            {
                Nivel n;
                switch (u[1])
                {
                    case "Recommended": n = Nivel.Seguro; break;
                    case "Advanced": n = Nivel.Precaucion; break;
                    case "Unsafe": n = Nivel.Bloqueado; break;
                    default: n = Nivel.Experto; break;
                }
                string dl = " " + u[2].ToLowerInvariant();
                string cat;
                if (Contiene(dl, PalabrasTelemetria)) cat = "TELEMETRÍA";
                else if (Contiene(dl, PalabrasPublicidad)) cat = "PUBLICIDAD";
                else if (u[0] == "Google") cat = "GOOGLE";
                else if (u[0] == "Oem" || u[0] == "Carrier") cat = "FABRICANTE";
                else if (u[0] == "Aosp") cat = n == Nivel.Seguro ? "BLOATWARE" : "SISTEMA";
                else cat = sistema ? "FABRICANTE" : "APPS INSTALADAS";
                if (n == Nivel.Bloqueado) cat = "SISTEMA";
                string texto = string.IsNullOrEmpty(u[2]) ? "Sin descripción." : u[2];
                return new Clasificacion(cat, n, "[Lista comunitaria UAD] " + texto, "UAD");
            }

            if (!sistema)
                return new Clasificacion("APPS INSTALADAS", Nivel.Precaucion,
                    "App instalada en el TV (no es parte del sistema). Puedes reinstalarla desde Play Store.", "detección");

            if (Contiene(low, PatronesSensibles))
                return new Clasificacion("SISTEMA", Nivel.Bloqueado,
                    "Componente del sistema no identificado con nombre sensible. Bloqueado por seguridad.", "regla de seguridad");

            return new Clasificacion("SISTEMA", Nivel.Experto,
                "Paquete del sistema no identificado. Solo en Modo experto; investiga el nombre antes.", "detección");
        }
    }
}
