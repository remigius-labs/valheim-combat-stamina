using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CombatStamina
{
    // Free stamina outside combat. Combat = you damaged a creature, or a creature damaged you,
    // within the last CombatSeconds (or a boss is within BossRadius). Client-side only.
    [BepInPlugin(Guid, "Combat Stamina", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "remi.valheim.combatstamina";
        public const string Version = "1.0.0";

        static ConfigEntry<bool> modEnabled, freeSwimming, showMessages;
        static ConfigEntry<float> combatSeconds, bossRadius;

        static float lastCombatTime = -9999f;
        static float nextBossCheck;
        static bool bossNear;
        bool wasInCombat;

        void Awake()
        {
            modEnabled = Config.Bind("General", "Enabled", true, "Master switch.");
            combatSeconds = Config.Bind("General", "CombatSeconds", 10f,
                "Seconds after the last hit (dealt or taken) before stamina is free again.");
            freeSwimming = Config.Bind("General", "FreeSwimming", true,
                "Free swimming out of combat. Note: you can't drown while swimming is free.");
            bossRadius = Config.Bind("General", "BossRadius", 80f,
                "A living boss within this many metres always counts as combat. 0 = off.");
            showMessages = Config.Bind("General", "ShowMessages", true,
                "Top-left message when combat starts and ends.");
            new Harmony(Guid).PatchAll();
        }

        public static void MarkCombat() => lastCombatTime = Time.time;

        static bool InCombat(Player p)
        {
            if (Time.time - lastCombatTime < combatSeconds.Value) return true;
            if (Time.time >= nextBossCheck)
            {
                nextBossCheck = Time.time + 1f;
                bossNear = BossNearby(p);
            }
            return bossNear;
        }

        static bool BossNearby(Player p)
        {
            float r = bossRadius.Value;
            if (r <= 0f) return false;
            Vector3 pos = p.transform.position;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c != null && c.IsBoss() && !c.IsDead() &&
                    (c.transform.position - pos).sqrMagnitude < r * r)
                    return true;
            }
            return false;
        }

        public static bool FreeStamina(Character c)
        {
            if (!modEnabled.Value) return false;
            Player p = Player.m_localPlayer;
            if (p == null || !ReferenceEquals(c, p)) return false;
            if (!freeSwimming.Value && p.IsSwimming()) return false;
            return !InCombat(p);
        }

        void Update()
        {
            Player p = Player.m_localPlayer;
            if (p == null || !modEnabled.Value) return;
            bool inCombat = InCombat(p);
            if (inCombat == wasInCombat) return;
            wasInCombat = inCombat;
            if (showMessages.Value && MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft,
                    inCombat ? "In combat: stamina costs on" : "Out of combat: free stamina");
        }
    }

    // Every stamina cost (sprint, jump, swim, attacks, tools, building, dodge, block) goes through here.
    [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
    static class UseStaminaPatch
    {
        static bool Prefix(Player __instance) => !Plugin.FreeStamina(__instance);
    }

    // So an empty bar right after a fight doesn't stop you sprinting/swinging once you're safe.
    [HarmonyPatch(typeof(Player), nameof(Player.HaveStamina))]
    static class HaveStaminaPatch
    {
        static void Postfix(Player __instance, ref bool __result)
        {
            if (!__result && Plugin.FreeStamina(__instance)) __result = true;
        }
    }

    // You hit a creature. Runs on the attacker's client, before the hit is routed to the target's owner.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    static class DealDamagePatch
    {
        static void Prefix(Character __instance, HitData hit)
        {
            Player p = Player.m_localPlayer;
            if (p == null || hit == null || ReferenceEquals(__instance, p)) return;
            if (ReferenceEquals(hit.GetAttacker(), p)) Plugin.MarkCombat();
        }
    }

    // A creature hit you (blocked hits included). Runs on your client because you own your player.
    // Damage without an attacker (falling, fire, poison ticks) doesn't count.
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    static class TakeDamagePatch
    {
        static void Prefix(Character __instance, HitData hit)
        {
            Player p = Player.m_localPlayer;
            if (p == null || hit == null || !ReferenceEquals(__instance, p)) return;
            Character attacker = hit.GetAttacker();
            if (attacker != null && !ReferenceEquals(attacker, p)) Plugin.MarkCombat();
        }
    }
}
