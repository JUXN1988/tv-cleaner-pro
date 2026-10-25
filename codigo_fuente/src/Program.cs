// Punto de entrada del programa.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: AssemblyTitle("TV Cleaner Pro")]
[assembly: AssemblyDescription("Limpieza segura y administración de Android TV, Google TV, Fire TV y TV Box por ADB")]
[assembly: AssemblyProduct("TV Cleaner Pro")]
[assembly: AssemblyCompany("Gabriel")]
[assembly: AssemblyCopyright("Uso personal")]
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]

namespace TVCleanerPro
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            if (Utilidades.EsWindows && Environment.OSVersion.Version.Major >= 6)
            {
                try { SetProcessDPIAware(); } catch (Exception) { }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Fallo(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Fallo(e.ExceptionObject as Exception);
            Application.Run(new VentanaPrincipal());
        }

        static void Fallo(Exception e)
        {
            string texto = e != null ? e.ToString() : "Error desconocido";
            try
            {
                string ruta = Path.Combine(Path.Combine(Utilidades.CarpetaDatos(), "registros"), "error_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                File.WriteAllText(ruta, texto);
                texto += "\n\nDetalle guardado en:\n" + ruta;
            }
            catch (Exception) { }
            MessageBox.Show("Ocurrió un error inesperado:\n\n" + Utilidades.Recortar(texto, 1500), Utilidades.NombreApp,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
