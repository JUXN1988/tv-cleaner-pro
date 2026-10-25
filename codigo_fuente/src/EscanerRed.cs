// Escáner de red: busca televisores y equipos multimedia de CUALQUIER marca en TODAS las redes de la PC.
// Métodos combinados: puertos TCP conocidos, mDNS (Bonjour), SSDP/UPnP, tabla ARP y nombre DNS.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TVCleanerPro
{
    public class EquipoRed
    {
        public string IP;
        public string Nombre = "";
        public string Marca = "";
        public string Modelo = "";
        public string Tipo = "";
        public string Mac = "";
        public string Interfaz = "";
        public int PuertoAdb;                  // 0 = no detectado
        public bool AdbInalambrico;            // depuración inalámbrica (Android 11+)
        public readonly SortedSet<int> Puertos = new SortedSet<int>();
        public readonly HashSet<string> Servicios = new HashSet<string>();
        public readonly HashSet<string> Pistas = new HashSet<string>();

        /// <summary>¿Usa Android? (entonces TV Cleaner puede trabajar con él).</summary>
        public bool EsAndroid
        {
            get
            {
                return PuertoAdb > 0 || Tipo.Contains("Android") || Tipo.Contains("Google TV") || Tipo.Contains("Fire TV") ||
                       Tipo.Contains("Shield") || Tipo.Contains("TV Box");
            }
        }

        public string Compatibilidad
        {
            get
            {
                if (PuertoAdb > 0 && !AdbInalambrico) return "Listo para conectar (ADB " + PuertoAdb + ")";
                if (AdbInalambrico) return "Depuración inalámbrica (puerto " + PuertoAdb + "): usa Emparejar";
                if (EsAndroid) return "Android: activa 'Depuración ADB' en el dispositivo";
                if (Tipo.Length > 0 && Tipo != "Equipo de red") return "No es Android: no se puede limpiar por ADB";
                return "";
            }
        }
    }

    public class RangoIp
    {
        public uint Inicio, Fin;
        public string Interfaz = "";

        public int Cantidad
        {
            get { return (int)Math.Min(int.MaxValue, (long)Fin - Inicio + 1); }
        }

        public override string ToString()
        {
            return Ip(Inicio) + " - " + Ip(Fin) + (Interfaz.Length > 0 ? "  (" + Interfaz + ")" : "");
        }

        public static uint ANumero(IPAddress ip)
        {
            var b = ip.GetAddressBytes();
            return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        }

        public static string Ip(uint n)
        {
            return string.Format("{0}.{1}.{2}.{3}", n >> 24, (n >> 16) & 255, (n >> 8) & 255, n & 255);
        }
    }

    public class EscanerRed
    {
        /// <summary>Descripción legible de los puertos abiertos de un equipo.</summary>
        public static string DescribirPuertos(EquipoRed e)
        {
            var l = new List<string>();
            foreach (var p in e.Puertos)
            {
                string d;
                l.Add(PuertosTv.TryGetValue(p, out d) ? p + " " + d : p.ToString());
            }
            if (e.AdbInalambrico) l.Add(e.PuertoAdb + " ADB inalámbrico");
            return string.Join(", ", l);
        }

        // Puertos que delatan a un TV o reproductor: número -> pista
        public static readonly Dictionary<int, string> PuertosTv = new Dictionary<int, string>
        {
            { 5555, "ADB (Android)" }, { 8009, "Google Cast" }, { 8008, "Google Cast" },
            { 6466, "Android TV Remote" }, { 6467, "Android TV Remote" }, { 8060, "Roku" },
            { 8001, "Samsung Tizen" }, { 8002, "Samsung Tizen" }, { 3000, "¿LG webOS?" }, { 3001, "¿LG webOS?" },
            { 1925, "Philips" }, { 1926, "Philips" }, { 55000, "Panasonic" }, { 36669, "Hisense" },
            { 52323, "Sony" },
        };

        static readonly string[] ServiciosMdns =
        {
            "_googlecast._tcp.local", "_androidtvremote2._tcp.local", "_androidtvremote._tcp.local",
            "_adb-tls-connect._tcp.local", "_adb-tls-pairing._tcp.local", "_adb._tcp.local",
            "_amzn-wplay._tcp.local", "_airplay._tcp.local", "_spotify-connect._tcp.local",
            "_hap._tcp.local", "_roku-rcp._tcp.local", "_lgtv._tcp.local", "_samsungmsf._tcp.local",
        };

        public int MaxHostsPorRed = 4094;        // /20
        public int TiempoConexionMs = 450;
        public int Paralelo = 384;

        readonly ConcurrentDictionary<string, EquipoRed> equipos = new ConcurrentDictionary<string, EquipoRed>();
        public Action<EquipoRed> AlEncontrar = delegate { };
        public Action<string> Log = delegate { };

        EquipoRed Equipo(string ip)
        {
            return equipos.GetOrAdd(ip, k => new EquipoRed { IP = k });
        }

        // ───────── redes de la PC ─────────
        public List<RangoIp> RedesLocales()
        {
            var res = new List<RangoIp>();
            string forzado = Environment.GetEnvironmentVariable("TVCLEANER_SCAN_RANGE");
            if (!string.IsNullOrEmpty(forzado)) return InterpretarRangos(forzado);
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                IPInterfaceProperties props;
                try { props = ni.GetIPProperties(); } catch (Exception) { continue; }
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    string s = ua.Address.ToString();
                    if (s.StartsWith("127.") || s.StartsWith("169.254.")) continue;
                    int prefijo = 24;
                    try
                    {
                        if (ua.IPv4Mask != null && !ua.IPv4Mask.Equals(IPAddress.Any))
                            prefijo = ContarBits(RangoIp.ANumero(ua.IPv4Mask));
                    }
                    catch (Exception) { }
                    // redes enormes (/8, /16): se escanea el bloque más cercano a la PC (hasta /20)
                    int efectivo = Math.Max(prefijo, 32 - (int)Math.Ceiling(Math.Log(MaxHostsPorRed + 2, 2)));
                    if (efectivo > 30) efectivo = 30;
                    uint ip = RangoIp.ANumero(ua.Address);
                    uint mascara = efectivo == 0 ? 0 : 0xFFFFFFFFu << (32 - efectivo);
                    uint red = ip & mascara;
                    uint difusion = red | ~mascara;
                    var r = new RangoIp { Inicio = red + 1, Fin = difusion - 1, Interfaz = ni.Name + (prefijo < efectivo ? " (parcial /" + efectivo + ")" : "") };
                    if (!res.Exists(x => x.Inicio == r.Inicio && x.Fin == r.Fin)) res.Add(r);
                }
            }
            return res;
        }

        static int ContarBits(uint m)
        {
            int c = 0;
            while (m != 0) { c += (int)(m & 1); m >>= 1; }
            return c;
        }

        /// <summary>Acepta: 192.168.1.0/24, 192.168.1.10-192.168.1.200, 192.168.1.10-200, 10.0.0.5 (separados por coma).</summary>
        public static List<RangoIp> InterpretarRangos(string texto)
        {
            var res = new List<RangoIp>();
            foreach (var parte0 in texto.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string parte = parte0.Trim();
                IPAddress a, b;
                var m = Regex.Match(parte, @"^([\d.]+)/(\d{1,2})$");
                if (m.Success && IPAddress.TryParse(m.Groups[1].Value, out a))
                {
                    int p = int.Parse(m.Groups[2].Value);
                    if (p < 16 || p > 32) throw new ArgumentException("Prefijo no permitido (usa /16 a /32): " + parte);
                    uint mask = p == 0 ? 0 : 0xFFFFFFFFu << (32 - p);
                    uint red = RangoIp.ANumero(a) & mask;
                    uint fin = red | ~mask;
                    res.Add(p >= 31 ? new RangoIp { Inicio = red, Fin = fin, Interfaz = "manual" }
                                    : new RangoIp { Inicio = red + 1, Fin = fin - 1, Interfaz = "manual" });
                    continue;
                }
                m = Regex.Match(parte, @"^([\d.]+)-([\d.]+)$");
                if (m.Success && IPAddress.TryParse(m.Groups[1].Value, out a))
                {
                    string fin = m.Groups[2].Value;
                    if (!fin.Contains(".")) fin = m.Groups[1].Value.Substring(0, m.Groups[1].Value.LastIndexOf('.') + 1) + fin;
                    if (!IPAddress.TryParse(fin, out b)) throw new ArgumentException("Rango no válido: " + parte);
                    uint x = RangoIp.ANumero(a), y = RangoIp.ANumero(b);
                    if (y < x) { uint t = x; x = y; y = t; }
                    if ((long)y - x > 65535) throw new ArgumentException("Rango demasiado grande (máx. 65.536 direcciones): " + parte);
                    res.Add(new RangoIp { Inicio = x, Fin = y, Interfaz = "manual" });
                    continue;
                }
                if (IPAddress.TryParse(parte, out a) && a.AddressFamily == AddressFamily.InterNetwork)
                {
                    uint n = RangoIp.ANumero(a);
                    res.Add(new RangoIp { Inicio = n, Fin = n, Interfaz = "manual" });
                    continue;
                }
                throw new ArgumentException("No se entiende el rango: " + parte);
            }
            return res;
        }

        // ───────── escaneo completo ─────────
        public List<EquipoRed> Escanear(List<RangoIp> rangos, Action<int, int> progreso, CancellationToken cancelar)
        {
            equipos.Clear();
            var tareasExtra = new List<Task>
            {
                Task.Run(() => BuscarMdns(cancelar)),
                Task.Run(() => BuscarSsdp(cancelar)),
                Task.Run(() => LeerArp()),
            };

            var hosts = new List<uint>();
            var vistos = new HashSet<uint>();
            foreach (var r in rangos)
                for (uint n = r.Inicio; n <= r.Fin && n >= r.Inicio; n++)
                {
                    if (vistos.Add(n)) hosts.Add(n);
                    if (n == uint.MaxValue) break;
                }
            Log(string.Format("Escaneando {0} direcciones en {1} red(es), {2} puertos de TV cada una…", hosts.Count, rangos.Count, PuertosTv.Count));

            var puertos = PuertosTv.Keys.ToArray();
            int total = hosts.Count * puertos.Length, hechos = 0;
            using (var sem = new SemaphoreSlim(Paralelo))
            {
                var tareas = new List<Task>();
                foreach (var h in hosts)
                {
                    if (cancelar.IsCancellationRequested) break;
                    string ip = RangoIp.Ip(h);
                    foreach (var p in puertos)
                    {
                        sem.Wait();
                        if (cancelar.IsCancellationRequested) { sem.Release(); break; }
                        int puerto = p;
                        tareas.Add(Task.Run(async () =>
                        {
                            try
                            {
                                if (await Probar(ip, puerto)) Marcar(ip, puerto);
                            }
                            finally
                            {
                                sem.Release();
                                int k = Interlocked.Increment(ref hechos);
                                if (progreso != null && (k % 200 == 0 || k == total)) progreso(k, total);
                            }
                        }));
                    }
                }
                Task.WaitAll(tareas.ToArray());
            }
            try { Task.WaitAll(tareasExtra.ToArray(), 8000); } catch (AggregateException) { }

            // nombre DNS de lo encontrado (máx. ~2 s en total)
            var propias = new HashSet<string>(IpsLocales().Select(x => x.ToString()));
            var interesantes = equipos.Values.Where(e => !propias.Contains(e.IP) &&
                (e.Puertos.Count > 0 || e.Servicios.Count > 0 || e.Pistas.Count > 0)).ToList();
            var dns = interesantes.Select(e => Task.Run(() =>
            {
                try
                {
                    var t = Dns.GetHostEntryAsync(e.IP);
                    if (t.Wait(1500) && t.Result.HostName != e.IP && e.Nombre.Length == 0) e.Nombre = t.Result.HostName;
                }
                catch (Exception) { }
            })).ToArray();
            Task.WaitAll(dns, 2500);

            foreach (var e in interesantes) Identificar(e);
            return interesantes
                .OrderByDescending(e => e.PuertoAdb > 0)
                .ThenByDescending(e => e.EsAndroid)
                .ThenByDescending(e => e.Tipo.Length > 0 && e.Tipo != "Equipo de red")
                .ThenBy(e => RangoIp.ANumero(IPAddress.Parse(e.IP)))
                .ToList();
        }

        async Task<bool> Probar(string ip, int puerto)
        {
            using (var c = new TcpClient())
            {
                try
                {
                    var t = c.ConnectAsync(ip, puerto);
                    var fin = await Task.WhenAny(t, Task.Delay(TiempoConexionMs));
                    if (fin != t) return false;
                    await t;
                    return c.Connected;
                }
                catch (Exception) { return false; }
            }
        }

        void Marcar(string ip, int puerto)
        {
            var e = Equipo(ip);
            lock (e)
            {
                e.Puertos.Add(puerto);
                if (puerto == 5555 && e.PuertoAdb == 0) e.PuertoAdb = 5555;
            }
            Identificar(e);
            AlEncontrar(e);
        }

        // ───────── identificación ─────────
        public static void Identificar(EquipoRed e)
        {
            lock (e)
            {
                string pistas = string.Join(" ", e.Pistas).ToLowerInvariant() + " " + e.Marca.ToLowerInvariant() + " " +
                                e.Modelo.ToLowerInvariant() + " " + e.Nombre.ToLowerInvariant();
                string tipo;
                if (e.Servicios.Contains("_amzn-wplay") || pistas.Contains("amazon") || pistas.Contains("fire tv") || Regex.IsMatch(pistas, @"\baft[a-z0-9]{1,6}\b"))
                    tipo = "Fire TV";
                else if (pistas.Contains("shield") || pistas.Contains("nvidia"))
                    tipo = "NVIDIA Shield (Android TV)";
                else if (e.Servicios.Contains("_androidtvremote2") || e.Servicios.Contains("_androidtvremote") || e.Puertos.Contains(6466))
                    tipo = pistas.Contains("google tv") || pistas.Contains("chromecast") ? "Google TV" : "Android TV / Google TV";
                else if (e.PuertoAdb > 0 || e.Servicios.Contains("_adb-tls-connect") || e.Servicios.Contains("_adb"))
                    tipo = "Android (TV Box / TV)";
                else if (e.Puertos.Contains(8060) || pistas.Contains("roku"))
                    tipo = "Roku";
                else if (pistas.Contains("samsung") || (e.Puertos.Contains(8001) && e.Puertos.Contains(8002)) || e.Servicios.Contains("_samsungmsf"))
                    tipo = "Samsung (Tizen)";
                else if (pistas.Contains("webos") || pistas.Contains("lg electronics") || pistas.Contains("lge ") || e.Servicios.Contains("_lgtv") || (e.Puertos.Contains(3000) && e.Puertos.Contains(3001)))
                    tipo = "LG (webOS)";
                else if (pistas.Contains("philips") || e.Puertos.Contains(1925) || e.Puertos.Contains(1926))
                    tipo = "Philips TV";
                else if (pistas.Contains("hisense") || pistas.Contains("vidaa") || e.Puertos.Contains(36669))
                    tipo = "Hisense (VIDAA)";
                else if (pistas.Contains("panasonic") || e.Puertos.Contains(55000))
                    tipo = "Panasonic TV";
                else if (pistas.Contains("sony") || pistas.Contains("bravia"))
                    tipo = "Sony BRAVIA";
                else if (e.Servicios.Contains("_googlecast") || e.Puertos.Contains(8009) || e.Puertos.Contains(8008))
                    tipo = pistas.Contains("chromecast") ? "Chromecast" : "Equipo con Google Cast";
                else if (e.Servicios.Contains("_airplay"))
                    tipo = "Equipo AirPlay (Apple TV / TV compatible)";
                else if (pistas.Contains("mediarenderer") || pistas.Contains("dial") || pistas.Contains("tv"))
                    tipo = "TV / reproductor multimedia";
                else
                    tipo = "Equipo de red";
                e.Tipo = tipo;
            }
        }

        // ───────── ARP ─────────
        void LeerArp()
        {
            if (!Utilidades.EsWindows) return;
            try
            {
                var psi = new ProcessStartInfo("arp", "-a")
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true
                };
                using (var p = Process.Start(psi))
                {
                    string salida = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);
                    foreach (Match m in Regex.Matches(salida, @"(\d+\.\d+\.\d+\.\d+)\s+([0-9a-fA-F]{2}(?:-[0-9a-fA-F]{2}){5})"))
                    {
                        string ip = m.Groups[1].Value;
                        if (ip.StartsWith("224.") || ip.StartsWith("239.") || ip.EndsWith(".255")) continue;
                        EquipoRed e;
                        if (equipos.TryGetValue(ip, out e)) e.Mac = m.Groups[2].Value.ToUpperInvariant();
                        else Equipo(ip).Mac = m.Groups[2].Value.ToUpperInvariant();
                    }
                }
            }
            catch (Exception) { }
        }

        static List<IPAddress> IpsLocales()
        {
            var res = new List<IPAddress>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !ua.Address.ToString().StartsWith("169.254."))
                            res.Add(ua.Address);
                }
            }
            catch (Exception) { }
            return res;
        }

        // ───────── SSDP / UPnP ─────────
        void BuscarSsdp(CancellationToken cancelar)
        {
            const string msg = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: ssdp:all\r\n\r\n";
            var datos = Encoding.ASCII.GetBytes(msg);
            var ubicaciones = new ConcurrentDictionary<string, string>();   // location -> ip
            var sockets = new List<UdpClient>();
            foreach (var ip in IpsLocales())
            {
                try
                {
                    var u = new UdpClient(new IPEndPoint(ip, 0));
                    u.Client.ReceiveTimeout = 500;
                    for (int k = 0; k < 2; k++) u.Send(datos, datos.Length, new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900));
                    sockets.Add(u);
                }
                catch (Exception) { }
            }
            DateTime fin = DateTime.Now.AddSeconds(4);
            while (DateTime.Now < fin && !cancelar.IsCancellationRequested && sockets.Count > 0)
            {
                foreach (var u in sockets)
                {
                    try
                    {
                        while (u.Available > 0)
                        {
                            var ep = new IPEndPoint(IPAddress.Any, 0);
                            var b = u.Receive(ref ep);
                            ProcesarSsdp(ep.Address.ToString(), Encoding.UTF8.GetString(b), ubicaciones);
                        }
                    }
                    catch (Exception) { }
                }
                Thread.Sleep(50);
            }
            foreach (var u in sockets) u.Close();

            // descargar la descripción de cada equipo: nombre, fabricante y modelo
            var tareas = ubicaciones.Select(kv => Task.Run(() => LeerDescripcionUpnp(kv.Key, kv.Value))).ToArray();
            Task.WaitAll(tareas, 6000);
        }

        void ProcesarSsdp(string ip, string resp, ConcurrentDictionary<string, string> ubicaciones)
        {
            var h = InterpretarCabeceras(resp);
            var e = Equipo(ip);
            string v;
            lock (e)
            {
                if (h.TryGetValue("server", out v)) e.Pistas.Add(v);
                if (h.TryGetValue("st", out v)) e.Pistas.Add(v);
                if (h.TryGetValue("usn", out v) && v.ToLowerInvariant().Contains("roku")) e.Pistas.Add("roku");
            }
            if (h.TryGetValue("location", out v) && Regex.IsMatch(v, @"^http://" + Regex.Escape(ip) + @"[:/]"))
                ubicaciones.TryAdd(v, ip);
            Identificar(e);
            AlEncontrar(e);
        }

        public static Dictionary<string, string> InterpretarCabeceras(string resp)
        {
            var h = new Dictionary<string, string>();
            foreach (var l in resp.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = l.IndexOf(':');
                if (i > 0) h[l.Substring(0, i).Trim().ToLowerInvariant()] = l.Substring(i + 1).Trim();
            }
            return h;
        }

        void LeerDescripcionUpnp(string url, string ip)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 2500;
                req.ReadWriteTimeout = 2500;
                req.Proxy = null;
                using (var resp = req.GetResponse())
                using (var s = new System.IO.StreamReader(resp.GetResponseStream()))
                {
                    var buf = new char[65536];
                    int n = s.ReadBlock(buf, 0, buf.Length);
                    var x = DescripcionUpnp(new string(buf, 0, n));
                    var e = Equipo(ip);
                    lock (e)
                    {
                        if (x[0].Length > 0 && e.Nombre.Length == 0) e.Nombre = x[0];
                        if (x[1].Length > 0) e.Marca = x[1];
                        if (x[2].Length > 0) e.Modelo = x[2];
                        if (x[3].Length > 0) e.Pistas.Add(x[3]);
                    }
                    Identificar(e);
                    AlEncontrar(e);
                }
            }
            catch (Exception) { }
        }

        /// <summary>{nombre, fabricante, modelo, descripción} de un XML de dispositivo UPnP.</summary>
        public static string[] DescripcionUpnp(string xml)
        {
            Func<string, string> tag = t =>
            {
                var m = Regex.Match(xml, "<" + t + @">\s*([^<]{1,120})\s*</" + t + ">", RegexOptions.IgnoreCase);
                return m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value.Trim()) : "";
            };
            string modelo = (tag("modelName") + " " + tag("modelNumber")).Trim();
            return new[] { tag("friendlyName"), tag("manufacturer"), modelo, tag("modelDescription") + " " + tag("deviceType") };
        }

        // ───────── mDNS ─────────
        void BuscarMdns(CancellationToken cancelar)
        {
            var consulta = ConsultaMdns(ServiciosMdns);
            var destino = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
            var sockets = new List<UdpClient>();
            foreach (var ip in IpsLocales())
            {
                try
                {
                    var u = new UdpClient(new IPEndPoint(ip, 0));
                    u.Send(consulta, consulta.Length, destino);
                    sockets.Add(u);
                }
                catch (Exception) { }
            }
            // también escuchar respuestas multicast (algunos equipos ignoran la petición de respuesta directa)
            try
            {
                var m = new UdpClient();
                m.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                m.Client.Bind(new IPEndPoint(IPAddress.Any, 5353));
                foreach (var ip in IpsLocales())
                {
                    try { m.JoinMulticastGroup(IPAddress.Parse("224.0.0.251"), ip); } catch (Exception) { }
                }
                sockets.Add(m);
            }
            catch (Exception) { }

            DateTime fin = DateTime.Now.AddSeconds(4);
            int reenvio = 0;
            while (DateTime.Now < fin && !cancelar.IsCancellationRequested && sockets.Count > 0)
            {
                foreach (var u in sockets)
                {
                    try
                    {
                        while (u.Available > 0)
                        {
                            var ep = new IPEndPoint(IPAddress.Any, 0);
                            var b = u.Receive(ref ep);
                            ProcesarMdns(ep.Address.ToString(), b);
                        }
                    }
                    catch (Exception) { }
                }
                if (++reenvio == 30)   // repetir la pregunta a los 1.5 s
                    foreach (var u in sockets) { try { u.Send(consulta, consulta.Length, destino); } catch (Exception) { } }
                Thread.Sleep(50);
            }
            foreach (var u in sockets) u.Close();
        }

        public static byte[] ConsultaMdns(string[] servicios)
        {
            var ms = new System.IO.MemoryStream();
            ms.Write(new byte[] { 0, 0, 0, 0, 0, (byte)servicios.Length, 0, 0, 0, 0, 0, 0 }, 0, 12);
            foreach (var s in servicios)
            {
                foreach (var etiqueta in s.Split('.'))
                {
                    var b = Encoding.ASCII.GetBytes(etiqueta);
                    ms.WriteByte((byte)b.Length);
                    ms.Write(b, 0, b.Length);
                }
                ms.WriteByte(0);
                ms.Write(new byte[] { 0, 12, 0x80, 1 }, 0, 4);   // tipo PTR, clase IN con bit "respuesta directa"
            }
            return ms.ToArray();
        }

        public class RegistroDns
        {
            public string Nombre;
            public int Tipo;
            public string Texto;       // PTR, nombre destino de SRV, A
            public int Puerto;         // SRV
            public Dictionary<string, string> Txt = new Dictionary<string, string>();
        }

        /// <summary>Lee un paquete DNS (con compresión de nombres) y devuelve sus registros.</summary>
        public static List<RegistroDns> LeerPaqueteDns(byte[] p)
        {
            var res = new List<RegistroDns>();
            if (p.Length < 12) return res;
            int qd = (p[4] << 8) | p[5], an = (p[6] << 8) | p[7], ns = (p[8] << 8) | p[9], ar = (p[10] << 8) | p[11];
            int pos = 12;
            for (int i = 0; i < qd; i++) { LeerNombre(p, ref pos); pos += 4; }
            int total = an + ns + ar;
            for (int i = 0; i < total && pos + 10 <= p.Length; i++)
            {
                var r = new RegistroDns();
                r.Nombre = LeerNombre(p, ref pos);
                if (pos + 10 > p.Length) break;
                r.Tipo = (p[pos] << 8) | p[pos + 1];
                int largo = (p[pos + 8] << 8) | p[pos + 9];
                pos += 10;
                int inicio = pos;
                if (pos + largo > p.Length) break;
                switch (r.Tipo)
                {
                    case 1:
                        if (largo == 4) r.Texto = string.Format("{0}.{1}.{2}.{3}", p[pos], p[pos + 1], p[pos + 2], p[pos + 3]);
                        break;
                    case 12:
                        { int q = pos; r.Texto = LeerNombre(p, ref q); }
                        break;
                    case 33:
                        if (largo >= 7)
                        {
                            r.Puerto = (p[pos + 4] << 8) | p[pos + 5];
                            int q = pos + 6;
                            r.Texto = LeerNombre(p, ref q);
                        }
                        break;
                    case 16:
                        {
                            int q = pos;
                            while (q < inicio + largo)
                            {
                                int l = p[q++];
                                if (l == 0 || q + l > inicio + largo) break;
                                string kv = Encoding.UTF8.GetString(p, q, l);
                                q += l;
                                int ig = kv.IndexOf('=');
                                if (ig > 0) r.Txt[kv.Substring(0, ig).ToLowerInvariant()] = kv.Substring(ig + 1);
                            }
                        }
                        break;
                }
                pos = inicio + largo;
                res.Add(r);
            }
            return res;
        }

        static string LeerNombre(byte[] p, ref int pos)
        {
            var partes = new List<string>();
            int saltos = 0, q = pos;
            bool salto = false;
            while (q < p.Length)
            {
                int l = p[q];
                if (l == 0) { q++; break; }
                if ((l & 0xC0) == 0xC0)
                {
                    if (q + 1 >= p.Length || ++saltos > 20) break;
                    int destino = ((l & 0x3F) << 8) | p[q + 1];
                    if (!salto) { pos = q + 2; salto = true; }
                    q = destino;
                    continue;
                }
                q++;
                if (q + l > p.Length) break;
                partes.Add(Encoding.UTF8.GetString(p, q, l));
                q += l;
            }
            if (!salto) pos = q;
            return string.Join(".", partes);
        }

        void ProcesarMdns(string ipOrigen, byte[] paquete)
        {
            List<RegistroDns> regs;
            try { regs = LeerPaqueteDns(paquete); } catch (Exception) { return; }
            if (regs.Count == 0) return;
            // dirección real del equipo: registro A si existe, si no, quien respondió
            var direcciones = new Dictionary<string, string>();
            foreach (var r in regs) if (r.Tipo == 1 && r.Texto != null) direcciones[r.Nombre.ToLowerInvariant()] = r.Texto;
            foreach (var r in regs)
            {
                if (r.Tipo != 12 && r.Tipo != 33 && r.Tipo != 16) continue;
                string instancia = r.Tipo == 12 ? r.Texto ?? "" : r.Nombre;
                var m = Regex.Match(instancia, @"^(.*?)\.(_[\w\-]+)\._tcp\.local$", RegexOptions.IgnoreCase);
                if (!m.Success) continue;
                string nombreInst = m.Groups[1].Value, servicio = m.Groups[2].Value.ToLowerInvariant();
                string ip = ipOrigen;
                if (r.Tipo == 33 && r.Texto != null && direcciones.ContainsKey(r.Texto.ToLowerInvariant()))
                    ip = direcciones[r.Texto.ToLowerInvariant()];
                var e = Equipo(ip);
                lock (e)
                {
                    e.Servicios.Add(servicio);
                    e.Pistas.Add(servicio);
                    if (r.Tipo == 33 && (servicio == "_adb-tls-connect" || servicio == "_adb"))
                    {
                        e.PuertoAdb = r.Puerto;
                        e.AdbInalambrico = servicio == "_adb-tls-connect" && r.Puerto != 5555;
                    }
                    if (r.Tipo == 16)
                    {
                        string v;
                        if (r.Txt.TryGetValue("fn", out v) && v.Length > 0) e.Nombre = v;          // Google Cast: nombre
                        if (r.Txt.TryGetValue("md", out v) && v.Length > 0) { e.Modelo = v; e.Pistas.Add(v); }
                        if (r.Txt.TryGetValue("model", out v) && v.Length > 0) { e.Modelo = v; e.Pistas.Add(v); }
                        if (r.Txt.TryGetValue("manufacturer", out v) && v.Length > 0) e.Marca = v;
                    }
                    if (e.Nombre.Length == 0 && !servicio.StartsWith("_adb") && !Regex.IsMatch(nombreInst, @"^[0-9a-f\-]{16,}$"))
                        e.Nombre = Regex.Replace(nombreInst, @"\\(\d{3})", x => ((char)int.Parse(x.Groups[1].Value)).ToString());
                }
                Identificar(e);
                AlEncontrar(e);
            }
        }
    }
}
