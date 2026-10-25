// Pestañas "Respaldos" (deshacer cambios) y "Registro".
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TVCleanerPro
{
    public partial class VentanaPrincipal
    {
        internal ListView lvHistorial;
        TextBox txtRegistro;

        Panel ConstruirRespaldos()
        {
            var p = new Panel();
            var arriba = Tema.Fila(Tema.Fondo);
            arriba.Controls.Add(Tema.Etiqueta("Respaldos y restauración", Tema.Subtitulo));
            var d = Tema.Etiqueta("Cada cambio hecho por TV Cleaner queda guardado por dispositivo. Puedes deshacerlo aquí, o con el archivo " +
                                  "_RESTAURAR.bat que se genera en la carpeta 'historial' (funciona aunque no abras el programa).", Tema.Pequena, Tema.Tenue);
            d.MaximumSize = new Size(1100, 0);
            var info = Tema.Fila(Tema.Fondo);
            info.Controls.Add(d);

            lvHistorial = Tema.Lista();
            lvHistorial.Dock = DockStyle.Fill;
            lvHistorial.Columns.Add("Paquete / ajuste", 420);
            lvHistorial.Columns.Add("Cambio", 220);
            lvHistorial.Columns.Add("Fecha", 180);

            var abajo = Tema.Fila(Tema.Panel);
            abajo.Dock = DockStyle.Bottom;
            var b1 = Tema.Boton("Restaurar seleccionados", (s, e) => RestaurarSeleccionHistorial());
            var b2 = Tema.Boton("Deshacer TODO lo hecho por TV Cleaner", (s, e) => DeshacerTodo(), Tema.EstiloBoton.Peligro);
            controlesDispositivo.Add(b1);
            controlesDispositivo.Add(b2);
            abajo.Controls.Add(b1);
            abajo.Controls.Add(b2);
            abajo.Controls.Add(Tema.Boton("Abrir carpeta de respaldos", (s, e) => AbrirRuta(motor.Datos)));

            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 4), BackColor = Tema.Fondo };
            cuerpo.Controls.Add(lvHistorial);
            p.Controls.Add(cuerpo);
            p.Controls.Add(abajo);
            p.Controls.Add(info);
            p.Controls.Add(arriba);
            return p;
        }

        internal void RefrescarHistorial()
        {
            if (lvHistorial == null) return;
            lvHistorial.BeginUpdate();
            lvHistorial.Items.Clear();
            var h = motor.Historial;
            if (h != null)
            {
                var nombres = new Dictionary<string, string>
                {
                    { "desactivada", "Desactivada" }, { "eliminada_usuario", "Eliminada (usuario)" }, { "desinstalada", "Desinstalada (APK respaldado)" }
                };
                foreach (var kv in h.Datos.actions.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    string accion = nombres.ContainsKey(kv.Value.action) ? nombres[kv.Value.action] : kv.Value.action;
                    var it = new ListViewItem(new[] { kv.Key, accion, (kv.Value.time ?? "").Replace("T", " ") });
                    it.Tag = "p:" + kv.Key;
                    lvHistorial.Items.Add(it);
                }
                foreach (var kv in h.Datos.settings.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    var it = new ListViewItem(new[] { kv.Key, "Ajuste (original: " + kv.Value + ")", "" });
                    it.Tag = "s:" + kv.Key;
                    lvHistorial.Items.Add(it);
                }
            }
            lvHistorial.EndUpdate();
        }

        void RestaurarSeleccionHistorial()
        {
            var sel = lvHistorial.SelectedItems.Cast<ListViewItem>().Select(i => (string)i.Tag).ToList();
            if (sel.Count == 0) { Dialogo.Info(this, "Selecciona filas en la lista (Ctrl+clic para varias)."); return; }
            Tarea("Restaurando", () =>
            {
                var r = new Motor.ResultadoLote();
                foreach (var t in sel)
                {
                    string clave = t.Substring(2);
                    if (t.StartsWith("p:"))
                    {
                        string txt;
                        if (motor.Restaurar(clave, out txt)) r.Ok.Add(clave); else r.Fallos.Add(new[] { clave, txt });
                    }
                    else
                    {
                        var partes = clave.Split(new[] { '/' }, 2);
                        motor.RestaurarAjuste(partes[0], partes[1]);
                        r.Ok.Add(clave);
                    }
                }
                return r;
            }, r =>
            {
                RefrescarHistorial();
                LlenarLista();
                Dialogo.Info(this, "Restaurados: " + r.Ok.Count + (r.Fallos.Count > 0 ? "\n\nFallaron:\n" +
                    string.Join("\n", r.Fallos.Select(f => "• " + f[0] + ": " + Utilidades.Recortar(f[1], 80))) : ""));
            }, true);
        }

        internal void DeshacerTodo()
        {
            var h = motor.Historial;
            if (h == null || (h.Datos.actions.Count == 0 && h.Datos.settings.Count == 0))
            {
                Dialogo.Info(this, "No hay cambios de TV Cleaner que deshacer en este dispositivo.");
                return;
            }
            int n = h.Datos.actions.Count + h.Datos.settings.Count;
            if (!Dialogo.Pregunta(this, "¿Deshacer los " + n + " cambios hechos por TV Cleaner en este dispositivo?")) return;
            Tarea("Deshaciendo todo", () => motor.RestaurarTodo(Progreso), r =>
            {
                RefrescarHistorial();
                Dialogo.Info(this, "Restaurados: " + r.Ok.Count + (r.Fallos.Count > 0 ? "\n\nFallaron:\n" +
                    string.Join("\n", r.Fallos.Take(15).Select(f => "• " + f[0] + ": " + Utilidades.Recortar(f[1], 80))) : ""));
                Escanear();
            }, true);
        }

        Panel ConstruirRegistro()
        {
            var p = new Panel();
            txtRegistro = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
                BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, Font = Tema.Mono, BorderStyle = BorderStyle.None
            };
            var abajo = Tema.Fila(Tema.Panel);
            abajo.Dock = DockStyle.Bottom;
            abajo.Controls.Add(Tema.Boton("Abrir carpeta de registros", (s, e) => AbrirRuta(Path.Combine(motor.Datos, "registros"))));
            abajo.Controls.Add(Tema.Boton("Copiar todo", (s, e) => { try { Clipboard.SetText(txtRegistro.Text.Length > 0 ? txtRegistro.Text : " "); } catch (Exception) { } }));
            abajo.Controls.Add(Tema.Boton("Limpiar pantalla", (s, e) => txtRegistro.Clear()));
            p.Controls.Add(txtRegistro);
            p.Controls.Add(abajo);
            return p;
        }

        void AgregarRegistro(string linea)
        {
            if (txtRegistro == null) return;
            if (txtRegistro.TextLength > 400000) txtRegistro.Text = txtRegistro.Text.Substring(200000);
            txtRegistro.AppendText(linea.Replace("\n", Environment.NewLine) + Environment.NewLine);
        }
    }
}
