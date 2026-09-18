using BepInEx;
using R2API.Utils;
using RoR2;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace RegigigasMod
{
    [BepInDependency("com.Moffein.RiskyArtifacts", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DirectorAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ItemAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.Skins.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.SoundAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class RegigigasPlugin : BaseUnityPlugin
    {
        public const string MODUID = "com.rob.RegigigasMod";
        public const string MODNAME = "RegigigasMod";
        public const string MODVERSION = "1.6.1";

        public const string developerPrefix = "ROB";

        public static RegigigasPlugin instance;

        public static bool riskyArtifactsInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Moffein.RiskyArtifacts");
        public static bool rooInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        //public static bool peakInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.JestAnAnimator.LoreFriendRegigigas");

        private void Awake()
        {
            instance = this;

            Modules.Config.myConfig = Config;

            Log.Init(Logger);
            Modules.RegiAssets.PopulateAssets();
            Modules.Config.ReadConfig();
            Modules.CameraParams.InitializeParams();
            Modules.States.RegisterStates();
            Modules.Buffs.RegisterBuffs();
            Modules.Projectiles.RegisterProjectiles();
            Modules.Tokens.AddTokens();
            Modules.ItemDisplays.PopulateDisplays();
            Modules.NetMessages.RegisterNetworkMessages();

            new Modules.Enemies.Regigigas().CreateCharacter();
            new Modules.ContentPacks().Initialize();

            RoR2.ContentManagement.ContentManager.onContentPacksAssigned += LateSetup;
        }

        private static void LateSetup(HG.ReadOnlyArray<RoR2.ContentManagement.ReadOnlyContentPack> obj)
        {
            Modules.Enemies.Regigigas.SetItemDisplays();


            // hate that i havze to do this
            Modules.Buffs.armorBuff.iconSprite = RoR2Content.Buffs.ArmorBoost.iconSprite;
            //Modules.Buffs.slowStartBuff.iconSprite = RoR2Content.Buffs.Slow50.iconSprite; wahoo
        }
    }
}