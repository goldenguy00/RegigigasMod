using BepInEx.Configuration;
using RegigigasMod.SkillStates.Regigigas;
using R2API;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using RoR2.CharacterAI;
using RoR2.Navigation;
using RegigigasMod.Modules.Misc;
using RoR2.Orbs;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using System;
using RegigigasMod.Modules.Achievements;

namespace RegigigasMod.Modules.Enemies
{
    internal class Regigigas
    {
        internal static Regigigas instance;

        internal static GameObject characterPrefab;
        internal static GameObject survivorPrefab;
        internal static GameObject displayPrefab;

        internal static GameObject bossMaster;
        internal static GameObject umbraMaster;

        internal static ConfigEntry<bool> characterEnabled;
        internal static ConfigEntry<bool> enemyEnabled;
        internal static ConfigEntry<int> minimumStageCount;
        internal static ConfigEntry<int> spawnCost;

        internal static ConfigEntry<bool> addToOrigination;

        public const string bodyName = "RegigigasBody";

        public static int bodyRendererIndex; // use this to store the rendererinfo index containing our character's body
                                             // keep it last in the rendererinfos because teleporter particles for some reason require this. hopoo pls

        // item display stuffs
        internal static ItemDisplayRuleSet itemDisplayRuleSet;
        internal static List<ItemDisplayRuleSet.KeyAssetRuleGroup> itemDisplayRules;

        // orb
        internal static GameObject slowStartOrb;

        // skilldefs
        public static SkillDef lunarPunchSkillDef;
        public static SkillDef lunarStompSkillDef;
        public static SkillDef lunarBounceSkillDef;

        internal static UnlockableDef masteryUnlockableDef;

        internal static bool lateInit = false;

        internal void CreateCharacter()
        {
            instance = this;

            enemyEnabled = Modules.Config.EnemyEnableConfig("Regigigas");
            characterEnabled = Modules.Config.CharacterEnableConfig("Regigigas (Playable)");

            addToOrigination = Modules.Config.RiskyArtifactsOriginConfig("Regigigas");

            if (enemyEnabled.Value || characterEnabled.Value)
            {
                CreateOrb();

                if (characterEnabled.Value)
                    masteryUnlockableDef = CreateAndAddUnlockableDef(RegigigasMasteryAchievement.IDENTIFIER, RegigigasMasteryAchievement.UNLOCKABLE_IDENTIFIER, RegigigasMasteryAchievement.Sprite);

                characterPrefab = CreateBodyPrefab(false);
                survivorPrefab = CreateBodyPrefab(true);

                CharacterBody survivorBody = survivorPrefab.GetComponent<CharacterBody>();
                survivorBody.baseMaxHealth = 480f;
                survivorBody.levelMaxHealth = 144f;

                displayPrefab = Modules.Prefabs.CreateDisplayPrefab("RegigigasDisplay", characterPrefab);

                if (characterEnabled.Value) Modules.Prefabs.RegisterNewSurvivor(survivorPrefab, displayPrefab, "REGIGIGAS");

                bossMaster = CreateMaster(characterPrefab, "RegigigasMaster");
                umbraMaster = CreateMaster(survivorPrefab, "RegigigasMonsterMaster");

                if (enemyEnabled.Value) CreateSpawnCard();
            }

            Hook();
        }

        internal static UnlockableDef CreateAndAddUnlockableDef(string identifier, string unlockableIdentifier, Sprite achievementIcon)
        {
            var unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
            unlockableDef.cachedName = unlockableIdentifier.ToUpperInvariant();
            unlockableDef.nameToken = "ACHIEVEMENT_" + identifier.ToUpperInvariant() + "_NAME";
            unlockableDef.achievementIcon = achievementIcon;

            RegiAssets.unlockableDefs.Add(unlockableDef);

            return unlockableDef;
        }

        private static void CreateOrb()
        {
            slowStartOrb = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Effects/OrbEffects/InfusionOrbEffect"), "SlowStartOrbEffect", true);
            if (!slowStartOrb.GetComponent<NetworkIdentity>()) slowStartOrb.AddComponent<NetworkIdentity>();

            //Material titanPredictionEffect = Resources.Load<GameObject>("Prefabs/Projectiles/TitanPreFistProjectile").transform.Find("TeamAreaIndicator, GroundOnly").GetComponent<TeamAreaIndicator>().teamMaterialPairs[0].sharedMaterial;
            //Material globMat = new EntityStates.TitanMonster.FireMegaLaser().laserPrefab.transform.Find("End").Find("EndEffect").Find("Particles").Find("Glob").GetComponent<ParticleSystemRenderer>().material;

            TrailRenderer trail = slowStartOrb.transform.Find("TrailParent").Find("Trail").GetComponent<TrailRenderer>();
            trail.widthMultiplier = 1f;
            trail.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Grandparent/matGrandparentTeleportOutBoom.mat").WaitForCompletion();

            slowStartOrb.transform.Find("VFX").Find("Core").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Loader/matOmniRing2Loader.mat").WaitForCompletion();
            slowStartOrb.transform.Find("VFX").localScale = Vector3.one * 2f;

            slowStartOrb.transform.Find("VFX").Find("Core").localScale = Vector3.one * 4.5f;

            slowStartOrb.transform.Find("VFX").Find("PulseGlow").GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Grandparent/matGrandParentSunGlow.mat").WaitForCompletion();

            slowStartOrb.GetComponent<OrbEffect>().endEffect = Modules.RegiAssets.slowStartPickupEffect;

            Modules.RegiAssets.AddNewEffectDef(slowStartOrb);
        }

        private static GameObject CreateBodyPrefab(bool isPlayer)
        {
            bool isLoreFriendly = false;

            if (!isPlayer && Modules.Config.loreFriendly) isLoreFriendly = true;
            if (isPlayer && Modules.Config.loreFriendly2) isLoreFriendly = true;

            string name = bodyName;
            if (isPlayer) name = "RegigigasPlayerBody";

            string iconName = "Regigigas";
            if (isPlayer) iconName = "RegigigasPlayer";

            Color charColor = Color.yellow;
            if (isLoreFriendly)
            {
                charColor = Color.grey;
                iconName = "StoneGigasEnemy";
                if (isPlayer) iconName = "StoneGigas";
            }

            string _nameToken = RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_NAME";
            if (isLoreFriendly) _nameToken += "2";

            #region Body
            GameObject newPrefab = Modules.Prefabs.CreatePrefab(name, "mdlRegigigas", new BodyInfo
            {
                armor = 20f,
                armorGrowth = 0f,
                bodyName = name,
                bodyNameToken = _nameToken,
                bodyColor = charColor,
                characterPortrait = Modules.RegiAssets.LoadCharacterIcon(iconName),
                crosshair = Modules.RegiAssets.ancientPowerCrosshairPrefab,
                damage = 40f,
                healthGrowth = 1260f,
                healthRegen = 0f,
                jumpCount = 1,
                maxHealth = 4200f,
                subtitleNameToken = RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_SUBTITLE",
                podPrefab = null,
                moveSpeed = 8f,
                jumpPower = 35f,
                attackSpeed = 2f
            });

            CharacterBody body = newPrefab.GetComponent<CharacterBody>();
            body.hideCrosshair = true;
            body.hullClassification = HullClassification.Golem;
            body.bodyFlags = CharacterBody.BodyFlags.None;
            body.isChampion = true;
            body.preferredInitialStateType = new EntityStates.SerializableEntityStateType(typeof(SpawnState));

            CharacterMotor motor = newPrefab.GetComponent<CharacterMotor>();
            motor.mass = 10000f;

            SfxLocator sfx = newPrefab.GetComponent<SfxLocator>();
            sfx.barkSound = "";
            sfx.landingSound = "Play_gravekeeper_land";
            sfx.deathSound = "sfx_regigigas_death";
            sfx.fallDamageSound = "";

            FootstepHandler footstep = newPrefab.GetComponentInChildren<FootstepHandler>();
            footstep.footstepDustPrefab = Resources.Load<GameObject>("Prefabs/GenericHugeFootstepDust");
            footstep.baseFootstepString = "Play_moonBrother_step";
            footstep.sprintFootstepOverrideString = "Play_moonBrother_sprint";

            CharacterCameraParams regiParams = Modules.CameraParams.CreateCameraParamsWithData(RegigigasCameraParams.DEFAULT);

            KinematicCharacterMotor characterController = newPrefab.GetComponent<KinematicCharacterMotor>();
            characterController.CapsuleRadius = 4f;
            characterController.CapsuleHeight = 9f;

            CharacterDirection direction = newPrefab.GetComponent<CharacterDirection>();
            direction.turnSpeed = 135f;

            Interactor interactor = newPrefab.GetComponent<Interactor>();
            interactor.maxInteractionDistance = 8f;

            newPrefab.GetComponent<CameraTargetParams>().cameraParams = regiParams;

            newPrefab.GetComponent<EntityStateMachine>().mainStateType = new EntityStates.SerializableEntityStateType(typeof(MainState));

            var state = isPlayer ? typeof(EntityStates.SpawnTeleporterState) : typeof(SpawnState);
            newPrefab.GetComponent<EntityStateMachine>().initialStateType = new EntityStates.SerializableEntityStateType(state);

            newPrefab.GetComponent<CharacterDeathBehavior>().deathState = new EntityStates.SerializableEntityStateType(typeof(DeathState));

            RegigigasPlugin.Destroy(newPrefab.GetComponent<SetStateOnHurt>());

            if (!isPlayer)
            {
                DeathRewards deathRewards = newPrefab.AddComponent<DeathRewards>();
                deathRewards.logUnlockableDef = Resources.Load<UnlockableDef>("UnlockableDefs/Logs.Parent.0");

                if (Modules.Config.loreFriendly)
                {
                    // stone guy drops knurl now sorry
                    deathRewards.bossPickup = new SerializablePickupIndex
                    {
                        pickupName = "ItemIndex.Knurl"
                    };
                }
                else
                {
                    deathRewards.bossPickup = new SerializablePickupIndex
                    {
                        pickupName = "ItemIndex.Pearl"
                    };
                }

                newPrefab.AddComponent<Components.RegigigasShinyComponent>();
            }

            newPrefab.AddComponent<Modules.Components.RegigigasController>();
            newPrefab.AddComponent<Modules.Components.RegigigasFlashController>();
            newPrefab.AddComponent<Modules.Components.SlowStartController>();
            #endregion

            #region Model
            Material bodyMat = null;
            if (isLoreFriendly)
            {
                GameObject golem = Resources.Load<GameObject>("Prefabs/CharacterBodies/GolemBody");
                bodyMat = new Material(golem.GetComponentInChildren<CharacterModel>().baseRendererInfos[0].defaultMaterial);

                // this is for the golem material to render correctly
                //simple way
                //bodyMat.DisableKeyword("PRINT_CUTOFF");
                //bodyMat.SetInt(PrintController.printOnPropertyId, 0);

                //not simple way that keeps the red glow on the legs
                PrintController print1 = newPrefab.GetComponent<ModelLocator>().modelTransform.gameObject.AddComponent<PrintController>();
                PrintController print2 = golem.GetComponentInChildren<PrintController>();

                // ew
                print1.age = print2.age;
                print1.animateFlowmapPower = print2.animateFlowmapPower;
                print1.disableWhenFinished = print2.disableWhenFinished;
                print1.maxFlowmapPower = print2.maxFlowmapPower;
                print1.maxPrintBias = print2.maxPrintBias;
                print1.maxPrintHeight = 69f;// print2.maxPrintHeight;
                print1.printCurve = print2.printCurve;
                print1.printTime = print2.printTime;
                print1.startingFlowmapPower = print2.startingFlowmapPower;
                print1.startingPrintBias = print2.startingPrintBias;
                print1.startingPrintHeight = print2.startingPrintHeight;

                // it gets worse
                Transform chestTransform = newPrefab.GetComponentInChildren<ChildLocator>().FindChild("Chest");
                GameObject eye = UnityEngine.GameObject.Instantiate(golem.GetComponentInChildren<ChildLocator>().FindChild("Eye").gameObject);
                eye.transform.parent = chestTransform;
                eye.transform.localPosition = new Vector3(-0.34f, 0.8f, 0.34f);
                eye.transform.localRotation = Quaternion.identity;
                eye.GetComponent<Light>().intensity = 200f;

                GameObject eye2 = UnityEngine.GameObject.Instantiate(eye);
                eye2.transform.parent = chestTransform;
                eye2.transform.localPosition = new Vector3(0.34f, 0.8f, 0.34f);
                eye2.transform.localRotation = Quaternion.identity;

                GameObject eye3 = UnityEngine.GameObject.Instantiate(eye);
                eye3.transform.parent = chestTransform;
                eye3.transform.localPosition = new Vector3(-0.34f, 0.25f, 0.34f);
                eye3.transform.localRotation = Quaternion.identity;

                GameObject eye4 = UnityEngine.GameObject.Instantiate(eye);
                eye4.transform.parent = chestTransform;
                eye4.transform.localPosition = new Vector3(0.34f, 0.25f, 0.34f);
                eye4.transform.localRotation = Quaternion.identity;
            }
            else
            {
                bodyMat = Modules.RegiAssets.CreateMaterial("matRegigigas", 0f, Color.white);
            }

            bodyRendererIndex = 1;

            Modules.Prefabs.SetupCharacterModel(newPrefab, new CustomRendererInfo[] {
                new CustomRendererInfo
                {
                    childName = "DummyModel",
                    material = bodyMat
                },
                new CustomRendererInfo
                {
                    childName = "Model",
                    material = bodyMat
                }}, bodyRendererIndex);

            // this is so incredibly fucking jank but it's for the logbook fix
            if (isLoreFriendly)
            {
                ((SkinnedMeshRenderer)newPrefab.GetComponentInChildren<CharacterModel>().baseRendererInfos[1].renderer).sharedMesh = Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Mesh>("meshRegigigasAlt");
            }

            newPrefab.GetComponentInChildren<CharacterModel>().gameObject.AddComponent<Modules.Components.RegiSkinPicker>();
            #endregion

            CreateHitboxes(newPrefab);
            SetupHurtboxes(newPrefab);
            CreateSkills(newPrefab, isPlayer);
            CreateSkins(newPrefab, isLoreFriendly);
            InitializeItemDisplays(newPrefab);

            return newPrefab;
        }

        private static void CreateSpawnCard()
        {
            minimumStageCount = RegigigasPlugin.instance.Config.Bind<int>(new ConfigDefinition("Regigigas", "Minimum Stage Clear Count"), 3, new ConfigDescription("Number of stages that must be completed before this boss can spawn"));
            spawnCost = RegigigasPlugin.instance.Config.Bind<int>(new ConfigDefinition("Regigigas", "Spawn Cost"), 800, new ConfigDescription("How many director credits does this boss cost"));

            CharacterSpawnCard characterSpawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            characterSpawnCard.name = "cscRegigigas";
            characterSpawnCard.prefab = bossMaster;
            characterSpawnCard.sendOverNetwork = true;
            characterSpawnCard.hullSize = HullClassification.BeetleQueen;
            characterSpawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            characterSpawnCard.requiredFlags = NodeFlags.None;
            characterSpawnCard.forbiddenFlags = NodeFlags.TeleporterOK;
            characterSpawnCard.directorCreditCost = spawnCost.Value;
            characterSpawnCard.occupyPosition = false;
            characterSpawnCard.loadout = new SerializableLoadout();
            characterSpawnCard.noElites = false;
            characterSpawnCard.forbiddenAsBoss = false;

            DirectorCard card = new DirectorCard
            {
                spawnCard = characterSpawnCard,
                selectionWeight = 1,
                preventOverhead = false,
                minimumStageCompletions = minimumStageCount.Value,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Close
            };
            DirectorAPI.DirectorCardHolder regigigasCard = new DirectorAPI.DirectorCardHolder
            {
                Card = card,
                MonsterCategory = DirectorAPI.MonsterCategory.Champions,
            };

            //DirectorCard cardGrove = new DirectorCard
            //{
            //    spawnCard = characterSpawnCard,
            //    selectionWeight = 2,
            //    preventOverhead = false,
            //    minimumStageCompletions = minimumStageCount.Value,
            //    spawnDistance = DirectorCore.MonsterSpawnDistance.Close
            //};
            //DirectorAPI.DirectorCardHolder regigigasCardGrove = new DirectorAPI.DirectorCardHolder
            //{
            //    Card = cardGrove,
            //    MonsterCategory = DirectorAPI.MonsterCategory.Champions,
            //};

            DirectorCard cardLoop = new DirectorCard {
                spawnCard = characterSpawnCard,
                selectionWeight = 1,
                preventOverhead = false,
                minimumStageCompletions = 5,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Close
            };
            DirectorAPI.DirectorCardHolder regigigasCardLoop = new DirectorAPI.DirectorCardHolder {
                Card = cardLoop,
                MonsterCategory = DirectorAPI.MonsterCategory.Champions,
            };

            DirectorCardCategorySelection dissonanceSpawns = Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/Base/MixEnemy/dccsMixEnemy.asset").WaitForCompletion();
            dissonanceSpawns.AddCard(0, card);  //0 is Champions

            foreach (StageSpawnInfo ssi in Config.StageList) {
                DirectorAPI.DirectorCardHolder toAdd = ssi.GetMinStages() == 0 ? regigigasCard : regigigasCardLoop;

                DirectorAPI.Helpers.AddNewMonsterToStage(toAdd, false, DirectorAPI.ParseInternalStageName(ssi.GetStageName()), ssi.GetStageName());
            }

            //DirectorAPI.MonsterActions += delegate (List<DirectorAPI.DirectorCardHolder> list, DirectorAPI.StageInfo stage)
            //{
            //    if (stage.stage == DirectorAPI.Stage.SirensCall 
            //    || stage.stage == DirectorAPI.Stage.RallypointDelta 
            //    || stage.stage == DirectorAPI.Stage.GildedCoast 
            //    || stage.stage == DirectorAPI.Stage.TitanicPlains 
            //    || stage.stage == DirectorAPI.Stage.VoidCell 
            //    || stage.stage == DirectorAPI.Stage.AbandonedAqueduct 
            //    || stage.stage == DirectorAPI.Stage.WetlandAspect)
            //    {
            //        if (!list.Contains(regigigasCard))
            //        {
            //            list.Add(regigigasCard);
            //        }
            //    }

            //    if (stage.stage == DirectorAPI.Stage.Custom && stage.CustomStageName == "rootjungle")
            //    {
            //        if (!list.Contains(regigigasCard))
            //        {
            //            list.Add(regigigasCardGrove);
            //        }
            //    }
            //};

            if (RegigigasPlugin.riskyArtifactsInstalled) SetupRiskyCompat(characterSpawnCard);
        }

        public static void SetupRiskyCompat(CharacterSpawnCard spawnCard)
        {
            if (addToOrigination.Value) Risky_Artifacts.Artifacts.Origin.AddSpawnCard(spawnCard, Risky_Artifacts.Artifacts.Origin.BossTier.t3);
        }

        private static void SetupHurtboxes(GameObject bodyPrefab)
        {
            HurtBoxGroup hurtboxGroup = bodyPrefab.GetComponentInChildren<HurtBoxGroup>();
            List<HurtBox> hurtboxes = new List<HurtBox>();

            hurtboxes.Add(bodyPrefab.GetComponentInChildren<ChildLocator>().FindChild("MainHurtbox").GetComponent<HurtBox>());

            HealthComponent healthComponent = bodyPrefab.GetComponent<HealthComponent>();

            foreach (Collider i in bodyPrefab.GetComponent<ModelLocator>().modelTransform.GetComponentsInChildren<Collider>())
            {
                if (i.gameObject.name != "MainHurtbox")
                {
                    HurtBox hurtbox = i.gameObject.AddComponent<HurtBox>();
                    hurtbox.gameObject.layer = LayerIndex.entityPrecise.intVal;
                    hurtbox.healthComponent = healthComponent;
                    hurtbox.isBullseye = false;
                    hurtbox.damageModifier = HurtBox.DamageModifier.Normal;
                    hurtbox.hurtBoxGroup = hurtboxGroup;

                    hurtboxes.Add(hurtbox);
                }
            }

            //creating weakpoint hitboxes from code becuase the unity project is lost
            Vector3[] eyeSpots = new Vector3[] {
                
                new Vector3(0,  -0.0341f, 0.7026f),
                
                //all his eyes made it way too easy to hit
                //also these are for chest not head
                //new Vector3(0,  1.0314f, 0.7869f),//top
                //new Vector3(0,  0.6466f, 0.7848f),//bottom
                //new Vector3(0.1653f, 0.8316f, 0.6549f),//left
                //new Vector3(-0.1653f, 0.8316f, 0.6549f),//right

                //new Vector3(0,  0.3124f, 0.8173f),
                //new Vector3(0, -0.0335f, 0.7869f),
                //new Vector3(0, -0.3514f, 0.7100f),
            };

            Transform chest = bodyPrefab.GetComponentInChildren<ChildLocator>().FindChild("Head");

            HurtBox eyeHurtbox = UnityEngine.Object.Instantiate(hurtboxes[0], chest);
            eyeHurtbox.isSniperTarget = true;
            CapsuleCollider collider = eyeHurtbox.transform.GetComponent<CapsuleCollider>();
            collider.radius = 0.2f;
            collider.height = collider.radius * 2;

            eyeHurtbox.transform.localPosition = eyeSpots[0];

            hurtboxes.Add(eyeHurtbox);

            for (int i = 1; i < eyeSpots.Length; i++) {

                eyeHurtbox = UnityEngine.Object.Instantiate(eyeHurtbox, chest);
                eyeHurtbox.transform.localPosition = eyeSpots[i];

                hurtboxes.Add(eyeHurtbox);
            }

            hurtboxGroup.hurtBoxes = hurtboxes.ToArray();
        }

        private static GameObject CreateMaster(GameObject bodyPrefab, string masterName)
        {
            GameObject newMaster = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/LemurianMaster"), masterName, true);
            newMaster.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;

            #region AI
            foreach (AISkillDriver ai in newMaster.GetComponentsInChildren<AISkillDriver>())
            {
                RegigigasPlugin.DestroyImmediate(ai);
            }

            newMaster.GetComponent<BaseAI>().fullVision = true;

            AISkillDriver revengeDriver = newMaster.AddComponent<AISkillDriver>();
            revengeDriver.customName = "Revenge";
            revengeDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            revengeDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            revengeDriver.activationRequiresAimConfirmation = true;
            revengeDriver.activationRequiresTargetLoS = false;
            revengeDriver.selectionRequiresTargetLoS = true;
            revengeDriver.maxDistance = 24f;
            revengeDriver.minDistance = 0f;
            revengeDriver.requireSkillReady = true;
            revengeDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            revengeDriver.ignoreNodeGraph = true;
            revengeDriver.moveInputScale = 1f;
            revengeDriver.driverUpdateTimerOverride = 2.5f;
            revengeDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            revengeDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            revengeDriver.maxTargetHealthFraction = Mathf.Infinity;
            revengeDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            revengeDriver.maxUserHealthFraction = 0.5f;
            revengeDriver.skillSlot = SkillSlot.Special;

            AISkillDriver grabDriver = newMaster.AddComponent<AISkillDriver>();
            grabDriver.customName = "Grab";
            grabDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            grabDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            grabDriver.activationRequiresAimConfirmation = true;
            grabDriver.activationRequiresTargetLoS = false;
            grabDriver.selectionRequiresTargetLoS = true;
            grabDriver.maxDistance = 8f;
            grabDriver.minDistance = 0f;
            grabDriver.requireSkillReady = true;
            grabDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            grabDriver.ignoreNodeGraph = true;
            grabDriver.moveInputScale = 1f;
            grabDriver.driverUpdateTimerOverride = 0.5f;
            grabDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            grabDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            grabDriver.maxTargetHealthFraction = Mathf.Infinity;
            grabDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            grabDriver.maxUserHealthFraction = Mathf.Infinity;
            grabDriver.skillSlot = SkillSlot.Primary;

            AISkillDriver stompDriver = newMaster.AddComponent<AISkillDriver>();
            stompDriver.customName = "Stomp";
            stompDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            stompDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            stompDriver.activationRequiresAimConfirmation = true;
            stompDriver.activationRequiresTargetLoS = false;
            stompDriver.selectionRequiresTargetLoS = true;
            stompDriver.maxDistance = 32f;
            stompDriver.minDistance = 0f;
            stompDriver.requireSkillReady = true;
            stompDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            stompDriver.ignoreNodeGraph = true;
            stompDriver.moveInputScale = 0.4f;
            stompDriver.driverUpdateTimerOverride = 0.5f;
            stompDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            stompDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            stompDriver.maxTargetHealthFraction = Mathf.Infinity;
            stompDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            stompDriver.maxUserHealthFraction = Mathf.Infinity;
            stompDriver.skillSlot = SkillSlot.Secondary;

            AISkillDriver followCloseDriver = newMaster.AddComponent<AISkillDriver>();
            followCloseDriver.customName = "ChaseClose";
            followCloseDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            followCloseDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            followCloseDriver.activationRequiresAimConfirmation = false;
            followCloseDriver.activationRequiresTargetLoS = false;
            followCloseDriver.maxDistance = 32f;
            followCloseDriver.minDistance = 0f;
            followCloseDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            followCloseDriver.ignoreNodeGraph = false;
            followCloseDriver.moveInputScale = 1f;
            followCloseDriver.driverUpdateTimerOverride = -1f;
            followCloseDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            followCloseDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            followCloseDriver.maxTargetHealthFraction = Mathf.Infinity;
            followCloseDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            followCloseDriver.maxUserHealthFraction = Mathf.Infinity;
            followCloseDriver.skillSlot = SkillSlot.None;

            AISkillDriver followDriver = newMaster.AddComponent<AISkillDriver>();
            followDriver.customName = "Chase";
            followDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            followDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            followDriver.activationRequiresAimConfirmation = false;
            followDriver.activationRequiresTargetLoS = false;
            followDriver.maxDistance = Mathf.Infinity;
            followDriver.minDistance = 0f;
            followDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            followDriver.ignoreNodeGraph = false;
            followDriver.moveInputScale = 1f;
            followDriver.driverUpdateTimerOverride = -1f;
            followDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            followDriver.minTargetHealthFraction = Mathf.NegativeInfinity;
            followDriver.maxTargetHealthFraction = Mathf.Infinity;
            followDriver.minUserHealthFraction = Mathf.NegativeInfinity;
            followDriver.maxUserHealthFraction = Mathf.Infinity;
            followDriver.skillSlot = SkillSlot.None;
            followDriver.shouldSprint = true;
            #endregion

            Modules.Prefabs.masterPrefabs.Add(newMaster);

            return newMaster;
        }

        private static void CreateHitboxes(GameObject prefab)
        {
            ChildLocator childLocator = prefab.GetComponentInChildren<ChildLocator>();
            GameObject model = childLocator.gameObject;

            Transform hitboxTransform = childLocator.FindChild("PunchHitbox");
            Modules.Prefabs.SetupHitbox(model, hitboxTransform, "Punch");
        }

        private static void CreateSkills(GameObject prefab, bool isPlayer)
        {
            Modules.Skills.CreateSkillFamilies(prefab);

            string prefix = RegigigasPlugin.developerPrefix;
            SkillLocator skillLocator = prefab.GetComponent<SkillLocator>();

            skillLocator.passiveSkill.enabled = true;
            skillLocator.passiveSkill.skillNameToken = prefix + "_REGIGIGAS_BODY_PASSIVE_NAME";
            skillLocator.passiveSkill.skillDescriptionToken = prefix + "_REGIGIGAS_BODY_PASSIVE_DESCRIPTION";
            skillLocator.passiveSkill.icon = Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texSlowStartIcon");

            #region Primary
            if (!lunarPunchSkillDef)
            {
                lunarPunchSkillDef = Modules.Skills.CreatePrimarySkillDef(new EntityStates.SerializableEntityStateType(typeof(SkillStates.Regigigas.Lunar.Punch)), "Weapon", prefix + "_REGIGIGAS_BODY_PRIMARY_ICEPUNCH_NAME", prefix + "_REGIGIGAS_BODY_PRIMARY_ICEPUNCH_DESCRIPTION", Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texIcePunchIcon"), false);
            }

            if (isPlayer)
            {
                Modules.Skills.AddPrimarySkills(prefab, Modules.Skills.CreatePrimarySkillDef(new EntityStates.SerializableEntityStateType(typeof(DrainPunch)), "Weapon", prefix + "_REGIGIGAS_BODY_PRIMARY_DRAINPUNCH_NAME", prefix + "_REGIGIGAS_BODY_PRIMARY_DRAINPUNCH_DESCRIPTION", Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texNewDrainPunchIcon"), false));
                Modules.Skills.AddPrimarySkills(prefab, Modules.Skills.CreatePrimarySkillDef(new EntityStates.SerializableEntityStateType(typeof(PunchCombo)), "Weapon", prefix + "_REGIGIGAS_BODY_PRIMARY_PUNCH_NAME", prefix + "_REGIGIGAS_BODY_PRIMARY_PUNCH_DESCRIPTION", Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texNewPunchIcon"), false));
                Modules.Skills.AddPrimarySkills(prefab, Modules.Skills.CreatePrimarySkillDef(new EntityStates.SerializableEntityStateType(typeof(IcePunch)), "Weapon", prefix + "_REGIGIGAS_BODY_PRIMARY_ICEPUNCH_NAME", prefix + "_REGIGIGAS_BODY_PRIMARY_ICEPUNCH_DESCRIPTION", Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texIcePunchIcon"), false));
                Modules.Skills.AddPrimarySkills(prefab, Modules.Skills.CreatePrimarySkillDef(new EntityStates.SerializableEntityStateType(typeof(MachPunch)), "Weapon", prefix + "_REGIGIGAS_BODY_PRIMARY_MACHPUNCH_NAME", prefix + "_REGIGIGAS_BODY_PRIMARY_MACHPUNCH_DESCRIPTION", Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texNewPunchIcon"), false));
            }
            else
            {
                if (Modules.Config.nerfedMelee)
                {
                    Modules.Skills.AddPrimarySkills(prefab, Modules.Skills.CreatePrimarySkillDef(new EntityStates.SerializableEntityStateType(typeof(IcePunch)), "Weapon", prefix + "_REGIGIGAS_BODY_PRIMARY_ICEPUNCH_NAME", prefix + "_REGIGIGAS_BODY_PRIMARY_ICEPUNCH_DESCRIPTION", Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texIcePunchIcon"), false));
                }
                else
                {
                    Modules.Skills.AddPrimarySkills(prefab, Modules.Skills.CreatePrimarySkillDef(new EntityStates.SerializableEntityStateType(typeof(GrabAttempt)), "Body", prefix + "_REGIGIGAS_BODY_PRIMARY_GRAB_NAME", prefix + "_REGIGIGAS_BODY_PRIMARY_GRAB_DESCRIPTION", Modules.RegiAssets.mainAssetBundle.LoadAsset<Sprite>("texCrushGripIcon"), false));
                }
            }
            #endregion

            #region Secondary
            SkillDef earthPowerSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_REGIGIGAS_BODY_SECONDARY_EARTHQUAKE_NAME",
                skillNameToken = prefix + "_REGIGIGAS_BODY_SECONDARY_EARTHQUAKE_NAME",
                skillDescriptionToken = prefix + "_REGIGIGAS_BODY_SECONDARY_EARTHQUAKE_DESCRIPTION",
                skillIcon = Modules.RegiAssets.mainAssetBundle.LoadAsset<Sprite>("texEarthPowerIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(Stomp)),
                activationStateMachineName = "Body",
                baseMaxStock = 1,
                baseRechargeInterval = 8f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.Any,
                resetCooldownTimerOnUse = false,
                isCombatSkill = true,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,
            });

            if (!lunarStompSkillDef)
            {
                lunarStompSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
                {
                    skillName = prefix + "_REGIGIGAS_BODY_SECONDARY_EARTHQUAKE_NAME",
                    skillNameToken = prefix + "_REGIGIGAS_BODY_SECONDARY_EARTHQUAKE_NAME",
                    skillDescriptionToken = prefix + "_REGIGIGAS_BODY_SECONDARY_EARTHQUAKE_DESCRIPTION",
                    skillIcon = Modules.RegiAssets.mainAssetBundle.LoadAsset<Sprite>("texEarthPowerIcon"),
                    activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Regigigas.Lunar.Stomp)),
                    activationStateMachineName = "Body",
                    baseMaxStock = 1,
                    baseRechargeInterval = 6f,
                    beginSkillCooldownOnSkillEnd = true,
                    canceledFromSprinting = false,
                    forceSprintDuringState = false,
                    fullRestockOnAssign = true,
                    interruptPriority = EntityStates.InterruptPriority.Any,
                    resetCooldownTimerOnUse = false,
                    isCombatSkill = true,
                    mustKeyPress = false,
                    cancelSprintingOnActivation = true,
                    rechargeStock = 1,
                    requiredStock = 1,
                    stockToConsume = 1,
                });
            }

            SkillDef ancientPowerSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_REGIGIGAS_BODY_SECONDARY_ANCIENTPOWER_NAME",
                skillNameToken = prefix + "_REGIGIGAS_BODY_SECONDARY_ANCIENTPOWER_NAME",
                skillDescriptionToken = prefix + "_REGIGIGAS_BODY_SECONDARY_ANCIENTPOWER_DESCRIPTION",
                skillIcon = Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texNewAncientPowerIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(ChargeAncientPower)),
                activationStateMachineName = "Weapon",
                baseMaxStock = 5,
                baseRechargeInterval = 5f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.Skill,
                resetCooldownTimerOnUse = false,
                isCombatSkill = true,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 0,
                stockToConsume = 0,
            });

            SkillDef crushGripSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_REGIGIGAS_BODY_PRIMARY_GRAB_NAME",
                skillNameToken = prefix + "_REGIGIGAS_BODY_PRIMARY_GRAB_NAME",
                skillDescriptionToken = prefix + "_REGIGIGAS_BODY_PRIMARY_GRAB_DESCRIPTION",
                skillIcon = Modules.RegiAssets.mainAssetBundle.LoadAsset<Sprite>("texCrushGripIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(GrabAttempt)),
                activationStateMachineName = "Body",
                baseMaxStock = 1,
                baseRechargeInterval = 5f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.Skill,
                resetCooldownTimerOnUse = false,
                isCombatSkill = true,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1
            });

            if (isPlayer) Modules.Skills.AddSecondarySkills(prefab, ancientPowerSkillDef, earthPowerSkillDef, crushGripSkillDef);
            else Modules.Skills.AddSecondarySkills(prefab, earthPowerSkillDef, ancientPowerSkillDef, crushGripSkillDef);
            #endregion

            #region Utility
            SkillDef revengeSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_REGIGIGAS_BODY_UTILITY_REVENGE_NAME",
                skillNameToken = prefix + "_REGIGIGAS_BODY_UTILITY_REVENGE_NAME",
                skillDescriptionToken = prefix + "_REGIGIGAS_BODY_UTILITY_REVENGE_DESCRIPTION",
                skillIcon = Modules.RegiAssets.mainAssetBundle.LoadAsset<Sprite>("texRevengeIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(Revenge)),
                activationStateMachineName = "Body",
                baseMaxStock = 1,
                baseRechargeInterval = 24f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.Any,
                resetCooldownTimerOnUse = false,
                isCombatSkill = false,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1
            });

            //Modules.Skills.AddUtilitySkills(prefab, revengeSkillDef);
            #endregion

            #region Special
            SkillDef impactSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_REGIGIGAS_BODY_SPECIAL_SLAM_NAME",
                skillNameToken = prefix + "_REGIGIGAS_BODY_SPECIAL_SLAM_NAME",
                skillDescriptionToken = prefix + "_REGIGIGAS_BODY_SPECIAL_SLAM_DESCRIPTION",
                skillIcon = Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texHeavySlamIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(BounceStart)),
                activationStateMachineName = "Weapon",
                baseMaxStock = 1,
                baseRechargeInterval = 16f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.Skill,
                resetCooldownTimerOnUse = false,
                isCombatSkill = true,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1
            });

            SkillDef impact2SkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = prefix + "_REGIGIGAS_BODY_SPECIAL_IMPACT_NAME",
                skillNameToken = prefix + "_REGIGIGAS_BODY_SPECIAL_IMPACT_NAME",
                skillDescriptionToken = prefix + "_REGIGIGAS_BODY_SPECIAL_IMPACT_DESCRIPTION",
                skillIcon = Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texNewGigaImpactIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Regigigas.GigaImpact.Fire)),
                activationStateMachineName = "Body",
                baseMaxStock = 1,
                baseRechargeInterval = 16f,
                beginSkillCooldownOnSkillEnd = true,
                canceledFromSprinting = false,
                forceSprintDuringState = false,
                fullRestockOnAssign = true,
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,
                resetCooldownTimerOnUse = false,
                isCombatSkill = true,
                mustKeyPress = false,
                cancelSprintingOnActivation = true,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1
            });

            if (!lunarBounceSkillDef)
            {
                lunarBounceSkillDef = Modules.Skills.CreateSkillDef(new SkillDefInfo
                {
                    skillName = prefix + "_REGIGIGAS_BODY_SPECIAL_SLAM_NAME",
                    skillNameToken = prefix + "_REGIGIGAS_BODY_SPECIAL_SLAM_NAME",
                    skillDescriptionToken = prefix + "_REGIGIGAS_BODY_SPECIAL_SLAM_DESCRIPTION",
                    skillIcon = Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texHeavySlamIcon"),
                    activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Regigigas.Lunar.BounceStart)),
                    activationStateMachineName = "Weapon",
                    baseMaxStock = 1,
                    baseRechargeInterval = 14f,
                    beginSkillCooldownOnSkillEnd = true,
                    canceledFromSprinting = false,
                    forceSprintDuringState = false,
                    fullRestockOnAssign = true,
                    interruptPriority = EntityStates.InterruptPriority.Skill,
                    resetCooldownTimerOnUse = false,
                    isCombatSkill = true,
                    mustKeyPress = false,
                    cancelSprintingOnActivation = true,
                    rechargeStock = 1,
                    requiredStock = 1,
                    stockToConsume = 1
                });
            }

            if (isPlayer)
            {
                Modules.Skills.AddSpecialSkills(prefab, impact2SkillDef);
                Modules.Skills.AddUtilitySkills(prefab, impactSkillDef, revengeSkillDef);
            }
            else
            {
                Modules.Skills.AddSpecialSkills(prefab, revengeSkillDef, impact2SkillDef);
                Modules.Skills.AddUtilitySkills(prefab, impactSkillDef);
            }
            #endregion
        }


        public static SkinDef CreateSkinDef(string skinName, Sprite skinIcon, GameObject root, UnlockableDef unlockableDef,
            CharacterModel.RendererInfo[] rendererInfos, SkinDefParams.MeshReplacement[] meshReplacements, SkinDefParams.GameObjectActivation[] gameObjectActivations)
        {
            return R2API.Skins.CreateNewSkinDef(new R2API.SkinDefParamsInfo
            {
                Name = skinName,
                NameToken = skinName,
                Icon = skinIcon,
                RootObject = root,
                UnlockableDef = unlockableDef,
                RendererInfos = rendererInfos,
                MeshReplacements = meshReplacements,
                GameObjectActivations = gameObjectActivations,
                BaseSkins = [],
                MinionSkinReplacements = [],
                ProjectileGhostReplacements = []
            });
        }

        public static CharacterModel.RendererInfo[] SkinRendererInfos(CharacterModel.RendererInfo[] defaultRenderers, Material[] materials)
        {
            CharacterModel.RendererInfo[] newRendererInfos = new CharacterModel.RendererInfo[defaultRenderers.Length];
            defaultRenderers.CopyTo(newRendererInfos, 0);

            for (int i = 0; i < materials.Length; i++)
                newRendererInfos[i].defaultMaterial = materials[i];

            return newRendererInfos;
        }

        private static void CreateSkins(GameObject prefab, bool isLoreFriendly)
        {
            GameObject model = prefab.GetComponentInChildren<ModelLocator>().modelTransform.gameObject;
            CharacterModel characterModel = model.GetComponent<CharacterModel>();

            ModelSkinController skinController = model.AddComponent<ModelSkinController>();
            ChildLocator childLocator = model.GetComponent<ChildLocator>();

            SkinnedMeshRenderer mainRenderer = characterModel.mainSkinnedMeshRenderer;

            CharacterModel.RendererInfo[] defaultRenderers = characterModel.baseRendererInfos;

            List<SkinDef> skins = new List<SkinDef>();

            // this should work right
            #region DefaultSkin
            SkinDefParams.MeshReplacement[] meshReplacements = null;
            if (isLoreFriendly)
            {
                meshReplacements =
                [
                    new SkinDefParams.MeshReplacement
                    {
                        mesh = Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Mesh>("meshRegigigasAlt"),
                        renderer = mainRenderer
                    }
                ];
            }

            SkinDef defaultSkin = CreateSkinDef(
                skinName: RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_DEFAULT_SKIN_NAME",
                skinIcon: RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texDefaultSkinIcon"),
                root: model,
                unlockableDef: null,
                rendererInfos: defaultRenderers,
                meshReplacements: meshReplacements,
                gameObjectActivations: null);

            skins.Add(defaultSkin);
            #endregion


            #region MasterySkin
            SkinDef masterySkin = CreateSkinDef(
                skinName: RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_MONSOON_SKIN_NAME",
                skinIcon: RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texMasterySkinIcon"),
                root: model,
                unlockableDef: masteryUnlockableDef,
                rendererInfos: SkinRendererInfos(defaultRenderers,
                [
                    isLoreFriendly ? Addressables.LoadAssetAsync<Material>("RoR2/Base/Titan/matTitanGold.mat").WaitForCompletion() 
                                   : Modules.RegiAssets.CreateMaterial("matRegigigasShiny", 0f, Color.white)
                ]),
                meshReplacements: meshReplacements,
                gameObjectActivations: null);

            skins.Add(masterySkin);
            #endregion

            #region BowserSkin
            SkinDef bowserSkin = CreateSkinDef(
                skinName: RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_BOWSER_SKIN_NAME",
                skinIcon: RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texBowserSkin"),
                root: model,
                unlockableDef: null,
                rendererInfos: SkinRendererInfos(defaultRenderers,
                [
                    Modules.RegiAssets.CreateMaterial2("matBowser", 0f, Color.black, 1f)
                ]),
                meshReplacements:
                [
                    new SkinDefParams.MeshReplacement
                    {
                        mesh = Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Mesh>("meshBowser"),
                        renderer = mainRenderer
                    }
                ],
                gameObjectActivations: null);

            //skins.Add(bowserSkin);
            #endregion

            skinController.skins = [.. skins];
        }

        private static void InitializeItemDisplays(GameObject prefab)
        {
            CharacterModel characterModel = prefab.GetComponentInChildren<CharacterModel>();

            if (itemDisplayRuleSet == null)
            {
                itemDisplayRuleSet = ScriptableObject.CreateInstance<ItemDisplayRuleSet>();
                itemDisplayRuleSet.name = "idrs" + bodyName;
            }

            characterModel.itemDisplayRuleSet = itemDisplayRuleSet;
        }

        internal static void SetItemDisplays()
        {
            itemDisplayRules = new List<ItemDisplayRuleSet.KeyAssetRuleGroup>();

            // add item displays here
            //  HIGHLY recommend using KingEnderBrine's ItemDisplayPlacementHelper mod for this
            #region Item Displays
            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Jetpack,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBugWings"),
childName = "Chest",
localPos = new Vector3(0F, 0.184F, -0.5651F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.5279F, 0.5279F, 0.5279F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.GoldGat,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGoldGat"),
childName = "Chest",
localPos = new Vector3(0.4688F, 1.146F, -0.0228F),
localAngles = new Vector3(5.4822F, 87.6507F, 326.8349F),
localScale = new Vector3(0.3452F, 0.3452F, 0.3452F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.BFG,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBFG"),
childName = "Chest",
localPos = new Vector3(0.5039F, 0.5503F, -0.4106F),
localAngles = new Vector3(0F, 354.982F, 350.9422F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.CritGlasses,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGlasses"),
childName = "Head",
localPos = new Vector3(0F, -0.6914F, 0.6125F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1.5465F, 0.8873F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Syringe,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySyringeCluster"),
childName = "Chest",
localPos = new Vector3(-0.48016F, -0.08906F, -0.48626F),
localAngles = new Vector3(353.6544F, 289.9089F, 87.70774F),
localScale = new Vector3(0.60334F, 0.60334F, 0.60334F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Behemoth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBehemoth"),
childName = "Chest",
localPos = new Vector3(-0.42951F, 1.10023F, 1.03641F),
localAngles = new Vector3(274.8848F, 0.00001F, 180F),
localScale = new Vector3(0.20987F, 0.20987F, 0.20987F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Missile,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMissileLauncher"),
childName = "Chest",
localPos = new Vector3(-0.79742F, 1.67357F, 0.25588F),
localAngles = new Vector3(0F, 0F, 20.30657F),
localScale = new Vector3(0.29291F, 0.29291F, 0.29291F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Dagger,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDagger"),
childName = "UpperArmL",
localPos = new Vector3(-0.33929F, -0.51308F, 0.1083F),
localAngles = new Vector3(24.04751F, 57.70527F, 91.49331F),
localScale = new Vector3(2.53478F, 2.53478F, 2.50847F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Hoof,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayHoof"),
childName = "CalfL",
localPos = new Vector3(0.2869F, 0.2642F, 0F),
localAngles = new Vector3(55.8093F, 270F, 0F),
localScale = new Vector3(0.4506F, 0.4001F, 0.1641F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ChainLightning,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayUkulele"),
childName = "Chest",
localPos = new Vector3(-0.09286F, 0.32996F, 0.3088F),
localAngles = new Vector3(0F, 90F, 70.83835F),
localScale = new Vector3(1.61559F, 1.61559F, 1.61559F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.GhostOnKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMask"),
childName = "Head",
localPos = new Vector3(0F, -0.0523F, 0.6298F),
localAngles = new Vector3(333.5951F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Mushroom,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMushroom"),
childName = "FootR",
localPos = new Vector3(0.0812F, -0.0456F, 0.0773F),
localAngles = new Vector3(67.8714F, 226.4006F, 180F),
localScale = new Vector3(0.2363F, 0.2363F, 0.2363F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.AttackSpeedOnCrit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWolfPelt"),
childName = "Head",
localPos = new Vector3(0F, 0.2783F, -0.002F),
localAngles = new Vector3(358.4554F, 0F, 0F),
localScale = new Vector3(0.5666F, 0.5666F, 0.5666F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BleedOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTriTip"),
childName = "HandL",
localPos = new Vector3(-0.1194F, 0.4038F, -0.0871F),
localAngles = new Vector3(270F, 79.8952F, 0F),
localScale = new Vector3(2.3065F, 2.3065F, 0.881F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.WardOnLevel,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWarbanner"),
childName = "Pelvis",
localPos = new Vector3(0F, 0.5767F, -0.8333F),
localAngles = new Vector3(0F, 0F, 90F),
localScale = new Vector3(1.4718F, 1.4718F, 1.4718F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.HealOnCrit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayScythe"),
childName = "Chest",
localPos = new Vector3(-0.5369F, 1.5847F, 0.0983F),
localAngles = new Vector3(355.0806F, 8.1859F, 114.0644F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.HealWhileSafe,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySnail"),
childName = "FootR",
localPos = new Vector3(-0.076F, -0.2002F, 0.082F),
localAngles = new Vector3(81.6783F, 317.1524F, 180F),
localScale = new Vector3(0.357F, 0.357F, 0.357F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Clover,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayClover"),
childName = "FootR",
localPos = new Vector3(-0.0017F, -0.30203F, -0.33739F),
localAngles = new Vector3(85.61921F, 0.0001F, 179.4897F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BarrierOnOverHeal,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAegis"),
childName = "Chest",
localPos = new Vector3(0F, 0.1302F, 0.7321F),
localAngles = new Vector3(275.0695F, 0F, 0F),
localScale = new Vector3(0.4083F, 0.4083F, 0.4083F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.GoldOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBoneCrown"),
childName = "Head",
localPos = new Vector3(0F, -0.1538F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(2.7392F, 4.1238F, 3.9193F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.WarCryOnMultiKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayPauldron"),
childName = "UpperArmL",
localPos = new Vector3(0.1904F, 0.0241F, 0.0469F),
localAngles = new Vector3(77.2628F, 76.1643F, 0F),
localScale = new Vector3(3.231F, 3.231F, 3.231F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SprintArmor,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBuckler"),
childName = "HandR",
localPos = new Vector3(-0.0702F, 0.2846F, 0.0001F),
localAngles = new Vector3(273.4401F, 270F, 90F),
localScale = new Vector3(0.618F, 0.618F, 0.618F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.IceRing,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayIceRing"),
childName = "LowerArmR",
localPos = new Vector3(0.0334F, 0.2587F, -0.1223F),
localAngles = new Vector3(274.3965F, 90F, 270F),
localScale = new Vector3(1.40409F, 1.40409F, 1.40409F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FireRing,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFireRing"),
childName = "LowerArmR",
localPos = new Vector3(0F, 0.387F, 0F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(1.6394F, 1.6394F, 1.6394F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.UtilitySkillMagazine,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAfterburnerShoulderRing"),
childName = "UpperArmL",
localPos = new Vector3(0.00501F, 0.00003F, -0.03148F),
localAngles = new Vector3(270F, 170.3474F, 0F),
localScale = new Vector3(1.59055F, 1.59055F, 1.59055F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAfterburnerShoulderRing"),
childName = "UpperArmR",
localPos = new Vector3(0.00501F, 0F, -0.03148F),
localAngles = new Vector3(60.53529F, 341.322F, 45.18088F),
localScale = new Vector3(1.59055F, 1.59055F, 1.59055F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.JumpBoost,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWaxBird"),
childName = "Head",
localPos = new Vector3(0F, 0.0529F, -0.1242F),
localAngles = new Vector3(24.419F, 0F, 0F),
localScale = new Vector3(0.5253F, 0.5253F, 0.5253F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ArmorReductionOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWarhammer"),
childName = "Chest",
localPos = new Vector3(0.4833F, 1.24642F, -0.24251F),
localAngles = new Vector3(342.9749F, 307.4495F, 107.3058F),
localScale = new Vector3(0.42408F, 0.42408F, 0.42408F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.NearbyDamageBonus,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDiamond"),
childName = "HandL",
localPos = new Vector3(-0.0984F, 0.3897F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.3879F, 0.3879F, 0.3879F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDiamond"),
childName = "HandR",
localPos = new Vector3(-0.0984F, 0.3897F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.3879F, 0.3879F, 0.3879F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ArmorPlate,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRepulsionArmorPlate"),
childName = "ThighL",
localPos = new Vector3(0F, 0.4032F, -0.1655F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.CommandMissile,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMissileRack"),
childName = "Chest",
localPos = new Vector3(0F, 0F, -0.6928F),
localAngles = new Vector3(83.8565F, 0F, 180F),
localScale = new Vector3(1.9801F, 1.9801F, 1.9801F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Feather,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFeather"),
childName = "LowerArmL",
localPos = new Vector3(-0.19168F, 0.27558F, -0.11192F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.09946F, 0.09946F, 0.09946F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Crowbar,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayCrowbar"),
childName = "Chest",
localPos = new Vector3(-0.76858F, 0.27133F, -0.58124F),
localAngles = new Vector3(37.19141F, 154.8673F, 0F),
localScale = new Vector3(0.81359F, 0.81359F, 0.81359F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FallBoots,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGravBoots"),
childName = "CalfL",
localPos = new Vector3(-0.0038F, 0.3729F, -0.0046F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1.13114F, 1.13114F, 1.13114F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGravBoots"),
childName = "CalfR",
localPos = new Vector3(-0.0038F, 0.3729F, -0.0046F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1.25586F, 1.25586F, 1.25586F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ExecuteLowHealthElite,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGuillotine"),
childName = "Chest",
localPos = new Vector3(0.8369F, 1.2494F, -0.7704F),
localAngles = new Vector3(330.2193F, 177.1101F, 267.1591F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.EquipmentMagazine,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBattery"),
childName = "Chest",
localPos = new Vector3(0.5581F, 0.7718F, 0.4092F),
localAngles = new Vector3(49.7878F, 9.4627F, 182.7204F),
localScale = new Vector3(0.2149F, 0.2149F, 0.2149F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.NovaOnHeal,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDevilHorns"),
childName = "Head",
localPos = new Vector3(0.09489F, -0.1928F, 0.56997F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1.66606F, 1.66606F, 1.66606F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDevilHorns"),
childName = "Head",
localPos = new Vector3(-0.09489F, -0.1928F, 0.56997F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(-1.66606F, 1.66606F, 1.66606F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Infusion,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayInfusion"),
childName = "Pelvis",
localPos = new Vector3(-0.41511F, 0.48808F, -0.57027F),
localAngles = new Vector3(343.5917F, 31.09355F, 349.7681F),
localScale = new Vector3(1.01135F, 1.01135F, 1.01135F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Medkit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMedkit"),
childName = "Chest",
localPos = new Vector3(-0.50472F, 0.62536F, 0.42338F),
localAngles = new Vector3(310.3619F, 140.8987F, 197.3985F),
localScale = new Vector3(1.23997F, 1.23997F, 1.23997F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Bandolier,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBandolier"),
childName = "Chest",
localPos = new Vector3(-0.06288F, -0.71258F, 0F),
localAngles = new Vector3(90F, 180F, 0F),
localScale = new Vector3(1.91502F, 2.37592F, 0.48777F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BounceNearby,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayHook"),
childName = "LowerArmR",
localPos = new Vector3(0.24682F, 0.5036F, 0.30617F),
localAngles = new Vector3(0F, 132.91F, 8.00144F),
localScale = new Vector3(0.84374F, 0.84374F, 0.84374F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.IgniteOnKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGasoline"),
childName = "ThighL",
localPos = new Vector3(0.33027F, 0.39659F, 0.02245F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(0.95221F, 0.95221F, 0.95221F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.StunChanceOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayStunGrenade"),
childName = "ThighR",
localPos = new Vector3(0.00098F, 0.36091F, 0.32414F),
localAngles = new Vector3(84.64566F, 0F, 0F),
localScale = new Vector3(2.17292F, 2.17292F, 2.17292F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Firework,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFirework"),
childName = "Chest",
localPos = new Vector3(0.0086F, 0.0069F, 0.0565F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.1194F, 0.1194F, 0.1194F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LunarDagger,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayLunarDagger"),
childName = "Chest",
localPos = new Vector3(0.64758F, 0.29912F, -0.61104F),
localAngles = new Vector3(293.776F, 336.3584F, 350.1967F),
localScale = new Vector3(0.52607F, 0.52607F, 0.52607F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Knurl,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayKnurl"),
childName = "LowerArmL",
localPos = new Vector3(0.01852F, 0.41024F, -0.30836F),
localAngles = new Vector3(78.87074F, 36.6722F, 105.8275F),
localScale = new Vector3(0.23595F, 0.23595F, 0.23595F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BeetleGland,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBeetleGland"),
childName = "Chest",
localPos = new Vector3(0.80453F, -0.12145F, 0.50536F),
localAngles = new Vector3(68.52694F, 194.2572F, 124.5163F),
localScale = new Vector3(0.12917F, 0.12917F, 0.12917F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SprintBonus,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySoda"),
childName = "Pelvis",
localPos = new Vector3(-0.87329F, 0.50205F, -0.27461F),
localAngles = new Vector3(270F, 251.0168F, 0F),
localScale = new Vector3(0.86578F, 0.86578F, 0.86578F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SecondarySkillMagazine,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDoubleMag"),
childName = "Chest",
localPos = new Vector3(-0.23728F, 1.3545F, 0.20177F),
localAngles = new Vector3(43.21322F, 177.9687F, 3.32021F),
localScale = new Vector3(0.19646F, 0.19646F, 0.19646F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDoubleMag"),
childName = "Chest",
localPos = new Vector3(0.23728F, 1.3545F, 0.20177F),
localAngles = new Vector3(43.21322F, 177.9687F, 3.32021F),
localScale = new Vector3(-0.19646F, 0.19646F, 0.19646F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.StickyBomb,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayStickyBomb"),
childName = "Head",
localPos = new Vector3(-0.00001F, 0.73349F, -0.62605F),
localAngles = new Vector3(330.8843F, 0F, 0F),
localScale = new Vector3(0.95255F, 0.95255F, 0.95255F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.TreasureCache,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayKey"),
childName = "Pelvis",
localPos = new Vector3(-0.37463F, 0.35709F, -0.54593F),
localAngles = new Vector3(359.1296F, 113.1945F, 79.46977F),
localScale = new Vector3(2.47573F, 2.47573F, 2.47573F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BossDamageBonus,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAPRound"),
childName = "Pelvis",
localPos = new Vector3(-0.09369F, 0.227F, -0.54099F),
localAngles = new Vector3(65.45425F, 344.6831F, 337.7953F),
localScale = new Vector3(1.07385F, 1.07385F, 1.07385F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SlowOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBauble"),
childName = "Pelvis",
localPos = new Vector3(0.80792F, 0.07597F, -0.65225F),
localAngles = new Vector3(0F, 84.83021F, 0F),
localScale = new Vector3(0.61666F, 0.61666F, 0.61666F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ExtraLife,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayHippo"),
childName = "Chest",
localPos = new Vector3(0F, 0.8946F, -0.543F),
localAngles = new Vector3(336.1F, 180F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.KillEliteFrenzy,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBrainstalk"),
childName = "Head",
localPos = new Vector3(0F, -0.2836F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.RepeatHeal,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayCorpseFlower"),
childName = "UpperArmR",
localPos = new Vector3(-0.21966F, 0.14843F, -0.11813F),
localAngles = new Vector3(270F, 47.5487F, 0F),
localScale = new Vector3(0.84843F, 0.84843F, 0.84843F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.AutoCastEquipment,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFossil"),
childName = "CalfR",
localPos = new Vector3(-0.00002F, 0.12286F, 0.28104F),
localAngles = new Vector3(0F, 277.6233F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.IncreaseHealing,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAntler"),
childName = "Head",
localPos = new Vector3(0.1003F, 0.269F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.3395F, 0.3395F, 0.3395F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAntler"),
childName = "Head",
localPos = new Vector3(-0.1003F, 0.269F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.3395F, 0.3395F, -0.3395F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.TitanGoldDuringTP,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGoldHeart"),
childName = "Chest",
localPos = new Vector3(-0.30044F, 0.00551F, 0.75263F),
localAngles = new Vector3(335.0033F, 343.2951F, 0F),
localScale = new Vector3(0.38673F, 0.38673F, 0.38673F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SprintWisp,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBrokenMask"),
childName = "UpperArmR",
localPos = new Vector3(-0.02831F, 0.04522F, -0.35931F),
localAngles = new Vector3(0F, 180F, 0F),
localScale = new Vector3(0.61631F, 0.61631F, 0.61631F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BarrierOnKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBrooch"),
childName = "Head",
localPos = new Vector3(0F, -0.0341F, 0.7026F),
localAngles = new Vector3(60.0987F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.TPHealingNova,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGlowFlower"),
childName = "UpperArmL",
localPos = new Vector3(0.17257F, 0.47208F, 0.05231F),
localAngles = new Vector3(0F, 73.1449F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LunarUtilityReplacement,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdFoot"),
childName = "Head",
localPos = new Vector3(-0.54458F, -0.15992F, -0.44903F),
localAngles = new Vector3(6.38042F, 270F, 0F),
localScale = new Vector3(1.24055F, 1.24055F, 1.24055F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Thorns,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRazorwireLeft"),
childName = "UpperArmL",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1.8655F, 1.8655F, 1.8655F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LunarPrimaryReplacement,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(0.4958F, -0.6786F, 0.5263F),
localAngles = new Vector3(270F, 23.8331F, 0F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(0.462F, -1.0016F, 0.5412F),
localAngles = new Vector3(270F, 23.8331F, 0F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(0.4084F, -1.3391F, 0.5182F),
localAngles = new Vector3(279.9289F, 21.5984F, 355.7885F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(-0.462F, -1.0016F, 0.5412F),
localAngles = new Vector3(270F, 336.1669F, 0F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(-0.502F, -0.6753F, 0.541F),
localAngles = new Vector3(270F, 342.6888F, 0F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBirdEye"),
childName = "Head",
localPos = new Vector3(-0.4089F, -1.3426F, 0.5184F),
localAngles = new Vector3(282.3309F, 315.1425F, 29.6489F),
localScale = new Vector3(0.6787F, 0.6787F, 0.6787F),
                            limbMask = LimbFlags.None
                        },
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.NovaOnLowHealth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayJellyGuts"),
childName = "ThighL",
localPos = new Vector3(-0.09928F, 0.18003F, -0.01285F),
localAngles = new Vector3(323.3031F, 0F, 0F),
localScale = new Vector3(0.35171F, 0.35171F, 0.35171F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LunarTrinket,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBeads"),
childName = "LowerArmL",
localPos = new Vector3(-0.14682F, 0.32493F, -0.00001F),
localAngles = new Vector3(0F, 0F, 90F),
localScale = new Vector3(6F, 6F, 6F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Plant,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayInterstellarDeskPlant"),
childName = "Chest",
localPos = new Vector3(0.24858F, 1.29878F, -0.00001F),
localAngles = new Vector3(308.3245F, 90.00002F, 219.6731F),
localScale = new Vector3(0.21971F, 0.21971F, 0.21971F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Bear,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBear"),
childName = "Chest",
localPos = new Vector3(-0.20579F, 0.87464F, -0.45017F),
localAngles = new Vector3(327.2887F, 210.7144F, 54.04544F),
localScale = new Vector3(0.33124F, 0.33124F, 0.33124F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.DeathMark,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDeathMark"),
childName = "LowerArmR",
localPos = new Vector3(0F, 0.4099F, 0.0252F),
localAngles = new Vector3(277.5254F, 0F, 0F),
localScale = new Vector3(-0.10935F, -0.09944F, -0.1353F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ExplodeOnDeath,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWilloWisp"),
childName = "Pelvis",
localPos = new Vector3(-0.92313F, 0.41035F, -0.05429F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.19732F, 0.19732F, 0.19732F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Seed,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySeed"),
childName = "Head",
localPos = new Vector3(-0.77396F, 0.20156F, 0.17595F),
localAngles = new Vector3(344.0657F, 196.8238F, 341.9944F),
localScale = new Vector3(0.11666F, 0.11666F, 0.11666F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SprintOutOfCombat,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWhip"),
childName = "Pelvis",
localPos = new Vector3(0.87087F, 0.52804F, 0F),
localAngles = new Vector3(0F, 0F, 356.1968F),
localScale = new Vector3(0.91785F, 0.91785F, 0.91785F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = JunkContent.Items.CooldownOnCrit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySkull"),
childName = "Chest",
localPos = new Vector3(0F, 0.4783F, 0.4991F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.8005F, 0.8005F, 0.8005F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Phasing,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayStealthkit"),
childName = "CalfL",
localPos = new Vector3(0.37219F, 0.20322F, 0.0013F),
localAngles = new Vector3(270F, 90F, 0F),
localScale = new Vector3(0.87138F, 1.43772F, 0.95888F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.PersonalShield,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayShieldGenerator"),
childName = "Chest",
localPos = new Vector3(0.19986F, 0.14573F, 0.62897F),
localAngles = new Vector3(330.479F, 292.0447F, 97.47173F),
localScale = new Vector3(0.3612F, 0.3612F, 0.3612F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ShockNearby,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTeslaCoil"),
childName = "Chest",
localPos = new Vector3(0.86271F, -0.16653F, -0.30456F),
localAngles = new Vector3(297.395F, 41.39772F, 252.9746F),
localScale = new Vector3(0.70799F, 0.70799F, 0.70799F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ShieldOnly,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayShieldBug"),
childName = "Head",
localPos = new Vector3(0.0868F, 0.3114F, 0F),
localAngles = new Vector3(348.1819F, 268.0985F, 0.3896F),
localScale = new Vector3(0.3521F, 0.3521F, 0.3521F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayShieldBug"),
childName = "Head",
localPos = new Vector3(-0.0868F, 0.3114F, 0F),
localAngles = new Vector3(11.8181F, 268.0985F, 359.6104F),
localScale = new Vector3(0.3521F, 0.3521F, -0.3521F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.AlienHead,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAlienHead"),
childName = "Chest",
localPos = new Vector3(-0.06458F, 1.01766F, -0.08734F),
localAngles = new Vector3(7.83251F, 192.1961F, 183.1065F),
localScale = new Vector3(2.56249F, 2.56249F, 2.56249F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.HeadHunter,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySkullCrown"),
childName = "Head",
localPos = new Vector3(0F, -0.2782F, 0.26529F),
localAngles = new Vector3(318.1623F, 0F, 0F),
localScale = new Vector3(2.16924F, 0.72308F, 0.72308F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.EnergizedOnEquipmentUse,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWarHorn"),
childName = "Pelvis",
localPos = new Vector3(0.79926F, 0.3463F, 0F),
localAngles = new Vector3(22.49351F, 76.45378F, 354.7336F),
localScale = new Vector3(0.71412F, 0.71412F, 0.71412F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FlatHealth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySteakCurved"),
childName = "Head",
localPos = new Vector3(0F, 0.39284F, -0.09037F),
localAngles = new Vector3(294.98F, 180F, 180F),
localScale = new Vector3(0.37123F, 0.34439F, 0.34439F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Tooth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayToothMeshLarge"),
childName = "Head",
localPos = new Vector3(-0.00002F, 0.25492F, 0.18287F),
localAngles = new Vector3(295.4421F, 180F, 180F),
localScale = new Vector3(20.17883F, 20.17883F, 20.17883F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Pearl,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayPearl"),
childName = "LowerArmR",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(308.5302F, 239.8277F, 180F),
localScale = new Vector3(0.2F, 0.2F, 0.2F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.ShinyPearl,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayShinyPearl"),
childName = "LowerArmL",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(308.5302F, 239.8277F, 180F),
localScale = new Vector3(0.2F, 0.2F, 0.2F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BonusGoldPackOnKill,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTome"),
childName = "ThighR",
localPos = new Vector3(0.26367F, 0.28094F, 0.09439F),
localAngles = new Vector3(0F, 69.11245F, 0F),
localScale = new Vector3(0.12716F, 0.12716F, 0.12716F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Squid,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySquidTurret"),
childName = "Head",
localPos = new Vector3(0.23866F, 0.15906F, -0.0005F),
localAngles = new Vector3(52.30758F, 90F, 0F),
localScale = new Vector3(0.11772F, 0.15885F, 0.18582F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Icicle,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFrostRelic"),
childName = "Root",
localPos = new Vector3(1.91132F, 2.12597F, -3.38895F),
localAngles = new Vector3(90F, 0F, 0F),
localScale = new Vector3(6.16466F, 6.16466F, 6.16466F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.Talisman,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTalisman"),
childName = "Root",
localPos = new Vector3(-1.82688F, 2.65262F, -2.91337F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(4.78702F, 4.78702F, 4.78702F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.LaserTurbine,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayLaserTurbine"),
childName = "Chest",
localPos = new Vector3(0F, -0.03297F, -0.78584F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.2159F, 0.2159F, 0.2159F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FocusConvergence,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFocusedConvergence"),
childName = "Root",
localPos = new Vector3(0F, 2.8529F, -3.95118F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.49217F, 0.49217F, 0.49217F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = JunkContent.Items.Incubator,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayAncestralIncubator"),
childName = "Chest",
localPos = new Vector3(0.2342F, 0.8036F, 0.1405F),
localAngles = new Vector3(353.0521F, 317.2421F, 351.5904F),
localScale = new Vector3(0.0528F, 0.0528F, 0.0528F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.FireballsOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFireballsOnHit"),
childName = "HandL",
localPos = new Vector3(0.46404F, 0.91137F, -0.00002F),
localAngles = new Vector3(276.4211F, 90F, 180F),
localScale = new Vector3(0.355F, 0.355F, 0.355F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.SiphonOnLowHealth,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySiphonOnLowHealth"),
childName = "Pelvis",
localPos = new Vector3(-0.63261F, 0.37481F, 0.4516F),
localAngles = new Vector3(18.26073F, 303.4368F, 0F),
localScale = new Vector3(0.1497F, 0.1497F, 0.1497F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.BleedOnHitAndExplode,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBleedOnHitAndExplode"),
childName = "ThighR",
localPos = new Vector3(-0.00001F, 0.0575F, 0.34951F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.16885F, 0.16885F, 0.16885F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.MonstersOnShrineUse,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMonstersOnShrineUse"),
childName = "ThighR",
localPos = new Vector3(0.00525F, 0.364F, 0.27657F),
localAngles = new Vector3(352.4521F, 260.6884F, 11.82157F),
localScale = new Vector3(0.13449F, 0.13449F, 0.13449F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Items.RandomDamageZone,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRandomDamageZone"),
childName = "LowerArmL",
localPos = new Vector3(0.40976F, 0.62986F, 0.28772F),
localAngles = new Vector3(349.218F, 235.9453F, 0F),
localScale = new Vector3(0.20573F, 0.20573F, 0.20573F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Fruit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayFruit"),
childName = "Chest",
localPos = new Vector3(0.01587F, -0.03261F, -0.26515F),
localAngles = new Vector3(342.1497F, 30.33666F, 10.86335F),
localScale = new Vector3(0.5805F, 0.5805F, 0.5805F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixRed,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteHorn"),
childName = "Head",
localPos = new Vector3(0.5048F, -0.08F, 0.0073F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.5F, 0.5F, 0.5F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteHorn"),
childName = "Head",
localPos = new Vector3(-0.5048F, -0.08F, 0.0073F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(-0.5F, 0.5F, 0.5F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixBlue,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteRhinoHorn"),
childName = "Head",
localPos = new Vector3(0F, -0.0795F, 0.807F),
localAngles = new Vector3(315F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        },
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteRhinoHorn"),
childName = "Head",
localPos = new Vector3(0F, 0.2194F, 0.5736F),
localAngles = new Vector3(300F, 0F, 0F),
localScale = new Vector3(0.5F, 0.5F, 0.5F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixWhite,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteIceCrown"),
childName = "Head",
localPos = new Vector3(0F, 0.3406F, 0F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.1371F, 0.1371F, 0.1371F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixPoison,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteUrchinCrown"),
childName = "Head",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.3183F, 0.3183F, 0.3183F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.AffixHaunted,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEliteStealthCrown"),
childName = "Head",
localPos = new Vector3(0F, -0.0472F, -0.2525F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(0.4073F, 0.4073F, 0.4073F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.CritOnUse,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayNeuralImplant"),
childName = "Head",
localPos = new Vector3(0F, -0.8998F, 1.37126F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1.21629F, 1.21629F, 1.21629F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.DroneBackup,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRadio"),
childName = "Pelvis",
localPos = new Vector3(0.91272F, 0.75753F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(1.85321F, 1.85321F, 1.85321F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Lightning,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayLightningArmRight"),
childName = "UpperArmR",
localPos = new Vector3(0F, 0F, 0F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(2.1862F, 2.1862F, 2.1862F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.BurnNearby,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayPotion"),
childName = "Pelvis",
localPos = new Vector3(0.45029F, 0.55185F, -0.66869F),
localAngles = new Vector3(359.1402F, 0.1068F, 331.8908F),
localScale = new Vector3(0.13235F, 0.13235F, 0.13235F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.CrippleWard,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEffigy"),
childName = "HandR",
localPos = new Vector3(0.24985F, 0.18655F, 0.04036F),
localAngles = new Vector3(9.95037F, 0.73904F, 274.2693F),
localScale = new Vector3(1.20442F, 1.20442F, 1.20442F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.QuestVolatileBattery,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayBatteryArray"),
childName = "Chest",
localPos = new Vector3(0F, 0F, -0.9528F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(1F, 1F, 1F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.GainArmor,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayElephantFigure"),
childName = "CalfR",
localPos = new Vector3(-0.00001F, 0.36602F, 0.37067F),
localAngles = new Vector3(77.5634F, 0F, 0F),
localScale = new Vector3(2.3571F, 2.3571F, 2.3571F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Recycle,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayRecycler"),
childName = "Chest",
localPos = new Vector3(0F, 0F, -0.726F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.3304F, 0.3304F, 0.3304F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.FireBallDash,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayEgg"),
childName = "HandR",
localPos = new Vector3(-0.08898F, 0.46266F, 0.00003F),
localAngles = new Vector3(270F, 0F, 0F),
localScale = new Vector3(1.94771F, 1.94771F, 1.94771F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Cleanse,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayWaterPack"),
childName = "Chest",
localPos = new Vector3(0F, -0.19435F, -0.82399F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.241F, 0.241F, 0.241F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Tonic,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTonic"),
childName = "Pelvis",
localPos = new Vector3(0.95965F, 0.66098F, 0F),
localAngles = new Vector3(0F, 90F, 0F),
localScale = new Vector3(0.51595F, 0.51595F, 0.51595F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Gateway,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayVase"),
childName = "Pelvis",
localPos = new Vector3(0.17921F, 0.56568F, 0.69266F),
localAngles = new Vector3(339.0288F, 198.4974F, 353.1723F),
localScale = new Vector3(0.44511F, 0.44511F, 0.44511F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Meteor,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayMeteor"),
childName = "Root",
localPos = new Vector3(0F, 3.38107F, -3.00693F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(3.82807F, 3.82807F, 3.82807F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Saw,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplaySawmerang"),
childName = "Root",
localPos = new Vector3(0F, 3.48373F, -2.89059F),
localAngles = new Vector3(312.0747F, 0F, 0F),
localScale = new Vector3(0.7303F, 0.7303F, 0.7303F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Blackhole,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayGravCube"),
childName = "Root",
localPos = new Vector3(0.89018F, 3.43774F, -2.43153F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(4.73209F, 4.73209F, 4.73209F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.Scanner,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayScanner"),
childName = "Pelvis",
localPos = new Vector3(0.45459F, 0.17369F, -0.28327F),
localAngles = new Vector3(311.1287F, 110.0194F, 287.6036F),
localScale = new Vector3(0.16743F, 0.16743F, 0.16743F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.DeathProjectile,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayDeathProjectile"),
childName = "Head",
localPos = new Vector3(0.52617F, -0.26535F, 0.56573F),
localAngles = new Vector3(326.2585F, 27.20248F, 344.0664F),
localScale = new Vector3(0.17002F, 0.17002F, 0.17002F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.LifestealOnHit,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayLifestealOnHit"),
childName = "Head",
localPos = new Vector3(-0.3442F, 0.67587F, -0.34088F),
localAngles = new Vector3(49.8615F, 23.39927F, 36.09192F),
localScale = new Vector3(0.28283F, 0.28283F, 0.28283F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });

            itemDisplayRules.Add(new ItemDisplayRuleSet.KeyAssetRuleGroup
            {
                keyAsset = RoR2Content.Equipment.TeamWarCry,
                displayRuleGroup = new DisplayRuleGroup
                {
                    rules = new ItemDisplayRule[]
                    {
                        new ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = ItemDisplays.LoadDisplay("DisplayTeamWarCry"),
childName = "Pelvis",
localPos = new Vector3(0F, 0.21244F, 0.77546F),
localAngles = new Vector3(0F, 0F, 0F),
localScale = new Vector3(0.17297F, 0.17297F, 0.17297F),
                            limbMask = LimbFlags.None
                        }
                    }
                }
            });
            #endregion

            itemDisplayRuleSet.keyAssetRuleGroups = itemDisplayRules.ToArray();
            //itemDisplayRuleSet.GenerateRuntimeValues();
        }

        private static void Hook()
        {
            //On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

            On.RoR2.CharacterBody.AddBuff_BuffIndex += CharacterBody_AddBuff_BuffIndex;
            On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += CharacterBody_AddTimedBuff_BuffDef_float;
            On.RoR2.UI.MainMenu.MainMenuController.Awake += MainMenuController_Awake;

            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }


        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {

            if (sender.HasBuff(Buffs.armorBuff))
            {
                args.armorAdd += 500f;
            }

            if (sender.HasBuff(Buffs.slowStartBuff))
            {

                args.armorAdd += 20f;
                args.moveSpeedReductionMultAdd += 1f; //movespeed *= 0.5f // 1 + 1 = divide by 2?
                args.attackSpeedMultAdd -= 0.5f; //attackSpeed *= 0.5f;
                args.damageMultAdd -= 0.5f; //damage *= 0.5f;
            }
        }

        private static void MainMenuController_Awake(On.RoR2.UI.MainMenu.MainMenuController.orig_Awake orig, RoR2.UI.MainMenu.MainMenuController self)
        {
            if (!lateInit)
            {
                lateInit = true;
                if (characterPrefab && survivorPrefab)
                {
                    // make sure skins are shared..
                    CharacterModel characterModel = characterPrefab.GetComponentInChildren<CharacterModel>();
                    List<SkinDef> newSkins = new List<SkinDef>();
                    foreach (SkinDef i in survivorPrefab.GetComponentInChildren<ModelSkinController>().skins)
                    {
                        newSkins.Add(CopySkinDef(i, characterModel));
                    }
                    characterPrefab.GetComponentInChildren<ModelSkinController>().skins = newSkins.ToArray();
                    // this sucks.
                }
            }
            orig(self);
        }

        private static SkinDef CopySkinDef(SkinDef skinDef, CharacterModel characterModel)
        {
            CharacterModel.RendererInfo[] rendererInfos = new CharacterModel.RendererInfo[skinDef.rendererInfos.Length];
            SkinDefParams.MeshReplacement[] meshReplacements = new SkinDefParams.MeshReplacement[skinDef.meshReplacements.Length];

            // hardcoded and straight up unholy. it just works.
            if (skinDef.rendererInfos.Length > 0)
            {
                skinDef.rendererInfos.CopyTo(rendererInfos, 0);
                for (int i = 0; i < rendererInfos.Length; i++)
                {
                    rendererInfos[i].renderer = characterModel.mainSkinnedMeshRenderer;
                }
            }

            if (skinDef.meshReplacements.Length > 0)
            {
                skinDef.meshReplacements.CopyTo(meshReplacements, 0);
                for (int i = 0; i < meshReplacements.Length; i++)
                {
                    meshReplacements[i].renderer = characterModel.mainSkinnedMeshRenderer;
                }
            }
            // easier would be asking lui to update that mod to add the skins to the enemy body as well, but it is what it is

            var skinDefInfo = new R2API.SkinDefParamsInfo
            {
                BaseSkins = [],
                GameObjectActivations = [],
                Icon = skinDef.icon,
                MeshReplacements = meshReplacements,
                MinionSkinReplacements = [],
                Name = skinDef.name,
                NameToken = skinDef.nameToken,
                ProjectileGhostReplacements = [],
                RendererInfos = rendererInfos,
                RootObject = characterModel.gameObject,
                UnlockableDef = null
            };

            // this is so fucking bad
            // GOD

            return Skins.CreateNewSkinDef(skinDefInfo);
        }

        private static void CharacterBody_AddTimedBuff_BuffDef_float(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration) {

            if(CheckRegigigasImmune(self, (buffDef != null) ? buffDef.buffIndex : BuffIndex.None)) {
                return;
            }

            orig(self, buffDef, duration);
        }

        private static void CharacterBody_AddBuff_BuffIndex(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType) {

            if (CheckRegigigasImmune(self, buffType)) {
                buffType = BuffIndex.None;
            }

            orig(self, buffType);
        }

        private static bool CheckRegigigasImmune(CharacterBody self, BuffIndex buffIndex) {
            if(self.baseNameToken == RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_NAME") {
                if (buffIndex == RoR2Content.Buffs.Slow50.buffIndex ||
                    buffIndex == RoR2Content.Buffs.Slow60.buffIndex ||
                    buffIndex == RoR2Content.Buffs.Slow80.buffIndex) {
                    return true;
                }
            }
            return false;
        }

        //two getcomponents on takedamage how dare you
        private static void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            bool isHealing = false;

            if (self)
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    if (attackerBody)
                    {
                        if (attackerBody.baseNameToken == RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_NAME")
                        {
                            /*if (damageInfo.damageType.HasFlag(DamageType.BlightOnHit))
                            {
                                damageInfo.damageType = DamageType.Generic;
                                isHealing = true;
                            }*/
                        }
                    }
                }
            }

            orig(self, damageInfo);

            if (isHealing && !damageInfo.rejected)
            {
                damageInfo.attacker.GetComponent<HealthComponent>().Heal(damageInfo.damage * 0.5f, default(ProcChainMask));
            }
        }

        private static void GlobalEventManager_onCharacterDeathGlobal(DamageReport damageReport)
        {
            CharacterMaster victim = damageReport.victimMaster;
            if (victim)
            {
                // make shiny regi drop irradiant pearl
                if (damageReport.victimBodyIndex == BodyCatalog.FindBodyIndex("RegigigasBody")) {
                    Components.RegigigasDropComponent dropComponent = victim.GetComponent<Components.RegigigasDropComponent>();
                    if (dropComponent) {
                        PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(dropComponent.itemDropDef.itemIndex), damageReport.victimBody.corePosition, Vector3.up * 20f);
                    }
                }

                // slow start 10 kills
                if (damageReport.attackerBody)
                {
                    if (damageReport.attackerBody.HasBuff(Modules.Buffs.slowStartBuff))
                    {
                        Components.SlowStartController slowStart = damageReport.attacker.GetComponent<Components.SlowStartController>();
                        if (slowStart)
                        {
                            //slowStart.GrantKill();

                            SlowStartOrb slowStartOrb = new SlowStartOrb();
                            slowStartOrb.origin = damageReport.victimBody.transform.position;
                            slowStartOrb.target = Util.FindBodyMainHurtBox(damageReport.attackerBody);
                            OrbManager.instance.AddOrb(slowStartOrb);
                        }
                    }
                }
            }
        }
    }
}