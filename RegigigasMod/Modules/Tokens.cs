using R2API;
using RegigigasMod.Modules.Achievements;
using System;

namespace RegigigasMod.Modules
{
    internal static class Tokens
    {
        internal static void AddTokens()
        {
            string prefix = RegigigasPlugin.developerPrefix + "_REGIGIGAS_BODY_";

            string desc = "Regigigas is a hulking beast that requires time and support to reach its full potential. If allowed to awaken, it dominates the battlefield with ease.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine; ;

            desc = desc + "< ! > Use Drain Punch to survive until Slow Start is mitigated. Having allies protect you later into a run is essential!" + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > Ancient Power is essential for dealing with flying enemies, and a good form of constant damage. Using Drain Punch to sustain yourself while doing this is key." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > Heavy Slam does more damage the faster you fall, so be sure to jump as high as you can." + Environment.NewLine + Environment.NewLine;
            desc = desc + "< ! > Giga Impact has a huge vacuum effect, so use that to maximize your damage output." + Environment.NewLine + Environment.NewLine;

            string outro = "..and so it left, leaving irreparable damage in its wake.";
            string outroFailure = "..and so it vanished, returning to its eternal slumber.";

            string lore = "There is an enduring legend that states this Pokémon towed continents with ropes.";
            string lore2 = "There is an enduring legend that states this golem towed continents with ropes.";

            LanguageAPI.Add(prefix + "NAME", "Regigigas");
            LanguageAPI.Add(prefix + "NAME2", "Stone Gigas");
            LanguageAPI.Add(prefix + "DESCRIPTION", desc);
            LanguageAPI.Add(prefix + "SUBTITLE", "Weary Colossus");
            LanguageAPI.Add(prefix + "LORE", lore);
            LanguageAPI.Add(prefix + "LORE2", lore2);
            LanguageAPI.Add(prefix + "OUTRO_FLAVOR", outro);
            LanguageAPI.Add(prefix + "OUTRO_FAILURE", outroFailure);

            #region Skins
            LanguageAPI.Add(prefix + "DEFAULT_SKIN_NAME", "Default");

            if (Modules.Config.loreFriendly) LanguageAPI.Add(prefix + "MONSOON_SKIN_NAME", "Gold");
            else LanguageAPI.Add(prefix + "MONSOON_SKIN_NAME", "Shiny");

            LanguageAPI.Add(prefix + "JUGGERNAUT_SKIN_NAME", "Juggernaut");

            LanguageAPI.Add(prefix + "BOWSER_SKIN_NAME", "King");
            #endregion

            #region Passive
            LanguageAPI.Add(prefix + "PASSIVE_NAME", "Slow Start");
            LanguageAPI.Add(prefix + "PASSIVE_DESCRIPTION", $"<style=cIsHealth>Stats are halved</style> upon spawning. Defeating <style=cIsUtility>10 enemies</style> will restore Regigigas to <style=cIsDamage>full power</style>.");
            LanguageAPI.Add(prefix + "PASSIVE_DESCRIPTION2", $"<style=cIsHealth>Stats are halved</style> upon spawning. Defeating <style=cIsUtility>10 enemies</style> will restore the Stone Juggernaut to <style=cIsDamage>full power</style>.");
            #endregion

            #region Primary
            LanguageAPI.Add(prefix + "PRIMARY_GRAB_NAME", "Crush Grip");
            LanguageAPI.Add(prefix + "PRIMARY_GRAB_DESCRIPTION", $"Grab a nearby enemy and crush them for <style=cIsHealth>50% of their max health</style>, then throw them away.");

            LanguageAPI.Add(prefix + "PRIMARY_PUNCH_NAME", "Brick Break");
            LanguageAPI.Add(prefix + "PRIMARY_PUNCH_DESCRIPTION", $"Punch for <style=cIsDamage>{SkillStates.Regigigas.PunchCombo.damageCoefficientOverride * 100f}% damage</style>.");

            LanguageAPI.Add(prefix + "PRIMARY_MACHPUNCH_NAME", "Mach Punch");
            LanguageAPI.Add(prefix + "PRIMARY_MACHPUNCH_DESCRIPTION", $"Punch really fast for <style=cIsDamage>{SkillStates.Regigigas.MachPunch.damageCoefficientOverride * 100f}% damage</style>.");

            LanguageAPI.Add(prefix + "PRIMARY_DRAINPUNCH_NAME", "Drain Punch");
            LanguageAPI.Add(prefix + "PRIMARY_DRAINPUNCH_DESCRIPTION", $"Punch for <style=cIsDamage>{SkillStates.Regigigas.DrainPunch.damageCoefficientOverride * 100f}% damage</style>, <style=cIsHealing>healing for 25% of damage dealt</style>.");

            LanguageAPI.Add(prefix + "PRIMARY_ICEPUNCH_NAME", "Ice Punch");
            LanguageAPI.Add(prefix + "PRIMARY_ICEPUNCH_DESCRIPTION", $"Punch for <style=cIsDamage>{SkillStates.Regigigas.IcePunch.damageCoefficientOverride * 100f}% damage</style>, <style=cIsUtility>freezing enemies hit</style>.");
            #endregion

            #region Secondary
            LanguageAPI.Add(prefix + "SECONDARY_EARTHQUAKE_NAME", "Earth Power");
            LanguageAPI.Add(prefix + "SECONDARY_EARTHQUAKE_DESCRIPTION", $"Stomp with all your might, summoning 5 pillars of <style=cIsDamage>energy</style> that deal <style=cIsDamage>{100f * SkillStates.Regigigas.Stomp.damageCoefficient}% damage</style>.");

            LanguageAPI.Add(prefix + "SECONDARY_ANCIENTPOWER_NAME", "Ancient Power");
            LanguageAPI.Add(prefix + "SECONDARY_ANCIENTPOWER_DESCRIPTION", $"Charge up a barrage of rocks for <style=cIsDamage>{100f * SkillStates.Regigigas.FireAncientPower.damageCoefficient}% damage</style> each. Costs <style=cIsHealth>10% max health</style> for each rock if out of stock.");
            #endregion

            #region Utility
            LanguageAPI.Add(prefix + "UTILITY_REVENGE_NAME", "Revenge");
            LanguageAPI.Add(prefix + "UTILITY_REVENGE_DESCRIPTION", "Channel for 8 seconds, gaining <style=cIsUtility>500 armor</style>. Then unleash <style=cIsDamage>3x all damage received during this time</style>.");
            #endregion

            #region Special
            LanguageAPI.Add(prefix + "SPECIAL_SLAM_NAME", "Heavy Slam");
            LanguageAPI.Add(prefix + "SPECIAL_SLAM_DESCRIPTION", $"Leap high into the sky, dealing <style=cIsDamage>{100f * SkillStates.Regigigas.Bounce.minDamageCoefficient}-{100f * SkillStates.Regigigas.Bounce.maxDamageCoefficient}% damage</style> around you in an explosive landing.");

            LanguageAPI.Add(prefix + "SPECIAL_IMPACT_NAME", "Giga Impact");
            LanguageAPI.Add(prefix + "SPECIAL_IMPACT_DESCRIPTION", $"Cloak yourself in <style=cIsDamage>flame</style> and rush forward, <style=cIsUtility>dragging in nearby enemies</style>, then explode for <style=cIsDamage>{100f * SkillStates.Regigigas.GigaImpactOld.blastAttackDamageCoefficient}% damage</style>.");
            #endregion

            #region Achievements

            // unlockables tied to achievements dont use tokens
            const string nameFormat = "ACHIEVEMENT_{0}_NAME";
            const string descFormat = "ACHIEVEMENT_{0}_DESCRIPTION";

            if (Modules.Config.loreFriendly2)
            {
                LanguageAPI.Add(string.Format(nameFormat, RegigigasMasteryAchievement.IDENTIFIER), "Stone Juggernaut: Mastery");
                LanguageAPI.Add(string.Format(descFormat, RegigigasMasteryAchievement.IDENTIFIER), "As Stone Juggernaut, beat the game or obliterate on Monsoon.");
            }
            else
            {
                LanguageAPI.Add(string.Format(nameFormat, RegigigasMasteryAchievement.IDENTIFIER), "Regigigas: Mastery");
                LanguageAPI.Add(string.Format(descFormat, RegigigasMasteryAchievement.IDENTIFIER), "As Regigigas, beat the game or obliterate on Monsoon.");
            }
            #endregion

            #region Misc
            LanguageAPI.Add(prefix + "SLOW_START", "SLOW START...");
            LanguageAPI.Add(prefix + "SLOW_START_RELEASED", "FULL POWER!");
            #endregion
        }
    }
}