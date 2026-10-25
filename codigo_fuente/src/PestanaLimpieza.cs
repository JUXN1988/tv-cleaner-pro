// Pestaña "Limpieza": lista de paquetes clasificados, selección y aplicación de cambios.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TVCleanerPro
{
    public partial class VentanaPrincipal
    {
        internal ListView lvPaquetes;
        ListBox lbCategorias;
        RichTextBox rtDetalle;
        internal CheckBox chkExperto;
        ComboBox cmbMostrar;
        TextBox txtBuscar;
        Label lblMarcadas;
        internal RadioButton rbDesactivar, rbEliminar;
        string filtroCategoria;
        List<string> clavesCategorias = new List<string>();
        bool llenando;
        string paqueteDetalle;

        Panel ConstruirLimpieza()
        {
            var p = new Panel();
            var barraSup = Tema.Fila(Tema.Fondo);
            var bEscanear = Tema.Boton("Escanear dispositivo", (s, e) => Escanear(), Tema.EstiloBoton.Acento);
            controlesDispositivo.Add(bEscanear);
            barraSup.Controls.Add(bEscanear);
            barraSup.Controls.Add(Tema.Boton("Marcar recomendadas", (s, e) => MarcarRecomendadas(), Tema.EstiloBoton.Ok));
            barraSup.Controls.Add(Tema.Boton("Desmarcar todo", (s, e) => { marcadas.Clear(); LlenarLista(); }));
            chkExperto = Tema.Casilla("Modo experto", false);
            chkExperto.CheckedChanged += (s, e) => CambioExperto();
            barraSup.Controls.Add(chkExperto);
            barraSup.Controls.Add(Tema.Etiqueta("Mostrar"));
            cmbMostrar = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, FlatStyle = FlatStyle.Flat, Font = Tema.Base };
            cmbMostrar.Items.AddRange(new object[] { "Todas", "Activas", "Desactivadas / eliminadas", "Marcadas" });
            cmbMostrar.SelectedIndex = 0;
            cmbMostrar.SelectedIndexChanged += (s, e) => LlenarLista();
            barraSup.Controls.Add(cmbMostrar);
            barraSup.Controls.Add(Tema.Etiqueta("Buscar"));
            txtBuscar = Tema.CajaTexto(200);
            txtBuscar.TextChanged += (s, e) => LlenarLista();
            barraSup.Controls.Add(txtBuscar);

            var barraInf = Tema.Fila(Tema.Panel);
            barraInf.Dock = DockStyle.Bottom;
            lblMarcadas = Tema.Etiqueta("0 marcadas", Tema.Negrita);
            barraInf.Controls.Add(lblMarcadas);
            barraInf.Controls.Add(Tema.Etiqueta("   Acción:", Tema.Base, Tema.Tenue));
            rbDesactivar = new RadioButton { Text = "Desactivar (recomendado, reversible)", Checked = true, AutoSize = true, ForeColor = Tema.Texto, Margin = new Padding(6, 7, 3, 3) };
            rbEliminar = new RadioButton { Text = "Eliminar para el usuario (reversible)", AutoSize = true, ForeColor = Tema.Texto, Margin = new Padding(6, 7, 3, 3) };
            barraInf.Controls.Add(rbDesactivar);
            barraInf.Controls.Add(rbEliminar);
            var bRestaurar = Tema.Boton("Restaurar las marcadas", (s, e) => RestaurarMarcadas());
            var bAplicar = Tema.Boton("Aplicar a las marcadas", (s, e) => Aplicar(), Tema.EstiloBoton.Acento);
            controlesDispositivo.Add(bRestaurar);
            controlesDispositivo.Add(bAplicar);
            barraInf.Controls.Add(bRestaurar);
            barraInf.Controls.Add(bAplicar);

            lbCategorias = new ListBox
            {
                Dock = DockStyle.Left, Width = 200, BackColor = Tema.Panel, ForeColor = Tema.Texto, BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 26, Font = Tema.Base, IntegralHeight = false
            };
            lbCategorias.DrawItem += DibujarCategoria;
            lbCategorias.SelectedIndexChanged += (s, e) =>
            {
                if (llenando || lbCategorias.SelectedIndex < 0 || lbCategorias.SelectedIndex >= clavesCategorias.Count) return;
                filtroCategoria = clavesCategorias[lbCategorias.SelectedIndex];
                LlenarLista();
            };

            rtDetalle = Tema.Detalle();
            var marcoDetalle = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Tema.Tarjeta, Padding = new Padding(12) };
            rtDetalle.Dock = DockStyle.Fill;
            marcoDetalle.Controls.Add(rtDetalle);

            lvPaquetes = Tema.Lista();
            lvPaquetes.Dock = DockStyle.Fill;
            lvPaquetes.CheckBoxes = true;
            lvPaquetes.Columns.Add("App", 190);
            lvPaquetes.Columns.Add("Paquete", 260);
            lvPaquetes.Columns.Add("Categoría", 105);
            lvPaquetes.Columns.Add("Riesgo", 85);
            lvPaquetes.Columns.Add("Estado", 95);
            lvPaquetes.ItemCheck += AlMarcar;
            lvPaquetes.ItemChecked += (s, e) =>
            {
                // segunda barrera: aunque el sistema ignore el bloqueo de ItemCheck, nunca queda marcado algo protegido
                if (llenando) return;
                string n = (string)e.Item.Tag;
                InfoPaquete info;
                if (!motor.Paquetes.TryGetValue(n, out info)) return;
                if (e.Item.Checked && !Seleccionable(info))
                {
                    llenando = true;
                    e.Item.Checked = false;
                    llenando = false;
                    marcadas.Remove(n);
                }
                else if (e.Item.Checked) marcadas.Add(n);
                else marcadas.Remove(n);
                ContarMarcadas();
            };
            lvPaquetes.SelectedIndexChanged += (s, e) =>
            {
                if (lvPaquetes.SelectedItems.Count > 0) MostrarDetalle((string)lvPaquetes.SelectedItems[0].Tag);
            };
            lvPaquetes.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Space && lvPaquetes.SelectedItems.Count > 1)
                {
                    bool nuevo = !lvPaquetes.SelectedItems[0].Checked;
                    foreach (ListViewItem it in lvPaquetes.SelectedItems) it.Checked = nuevo;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            var centro = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0), BackColor = Tema.Fondo };
            centro.Controls.Add(lvPaquetes);
            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 6), BackColor = Tema.Fondo };
            cuerpo.Controls.Add(centro);
            cuerpo.Controls.Add(lbCategorias);
            cuerpo.Controls.Add(marcoDetalle);
            p.Controls.Add(cuerpo);
            p.Controls.Add(barraInf);
            p.Controls.Add(barraSup);
            MostrarAyudaLimpieza();
            return p;
        }

        void MostrarAyudaLimpieza()
        {
            rtDetalle.Clear();
            Tema.Agregar(rtDetalle, "Cómo usar\n", Tema.Subtitulo);
            Tema.Agregar(rtDetalle, "\n1. Conecta el dispositivo (se escanea solo).\n2. Pulsa 'Marcar recomendadas': solo marca lo " +
                "seguro (telemetría, publicidad y relleno).\n3. Revisa la lista: la casilla marca o desmarca. Con varias filas " +
                "seleccionadas, la barra espaciadora las marca juntas.\n4. Pulsa 'Aplicar a las marcadas'.\n\n");
            Tema.Agregar(rtDetalle, "Niveles\n", Tema.Subtitulo);
            foreach (var n in new[] { Nivel.Seguro, Nivel.Precaucion, Nivel.Experto, Nivel.Bloqueado })
            {
                Tema.Agregar(rtDetalle, "\n" + BaseDatos.TextoNivel(n) + ": ", Tema.Negrita, Tema.ColorNivel(n));
                Tema.Agregar(rtDetalle, BaseDatos.ExplicaNivel(n));
            }
            Tema.Agregar(rtDetalle, "\n\nTodo se puede deshacer en la pestaña Respaldos.", Tema.Pequena, Tema.Tenue);
        }

        void DibujarCategoria(object s, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (var bg = new SolidBrush(sel ? Tema.Seleccion : Tema.Panel)) e.Graphics.FillRectangle(bg, e.Bounds);
            string clave = e.Index < clavesCategorias.Count ? clavesCategorias[e.Index] : null;
            Color c = clave == null ? Tema.Texto : Tema.ColorCategoria(clave);
            var r = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, lbCategorias.Items[e.Index].ToString(), Tema.Base, r, c, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        internal void Escanear()
        {
            Tarea("Escaneando paquetes", () => motor.Escanear(), pk =>
            {
                LlenarLista();
                int off = pk.Values.Count(i => i.Estado != "activa");
                Log(string.Format("Escaneo: {0} paquetes ({1} desactivados/eliminados). Tipo: {2}. Inicio actual: {3}",
                    pk.Count, off, motor.TipoDispositivo, motor.Inicio ?? "sin predeterminado"));
                lblInicio.Text = "Inicio actual: " + (motor.Inicio ?? "—");
            }, true);
        }

        bool Seleccionable(InfoPaquete i)
        {
            return i.N == Nivel.Seguro || i.N == Nivel.Precaucion || (i.N == Nivel.Experto && chkExperto.Checked);
        }

        bool EsVisible(InfoPaquete i)
        {
            if (filtroCategoria != null && i.Cat != filtroCategoria) return false;
            switch (cmbMostrar.SelectedIndex)
            {
                case 1: if (i.Estado != "activa") return false; break;
                case 2: if (i.Estado == "activa") return false; break;
                case 3: if (!marcadas.Contains(i.Nombre)) return false; break;
            }
            string q = txtBuscar.Text.Trim().ToLowerInvariant();
            if (q.Length > 0 && !i.Nombre.ToLowerInvariant().Contains(q) && !i.Desc.ToLowerInvariant().Contains(q) &&
                !BaseDatos.NombreAmigable(i.Nombre).ToLowerInvariant().Contains(q)) return false;
            return true;
        }

        internal readonly HashSet<string> marcadas = new HashSet<string>();

        internal void LlenarLista()
        {
            if (lvPaquetes == null) return;
            llenando = true;
            var pk = motor.Paquetes;
            // categorías
            var conteo = new Dictionary<string, int>();
            foreach (var i in pk.Values) conteo[i.Cat] = (conteo.ContainsKey(i.Cat) ? conteo[i.Cat] : 0) + 1;
            lbCategorias.Items.Clear();
            clavesCategorias = new List<string> { null };
            lbCategorias.Items.Add("Todas  (" + pk.Count + ")");
            foreach (var c in BaseDatos.OrdenCategorias)
                if (conteo.ContainsKey(c))
                {
                    clavesCategorias.Add(c);
                    lbCategorias.Items.Add(char.ToUpper(c[0]) + c.Substring(1).ToLower() + "  (" + conteo[c] + ")");
                }
            int idx = clavesCategorias.IndexOf(filtroCategoria);
            if (idx < 0) { filtroCategoria = null; idx = 0; }
            lbCategorias.SelectedIndex = idx;

            // filas
            lvPaquetes.BeginUpdate();
            lvPaquetes.Items.Clear();
            var orden = BaseDatos.OrdenCategorias.ToList();
            var filas = pk.Values.Where(EsVisible)
                .OrderBy(i => orden.IndexOf(i.Cat))
                .ThenBy(i => i.N == Nivel.Bloqueado ? 9 : (int)i.N)
                .ThenBy(i => i.Nombre, StringComparer.Ordinal);
            var items = new List<ListViewItem>();
            foreach (var i in filas)
            {
                var it = new ListViewItem(new[]
                {
                    BaseDatos.NombreAmigable(i.Nombre), i.Nombre, char.ToUpper(i.Cat[0]) + i.Cat.Substring(1).ToLower(),
                    BaseDatos.TextoNivel(i.N), char.ToUpper(i.Estado[0]) + i.Estado.Substring(1)
                });
                it.Tag = i.Nombre;
                it.Checked = marcadas.Contains(i.Nombre);
                if (i.Estado != "activa") it.ForeColor = Tema.Aviso;
                else if (i.N == Nivel.Bloqueado) it.ForeColor = Tema.Apagado;
                else if (i.N == Nivel.Experto && !chkExperto.Checked) it.ForeColor = Tema.Tenue;
                items.Add(it);
            }
            lvPaquetes.Items.AddRange(items.ToArray());
            lvPaquetes.EndUpdate();
            llenando = false;
            if (paqueteDetalle != null && pk.ContainsKey(paqueteDetalle)) MostrarDetalle(paqueteDetalle);
            ContarMarcadas();
        }

        void AlMarcar(object s, ItemCheckEventArgs e)
        {
            if (llenando) return;
            string n = (string)lvPaquetes.Items[e.Index].Tag;
            InfoPaquete i;
            if (!motor.Paquetes.TryGetValue(n, out i)) return;
            if (e.NewValue == CheckState.Checked && !Seleccionable(i))
            {
                e.NewValue = e.CurrentValue;
                lblEstado.Text = i.N == Nivel.Bloqueado ? n + " está protegido y no se puede marcar."
                                                        : "Activa el Modo experto para marcar paquetes de nivel Experto.";
                return;
            }
            if (e.NewValue == CheckState.Checked) marcadas.Add(n); else marcadas.Remove(n);
        }

        void ContarMarcadas()
        {
            marcadas.RemoveWhere(n => !motor.Paquetes.ContainsKey(n));
            lblMarcadas.Text = marcadas.Count + (marcadas.Count == 1 ? " marcada" : " marcadas");
        }

        void MostrarDetalle(string n)
        {
            InfoPaquete i;
            if (!motor.Paquetes.TryGetValue(n, out i)) return;
            paqueteDetalle = n;
            rtDetalle.Clear();
            Tema.Agregar(rtDetalle, BaseDatos.NombreAmigable(n) + "\n", Tema.Subtitulo);
            Tema.Agregar(rtDetalle, n + "\n\n", Tema.Mono, Tema.Tenue);
            Tema.Agregar(rtDetalle, BaseDatos.TextoNivel(i.N), Tema.Negrita, Tema.ColorNivel(i.N));
            Tema.Agregar(rtDetalle, "   ·   " + i.Cat + "   ·   " + i.Estado + "\n\n");
            Tema.Agregar(rtDetalle, i.Desc + "\n\n");
            Tema.Agregar(rtDetalle, BaseDatos.ExplicaNivel(i.N) + "\n\n", Tema.Pequena, Tema.Tenue);
            Tema.Agregar(rtDetalle, (i.Sistema ? "App del sistema" : "App instalada") + " · fuente: " + i.Fuente + "\n", Tema.Pequena, Tema.Tenue);
            var a = motor.Historial != null ? motor.Historial.AccionDe(n) : null;
            if (a != null) Tema.Agregar(rtDetalle, "\nCambiado por TV Cleaner: " + a.action + " el " + a.time.Replace("T", " ") + "\n", Tema.Pequena, Tema.Tenue);
        }

        void CambioExperto()
        {
            if (chkExperto.Checked && !Dialogo.Pregunta(this,
                "El Modo experto permite marcar paquetes de nivel 'Experto': componentes del sistema poco conocidos.\n\n" +
                "Aunque todo es reversible, desactivar el equivocado puede hacer que alguna función deje de " +
                "funcionar hasta que lo restaures.\n\n¿Activar Modo experto?", true))
            {
                chkExperto.Checked = false;
                return;
            }
            if (!chkExperto.Checked)
                marcadas.RemoveWhere(n => motor.Paquetes.ContainsKey(n) && motor.Paquetes[n].N == Nivel.Experto);
            LlenarLista();
        }

        internal void MarcarRecomendadas()
        {
            if (motor.Paquetes.Count == 0) { Dialogo.Info(this, "Primero conecta y escanea el dispositivo."); return; }
            var cats = new HashSet<string> { "TELEMETRÍA", "PUBLICIDAD", "BLOATWARE", "JUEGOS", "FABRICANTE", "GOOGLE" };
            int n = 0;
            foreach (var i in motor.Paquetes.Values)
                if (i.N == Nivel.Seguro && cats.Contains(i.Cat) && i.Estado == "activa" && i.Sistema && marcadas.Add(i.Nombre)) n++;
            LlenarLista();
            Log("Marcadas " + n + " apps recomendadas (nivel Seguro). Revisa la lista antes de aplicar.");
        }

        internal void Aplicar()
        {
            string modo = rbEliminar.Checked ? "eliminar" : "desactivar";
            var nombres = marcadas.Where(n => motor.Paquetes.ContainsKey(n) &&
                (motor.Paquetes[n].Estado == "activa" || (modo == "eliminar" && motor.Paquetes[n].Estado == "desactivada"))).ToList();
            if (nombres.Count == 0)
            {
                Dialogo.Info(this, "No hay apps activas marcadas.");
                return;
            }
            if (!Dialogo.ConfirmarCambios(this, nombres.Select(n => motor.Paquetes[n]).ToList(), modo)) return;
            nombres.Sort(StringComparer.Ordinal);
            Tarea("Aplicando cambios", () => motor.Aplicar(nombres, modo, Progreso), r =>
            {
                foreach (var n in r.Ok) marcadas.Remove(n);
                LlenarLista();
                RefrescarHistorial();
                RefrescarEstadisticas();
                string msg = "Listo: " + r.Ok.Count + " app(s) " + (modo == "desactivar" ? "desactivadas." : "eliminadas.");
                if (r.Fallos.Count > 0)
                {
                    msg += "\n\nNo se pudieron cambiar " + r.Fallos.Count + ":\n" +
                           string.Join("\n", r.Fallos.Take(12).Select(f => "• " + f[0] + ": " + Utilidades.Recortar(f[1], 90)));
                    if (r.Fallos.Count > 12) msg += "\n… y " + (r.Fallos.Count - 12) + " más (ver Registro).";
                }
                msg += "\n\nRecomendado: reinicia el dispositivo (pestaña Optimizar, botón Reiniciar).";
                Dialogo.Info(this, msg);
            }, true);
        }

        void RestaurarMarcadas()
        {
            var nombres = marcadas.Where(n => motor.Paquetes.ContainsKey(n) && motor.Paquetes[n].Estado != "activa").ToList();
            if (nombres.Count == 0)
            {
                Dialogo.Info(this, "Marca apps que estén desactivadas o eliminadas para restaurarlas.\n" +
                                   "Consejo: en 'Mostrar' elige 'Desactivadas / eliminadas'.");
                return;
            }
            RestaurarLista(nombres);
        }

        internal void RestaurarLista(List<string> nombres)
        {
            Tarea("Restaurando", () =>
            {
                var r = new Motor.ResultadoLote();
                int k = 0;
                foreach (var n in nombres)
                {
                    Progreso(++k, nombres.Count, n);
                    string t;
                    if (motor.Restaurar(n, out t)) r.Ok.Add(n); else r.Fallos.Add(new[] { n, t });
                }
                return r;
            }, r =>
            {
                foreach (var n in r.Ok) marcadas.Remove(n);
                LlenarLista();
                RefrescarHistorial();
                Dialogo.Info(this, "Restauradas: " + r.Ok.Count + (r.Fallos.Count > 0 ? "\n\nFallaron:\n" +
                    string.Join("\n", r.Fallos.Take(12).Select(f => "• " + f[0] + ": " + Utilidades.Recortar(f[1], 90))) : ""));
            }, true);
        }
    }
}
