#!/usr/bin/env python3
"""ADB falso que simula un dispositivo para pruebas. Estado en $FAKEADB_STATE (JSON).
FAKEADB_DEVICE = googletv (por defecto) | firetv | tvbox.  No toca ningún dispositivo real."""
import json, os, re, sys, time
from pathlib import Path

STATE = Path(os.environ.get("FAKEADB_STATE", Path(__file__).with_name("fake_state.json")))
TV_IP = os.environ.get("FAKEADB_IP", "192.168.1.2")

SYSTEM = """android com.android.systemui com.android.tv.settings com.google.android.gms com.google.android.gsf
com.android.vending com.android.location.fused com.google.android.tv.remote.service com.google.android.tungsten.setupwraith
com.google.android.tv.axel com.google.android.ext.services com.google.android.webview com.android.providers.tv
com.android.providers.settings com.android.providers.media com.android.providers.downloads com.android.packageinstaller
com.android.permissioncontroller com.android.bluetooth com.android.shell com.google.android.leanback.ime
com.android.externalstorage com.android.keychain com.android.certinstaller com.google.android.apps.tv.launcherx
com.google.android.tvrecommendations com.google.android.feedback com.google.android.tv.bugreportsender
com.google.android.apps.tv.netoscope com.google.android.onetimeinitializer com.google.android.partnersetup
com.google.android.backuptransport com.google.android.katniss com.google.android.apps.tv.dreamx
com.google.android.apps.mediashell com.google.android.youtube.tv com.google.android.youtube.tvmusic
com.google.android.play.games com.google.android.videos com.google.android.tts com.google.android.marvin.talkback
com.google.android.configupdater com.android.dreams.basic com.android.wallpaper.livepicker com.android.printspooler
com.android.htmlviewer com.android.providers.partnerbookmarks tv.alphonso.alphonso_eula com.sony.dtv.promos
com.sony.dtv.sonyselect com.sony.dtv.smarthelp com.sony.dtv.tvx com.sony.dtv.timers com.sony.dtv.osdcoreservice
com.mediatek.tvinput com.mtk.tvsystemservice com.vendor.unknownthing com.android.inputdevices com.android.se
com.android.wallpaperbackup com.android.statementservice com.google.android.overlay.modules.ext.services
com.android.galaxy4 com.android.email com.netflix.ninja com.amazon.amazonvideo.livingroom""".split()

THIRD = ["com.disney.disneyplus", "com.kiloo.subwaysurf", "tv.pluto.android", "com.example.myapp"]
DEVICE = os.environ.get("FAKEADB_DEVICE", "googletv")
FIRE = """android com.android.systemui com.amazon.tv.launcher com.amazon.venezia com.amazon.tv.acr com.amazon.dp.logger
com.amazon.hybridadidservice com.amazon.device.software.ota com.amazon.tv.oobe com.amazon.tv.settings.v2
com.amazon.tv.inputpreference.service com.android.providers.settings com.amazon.gamehub com.amazon.tahoe
com.amazon.tv.ime com.android.shell com.amazon.spiderpork""".split()
BOX = """android com.android.systemui com.android.settings com.droidlogic com.droidlogic.tvinput com.droidlogic.appinstall
com.android.launcher3 com.softwinner.explore com.android.providers.settings com.android.inputmethod.latin com.android.shell
com.android.galaxy4 com.mbox.weirdthing""".split()


def default_state():
    base = {"firetv": FIRE, "tvbox": BOX}.get(DEVICE, SYSTEM)
    pk = {p: {"system": True, "enabled": True, "installed": True} for p in base}
    for p in THIRD:
        pk[p] = {"system": False, "enabled": True, "installed": True}
    if "com.google.android.feedback" in pk:
        pk["com.google.android.feedback"]["enabled"] = False   # ya desactivado de fábrica
    home = {"firetv": "com.amazon.tv.launcher/.ui.HomeActivity_vNext", "tvbox": "com.android.launcher3/.Launcher"}.get(
        DEVICE, "com.google.android.apps.tv.launcherx/com.google.android.apps.tv.launcherx.home.HomeActivity")
    ime = {"firetv": "com.amazon.tv.ime/.FireTVIME", "tvbox": "com.android.inputmethod.latin/.LatinIME"}.get(
        DEVICE, "com.google.android.leanback.ime/.LeanbackImeService")
    return {"connected": False, "usb": DEVICE == "tvbox", "authorized": os.environ.get("FAKEADB_AUTH", "1") == "1", "auth_wait": 0,
            "packages": pk, "home": home,
            "settings": {"global/window_animation_scale": "1.0", "global/transition_animation_scale": "1.0",
                         "global/animator_duration_scale": "1.0", "global/wifi_scan_always_enabled": "1",
                         "secure/default_input_method": ime,
                         "secure/enabled_accessibility_services": "null"},
            "calls": []}


def load():
    if STATE.exists():
        return json.loads(STATE.read_text())
    return default_state()


def save(s):
    STATE.write_text(json.dumps(s, indent=1))


def out(t="", code=0):
    if t:
        sys.stdout.write(t + "\n")
    sys.exit(code)


LAUNCHER_ACT = {"com.google.android.apps.tv.launcherx": "com.google.android.apps.tv.launcherx.home.HomeActivity",
                "me.efesser.flauncher": "me.efesser.flauncher.MainActivity",
                "com.amazon.tv.launcher": ".ui.HomeActivity_vNext", "com.android.launcher3": ".Launcher"}


def homes(s):
    return [f"{p}/{a}" for p, a in LAUNCHER_ACT.items()
            if p in s["packages"] and s["packages"][p]["installed"] and s["packages"][p]["enabled"]]


def shell(s, cmd):
    s["calls"].append(cmd)
    pk = s["packages"]
    a = cmd.split()
    if cmd == "echo ok":
        out("ok")
    if a[0] == "getprop":
        props = {"ro.product.brand": "Sony", "ro.product.manufacturer": "Sony", "ro.product.model": "KD-55X80K",
                 "ro.build.version.release": "12", "ro.build.version.sdk": "31", "ro.serialno": "SIM123456",
                 "ro.build.display.id": "PSL.SIM.1", "ro.product.name": "BRAVIA_SIM", "ro.board.platform": "mt5895",
                 "ro.product.cpu.abi": "armeabi-v7a"}
        if DEVICE == "firetv":
            props.update({"ro.product.brand": "Amazon", "ro.product.manufacturer": "Amazon", "ro.product.model": "AFTKA",
                          "ro.build.mktg.fireos": "7.6.6.9", "ro.build.version.release": "9", "ro.build.version.sdk": "28",
                          "ro.serialno": "G4N0FIRE"})
        if DEVICE == "tvbox":
            props.update({"ro.product.brand": "MBOX", "ro.product.manufacturer": "Amlogic", "ro.product.model": "X96 Max",
                          "ro.build.version.release": "9", "ro.build.version.sdk": "28", "ro.serialno": "BOX777",
                          "ro.board.platform": "g12a"})
        out(props.get(a[1], ""))
    if cmd == "pm list features":
        f = ["android.hardware.wifi", "android.software.leanback", "android.hardware.type.television"]
        if DEVICE == "firetv": f.append("amazon.hardware.fire_tv")
        if DEVICE == "tvbox": f.remove("android.software.leanback")
        out("\n".join("feature:" + x for x in f))
    if a[:2] == ["pm", "list"] and len(a) > 3 and a[2] == "packages" and not a[3].startswith("-"):
        out("\n".join("package:" + n for n, v in sorted(pk.items()) if a[3] in n and v["installed"]))
    if cmd == "pm list packages -3 --show-versioncode":
        out("\n".join(f"package:{n} versionCode:{len(n) * 7}" for n, v in sorted(pk.items()) if not v["system"] and v["installed"]))
    if a[:2] == ["dumpsys", "package"] and len(a) == 3:
        n = a[2]
        flags = "DEBUGGABLE TEST_ONLY" if pk.get(n, {}).get("testonly") else ""
        out(f"Packages:\n  Package [{n}]\n    versionCode=42 minSdk=21 targetSdk=33\n    versionName=1.4.2\n    flags=[ HAS_CODE {flags} ]\n"
            f"    firstInstallTime=2026-01-02 10:00:00\n    lastUpdateTime=2026-10-20 18:30:00\n    installerPackageName=null")
    if a[:2] == ["am", "force-stop"]:
        s["calls"].append("stopped " + a[2]); save(s); out("")
    if a[:2] == ["pm", "clear"]:
        out("Success")
    if a[0] == "monkey":
        n = a[2]
        if n in pk: out("Events injected: 1")
        out("** No activities found to run, monkey aborted.")
    if a[0] == "pidof":
        out("4242" if a[1] in pk else "")
    if a[0] == "logcat":
        out("10-24 21:00:00.000 I/MiApp( 4242): hola desde la app\n10-24 21:00:01.000 E/MiApp( 4242): error de prueba")
    if cmd == "cat /proc/meminfo":
        free = 900 + 15 * sum(1 for v in pk.values() if not v["enabled"] or not v["installed"])
        out(f"MemTotal:        2048000 kB\nMemFree:          300000 kB\nMemAvailable:    {free * 1024} kB\nCached: 400000 kB")
    if cmd == "df /data":
        out("Filesystem     1K-blocks    Used Available Use% Mounted on\n/dev/block/dm-5 8000000 5000000 3000000  63% /data")
    if a[:3] == ["pm", "list", "packages"]:
        f = a[3] if len(a) > 3 else ""
        res = []
        for n, v in sorted(pk.items()):
            if f == "-u":
                res.append(n); continue
            if not v["installed"]:
                continue
            if f == "-s" and not v["system"]: continue
            if f == "-3" and v["system"]: continue
            if f == "-d" and v["enabled"]: continue
            res.append(n)
        out("\n".join("package:" + n for n in res))
    if a[:4] == ["pm", "disable-user", "--user", "0"]:
        n = a[4]
        if n not in pk or not pk[n]["installed"]:
            out(f"Error: java.lang.IllegalArgumentException: Unknown package: {n}", 255)
        if n in ("com.google.android.gms", "android"):
            out(f"Error: java.lang.SecurityException: Cannot disable a protected package: {n}", 255)
        pk[n]["enabled"] = False
        if s["home"].startswith(n + "/"):
            h = homes(s)
            s["home"] = h[0] if h else "android/com.android.internal.app.ResolverActivity"
        save(s); out(f"Package {n} new state: disabled-user")
    if a[:2] == ["pm", "enable"]:
        n = a[2]
        if n not in pk or not pk[n]["installed"]:
            out(f"Error: java.lang.IllegalArgumentException: Unknown package: {n}", 255)
        pk[n]["enabled"] = True; save(s); out(f"Package {n} new state: enabled")
    if a[:2] == ["pm", "uninstall"]:
        n = a[-1]
        if n not in pk or not pk[n]["installed"]:
            out("Failure [DELETE_FAILED_INTERNAL_ERROR]", 1)
        if "--user" in a:
            pk[n]["installed"] = False
        else:
            if pk[n]["system"]:
                out("Failure [DELETE_FAILED_INTERNAL_ERROR]", 1)
            del pk[n]
        save(s); out("Success")
    if a[:3] == ["cmd", "package", "install-existing"] or a[:2] == ["pm", "install-existing"]:
        n = a[-1]
        if n in pk:
            pk[n]["installed"] = True; save(s); out(f"Package {n} installed for user: 0")
        out(f"Failure [NameNotFoundException] {n}", 1)
    if cmd == "pm path com.android.vending":
        out("package:/system/priv-app/Phonesky/Phonesky.apk" if "com.android.vending" in pk else "")
    if a[:2] == ["pm", "path"]:
        n = a[2]
        if n in pk and not pk[n]["system"]:
            out(f"package:/data/app/~~Xy12==/{n}-Ab3==/base.apk\npackage:/data/app/~~Xy12==/{n}-Ab3==/split_config.xhdpi.apk")
        out(f"package:/system/app/{n}/{n}.apk")
    if a[:2] == ["pm", "trim-caches"] or a[:2] == ["am", "kill-all"] or a[:2] == ["am", "start"]:
        out("")
    if a[:3] == ["cmd", "package", "bg-dexopt-job"]:
        time.sleep(1); out("Success")
    if a[0] == "settings":
        k = f"{a[2]}/{a[3]}"
        if a[1] == "get":
            out(s["settings"].get(k, "null"))
        if a[1] == "put":
            s["settings"][k] = a[4]; save(s); out("")
        if a[1] == "delete":
            s["settings"].pop(k, None); save(s); out("Deleted 1 rows")
    if a[:3] == ["cmd", "package", "resolve-activity"]:
        h = s["home"]
        pkg = h.split("/")[0]
        if pkg != "android" and not (pkg in pk and pk[pkg]["enabled"] and pk[pkg]["installed"]):
            hs = homes(s)
            h = hs[0] if len(hs) == 1 else "android/com.android.internal.app.ResolverActivity"
        out("priority=0 preferredOrder=0 match=0x108000 specificIndex=-1 isDefault=true\n" + h)
    if a[:3] == ["cmd", "package", "query-activities"]:
        hs = homes(s) + ["com.android.tv.settings/.FallbackHome"]
        body = "\n".join(f"  Activity #{i}:\n    priority=0 preferredOrder=0\n    {h}" for i, h in enumerate(hs))
        out(f"{len(hs)} activities found:\n{body}")
    if a[:3] == ["cmd", "package", "set-home-activity"]:
        if a[3].split("/")[0] in [h.split("/")[0] for h in homes(s)]:
            s["home"] = a[3]; save(s); out("Success")
        out("Error: component not found", 255)
    if a[:2] == ["input", "keyevent"] or a[:2] == ["input", "text"]:
        out("")
    if a[0] == "dumpsys":
        out(f"  mCurrentFocus=Window{{abc u0 {s['home']}}}")
    out(f"/system/bin/sh: {a[0]}: inaccessible or not found", 127)


def main():
    args = sys.argv[1:]
    s = load()
    if not args:
        out("usage", 1)
    if args[0] == "version":
        out("Android Debug Bridge version 1.0.41\nVersion 36.0.0-fake")
    if args[0] in ("start-server", "kill-server"):
        if args[0] == "kill-server":
            s["connected"] = False; save(s)
        out("")
    if args[0] == "connect":
        host = args[1].split(":")[0]
        if host != TV_IP:
            out(f"failed to connect to '{args[1]}': Connection timed out")
        s["connected"] = True
        if not s["authorized"]:
            s["auth_wait"] += 1
            if s["auth_wait"] >= 2:
                s["authorized"] = True
            save(s); out(f"failed to authenticate to {args[1]}")
        save(s); out(f"connected to {args[1]}")
    if args[0] == "disconnect":
        s["connected"] = False; save(s); out("disconnected everything")
    if args[0] == "mdns":
        out("List of discovered mdns services\nadb-SIMWIRELESS\t_adb-tls-connect._tcp.\t192.168.1.50:37123")
    if args[0] == "devices":
        lines = ["List of devices attached"]
        if s.get("usb"):
            lines.append("BOX777\tdevice usb:1-1 product:x96max model:X96_Max device:x96max transport_id:1")
        if s["connected"]:
            if not s["authorized"]:
                s["auth_wait"] += 1
                if s["auth_wait"] >= 3:
                    s["authorized"] = True
                save(s)
            lines.append(f"{TV_IP}:5555\t{'device' if s['authorized'] else 'unauthorized'}")
        out("\n".join(lines))
    if args[0] == "pair":
        out(f"Successfully paired to {args[1]} [guid=adb-SIM]" if args[2] == "123456" else "Failed: Wrong password or connection was dropped.", 0)
    if args[0] == "-s":
        serial, rest = args[1], args[2:]
        if not ((s["connected"] and serial == f"{TV_IP}:5555") or (s.get("usb") and serial == "BOX777")):
            sys.stderr.write(f"adb: device '{serial}' not found\n"); sys.exit(1)
        if rest[0] == "shell":
            shell(s, " ".join(rest[1:]))
        if rest[0] == "pull":
            Path(rest[2]).write_bytes(b"PK\x03\x04fakeapk"); out("1 file pulled")
        if rest[0] in ("install", "install-multiple"):
            opts = [x for x in rest[1:] if x.startswith("-")]
            files = [x for x in rest[1:] if not x.startswith("-")]
            s["calls"].append(" ".join(rest[:1] + opts) + " " + str(len(files)))
            for f in files:
                data = Path(f).read_bytes()[:200].decode("latin-1")
                if data.startswith("FAKEAPK:"):
                    parts = data.strip().split(":")
                    name = parts[1]
                    if "TEST_ONLY" in parts and "-t" not in opts:
                        save(s); out("Performing Streamed Install\nadb: failed to install " + f + ": Failure [INSTALL_FAILED_TEST_ONLY: installPackageLI]", 1)
                    if "ARM64" in parts:
                        save(s); out("Failure [INSTALL_FAILED_NO_MATCHING_ABIS: Failed to extract native libraries, res=-113]", 1)
                    if name in s["packages"] and "-r" not in opts:
                        save(s); out("Failure [INSTALL_FAILED_ALREADY_EXISTS: Attempt to re-install " + name + " without first uninstalling.]", 1)
                    s["packages"][name] = {"system": False, "enabled": True, "installed": True, "testonly": "TEST_ONLY" in parts}
                    continue
                name = Path(f).parent.name
                if re.fullmatch(r"[A-Za-z][\w.]+", name) and "." in name:
                    s["packages"][name] = {"system": False, "enabled": True, "installed": True}
                elif "flauncher" in f.lower():
                    s["packages"]["me.efesser.flauncher"] = {"system": False, "enabled": True, "installed": True}
            save(s); out("Performing Streamed Install\nSuccess")
        if rest[0] == "push":
            s["calls"].append("push " + Path(rest[1]).name + " " + rest[2]); save(s); out("1 file pushed, 0 skipped.")
        if rest[0] == "exec-out":
            import base64
            sys.stdout.buffer.write(base64.b64decode(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="))
            sys.exit(0)
        if rest[0] == "reboot":
            s["connected"] = False; save(s); out("")
    out(f"adb: unknown command {args[0]}", 1)


if __name__ == "__main__":
    main()
