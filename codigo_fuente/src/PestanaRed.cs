// Pestaña "Red": escanea TODAS las redes de la PC y muestra los TVs y reproductores encontrados.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TVCleanerPro
{
    public partial class VentanaPrincipal
    {
        internal ListView lvRed;
        internal TextBox txtRango;
        Label lblRedes;
        Button bEscanearRed, bDetener;
        internal CancellationTokenSource cancelarEscaneo;
        readonly Dictionary<string, ListViewItem> filasRed = new Dictionary<string, ListViewItem>();
        internal List<EquipoRed> ultimoEscaneo = new List<EquipoRed>();

        Panel ConstruirRed()
        {
            var p = new Panel();
            var barra = Tema.Fila(Tema.Fondo);
            bEscanearRed = Tema.Boton("Escanear toda la red", (s, e) => EscanearRed(), Tema.EstiloBoton.Acento);
            bDetener = Tema.Boton("Detener", (s, e) => { if (cancelarEscaneo != null) cancelarEscaneo.Cancel(); });
            bDetener.Enabled = false;
            barra.Controls.Add(bEscanearRed);
            barra.Controls.Add(bDetener);
            barra.Controls.Add(Tema.Etiqueta("   Rango manual (opcional):"));
            txtRango = Tema.CajaTexto(280);
            barra.Controls.Add(txtRango);
            barra.Controls.Add(Tema.Etiqueta("ej. 192.168.0.0/16 · 10.0.0.1-10.0.3.254", Tema.Pequena, Tema.Tenue));

            var info = Tema.Fila(Tema.Fondo);
            lblRedes = Tema.Etiqueta("Redes de esta PC: (se detectan al escanear)", Tema.Pequena, Tema.Tenue);
            lblRedes.MaximumSize = new Size(1200, 0);
            info.Controls.Add(lblRedes);

            var abajo = Tema.Fila(Tema.Panel);
            abajo.Dock = DockStyle.Bottom;
            abajo.Controls.Add(Tema.Boton("Conectar al seleccionado", (s, e) => ConectarSeleccionRed(), Tema.EstiloBoton.Ok));
            abajo.Controls.Add(Tema.Boton("Emparejar al seleccionado (Android 11+)", (s, e) =>
            {
                var eq = EquipoSeleccionado();
                DialogoEmparejar(eq != null ? eq.IP : null);
            }));
            var nota = Tema.Etiqueta("Se buscan TVs de cualquier marca (Android TV, Google TV, Fire TV, TV Box, Chromecast, Samsung, LG, " +
                                     "Roku, Sony, Philips, Hisense…) por cable, WiFi y cualquier otro adaptador de red. " +
                                     "Solo los que usan Android se pueden limpiar.", Tema.Pequena, Tema.Tenue);
            nota.MaximumSize = new Size(760, 0);
            abajo.Controls.Add(nota);

            lvRed = Tema.Lista();
            lvRed.Dock = DockStyle.Fill;
            lvRed.MultiSelect = false;
            lvRed.Columns.Add("IP", 115);
            lvRed.Columns.Add("Nombre", 190);
            lvRed.Columns.Add("Marca / modelo", 190);
            lvRed.Columns.Add("Tipo", 170);
            lvRed.Columns.Add("Estado para TV Cleaner", 260);
            lvRed.Columns.Add("Puertos / servicios", 230);
            lvRed.Columns.Add("MAC", 130);
            lvRed.DoubleClick += (s, e) => ConectarSeleccionRed();

            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 4), BackColor = Tema.Fondo };
            cuerpo.Controls.Add(lvRed);
            p.Controls.Add(cuerpo);
            p.Controls.Add(abajo);
            p.Controls.Add(info);
            p.Controls.Add(barra);
            return p;
        }

        internal void EscanearRed()
        {
            if (cancelarEscaneo != null) return;   // ya hay uno en curso
            var esc = new EscanerRed();
            List<RangoIp> rangos;
            try
            {
                rangos = txtRango.Text.Trim().Length > 0 ? EscanerRed.InterpretarRangos(txtRango.Text) : esc.RedesLocales();
            }
            catch (ArgumentException e) { Dialogo.Aviso(this, e.Message); return; }
            if (rangos.Count == 0) { Dialogo.Aviso(this, "No se encontró ninguna red activa en esta PC. ¿Está conectada por cable o WiFi?"); return; }
            lblRedes.Text = "Escaneando: " + string.Join("   |   ", rangos.Select(r => r.ToString()));
            filasRed.Clear();
            lvRed.Items.Clear();
            cancelarEscaneo = new CancellationTokenSource();
            var token = cancelarEscaneo.Token;
            bEscanearRed.Enabled = false;
            bDetener.Enabled = true;
            esc.Log = Log;
            esc.AlEncontrar = eq => UI(() => ActualizarFilaRed(eq));
            Task.Run(() =>
            {
                List<EquipoRed> res = null;
                string error = null;
                try
                {
                    res = esc.Escanear(rangos, (i, t) => Progreso(i, t, "Escaneando la red…"), token);
                    // dispositivos con depuración inalámbrica que ADB ya conoce por su cuenta
                    foreach (var s in motor.Adb.ServiciosMdns())
                    {
                        var eq = res.FirstOrDefault(x => x.IP == s[2]);
                        if (eq == null) { eq = new EquipoRed { IP = s[2] }; res.Add(eq); }
                        eq.Servicios.Add(s[1].Replace("._tcp", ""));
                        if (s[1].StartsWith("_adb-tls-connect") || s[1] == "_adb._tcp") { eq.PuertoAdb = int.Parse(s[3]); eq.AdbInalambrico = s[3] != "5555"; }
                        EscanerRed.Identificar(eq);
                    }
                }
                catch (Exception e) { error = e.Message; }
                UI(() =>
                {
                    cancelarEscaneo = null;
                    bEscanearRed.Enabled = true;
                    bDetener.Enabled = false;
                    if (error != null) { Dialogo.Error(this, "Escaneo de red", error); return; }
                    ultimoEscaneo = res;
                    filasRed.Clear();
                    lvRed.Items.Clear();
                    foreach (var eq in res) ActualizarFilaRed(eq);
                    int android = res.Count(x => x.EsAndroid), listos = res.Count(x => x.PuertoAdb > 0);
                    string resumen = string.Format("Escaneo terminado: {0} equipo(s) multimedia/red, {1} con Android, {2} listo(s) para conectar.",
                        res.Count, android, listos);
                    Log(resumen);
                    lblEstado.Text = resumen;
                    if (listos == 1 && motor.Adb.Serial == null)
                    {
                        var eq = res.First(x => x.PuertoAdb > 0);
                        cmbDispositivo.Text = eq.IP;
                        txtPuerto.Text = eq.PuertoAdb.ToString();
                    }
                });
            });
        }

        void ActualizarFilaRed(EquipoRed e)
        {
            string marcaModelo;
            string[] valores;
            lock (e)
            {
                marcaModelo = (e.Marca + " " + e.Modelo).Trim();
                var serv = string.Join(", ", e.Servicios.Select(s => s.TrimStart('_')));
                string puertos = EscanerRed.DescribirPuertos(e);
                valores = new[] { e.IP, e.Nombre, marcaModelo, e.Tipo, e.Compatibilidad,
                                  (puertos + (serv.Length > 0 ? (puertos.Length > 0 ? " · " : "") + serv : "")), e.Mac };
            }
            ListViewItem it;
            if (!filasRed.TryGetValue(e.IP, out it))
            {
                it = new ListViewItem(valores);
                it.Tag = e;
                filasRed[e.IP] = it;
                lvRed.Items.Add(it);
            }
            else
            {
                for (int i = 0; i < valores.Length; i++) it.SubItems[i].Text = valores[i];
            }
            it.ForeColor = e.PuertoAdb > 0 ? Tema.Ok : (e.EsAndroid ? Tema.Aviso : (e.Tipo == "Equipo de red" ? Tema.Apagado : Tema.Texto));
        }

        EquipoRed EquipoSeleccionado()
        {
            return lvRed.SelectedItems.Count > 0 ? lvRed.SelectedItems[0].Tag as EquipoRed : null;
        }

        void ConectarSeleccionRed()
        {
            var eq = EquipoSeleccionado();
            if (eq == null) { Dialogo.Info(this, "Selecciona un equipo de la lista."); return; }
            if (eq.AdbInalambrico)
            {
                Dialogo.Info(this, "Este dispositivo usa 'Depuración inalámbrica' (Android 11+). La primera vez hay que emparejarlo con un código.");
                DialogoEmparejar(eq.IP);
                return;
            }
            if (!eq.EsAndroid && eq.PuertoAdb == 0)
            {
                Dialogo.Aviso(this, eq.Tipo + " no usa Android, así que no se puede limpiar con ADB.\n\n" +
                                    "TV Cleaner funciona con Android TV, Google TV, Fire TV, NVIDIA Shield y TV Box Android.");
                return;
            }
            cmbDispositivo.Text = eq.IP;
            txtPuerto.Text = (eq.PuertoAdb > 0 ? eq.PuertoAdb : 5555).ToString();
            if (eq.PuertoAdb == 0)
                Log("El puerto ADB de " + eq.IP + " no respondió: activa 'Depuración ADB' en el dispositivo. Se intentará igual.");
            MostrarPestana(0);
            Conectar();
        }
    }
}
