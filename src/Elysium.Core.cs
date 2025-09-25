using System;
using System.IO;
using KSP;
using KSP.UI.Screens;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

using ElysiumKSP.Mod;

namespace ElysiumKSP
{
    public interface ICelestialEvents
    {
        void OnOrbitChanged(Vessel v);
        void OnSOIChanged(Vessel v, CelestialBody oldBody, CelestialBody newBody);
        void OnLandingDetected(Vessel v, CelestialBody body, double lat, double lon);
    }

    public interface ISaveEvents
    {
        void OnGameSaved(Game game);
        void OnGameLoaded(Game game);
        void OnVesselLoaded(Vessel vessel);
        void OnVesselUnloaded(Vessel vessel);
    }

    public interface IVesselPhysicsEvents
    {
        void OnHighSpeed(Vessel vessel, double speed);
        void OnAtmosphereEntry(Vessel vessel, CelestialBody body);
        void OnAtmosphereExit(Vessel vessel, CelestialBody body);
        void OnOrbitDecayWarning(Vessel vessel, double timeToDecay);
        void OnCollisionWarning(Vessel vessel, Vessel other, double distance);
    }

    public enum EventPriority
    {
        High,
        Normal,
        Low
    }

    public class BaseEvent
    {
        public bool IsCancelled { get; private set; }

        public void Cancel()
        {
            IsCancelled = true;
        }

        public void UnCancel()
        {
            IsCancelled = false;
        }
    }

    public class KSPGameObjects
    {
        public static double GetResourceAmount(Vessel v, string resourceName)
        {
            double total = 0;
            foreach (var part in v.parts)
            {
                PartResource res = part.Resources.Get(resourceName);
                if (res != null) total += res.amount;
            }
            return total;
        }

        public static Transform FindTransform(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = FindTransform(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ElysiumAPIConfigButton : MonoBehaviour
    {
        private ApplicationLauncherButton btn;
        private Rect winRect = new Rect(200, 200, 400, 300);
        private bool sW = false;
        private string cfgPath;
        public float a = 1200f, b = 120f, c = 1000f, d = 10f, e = 10f, f = 50f, g = 5f, h = 8f;
        public bool aa = true;

        void Start()
        {
            cfgPath = KSPUtil.ApplicationRootPath + "/GameData/Elysium/Props/Elysium.cfg";
            LoadS();
            GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
        }

        private void OnAppLauncherReady()
        {
            if (btn == null) btn = ApplicationLauncher.Instance.AddModApplication(OnTrue, OnFalse, null, null, null, null, ApplicationLauncher.AppScenes.MAINMENU, GameDatabase.Instance.GetTexture("Elysium/Icons/elysium_01", false));
        }

        private void OnTrue() { sW = true; }
        private void OnFalse() { sW = false; }

        void OnGUI()
        {
            if (sW) winRect = GUILayout.Window(GetHashCode(), winRect, DrawWindow, "Elysium API Config / Конфиг ПО Elysium");
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("High Speed Event: Activation speed (m/s)");
            a = GUILayout.HorizontalSlider(a, 50f, 4000f);
            GUILayout.Label(a.ToString("F0"));

            GUILayout.Label("Orbit Decay Event: Periapsis height (km)");
            b = GUILayout.HorizontalSlider(b, 0.001f, 79.999f);
            GUILayout.Label(b.ToString("F3"));

            GUILayout.Label("Vessel Collision Warning Event: Distance (m)");
            c = GUILayout.HorizontalSlider(c, 5f, 2299f);
            GUILayout.Label(c.ToString("F0"));

            GUILayout.Label("Apogee Reach Event: Accuracy (m)");
            d = GUILayout.HorizontalSlider(d, 1f, 1300f);
            GUILayout.Label(d.ToString("F3"));

            GUILayout.Label("Perigee Reach Event: Accuracy (m)");
            e = GUILayout.HorizontalSlider(e, 1f, 1500f);
            GUILayout.Label(e.ToString("F3"));

            GUILayout.Label("Low Fuel Warning Event: Threshold (units)");
            f = GUILayout.HorizontalSlider(f, 1f, 1500f);
            GUILayout.Label(f.ToString("F2"));

            GUILayout.Label("Low Electric Charge Warning Event: Threshold (units)");
            g = GUILayout.HorizontalSlider(g, 1f, 1500f);
            GUILayout.Label(g.ToString("F2"));

            GUILayout.Label("Vessel Impact Event: Speed (m/s)");
            h = GUILayout.HorizontalSlider(h, 8.2f, 399.9f);
            GUILayout.Label(h.ToString("F1"));

            GUILayout.Label("================ Elysium Internal Settings ================");

            GUILayout.Space(10);

            GUILayout.Label("Enable logging (Elysium/Logs/*.log, KSP.log)");
            aa = GUILayout.Toggle(aa, "TOUCH ME!");
            GUILayout.Label(aa.ToString());

            if (GUILayout.Button("Back to MainMenu")) sW = false;
            if (GUILayout.Button("Save")) SaveS();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void LoadS()
        {
            if (!File.Exists(cfgPath))
            {
                UnityEngine.Debug.Log("[ElysiumKSP]: Elysium/Props/Elysium.cfg not found, creating default...");
                SaveS();
                return;
            }

            ConfigNode nod = ConfigNode.Load(cfgPath);
            if (nod == null) return;
            ConfigNode cfg0 = nod.GetNode("ElysiumEventSystem");
            ConfigNode cfg1 = nod.GetNode("ElysiumKSP");
            if (cfg0 == null || cfg1 == null) return;

            a = float.Parse(cfg0.GetValue("highSpeedEvent_m_per_s"));
            b = float.Parse(cfg0.GetValue("orbitDecayPeriapsis_km"));
            c = float.Parse(cfg0.GetValue("vesselCollisionWarning_m"));
            d = float.Parse(cfg0.GetValue("apogeeReach_m"));
            e = float.Parse(cfg0.GetValue("perigeeReach_m"));
            f = float.Parse(cfg0.GetValue("lowFuelWarning_units"));
            g = float.Parse(cfg0.GetValue("lowElectricChargeWarning_units"));
            h = float.Parse(cfg0.GetValue("vesselImpact_m_per_s"));
            aa = bool.Parse(cfg1.GetValue("p_001_"));
        }

        private void SaveS()
        {
            ConfigNode n = new ConfigNode("ElysiumEventSystem");
            n.AddValue("highSpeedEvent_m_per_s", a);
            n.AddValue("orbitDecayPeriapsis_km", b);
            n.AddValue("vesselCollisionWarning_m", c);
            n.AddValue("apogeeReach_m", d);
            n.AddValue("perigeeReach_m", e);
            n.AddValue("lowFuelWarning_units", f);
            n.AddValue("lowElectricChargeWarning_units", g);
            n.AddValue("vesselImpact_m_per_s", h);
            ConfigNode o = new ConfigNode("ElysiumAPI");
            o.AddValue("p_001_", aa);

            ConfigNode sn = new ConfigNode();
            sn.AddNode(n);

            sn.Save(cfgPath);
            UnityEngine.Debug.Log("[ElysiumKSP]-[ElysiumEventSystem]: Saved settings from MainMenu in cfg => " + cfgPath);
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ModsConfigButton : MonoBehaviour
    {
        private ApplicationLauncherButton btn;
        private Rect winRect = new Rect(200, 200, 400, 300);
        private bool sW = false;
        private Vector2 scrollPos;

        private class ModInfo
        {
            public string FolderName, DllName, Version;
            public bool HasConfig;
        }

        private List<ModInfo> mods = new List<ModInfo>();

        void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
        }

        private void OnAppLauncherReady()
        {
            if (btn == null) btn = ApplicationLauncher.Instance.AddModApplication(OnTrue, OnFalse, null, null, null, null, ApplicationLauncher.AppScenes.MAINMENU, GameDatabase.Instance.GetTexture("Elysium/Icons/config_icon", false));
        }

        private void OnTrue() { sW = true; ScanMods(); }
        private void OnFalse() { sW = false; }

        private void ScanMods()
        {
            mods.Clear();
            string gdPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
            foreach (string folder in Directory.GetDirectories(gdPath))
            {
                string fn = Path.GetFileName(folder);
                if (fn == "Squad" || fn == "SquadExpansion") continue;

                foreach (string dll in Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories))
                {
                    FileVersionInfo ver = FileVersionInfo.GetVersionInfo(dll);
                    mods.Add(new ModInfo()
                    {
                        FolderName = fn,
                        DllName = Path.GetFileName(dll),
                        Version = string.IsNullOrEmpty(ver.FileVersion) ? "Unknown" : ver.FileVersion,
                        HasConfig = Directory.Exists(Path.Combine(folder, "ElysiumCfg"))
                    });
                }
            }
        }

        public void OnGUI()
        {
            if (sW) winRect = GUILayout.Window(GetHashCode(), winRect, DrawWindow, "Mods Config / Конфиги Модов");
        }

        private void DrawWindow(int windowID)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);

            foreach (var mod in mods)
            {
                GUILayout.Label(mod.FolderName, new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold
                });

                GUILayout.Label(mod.DllName + "  v" + mod.Version, new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12
                });

                if (mod.HasConfig) if (GUILayout.Button("Config")) { UnityEngine.Debug.Log("EEEEEEEEE"); }
                else GUILayout.Label("Not have config", new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.red }
                });

                GUILayout.Space(10);
            }

            GUILayout.EndScrollView();
            if (GUILayout.Button("Back to Main Menu")) sW = false;
            GUI.DragWindow();
        }

        public void OnDestroy()
        {
            if (btn != null) ApplicationLauncher.Instance.RemoveModApplication(btn);
        }
    }

    public class ModConfigParser
    {
        public class ConfigEntry
        {
            public string Key, Type;
            public object Value;
        }

        public static Dictionary<string, ConfigEntry> Load(string filePath)
        {
            var dict = new Dictionary<string, ConfigEntry>();

            if (!File.Exists(filePath)) return dict;

            foreach (var line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                string key = parts[0].Trim();
                string valueAndType = parts[1].Trim();

                int dashIdx = valueAndType.LastIndexOf("-(");
                if (dashIdx < 0) continue;

                string valueStr = valueAndType.Substring(0, dashIdx);
                string typeStr = valueAndType.Substring(dashIdx + 2).TrimEnd(')');

                object val = ParseValue(valueStr, typeStr);

                dict[key] = new ConfigEntry()
                {
                    Key = key,
                    Value = val,
                    Type = typeStr
                };
            }

            return dict;
        }

        public static void Save(string filePath, Dictionary<string, ConfigEntry> dict)
        {
            List<string> lines = new List<string>();
            foreach (var e in dict.Values) lines.Add(e.Key+"="+e.Value+"-("+e.Type+")");
            File.WriteAllLines(filePath, lines);
        }

        private static object ParseValue(string valueStr, string typeStr)
        {
            switch (typeStr.ToLower())
            {
                case "int": return int.Parse(valueStr);
                case "double": return double.Parse(valueStr);
                case "byte": return byte.Parse(valueStr);
                case "string": return valueStr;
                default: return valueStr;
            }
        }
    }
}