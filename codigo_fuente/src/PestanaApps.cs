// Pestaña "Mis apps": instalar apps propias (APK, APK divididos, .apks/.xapk/.apkm) y administrar las instaladas.
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
        internal CheckBox chkReemplazar, chkPrueba, chkAnterior, chkPermisos, chkJuntos;
        internal ListView lvApps;
        RichTextBox rtApp;
        TextBox txtBuscarApp;
        Dictionary<string, string> appsUsuario = new Dictionary<string, string>();

        Panel ConstruirApps()
        {
            var p = new Panel();

            // ── zona de instalación ──
            var zona = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Tema.Tarjeta, Padding = new Padding(14, 10, 14, 10), AllowDrop = true };
            var t = Tema.Etiqueta("Instalar apps (las tuyas o descargadas)", Tema.Subtitulo);
            t.Location = new Point(14, 10);
            zona.Controls.Add(t);
            var ayuda = Tema.Etiqueta("Arrastra aquí archivos .apk, .apks, .xapk o .apkm, o pulsa 'Seleccionar archivos'. " +
                                      "Puedes elegir varios: cada app se instala por separado.", Tema.Pequena, Tema.Tenue);
            ayuda.Location = new Point(16, 40);
            zona.Controls.Add(ayuda);
            var opciones = new FlowLayoutPanel { Location = new Point(10, 64), AutoSize = true, BackColor = Tema.Tarjeta, WrapContents = true, MaximumSize = new Size(1100, 0) };
            chkReemplazar = Tema.Casilla("Reemplazar conservando datos (-r)", true);
            chkPrueba = Tema.Casilla("Permitir apps de prueba / debug (-t)", true);
            chkAnterior = Tema.Casilla("Permitir versión anterior (-d)", false);
            chkPermisos = Tema.Casilla("Conceder todos los permisos (-g)", false);
            chkJuntos = Tema.Casilla("Los .apk elegidos son partes de UNA misma app (APK divididos)", false);
            foreach (var c in new[] { chkReemplazar, chkPrueba, chkAnterior, chkPermisos, chkJuntos }) opciones.Controls.Add(c);
            zona.Controls.Add(opciones);
            var bSel = Tema.Boton("Seleccionar archivos…", (s, e) => SeleccionarApks(), Tema.EstiloBoton.Acento);
            bSel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            zona.Resize += (s, e) => bSel.Location = new Point(zona.ClientSize.Width - bSel.Width - 16, 12);
            controlesDispositivo.Add(bSel);
            zona.Controls.Add(bSel);
            zona.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            zona.DragDrop += (s, e) => SoltarArchivos((string[])e.Data.GetData(DataFormats.FileDrop));

            // ── apps instaladas ──
            var barra = Tema.Fila(Tema.Fondo);
            barra.Controls.Add(Tema.Etiqueta("Apps instaladas por el usuario", Tema.Subtitulo));
            var bAct = Tema.Boton("Actualizar lista", (s, e) => CargarApps());
            controlesDispositivo.Add(bAct);
            barra.Controls.Add(bAct);
            barra.Controls.Add(Tema.Etiqueta("Buscar"));
            txtBuscarApp = Tema.CajaTexto(200);
            txtBuscarApp.TextChanged += (s, e) => LlenarApps();
            barra.Controls.Add(txtBuscarApp);

            var acciones = Tema.Fila(Tema.Panel);
            acciones.Dock = DockStyle.Bottom;
            var botones = new[]
            {
                Tema.Boton("Abrir en el TV", (s, e) => AccionApp("abrir"), Tema.EstiloBoton.Ok),
                Tema.Boton("Forzar cierre", (s, e) => AccionApp("cerrar")),
                Tema.Boton("Ajustes de la app", (s, e) => AccionApp("ajustes")),
                Tema.Boton("Ver registro (logcat)", (s, e) => AccionApp("logcat")),
                Tema.Boton("Extraer APK a la PC", (s, e) => AccionApp("extraer")),
                Tema.Boton("Borrar datos", (s, e) => AccionApp("borrar")),
                Tema.Boton("Desinstalar", (s, e) => AccionApp("desinstalar"), Tema.EstiloBoton.Peligro),
                Tema.Boton("Enviar archivos a Descargas…", (s, e) => EnviarArchivos()),
            };
            foreach (var b in botones) { acciones.Controls.Add(b); controlesDispositivo.Add(b); }

            rtApp = Tema.Detalle();
            var marco = new Panel { Dock = DockStyle.Right, Width = 320, BackColor = Tema.Tarjeta, Padding = new Padding(12) };
            rtApp.Dock = DockStyle.Fill;
            marco.Controls.Add(rtApp);
            Tema.Agregar(rtApp, "Tus apps\n", Tema.Subtitulo);
            Tema.Agregar(rtApp, "\nSelecciona una app para ver su versión, fechas y si es compilación de prueba.\n\n" +
                "Consejos para tus propias apps:\n• Una compilación 'debug' de Android Studio necesita la opción -t.\n" +
                "• Si cambias de firma (debug ↔ release), desinstala la anterior primero.\n" +
                "• Para TV, declara en el manifiesto la categoría LEANBACK_LAUNCHER y un banner, o tu app no aparecerá en el inicio.\n" +
                "• 'Ver registro' muestra el logcat solo de tu app mientras está abierta.", Tema.Pequena, Tema.Tenue);

            lvApps = Tema.Lista();
            lvApps.Dock = DockStyle.Fill;
            lvApps.MultiSelect = false;
            lvApps.Columns.Add("App", 220);
            lvApps.Columns.Add("Paquete", 300);
            lvApps.Columns.Add("Versión (código)", 120);
            lvApps.SelectedIndexChanged += (s, e) => DetalleAppSeleccionada();
            lvApps.DoubleClick += (s, e) => AccionApp("abrir");

            var centro = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0), BackColor = Tema.Fondo };
            centro.Controls.Add(lvApps);
            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 4), BackColor = Tema.Fondo };
            cuerpo.Controls.Add(centro);
            cuerpo.Controls.Add(marco);
            p.Controls.Add(cuerpo);
            p.Controls.Add(acciones);
            p.Controls.Add(barra);
            var sep = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Tema.Fondo };
            p.Controls.Add(sep);
            p.Controls.Add(zona);
            return p;
        }

        string[] OpcionesInstalacion()
        {
            var o = new List<string>();
            if (chkReemplazar.Checked) o.Add("-r");
            if (chkPrueba.Checked) o.Add("-t");
            if (chkAnterior.Checked) o.Add("-d");
            if (chkPermisos.Checked) o.Add("-g");
            return o.ToArray();
        }

        void SeleccionarApks()
        {
            using (var d = new OpenFileDialog())
            {
                d.Title = "Selecciona las apps a instalar";
                d.Filter = "Apps Android (*.apk;*.apks;*.xapk;*.apkm)|*.apk;*.apks;*.xapk;*.apkm|Todos (*.*)|*.*";
                d.Multiselect = true;
                if (d.ShowDialog(this) == DialogResult.OK) InstalarArchivos(d.FileNames);
            }
        }

        internal void SoltarArchivos(string[] archivos)
        {
            if (archivos == null) return;
            var validos = archivos.Where(Instalador.EsInstalable).ToArray();
            if (validos.Length == 0) { Dialogo.Aviso(this, "Arrastra archivos .apk, .apks, .xapk o .apkm."); return; }
            MostrarPestana(1);
            InstalarArchivos(validos);
        }

        internal void InstalarArchivos(string[] archivos)
        {
            if (motor.Adb.Serial == null) { Dialogo.Aviso(this, "Primero conecta el dispositivo."); return; }
            var opciones = OpcionesInstalacion();
            bool juntos = chkJuntos.Checked;
            Log("Instalando " + archivos.Length + " archivo(s) con opciones: " + string.Join(" ", opciones));
            Tarea("Instalando apps", () => instalador.Instalar(archivos, opciones, juntos, Progreso), res =>
            {
                int ok = res.Count(r => r.Ok);
                string msg = ok + " de " + res.Count + " instalada(s) correctamente.\n";
                foreach (var r in res) msg += "\n" + (r.Ok ? "✔ " : "✘ ") + r.Archivo + ": " + r.Mensaje.Replace("\n", "\n     ");
                if (ok == res.Count) Dialogo.Info(this, msg); else Dialogo.Aviso(this, msg);
                CargarApps();
            }, true);
        }

        internal void CargarApps()
        {
            Tarea("Leyendo apps instaladas", () => motor.Adb.AppsUsuario(), apps =>
            {
                appsUsuario = apps;
                LlenarApps();
                Log(apps.Count + " apps instaladas por el usuario.");
            }, true);
        }

        void LlenarApps()
        {
            string q = txtBuscarApp.Text.Trim().ToLowerInvariant();
            lvApps.BeginUpdate();
            lvApps.Items.Clear();
            foreach (var kv in appsUsuario.OrderBy(x => BaseDatos.NombreAmigable(x.Key), StringComparer.CurrentCultureIgnoreCase))
            {
                string nombre = BaseDatos.NombreAmigable(kv.Key);
                if (q.Length > 0 && !kv.Key.ToLowerInvariant().Contains(q) && !nombre.ToLowerInvariant().Contains(q)) continue;
                var it = new ListViewItem(new[] { nombre, kv.Key, kv.Value });
                it.Tag = kv.Key;
                lvApps.Items.Add(it);
            }
            lvApps.EndUpdate();
        }

        string AppSeleccionada()
        {
            return lvApps.SelectedItems.Count > 0 ? (string)lvApps.SelectedItems[0].Tag : null;
        }

        void DetalleAppSeleccionada()
        {
            string pkg = AppSeleccionada();
            if (pkg == null || Ocupado || motor.Adb.Serial == null) return;
            System.Threading.Tasks.Task.Run(() =>
            {
                Dictionary<string, string> d;
                try { d = motor.Adb.DetallesApp(pkg); } catch (Exception) { return; }
                UI(() =>
                {
                    if (AppSeleccionada() != pkg) return;
                    rtApp.Clear();
                    Tema.Agregar(rtApp, BaseDatos.NombreAmigable(pkg) + "\n", Tema.Subtitulo);
                    Tema.Agregar(rtApp, pkg + "\n\n", Tema.Mono, Tema.Tenue);
                    var etiquetas = new Dictionary<string, string>
                    {
                        { "versionName", "Versión" }, { "versionCode", "Código de versión" }, { "targetSdk", "targetSdk" },
                        { "minSdk", "minSdk" }, { "firstInstallTime", "Instalada" }, { "lastUpdateTime", "Actualizada" },
                        { "installerPackageName", "Instalada por" }, { "debuggable", "Depurable (debug)" }, { "testOnly", "Solo prueba (-t)" },
                    };
                    foreach (var kv in etiquetas)
                        if (d.ContainsKey(kv.Key))
                        {
                            Tema.Agregar(rtApp, kv.Value + ": ", Tema.Negrita);
                            Tema.Agregar(rtApp, d[kv.Key] + "\n");
                        }
                });
            });
        }

        void AccionApp(string accion)
        {
            string pkg = AppSeleccionada();
            if (pkg == null) { Dialogo.Info(this, "Selecciona una app de la lista (pulsa 'Actualizar lista' si está vacía)."); return; }
            switch (accion)
            {
                case "abrir":
                    Tarea("Abriendo app", () => { string t; bool ok = motor.Adb.AbrirApp(pkg, out t); return ok ? null : t; },
                        err => { if (err != null) Dialogo.Aviso(this, "No se pudo abrir " + pkg + ". ¿Tiene actividad de inicio (LAUNCHER o LEANBACK_LAUNCHER)?\n\n" + Utilidades.Recortar(err, 300)); else Log("Abierta: " + pkg); }, true);
                    break;
                case "cerrar":
                    Orden("Cerrando app", () => motor.Adb.ForzarCierre(pkg), "Cierre forzado: " + pkg);
                    break;
                case "ajustes":
                    Orden("Abriendo ajustes", () => motor.Adb.AbrirAjustesApp(pkg), "Ajustes de " + pkg + " abiertos en el TV.");
                    break;
                case "logcat":
                    Dialogo.VentanaTexto(this, "Registro (logcat) — " + pkg, () => motor.Adb.Logcat(pkg, 1500), Path.Combine(motor.Datos, "registros"));
                    break;
                case "extraer":
                    Tarea("Extrayendo APK", () => motor.RespaldarApk(pkg), lista =>
                    {
                        if (lista.Count == 0) { Dialogo.Aviso(this, "No se pudo extraer el APK."); return; }
                        Log("APK extraído: " + string.Join(", ", lista));
                        if (Dialogo.Pregunta(this, lista.Count + " archivo(s) guardado(s) en:\n" + Path.GetDirectoryName(lista[0]) + "\n\n¿Abrir la carpeta?"))
                            AbrirRuta(Path.GetDirectoryName(lista[0]));
                    }, true);
                    break;
                case "borrar":
                    if (!Dialogo.Pregunta(this, "¿Borrar TODOS los datos de " + pkg + "?\nSe pierden sesiones, configuración y archivos de la app. No se puede deshacer.", true)) return;
                    Tarea("Borrando datos", () => { string t; return motor.Adb.BorrarDatos(pkg, out t) ? null : t; },
                        err => { if (err != null) Dialogo.Aviso(this, err); else Log("Datos borrados: " + pkg); }, true);
                    break;
                case "desinstalar":
                    if (!Dialogo.Pregunta(this, "¿Desinstalar " + pkg + "?\nAntes se guardará una copia del APK en la PC para poder reinstalarla.", true)) return;
                    Tarea("Desinstalando", () =>
                    {
                        var apks = motor.RespaldarApk(pkg);
                        string t;
                        bool ok = motor.Adb.Desinstalar(pkg, out t);
                        if (ok && motor.Historial != null && apks.Count > 0) motor.Historial.AgregarAccion(pkg, "desinstalada", apks);
                        return ok ? null : t;
                    }, err =>
                    {
                        if (err != null) Dialogo.Aviso(this, Instalador.Explicar(err));
                        else { Log("Desinstalada: " + pkg); RefrescarHistorial(); CargarApps(); }
                    }, true);
                    break;
            }
        }

        void EnviarArchivos()
        {
            if (motor.Adb.Serial == null) { Dialogo.Aviso(this, "Primero conecta el dispositivo."); return; }
            using (var d = new OpenFileDialog { Multiselect = true, Title = "Archivos a enviar a la carpeta Descargas del dispositivo" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                var archivos = d.FileNames;
                Tarea("Enviando archivos", () =>
                {
                    var fallos = new List<string>();
                    int k = 0;
                    foreach (var a in archivos)
                    {
                        Progreso(++k, archivos.Length, Path.GetFileName(a));
                        var r = motor.Adb.Enviar(a, "/sdcard/Download/");
                        if (r.Codigo != 0) fallos.Add(Path.GetFileName(a) + ": " + r.Todo);
                    }
                    return fallos;
                }, fallos => Dialogo.Info(this, (archivos.Length - fallos.Count) + " archivo(s) enviados a Descargas (/sdcard/Download)." +
                                                (fallos.Count > 0 ? "\n\nFallaron:\n" + string.Join("\n", fallos) : "")), true);
            }
        }
    }
}
