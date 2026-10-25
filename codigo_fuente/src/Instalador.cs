// Instalación de apps propias o descargadas: APK sueltos, APK divididos y paquetes .apks / .xapk / .apkm.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace TVCleanerPro
{
    public class Instalador
    {
        readonly Adb adb;
        public Action<string> Log = delegate { };

        public Instalador(Adb adb)
        {
            this.adb = adb;
        }

        public static readonly string[] Extensiones = { ".apk", ".apks", ".xapk", ".apkm" };

        public static bool EsInstalable(string ruta)
        {
            string e = Path.GetExtension(ruta).ToLowerInvariant();
            return Array.IndexOf(Extensiones, e) >= 0;
        }

        /// <summary>Traduce los códigos de error de Android a una explicación y solución en español.</summary>
        public static string Explicar(string salida)
        {
            var m = Regex.Match(salida ?? "", @"(INSTALL_[A-Z_]+|DELETE_[A-Z_]+)");
            if (!m.Success) return Utilidades.Recortar(salida, 300);
            string c = m.Groups[1].Value;
            string ex;
            switch (c)
            {
                case "INSTALL_FAILED_TEST_ONLY": ex = "Es una app de prueba (compilación de depuración). Marca 'Permitir apps de prueba/debug (-t)'."; break;
                case "INSTALL_FAILED_VERSION_DOWNGRADE": ex = "Ya hay una versión más nueva instalada. Marca 'Permitir versión anterior (-d)'."; break;
                case "INSTALL_FAILED_UPDATE_INCOMPATIBLE": ex = "La app instalada tiene otra firma (por ejemplo, la de la tienda vs. tu compilación). Desinstálala primero desde 'Mis apps' y vuelve a instalar."; break;
                case "INSTALL_FAILED_ALREADY_EXISTS": ex = "La app ya existe. Marca 'Reemplazar conservando datos (-r)'."; break;
                case "INSTALL_FAILED_INSUFFICIENT_STORAGE": ex = "No hay espacio suficiente. Libera espacio (pestaña Optimizar o desinstala apps)."; break;
                case "INSTALL_FAILED_NO_MATCHING_ABIS": ex = "El APK no es para el procesador de este dispositivo (ej. solo x86 o solo arm64). Usa la versión armeabi-v7a/arm64-v8a correcta o un APK 'universal'."; break;
                case "INSTALL_FAILED_OLDER_SDK": ex = "La app necesita una versión de Android más nueva que la de este dispositivo (revisa minSdk)."; break;
                case "INSTALL_FAILED_MISSING_SPLIT": ex = "Faltan partes del APK dividido. Instala el paquete .apks/.xapk completo o selecciona todos los APK juntos."; break;
                case "INSTALL_PARSE_FAILED_NO_CERTIFICATES": ex = "El APK no está firmado. En Android Studio usa Build > Generate Signed APK, o instala la versión debug."; break;
                case "INSTALL_FAILED_INVALID_APK": ex = "El APK está dañado o incompleto. Vuelve a generarlo o descargarlo."; break;
                case "INSTALL_PARSE_FAILED_NOT_APK": ex = "El archivo no es un APK válido."; break;
                case "INSTALL_FAILED_DUPLICATE_PERMISSION": ex = "Otra app ya define el mismo permiso personalizado. Desinstala la otra versión."; break;
                case "INSTALL_FAILED_USER_RESTRICTED": ex = "El dispositivo bloqueó la instalación. Activa 'Apps de origen desconocido' / 'Instalar vía USB' en Opciones de desarrollador."; break;
                case "INSTALL_FAILED_VERIFICATION_FAILURE": ex = "Play Protect rechazó la app. Desactiva 'Verificar apps instaladas por USB/ADB' en Opciones de desarrollador si confías en ella."; break;
                case "INSTALL_FAILED_DEPRECATED_SDK_VERSION": ex = "La app tiene un targetSdk muy antiguo y Android 14+ la bloquea. Sube targetSdkVersion en tu proyecto (23 o más)."; break;
                default: ex = "Código de Android: " + c; break;
            }
            return c + "\n" + ex;
        }

        public class Resultado
        {
            public string Archivo;
            public bool Ok;
            public string Mensaje;
        }

        /// <summary>
        /// Instala una lista de archivos. Cada .apk suelto se instala por separado, salvo que
        /// 'juntos' sea verdadero (APK divididos de la misma app). Los paquetes se descomprimen solos.
        /// </summary>
        public List<Resultado> Instalar(IList<string> archivos, string[] opciones, bool juntos, Action<int, int, string> progreso)
        {
            var res = new List<Resultado>();
            var grupos = new List<List<string>>();
            var apksSueltos = new List<string>();
            foreach (var a in archivos)
            {
                if (Path.GetExtension(a).ToLowerInvariant() == ".apk") apksSueltos.Add(a);
                else grupos.Add(new List<string> { a });
            }
            if (juntos && apksSueltos.Count > 0) grupos.Insert(0, apksSueltos);
            else foreach (var a in apksSueltos) grupos.Add(new List<string> { a });

            int i = 0;
            foreach (var g in grupos)
            {
                i++;
                string nombre = g.Count == 1 ? Path.GetFileName(g[0]) : g.Count + " APK divididos";
                if (progreso != null) progreso(i, grupos.Count, nombre);
                var r = new Resultado { Archivo = nombre };
                try
                {
                    if (g.Count == 1 && Path.GetExtension(g[0]).ToLowerInvariant() != ".apk")
                        InstalarPaquete(g[0], opciones, r);
                    else
                    {
                        var salida = adb.Instalar(g, opciones);
                        r.Ok = salida.Todo.ToLowerInvariant().Contains("success");
                        r.Mensaje = r.Ok ? "Instalada correctamente." : Explicar(salida.Todo);
                    }
                }
                catch (Exception e)
                {
                    r.Ok = false;
                    r.Mensaje = e.Message;
                }
                Log((r.Ok ? "OK  instalada: " : "ERR instalar ") + nombre + (r.Ok ? "" : ": " + r.Mensaje.Replace("\n", " ")));
                res.Add(r);
            }
            return res;
        }

        /// <summary>.apks (SAI/bundletool), .xapk (APKPure) y .apkm (APKMirror) son ZIP con varios APK y, a veces, datos OBB.</summary>
        void InstalarPaquete(string archivo, string[] opciones, Resultado r)
        {
            string tmp = Path.Combine(Path.GetTempPath(), "tvcleaner_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var apks = new List<string>();
                var obbs = new List<string[]>();   // {local, remoto}
                using (var zip = ZipFile.OpenRead(archivo))
                {
                    foreach (var e in zip.Entries)
                    {
                        if (e.Length == 0 || e.FullName.EndsWith("/")) continue;
                        string nombre = e.FullName.Replace('\\', '/');
                        if (nombre.Contains("..")) continue;                       // evita rutas maliciosas
                        string ext = Path.GetExtension(nombre).ToLowerInvariant();
                        if (ext == ".apk")
                        {
                            string local = Path.Combine(tmp, apks.Count.ToString("00") + "_" + Path.GetFileName(nombre));
                            e.ExtractToFile(local);
                            apks.Add(local);
                        }
                        else if (ext == ".obb" && nombre.StartsWith("Android/obb/", StringComparison.OrdinalIgnoreCase))
                        {
                            string local = Path.Combine(tmp, Path.GetFileName(nombre));
                            e.ExtractToFile(local);
                            obbs.Add(new[] { local, "/sdcard/" + nombre });
                        }
                    }
                }
                if (apks.Count == 0) throw new Exception("El paquete no contiene ningún APK.");
                // en .apks de bundletool puede venir "toc.pb" y un "universal.apk": si existe, basta con él
                var universal = apks.Find(a => Path.GetFileName(a).ToLowerInvariant().EndsWith("universal.apk"));
                if (universal != null) apks = new List<string> { universal };
                var salida = adb.Instalar(apks, opciones);
                r.Ok = salida.Todo.ToLowerInvariant().Contains("success");
                r.Mensaje = r.Ok ? "Instalada correctamente (" + apks.Count + " APK)." : Explicar(salida.Todo);
                if (r.Ok)
                {
                    foreach (var o in obbs)
                    {
                        var e = adb.Enviar(o[0], o[1]);
                        r.Mensaje += e.Codigo == 0 ? "\nDatos OBB copiados." : "\nNo se pudieron copiar los datos OBB: " + e.Todo;
                    }
                }
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch (Exception) { }
            }
        }
    }
}
