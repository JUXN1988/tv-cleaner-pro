// Colores, fuentes y controles con el estilo oscuro del programa.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TVCleanerPro
{
    static class Tema
    {
        public static readonly Color Fondo = Color.FromArgb(0x0F, 0x14, 0x1A);
        public static readonly Color Panel = Color.FromArgb(0x16, 0x1C, 0x24);
        public static readonly Color Tarjeta = Color.FromArgb(0x1C, 0x24, 0x30);
        public static readonly Color Linea = Color.FromArgb(0x2A, 0x34, 0x41);
        public static readonly Color Texto = Color.FromArgb(0xE8, 0xED, 0xF2);
        public static readonly Color Tenue = Color.FromArgb(0x93, 0xA1, 0xB0);
        public static readonly Color Apagado = Color.FromArgb(0x5D, 0x6B, 0x7A);
        public static readonly Color Acento = Color.FromArgb(0x4D, 0xA3, 0xFF);
        public static readonly Color Ok = Color.FromArgb(0x3F, 0xB9, 0x7A);
        public static readonly Color Aviso = Color.FromArgb(0xE5, 0xA5, 0x3A);
        public static readonly Color Malo = Color.FromArgb(0xF0, 0x57, 0x4B);
        public static readonly Color Morado = Color.FromArgb(0xB4, 0x8C, 0xFF);
        public static readonly Color Cian = Color.FromArgb(0x58, 0xC4, 0xDC);
        public static readonly Color Amarillo = Color.FromArgb(0xD9, 0xC2, 0x5B);
        public static readonly Color Seleccion = Color.FromArgb(0x23, 0x40, 0x5F);

        static readonly string Familia = ElegirFamilia();
        public static readonly Font Base = new Font(Familia, 9.5f);
        public static readonly Font Negrita = new Font(Familia, 9.5f, FontStyle.Bold);
        public static readonly Font Pequena = new Font(Familia, 8.5f);
        public static readonly Font Titulo = new Font(Familia, 15f, FontStyle.Bold);
        public static readonly Font Subtitulo = new Font(Familia, 11.5f, FontStyle.Bold);
        public static readonly Font Mono = new Font(Utilidades.EsWindows ? "Consolas" : FontFamily.GenericMonospace.Name, 9f);

        static string ElegirFamilia()
        {
            foreach (var f in new[] { "Segoe UI", "Inter", "Noto Sans", "DejaVu Sans" })
            {
                using (var prueba = new Font(f, 10f))
                    if (prueba.Name == f) return f;
            }
            return SystemFonts.DefaultFont.FontFamily.Name;
        }

        public static Color ColorNivel(Nivel n)
        {
            switch (n)
            {
                case Nivel.Bloqueado: return Malo;
                case Nivel.Seguro: return Ok;
                case Nivel.Precaucion: return Aviso;
                default: return Morado;
            }
        }

        public static Color ColorCategoria(string c)
        {
            switch (c)
            {
                case "TELEMETRÍA": return Morado;
                case "PUBLICIDAD": return Aviso;
                case "STREAMING": return Acento;
                case "JUEGOS": return Ok;
                case "GOOGLE": return Cian;
                case "FABRICANTE": return Amarillo;
                case "BLOATWARE": return Tenue;
                case "APPS INSTALADAS": return Color.FromArgb(0x9F, 0xB4, 0xC7);
                case "SISTEMA": return Malo;
                default: return Texto;
            }
        }

        public enum EstiloBoton { Normal, Acento, Ok, Peligro }

        public static Button Boton(string texto, EventHandler clic, EstiloBoton estilo = EstiloBoton.Normal)
        {
            var b = new Button();
            b.Text = texto;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.FlatStyle = FlatStyle.Flat;
            b.Padding = new Padding(8, 3, 8, 3);
            b.Margin = new Padding(3);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            Color fondo, texto2 = Texto, borde = Linea;
            switch (estilo)
            {
                case EstiloBoton.Acento: fondo = Acento; texto2 = Color.FromArgb(6, 18, 31); borde = Acento; b.Font = Negrita; break;
                case EstiloBoton.Ok: fondo = Color.FromArgb(0x1E, 0x4D, 0x36); texto2 = Color.FromArgb(0xD3, 0xF5, 0xE3); break;
                case EstiloBoton.Peligro: fondo = Color.FromArgb(0x5A, 0x23, 0x20); texto2 = Color.FromArgb(0xFF, 0xD9, 0xD5); break;
                default: fondo = Tarjeta; break;
            }
            if (b.Font != Negrita) b.Font = Base;
            b.BackColor = fondo;
            b.ForeColor = texto2;
            b.FlatAppearance.BorderColor = borde;
            b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(fondo, 0.25f);
            b.FlatAppearance.MouseDownBackColor = ControlPaint.Light(fondo, 0.45f);
            if (clic != null) b.Click += clic;
            return b;
        }

        public static Label Etiqueta(string texto, Font fuente = null, Color? color = null)
        {
            var l = new Label();
            l.Text = texto;
            l.AutoSize = true;
            l.Font = fuente ?? Base;
            l.ForeColor = color ?? Texto;
            l.BackColor = Color.Transparent;
            l.Margin = new Padding(3, 6, 3, 3);
            return l;
        }

        public static TextBox CajaTexto(int ancho)
        {
            var t = new TextBox();
            t.Width = ancho;
            t.BackColor = Tarjeta;
            t.ForeColor = Texto;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Font = Base;
            t.Margin = new Padding(3, 4, 3, 3);
            return t;
        }

        public static CheckBox Casilla(string texto, bool marcado)
        {
            var c = new CheckBox();
            c.Text = texto;
            c.Checked = marcado;
            c.AutoSize = true;
            c.ForeColor = Texto;
            c.BackColor = Color.Transparent;
            c.Font = Base;
            c.Margin = new Padding(6, 7, 3, 3);
            return c;
        }

        public static ListView Lista()
        {
            var lv = new ListView();
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.HideSelection = false;
            lv.BackColor = Tarjeta;
            lv.ForeColor = Texto;
            lv.BorderStyle = BorderStyle.None;
            lv.Font = Base;
            if (Utilidades.EsWindows)
            {
                // encabezado oscuro (en Windows el encabezado normal es blanco)
                lv.OwnerDraw = true;
                lv.DrawColumnHeader += (s, e) =>
                {
                    using (var bg = new SolidBrush(Panel)) e.Graphics.FillRectangle(bg, e.Bounds);
                    using (var p = new Pen(Linea)) e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    var r = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
                    TextRenderer.DrawText(e.Graphics, e.Header.Text, Pequena, r, Tenue,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                };
                lv.DrawItem += (s, e) => { e.DrawDefault = true; };
                lv.DrawSubItem += (s, e) => { e.DrawDefault = true; };
            }
            return lv;
        }

        public static RichTextBox Detalle()
        {
            var r = new RichTextBox();
            r.ReadOnly = true;
            r.BorderStyle = BorderStyle.None;
            r.BackColor = Tarjeta;
            r.ForeColor = Texto;
            r.Font = Base;
            r.DetectUrls = false;
            r.ScrollBars = RichTextBoxScrollBars.Vertical;
            return r;
        }

        public static void Agregar(RichTextBox r, string texto, Font f = null, Color? c = null)
        {
            r.SelectionStart = r.TextLength;
            r.SelectionLength = 0;
            r.SelectionFont = f ?? Base;
            r.SelectionColor = c ?? Texto;
            r.AppendText(texto);
        }

        /// <summary>Fila horizontal que se acomoda sola.</summary>
        public static FlowLayoutPanel Fila(Color fondo)
        {
            var f = new FlowLayoutPanel();
            f.AutoSize = true;
            f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            f.WrapContents = true;
            f.BackColor = fondo;
            f.Dock = DockStyle.Top;
            f.Padding = new Padding(4);
            return f;
        }
    }
}
