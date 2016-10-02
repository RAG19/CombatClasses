﻿/*
* CombatClass for TheNoobBot
* Credit : Vesper, Neo2003, Dreadlocks, Ryuichiro
* Thanks you !
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using nManager.Helpful;
using nManager.Wow;
using nManager.Wow.Bot.Tasks;
using nManager.Wow.Class;
using nManager.Wow.Enums;
using nManager.Wow.Helpers;
using nManager.Wow.ObjectManager;
using Timer = nManager.Helpful.Timer;

// ReSharper disable EmptyGeneralCatchClause
// ReSharper disable ObjectCreationAsStatement

public class Main : ICombatClass
{
    internal static float InternalRange = 5.0f;
    internal static float InternalAggroRange = 5.0f;
    internal static bool InternalLoop = true;
    internal static Spell InternalLightHealingSpell;

    #region ICombatClass Members

    public float AggroRange
    {
        get { return InternalAggroRange; }
    }

    public Spell LightHealingSpell
    {
        get { return InternalLightHealingSpell; }
        set { InternalLightHealingSpell = value; }
    }

    public float Range
    {
        get { return InternalRange; }
        set { InternalRange = value; }
    }

    public void Initialize()
    {
        Initialize(false);
    }

    public void Dispose()
    {
        Logging.WriteFight("Combat system stopped.");
        InternalLoop = false;
    }

    public void ShowConfiguration()
    {
        Directory.CreateDirectory(Application.StartupPath + "\\CombatClasses\\Settings\\");
        Initialize(true);
    }

    public void ResetConfiguration()
    {
        Directory.CreateDirectory(Application.StartupPath + "\\CombatClasses\\Settings\\");
        Initialize(true, true);
    }

    #endregion

    public void Initialize(bool configOnly, bool resetSettings = false)
    {
        try
        {
            if (!InternalLoop)
                InternalLoop = true;
            Logging.WriteFight("Loading combat system.");
            WoWSpecialization wowSpecialization = ObjectManager.Me.WowSpecialization(true);
            switch (ObjectManager.Me.WowClass)
            {
                #region Warlock Specialisation checking

                case WoWClass.Warlock:

                if (wowSpecialization == WoWSpecialization.WarlockAffliction || wowSpecialization == WoWSpecialization.None)
                {
                    if (configOnly)
                    {
                        string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warlock_Affliction.xml";
                        var currentSetting = new WarlockAffliction.WarlockAfflictionSettings();
                        if (File.Exists(currentSettingsFile) && !resetSettings)
                        {
                            currentSetting = Settings.Load<WarlockAffliction.WarlockAfflictionSettings>(currentSettingsFile);
                        }
                        currentSetting.ToForm();
                        currentSetting.Save(currentSettingsFile);
                    }
                    else
                    {
                        Logging.WriteFight("Loading Warlock Affliction Combat class...");
                        InternalRange = 40.0f;
                        EquipmentAndStats.SetPlayerSpe(WoWSpecialization.WarlockAffliction);
                        new WarlockAffliction();
                    }
                    break;
                }
                if (wowSpecialization == WoWSpecialization.WarlockDemonology)
                {
                    if (configOnly)
                    {
                        string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warlock_Demonology.xml";
                        var currentSetting = new WarlockDemonology.WarlockDemonologySettings();
                        if (File.Exists(currentSettingsFile) && !resetSettings)
                        {
                            currentSetting = Settings.Load<WarlockDemonology.WarlockDemonologySettings>(currentSettingsFile);
                        }
                        currentSetting.ToForm();
                        currentSetting.Save(currentSettingsFile);
                    }
                    else
                    {
                        Logging.WriteFight("Loading Warlock Demonology Combat class...");
                        InternalRange = 40.0f;
                        EquipmentAndStats.SetPlayerSpe(WoWSpecialization.WarlockDemonology);
                        new WarlockDemonology();
                    }
                    break;
                }
                if (wowSpecialization == WoWSpecialization.WarlockDestruction)
                {
                    if (configOnly)
                    {
                        string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warlock_Destruction.xml";
                        var currentSetting = new WarlockDestruction.WarlockDestructionSettings();
                        if (File.Exists(currentSettingsFile) && !resetSettings)
                        {
                            currentSetting = Settings.Load<WarlockDestruction.WarlockDestructionSettings>(currentSettingsFile);
                        }
                        currentSetting.ToForm();
                        currentSetting.Save(currentSettingsFile);
                    }
                    else
                    {
                        Logging.WriteFight("Loading Warlock Destruction Combat class...");
                        InternalRange = 40.0f;
                        EquipmentAndStats.SetPlayerSpe(WoWSpecialization.WarlockDestruction);
                        new WarlockDestruction();
                    }
                    break;
                }
                break;

                #endregion

                default:
                Dispose();
                break;
            }
        }
        catch
        {
        }
        Logging.WriteFight("Combat system stopped.");
    }

    internal static void DumpCurrentSettings<T>(object mySettings)
    {
        mySettings = mySettings is T ? (T)mySettings : default(T);
        BindingFlags bindingFlags = BindingFlags.Public |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Instance |
                                    BindingFlags.Static;
        for (int i = 0; i < mySettings.GetType().GetFields(bindingFlags).Length - 1; i++)
        {
            FieldInfo field = mySettings.GetType().GetFields(bindingFlags)[i];
            Logging.WriteDebug(field.Name + " = " + field.GetValue(mySettings));
        }

        // Last field is intentionnally ommited because it's a backing field.
    }
}

#region Warlock

public class WarlockAffliction
{
    private static WarlockAfflictionSettings MySettings = WarlockAfflictionSettings.GetSettings();

    #region General Timers & Variables

    private readonly WoWItem _firstTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET);
    private readonly WoWItem _secondTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET, 2);

    private bool CombatMode = true;

    private Timer DefensiveTimer = new Timer(0);
    private Timer StunTimer = new Timer(0);

    #endregion

    #region Professions & Racial

    //private readonly Spell ArcaneTorrent = new Spell("Arcane Torrent"); //No GCD
    private readonly Spell Berserking = new Spell("Berserking"); //No GCD
    private readonly Spell BloodFury = new Spell("Blood Fury"); //No GCD
    private readonly Spell Darkflight = new Spell("Darkflight"); //No GCD
    private readonly Spell GiftoftheNaaru = new Spell("Gift of the Naaru"); //No GCD
    private readonly Spell Stoneform = new Spell("Stoneform"); //No GCD
    private readonly Spell WarStomp = new Spell("War Stomp"); //No GCD

    #endregion

    #region Talents

    private readonly Spell Contagion = new Spell("Contagion");
    private readonly Spell GrimoireofSupremacy = new Spell("Grimoire of Supremacy");

    #endregion

    #region Buffs

    private readonly Spell DemonicPower = new Spell("Demonic Power");
    private readonly Spell TormentedSouls = new Spell("Tormented Souls");

    #endregion

    #region Dots

    private readonly Spell Agony = new Spell("Agony");
    private readonly Spell AgonyDot = new Spell(980);
    private readonly Spell Corruption = new Spell("Corruption");
    private readonly Spell CorruptionDot = new Spell(146739);
    private readonly Spell PhantomSingularity = new Spell("Phantom Singularity");
    private readonly Spell SeedofCorruption = new Spell("Seed of Corruption");
    private readonly Spell SiphonLife = new Spell("Siphon Life");
    private readonly Spell SiphonLifeDot = new Spell(63106);
    private readonly Spell UnstableAffliction = new Spell("Unstable Affliction");
    private readonly Spell UnstableAfflictionDot = new Spell(30108);
    private bool StackUnstableAffliction = true;

    #endregion

    #region Artifact Spells

    private readonly Spell ReapSouls = new Spell("Reap Souls");

    #endregion

    #region Offensive Spells

    private readonly Spell DrainLife = new Spell("Drain Life");
    private readonly Spell GrimoireofSacrifice = new Spell("Grimoire of Sacrifice");
    private readonly Spell Haunt = new Spell("Haunt");
    private readonly Spell ManaTap = new Spell("Mana Tap");

    #endregion

    #region Offensive Cooldowns

    private readonly Spell GrimoireImp = new Spell("Grimoire: Imp");
    private readonly Spell GrimoireFelguard = new Spell("Grimoire: Felguard");
    private readonly Spell GrimoireFelhunter = new Spell("Grimoire: Felhunter");
    private readonly Spell GrimoireSuccubus = new Spell("Grimoire: Succubus");
    private readonly Spell GrimoireVoidwalker = new Spell("Grimoire: Voidwalker");
    private readonly Spell GrimoireofService = new Spell("Grimoire of Service");
    private readonly Spell SoulHarvest = new Spell("Soul Harvest"); //No GCD
    private readonly Spell SummonDoomguard = new Spell("Summon Doomguard");
    private readonly Spell SummonImp = new Spell("Summon Imp");
    private readonly Spell SummonInfernal = new Spell("Summon Infernal");

    #endregion

    #region Defensive Spells

    private readonly Spell DarkPact = new Spell("Dark Pact"); //No GCD
    private readonly Spell UnendingResolve = new Spell("Unending Resolve"); //No GCD

    #endregion

    #region Utility Spells

    private readonly Spell BurningRush = new Spell("Burning Rush");
    private readonly Spell DemonicCircle = new Spell("Demonic Circle");
    private readonly Spell DemonicGateway = new Spell("Demonic Gateway");
    private readonly Spell Fear = new Spell("Fear");
    private readonly Spell CreateHealthstone = new Spell("Create Healthstone");
    private Timer HealthstoneTimer = new Timer(0);
    private readonly Spell HowlofTerror = new Spell("Howl of Terror"); //No GCD
    private readonly Spell LifeTap = new Spell("Life Tap");
    private readonly Spell SoulEffigy = new Spell("Soul Effigy");
    private WoWUnit SummonedSoulEffigy = new WoWUnit(0);
    private readonly Spell Soulstone = new Spell("Soulstone");

    #endregion

    /*#region PvP Talents
    
    private readonly Spell Soulburn = new Spell("Soulburn");
    private readonly Spell SoulSwap = new Spell("Soul Swap");

    #endregion*/

    public WarlockAffliction()
    {
        Main.InternalRange = 39f;
        Main.InternalAggroRange = 39f;
        Main.InternalLightHealingSpell = null;
        MySettings = WarlockAfflictionSettings.GetSettings();
        Main.DumpCurrentSettings<WarlockAfflictionSettings>(MySettings);
        UInt128 lastTarget = 0;

        while (Main.InternalLoop)
        {
            try
            {
                if (!ObjectManager.Me.IsDeadMe)
                {
                    if (!ObjectManager.Me.IsMounted)
                    {
                        if (Fight.InFight && ObjectManager.Me.Target > 0)
                        {
                            if (ObjectManager.Me.Target != lastTarget)
                                lastTarget = ObjectManager.Me.Target;

                            if (CombatClass.InSpellRange(ObjectManager.Target, 0, 40))
                                Combat();
                            else if (!ObjectManager.Me.IsCast)
                                Patrolling();
                        }
                        else if (!ObjectManager.Me.IsCast)
                            Patrolling();
                    }
                }
                else
                    Thread.Sleep(500);
            }
            catch
            {
            }
            Thread.Sleep(100);
        }
    }

    // For Movement Spells (always return after Casting)
    private void Patrolling()
    {
        //Log
        if (CombatMode)
        {
            Logging.WriteFight("Patrolling:");
            CombatMode = false;
        }

        if (ObjectManager.Me.GetMove)
        {
            //Movement Buffs
            if (!Darkflight.HaveBuff && !BurningRush.HaveBuff) // doesn't stack
            {
                if (MySettings.UseDarkflight && Darkflight.IsSpellUsable)
                {
                    Darkflight.Cast();
                    return;
                }
                if (ObjectManager.Me.HealthPercent > MySettings.StartBurningRushAbovePercentage && BurningRush.IsSpellUsable)
                {
                    BurningRush.Cast();
                    return;
                }
            }
            if (ObjectManager.Me.HealthPercent < MySettings.StopBurningRushBelowPercentage &&
                BurningRush.IsSpellUsable && BurningRush.HaveBuff)
            {
                BurningRush.Cast();
                return;
            }
        }
        else
        {
            if (BurningRush.IsSpellUsable && BurningRush.HaveBuff)
            {
                BurningRush.Cast();
                return;
            }
            //Self Heal for Damage Dealer
            if (nManager.Products.Products.ProductName == "Damage Dealer" && Main.InternalLightHealingSpell.IsSpellUsable &&
                ObjectManager.Me.HealthPercent < 90 && ObjectManager.Target.Guid == ObjectManager.Me.Guid)
            {
                Main.InternalLightHealingSpell.CastOnSelf();
                return;
            }
            //Create Healthstone
            if (MySettings.UseCreateHealthstone && ItemsManager.GetItemCount(5512) == 0 &&
                Usefuls.GetContainerNumFreeSlots > 0 && CreateHealthstone.IsSpellUsable)
            {
                Logging.WriteFight("Create Healthstone");
                CreateHealthstone.Cast();
                /*Others.SafeSleep(500);
                while (ObjectManager.Me.IsCast)
                    Others.SafeSleep(200);*/
            }
            Pet();
        }
    }

    // For Summoning permanent Pets (always return after Casting)
    private bool Pet()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (GrimoireofSupremacy.HaveBuff)
            {
                if (ObjectManager.Pet.Health == 0 || ObjectManager.Pet.Guid == 0 || !ObjectManager.Pet.IsValid)
                {
                    //Summon Infernal
                    if (MySettings.UseSummonInfernal && SummonInfernal.IsSpellUsable &&
                        SummonInfernal.IsHostileDistanceGood)
                    {
                        SummonInfernal.CastAtPosition(ObjectManager.Target.Position);
                        return true;
                    }
                    //Summon Doomguard
                    if (MySettings.UseSummonDoomguard && SummonDoomguard.IsSpellUsable &&
                        SummonDoomguard.IsHostileDistanceGood)
                    {
                        SummonDoomguard.CastAtPosition(ObjectManager.Target.Position);
                        return true;
                    }
                }
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For general InFight Behavior (only touch if you want to add a new method like PetManagement())
    private void Combat()
    {
        //Log
        if (!CombatMode)
        {
            Logging.WriteFight("Combat:");
            CombatMode = true;
        }
        Healing();
        if (Defensive() || Pet() || Offensive())
            return;
        Rotation();
    }

    // For Self-Healing Spells (always return after Casting)
    private bool Healing()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            //Gift of the Naaru
            if (ObjectManager.Me.HealthPercent < MySettings.UseGiftoftheNaaruBelowPercentage && GiftoftheNaaru.IsSpellUsable)
            {
                GiftoftheNaaru.CastOnSelf();
                return true;
            }
            //Healthstone
            if (ObjectManager.Me.HealthPercent < MySettings.UseHealthstoneBelowPercentage &&
                HealthstoneTimer.IsReady && ItemsManager.GetItemCount(5512) > 0)
            {
                Logging.WriteFight("Use Healthstone.");
                ItemsManager.UseItem("Healthstone");
                HealthstoneTimer = new Timer(1000 * 60);
                return true;
            }
            //Channel Drain Life & Drain Soul
            if (ObjectManager.Me.HealthPercent < MySettings.UseDrainLife_SoulBelowPercentage && DrainLife.IsSpellUsable &&
                !ObjectManager.Me.GetMove && DrainLife.IsHostileDistanceGood)
            {
                DrainLife.Cast();
                return true;
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For Defensive Buffs and Livesavers (always return after Casting)
    private bool Defensive()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (StunTimer.IsReady && ((DefensiveTimer.IsReady && !DarkPact.HaveBuff) || ObjectManager.Me.HealthPercent < 20))
            {
                //Stun
                if (ObjectManager.Target.IsStunnable)
                {
                    if (ObjectManager.Me.HealthPercent < MySettings.UseWarStompBelowPercentage && WarStomp.IsSpellUsable)
                    {
                        WarStomp.Cast();
                        StunTimer = new Timer(1000 * 2.5);
                        return true;
                    }
                }
                //Mitigate Damage
                if (ObjectManager.Me.HealthPercent < MySettings.UseStoneformBelowPercentage && Stoneform.IsSpellUsable)
                {
                    Stoneform.Cast();
                    DefensiveTimer = new Timer(1000 * 8);
                    return true;
                }
                if (ObjectManager.Me.HealthPercent < MySettings.UseDarkPactBelowPercentage && DarkPact.IsSpellUsable)
                {
                    DarkPact.Cast();
                    return true;
                }
            }
            //Mitigate Damage in Emergency Situations
            if (ObjectManager.Me.HealthPercent < MySettings.UseUnendingResolveBelowPercentage && UnendingResolve.IsSpellUsable)
            {
                UnendingResolve.Cast();
                return true;
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For Offensive Buffs (only return if a Cast triggered Global Cooldown)
    private bool Offensive()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (MySettings.UseTrinketOne && !ItemsManager.IsItemOnCooldown(_firstTrinket.Entry) && ItemsManager.IsItemUsable(_firstTrinket.Entry))
            {
                ItemsManager.UseItem(_firstTrinket.Name);
                Logging.WriteFight("Use First Trinket Slot");
            }
            if (MySettings.UseTrinketTwo && !ItemsManager.IsItemOnCooldown(_secondTrinket.Entry) && ItemsManager.IsItemUsable(_secondTrinket.Entry))
            {
                ItemsManager.UseItem(_secondTrinket.Name);
                Logging.WriteFight("Use Second Trinket Slot");
            }
            if (MySettings.UseBerserking && Berserking.IsSpellUsable)
            {
                Berserking.Cast();
            }
            if (MySettings.UseBloodFury && BloodFury.IsSpellUsable)
            {
                BloodFury.Cast();
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For the Ability Priority Logic
    private void Rotation()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            //Start depends on Contagion Talent
            if (Contagion.HaveBuff)
            {
                //Maintain Unstable Affliction when you have the Contagion Talent
                if (MySettings.UseUnstableAffliction && UnstableAffliction.IsSpellUsable &&
                    !ObjectManager.Me.GetMove && UnstableAffliction.IsHostileDistanceGood &&
                    ObjectManager.Target.AuraTimeLeft(UnstableAfflictionDot.Id, true) <= 1000 * 8 / 3)
                {
                    UnstableAffliction.Cast();
                    return;
                }
            }
            else
            {
                //Stack Unstable Affliction when you have the Contagion Talent
                if (MySettings.UseUnstableAffliction && UnstableAffliction.IsSpellUsable &&
                    !ObjectManager.Me.GetMove && UnstableAffliction.IsHostileDistanceGood &&
                    //you are capping Soul Shards or want to continue stacking
                    (ObjectManager.Me.SoulShards == ObjectManager.Me.MaxSoulShards || StackUnstableAffliction))
                {
                    //Apply Mana Tap
                    if (ObjectManager.Me.ManaPercentage >= MySettings.UseManaTapAbovePercentage && ManaTap.IsSpellUsable)
                    {
                        ManaTap.Cast();
                        return;
                    }
                    //Apply Reap Souls when
                    if (MySettings.UseReapSouls && ReapSouls.IsSpellUsable && !ReapSouls.HaveBuff &&
                        //you start to stack Unstable Affliction and you have 2 or more Tormented Souls
                        !StackUnstableAffliction && TormentedSouls.BuffStack >= 2)
                    {
                        ReapSouls.Cast();
                    }
                    //Apply Soul Harvest
                    if (MySettings.UseSoulHarvest && SoulHarvest.IsSpellUsable)
                    {
                        SoulHarvest.Cast();
                    }

                    StackUnstableAffliction = true;

                    UnstableAffliction.Cast();
                    return;
                }
                if (ObjectManager.Me.SoulShards == 0)
                    StackUnstableAffliction = false;
                //Summon Doomguard when you don't have the Contagion Talent
                if (MySettings.UseSummonDoomguard && SummonDoomguard.IsSpellUsable && SummonDoomguard.IsHostileDistanceGood)
                {
                    SummonDoomguard.CastAtPosition(ObjectManager.Target.Position);
                    return;
                }
            }

            //2. Dots on Target
            //Apply Phantom Singularity when
            if (MySettings.UsePhantomSingularity && PhantomSingularity.IsSpellUsable && PhantomSingularity.IsHostileDistanceGood &&
                //you have 3 or more targets
                ObjectManager.Target.GetUnitInSpellRange(25f) >= 3)
            {
                PhantomSingularity.Cast();
                return;
            }
            //Apply Seed of Corruption when
            if (MySettings.UseSeedofCorruption && SeedofCorruption.IsSpellUsable &&
                !ObjectManager.Me.GetMove && SeedofCorruption.IsHostileDistanceGood &&
                !SeedofCorruption.TargetHaveBuffFromMe &&
                //you have 3 or more targets
                ObjectManager.Target.GetUnitInSpellRange(10f) >= 3)
            {
                SeedofCorruption.Cast();
                return;
            }
            //Maintain Agony
            if (MySettings.UseAgony && Agony.IsSpellUsable && Agony.IsHostileDistanceGood &&
                ObjectManager.Target.AuraTimeLeft(AgonyDot.Id, true) <= 1000 * 18 / 3)
            {
                Agony.Cast();
                return;
            }
            //Maintain Corruption
            if (MySettings.UseCorruption && Corruption.IsSpellUsable && Corruption.IsHostileDistanceGood &&
                ObjectManager.Target.AuraTimeLeft(CorruptionDot.Id, true) <= 1000 * 14 / 3)
            {
                Corruption.Cast();
                return;
            }
            //Maintain Siphon Life
            if (MySettings.UseSiphonLife && SiphonLife.IsSpellUsable &&
                !ObjectManager.Me.GetMove && SiphonLife.IsHostileDistanceGood &&
                ObjectManager.Target.AuraTimeLeft(SiphonLifeDot.Id, true) <= 1000 * 10 / 3)
            {
                SiphonLife.Cast();
                return;
            }

            //3. Use Soul Effigy
            if (MySettings.UseSoulEffigy && SoulEffigy.IsSpellUsable)
            {
                //Summon Soul Effigy
                if (!ObjectManager.Me.GetMove && SoulEffigy.IsHostileDistanceGood && !SummonedSoulEffigy.IsValid)
                {
                    SoulEffigy.Cast();

                    Others.SafeSleep(1500);
                    while (ObjectManager.Me.IsCast)
                        Others.SafeSleep(20);

                    //Testing: save SummonedSoulEffigy
                    SummonedSoulEffigy = ObjectManager.GetWoWUnitSummonedOrCreatedByMeAndFighting();
                    return;
                }
                //Maintain Agony
                if (MySettings.UseAgony && Agony.IsSpellUsable &&
                    SoulEffigy.CreatedBySpellInRange((uint)Agony.MaxRangeHostile) &&
                    SummonedSoulEffigy.AuraTimeLeft(AgonyDot.Id, true) <= 1000 * 18 / 3)
                {
                    Lua.RunMacroText("/target " + SoulEffigy.NameInGame);
                    Agony.Cast();
                    Lua.RunMacroText("/targetlasttarget");
                    return;
                }
                //Maintain Corruption
                if (MySettings.UseCorruption && Corruption.IsSpellUsable &&
                    SoulEffigy.CreatedBySpellInRange((uint)Corruption.MaxRangeHostile) &&
                    SummonedSoulEffigy.AuraTimeLeft(CorruptionDot.Id, true) <= 1000 * 14 / 3)
                {
                    Lua.RunMacroText("/target " + SoulEffigy.NameInGame);
                    Corruption.Cast();
                    Lua.RunMacroText("/targetlasttarget");
                    return;
                }
                //Maintain Siphon Life
                if (MySettings.UseSiphonLife && SiphonLife.IsSpellUsable && !ObjectManager.Me.GetMove &&
                    SoulEffigy.CreatedBySpellInRange((uint)SiphonLife.MaxRangeHostile) &&
                    SummonedSoulEffigy.AuraTimeLeft(SiphonLifeDot.Id, true) <= 1000 * 10 / 3)
                {
                    Lua.RunMacroText("/target " + SoulEffigy.NameInGame);
                    SiphonLife.Cast();
                    Lua.RunMacroText("/targetlasttarget");
                    return;
                }
            }

            //4. Cooldowns 
            //when you have the Contagion Talent
            if (Contagion.HaveBuff)
            {
                //7. Apply Mana Tap
                if (ObjectManager.Me.ManaPercentage >= MySettings.UseManaTapAbovePercentage &&
                    ManaTap.IsSpellUsable && !ManaTap.HaveBuff)
                {
                    ManaTap.Cast();
                    return;
                }
                //7. Apply Soul Harvest 
                if (MySettings.UseSoulHarvest && SoulHarvest.IsSpellUsable)
                {
                    SoulHarvest.Cast();
                }
            }
            //Apply Reap Souls when
            if (MySettings.UseReapSouls && ReapSouls.IsSpellUsable && !ReapSouls.HaveBuff &&
                //you have 12 or more Tormented Souls
                TormentedSouls.BuffStack >= 12)
            {
                ReapSouls.Cast();
            }
            //Summon Infernal when
            if (MySettings.UseSummonInfernal && SummonInfernal.IsSpellUsable && SummonInfernal.IsHostileDistanceGood &&
                //you have 4 or more targets.
                ObjectManager.Target.GetUnitInSpellRange(10f) >= 4)
            {
                SummonInfernal.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            //Summon Doomguard
            if (MySettings.UseSummonDoomguard && SummonDoomguard.IsSpellUsable && SummonDoomguard.IsHostileDistanceGood)
            {
                SummonDoomguard.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            //Apply Grimoire of Sacrifice
            if (MySettings.UseGrimoireofSacrifice && GrimoireofSacrifice.IsSpellUsable)
            {
                if (!DemonicPower.HaveBuff)
                {
                    GrimoireofSacrifice.Cast();
                    return;
                }
            }
            //Summon Grimoire
            else if (MySettings.UseGrimoireImp && GrimoireImp.IsSpellUsable && GrimoireImp.IsHostileDistanceGood)
            {
                GrimoireImp.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            else if (MySettings.UseGrimoireFelguard && GrimoireFelguard.IsSpellUsable && GrimoireFelguard.IsHostileDistanceGood)
            {
                GrimoireFelguard.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            else if (MySettings.UseGrimoireFelhunter && GrimoireFelhunter.IsSpellUsable && GrimoireFelhunter.IsHostileDistanceGood)
            {
                GrimoireFelhunter.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            else if (MySettings.UseGrimoireSuccubus && GrimoireSuccubus.IsSpellUsable && GrimoireSuccubus.IsHostileDistanceGood)
            {
                GrimoireSuccubus.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            else if (MySettings.UseGrimoireVoidwalker && GrimoireVoidwalker.IsSpellUsable && GrimoireVoidwalker.IsHostileDistanceGood)
            {
                GrimoireVoidwalker.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            //Summon Grimoire of Service
            if (MySettings.UseGrimoireofService && GrimoireofService.IsSpellUsable && GrimoireofService.IsHostileDistanceGood)
            {
                GrimoireofService.CastAtPosition(ObjectManager.Target.Position);
                return;
            }

            //5. Filler
            //Cast Haunt
            if (MySettings.UseHaunt && Haunt.IsSpellUsable && !ObjectManager.Me.GetMove && Haunt.IsHostileDistanceGood)
            {
                Haunt.Cast();
                return;
            }
            //Cast Unstable Affliction when
            if (MySettings.UseUnstableAffliction && UnstableAffliction.IsSpellUsable &&
                !ObjectManager.Me.GetMove && UnstableAffliction.IsHostileDistanceGood &&
                //you have the Contagion Talent
                Contagion.HaveBuff)
            {
                UnstableAffliction.Cast();
                return;
            }
            //Channel Drain Life & Drain Soul
            if (MySettings.UseDrainLife_Soul && DrainLife.IsSpellUsable &&
                !ObjectManager.Me.GetMove && DrainLife.IsHostileDistanceGood)
            {
                DrainLife.Cast();
                return;
            }
            //Cast Life Tap
            if (ObjectManager.Me.ManaPercentage < MySettings.UseLifeTapBelowPercentage &&
                ObjectManager.Me.HealthPercent > MySettings.UseLifeTapAbovePercentage &&
                LifeTap.IsSpellUsable)
            {
                LifeTap.Cast();
                return;
            }
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    #region Nested type: WarlockAfflictionSettings

    [Serializable]
    public class WarlockAfflictionSettings : Settings
    {
        /* Professions & Racials */
        //public bool UseArcaneTorrent = true;
        public bool UseBerserking = true;
        public bool UseBloodFury = true;
        public bool UseDarkflight = true;
        public int UseGiftoftheNaaruBelowPercentage = 50;
        public int UseStoneformBelowPercentage = 50;
        public int UseWarStompBelowPercentage = 50;

        /* Dots */
        public bool UseAgony = true;
        public bool UseCorruption = true;
        public bool UseSeedofCorruption = true;
        public bool UseSiphonLife = true;
        public bool UseUnstableAffliction = true;

        /* Artifact Spells */
        public bool UseReapSouls = true;

        /* Offensive Spells */
        public bool UseDrainLife_Soul = true;
        public bool UseHaunt = true;
        public int UseManaTapAbovePercentage = 50;

        /* Offensive Cooldowns */
        public bool UseGrimoireImp = false;
        public bool UseGrimoireFelguard = false;
        public bool UseGrimoireFelhunter = true;
        public bool UseGrimoireofSacrifice = true;
        public bool UseGrimoireofService = true;
        public bool UseGrimoireSuccubus = false;
        public bool UseGrimoireVoidwalker = false;
        public bool UsePhantomSingularity = true;
        public bool UseSoulHarvest = true;
        public bool UseSummonDoomguard = true;
        public bool UseSummonInfernal = true;

        /* Defensive Spells */
        public int UseDarkPactBelowPercentage = 70;
        public int UseUnendingResolveBelowPercentage = 30;

        /* Healing Spells */
        public bool UseCreateHealthstone = true;
        public int UseHealthstoneBelowPercentage = 25;
        public int UseDrainLife_SoulBelowPercentage = 25;
        

        /* Utility Spells */
        public int StartBurningRushAbovePercentage = 99;
        public int StopBurningRushBelowPercentage = 60;
        public int UseLifeTapAbovePercentage = 35;
        public int UseLifeTapBelowPercentage = 50;
        public bool UseSoulEffigy = true;

        /* Game Settings */
        public bool UseTrinketOne = true;
        public bool UseTrinketTwo = true;

        public WarlockAfflictionSettings()
        {
            ConfigWinForm("Warlock Affliction Settings");
            /* Professions & Racials */
            //AddControlInWinForm("Use Arcane Torrent", "UseArcaneTorrent", "Professions & Racials");
            AddControlInWinForm("Use Berserking", "UseBerserking", "Professions & Racials");
            AddControlInWinForm("Use Blood Fury", "UseBloodFury", "Professions & Racials");
            AddControlInWinForm("Use Darkflight", "UseDarkflight", "Professions & Racials");
            AddControlInWinForm("Use Gift of the Naaru", "UseGiftoftheNaaruBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            AddControlInWinForm("Use Stone Form", "UseStoneformBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            AddControlInWinForm("Use War Stomp", "UseWarStompBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            /* Dots */
            AddControlInWinForm("Use Agony", "UseAgony", "Dots");
            AddControlInWinForm("Use Corruption", "UseCorruption", "Dots");
            AddControlInWinForm("Use Seed of Corruption", "UseSeedofCorruption", "Dots");
            AddControlInWinForm("Use Siphon Life", "UseSiphonLife", "Dots");
            AddControlInWinForm("Use Unstable Affliction", "UseUnstableAffliction", "Dots");
            /* Artifact Spells */
            AddControlInWinForm("Use Reap Souls", "UseReapSouls", "Artifact Spells");
            /* Offensive Spells */
            AddControlInWinForm("Use Drain Life & Drain Soul", "UseDrainLife_Soul", "Offensive Spells");
            AddControlInWinForm("Use Haunt", "UseHaunt", "Offensive Spells");
            AddControlInWinForm("Use Mana Tap", "UseManaTapAbovePercentage", "Offensive Spells", "AbovePercentage", "Mana");
            /* Offensive Cooldowns */
            AddControlInWinForm("Use Grimoire: Imp", "UseGrimoireImp", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Felguard", "UseGrimoireFelguard", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Felhunter", "UseGrimoireFelhunter", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Succubus", "UseGrimoireSuccubus", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Voidwalker", "UseGrimoireVoidwalker", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire of Sacrifice", "UseGrimoireofSacrifice", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire of Service", "UseGrimoireofService", "Offensive Cooldowns");
            AddControlInWinForm("Use Phantom Singularity", "UsePhantomSingularity", "Offensive Cooldowns");
            AddControlInWinForm("Use Soul Harvest", "UseSoulHarvest", "Offensive Cooldowns");
            AddControlInWinForm("Use Summon Doomguard", "UseSummonDoomguard", "Offensive Cooldowns");
            AddControlInWinForm("Use Summon Infernal", "UseSummonInfernal", "Offensive Cooldowns");
            /* Defensive Spells */
            AddControlInWinForm("Use Dark Pact", "UseDarkPactBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Unending Resolve", "UseUnendingResolveBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            /* Healing Spell */
            AddControlInWinForm("Use Create Healthstone", "UseCreateHealthstone", "Healing Spells");
            AddControlInWinForm("Use Healthstone", "UseHealthstoneBelowPercentage", "Healing Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Drain Life & Drain Soul", "UseDrainLife_SoulBelowPercentage", "Healing Spells", "BelowPercentage", "Life");
            /* Utility Spells */
            AddControlInWinForm("Start Burning Rush", "StartBurningRushAbovePercentage", "Utility Spells", "AbovePercentage", "Life");
            AddControlInWinForm("Stop Burning Rush", "StopBurningRushBelowPercentage", "Utility Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Life Tap", "UseLifeTapAbovePercentage", "Utility Spells", "AbovePercentage", "Life");
            AddControlInWinForm("Use Life Tap", "UseLifeTapBelowPercentage", "Utility Spells", "BelowPercentage", "Mana");
            AddControlInWinForm("Use Soul Effigy", "UseSoulEffigy", "Utility Spells");
            /* Game Settings */
            AddControlInWinForm("Use Trinket One", "UseTrinketOne", "Game Settings");
            AddControlInWinForm("Use Trinket Two", "UseTrinketTwo", "Game Settings");
        }

        public static WarlockAfflictionSettings CurrentSetting { get; set; }

        public static WarlockAfflictionSettings GetSettings()
        {
            string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warlock_Affliction.xml";
            if (File.Exists(currentSettingsFile))
            {
                return
                    CurrentSetting = Load<WarlockAfflictionSettings>(currentSettingsFile);
            }
            return new WarlockAfflictionSettings();
        }
    }

    #endregion
}

public class WarlockDemonology
{
    private static WarlockDemonologySettings MySettings = WarlockDemonologySettings.GetSettings();

    #region General Timers & Variables

    private readonly WoWItem _firstTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET);
    private readonly WoWItem _secondTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET, 2);

    private bool CombatMode = true;

    private Timer DefensiveTimer = new Timer(0);
    private Timer StunTimer = new Timer(0);
    private static List<Timer> SummonTimers;
    private int SummonedDemons = 0;

    #endregion

    #region Professions & Racial

    //private readonly Spell ArcaneTorrent = new Spell("Arcane Torrent"); //No GCD
    private readonly Spell Berserking = new Spell("Berserking"); //No GCD
    private readonly Spell BloodFury = new Spell("Blood Fury"); //No GCD
    private readonly Spell Darkflight = new Spell("Darkflight"); //No GCD
    private readonly Spell GiftoftheNaaru = new Spell("Gift of the Naaru"); //No GCD
    private readonly Spell Stoneform = new Spell("Stoneform"); //No GCD
    private readonly Spell WarStomp = new Spell("War Stomp"); //No GCD

    #endregion

    #region Talents

    private readonly Spell GrimoireofSupremacy = new Spell("Grimoire of Supremacy");

    #endregion

    #region Artifact Spells

    private readonly Spell ThalkielsConsumption = new Spell("Thal'kiel's Consumption");

    #endregion

    #region Offensive Spells

    private readonly Spell CallDreadstalkers = new Spell("Call Dreadstalkers");
    private readonly Spell Demonbolt = new Spell("Demonbolt");
    private readonly Spell DemonicEmpowerment = new Spell("Demonic Empowerment");
    private readonly Spell Demonwrath = new Spell("Demonwrath");
    private Timer DemonicEmpowermentTimer = new Timer(0);
    private readonly Spell DrainLife = new Spell("Drain Life");
    private readonly Spell Doom = new Spell("Doom");
    private readonly Spell Felstorm = new Spell("Felstorm");
    private readonly Spell HandofGuldan = new Spell("Hand of Gul'dan");
    private Timer ImpsAliveTimer = new Timer(0);
    private readonly Spell Implosion = new Spell("Implosion");
    private Timer ImplosionTimer = new Timer(0);
    private readonly Spell ShadowBolt = new Spell("Shadow Bolt");
    private readonly Spell SummonDarkglare = new Spell("Summon Darkglare");

    #endregion

    #region Offensive Cooldowns

    private readonly Spell GrimoireImp = new Spell("Grimoire: Imp");
    private readonly Spell GrimoireFelguard = new Spell("Grimoire: Felguard");
    private readonly Spell GrimoireFelhunter = new Spell("Grimoire: Felhunter");
    private readonly Spell GrimoireSuccubus = new Spell("Grimoire: Succubus");
    private readonly Spell GrimoireVoidwalker = new Spell("Grimoire: Voidwalker");
    private readonly Spell GrimoireofService = new Spell("Grimoire of Service");
    private readonly Spell SoulHarvest = new Spell("Soul Harvest"); //No GCD
    private readonly Spell SummonDoomguard = new Spell("Summon Doomguard");
    private readonly Spell SummonImp = new Spell("Summon Imp");
    private readonly Spell SummonInfernal = new Spell("Summon Infernal");

    #endregion

    #region Defensive Spells

    private readonly Spell DarkPact = new Spell("Dark Pact"); //No GCD
    private readonly Spell UnendingResolve = new Spell("Unending Resolve"); //No GCD

    #endregion

    #region Utility Spells

    private readonly Spell BurningRush = new Spell("Burning Rush");
    private readonly Spell DemonicCircle = new Spell("Demonic Circle");
    private readonly Spell DemonicGateway = new Spell("Demonic Gateway");
    private readonly Spell Fear = new Spell("Fear");
    private readonly Spell CreateHealthstone = new Spell("Create Healthstone");
    private Timer HealthstoneTimer = new Timer(0);
    private readonly Spell HowlofTerror = new Spell("Howl of Terror"); //No GCD
    private readonly Spell LifeTap = new Spell("Life Tap");
    private readonly Spell SoulEffigy = new Spell("Soul Effigy");
    private WoWUnit SummonedSoulEffigy = new WoWUnit(0);
    private readonly Spell Soulstone = new Spell("Soulstone");

    #endregion

    public WarlockDemonology()
    {
        Main.InternalRange = 39f;
        Main.InternalAggroRange = 39f;
        Main.InternalLightHealingSpell = null;
        MySettings = WarlockDemonologySettings.GetSettings();
        Main.DumpCurrentSettings<WarlockDemonologySettings>(MySettings);
        UInt128 lastTarget = 0;

        while (Main.InternalLoop)
        {
            try
            {
                SummonedDemons = (ObjectManager.Pet.Health > 0) ? 1 : 0;
                SummonedDemons += (!ImpsAliveTimer.IsReady) ? 4 : 0;
                foreach (Timer timer in SummonTimers)
                {
                    if (timer.IsReady)
                        SummonTimers.Remove(timer);
                    else
                        SummonedDemons++;
                }

                if (!ObjectManager.Me.IsDeadMe)
                {
                    if (!ObjectManager.Me.IsMounted)
                    {
                        if (Fight.InFight && ObjectManager.Me.Target > 0)
                        {
                            if (ObjectManager.Me.Target != lastTarget)
                                lastTarget = ObjectManager.Me.Target;

                            if (CombatClass.InSpellRange(ObjectManager.Target, 0, 40))
                                Combat();
                            else if (!ObjectManager.Me.IsCast)
                                Patrolling();
                        }
                        else if (!ObjectManager.Me.IsCast)
                            Patrolling();
                    }
                }
                else
                    Thread.Sleep(500);
            }
            catch
            {
            }
            Thread.Sleep(100);
        }
    }

    // For Movement Spells (always return after Casting)
    private void Patrolling()
    {
        //Log
        if (CombatMode)
        {
            Logging.WriteFight("Patrolling:");
            CombatMode = false;
        }

        if (ObjectManager.Me.GetMove)
        {
            //Movement Buffs
            if (!Darkflight.HaveBuff && !BurningRush.HaveBuff) // doesn't stack
            {
                if (MySettings.UseDarkflight && Darkflight.IsSpellUsable)
                {
                    Darkflight.Cast();
                    return;
                }
                if (ObjectManager.Me.HealthPercent > MySettings.StartBurningRushAbovePercentage && BurningRush.IsSpellUsable)
                {
                    BurningRush.Cast();
                    return;
                }
            }
            if (ObjectManager.Me.HealthPercent < MySettings.StopBurningRushBelowPercentage &&
                BurningRush.IsSpellUsable && BurningRush.HaveBuff)
            {
                BurningRush.Cast();
                return;
            }
        }
        else
        {
            if (BurningRush.IsSpellUsable && BurningRush.HaveBuff)
            {
                BurningRush.Cast();
                return;
            }
            //Self Heal for Damage Dealer
            if (nManager.Products.Products.ProductName == "Damage Dealer" && Main.InternalLightHealingSpell.IsSpellUsable &&
                ObjectManager.Me.HealthPercent < 90 && ObjectManager.Target.Guid == ObjectManager.Me.Guid)
            {
                Main.InternalLightHealingSpell.CastOnSelf();
                return;
            }
            //Create Healthstone
            if (MySettings.UseCreateHealthstone && ItemsManager.GetItemCount(5512) == 0 &&
                Usefuls.GetContainerNumFreeSlots > 0 && CreateHealthstone.IsSpellUsable)
            {
                Logging.WriteFight("Create Healthstone");
                CreateHealthstone.Cast();
                /*Others.SafeSleep(500);
                while (ObjectManager.Me.IsCast)
                    Others.SafeSleep(200);*/
            }
            Pet();
        }
    }

    // For Summoning permanent Pets (always return after Casting)
    private bool Pet()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (GrimoireofSupremacy.HaveBuff)
            {
                if (ObjectManager.Pet.Health == 0 || ObjectManager.Pet.Guid == 0 || !ObjectManager.Pet.IsValid)
                {
                    //Summon Infernal
                    if (MySettings.UseSummonInfernal && SummonInfernal.IsSpellUsable &&
                        SummonInfernal.IsHostileDistanceGood)
                    {
                        SummonInfernal.CastAtPosition(ObjectManager.Target.Position);
                        DemonicEmpowermentTimer.Reset();
                        return true;
                    }
                    //Summon Doomguard
                    if (MySettings.UseSummonDoomguard && SummonDoomguard.IsSpellUsable &&
                        SummonDoomguard.IsHostileDistanceGood)
                    {
                        SummonDoomguard.CastAtPosition(ObjectManager.Target.Position);
                        DemonicEmpowermentTimer.Reset();
                        return true;
                    }
                }
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For general InFight Behavior (only touch if you want to add a new method like PetManagement())
    private void Combat()
    {
        //Log
        if (!CombatMode)
        {
            Logging.WriteFight("Combat:");
            CombatMode = true;
        }
        Healing();
        if (Defensive() || Pet() || Offensive())
            return;
        Rotation();
    }

    // For Self-Healing Spells (always return after Casting)
    private bool Healing()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            //Gift of the Naaru
            if (ObjectManager.Me.HealthPercent < MySettings.UseGiftoftheNaaruBelowPercentage && GiftoftheNaaru.IsSpellUsable)
            {
                GiftoftheNaaru.CastOnSelf();
                return true;
            }
            //Healthstone
            if (ObjectManager.Me.HealthPercent < MySettings.UseHealthstoneBelowPercentage &&
                HealthstoneTimer.IsReady && ItemsManager.GetItemCount(5512) > 0)
            {
                Logging.WriteFight("Use Healthstone.");
                ItemsManager.UseItem("Healthstone");
                HealthstoneTimer = new Timer(1000 * 60);
                return true;
            }
            //Channel Drain Life
            if (ObjectManager.Me.HealthPercent < MySettings.UseDrainLifeBelowPercentage && DrainLife.IsSpellUsable &&
                !ObjectManager.Me.GetMove && DrainLife.IsHostileDistanceGood)
            {
                DrainLife.Cast();
                return true;
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For Defensive Buffs and Livesavers (always return after Casting)
    private bool Defensive()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (StunTimer.IsReady && ((DefensiveTimer.IsReady && !DarkPact.HaveBuff) || ObjectManager.Me.HealthPercent < 20))
            {
                //Stun
                if (ObjectManager.Target.IsStunnable)
                {
                    if (ObjectManager.Me.HealthPercent < MySettings.UseWarStompBelowPercentage && WarStomp.IsSpellUsable)
                    {
                        WarStomp.Cast();
                        StunTimer = new Timer(1000 * 2.5);
                        return true;
                    }
                }
                //Mitigate Damage
                if (ObjectManager.Me.HealthPercent < MySettings.UseStoneformBelowPercentage && Stoneform.IsSpellUsable)
                {
                    Stoneform.Cast();
                    DefensiveTimer = new Timer(1000 * 8);
                    return true;
                }
                if (ObjectManager.Me.HealthPercent < MySettings.UseDarkPactBelowPercentage && DarkPact.IsSpellUsable)
                {
                    DarkPact.Cast();
                    return true;
                }
            }
            //Mitigate Damage in Emergency Situations
            if (ObjectManager.Me.HealthPercent < MySettings.UseUnendingResolveBelowPercentage && UnendingResolve.IsSpellUsable)
            {
                UnendingResolve.Cast();
                return true;
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For Offensive Buffs (only return if a Cast triggered Global Cooldown)
    private bool Offensive()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (MySettings.UseTrinketOne && !ItemsManager.IsItemOnCooldown(_firstTrinket.Entry) && ItemsManager.IsItemUsable(_firstTrinket.Entry))
            {
                ItemsManager.UseItem(_firstTrinket.Name);
                Logging.WriteFight("Use First Trinket Slot");
            }
            if (MySettings.UseTrinketTwo && !ItemsManager.IsItemOnCooldown(_secondTrinket.Entry) && ItemsManager.IsItemUsable(_secondTrinket.Entry))
            {
                ItemsManager.UseItem(_secondTrinket.Name);
                Logging.WriteFight("Use Second Trinket Slot");
            }
            if (MySettings.UseBerserking && Berserking.IsSpellUsable)
            {
                Berserking.Cast();
            }
            if (MySettings.UseBloodFury && BloodFury.IsSpellUsable)
            {
                BloodFury.Cast();
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For the Ability Priority Logic
    private void Rotation()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            //Apply Doom
            if (MySettings.UseDoom && Doom.IsSpellUsable && Doom.IsHostileDistanceGood &&
                !Doom.TargetHaveBuff)
            {
                Doom.Cast();
                return;
            }
            //Cast Thal'kiel's Consumption when
            if (MySettings.UseThalkielsConsumption && ThalkielsConsumption.IsSpellUsable &&
                ThalkielsConsumption.IsHostileDistanceGood &&
                //you have 3 or more Demons
                SummonedDemons >= 3)
            {
                //Apply Soul Harvest 
                if (MySettings.UseSoulHarvest && SoulHarvest.IsSpellUsable)
                {
                    SoulHarvest.Cast();
                }
                ThalkielsConsumption.Cast();
                return;
            }
            //Cast Demonic Empowerment when
            if (MySettings.UseDemonicEmpowerment && ThalkielsConsumption.IsSpellUsable &&
                //you have 1 or more Demons and
                SummonedDemons >= 1 &&
                //Demonic Empowerment isn't up for all
                DemonicEmpowermentTimer.IsReady)
            {
                DemonicEmpowerment.Cast();
                DemonicEmpowermentTimer = new Timer(1000 * 12);
                return;
            }

            //Summon Infernal when
            if (MySettings.UseSummonInfernal && SummonInfernal.IsSpellUsable &&
                SummonInfernal.IsHostileDistanceGood && !GrimoireofSupremacy.HaveBuff &&
                //you have 4 or more targets.
                ObjectManager.Target.GetUnitInSpellRange(10f) >= 4)
            {
                SummonInfernal.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 25));
                return;
            }
            //Summon Doomguard
            if (MySettings.UseSummonDoomguard && SummonDoomguard.IsSpellUsable &&
                SummonDoomguard.IsHostileDistanceGood && !GrimoireofSupremacy.HaveBuff)
            {
                SummonDoomguard.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 25));
                return;
            }
            //Summon Darkglare
            if (MySettings.UseSummonDarkglare && SummonDarkglare.IsSpellUsable)
            {
                SummonDarkglare.Cast();
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 12));
                return;
            }
            //Summon Grimoire
            if (MySettings.UseGrimoireImp && GrimoireImp.IsSpellUsable && GrimoireImp.IsHostileDistanceGood)
            {
                GrimoireImp.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 25));
                return;
            }
            else if (MySettings.UseGrimoireFelguard && GrimoireFelguard.IsSpellUsable && GrimoireFelguard.IsHostileDistanceGood)
            {
                GrimoireFelguard.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 25));
                return;
            }
            else if (MySettings.UseGrimoireFelhunter && GrimoireFelhunter.IsSpellUsable && GrimoireFelhunter.IsHostileDistanceGood)
            {
                GrimoireFelhunter.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 25));
                return;
            }
            else if (MySettings.UseGrimoireSuccubus && GrimoireSuccubus.IsSpellUsable && GrimoireSuccubus.IsHostileDistanceGood)
            {
                GrimoireSuccubus.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 25));
                return;
            }
            else if (MySettings.UseGrimoireVoidwalker && GrimoireVoidwalker.IsSpellUsable && GrimoireVoidwalker.IsHostileDistanceGood)
            {
                GrimoireVoidwalker.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 25));
                return;
            }
            //Summon Grimoire of Service
            if (MySettings.UseGrimoireofService && GrimoireofService.IsSpellUsable && GrimoireofService.IsHostileDistanceGood)
            {
                GrimoireofService.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 25));
                return;
            }
            //Summon Dreadstalkers
            if (MySettings.UseCallDreadstalkers && CallDreadstalkers.IsSpellUsable &&
                !ObjectManager.Me.GetMove && CallDreadstalkers.IsHostileDistanceGood)
            {
                CallDreadstalkers.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                SummonTimers.Add(new Timer(1000 * 12));
                SummonTimers.Add(new Timer(1000 * 12));
                return;
            }
            //Cast Hand of Guldan when
            if (MySettings.UseHandofGuldan && HandofGuldan.IsSpellUsable &&
                !ObjectManager.Me.GetMove && HandofGuldan.IsHostileDistanceGood &&
                //you have 4 or moreSoul Shards.
                ObjectManager.Me.SoulShards >= 4)
            {
                HandofGuldan.CastAtPosition(ObjectManager.Target.Position);
                DemonicEmpowermentTimer.Reset();
                ImpsAliveTimer = new Timer(1000 * 12);
                ImplosionTimer = new Timer(1000 * (12 - 1));
                return;
            }
            //Apply Soul Harvest when you don't use it to Buff Thal'kiel's Consumption
            if (MySettings.UseSoulHarvest && SoulHarvest.IsSpellUsable && !MySettings.UseThalkielsConsumption)
            {
                SoulHarvest.Cast();
            }
            //Cast Implosion when
            if (MySettings.UseImplosion && Implosion.IsSpellUsable && Implosion.IsHostileDistanceGood &&
                //there are Imps alive and they will die shortly and
                !ImpsAliveTimer.IsReady && ImplosionTimer.IsReady &&
                //you have 4 or more targets.
                ObjectManager.Target.GetUnitInSpellRange(8f) >= 4)
            {
                Implosion.Cast();
                ImpsAliveTimer.Reset();
            }
            //Use Felguards Felstorm Spell //TESTING 
            /*if (MySettings.UseFelstorm && Felstorm.IsSpellUsable)
            {
                Felstorm.Cast();
            }*/
            //Cast Shadow Bolt / Demonbolt
            if (MySettings.UseShadowBolt_Demonbolt && ShadowBolt.IsSpellUsable &&
                !ObjectManager.Me.GetMove && ShadowBolt.IsHostileDistanceGood)
            {
                ShadowBolt.Cast();
                return;
            }
            //Channel Drain Life
            if (MySettings.UseDrainLife && DrainLife.IsSpellUsable &&
                !ObjectManager.Me.GetMove && DrainLife.IsHostileDistanceGood)
            {
                DrainLife.Cast();
                return;
            }
            //Cast Demonwrath
            if (ObjectManager.Me.ManaPercentage > MySettings.UseDemonwrathAbovePercentage &&
                Demonwrath.IsSpellUsable)
            {
                Demonwrath.Cast();
                return;
            }
            //Cast Life Tap
            if (ObjectManager.Me.ManaPercentage < MySettings.UseLifeTapBelowPercentage &&
                ObjectManager.Me.HealthPercent > MySettings.UseLifeTapAbovePercentage &&
                LifeTap.IsSpellUsable)
            {
                LifeTap.Cast();
                return;
            }
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    #region Nested type: WarlockDemonologySettings

    [Serializable]
    public class WarlockDemonologySettings : Settings
    {
        /* Professions & Racials */
        //public bool UseArcaneTorrent = true;
        public bool UseBerserking = true;
        public bool UseBloodFury = true;
        public bool UseDarkflight = true;
        public int UseGiftoftheNaaruBelowPercentage = 50;
        public int UseStoneformBelowPercentage = 50;
        public int UseWarStompBelowPercentage = 50;

        /* Artifact Spells */
        public bool UseThalkielsConsumption = true;

        /* Offensive Spells */
        public bool UseCallDreadstalkers = true;
        public bool UseDemonicEmpowerment = true;
        public int UseDemonwrathAbovePercentage = 60;
        public bool UseDrainLife = true;
        public bool UseDoom = true;
        public bool UseHandofGuldan = true;
        public bool UseImplosion = true;
        public bool UseShadowBolt_Demonbolt = true;
        public bool UseSummonDarkglare = true;

        /* Offensive Cooldowns */
        public bool UseGrimoireImp = false;
        public bool UseGrimoireFelguard = true;
        public bool UseGrimoireFelhunter = false;
        public bool UseGrimoireofService = true;
        public bool UseGrimoireSuccubus = false;
        public bool UseGrimoireVoidwalker = false;
        public bool UseSoulHarvest = true;
        public bool UseSummonDoomguard = true;
        public bool UseSummonInfernal = true;

        /* Defensive Spells */
        public int UseDarkPactBelowPercentage = 70;
        public int UseUnendingResolveBelowPercentage = 30;

        /* Healing Spells */
        public bool UseCreateHealthstone = true;
        public int UseHealthstoneBelowPercentage = 25;
        public int UseDrainLifeBelowPercentage = 25;

        /* Utility Spells */
        public int StartBurningRushAbovePercentage = 99;
        public int StopBurningRushBelowPercentage = 60;
        public int UseLifeTapAbovePercentage = 35;
        public int UseLifeTapBelowPercentage = 50;
        public bool UseSoulEffigy = true;

        /* Game Settings */
        public bool UseTrinketOne = true;
        public bool UseTrinketTwo = true;

        public WarlockDemonologySettings()
        {
            ConfigWinForm("Warlock Demonology Settings");
            /* Professions & Racials */
            //AddControlInWinForm("Use Arcane Torrent", "UseArcaneTorrent", "Professions & Racials");
            AddControlInWinForm("Use Berserking", "UseBerserking", "Professions & Racials");
            AddControlInWinForm("Use Blood Fury", "UseBloodFury", "Professions & Racials");
            AddControlInWinForm("Use Darkflight", "UseDarkflight", "Professions & Racials");
            AddControlInWinForm("Use Gift of the Naaru", "UseGiftoftheNaaruBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            AddControlInWinForm("Use Stone Form", "UseStoneformBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            AddControlInWinForm("Use War Stomp", "UseWarStompBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            /* Artifact Spells */
            AddControlInWinForm("Use Thal'kiel's Consumption", "UseThalkielsConsumption", "Artifact Spells");
            /* Offensive Spells */
            AddControlInWinForm("Use Call Dreadstalkers", "UseCallDreadstalkers", "Offensive Spells");
            AddControlInWinForm("Use Demonic Empowerment", "UseDemonicEmpowerment", "Offensive Spells");
            AddControlInWinForm("Use Demonwrath", "UseDemonwrathAbovePercentage", "Offensive Spells", "AbovePercentage", "Mana");
            AddControlInWinForm("Use Drain Life", "UseDrainLife", "Offensive Spells");
            AddControlInWinForm("Use Doom", "UseDoom", "Offensive Spells");
            AddControlInWinForm("Use Hand of Guldan", "UseHandofGuldan", "Offensive Spells");
            AddControlInWinForm("Use Implosion", "UseImplosion", "Offensive Spells");
            AddControlInWinForm("Use Shadow Bolt & Demonbolt", "UseShadowBolt_Demonbolt", "Offensive Spells");
            AddControlInWinForm("Use Summon Darkglare", "UseSummonDarkglare", "Offensive Spells");
            /* Offensive Cooldowns */
            AddControlInWinForm("Use Grimoire: Imp", "UseGrimoireImp", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Felguard", "UseGrimoireFelguard", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Felhunter", "UseGrimoireFelhunter", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Succubus", "UseGrimoireSuccubus", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Voidwalker", "UseGrimoireVoidwalker", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire of Service", "UseGrimoireofService", "Offensive Cooldowns");
            AddControlInWinForm("Use Soul Harvest", "UseSoulHarvest", "Offensive Cooldowns");
            AddControlInWinForm("Use Summon Doomguard", "UseSummonDoomguard", "Offensive Cooldowns");
            AddControlInWinForm("Use Summon Infernal", "UseSummonInfernal", "Offensive Cooldowns");
            /* Defensive Spells */
            AddControlInWinForm("Use Dark Pact", "UseDarkPactBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Unending Resolve", "UseUnendingResolveBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            /* Healing Spell */
            AddControlInWinForm("Use Create Healthstone", "UseCreateHealthstone", "Healing Spells");
            AddControlInWinForm("Use Healthstone", "UseHealthstoneBelowPercentage", "Healing Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Drain Life", "UseDrainLifeBelowPercentage", "Healing Spells", "BelowPercentage", "Life");
            /* Utility Spells */
            AddControlInWinForm("Start Burning Rush", "StartBurningRushAbovePercentage", "Utility Spells", "AbovePercentage", "Life");
            AddControlInWinForm("Stop Burning Rush", "StopBurningRushBelowPercentage", "Utility Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Life Tap", "UseLifeTapAbovePercentage", "Utility Spells", "AbovePercentage", "Life");
            AddControlInWinForm("Use Life Tap", "UseLifeTapBelowPercentage", "Utility Spells", "BelowPercentage", "Mana");
            AddControlInWinForm("Use Soul Effigy", "UseSoulEffigy", "Utility Spells");
            /* Game Settings */
            AddControlInWinForm("Use Trinket One", "UseTrinketOne", "Game Settings");
            AddControlInWinForm("Use Trinket Two", "UseTrinketTwo", "Game Settings");
        }

        public static WarlockDemonologySettings CurrentSetting { get; set; }

        public static WarlockDemonologySettings GetSettings()
        {
            string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warlock_Demonology.xml";
            if (File.Exists(currentSettingsFile))
            {
                return
                    CurrentSetting = Load<WarlockDemonologySettings>(currentSettingsFile);
            }
            return new WarlockDemonologySettings();
        }
    }

    #endregion
}

public class WarlockDestruction
{
    private static WarlockDestructionSettings MySettings = WarlockDestructionSettings.GetSettings();

    #region General Timers & Variables

    private readonly WoWItem _firstTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET);
    private readonly WoWItem _secondTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET, 2);

    private bool CombatMode = true;

    private Timer DefensiveTimer = new Timer(0);
    private Timer StunTimer = new Timer(0);

    #endregion

    #region Professions & Racial

    //private readonly Spell ArcaneTorrent = new Spell("Arcane Torrent"); //No GCD
    private readonly Spell Berserking = new Spell("Berserking"); //No GCD
    private readonly Spell BloodFury = new Spell("Blood Fury"); //No GCD
    private readonly Spell Darkflight = new Spell("Darkflight"); //No GCD
    private readonly Spell GiftoftheNaaru = new Spell("Gift of the Naaru"); //No GCD
    private readonly Spell Stoneform = new Spell("Stoneform"); //No GCD
    private readonly Spell WarStomp = new Spell("War Stomp"); //No GCD

    #endregion

    #region Talents

    private readonly Spell Eradication = new Spell("Eradication");
    private readonly Spell GrimoireofSupremacy = new Spell("Grimoire of Supremacy");
    private readonly Spell RoaringBlaze = new Spell("Roaring Blaze");

    #endregion

    #region Dots
    
    private readonly Spell ImmolateDot = new Spell(193541); //TESTING 157736

    #endregion

    #region Buffs

    private readonly Spell DemonicPower = new Spell("Demonic Power");
    private readonly Spell EradicationBuff = new Spell(196414);
    private readonly Spell TormentedSouls = new Spell("Tormented Souls");

    #endregion

    #region Artifact Spells

    private readonly Spell DimensionalRift = new Spell("Dimensional Rift");
    private readonly Spell LordofFlamesTrait = new Spell("Lord of Flames");

    #endregion

    #region Offensive Spells

    private readonly Spell Cataclysm = new Spell("Cataclysm");
    private readonly Spell ChaosBolt = new Spell("Chaos Bolt");
    private readonly Spell Conflagrate = new Spell("Conflagrate");
    private readonly Spell DrainLife = new Spell("Drain Life");
    private readonly Spell GrimoireofSacrifice = new Spell("Grimoire of Sacrifice");
    private readonly Spell Immolate = new Spell("Immolate");
    private readonly Spell Incinerate = new Spell("Incinerate");
    private readonly Spell ManaTap = new Spell("Mana Tap");
    private readonly Spell RainofFire = new Spell("Rain of Fire");
    private readonly Spell Shadowburn = new Spell("Shadowburn");

    #endregion

    #region Offensive Cooldowns

    private readonly Spell GrimoireImp = new Spell("Grimoire: Imp");
    private readonly Spell GrimoireFelguard = new Spell("Grimoire: Felguard");
    private readonly Spell GrimoireFelhunter = new Spell("Grimoire: Felhunter");
    private readonly Spell GrimoireSuccubus = new Spell("Grimoire: Succubus");
    private readonly Spell GrimoireVoidwalker = new Spell("Grimoire: Voidwalker");
    private readonly Spell GrimoireofService = new Spell("Grimoire of Service");
    private readonly Spell SoulHarvest = new Spell("Soul Harvest"); //No GCD
    private readonly Spell SummonDoomguard = new Spell("Summon Doomguard");
    private readonly Spell SummonImp = new Spell("Summon Imp");
    private readonly Spell SummonInfernal = new Spell("Summon Infernal");

    #endregion

    #region Defensive Spells

    private readonly Spell DarkPact = new Spell("Dark Pact"); //No GCD
    private readonly Spell UnendingResolve = new Spell("Unending Resolve"); //No GCD

    #endregion

    #region Utility Spells

    private readonly Spell BurningRush = new Spell("Burning Rush");
    private readonly Spell DemonicCircle = new Spell("Demonic Circle");
    private readonly Spell DemonicGateway = new Spell("Demonic Gateway");
    private readonly Spell Fear = new Spell("Fear");
    private readonly Spell Havoc = new Spell("Havoc");
    private readonly Spell CreateHealthstone = new Spell("Create Healthstone");
    private Timer HealthstoneTimer = new Timer(0);
    private readonly Spell HowlofTerror = new Spell("Howl of Terror"); //No GCD
    private readonly Spell LifeTap = new Spell("Life Tap");
    private readonly Spell SoulEffigy = new Spell("Soul Effigy");
    private WoWUnit SummonedSoulEffigy = new WoWUnit(0);
    private readonly Spell Soulstone = new Spell("Soulstone");

    #endregion

    public WarlockDestruction()
    {
        Main.InternalRange = 39f;
        Main.InternalAggroRange = 39f;
        Main.InternalLightHealingSpell = null;
        MySettings = WarlockDestructionSettings.GetSettings();
        Main.DumpCurrentSettings<WarlockDestructionSettings>(MySettings);
        UInt128 lastTarget = 0;

        while (Main.InternalLoop)
        {
            try
            {
                if (!ObjectManager.Me.IsDeadMe)
                {
                    if (!ObjectManager.Me.IsMounted)
                    {
                        if (Fight.InFight && ObjectManager.Me.Target > 0)
                        {
                            if (ObjectManager.Me.Target != lastTarget)
                                lastTarget = ObjectManager.Me.Target;

                            if (CombatClass.InSpellRange(ObjectManager.Target, 0, 40))
                                Combat();
                            else if (!ObjectManager.Me.IsCast)
                                Patrolling();
                        }
                        else if (!ObjectManager.Me.IsCast)
                            Patrolling();
                    }
                }
                else
                    Thread.Sleep(500);
            }
            catch
            {
            }
            Thread.Sleep(100);
        }
    }

    // For Movement Spells (always return after Casting)
    private void Patrolling()
    {
        //Log
        if (CombatMode)
        {
            Logging.WriteFight("Patrolling:");
            CombatMode = false;
        }

        if (ObjectManager.Me.GetMove)
        {
            //Movement Buffs
            if (!Darkflight.HaveBuff && !BurningRush.HaveBuff) // doesn't stack
            {
                if (MySettings.UseDarkflight && Darkflight.IsSpellUsable)
                {
                    Darkflight.Cast();
                    return;
                }
                if (ObjectManager.Me.HealthPercent > MySettings.StartBurningRushAbovePercentage && BurningRush.IsSpellUsable)
                {
                    BurningRush.Cast();
                    return;
                }
            }
            if (ObjectManager.Me.HealthPercent < MySettings.StopBurningRushBelowPercentage &&
                BurningRush.IsSpellUsable && BurningRush.HaveBuff)
            {
                BurningRush.Cast();
                return;
            }
        }
        else
        {
            if (BurningRush.IsSpellUsable && BurningRush.HaveBuff)
            {
                BurningRush.Cast();
                return;
            }
            //Self Heal for Damage Dealer
            if (nManager.Products.Products.ProductName == "Damage Dealer" && Main.InternalLightHealingSpell.IsSpellUsable &&
                ObjectManager.Me.HealthPercent < 90 && ObjectManager.Target.Guid == ObjectManager.Me.Guid)
            {
                Main.InternalLightHealingSpell.CastOnSelf();
                return;
            }
            //Create Healthstone
            if (MySettings.UseCreateHealthstone && ItemsManager.GetItemCount(5512) == 0 &&
                Usefuls.GetContainerNumFreeSlots > 0 && CreateHealthstone.IsSpellUsable)
            {
                Logging.WriteFight("Create Healthstone");
                CreateHealthstone.Cast();
                /*Others.SafeSleep(500);
                while (ObjectManager.Me.IsCast)
                    Others.SafeSleep(200);*/
            }
            Pet();
        }
    }

    // For Summoning permanent Pets (always return after Casting)
    private bool Pet()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (GrimoireofSupremacy.HaveBuff)
            {
                if (ObjectManager.Pet.Health == 0 || ObjectManager.Pet.Guid == 0 || !ObjectManager.Pet.IsValid)
                {
                    //Summon Infernal
                    if (MySettings.UseSummonInfernal && SummonInfernal.IsSpellUsable &&
                        SummonInfernal.IsHostileDistanceGood)
                    {
                        SummonInfernal.CastAtPosition(ObjectManager.Target.Position);
                        return true;
                    }
                    //Summon Doomguard
                    if (MySettings.UseSummonDoomguard && SummonDoomguard.IsSpellUsable &&
                        SummonDoomguard.IsHostileDistanceGood)
                    {
                        SummonDoomguard.CastAtPosition(ObjectManager.Target.Position);
                        return true;
                    }
                }
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For general InFight Behavior (only touch if you want to add a new method like PetManagement())
    private void Combat()
    {
        //Log
        if (!CombatMode)
        {
            Logging.WriteFight("Combat:");
            CombatMode = true;
        }
        Healing();
        if (Defensive() || Pet() || Offensive())
            return;
        Rotation();
    }

    // For Self-Healing Spells (always return after Casting)
    private bool Healing()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            //Gift of the Naaru
            if (ObjectManager.Me.HealthPercent < MySettings.UseGiftoftheNaaruBelowPercentage && GiftoftheNaaru.IsSpellUsable)
            {
                GiftoftheNaaru.CastOnSelf();
                return true;
            }
            //Healthstone
            if (ObjectManager.Me.HealthPercent < MySettings.UseHealthstoneBelowPercentage &&
                HealthstoneTimer.IsReady && ItemsManager.GetItemCount(5512) > 0)
            {
                Logging.WriteFight("Use Healthstone.");
                ItemsManager.UseItem("Healthstone");
                HealthstoneTimer = new Timer(1000 * 60);
                return true;
            }
            //Channel Drain Life
            if (ObjectManager.Me.HealthPercent < MySettings.UseDrainLifeBelowPercentage && DrainLife.IsSpellUsable &&
                !ObjectManager.Me.GetMove && DrainLife.IsHostileDistanceGood)
            {
                DrainLife.Cast();
                return true;
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For Defensive Buffs and Livesavers (always return after Casting)
    private bool Defensive()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (StunTimer.IsReady && ((DefensiveTimer.IsReady && !DarkPact.HaveBuff) || ObjectManager.Me.HealthPercent < 20))
            {
                //Stun
                if (ObjectManager.Target.IsStunnable)
                {
                    if (ObjectManager.Me.HealthPercent < MySettings.UseWarStompBelowPercentage && WarStomp.IsSpellUsable)
                    {
                        WarStomp.Cast();
                        StunTimer = new Timer(1000 * 2.5);
                        return true;
                    }
                }
                //Mitigate Damage
                if (ObjectManager.Me.HealthPercent < MySettings.UseStoneformBelowPercentage && Stoneform.IsSpellUsable)
                {
                    Stoneform.Cast();
                    DefensiveTimer = new Timer(1000 * 8);
                    return true;
                }
                if (ObjectManager.Me.HealthPercent < MySettings.UseDarkPactBelowPercentage && DarkPact.IsSpellUsable)
                {
                    DarkPact.Cast();
                    return true;
                }
            }
            //Mitigate Damage in Emergency Situations
            if (ObjectManager.Me.HealthPercent < MySettings.UseUnendingResolveBelowPercentage && UnendingResolve.IsSpellUsable)
            {
                UnendingResolve.Cast();
                return true;
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For Offensive Buffs (only return if a Cast triggered Global Cooldown)
    private bool Offensive()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (MySettings.UseTrinketOne && !ItemsManager.IsItemOnCooldown(_firstTrinket.Entry) && ItemsManager.IsItemUsable(_firstTrinket.Entry))
            {
                ItemsManager.UseItem(_firstTrinket.Name);
                Logging.WriteFight("Use First Trinket Slot");
            }
            if (MySettings.UseTrinketTwo && !ItemsManager.IsItemOnCooldown(_secondTrinket.Entry) && ItemsManager.IsItemUsable(_secondTrinket.Entry))
            {
                ItemsManager.UseItem(_secondTrinket.Name);
                Logging.WriteFight("Use Second Trinket Slot");
            }
            if (MySettings.UseBerserking && Berserking.IsSpellUsable)
            {
                Berserking.Cast();
            }
            if (MySettings.UseBloodFury && BloodFury.IsSpellUsable)
            {
                BloodFury.Cast();
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For the Ability Priority Logic
    private void Rotation()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            //Summon Infernal when
            if (MySettings.UseSummonInfernal && SummonInfernal.IsSpellUsable && SummonInfernal.IsHostileDistanceGood &&
                //you have 4 or more targets.
                ObjectManager.Target.GetUnitInSpellRange(10f) >= 4)
            {
                SummonInfernal.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            //Summon Doomguard
            if (MySettings.UseSummonDoomguard && SummonDoomguard.IsSpellUsable && SummonDoomguard.IsHostileDistanceGood)
            {
                SummonDoomguard.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            //Apply Grimoire of Sacrifice
            if (MySettings.UseGrimoireofSacrifice && GrimoireofSacrifice.IsSpellUsable)
            {
                if (!DemonicPower.HaveBuff)
                {
                    GrimoireofSacrifice.Cast();
                    return;
                }
            }
            //Summon Grimoire
            else if (MySettings.UseGrimoireImp && GrimoireImp.IsSpellUsable && GrimoireImp.IsHostileDistanceGood)
            {
                GrimoireImp.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            else if (MySettings.UseGrimoireFelguard && GrimoireFelguard.IsSpellUsable && GrimoireFelguard.IsHostileDistanceGood)
            {
                GrimoireFelguard.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            else if (MySettings.UseGrimoireFelhunter && GrimoireFelhunter.IsSpellUsable && GrimoireFelhunter.IsHostileDistanceGood)
            {
                GrimoireFelhunter.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            else if (MySettings.UseGrimoireSuccubus && GrimoireSuccubus.IsSpellUsable && GrimoireSuccubus.IsHostileDistanceGood)
            {
                GrimoireSuccubus.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            else if (MySettings.UseGrimoireVoidwalker && GrimoireVoidwalker.IsSpellUsable && GrimoireVoidwalker.IsHostileDistanceGood)
            {
                GrimoireVoidwalker.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            //Summon Grimoire of Service
            if (MySettings.UseGrimoireofService && GrimoireofService.IsSpellUsable && GrimoireofService.IsHostileDistanceGood)
            {
                GrimoireofService.CastAtPosition(ObjectManager.Target.Position);
                return;
            }
            //Maintain Immolate
            if (MySettings.UseImmolate && Immolate.IsSpellUsable &&
                !ObjectManager.Me.GetMove && Immolate.IsHostileDistanceGood &&
                ObjectManager.Target.AuraTimeLeft(ImmolateDot.Id, true) <= 1000 * 21 / 3)
            {
                Immolate.Cast();
                return;
            }
            //Apply Soul Harvest
            if (MySettings.UseSoulHarvest && SoulHarvest.IsSpellUsable)
            {
                SoulHarvest.Cast();
            }
            //Cast Chaos Bolt when
            if (MySettings.UseChaosBolt && ChaosBolt.IsSpellUsable &&
                !ObjectManager.Me.GetMove && ChaosBolt.IsHostileDistanceGood &&
                //you have 5 or more Soul Shards.
                ObjectManager.Me.SoulShards >= 5)
            {
                ChaosBolt.Cast();
                return;
            }
            //Cast Shadowburn when
            if (MySettings.UseShadowburn && Shadowburn.IsSpellUsable && Shadowburn.IsHostileDistanceGood &&
                //the target will die in 5 seconds
                ObjectManager.Target.HealthPercent < 10)
            {
                Shadowburn.Cast();
                return;
            }
            //Cast Conflagrate when
            if (MySettings.UseConflagrate && Conflagrate.IsSpellUsable && Conflagrate.IsHostileDistanceGood &&
                //you have only one Target and
                ObjectManager.Target.GetUnitInSpellRange(8f) == 1 &&
                //you have the Roaring Blaze Talent and Immolate is up.
                RoaringBlaze.HaveBuff && Immolate.TargetHaveBuff)
            {
                Conflagrate.Cast();
                return;
            }
            //Cast Dimensional Rift when
            if (MySettings.UseDimensionalRift && DimensionalRift.IsSpellUsable &&
                DimensionalRift.IsHostileDistanceGood)
            {
                DimensionalRift.Cast();
                return;
            }
            //Maintain Eradication Buff when
            if (MySettings.UseChaosBolt && ChaosBolt.IsSpellUsable &&
                !ObjectManager.Me.GetMove && ChaosBolt.IsHostileDistanceGood &&
                //you have the Eradication Talent and
                Eradication.HaveBuff &&
                //it won't cap it's duration [1000 * (duration / 3) + casttime]
                ObjectManager.Me.AuraTimeLeft(EradicationBuff.Id, true) <= 1000 * (6 / 3) + 3)
            {
                ChaosBolt.Cast();
                return;
            }
            //Cast Cataclysm
            if (MySettings.UseCataclysm && Cataclysm.IsSpellUsable &&
                !ObjectManager.Me.GetMove && Cataclysm.IsHostileDistanceGood &&
                //you have 4 or more Targets
                ObjectManager.Target.GetUnitInSpellRange(8f) >= 4)
            {
                Cataclysm.Cast();
                return;
            }
            //Cast Rain of Fire
            if (MySettings.UseRainofFire && RainofFire.IsSpellUsable &&
                !ObjectManager.Me.GetMove && RainofFire.IsHostileDistanceGood &&
                //you have 4 or more Targets
                ObjectManager.Target.GetUnitInSpellRange(8f) >= 4)
            {
                RainofFire.Cast();
                return;
            }
            //Summon Imp
            if (ObjectManager.Me.SoulShards > MySettings.UseSummonImpAbovePercentage && SummonImp.IsSpellUsable &&
                !ObjectManager.Me.GetMove)
            {
                SummonImp.Cast();
                return;
            }
            //Cast Conflagrate
            if (MySettings.UseConflagrate && Conflagrate.IsSpellUsable && Conflagrate.IsHostileDistanceGood)
            {
                Conflagrate.Cast();
                return;
            }
            //Cast Incinerate
            if (MySettings.UseIncinerate && Incinerate.IsSpellUsable &&
                !ObjectManager.Me.GetMove && Incinerate.IsHostileDistanceGood)
            {
                Incinerate.Cast();
                return;
            }
            //Channel Drain Life
            if (MySettings.UseDrainLife && DrainLife.IsSpellUsable &&
                !ObjectManager.Me.GetMove && DrainLife.IsHostileDistanceGood)
            {
                DrainLife.Cast();
                return;
            }
            //Cast Life Tap
            if (ObjectManager.Me.ManaPercentage < MySettings.UseLifeTapBelowPercentage &&
                ObjectManager.Me.HealthPercent > MySettings.UseLifeTapAbovePercentage &&
                LifeTap.IsSpellUsable)
            {
                LifeTap.Cast();
                return;
            }
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    #region Nested type: WarlockDestructionSettings

    [Serializable]
    public class WarlockDestructionSettings : Settings
    {
        /* Professions & Racials */
        //public bool UseArcaneTorrent = true;
        public bool UseBerserking = true;
        public bool UseBloodFury = true;
        public bool UseDarkflight = true;
        public int UseGiftoftheNaaruBelowPercentage = 50;
        public int UseStoneformBelowPercentage = 50;
        public int UseWarStompBelowPercentage = 50;

        /* Dots */
        public bool UseAgony = true;
        public bool UseCorruption = true;
        public bool UseSeedofCorruption = true;
        public bool UseSiphonLife = true;

        /* Artifact Spells */
        public bool UseDimensionalRift = true;

        /* Offensive Spells */
        public bool UseCataclysm = true;
        public bool UseChaosBolt = true;
        public bool UseConflagrate = true;
        public bool UseDrainLife = true;
        public bool UseImmolate = true;
        public bool UseIncinerate = true;
        public int UseManaTapAbovePercentage = 50;
        public bool UseRainofFire = true;
        public bool UseShadowburn = true;
        public int UseSummonImpAbovePercentage = 3;

        /* Offensive Cooldowns */
        public bool UseGrimoireImp = true;
        public bool UseGrimoireFelguard = false;
        public bool UseGrimoireFelhunter = false;
        public bool UseGrimoireofSacrifice = true;
        public bool UseGrimoireofService = true;
        public bool UseGrimoireSuccubus = false;
        public bool UseGrimoireVoidwalker = false;
        public bool UseSoulHarvest = true;
        public bool UseSummonDoomguard = true;
        public bool UseSummonInfernal = true;

        /* Defensive Spells */
        public int UseDarkPactBelowPercentage = 70;
        public int UseUnendingResolveBelowPercentage = 30;

        /* Healing Spells */
        public bool UseCreateHealthstone = true;
        public int UseHealthstoneBelowPercentage = 25;
        public int UseDrainLifeBelowPercentage = 25;

        /* Utility Spells */
        public int StartBurningRushAbovePercentage = 99;
        public int StopBurningRushBelowPercentage = 60;
        public int UseLifeTapAbovePercentage = 35;
        public int UseLifeTapBelowPercentage = 50;
        public bool UseSoulEffigy = true;

        /* Game Settings */
        public bool UseTrinketOne = true;
        public bool UseTrinketTwo = true;

        public WarlockDestructionSettings()
        {
            ConfigWinForm("Warlock Destruction Settings");
            /* Professions & Racials */
            //AddControlInWinForm("Use Arcane Torrent", "UseArcaneTorrent", "Professions & Racials");
            AddControlInWinForm("Use Berserking", "UseBerserking", "Professions & Racials");
            AddControlInWinForm("Use Blood Fury", "UseBloodFury", "Professions & Racials");
            AddControlInWinForm("Use Darkflight", "UseDarkflight", "Professions & Racials");
            AddControlInWinForm("Use Gift of the Naaru", "UseGiftoftheNaaruBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            AddControlInWinForm("Use Stone Form", "UseStoneformBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            AddControlInWinForm("Use War Stomp", "UseWarStompBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            /* Artifact Spells */
            AddControlInWinForm("Use Dimensional Rift", "UseDimensionalRift", "Artifact Spells");
            /* Offensive Spells */
            AddControlInWinForm("Use Cataclysm", "UseCataclysm", "Offensive Spells");
            AddControlInWinForm("Use Chaos Bolt", "UseChaosBolt", "Offensive Spells");
            AddControlInWinForm("Use Conflagrate", "UseConflagrate", "Offensive Spells");
            AddControlInWinForm("Use Drain Life", "UseDrainLife", "Offensive Spells");
            AddControlInWinForm("Use Immolate", "UseImmolate", "Offensive Spells");
            AddControlInWinForm("Use Incinerate", "UseIncinerate", "Offensive Spells");
            AddControlInWinForm("Use Mana Tap", "UseManaTapAbovePercentage", "Offensive Spells", "AbovePercentage", "Mana");
            AddControlInWinForm("Use Rain of Fire", "UseRainofFire", "Offensive Spells");
            AddControlInWinForm("Use Shadowburn", "UseShadowburn", "Offensive Spells");
            AddControlInWinForm("Use Mana Tap", "UseManaTapAbovePercentage", "Offensive Spells", "AbovePercentage", "Soul Shards");
            /* Offensive Cooldowns */
            AddControlInWinForm("Use Grimoire: Imp", "UseGrimoireImp", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Felguard", "UseGrimoireFelguard", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Felhunter", "UseGrimoireFelhunter", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Succubus", "UseGrimoireSuccubus", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire: Voidwalker", "UseGrimoireVoidwalker", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire of Sacrifice", "UseGrimoireofSacrifice", "Offensive Cooldowns");
            AddControlInWinForm("Use Grimoire of Service", "UseGrimoireofService", "Offensive Cooldowns");
            AddControlInWinForm("Use Soul Harvest", "UseSoulHarvest", "Offensive Cooldowns");
            AddControlInWinForm("Use Summon Doomguard", "UseSummonDoomguard", "Offensive Cooldowns");
            AddControlInWinForm("Use Summon Infernal", "UseSummonInfernal", "Offensive Cooldowns");
            /* Defensive Spells */
            AddControlInWinForm("Use Dark Pact", "UseDarkPactBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Unending Resolve", "UseUnendingResolveBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            /* Healing Spell */
            AddControlInWinForm("Use Create Healthstone", "UseCreateHealthstone", "Healing Spells");
            AddControlInWinForm("Use Healthstone", "UseHealthstoneBelowPercentage", "Healing Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Drain Life", "UseDrainLifeBelowPercentage", "Healing Spells", "BelowPercentage", "Life");
            /* Utility Spells */
            AddControlInWinForm("Start Burning Rush", "StartBurningRushAbovePercentage", "Utility Spells", "AbovePercentage", "Life");
            AddControlInWinForm("Stop Burning Rush", "StopBurningRushBelowPercentage", "Utility Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Life Tap", "UseLifeTapAbovePercentage", "Utility Spells", "AbovePercentage", "Life");
            AddControlInWinForm("Use Life Tap", "UseLifeTapBelowPercentage", "Utility Spells", "BelowPercentage", "Mana");
            AddControlInWinForm("Use Soul Effigy", "UseSoulEffigy", "Utility Spells");
            /* Game Settings */
            AddControlInWinForm("Use Trinket One", "UseTrinketOne", "Game Settings");
            AddControlInWinForm("Use Trinket Two", "UseTrinketTwo", "Game Settings");
        }

        public static WarlockDestructionSettings CurrentSetting { get; set; }

        public static WarlockDestructionSettings GetSettings()
        {
            string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warlock_Destruction.xml";
            if (File.Exists(currentSettingsFile))
            {
                return
                    CurrentSetting = Load<WarlockDestructionSettings>(currentSettingsFile);
            }
            return new WarlockDestructionSettings();
        }
    }

    #endregion
}

#endregion

// ReSharper restore ObjectCreationAsStatement
// ReSharper restore EmptyGeneralCatchClause