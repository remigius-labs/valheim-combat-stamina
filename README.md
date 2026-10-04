# Combat Stamina (Valheim, BepInEx)

Free stamina outside combat; normal stamina in combat.

**Combat** = you damaged a creature, or a creature damaged you (blocked hits count), within the
last `CombatSeconds` (10). A living boss within `BossRadius` (80 m) always counts as combat.
Damage with no attacker (falling, fire, poison ticks) doesn't count. Client-side only.

Config: `BepInEx/config/remi.valheim.combatstamina.cfg` (Enabled, CombatSeconds, FreeSwimming, BossRadius, ShowMessages).

How: Harmony prefix on `Player.UseStamina` (every stamina cost goes through it), postfix on
`Player.HaveStamina`, and combat detection on `Character.Damage` (you hit something; runs on the
attacker's client) and `Character.RPC_Damage` (you got hit; runs on your client, as owner of your player).

Known: the first swing of a fight is free (it's paid before the hit lands).

Build: `./build.sh` (needs .NET 8 SDK in `~/.local/opt/dotnet`, references the game's DLLs), `./build.sh --install`.
Built against Valheim 1.0.16, BepInEx 5.4.23.
