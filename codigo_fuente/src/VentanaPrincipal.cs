// Ventana principal: encabezado de conexión, barra de pestañas, tareas en segundo plano y registro.
// Cada pestaña está en su propio archivo (Pestana*.cs).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TVCleanerPro
{
    public partial class VentanaPrincipal : Form
    {
        internal readonly Motor motor = new Motor();
        internal readonly Instalador instalador;
        volatile bool ocupado;
        readonly string archivoRegistro;
        readonly string archivoConfig;
        Dictionary<string, object> config = new Dictionary<string, object>();

        // encabezado
        internal ComboBox cmbDispositivo;
        internal TextBox txtPuerto;
        Label lblPunto, lblDispositivo, lblRam, lblAlmacen, lblEstado;
        ProgressBar barra;

        // pestañas
        readonly Panel contenido = new Panel();
        readonly FlowLayoutPanel barraPestanas = new FlowLayoutPanel();
        readonly List<Button> botonesPestana = new List<Button>();
        readonly List<Panel> paneles = new List<Panel>();
        internal int pestanaActual;
        internal readonly List<Control> controlesDispositivo = new List<Control>();

        public VentanaPrincipal()
        {
            instalador = new Instalador(motor.Adb);
            motor.Log = Log;
            instalador.Log = Log;
            archivoRegistro = Path.Combine(Path.Combine(motor.Datos, "registros"), "registro_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            archivoConfig = Path.Combine(motor.Datos, "config.json");
            CargarConfig();

            Text = Utilidades.NombreApp + " " + Utilidades.Version;
            BackColor = Tema.Fondo;
            ForeColor = Tema.Texto;
            Font = Tema.Base;
            StartPosition = FormStartPosition.CenterScreen;
            var area = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(Math.Min(1280, area.Width - 40), Math.Min(820, area.Height - 40));
            MinimumSize = new Size(1000, 640);
            KeyPreview = true;
            AllowDrop = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }

            Construir();
            MarcarConectado(false);
            KeyDown += TeclaGlobal;
            DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += (s, e) => SoltarArchivos((string[])e.Data.GetData(DataFormats.FileDrop));
            FormClosing += AlCerrar;
            Shown += (s, e) => Iniciar();
        }

        void Iniciar()
        {
            Log(Utilidades.NombreApp + " " + Utilidades.Version + " iniciado.");
            Log("Datos y respaldos en: " + motor.Datos);
            if (motor.Adb.Ruta == null)
            {
                Log("ERROR: no se encontró ADB.");
                Dialogo.Error(this, "ADB", "No se encontró ADB. Vuelve a descargar el programa.");
                return;
            }
            Log("ADB: " + motor.Adb.Ruta);
            Tarea("Iniciando ADB", () => motor.Adb.VersionAdb(), v => { Log("ADB listo — " + v); ActualizarListaDispositivos(true); });
        }

        // ───────── construcción ─────────
        void Construir()
        {
            SuspendLayout();
            // contenido (Fill) primero; luego lo que va arriba/abajo, de adentro hacia afuera
            contenido.Dock = DockStyle.Fill;
            contenido.Padding = new Padding(10, 6, 10, 6);
            contenido.BackColor = Tema.Fondo;
            Controls.Add(contenido);

            var pie = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Tema.Panel, Padding = new Padding(10, 4, 10, 4) };
            lblEstado = Tema.Etiqueta("Listo.", Tema.Pequena, Tema.Tenue);
            lblEstado.AutoSize = false;
            lblEstado.Dock = DockStyle.Fill;
            lblEstado.AutoEllipsis = true;
            lblEstado.Margin = Padding.Empty;
            lblEstado.TextAlign = ContentAlignment.MiddleLeft;
            barra = new ProgressBar { Dock = DockStyle.Right, Width = 220, Style = ProgressBarStyle.Continuous };
            pie.Controls.Add(lblEstado);
            pie.Controls.Add(barra);
            Controls.Add(pie);

            barraPestanas.Dock = DockStyle.Top;
            barraPestanas.AutoSize = true;
            barraPestanas.BackColor = Tema.Fondo;
            barraPestanas.Padding = new Padding(10, 8, 10, 0);
            Controls.Add(barraPestanas);

            Controls.Add(ConstruirFranja());
            Controls.Add(ConstruirEncabezado());

            AgregarPestana("Limpieza", ConstruirLimpieza());
            AgregarPestana("Mis apps", ConstruirApps());
            AgregarPestana("Red", ConstruirRed());
            AgregarPestana("Optimizar", ConstruirOptimizar());
            AgregarPestana("Launcher", ConstruirLauncher());
            AgregarPestana("Control remoto", ConstruirControl());
            AgregarPestana("Respaldos", ConstruirRespaldos());
            AgregarPestana("Registro", ConstruirRegistro());
            MostrarPestana(0);
            ResumeLayout();
        }

        Control ConstruirEncabezado()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Tema.Panel, Padding = new Padding(16, 10, 16, 8) };
            var titulo = new FlowLayoutPanel { Dock = DockStyle.Left, FlowDirection = FlowDirection.TopDown, AutoSize = true, BackColor = Tema.Panel, WrapContents = false };
            var t1 = Tema.Etiqueta("TV Cleaner Pro", Tema.Titulo);
            t1.Margin = new Padding(0);
            titulo.Controls.Add(t1);
            titulo.Controls.Add(Tema.Etiqueta("Android TV · Google TV · Fire TV · TV Box — por WiFi, cable o USB", Tema.Pequena, Tema.Tenue));
            top.Controls.Add(titulo);

            var con = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, BackColor = Tema.Panel };
            var col1 = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, BackColor = Tema.Panel, WrapContents = false, Margin = new Padding(0) };
            var l1 = Tema.Etiqueta("IP del TV o dispositivo USB", Tema.Pequena, Tema.Tenue);
            l1.Margin = new Padding(3, 0, 3, 2);
            col1.Controls.Add(l1);
            cmbDispositivo = new ComboBox { Width = 250, Font = Tema.Base, BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDown };
            foreach (var ip in ListaConfig("ips")) cmbDispositivo.Items.Add(ip);
            cmbDispositivo.Text = cmbDispositivo.Items.Count > 0 ? cmbDispositivo.Items[0].ToString() : "192.168.1.2";
            cmbDispositivo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Conectar(); } };
            col1.Controls.Add(cmbDispositivo);
            con.Controls.Add(col1);

            var col2 = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, BackColor = Tema.Panel, WrapContents = false, Margin = new Padding(6, 0, 0, 0) };
            var l2 = Tema.Etiqueta("Puerto", Tema.Pequena, Tema.Tenue);
            l2.Margin = new Padding(3, 0, 3, 2);
            col2.Controls.Add(l2);
            txtPuerto = Tema.CajaTexto(64);
            txtPuerto.Margin = new Padding(3, 0, 3, 0);
            txtPuerto.Text = config.ContainsKey("port") ? Convert.ToString(config["port"]) : "5555";
            col2.Controls.Add(txtPuerto);
            con.Controls.Add(col2);

            var botones = new FlowLayoutPanel { AutoSize = true, BackColor = Tema.Panel, WrapContents = false, Margin = new Padding(8, 14, 0, 0) };
            botones.Controls.Add(Tema.Boton("Conectar", (s, e) => Conectar(), Tema.EstiloBoton.Acento));
            botones.Controls.Add(Tema.Boton("Ver USB", (s, e) => ActualizarListaDispositivos(false)));
            botones.Controls.Add(Tema.Boton("Buscar en la red", (s, e) => { MostrarPestana(2); EscanearRed(); }));
            botones.Controls.Add(Tema.Boton("Emparejar", (s, e) => DialogoEmparejar(null)));
            con.Controls.Add(botones);
            top.Controls.Add(con);
            return top;
        }

        Control ConstruirFranja()
        {
            var franja = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Tema.Tarjeta, Padding = new Padding(16, 0, 16, 0) };
            lblPunto = Tema.Etiqueta("●", Tema.Subtitulo, Tema.Malo);
            lblPunto.Dock = DockStyle.Left;
            lblPunto.AutoSize = false;
            lblPunto.Width = 22;
            lblPunto.TextAlign = ContentAlignment.MiddleLeft;
            lblDispositivo = Tema.Etiqueta("", Tema.Base);
            lblDispositivo.Dock = DockStyle.Fill;
            lblDispositivo.AutoSize = false;
            lblDispositivo.AutoEllipsis = true;
            lblDispositivo.TextAlign = ContentAlignment.MiddleLeft;
            lblRam = Tema.Etiqueta("", Tema.Pequena, Tema.Tenue);
            lblRam.Dock = DockStyle.Right;
            lblRam.TextAlign = ContentAlignment.MiddleRight;
            lblRam.AutoSize = false;
            lblRam.Width = 250;
            lblAlmacen = Tema.Etiqueta("", Tema.Pequena, Tema.Tenue);
            lblAlmacen.Dock = DockStyle.Right;
            lblAlmacen.TextAlign = ContentAlignment.MiddleRight;
            lblAlmacen.AutoSize = false;
            lblAlmacen.Width = 290;
            franja.Controls.Add(lblDispositivo);
            franja.Controls.Add(lblPunto);
            franja.Controls.Add(lblRam);
            franja.Controls.Add(lblAlmacen);
            return franja;
        }

        void AgregarPestana(string nombre, Panel panel)
        {
            int indice = paneles.Count;
            var b = Tema.Boton(nombre, (s, e) => MostrarPestana(indice));
            b.Margin = new Padding(0, 0, 4, 0);
            b.Padding = new Padding(12, 5, 12, 5);
            botonesPestana.Add(b);
            barraPestanas.Controls.Add(b);
            panel.Dock = DockStyle.Fill;
            panel.Visible = false;
            panel.BackColor = Tema.Fondo;
            paneles.Add(panel);
            contenido.Controls.Add(panel);
        }

        internal void MostrarPestana(int i)
        {
            pestanaActual = i;
            for (int k = 0; k < paneles.Count; k++)
            {
                paneles[k].Visible = k == i;
                botonesPestana[k].BackColor = k == i ? Tema.Seleccion : Tema.Panel;
                botonesPestana[k].ForeColor = k == i ? Tema.Texto : Tema.Tenue;
                botonesPestana[k].FlatAppearance.BorderColor = k == i ? Tema.Acento : Tema.Linea;
            }
            if (i == 6) RefrescarHistorial();
            bool conectado = motor.Adb.Serial != null && !ocupado;
            if (i == 1 && conectado && appsUsuario.Count == 0) CargarApps();
            if (i == 4 && conectado && launchers.Count == 0) ListarLaunchers();
        }

        // ───────── infraestructura ─────────
        internal void UI(Action a)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke(a); } catch (Exception) { } }
            else a();
        }

        internal void Log(string msg)
        {
            string linea = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg;
            try { File.AppendAllText(archivoRegistro, linea + Environment.NewLine, Encoding.UTF8); } catch (Exception) { }
            UI(() =>
            {
                AgregarRegistro(linea);
                string primera = msg.Split('\n')[0];
                lblEstado.Text = Utilidades.Recortar(primera, 180);
            });
        }

        internal void Progreso(int i, int total, string etiqueta)
        {
            UI(() =>
            {
                barra.Style = ProgressBarStyle.Continuous;
                barra.Maximum = Math.Max(total, 1);
                barra.Value = Math.Min(i, barra.Maximum);
                if (!string.IsNullOrEmpty(etiqueta)) lblEstado.Text = Utilidades.Recortar(i + "/" + total + "  " + etiqueta, 180);
            });
        }

        internal bool Ocupado
        {
            get { return ocupado; }
        }

        /// <summary>Ejecuta 'trabajo' en segundo plano y luego 'listo' en la ventana. Una tarea a la vez.</summary>
        internal void Tarea<T>(string titulo, Func<T> trabajo, Action<T> listo, bool requiereDispositivo = false)
        {
            if (ocupado) { Dialogo.Info(this, "Espera a que termine la operación en curso."); return; }
            if (requiereDispositivo && motor.Adb.Serial == null) { Dialogo.Aviso(this, "Primero conecta el dispositivo."); return; }
            ocupado = true;
            lblEstado.Text = titulo + "…";
            barra.Style = ProgressBarStyle.Marquee;
            UseWaitCursor = true;
            Task.Run(() =>
            {
                T res = default(T);
                string error = null;
                bool caido = false;
                try { res = trabajo(); }
                catch (AdbException e) { error = e.Message; }
                catch (Exception e) { error = e.GetType().Name + ": " + e.Message; Log("ERROR " + e); }
                if (error != null) caido = motor.Adb.Serial == null || !motor.Adb.Responde();
                UI(() =>
                {
                    ocupado = false;
                    barra.Style = ProgressBarStyle.Continuous;
                    barra.Value = 0;
                    UseWaitCursor = false;
                    if (error != null)
                    {
                        Log("ERROR en '" + titulo + "': " + error.Split('\n')[0]);
                        if (caido) MarcarConectado(false);
                        Dialogo.Error(this, titulo, error);
                    }
                    else if (listo != null)
                    {
                        try { listo(res); }
                        catch (Exception e) { Log("ERROR interfaz: " + e); }
                    }
                });
            });
        }

        /// <summary>Atajo para órdenes simples al dispositivo.</summary>
        internal void Orden(string titulo, Action trabajo, string mensajeOk)
        {
            Tarea<object>(titulo, () => { trabajo(); return null; }, r => Log(mensajeOk), true);
        }

        // ───────── configuración ─────────
        void CargarConfig()
        {
            try
            {
                if (File.Exists(archivoConfig))
                    config = Utilidades.Json().Deserialize<Dictionary<string, object>>(File.ReadAllText(archivoConfig)) ?? config;
            }
            catch (Exception) { }
        }

        void GuardarConfig()
        {
            try { File.WriteAllText(archivoConfig, Utilidades.Indentar(Utilidades.Json().Serialize(config))); } catch (Exception) { }
        }

        List<string> ListaConfig(string clave)
        {
            var res = new List<string>();
            object v;
            if (config.TryGetValue(clave, out v) && v is System.Collections.IEnumerable && !(v is string))
                foreach (var x in (System.Collections.IEnumerable)v) res.Add(Convert.ToString(x));
            return res;
        }

        // ───────── conexión ─────────
        internal void MarcarConectado(bool si)
        {
            lblPunto.ForeColor = si ? Tema.Ok : Tema.Malo;
            foreach (var c in controlesDispositivo) c.Enabled = si;
            if (!si)
            {
                lblDispositivo.Text = "Sin conexión — escribe la IP del dispositivo (o elige uno USB) y pulsa Conectar";
                lblRam.Text = "";
                lblAlmacen.Text = "";
            }
        }

        internal void ActualizarListaDispositivos(bool silencioso)
        {
            Task.Run(() =>
            {
                List<DispositivoAdb> lista;
                try { lista = motor.Adb.Dispositivos(); } catch (Exception) { return; }
                UI(() =>
                {
                    string actual = cmbDispositivo.Text;
                    cmbDispositivo.Items.Clear();
                    foreach (var d in lista) cmbDispositivo.Items.Add(d.ToString());
                    foreach (var ip in ListaConfig("ips"))
                        if (!lista.Any(d => d.Serial.StartsWith(ip + ":"))) cmbDispositivo.Items.Add(ip);
                    cmbDispositivo.Text = actual;
                    var usb = lista.Where(d => d.EsUsb).ToList();
                    if (!silencioso)
                    {
                        if (usb.Count == 0)
                            Dialogo.Info(this, "No hay dispositivos USB conectados.\n\nPara TV Box o celulares por cable: activa " +
                                               "'Depuración USB' y conecta el cable (en TV Box suele ser un cable USB A-A al puerto OTG).");
                        else
                        {
                            cmbDispositivo.Text = usb[0].ToString();
                            Log("Dispositivos USB: " + string.Join(", ", usb.Select(d => d.Serial + " " + d.Modelo)));
                        }
                    }
                });
            });
        }

        internal void Conectar()
        {
            string texto = cmbDispositivo.Text.Trim();
            string primero = texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            string puerto = txtPuerto.Text.Trim();
            if (puerto.Length == 0) puerto = "5555";
            bool esIp = false;
            string host = primero;
            if (primero.Contains(":"))
            {
                var p = primero.Split(':');
                host = p[0];
                if (Utilidades.PuertoValido(p[1])) puerto = p[1];
            }
            System.Net.IPAddress tmp;
            esIp = System.Net.IPAddress.TryParse(host, out tmp) || (Utilidades.HostValido(host) && host.Contains("."));
            bool esSerialUsb = !esIp && System.Text.RegularExpressions.Regex.IsMatch(primero, @"^[A-Za-z0-9._\-]{4,64}$");
            if (!esIp && !esSerialUsb)
            {
                Dialogo.Aviso(this, "Escribe una IP válida (ej. 192.168.1.2) o elige un dispositivo USB de la lista.");
                return;
            }
            Log(esIp ? "Conectando a " + host + ":" + puerto + "…" : "Usando dispositivo USB " + primero + "…");
            bool avisado = false;
            Action<int> auth = seg =>
            {
                if (!avisado)
                {
                    avisado = true;
                    UI(() => Dialogo.Info(this, "Mira la pantalla del dispositivo: aparece '¿Permitir la depuración?'.\n\n" +
                                                "Marca 'Permitir siempre desde esta computadora' y pulsa ACEPTAR con el control.\n\n" +
                                                "TV Cleaner esperará hasta 90 segundos."));
                }
                UI(() => lblEstado.Text = "Esperando autorización en el dispositivo… " + seg + " s");
            };
            Tarea("Conectando", () =>
            {
                if (esIp) motor.ConectarRed(host, puerto, auth);
                else motor.ConectarSerial(primero, auth);
                return motor.Adb.Serial;
            }, serial =>
            {
                if (esIp)
                {
                    var ips = ListaConfig("ips");
                    ips.Remove(host);
                    ips.Insert(0, host);
                    config["ips"] = ips.Take(10).ToList();
                    config["port"] = int.Parse(puerto);
                    GuardarConfig();
                }
                string marca = motor.Prop("marca").ToUpperInvariant();
                string txt = string.Format("{0} {1}  ·  {2}  ·  Android {3} (SDK {4})  ·  {5}", marca, motor.Prop("modelo"),
                    motor.TipoDispositivo, motor.Prop("android"), motor.Prop("sdk"), serial);
                lblDispositivo.Text = txt;
                MarcarConectado(true);
                Log("Conectado: " + txt);
                AjustarSegunDispositivo();
                RefrescarEstadisticas();
                RefrescarHistorial();
                Escanear();
            });
        }

        internal void RefrescarEstadisticas()
        {
            if (motor.Adb.Serial == null) return;
            Task.Run(() =>
            {
                try
                {
                    var mem = motor.Adb.Memoria();
                    var alm = motor.Adb.Almacenamiento();
                    UI(() =>
                    {
                        int total = mem.ContainsKey("MemTotal") ? mem["MemTotal"] : 0;
                        int libre = mem.ContainsKey("MemAvailable") ? mem["MemAvailable"] : (mem.ContainsKey("MemFree") ? mem["MemFree"] : 0);
                        int pct = total > 0 ? libre * 100 / total : 0;
                        lblRam.Text = string.Format("RAM libre {0} de {1} MB ({2}%)", libre, total, pct);
                        if (alm[0] > 0) lblAlmacen.Text = string.Format("Almacenamiento libre {0:0.0} de {1:0.0} GB", alm[2] / 1024.0, alm[0] / 1024.0);
                    });
                }
                catch (Exception) { }
            });
        }

        void DialogoEmparejar(string ip)
        {
            var d = Dialogo.Emparejar(this, ip ?? cmbDispositivo.Text.Split(' ')[0].Split(':')[0]);
            if (d == null) return;
            Tarea("Emparejando", () => motor.Adb.Emparejar(d[0], d[1], d[2]), msg =>
            {
                Log(msg);
                if (d[3].Length > 0)
                {
                    cmbDispositivo.Text = d[0];
                    txtPuerto.Text = d[3];
                    Conectar();
                }
                else Dialogo.Info(this, "Emparejado. Escribe el 'puerto de conexión' que muestra el dispositivo en " +
                                        "'Depuración inalámbrica' y pulsa Conectar.");
            });
        }

        void TeclaGlobal(object s, KeyEventArgs e)
        {
            if (pestanaActual != 5 || motor.Adb.Serial == null) return;
            if (ActiveControl is TextBox || ActiveControl is ComboBox) return;
            string c = null;
            switch (e.KeyCode)
            {
                case Keys.Up: c = "KEYCODE_DPAD_UP"; break;
                case Keys.Down: c = "KEYCODE_DPAD_DOWN"; break;
                case Keys.Left: c = "KEYCODE_DPAD_LEFT"; break;
                case Keys.Right: c = "KEYCODE_DPAD_RIGHT"; break;
                case Keys.Enter: c = "KEYCODE_DPAD_CENTER"; break;
                case Keys.Escape:
                case Keys.Back: c = "KEYCODE_BACK"; break;
                case Keys.Home: c = "KEYCODE_HOME"; break;
            }
            if (c != null) { EnviarTecla(c); e.Handled = true; e.SuppressKeyPress = true; }
        }

        internal void AbrirRuta(string ruta)
        {
            try { System.Diagnostics.Process.Start(ruta); }
            catch (Exception e) { Dialogo.Info(this, "Ubicación:\n" + ruta + "\n\n(" + e.Message + ")"); }
        }

        void AlCerrar(object s, FormClosingEventArgs e)
        {
            if (ocupado && !Dialogo.Pregunta(this, "Hay una operación en curso. ¿Salir de todas formas?")) { e.Cancel = true; return; }
            try { if (cancelarEscaneo != null) cancelarEscaneo.Cancel(); } catch (Exception) { }
            try
            {
                motor.Adb.Desconectar();
                motor.Adb.CerrarServidor();
            }
            catch (Exception) { }
        }
    }
}
