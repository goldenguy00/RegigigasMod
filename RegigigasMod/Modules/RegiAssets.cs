using System.Reflection;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.IO;
using RoR2.Audio;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering.PostProcessing;
using ThreeEyedGames;
using RoR2.UI;
using RoR2.Projectile;

namespace RegigigasMod.Modules
{
    internal static class RegiAssets
    {
        internal static AssetBundle mainAssetBundle;
        internal static AssetBundle secondaryAssetBundle;

        internal static Shader hotpoo = Resources.Load<Shader>("Shaders/Deferred/HGStandard");
        internal static Material commandoMat;

        internal static GameObject drainPunchChargeEffect;
        internal static GameObject rockHitEffect;

        internal static GameObject slamImpactEffect;
        internal static GameObject punchImpactEffect;

        internal static NetworkSoundEventDef punchSoundDef;

        internal static List<EffectDef> effectDefs = new List<EffectDef>();
        internal static List<NetworkSoundEventDef> networkSoundEventDefs = new List<NetworkSoundEventDef>();
        internal static List<UnlockableDef> unlockableDefs = new List<UnlockableDef>();

        internal static GameObject slowStartEffect;
        internal static GameObject slowStartReleasedEffect;

        internal static GameObject ancientPowerCrosshairPrefab;

        internal static GameObject gigaImpactRushEffect;
        internal static GameObject slowStartPickupEffect;

        internal static void PopulateAssets()
        {
            if (mainAssetBundle == null)
            {
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RegigigasMod.regigigas"))
                {
                    mainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                }
            }

            // lost the original unityproject so this is necessary
            if (secondaryAssetBundle == null)
            {
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RegigigasMod.regigigas2"))
                {
                    secondaryAssetBundle = AssetBundle.LoadFromStream(assetStream);
                }
            }

            using (Stream manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("RegigigasMod.RegigigasBank.bnk"))
            {
                byte[] array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
                SoundAPI.SoundBanks.Add(array);
            }

            drainPunchChargeEffect = LoadEffect("DrainPunchChargeEffect", true);

            punchSoundDef = CreateNetworkSoundEventDef("RegigigasPunchImpact");

            rockHitEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bell/OmniExplosionVFXBellDeath.prefab").WaitForCompletion().InstantiateClone("RegigigasRockImpact", true);
            AddNewEffectDef(rockHitEffect, "sfx_regigigas_rock_hit");

            string prefix = RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_";
            slowStartEffect = CreateTextPopupEffect("RegigigasSlowStartEffect", prefix + "SLOW_START", 2f);
            slowStartReleasedEffect = CreateTextPopupEffect("RegigigasSlowStartReleasedEffect", prefix + "SLOW_START_RELEASED", 2.5f);

            slamImpactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/BrotherSlamImpact.prefab").WaitForCompletion().InstantiateClone("RegigigasSlamImpact", true);

            slamImpactEffect.transform.Find("Spikes, Small").gameObject.SetActive(false);

            slamImpactEffect.transform.Find("PP").GetComponent<PostProcessVolume>().sharedProfile = Addressables.LoadAssetAsync<PostProcessProfile>("RoR2/Base/title/ppLocalMagmaWorm.asset").WaitForCompletion();

            slamImpactEffect.transform.Find("Point light").GetComponent<Light>().color = Color.yellow;

            slamImpactEffect.transform.Find("Flash Lines, Fire").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matFirePillarParticle.mat").WaitForCompletion();

            slamImpactEffect.transform.Find("Fire").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matFirePillarParticle.mat").WaitForCompletion();

            slamImpactEffect.transform.Find("Physics").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/MagmaWorm/matFracturedGround.mat").WaitForCompletion();

            slamImpactEffect.transform.Find("Decal").GetComponent<Decal>().Material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Beetle/matBeetleGuardSlamDecal.mat").WaitForCompletion();

            AddNewEffectDef(slamImpactEffect, "");


            punchImpactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Loader/OmniImpactVFXLoader.prefab").WaitForCompletion().InstantiateClone("RegigigasPunchImpact", true);
            punchImpactEffect.GetComponent<EffectComponent>().applyScale = true;

            punchImpactEffect.transform.Find("Scaled Hitspark 1 (Random Color)").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matOmniHitspark1.mat").WaitForCompletion();
            punchImpactEffect.transform.Find("Scaled Hitspark 3 (Random Color)").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matOmniHitspark3.mat").WaitForCompletion();
            punchImpactEffect.transform.Find("ScaledSmokeRing, Mesh").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/LifestealOnHit/matLifeStealOnHitAuraTrails.mat").WaitForCompletion();

            punchImpactEffect.transform.Find("Impact Shockwave").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matOmniRing2.mat").WaitForCompletion();

            AddNewEffectDef(punchImpactEffect, "");


            ancientPowerCrosshairPrefab = PrefabAPI.InstantiateClone(LoadCrosshair("ToolbotGrenadeLauncher"), "AncientPowerCrosshair", false);
            CrosshairController crosshair = ancientPowerCrosshairPrefab.GetComponent<CrosshairController>();
            crosshair.skillStockSpriteDisplays = new CrosshairController.SkillStockSpriteDisplay[0];
            ancientPowerCrosshairPrefab.transform.Find("StockCountHolder").gameObject.SetActive(false);


            gigaImpactRushEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarWisp/LunarWispTrackingBombGhost.prefab").WaitForCompletion().InstantiateClone("RegigigasGigaImpactRushEffect", false);
            RegigigasPlugin.Destroy(gigaImpactRushEffect.GetComponent<ProjectileGhostController>());

            gigaImpactRushEffect.transform.Find("Point Light").GetComponent<Light>().color = Color.white;
            gigaImpactRushEffect.transform.Find("Glow").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matFirePillarParticle.mat").WaitForCompletion();
            gigaImpactRushEffect.transform.Find("BombOrb").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Grandparent/matGrandparentTeleportOutBoom.mat").WaitForCompletion();
            gigaImpactRushEffect.transform.Find("BombOrb").Find("Sparks").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Captain/matCaptainAirstrikeTrail.mat").WaitForCompletion();

            slowStartPickupEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Infusion/InfusionOrbFlash.prefab").WaitForCompletion().InstantiateClone("RegigigasGigaImpactRushEffect", true);

            slowStartPickupEffect.transform.Find("Blood").localScale = Vector3.one * 7f;
            slowStartPickupEffect.transform.Find("Blood").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Firework/matFireworkSparkle.mat").WaitForCompletion();

            AddNewEffectDef(slowStartPickupEffect, "sfx_regigigas_slowstart_pickup");
        }

        internal static NetworkSoundEventDef CreateNetworkSoundEventDef(string eventName)
        {
            NetworkSoundEventDef networkSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            networkSoundEventDef.akId = AkSoundEngine.GetIDFromString(eventName);
            networkSoundEventDef.eventName = eventName;

            networkSoundEventDefs.Add(networkSoundEventDef);

            return networkSoundEventDef;
        }

        internal static GameObject CreateTextPopupEffect(string prefabName, string token, float scale = 1f, string soundName = "")
        {
            GameObject i = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BearProc").InstantiateClone(prefabName, true);

            i.GetComponent<EffectComponent>().soundName = soundName;
            if (!i.GetComponent<NetworkIdentity>()) i.AddComponent<NetworkIdentity>();

            i.GetComponentInChildren<RoR2.UI.LanguageTextMeshController>().token = token;

            i.GetComponentInChildren<ObjectScaleCurve>().timeMax *= 3f;

            i.transform.localScale = Vector3.one * 3f;

            RegiAssets.AddNewEffectDef(i);

            return i;
        }

        internal static void ConvertAllRenderersToHopooShader(GameObject objectToConvert)
        {
            foreach (Renderer i in objectToConvert.GetComponentsInChildren<Renderer>())
            {
                if (i)
                {
                    if (i.material)
                    {
                        i.material.shader = hotpoo;
                    }
                }
            }
        }

        internal static CharacterModel.RendererInfo[] SetupRendererInfos(GameObject obj)
        {
            MeshRenderer[] meshes = obj.GetComponentsInChildren<MeshRenderer>();
            CharacterModel.RendererInfo[] rendererInfos = new CharacterModel.RendererInfo[meshes.Length];

            for (int i = 0; i < meshes.Length; i++)
            {
                rendererInfos[i] = new CharacterModel.RendererInfo
                {
                    defaultMaterial = meshes[i].material,
                    renderer = meshes[i],
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false
                };
            }

            return rendererInfos;
        }

        public static GameObject LoadSurvivorModel(string modelName) {
            GameObject model = mainAssetBundle.LoadAsset<GameObject>(modelName);
            if (model == null) {
                Log.Error("Trying to load a null model- check to see if the name in your code matches the name of the object in Unity");
                return null;
            }

            return PrefabAPI.InstantiateClone(model, model.name, false);
        }

        internal static Texture LoadCharacterIcon(string characterName)
        {
            // whoops, needed a fallback
            if (mainAssetBundle.LoadAsset<Texture>("tex" + characterName + "Icon") == null) return secondaryAssetBundle.LoadAsset<Texture>("tex" + characterName + "Icon");
            return mainAssetBundle.LoadAsset<Texture>("tex" + characterName + "Icon");
        }

        internal static GameObject LoadCrosshair(string crosshairName)
        {
            return Resources.Load<GameObject>("Prefabs/Crosshair/" + crosshairName + "Crosshair");
        }

        private static GameObject LoadEffect(string resourceName)
        {
            return LoadEffect(resourceName, "", false);
        }

        private static GameObject LoadEffect(string resourceName, string soundName)
        {
            return LoadEffect(resourceName, soundName, false);
        }

        private static GameObject LoadEffect(string resourceName, bool parentToTransform)
        {
            return LoadEffect(resourceName, "", parentToTransform);
        }

        private static GameObject LoadEffect(string resourceName, string soundName, bool parentToTransform)
        {
            GameObject newEffect = mainAssetBundle.LoadAsset<GameObject>(resourceName);

            newEffect.AddComponent<DestroyOnTimer>().duration = 12;
            newEffect.AddComponent<NetworkIdentity>();
            newEffect.AddComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
            var effect = newEffect.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = parentToTransform;
            effect.positionAtReferencedTransform = true;
            effect.soundName = soundName;

            AddNewEffectDef(newEffect, soundName);

            return newEffect;
        }

        internal static void AddNewEffectDef(GameObject effectPrefab)
        {
            AddNewEffectDef(effectPrefab, "");
        }

        internal static void AddNewEffectDef(GameObject effectPrefab, string soundName)
        {
            EffectDef newEffectDef = new EffectDef();
            newEffectDef.prefab = effectPrefab;
            newEffectDef.prefabEffectComponent = effectPrefab.GetComponent<EffectComponent>();
            newEffectDef.prefabName = effectPrefab.name;
            newEffectDef.prefabVfxAttributes = effectPrefab.GetComponent<VFXAttributes>();
            newEffectDef.spawnSoundEventName = soundName;

            effectDefs.Add(newEffectDef);
        }

        //ugh
        public static Material CreateMaterial2(string materialName, float emission, Color emissionColor, float normalStrength)
        {
            if (!commandoMat) commandoMat = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponentInChildren<CharacterModel>().baseRendererInfos[0].defaultMaterial;

            Material mat = UnityEngine.Object.Instantiate<Material>(commandoMat);
            Material tempMat = RegiAssets.secondaryAssetBundle.LoadAsset<Material>(materialName);

            if (!tempMat) return commandoMat;

            mat.name = materialName;
            mat.SetColor("_Color", tempMat.GetColor("_Color"));
            mat.SetTexture("_MainTex", tempMat.GetTexture("_MainTex"));
            mat.SetColor("_EmColor", emissionColor);
            mat.SetFloat("_EmPower", emission);
            mat.SetTexture("_EmTex", tempMat.GetTexture("_EmissionMap"));
            mat.SetFloat("_NormalStrength", normalStrength);

            return mat;
        }

        public static Material CreateMaterial(string materialName, float emission, Color emissionColor, float normalStrength)
        {
            if (!commandoMat) commandoMat = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponentInChildren<CharacterModel>().baseRendererInfos[0].defaultMaterial;

            Material mat = UnityEngine.Object.Instantiate<Material>(commandoMat);
            Material tempMat = RegiAssets.mainAssetBundle.LoadAsset<Material>(materialName);

            if (!tempMat) return commandoMat;

            mat.name = materialName;
            mat.SetColor("_Color", tempMat.GetColor("_Color"));
            mat.SetTexture("_MainTex", tempMat.GetTexture("_MainTex"));
            mat.SetColor("_EmColor", emissionColor);
            mat.SetFloat("_EmPower", emission);
            mat.SetTexture("_EmTex", tempMat.GetTexture("_EmissionMap"));
            mat.SetFloat("_NormalStrength", normalStrength);

            return mat;
        }

        public static Material CreateMaterial(string materialName)
        {
            return RegiAssets.CreateMaterial(materialName, 0f);
        }

        public static Material CreateMaterial(string materialName, float emission)
        {
            return RegiAssets.CreateMaterial(materialName, emission, Color.black);
        }

        public static Material CreateMaterial(string materialName, float emission, Color emissionColor)
        {
            return RegiAssets.CreateMaterial(materialName, emission, emissionColor, 0f);
        }
    }
}