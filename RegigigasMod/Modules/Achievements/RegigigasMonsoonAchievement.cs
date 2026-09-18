using RoR2;
using RoR2.Achievements;
using UnityEngine;

namespace RegigigasMod.Modules.Achievements
{
    //string identifier, string unlockableRewardIdentifier, string prerequisiteAchievementIdentifier, uint lunarCoinReward, Type serverTrackerType = null
    //automatically creates language tokens "ACHIEVEMENT_{identifier.ToUpper()}_NAME" and "ACHIEVEMENT_{identifier.ToUpper()}_DESCRIPTION" 
    [RegisterAchievement(IDENTIFIER, UNLOCKABLE_IDENTIFIER, null, 10, null)]
    internal class RegigigasMasteryAchievement : BaseAchievement
    {
        public const string IDENTIFIER = "ROB_REGIGIGAS_BODY_MONSOONUNLOCKABLE_ACHIEVEMENT_ID";
        public const string UNLOCKABLE_IDENTIFIER = "ROB_REGIGIGAS_BODY_MONSOONUNLOCKABLE_REWARD_ID";

        public static Sprite Sprite => Modules.RegiAssets.secondaryAssetBundle.LoadAsset<Sprite>("texMasterySkinIcon");

        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("RegigigasPlayerBody");

        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();

            Run.onClientGameOverGlobal += this.Run_OnClientGameOverGlobal;
        }

        public override void OnBodyRequirementBroken()
        {
            base.OnBodyRequirementBroken();

            Run.onClientGameOverGlobal -= this.Run_OnClientGameOverGlobal;
        }

        protected virtual void Run_OnClientGameOverGlobal(Run run, RunReport runReport)
        {
            if (base.meetsBodyRequirement && runReport?.gameEnding && runReport.gameEnding.isWin)
            {
                var difficultyIndex = runReport.ruleBook.FindDifficulty();
                var difficultyDef = DifficultyCatalog.GetDifficultyDef(difficultyIndex);
                if (difficultyDef != null)
                {
                    var isDifficulty = difficultyDef.countsAsHardMode || difficultyDef.scalingValue >= 3f;
                    var isInferno = difficultyDef.nameToken == "INFERNO_NAME";
                    var isEclipse = difficultyIndex <= DifficultyIndex.Eclipse8 && difficultyIndex >= DifficultyIndex.Hard;

                    if (isDifficulty || isInferno || isEclipse)
                        Grant();
                }
            }
        }
    }
}