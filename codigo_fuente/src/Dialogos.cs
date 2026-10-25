// Ventanas de diálogo: avisos, confirmación de cambios, emparejamiento y visor de texto (logcat).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TVCleanerPro
{
    static class Dialogo
    {
        /// <summary>Solo para pruebas automáticas: responde "sí" sin mostrar ventanas.</summary>
        public static bool Automatico = false;
        public static readonly List<string> Registro = new List<string>();

        public static void Info(IWin32Window w, string texto)
        {
            Registro.Add("INFO: " + texto);
            if (!Automatico) MessageBox.Show(w, texto, Utilidades.NombreApp, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Aviso(IWin32Window w, string texto)
        {
            Registro.Add("AVISO: " + texto);
            if (!Automatico) MessageBox.Show(w, texto, Utilidades.NombreApp, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Error(IWin32Window w, string titulo, string texto)
        {
            Registro.Add("ERROR: " + titulo + ": " + texto);
            if (!Automatico) MessageBox.Show(w, titulo + "\n\n" + texto, Utilidades.NombreApp, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool Pregunta(IWin32Window w, string texto, bool peligro = false)
        {
            Registro.Add("PREGUNTA: " + texto);
            if (Automatico) return true;
            return MessageBox.Show(w, texto, Utilidades.NombreApp, MessageBoxButtons.YesNo,
                peligro ? MessageBoxIcon.Warning : MessageBoxIcon.Question) == DialogResult.Yes;
        }

        static Form Base(string titulo, int ancho, int alto)
        {
            var f = new Form();
            f.Text = titulo;
            f.BackColor = Tema.Fondo;
            f.ForeColor = Tema.Texto;
            f.Font = Tema.Base;
            f.StartPosition = FormStartPosition.CenterParent;
            f.FormBorderStyle = FormBorderStyle.Sizable;
            f.MinimizeBox = false;
            f.ShowInTaskbar = false;
            f.ClientSize = new Size(ancho, alto);
            f.Padding = new Padding(14);
            return f;
        }

        /// <summary>Lista de cambios a aplicar; si hay apps de riesgo exige marcar una casilla.</summary>
        public static bool ConfirmarCambios(IWin32Window w, IList<InfoPaquete> paquetes, string modo)
        {
            int riesgosas = 0;
            foreach (var p in paquetes) if (p.N >= Nivel.Precaucion) riesgosas++;
            Registro.Add("CONFIRMAR: " + paquetes.Count + " (" + modo + ")");
            if (Automatico) return true;

            var f = Base("Confirmar cambios", 660, 520);
            string accion = modo == "desactivar" ? "DESACTIVAR" : "ELIMINAR (para el usuario)";
            var titulo = Tema.Etiqueta("Vas a " + accion + " " + paquetes.Count + " app(s)", Tema.Subtitulo);
            titulo.Dock = DockStyle.Top;
            var expl = Tema.Etiqueta(modo == "desactivar"
                ? "Desactivar: la app deja de ejecutarse y desaparece del menú. Se reactiva en un clic."
                : "Eliminar: las apps del sistema se quitan solo para tu usuario (se restauran desde TV Cleaner). " +
                  "Las apps instaladas se desinstalan guardando antes su APK en la PC.", Tema.Pequena, Tema.Tenue);
            expl.Dock = DockStyle.Top;
            expl.AutoSize = false;
            expl.Height = 40;
            var lista = Tema.Detalle();
            lista.Dock = DockStyle.Fill;
            lista.Font = Tema.Mono;
            var orden = new List<InfoPaquete>(paquetes);
            orden.Sort((a, b) => a.N != b.N ? ((int)b.N).CompareTo((int)a.N) : string.CompareOrdinal(a.Nombre, b.Nombre));
            foreach (var p in orden)
            {
                Tema.Agregar(lista, BaseDatos.TextoNivel(p.N).PadRight(12), Tema.Mono, Tema.ColorNivel(p.N));
                Tema.Agregar(lista, p.Nombre + "\n", Tema.Mono);
            }
            var abajo = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, BackColor = Tema.Fondo };
            var casilla = Tema.Casilla("Entiendo que " + riesgosas + " de estas apps quitan funciones (Precaución/Experto)", false);
            var aplicar = Tema.Boton("Aplicar", null, Tema.EstiloBoton.Acento);
            var cancelar = Tema.Boton("Cancelar", null);
            aplicar.Click += (s, e) =>
            {
                if (riesgosas > 0 && !casilla.Checked) { MessageBox.Show(f, "Marca la casilla de confirmación.", Utilidades.NombreApp); return; }
                f.DialogResult = DialogResult.OK;
            };
            cancelar.Click += (s, e) => f.DialogResult = DialogResult.Cancel;
            abajo.Controls.Add(cancelar);
            abajo.Controls.Add(aplicar);
            if (riesgosas > 0) abajo.Controls.Add(casilla);
            f.Controls.Add(lista);
            f.Controls.Add(expl);
            f.Controls.Add(titulo);
            f.Controls.Add(abajo);
            f.AcceptButton = aplicar;
            f.CancelButton = cancelar;
            return f.ShowDialog(w) == DialogResult.OK;
        }

        /// <summary>Devuelve {ip, puertoVinculacion, codigo, puertoConexion} o null.</summary>
        public static string[] Emparejar(IWin32Window w, string ip)
        {
            var f = Base("Emparejar (Android 11 o superior)", 460, 330);
            var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Tema.Fondo };
            var ayuda = Tema.Etiqueta("En el dispositivo: Opciones de desarrollador > Depuración inalámbrica > " +
                                      "Vincular dispositivo con código de vinculación.", Tema.Pequena, Tema.Tenue);
            ayuda.MaximumSize = new Size(420, 0);
            tabla.Controls.Add(ayuda, 0, 0);
            tabla.SetColumnSpan(ayuda, 2);
            var campos = new TextBox[4];
            string[] nombres = { "IP del dispositivo", "Puerto de vinculación", "Código (6 dígitos)", "Puerto de conexión (opcional)" };
            for (int i = 0; i < 4; i++)
            {
                tabla.Controls.Add(Tema.Etiqueta(nombres[i]), 0, i + 1);
                campos[i] = Tema.CajaTexto(170);
                tabla.Controls.Add(campos[i], 1, i + 1);
            }
            campos[0].Text = ip;
            var ok = Tema.Boton("Emparejar", (s, e) => f.DialogResult = DialogResult.OK, Tema.EstiloBoton.Acento);
            tabla.Controls.Add(ok, 1, 5);
            f.Controls.Add(tabla);
            f.AcceptButton = ok;
            if (f.ShowDialog(w) != DialogResult.OK) return null;
            return new[] { campos[0].Text.Trim(), campos[1].Text.Trim(), campos[2].Text.Trim(), campos[3].Text.Trim() };
        }

        /// <summary>Ventana con texto largo (logcat), con opción de actualizar, buscar y guardar.</summary>
        public static void VentanaTexto(IWin32Window w, string titulo, Func<string> cargar, string carpetaGuardar)
        {
            var f = Base(titulo, 980, 640);
            f.ShowInTaskbar = true;
            f.MinimizeBox = true;
            var caja = new TextBox
            {
                Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, ReadOnly = true, Dock = DockStyle.Fill,
                BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, Font = Tema.Mono, BorderStyle = BorderStyle.None
            };
            var barra = Tema.Fila(Tema.Fondo);
            var buscar = Tema.CajaTexto(220);
            string completo = "";
            Action filtrar = () =>
            {
                string q = buscar.Text.Trim();
                if (q.Length == 0) { caja.Text = completo; return; }
                var l = new List<string>();
                foreach (var linea in completo.Split('\n'))
                    if (linea.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) l.Add(linea);
                caja.Text = string.Join("\r\n", l);
            };
            Action recargar = () =>
            {
                f.Cursor = Cursors.WaitCursor;
                try { completo = (cargar() ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n").Replace("\r\r", "\r"); }
                catch (Exception ex) { completo = "Error: " + ex.Message; }
                f.Cursor = Cursors.Default;
                filtrar();
                caja.SelectionStart = caja.TextLength;
                caja.ScrollToCaret();
            };
            barra.Controls.Add(Tema.Boton("Actualizar", (s, e) => recargar(), Tema.EstiloBoton.Acento));
            barra.Controls.Add(Tema.Etiqueta("Filtrar:"));
            barra.Controls.Add(buscar);
            buscar.TextChanged += (s, e) => filtrar();
            barra.Controls.Add(Tema.Boton("Copiar", (s, e) => { try { Clipboard.SetText(caja.Text.Length > 0 ? caja.Text : " "); } catch (Exception) { } }));
            barra.Controls.Add(Tema.Boton("Guardar en la PC", (s, e) =>
            {
                string ruta = Path.Combine(carpetaGuardar, "logcat_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                File.WriteAllText(ruta, caja.Text);
                MessageBox.Show(f, "Guardado en:\n" + ruta, Utilidades.NombreApp);
            }));
            f.Controls.Add(caja);
            f.Controls.Add(barra);
            f.Shown += (s, e) => recargar();
            f.Show(w);
        }
    }
}
