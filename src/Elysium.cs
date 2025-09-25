using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI.Screens;
using System.IO;

using ElysiumKSP;
using ElysiumKSP.Optional;

namespace ElysiumKSP.Mod
{
    #region Attributes

    [AttributeUsage(AttributeTargets.Class)]
    public class KSPModElysiumAttribute : Attribute
    {
        public string ModName { get; private set; }
        public string Description { get; private set; }
        public string Id { get; private set; }
        //public GameScenes LoadIn { get; private set; }

        public KSPModElysiumAttribute(string modName, string description, string id)
        {
            ModName = modName;
            Description = description;
            Id = id;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SubscribeEventAttribute : Attribute
    {
        public EventPriority Priority { get; set; }
        public bool CanCancel { get; set; }

        public SubscribeEventAttribute(EventPriority Priority = EventPriority.Normal, bool CanCancel = false)
        {
            this.Priority = Priority;
            this.CanCancel = CanCancel;
        }
    }

    #endregion

    #region Events

    public interface IELifecycle
    {
        void OnMainMenu();
        void OnSpaceCenter();
        void OnEditor();
        void OnFlight();
    }

    public interface IEEvent
    {
        void OnResourceConsumed(Part p, string resName, double amount);
        void OnResourceProduced(Part p, string resName, double amount);
        void OnResourceUpdated(PartResource[] resources);

        void OnExpirimentDeployed(ScienceExperiment exp, Vessel v);
        void OnScienceReceived(float data, ScienceSubject subject, Vessel v, bool lab);

        void OnActionGroupTriggered(Vessel v, KSPActionGroup group);
        void OnThrottleChanged(Vessel v, float newThrottle);

        void OnVesselLauched(Vessel vessel);
        void OnVesselRecovered(Vessel vessel, bool quick);
        void OnVesselDestroyed(Vessel vessel);

        void OnPartAttached(Part p, Part target, Vessel vessel);
        void OnPartDetached(Part p, Vessel vessel);
        void OnStageActivate(Vessel vessel, int stageID);

        void OnSceneChanged(GameScenes scene);
    }

    interface IVessel
    {
        void ApogeeReach(Vessel v, double alt, double timeToAp);
        void PerigeeReach(Vessel v, double alt, double timeToPe);
        void Impact(Vessel v, CelestialBody body, double impactSpeed, Vector3 impactPos);
        void FuelLow(Vessel v, double currFuel, double threshold);
        void ElectricChargeLow(Vessel v, double currCharge, double threshold);
    }

    #endregion

    #region Helpers

    public class ElysiumLoggerClient
    {
        private static string logFP;
        private static StreamWriter sw;

        public ElysiumLoggerClient()
        {
            if (ElysiumLoader.Instance.GetConfig().aa)
            {
                string lp = KSPUtil.ApplicationRootPath + "/GameData/Elysium/Logs";
                Directory.CreateDirectory(lp);

                logFP = lp + "/elysiumksp-client-log.log";
                sw = new StreamWriter(logFP, true);

                LogH("Elysium KSP Version v0.0.1 prerelease-1 CLIENT log, at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
        }

        public void LogH(string msg)
        {
            try
            {
                if (sw!=null)sw.WriteLine(msg);
            } catch (Exception e)
            {
                Debug.LogError("[ElysiumKSP/Logger]: Failed to write log: " + e.Message);
            }
        }

        public void Log(string msg)
        {
            string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string fin = "[" + ts +"]:" + msg;

            try
            {
                if(sw!=null)sw.WriteLine(fin);
            } catch (Exception e)
            {
                Debug.LogError("[ElysiumKSP/Logger]: Failed to write log: " + e.Message);
            }
        }

        public void Close()
        {
            if(sw!=null)Log("ElysiumKSP CLIENT LOG END");
            if(sw!=null)Debug.Log("[ElysiumKSP]-[KSPClientLoggger]: Closed log, path -> " + logFP);
            if(sw!=null)sw.Close();
        }

        public class ModificableLogger
        {
            private static string logFP;
            private static StreamWriter sw;

            public ModificableLogger(string filename)
            {
                if (ElysiumLoader.Instance.GetConfig().aa)
                {
                    string lp = KSPUtil.ApplicationRootPath + "/GameData/Elysium/Logs";
                    Directory.CreateDirectory(lp);

                    logFP = lp + filename + ".log";
                    sw = new StreamWriter(logFP, true);

                    LogH("Elysium KSP Version v0.0.1 prerelease-1 CLIENT log, at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }
            }

            public void LogH(string msg)
            {
                try
                {
                    if(sw!=null)sw.WriteLine(msg);
                } catch (Exception e)
                {
                    Debug.LogError("[ElysiumKSP/Logger]: Failed to write log: " + e.Message);
                }
            }

            public void Log(string msg)
            {
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string fin = "[" + ts +"]:" + msg;

                try
                {
                    if(sw!=null)sw.WriteLine(fin);
                } catch (Exception e)
                {
                    Debug.LogError("[ElysiumKSP/Logger]: Failed to write log: " + e.Message);
                }
            }

            public void Close()
            {
                if(sw!=null)Log("ElysiumKSP CLIENT LOG END");
                if(sw!=null)Debug.Log("[ElysiumKSP]-[KSPClientLoggger]: Closed log, path -> " + logFP);
                if(sw!=null)sw.Close();
            }
        }
    }

    public class EventSubscriber
    {
        public object Target;
        public MethodInfo Method;
        public EventPriority Priority;
        public bool CanCancel;
    }

    public class ModEntry
    {
        public string Name, Id, Description, CacheID;
    }

    public class ModCacheWriter
    {
        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace(",", "\\,").Replace("=", "\\=");
        }

        private static string FormatLine(ModEntry m)
        {
            return string.Format("{0}={1},{2},{3}", Escape(m.Name), Escape(m.Id), Escape(m.Description), Escape(m.CacheID));
        }

        public static void AddMod(string path, ModEntry mod)
        {
            using (var sw = new StreamWriter(path, true, Encoding.UTF8))
            {
                sw.WriteLine(FormatLine(mod));
            }
        }
    }

    #endregion

    #region Main

    public static class EventBus
    {
        public static event Action<string> OnCustomEvent;
        private static Dictionary<Type, List<EventSubscriber>> subs = new Dictionary<Type, List<EventSubscriber>>();
        private static List<object> regMods = new List<object>();

        public static void Register(object mod)
        {
            if (!regMods.Contains(mod))
            {
                regMods.Add(mod);
                Debug.Log("[ElysiumKSP]-[EventBus]: Registered mod '" + mod.GetType().Name + "' in Elysium EventBus");
            }
        }

        public static void SubscribeAll(object target)
        {
            var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<SubscribeEventAttribute>();
                if (attr != null)
                {
                    var parames = method.GetParameters();
                    if (parames.Length == 1)
                    {
                        Type evType = parames[0].ParameterType;
                        if (!subs.ContainsKey(evType)) subs[evType] = new List<EventSubscriber>();

                        subs[evType].Add(new EventSubscriber
                        {
                            Target = target,
                            Method = method,
                            Priority = attr.Priority,
                            CanCancel = attr.CanCancel
                        });

                        subs[evType].Sort((a, b) => b.Priority.CompareTo(a.Priority));
                    }
                }
            }
        }

        public static void Publish<T>(T eventInst) where T : BaseEvent
        {
            List<EventSubscriber> list;
            if (!subs.TryGetValue(typeof(T), out list)) return;

            foreach (var sub in list)
            {
                sub.Method.Invoke(sub.Target, new object[] { eventInst });
                if (eventInst.IsCancelled && sub.CanCancel) break;
            }
        }

        public static void Invoke<T>(Action<T> action) where T : class
        {
            foreach (var mod in regMods)
            {
                T t = mod as T;
                if (t != null)
                {
                    try
                    {
                        action(t);
                        ElysiumLoader.LOG0.Log("Invoked action " + t);
                        ElysiumLoader.LOG.Log("[EventBus]: Executed action Invoke withot args: type="+t);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[ElysiumKSP]-[EventBus]: Error in mod '"+ mod.GetType().Name + "': " + ex);
                    }
                }
            }
        }

        public static void Invoke<T, U1>(Action<T, U1> action, U1 arg1) where T : class
        {
            foreach (var mod in regMods)
            {
                T t = mod as T;
                if (t != null)
                {
                    try
                    {
                        action(t, arg1);
                        ElysiumLoader.LOG0.Log("Invoked action " + t + "("+arg1+")");
                        ElysiumLoader.LOG.Log("[EventBus]: Executed action Invoke with args: type="+t+", args: " + arg1);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[ElysiumKSP]-[EventBus]: Error in mod '"+ mod.GetType().Name + "': " + ex);
                    }
                }
            }
        }

        public static void Invoke<T, U1, U2>(Action<T, U1, U2> action, U1 arg1, U2 arg2) where T : class
        {
            foreach (var mod in regMods)
            {
                T t = mod as T;
                if (t != null)
                {
                    try
                    {
                        action(t, arg1, arg2);
                        ElysiumLoader.LOG0.Log("Invoked action " + t + "("+arg1+arg2+")");
                        ElysiumLoader.LOG.Log("[EventBus]: Executed action Invoke with args: type="+t+", args: " + arg1+arg2);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[ElysiumKSP]-[EventBus]: Error in mod '"+ mod.GetType().Name + "': " + ex);
                    }
                }
            }
        }

        public static void Invoke<T, U1, U2, U3>(Action<T, U1, U2, U3> action, U1 arg1, U2 arg2, U3 arg3) where T : class
        {
            foreach (var mod in regMods)
            {
                T t = mod as T;
                if (t != null)
                {
                    try
                    {
                        action(t, arg1, arg2, arg3);
                        ElysiumLoader.LOG0.Log("Invoked action " + t + "("+arg1+arg2+arg3+")");
                        ElysiumLoader.LOG.Log("[EventBus]: Executed action Invoke with args: type="+t+", args: " + arg1+arg2+arg3);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[ElysiumKSP]-[EventBus]: Error in mod '"+ mod.GetType().Name + "': " + ex);
                    }
                }
            }
        }

        public static void Invoke<T, U1, U2, U3, U4>(Action<T, U1, U2, U3, U4> action, U1 arg1, U2 arg2, U3 arg3, U4 arg4) where T : class
        {
            foreach (var mod in regMods)
            {
                T t = mod as T;
                if (t != null)
                {
                    try
                    {
                        action(t, arg1, arg2, arg3, arg4);
                        ElysiumLoader.LOG0.Log("Invoked action " + t + "("+arg1+arg2+arg3+arg4+")");
                        ElysiumLoader.LOG.Log("[EventBus]: Executed action Invoke with args: type="+t+", args: " + arg1+arg2+arg3+arg4);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[ElysiumKSP]-[EventBus]: Error in mod '"+ mod.GetType().Name + "': " + ex);
                    }
                }
            }
        }

        public static void Publish(string evName)
        {
            if (OnCustomEvent != null){
                OnCustomEvent(evName);
                ElysiumLoader.LOG.Log("[EventBus]: Published new event: '"+evName+"'.");
                Debug.Log("[ElysiumKSP]-[EventBus]: Published new custom event '" + evName + "'.");
            }
        }

        public static Vessel GetCurrentVessel()
        {
            return FlightGlobals.ActiveVessel;
        }

        public static void SendMessage(string modID, string msg)
        {
            Debug.Log("[ElysiumKSP]-[EventBus]: " + msg);
            EventBus.Invoke<INetworkEvents, string, string>(
                delegate(INetworkEvents mod, string modId, string msg0){
                    mod.OnCustomMessage(modId, msg0);
                }, modID, msg);
        }
    }

    public class Logger
    {
        public static void LogInfo(string modID, string msg)
        {
            ElysiumLoader.LOG.Log("[ModLogger]: Mod '" + modID + "' logged message: " + msg);
            Debug.Log("[ElysiumKSP/ModLogger]-[" + modID + "]: " + msg);
            EventBus.Invoke<INetworkEvents, string, string>(
                delegate(INetworkEvents mod, string modId, string msg0){
                    mod.OnCustomMessage(modId, msg0);
                }, modID, msg);
        }
    }

    public class ElysiumUI
    {
        private static List<ApplicationLauncherButton> btns = new List<ApplicationLauncherButton>();

        public static void RegisterButton(string name, Texture2D icon, Action onClick)
        {
            if (ApplicationLauncher.Instance == null) return;

            ApplicationLauncherButton btn = ApplicationLauncher.Instance.AddModApplication(
                () => onClick(),
                () => onClick(),
                null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS,
                icon
            );

            btn.SetTrue(false);
            btns.Add(btn);
            Debug.Log("[ElysiumKSP]-[UI]: Added button '" + name + "'");
            ElysiumLoader.LOG.Log("[UI] -> Added button '"+name+"'.");
        }
    }

    public class Parts
    {
        public static Part[] FindPartsByName(string name)
        {
            return null;
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ElysiumLoader : MonoBehaviour
    {
        private static bool initial = false, a = true;
        public static ElysiumLoader Instance;
        public static string CACHE_PATH = KSPUtil.ApplicationRootPath + "//GameData/ElysiumMods.ec";
        private static List<IELifecycle> lcMods = new List<IELifecycle>();
        private static List<IEEvent> evMods = new List<IEEvent>();
        private static List<ICelestialEvents> ceMods = new List<ICelestialEvents>();
        private static List<ISaveEvents> geMods = new List<ISaveEvents>();
        private static List<IVessel> veMods = new List<IVessel>();
        public static readonly ElysiumLoggerClient LOG = new ElysiumLoggerClient();
        public static readonly ElysiumLoggerClient.ModificableLogger LOG0 = new ElysiumLoggerClient.ModificableLogger("/invokes-list-eventbus");
        private float lastThr = -1f;

        public void Awake()
        {
            LOG.Log("Starting mod in GameScenes.MainMenu.");
            int mc = 0;
            int nnm = 0;
            if (initial)
            {
                Destroy(this);
                LOG.Log("Mod already created, destroying...");
                LOG.Close();
                return;
            }
            initial = true;
            DontDestroyOnLoad(this);

            LOG.Log("Loader awake, scanning for mods...");
            Debug.Log("[ElysiumKSP]: Loader awake, scanning for mods...");

            foreach (var asm in AssemblyLoader.loadedAssemblies)
            {
                if (asm.assembly == null) continue;
                foreach (var type in asm.assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<KSPModElysiumAttribute>();
                    if (attr != null)
                    {
                        try
                        {
                            var inst = Activator.CreateInstance(type);
                            ModEntry modE = new ModEntry();
                            modE.Name = attr.ModName == null ? "NONAMEMOD_" + nnm.ToString() : attr.ModName;
                            modE.Id = attr.Id;
                            modE.Description = attr.Description;
                            modE.CacheID = attr.ModName == "NONAMEMOD_" + nnm.ToString() ? 404.ToString() : 202.ToString();
                            ModCacheWriter.AddMod(CACHE_PATH, modE);
                            Debug.Log("[ElysiumKSP]: Loaded mod: " + attr.ModName + " (" + attr.Id + ") - " + attr.Description);
                            LOG.Log("[ElysiumKSP]: Loaded mod: " + attr.ModName + " (" + attr.Id + ") - " + attr.Description);
                            var lc = inst as IELifecycle;
                            var e = inst as IEEvent;
                            var ce = inst as ICelestialEvents;
                            var ge = inst as ISaveEvents;
                            var ve = inst as IVessel;
                            if (ve != null) veMods.Add(ve);
                            if (ge != null) geMods.Add(ge);
                            if (ce != null) ceMods.Add(ce);
                            if (lc != null) lcMods.Add(lc);
                            if (e != null) evMods.Add(e);
                            mc++;
                            nnm++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("[ElysiumKSP]: Failed to initialize " + attr.ModName + ": " + ex);
                        }
                    }
                }
            }

            Debug.Log("[ElysiumKSP]: Loaded '" + mc + "' mods in KSP.");
            LOG.Log("[ElysiumKSP]: Loaded '" + mc + "' mods in KSP.");
        }

        public ElysiumAPIConfigButton GetConfig()
        {
            return FindObjectOfType<ElysiumAPIConfigButton>();
        }

        void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && a)
            {
                LOG.Log("Current scene is FLIGHT, loading flight events...");
                GameEvents.onVesselGoOnRails.Add(OnVesselLauched);
                LOG.Log("Loaded event: OnVesselLaunched");
                GameEvents.OnScienceRecieved.Add(OnScienceReceived);
                LOG.Log("Loaded event: OnScienceReceived");
                GameEvents.onPartAttach.Add(OnPartAttach);
                LOG.Log("Loaded event: OnPartAttach");
                GameEvents.onPartDeCouple.Add(OnPartDetach);
                LOG.Log("Loaded event: OnPartDetach");
                GameEvents.onStageActivate.Add(OnStageActivate);
                LOG.Log("Loaded event: OnStageActivate");
                GameEvents.onVesselRecovered.Add(OnVesselRecovered);
                LOG.Log("Loaded event: OnVesselRecovered");
                GameEvents.onVesselWillDestroy.Add(OnVesselDestroyed);
                LOG.Log("Loaded event: OnVesselDestroyed");
                GameEvents.onPartResourceListChange.Add(OnResourceChanged);
                LOG.Log("Loaded event: OnResourceChanged");
                GameEvents.onVesselSOIChanged.Add(OnVesselSOIChanged);
                LOG.Log("Loaded event: OnVesselSOIChanged");
                GameEvents.onVesselLoaded.Add(OnVesselLoaded);
                LOG.Log("Loaded event: OnVesselLoaded");
                GameEvents.onVesselUnloaded.Add(OnVesselUnloaded);
                LOG.Log("Loaded event: OnVesselUnloaded");
                LOG.Log("Finished event loading!");
                a = false;
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                var cfg = FindObjectOfType<ElysiumAPIConfigButton>();
                var v = FlightGlobals.ActiveVessel;
                if (v == null) return;

                foreach(Vessel v0 in FlightGlobals.Vessels)
                {
                    if (v0 == null || !v0.loaded) continue;

                    if (v0.orbit != null)
                    {
                        if (Math.Abs(v0.orbit.ApA - v0.altitude) < cfg.d) foreach (var m in veMods) m.ApogeeReach(v0, v0.orbit.ApA, v0.orbit.timeToAp);
                        if (Math.Abs(v0.orbit.PeA - v0.altitude) < cfg.e) foreach (var m in veMods) m.PerigeeReach(v0, v0.orbit.PeA, v0.orbit.timeToPe);
                    }

                    double fuelA = KSPGameObjects.GetResourceAmount(v0, "LiquidFuel"), energyA = KSPGameObjects.GetResourceAmount(v0, "ElectricCharge");

                    if (fuelA < cfg.f) foreach (var m in veMods) m.FuelLow(v0, fuelA, cfg.f);
                    if (energyA < cfg.g) foreach (var m in veMods) m.ElectricChargeLow(v0, energyA, cfg.g);
                
                    if (v0.altitude <= 0.1 && v.srfSpeed > cfg.h) foreach(var m in veMods) m.Impact(v0, v0.mainBody, v0.srfSpeed, v0.transform.position);
                }
    
                double s = v.srfSpeed;
                if (s > cfg.a) EventBus.Invoke<IVesselPhysicsEvents, Vessel, double>(
                    delegate(IVesselPhysicsEvents mod, Vessel vessel, double speed){
                        mod.OnHighSpeed(vessel, speed);
                    }, v, s);
    
                bool inAtm = v.mainBody.atmosphere;
    
                if (inAtm && !v.packed) EventBus.Invoke<IVesselPhysicsEvents, Vessel, CelestialBody>(
                    delegate(IVesselPhysicsEvents mod, Vessel vessel, CelestialBody body){
                        mod.OnAtmosphereEntry(vessel, body);
                    }, v, v.mainBody);
    
                if (!inAtm && !v.packed) EventBus.Invoke<IVesselPhysicsEvents, Vessel, CelestialBody>(
                    delegate(IVesselPhysicsEvents mod, Vessel vessel, CelestialBody body){
                        mod.OnAtmosphereExit(vessel, body);
                    }, v, v.mainBody);
    
                if (v.orbit.PeA < v.mainBody.Radius + cfg.b) EventBus.Invoke<IVesselPhysicsEvents, Vessel, double>(
                    delegate(IVesselPhysicsEvents mod, Vessel vessel, double t){
                        mod.OnOrbitDecayWarning(vessel, t);
                    }, v, v.orbit.timeToPe);
    
                foreach (var other in FlightGlobals.Vessels)
                {
                    if (other == null) continue;
                    double dist = Vector3.Distance(v.transform.position, other.transform.position);
                    if (dist < cfg.c) EventBus.Invoke<IVesselPhysicsEvents, Vessel, Vessel, double>(
                        delegate(IVesselPhysicsEvents mod, Vessel vessel, Vessel other0, double distance){
                            mod.OnCollisionWarning(vessel, other0, distance);
                        }, v, other, dist);
                }
    
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                {
                    float curr = FlightInputHandler.state.mainThrottle;
                    if (Math.Abs(curr - lastThr) > 0.001f)
                    {
                        foreach (var m in evMods) m.OnThrottleChanged(FlightGlobals.ActiveVessel, curr);
                        lastThr = curr;
                    }
                }
            }
        }

        private void Start()
        {
            Instance = this;
            LOG.Log("Loading EventSystem in GameEvents...");
            Debug.Log("[ElysiumKSP]: Loading non-vessel events...");
            GameEvents.onLevelWasLoadedGUIReady.Add(OnSceneLoaded);
            LOG.Log("Loaded event: OnSceneLoaded");
            GameEvents.onGameStateSaved.Add(OnGS);
            LOG.Log("Loaded event: OnGameSave");
            GameEvents.onGameStateCreated.Add(OnGC);
            LOG.Log("Loaded event: OnGameLoaded");
            LOG.Log("Non-vessel events loaded! Waiting to other events loading...");
        }

        void OnDestroy()
        {
            LOG.LogH("KSP Closing... end logging!");
            LOG.Close();
            LOG0.LogH("EventBus end working.");
            LOG0.Close();
        }

        private void OnGC(Game g) { foreach (var m in geMods) m.OnGameLoaded(g); }
        private void OnGS(Game g) { foreach (var m in geMods) m.OnGameSaved(g); }
        private void OnVesselLoaded(Vessel v) { foreach (var m in geMods) m.OnVesselLoaded(v); }
        private void OnVesselUnloaded(Vessel v) { foreach (var m in geMods) m.OnVesselUnloaded(v); }

        private void OnVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> e) { foreach (var m in ceMods) m.OnSOIChanged(e.host, e.from, e.to); }

        private void OnResourceChanged(Part part)
        {
            foreach (PartResource res in part.Resources)
            {
                double oldAmount = res.amount;
                double newAmount = res.amount;
                double delta = res.amount - res.maxAmount;
                EventBus.Invoke<INetworkEvents, PartResource, double, double, Vessel>(
                    delegate(INetworkEvents mod, PartResource r, double oldA, double newA, Vessel v) {
                        mod.OnResourceSync(r, oldA, newA, v);
                    }, res, oldAmount, newAmount, FlightGlobals.ActiveVessel);
                if (delta < 0) foreach (var m in evMods) m.OnResourceConsumed(part, res.resourceName, Math.Abs(delta));
                else if (delta > 0) foreach (var m in evMods) m.OnResourceProduced(part, res.resourceName, delta);
                newAmount = res.amount;
            }
        }

        private void OnVesselDestroyed(Vessel vessel)
        {
            foreach (var m in evMods) m.OnVesselDestroyed(vessel);
        }

        private void OnVesselRecovered(ProtoVessel vessel, bool quick)
        {
            foreach (var m in evMods) m.OnVesselRecovered(vessel.vesselRef, quick);
        }

        private void OnStageActivate(int stageID)
        {
            foreach (var m in evMods) m.OnStageActivate(FlightGlobals.ActiveVessel, stageID);
        }

        private void OnScienceReceived(float data, ScienceSubject subject, ProtoVessel vessel, bool lab)
        {
            foreach (var m in evMods) m.OnScienceReceived(data, subject, vessel.vesselRef, lab);
        }

        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        {
            foreach (var m in evMods) m.OnPartAttached(data.host, data.target, data.host.vessel);
        }

        private void OnPartDetach(Part p)
        {
            foreach (var m in evMods) m.OnPartDetached(p, FlightGlobals.ActiveVessel);
        }

        private void OnSceneLoaded(GameScenes scene)
        {
            foreach (var m in evMods) m.OnSceneChanged(scene);

            switch (scene)
            {
                case GameScenes.MAINMENU:
                    foreach (var m in lcMods) m.OnMainMenu();
                    break;
                case GameScenes.SPACECENTER:
                    foreach (var m in lcMods) m.OnSpaceCenter();
                    break;
                case GameScenes.EDITOR:
                    foreach (var m in lcMods) m.OnEditor();
                    break;
                case GameScenes.FLIGHT:
                    foreach (var m in lcMods) m.OnFlight();
                    break;
            }
        }

        private void OnVesselLauched(Vessel v)
        {
            foreach (var m in evMods) m.OnVesselLauched(v);
        }
    }

    #endregion
}