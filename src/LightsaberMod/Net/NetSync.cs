using HarmonyLib;
using LightsaberMod.Forms;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Net
{
    /// <summary>
    /// Tells other clients about effects that only exist locally on the caster's machine:
    /// thrown sabers and Force lightning. Damage never travels this way; it already reaches
    /// everyone through Valheim's own hit RPCs, so these messages are purely cosmetic.
    /// </summary>
    /// <remarks>
    /// The chosen form is published on the player's ZDO, so any client can tell which form a
    /// remote player is fighting in (used by <see cref="Animation.FormAnimations"/>).
    /// </remarks>
    internal static class NetSync
    {
        private const string ThrowRpc = "LightsaberMod_SaberThrow";
        private const string LightningRpc = "LightsaberMod_Lightning";
        private const string FormKey = "LightsaberMod_Form";
        private const string JarKaiKey = "LightsaberMod_JarKai";

        // ZRoutedRpc.Everybody; the routed RPC also loops back to the sender, which is ignored below.
        private const long Everybody = 0L;

        [HarmonyPatch(typeof(Game), "Start")]
        private static class RegisterOnGameStart
        {
            private static void Postfix()
            {
                if (ZRoutedRpc.instance == null) return;
                ZRoutedRpc.instance.Register<ZPackage>(ThrowRpc, OnThrow);
                ZRoutedRpc.instance.Register<ZPackage>(LightningRpc, OnLightning);
            }
        }

        // ----- Saber throw ----------------------------------------------------------------

        public static void BroadcastThrow(Player thrower, string prefabName, Vector3 origin, Vector3 aim, float range)
        {
            if (ZRoutedRpc.instance == null) return;
            var pkg = new ZPackage();
            pkg.Write(thrower.GetPlayerID());
            pkg.Write(prefabName);
            pkg.Write(origin);
            pkg.Write(aim);
            pkg.Write(range);
            ZRoutedRpc.instance.InvokeRoutedRPC(Everybody, ThrowRpc, pkg);
        }

        private static void OnThrow(long sender, ZPackage pkg)
        {
            Player thrower = FindRemote(pkg.ReadLong());
            string prefab = pkg.ReadString();
            Vector3 origin = pkg.ReadVector3();
            Vector3 aim = pkg.ReadVector3();
            float range = pkg.ReadSingle();
            if (thrower != null)
                SaberThrow.SpawnRemote(thrower, prefab, origin, aim, range);
        }

        // ----- Force lightning ------------------------------------------------------------

        public static void BroadcastLightning(Player caster, bool on)
        {
            if (ZRoutedRpc.instance == null || caster == null) return;
            var pkg = new ZPackage();
            pkg.Write(caster.GetPlayerID());
            pkg.Write(on);
            ZRoutedRpc.instance.InvokeRoutedRPC(Everybody, LightningRpc, pkg);
        }

        private static void OnLightning(long sender, ZPackage pkg)
        {
            Player caster = FindRemote(pkg.ReadLong());
            bool on = pkg.ReadBool();
            if (caster == null) return;

            LightningFx fx = LightningFx.For(caster);
            if (on) fx.Begin(computeTargets: true);
            else fx.End();
        }

        // ----- Form -----------------------------------------------------------------------

        /// <summary>Publishes the local player's form for other clients. Only the owner can write its ZDO.</summary>
        public static void PublishForm(Player player, int form)
        {
            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            if (zdo != null) Compat.Call(zdo, "Set", FormKey, form);
        }

        /// <summary>A player's form as last published, or -1 if unknown.</summary>
        public static int ReadForm(Player player)
        {
            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            return zdo != null && Compat.Call(zdo, "GetInt", FormKey, -1) is int form ? form : -1;
        }

        // ----- Jar'Kai --------------------------------------------------------------------

        /// <summary>Publishes the prefab name of the local player's off-hand saber, or "" for none.</summary>
        public static void PublishJarKai(Player player, string prefabName)
        {
            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            if (zdo != null) Compat.Call(zdo, "Set", JarKaiKey, prefabName);
        }

        public static string ReadJarKai(Player player)
        {
            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            return zdo != null && Compat.Call(zdo, "GetString", JarKaiKey, "") is string name ? name : "";
        }

        // ----- Helpers --------------------------------------------------------------------

        /// <summary>The player with this ID, unless it's us (our own broadcasts loop back).</summary>
        private static Player FindRemote(long playerId)
        {
            foreach (Player p in Player.GetAllPlayers())
                if (p != null && p.GetPlayerID() == playerId)
                    return p == Player.m_localPlayer ? null : p;
            return null;
        }
    }
}
