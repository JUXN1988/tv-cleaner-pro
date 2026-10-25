# Servicios TCP falsos para probar el escáner de red sin una red real.
import socket, threading, time, sys
def escuchar(ip, puerto):
    s = socket.socket(); s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1); s.bind((ip, puerto)); s.listen(50)
    while True:
        c, _ = s.accept(); c.close()
for ip, p in [("127.0.0.2", 5555), ("127.0.0.3", 6466), ("127.0.0.3", 8009), ("127.0.0.4", 8060), ("127.0.0.5", 3000)]:
    threading.Thread(target=escuchar, args=(ip, p), daemon=True).start()
time.sleep(float(sys.argv[1]) if len(sys.argv) > 1 else 120)
