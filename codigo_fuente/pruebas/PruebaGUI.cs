// Prueba automática de la INTERFAZ: abre la ventana real y la maneja por código contra el dispositivo simulado.
// Guarda capturas en la carpeta indicada por GUI_OUT (si existe ImageMagick 'import') y un resumen en resultado.txt
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TVCleanerPro
{
    static class PruebaGUI
    {
        static VentanaPrincipal v;
        static readonly List<string> resultados = new List<string>();
        static readonly Queue<Func<bool>> pasos = new Queue<Func<bool>>();
        static string salida;

        static void Afirmar(bool c, string q) { resultados.Add((c ? "ok    " : "FALLA ") + q); }

        static void Captura(string nombre)
        {
            Application.DoEvents();
            try
            {
                var p = Process.Start(new ProcessStartInfo("import", "-window root \"" + Path.Combine(salida, nombre + ".png") + "\"") { UseShellExecute = false });
                p.WaitForExit(10000);
            }
            catch (Exception) { }
        }

        static bool Libre() { return !v.Ocupado && v.cancelarEscaneo == null; }

        static string EstadoTv() { return File.ReadAllText(Environment.GetEnvironmentVariable("FAKEADB_STATE")); }

        [STAThread]
        static void Main()
        {
            salida = Environment.GetEnvironmentVariable("GUI_OUT") ?? ".";
            Directory.CreateDirectory(salida);
            Dialogo.Automatico = true;
            Application.EnableVisualStyles();
            v = new VentanaPrincipal();
            string apk = Path.Combine(salida, "mi_app-debug.apk");
            File.WriteAllText(apk, "FAKEAPK:com.gabriel.miapp:TEST_ONLY");

            pasos.Enqueue(() => { Captura("01_inicio"); v.cmbDispositivo.Text = "192.168.1.2"; v.Conectar(); return true; });
            pasos.Enqueue(() => Libre() && v.motor.Paquetes.Count > 0);
            pasos.Enqueue(() =>
            {
                Afirmar(v.motor.Adb.Serial == "192.168.1.2:5555", "conectado");
                Afirmar(v.lvPaquetes.Items.Count > 40, "lista llena: " + v.lvPaquetes.Items.Count);
                v.MarcarRecomendadas();
                Afirmar(v.marcadas.Count >= 5, "recomendadas marcadas: " + v.marcadas.Count);
                Afirmar(v.marcadas.All(n => v.motor.Paquetes[n].N == Nivel.Seguro), "solo nivel Seguro");
                // intentar marcar algo bloqueado
                var gms = v.lvPaquetes.Items.Cast<ListViewItem>().First(i => (string)i.Tag == "com.google.android.gms");
                gms.Checked = true;
                Afirmar(!v.marcadas.Contains("com.google.android.gms") && !gms.Checked, "gms no se puede marcar");
                var alph = v.lvPaquetes.Items.Cast<ListViewItem>().First(i => (string)i.Tag == "tv.alphonso.alphonso_eula");
                alph.Selected = true;
                alph.EnsureVisible();
                Captura("02_limpieza");
                v.Aplicar();
                return true;
            });
            pasos.Enqueue(Libre);
            pasos.Enqueue(() =>
            {
                Afirmar(EstadoTv().Contains("\"tv.alphonso.alphonso_eula\": {\n  \"system\": true,\n  \"enabled\": false") ||
                        v.motor.Paquetes["tv.alphonso.alphonso_eula"].Estado == "desactivada", "aplicado en el TV");
                Afirmar(v.marcadas.Count == 0, "selección limpia");
                v.MostrarPestana(1);
                v.CargarApps();
                return true;
            });
            pasos.Enqueue(Libre);
            pasos.Enqueue(() =>
            {
                v.chkPrueba.Checked = true;
                v.InstalarArchivos(new[] { apk });
                return true;
            });
            pasos.Enqueue(Libre);
            pasos.Enqueue(() =>
            {
                Afirmar(v.lvApps.Items.Cast<ListViewItem>().Any(i => (string)i.Tag == "com.gabriel.miapp"), "app propia instalada y listada");
                var it = v.lvApps.Items.Cast<ListViewItem>().First(i => (string)i.Tag == "com.gabriel.miapp");
                it.Selected = true;
                return true;
            });
            pasos.Enqueue(() => { System.Threading.Thread.Sleep(1500); Application.DoEvents(); Captura("03_mis_apps"); v.MostrarPestana(2); v.txtRango.Text = "127.0.0.1-6"; v.EscanearRed(); return true; });
            pasos.Enqueue(Libre);
            pasos.Enqueue(() =>
            {
                Afirmar(v.ultimoEscaneo.Count >= 3, "escáner encontró " + v.ultimoEscaneo.Count + " equipos");
                Captura("04_red");
                v.MostrarPestana(3);
                return true;
            });
            pasos.Enqueue(() => { Captura("05_optimizar"); v.MostrarPestana(4); return true; });
            pasos.Enqueue(() => { Captura("06_launcher"); v.MostrarPestana(5); return true; });
            pasos.Enqueue(() => { Captura("07_control"); v.MostrarPestana(6); return true; });
            pasos.Enqueue(() =>
            {
                Afirmar(v.lvHistorial.Items.Count >= 5, "historial con " + v.lvHistorial.Items.Count + " cambios");
                Captura("08_respaldos");
                v.DeshacerTodo();
                return true;
            });
            pasos.Enqueue(Libre);
            pasos.Enqueue(() =>
            {
                Afirmar(v.motor.Historial.Datos.actions.Count == 0, "deshacer todo vació el historial");
                return true;
            });
            pasos.Enqueue(Libre);
            pasos.Enqueue(() =>
            {
                v.MostrarPestana(7);
                Captura("09_registro");
                v.MostrarPestana(0);
                v.chkExperto.Checked = true;
                Captura("10_experto");
                var errores = Dialogo.Registro.Where(x => x.StartsWith("ERROR")).ToList();
                Afirmar(errores.Count == 0, "sin errores en diálogos" + (errores.Count > 0 ? ": " + string.Join(" | ", errores) : ""));
                File.WriteAllLines(Path.Combine(salida, "dialogos.txt"), Dialogo.Registro);
                return true;
            });

            var t = new Timer { Interval = 300 };
            int esperas = 0;
            t.Tick += (s, e) =>
            {
                if (pasos.Count == 0)
                {
                    t.Stop();
                    File.WriteAllLines(Path.Combine(salida, "resultado.txt"), resultados);
                    v.Close();
                    return;
                }
                t.Stop();
                bool listo;
                try { listo = pasos.Peek()(); }
                catch (Exception ex) { resultados.Add("FALLA excepción: " + ex); listo = true; }
                if (listo) { pasos.Dequeue(); esperas = 0; }
                else if (++esperas > 600) { resultados.Add("FALLA tiempo agotado en un paso"); pasos.Dequeue(); }
                t.Start();
            };
            v.Shown += (s, e) => t.Start();
            Application.Run(v);
        }
    }
}
