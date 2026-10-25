// Pestañas "Optimizar", "Launcher" y "Control remoto".
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
        Label lblInicio, lblAvisoLauncher;
        ListBox lbLaunchers;
        CheckBox chkDejarAnterior;
        Button bTiendaFLauncher;
        List<string> launchers = new List<string>();
        TextBox txtEscribir;

        Panel Tarjeta(FlowLayoutPanel contenedor, string titulo, string texto, params Button[] botones)
        {
            var t = new Panel { BackColor = Tema.Tarjeta, Padding = new Padding(14, 10, 14, 10), Margin = new Padding(0, 0, 0, 8), Height = 74 };
            var der = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, BackColor = Tema.Tarjeta, Padding = new Padding(0, 6, 0, 0) };
            foreach (var b in botones) { der.Controls.Add(b); controlesDispositivo.Add(b); }
            var izq = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Tarjeta };
            var l1 = Tema.Etiqueta(titulo, Tema.Negrita);
            l1.Location = new Point(0, 0);
            var l2 = Tema.Etiqueta(texto, Tema.Pequena, Tema.Tenue);
            l2.Location = new Point(0, 22);
            l2.AutoSize = true;
            l2.MaximumSize = new Size(700, 0);
            izq.Controls.Add(l1);
            izq.Controls.Add(l2);
            izq.Resize += (s, e) =>
            {
                l2.MaximumSize = new Size(Math.Max(200, izq.Width - 10), 0);
                t.Height = Math.Max(74, l2.Top + l2.PreferredSize.Height + 24);
            };
            t.Width = 1000;
            t.Controls.Add(izq);
            t.Controls.Add(der);
            contenedor.Controls.Add(t);
            contenedor.Resize += (s, e) => t.Width = contenedor.ClientSize.Width - 24;
            return t;
        }

        Panel ConstruirOptimizar()
        {
            var p = new Panel();
            var lista = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Tema.Fondo, Padding = new Padding(0, 6, 0, 0) };
            var tit = Tema.Etiqueta("Optimizaciones del sistema", Tema.Subtitulo);
            lista.Controls.Add(tit);
            lista.Controls.Add(Tema.Etiqueta("Todas guardan el valor original y se pueden deshacer desde la pestaña Respaldos.", Tema.Pequena, Tema.Tenue));
            Tarjeta(lista, "Animaciones más rápidas", "Reduce las animaciones del sistema. El dispositivo se siente más ágil al navegar.",
                Tema.Boton("0.5x (recomendado)", (s, e) => Animaciones("0.5"), Tema.EstiloBoton.Ok),
                Tema.Boton("Sin animaciones", (s, e) => Animaciones("0")),
                Tema.Boton("Normal 1x", (s, e) => Animaciones("1.0")));
            Tarjeta(lista, "Liberar memoria", "Limpia cachés de apps y cierra procesos en segundo plano. No borra cuentas ni datos.",
                Tema.Boton("Limpiar cachés", (s, e) => LiberarMemoria("pm trim-caches 999G", "Cachés limpiadas"), Tema.EstiloBoton.Ok),
                Tema.Boton("Cerrar apps en segundo plano", (s, e) => LiberarMemoria("am kill-all", "Apps en segundo plano cerradas")));
            Tarjeta(lista, "Optimizar apps (compilación)", "Hace que el sistema compile las apps para que abran más rápido. Puede tardar 2 a 10 minutos.",
                Tema.Boton("Optimizar ahora", (s, e) =>
                {
                    if (!Dialogo.Pregunta(this, "La optimización puede tardar varios minutos y el dispositivo puede ir lento mientras tanto. ¿Continuar?")) return;
                    Tarea("Optimizando apps (puede tardar)", () => motor.Adb.Shell("cmd package bg-dexopt-job", 1200).Todo,
                        r => Log("Optimización de apps terminada: " + Utilidades.Recortar(r.Length > 0 ? r : "ok", 200)), true);
                }));
            Tarjeta(lista, "Menos actividad en segundo plano",
                "Desactiva el escaneo permanente de WiFi y Bluetooth que el sistema hace aunque no lo uses (WiFi y control siguen funcionando).",
                Tema.Boton("Aplicar", (s, e) => AjustesEscaneo(true), Tema.EstiloBoton.Ok),
                Tema.Boton("Deshacer", (s, e) => AjustesEscaneo(false)));
            Tarjeta(lista, "Privacidad en los ajustes del dispositivo",
                "En el menú del dispositivo: Privacidad > Anuncios (borrar ID de publicidad) y Uso y diagnóstico > No. Fire TV: Preferencias > Privacidad.",
                Tema.Boton("Abrir Ajustes en el TV", (s, e) => Orden("Abriendo ajustes", () => motor.Adb.Shell("am start -a android.settings.SETTINGS"), "Ajustes abiertos en el dispositivo.")));
            Tarjeta(lista, "Herramientas", "Información completa, captura de pantalla, instalar apps o reiniciar.",
                Tema.Boton("Información", (s, e) => Informacion()),
                Tema.Boton("Captura de pantalla", (s, e) => Captura()),
                Tema.Boton("Instalar apps", (s, e) => { MostrarPestana(1); SeleccionarApks(); }),
                Tema.Boton("Reiniciar", (s, e) => Reiniciar(), Tema.EstiloBoton.Peligro));
            p.Controls.Add(lista);
            return p;
        }

        void Animaciones(string v)
        {
            Tarea<object>("Ajustando animaciones", () =>
            {
                foreach (var k in new[] { "window_animation_scale", "transition_animation_scale", "animator_duration_scale" })
                    motor.CambiarAjuste("global", k, v);
                return null;
            }, r => { Log("Animaciones en " + v + "x"); RefrescarHistorial(); }, true);
        }

        void LiberarMemoria(string comando, string texto)
        {
            Tarea(texto, () =>
            {
                var antes = motor.Adb.Memoria();
                motor.Adb.Shell(comando, 120);
                System.Threading.Thread.Sleep(1000);
                var despues = motor.Adb.Memoria();
                int a = antes.ContainsKey("MemAvailable") ? antes["MemAvailable"] : 0;
                int d = despues.ContainsKey("MemAvailable") ? despues["MemAvailable"] : 0;
                return new[] { a, d };
            }, r => { Log(texto + ". RAM libre: " + r[0] + " a " + r[1] + " MB"); RefrescarEstadisticas(); }, true);
        }

        void AjustesEscaneo(bool aplicar)
        {
            Tarea<object>("Ajustando", () =>
            {
                foreach (var k in new[] { "wifi_scan_always_enabled", "ble_scan_always_enabled" })
                    if (aplicar) motor.CambiarAjuste("global", k, "0"); else motor.RestaurarAjuste("global", k);
                return null;
            }, r => { Log(aplicar ? "Escaneo permanente WiFi/BT desactivado" : "Escaneo WiFi/BT restaurado"); RefrescarHistorial(); }, true);
        }

        void Informacion()
        {
            Tarea("Leyendo información", () =>
            {
                var a = motor.Adb;
                var props = a.Propiedades();
                var mem = a.Memoria();
                var alm = a.Almacenamiento();
                string inicio = a.InicioActual(), teclado = a.TecladoActual();
                Func<string, string> P = k => props.ContainsKey(k) ? props[k] : "?";
                var l = new List<string>
                {
                    "Tipo: " + motor.TipoDispositivo,
                    "Marca: " + P("marca") + "  (" + P("fabricante") + ")", "Modelo: " + P("modelo"),
                    "Android: " + P("android") + "  (SDK " + P("sdk") + ")" + (props.ContainsKey("fireos") ? "  ·  Fire OS " + props["fireos"] : ""),
                    "Compilación: " + P("compilacion"), "Procesador: " + P("plataforma") + "  ·  " + P("abi"), "Serie: " + P("serie"), "",
                    "RAM total: " + (mem.ContainsKey("MemTotal") ? mem["MemTotal"] : 0) + " MB",
                    "RAM disponible: " + (mem.ContainsKey("MemAvailable") ? mem["MemAvailable"] : 0) + " MB",
                    string.Format("Almacenamiento: {0:0.0} GB libres de {1:0.0} GB", alm[2] / 1024.0, alm[0] / 1024.0), "",
                    "Inicio actual: " + (inicio ?? "sin predeterminado"), "Teclado: " + (teclado ?? "?"),
                    "Play Store: " + (motor.TienePlayStore ? "sí" : "no"), "Paquetes escaneados: " + motor.Paquetes.Count,
                };
                return string.Join("\n", l);
            }, t => Dialogo.Info(this, t), true);
        }

        void Captura()
        {
            Tarea("Capturando pantalla", () =>
            {
                var datos = motor.Adb.Captura();
                string ruta = Path.Combine(Path.Combine(motor.Datos, "capturas"), "captura_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                File.WriteAllBytes(ruta, datos);
                return ruta;
            }, ruta =>
            {
                Log("Captura guardada: " + ruta);
                if (Dialogo.Pregunta(this, "Captura guardada en:\n" + ruta + "\n\n¿Abrirla?")) AbrirRuta(ruta);
            }, true);
        }

        void Reiniciar()
        {
            if (!Dialogo.Pregunta(this, "¿Reiniciar el dispositivo ahora?\nTendrás que volver a conectar cuando encienda.")) return;
            Tarea<object>("Reiniciando", () => { motor.Adb.Reiniciar(); motor.Adb.Desconectar(); return null; }, r =>
            {
                MarcarConectado(false);
                Log("Dispositivo reiniciándose. Espera ~1 minuto y vuelve a pulsar Conectar.");
            }, true);
        }

        // ───────── Launcher ─────────
        Panel ConstruirLauncher()
        {
            var p = new Panel();
            var col = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Tema.Fondo, Padding = new Padding(0, 6, 0, 0) };
            col.Controls.Add(Tema.Etiqueta("Pantalla de inicio sin anuncios", Tema.Subtitulo));
            var d = Tema.Etiqueta("Los inicios de Google TV y Fire TV muestran publicidad y recomendaciones patrocinadas. " +
                                  "FLauncher es gratuito, de código abierto y sin anuncios.", Tema.Base, Tema.Tenue);
            d.MaximumSize = new Size(1000, 0);
            col.Controls.Add(d);
            var pasos = Tema.Etiqueta("1. Instala FLauncher (botón de abajo: Play Store del TV, o un APK desde 'Mis apps').\n" +
                                      "2. Pulsa 'Actualizar lista' y elige FLauncher.\n" +
                                      "3. Pulsa 'Usar como inicio'. TV Cleaner lo pone como predeterminado, verifica que funcione y luego " +
                                      "desactiva el launcher anterior. Si algo falla, lo revierte solo.");
            pasos.MaximumSize = new Size(1000, 0);
            pasos.Padding = new Padding(10);
            pasos.BackColor = Tema.Tarjeta;
            col.Controls.Add(pasos);
            lblAvisoLauncher = Tema.Etiqueta("", Tema.Negrita, Tema.Aviso);
            lblAvisoLauncher.MaximumSize = new Size(1000, 0);
            col.Controls.Add(lblAvisoLauncher);
            var fila = new FlowLayoutPanel { AutoSize = true, BackColor = Tema.Fondo, Margin = new Padding(0, 6, 0, 6) };
            bTiendaFLauncher = Tema.Boton("Abrir FLauncher en Play Store del TV", (s, e) =>
                Orden("Abriendo Play Store", () => motor.Adb.AbrirTienda("me.efesser.flauncher"), "Play Store abierto en el TV. Pulsa Instalar con el control."), Tema.EstiloBoton.Acento);
            var bApk = Tema.Boton("Instalar launcher desde APK", (s, e) => { MostrarPestana(1); SeleccionarApks(); });
            var bLista = Tema.Boton("Actualizar lista", (s, e) => ListarLaunchers());
            foreach (var b in new[] { bTiendaFLauncher, bApk, bLista }) { fila.Controls.Add(b); controlesDispositivo.Add(b); }
            col.Controls.Add(fila);
            lblInicio = Tema.Etiqueta("Inicio actual: —", Tema.Base, Tema.Tenue);
            col.Controls.Add(lblInicio);
            lbLaunchers = new ListBox { Width = 900, Height = 150, BackColor = Tema.Tarjeta, ForeColor = Tema.Texto, BorderStyle = BorderStyle.None, Font = Tema.Base };
            col.Controls.Add(lbLaunchers);
            var fila2 = new FlowLayoutPanel { AutoSize = true, BackColor = Tema.Fondo };
            var bUsar = Tema.Boton("Usar como inicio", (s, e) => UsarLauncher(), Tema.EstiloBoton.Ok);
            controlesDispositivo.Add(bUsar);
            fila2.Controls.Add(bUsar);
            chkDejarAnterior = Tema.Casilla("Dejar activo el launcher anterior", false);
            fila2.Controls.Add(chkDejarAnterior);
            col.Controls.Add(fila2);
            col.Resize += (s, e) => lbLaunchers.Width = Math.Max(400, col.ClientSize.Width - 30);
            p.Controls.Add(col);
            return p;
        }

        /// <summary>Ajusta textos y botones según sea Fire TV, Google TV, TV Box, etc.</summary>
        void AjustarSegunDispositivo()
        {
            bTiendaFLauncher.Enabled = motor.TienePlayStore;
            if (motor.EsFireTv)
                lblAvisoLauncher.Text = "Fire TV: desde Fire OS 7.6 Amazon no permite reemplazar la pantalla de inicio (el botón Inicio siempre " +
                                        "vuelve al de Amazon). Puedes instalar FLauncher y abrirlo como app, pero no desactives el inicio de Amazon.";
            else if (!motor.TienePlayStore)
                lblAvisoLauncher.Text = "Este dispositivo no tiene Play Store: instala FLauncher con un APK desde 'Mis apps'.";
            else lblAvisoLauncher.Text = "";
        }

        void ListarLaunchers()
        {
            Tarea("Leyendo launchers", () => new object[] { motor.Adb.InicioActual(), motor.Adb.CandidatosInicio() }, r =>
            {
                string inicio = (string)r[0];
                launchers = (List<string>)r[1];
                lbLaunchers.Items.Clear();
                foreach (var c in launchers)
                {
                    string pkg = c.Split('/')[0];
                    bool actual = inicio != null && inicio.StartsWith(pkg + "/");
                    lbLaunchers.Items.Add("  " + BaseDatos.NombreAmigable(pkg) + "  —  " + c + (actual ? "   ← ACTUAL" : ""));
                }
                lblInicio.Text = "Inicio actual: " + (inicio ?? "sin predeterminado");
                if (!launchers.Any(c => c.StartsWith("me.efesser.flauncher/"))) Log("FLauncher no está instalado todavía.");
            }, true);
        }

        void UsarLauncher()
        {
            if (lbLaunchers.SelectedIndex < 0 || lbLaunchers.SelectedIndex >= launchers.Count)
            {
                Dialogo.Info(this, "Pulsa 'Actualizar lista' y elige un launcher.");
                return;
            }
            string comp = launchers[lbLaunchers.SelectedIndex];
            bool desactivar = !chkDejarAnterior.Checked && !motor.EsFireTv;
            if (!Dialogo.Pregunta(this, "Usar como inicio:\n" + comp + "\n\n" + (desactivar
                ? "Luego se desactivará el launcher anterior (reversible)." : "El launcher anterior quedará activo."))) return;
            Tarea("Cambiando launcher", () =>
            {
                string m;
                bool ok = motor.CambiarLauncher(comp, desactivar, out m);
                return new object[] { ok, m };
            }, r =>
            {
                Log((string)r[1]);
                if ((bool)r[0]) Dialogo.Info(this, (string)r[1]); else Dialogo.Aviso(this, (string)r[1]);
                RefrescarHistorial();
                ListarLaunchers();
            }, true);
        }

        // ───────── Control remoto ─────────
        Panel ConstruirControl()
        {
            var p = new Panel();
            var col = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Tema.Fondo, Padding = new Padding(0, 6, 0, 0) };
            col.Controls.Add(Tema.Etiqueta("Control remoto desde la PC", Tema.Subtitulo));
            col.Controls.Add(Tema.Etiqueta("También puedes usar las flechas, Enter, Esc e Inicio del teclado mientras esta pestaña está abierta.", Tema.Pequena, Tema.Tenue));
            var pad = new TableLayoutPanel { ColumnCount = 3, RowCount = 3, AutoSize = true, BackColor = Tema.Fondo, Margin = new Padding(0, 10, 0, 10) };
            var teclas = new[] { null, "▲|KEYCODE_DPAD_UP", null, "◄|KEYCODE_DPAD_LEFT", "OK|KEYCODE_DPAD_CENTER", "►|KEYCODE_DPAD_RIGHT", null, "▼|KEYCODE_DPAD_DOWN", null };
            for (int i = 0; i < 9; i++)
            {
                if (teclas[i] == null) continue;
                var partes = teclas[i].Split('|');
                string codigo = partes[1];
                var b = Tema.Boton(partes[0], (s, e) => EnviarTecla(codigo));
                b.AutoSize = false;
                b.Size = new Size(70, 48);
                controlesDispositivo.Add(b);
                pad.Controls.Add(b, i % 3, i / 3);
            }
            col.Controls.Add(pad);
            var fila = new FlowLayoutPanel { AutoSize = true, BackColor = Tema.Fondo };
            foreach (var t in new[] { "Inicio|KEYCODE_HOME", "Atrás|KEYCODE_BACK", "Menú|KEYCODE_MENU", "Vol −|KEYCODE_VOLUME_DOWN",
                                      "Vol +|KEYCODE_VOLUME_UP", "Silencio|KEYCODE_VOLUME_MUTE", "Play/Pausa|KEYCODE_MEDIA_PLAY_PAUSE",
                                      "Apagar pantalla|KEYCODE_SLEEP", "Encender|KEYCODE_WAKEUP" })
            {
                var partes = t.Split('|');
                string codigo = partes[1];
                var b = Tema.Boton(partes[0], (s, e) => EnviarTecla(codigo));
                controlesDispositivo.Add(b);
                fila.Controls.Add(b);
            }
            col.Controls.Add(fila);
            var fila2 = new FlowLayoutPanel { AutoSize = true, BackColor = Tema.Fondo, Margin = new Padding(0, 12, 0, 0) };
            fila2.Controls.Add(Tema.Etiqueta("Escribir en el dispositivo:"));
            txtEscribir = Tema.CajaTexto(320);
            fila2.Controls.Add(txtEscribir);
            var bEnviar = Tema.Boton("Enviar texto", (s, e) =>
            {
                string texto = txtEscribir.Text;
                if (texto.Length == 0) return;
                Tarea<object>("Escribiendo", () => { motor.Adb.EscribirTexto(texto); return null; }, r => { txtEscribir.Clear(); Log("Texto enviado."); }, true);
            });
            controlesDispositivo.Add(bEnviar);
            fila2.Controls.Add(bEnviar);
            col.Controls.Add(fila2);
            p.Controls.Add(col);
            return p;
        }

        internal void EnviarTecla(string codigo)
        {
            if (motor.Adb.Serial == null) return;
            System.Threading.Tasks.Task.Run(() =>
            {
                try { motor.Adb.Tecla(codigo); }
                catch (AdbException e) { Log("ERROR: " + e.Message); }
            });
        }
    }
}
