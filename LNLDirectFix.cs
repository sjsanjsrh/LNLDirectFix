using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using FrooxEngine;
using Elements.Core;

namespace LNLDirectFix;

public class LNLDirectFix : ResoniteMod
{
    internal const string VERSION_CONSTANT = "1.4.0";
    public override string Name    => "LNLDirectFix";
    public override string Author  => "Sinduy";
    public override string Version => VERSION_CONSTANT;
    public override string Link    => "https://github.com/sjsanjsrh/lnl-direct-fix";

    public override void OnEngineInit()
    {
        Harmony harmony = new Harmony("com.sinduy.LNLDirectFix");
        harmony.PatchAll();
    }
}


[HarmonyPatch]
class LNL_Listener_LocalUris_Patch
{
    static MethodBase TargetMethod()
    {
        return typeof(LNL_Listener).GetProperty(
            "LocalUris",
            BindingFlags.Public | BindingFlags.Instance
        )!.GetGetMethod();
    }

    static bool Prefix(LNL_Listener __instance, out IEnumerable<Uri> __result)
    {
        __result = GetLocalUris(__instance);
        return false;
    }

    static IEnumerable<Uri> GetLocalUris(LNL_Listener listener)
    {
        ushort? port = listener.Port;
        if (!port.HasValue || port.Value == 0)
            yield break;

        if (!listener.BoundToSpecificIP)
        {
            // Any listener — emit lnl:// for all local IPs using the actual bound port
            var ips = NetUtils.FilteredLocalIPs;
            int ipCount = 0;
            if (ips != null)
            {
                foreach (var ip in ips)
                    ipCount++;
            }
            LNLDirectFix.Debug($"Any→IP listener, port {port}, local IPs count: {ipCount}");
            
            if (ips != null && ipCount > 0)
            {
                foreach (IPAddress ip in ips)
                {
                    var uri = new Uri($"lnl://{ip}:{port.Value}");
                    LNLDirectFix.Debug($"LocalUri (Any→IP): {uri}");
                    yield return uri;
                }
            }
            else if (ips == null)
            {
                LNLDirectFix.Error("FilteredLocalIPs is NULL!");
            }
            else
            {
                LNLDirectFix.Warn("FilteredLocalIPs is empty!");
            }
        }
        else
        {
            LNLDirectFix.Debug($"Per-IP listener (BoundToSpecificIP=true), suppressing URI");
        }
        // per-IP listener → emit nothing (port is wrong due to Linux SO_REUSEADDR rebind failure)
    }
}

[HarmonyPatch(typeof(World), "UpdateSessionURLs")]
class World_UpdateSessionURLs_Patch
{
    static void Postfix(World __instance)
    {
        // Directly inject lnl:// URIs into Configuration.SessionURLs
        // This bypasses potential announceLocalIPs flag issues
        
        if (__instance?.Session?.Connections?.Listener == null)
            return;
        if (__instance.Configuration?.SessionURLs == null)
            return;

        bool added = false;
        
        foreach (IListener listener in __instance.Session.Connections.Listener.Listeners)
        {
            foreach (Uri localUri in listener.LocalUris)
            {
                if (localUri == null || localUri.Scheme != "lnl")
                    continue;

                // Check if already exists
                bool exists = false;
                foreach (Uri existingUrl in __instance.Configuration.SessionURLs)
                {
                    if (existingUrl?.ToString() == localUri.ToString())
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    LNLDirectFix.Debug($"Directly adding to SessionURLs: {localUri}");
                    __instance.Configuration.SessionURLs.Add(localUri);
                    added = true;
                }
            }
        }

        if (added)
        {
            LNLDirectFix.Debug("Added new local URIs, session should re-broadcast");
        }
    }
}
