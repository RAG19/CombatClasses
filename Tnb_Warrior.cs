/*
* CombatClass for TheNoobBot
* Credit : Vesper, Neo2003, Dreadlocks
* Thanks you !
*/

using System;
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
                #region Warrior Specialisation checking

                case WoWClass.Warrior:

                if (wowSpecialization == WoWSpecialization.WarriorArms || wowSpecialization == WoWSpecialization.None)
                {
                    if (configOnly)
                    {
                        string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warrior_Arms.xml";
                        var currentSetting = new WarriorArms.WarriorArmsSettings();
                        if (File.Exists(currentSettingsFile) && !resetSettings)
                        {
                            currentSetting = Settings.Load<WarriorArms.WarriorArmsSettings>(currentSettingsFile);
                        }
                        currentSetting.ToForm();
                        currentSetting.Save(currentSettingsFile);
                    }
                    else
                    {
                        Logging.WriteFight("Loading Warrior Arms Combat class...");
                        EquipmentAndStats.SetPlayerSpe(WoWSpecialization.WarriorArms);
                        new WarriorArms();
                    }
                    break;
                }
                if (wowSpecialization == WoWSpecialization.WarriorFury)
                {
                    if (configOnly)
                    {
                        string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warrior_Fury.xml";
                        var currentSetting = new WarriorFury.WarriorFurySettings();
                        if (File.Exists(currentSettingsFile) && !resetSettings)
                        {
                            currentSetting = Settings.Load<WarriorFury.WarriorFurySettings>(currentSettingsFile);
                        }
                        currentSetting.ToForm();
                        currentSetting.Save(currentSettingsFile);
                    }
                    else
                    {
                        Logging.WriteFight("Loading Warrior Fury Combat class...");
                        EquipmentAndStats.SetPlayerSpe(WoWSpecialization.WarriorFury);
                        new WarriorFury();
                    }
                    break;
                }
                if (wowSpecialization == WoWSpecialization.WarriorProtection)
                {
                    if (configOnly)
                    {
                        string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warrior_Protection.xml";
                        var currentSetting = new WarriorProtection.WarriorProtectionSettings();
                        if (File.Exists(currentSettingsFile) && !resetSettings)
                        {
                            currentSetting = Settings.Load<WarriorProtection.WarriorProtectionSettings>(currentSettingsFile);
                        }
                        currentSetting.ToForm();
                        currentSetting.Save(currentSettingsFile);
                    }
                    else
                    {
                        Logging.WriteFight("Loading Warrior Protection Combat class...");
                        EquipmentAndStats.SetPlayerSpe(WoWSpecialization.WarriorProtection);
                        new WarriorProtection();
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

#region Warrior

public class WarriorArms
{
    private static WarriorArmsSettings MySettings = WarriorArmsSettings.GetSettings();

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

    private readonly Spell DeadlyCalm = new Spell("Deadly Calm");
    private readonly Spell FervorofBattle = new Spell("Fervor of Battle");

    #endregion

    #region Buffs

    private readonly Spell ShatteredDefensesBuff = new Spell(209706);
    private readonly Spell StoneHeartBuff = new Spell(225947);

    #endregion

    #region Dots

    //private readonly Spell ColossusSmashDot = new Spell(208086);

    #endregion

    #region Artifact Spells

    private readonly Spell Warbreaker = new Spell("Warbreaker");
    private readonly Spell ShatteredDefensesTrait = new Spell(209574);

    #endregion

    #region Offensive Spells

    private readonly Spell Charge = new Spell("Charge"); //No GCD
    private readonly Spell Cleave = new Spell("Cleave");
    private readonly Spell ColossusSmash = new Spell("Colossus Smash");
    private readonly Spell Execute = new Spell("Execute");
    private readonly Spell FocusedRage = new Spell("Focused Rage"); //No GCD //TESTING Does Buffstack work for a Buff which is also a Talent?
    private readonly Spell HeroicLeap = new Spell("Heroic Leap");
    private readonly Spell MortalStrike = new Spell("Mortal Strike");
    private readonly Spell Overpower = new Spell("Overpower");
    private readonly Spell Rend = new Spell("Rend"); //TESTING: Create seperate Dot version of the Spell for checks
    private readonly Spell Shockwave = new Spell("Shockwave");
    private readonly Spell Slam = new Spell("Slam");
    private readonly Spell StormBolt = new Spell("Storm Bolt");
    private readonly Spell Whirlwind = new Spell("Whirlwind");

    #endregion

    #region Offensive Cooldowns

    private readonly Spell Avatar = new Spell("Avatar"); //No GCD
    private readonly Spell BattleCry = new Spell("Battle Cry"); //No GCD
    private readonly Spell Bladestorm = new Spell("Bladestorm");
    //private readonly Spell Ravager = new Spell("Ravager");

    #endregion

    #region Defensive Spells

    private readonly Spell CommandingShout = new Spell("Commanding Shout"); //No GCD
    private readonly Spell DefensiveStance = new Spell("Defensive Stance"); //No GCD
    private readonly Spell DiebytheSword = new Spell("Die by the Sword"); //No GCD

    #endregion

    #region Utility Spells

    private readonly Spell Hamstring = new Spell("Hamstring"); //No GCD
    private readonly Spell IntimidatingShout = new Spell("Intimidating Shout");
    private readonly Spell Taunt = new Spell("Taunt"); //No GCD

    #endregion

    public WarriorArms()
    {
        Main.InternalRange = ObjectManager.Me.GetCombatReach;
        Main.InternalAggroRange = Main.InternalRange;
        Main.InternalLightHealingSpell = null;
        MySettings = WarriorArmsSettings.GetSettings();
        Main.DumpCurrentSettings<WarriorArmsSettings>(MySettings);
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
            if (!Darkflight.HaveBuff) // doesn't stack
            {
                if (MySettings.UseDarkflight && Darkflight.IsSpellUsable)
                {
                    Darkflight.Cast();
                    return;
                }
            }
        }
        else
        {
            //Self Heal for Damage Dealer
            if (nManager.Products.Products.ProductName == "Damage Dealer" && Main.InternalLightHealingSpell.IsSpellUsable &&
                ObjectManager.Me.HealthPercent < 90 && ObjectManager.Target.Guid == ObjectManager.Me.Guid)
            {
                Main.InternalLightHealingSpell.CastOnSelf();
                return;
            }
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
        if (Defensive() || AggroManagement() || Offensive())
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

            //Toggle Defensive Stance
            if (((ObjectManager.Me.HealthPercent < MySettings.UseDefensiveStanceBelowPercentage && !DefensiveStance.HaveBuff) ||
                (ObjectManager.Me.HealthPercent >= MySettings.UseDefensiveStanceBelowPercentage && DefensiveStance.HaveBuff)) &&
                DefensiveStance.IsSpellUsable)
            {
                DefensiveStance.Cast();
            }
            if (StunTimer.IsReady && (DefensiveTimer.IsReady || ObjectManager.Me.HealthPercent < 20))
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
            }
            //Mitigate Damage in Emergency Situations
            //Die by the Sword
            if (ObjectManager.Me.HealthPercent < MySettings.UseDiebytheSwordBelowPercentage && DiebytheSword.IsSpellUsable)
            {
                DiebytheSword.Cast();
                DefensiveTimer = new Timer(1000 * 8);
                return true;
            }
            //Commanding Shout
            if (ObjectManager.Me.HealthPercent < MySettings.UseCommandingShoutBelowPercentage && CommandingShout.IsSpellUsable)
            {
                CommandingShout.Cast();
                DefensiveTimer = new Timer(1000 * 8);
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
            //Apply Avatar
            if (MySettings.UseAvatar && Avatar.IsSpellUsable)
            {
                Avatar.Cast();
            }
            //Apply Battle Cry
            if (MySettings.UseBattleCry && BattleCry.IsSpellUsable)
            {
                BattleCry.Cast();
            }
            return false;
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    // For Spots (always return after Casting)
    private bool AggroManagement()
    {
        Usefuls.SleepGlobalCooldown();

        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            //Cast Taunt when you are in a party and the target of your target is a low health player
            if (MySettings.UseTauntBelowToTPercentage > 0 && Taunt.IsSpellUsable &&
                Taunt.IsHostileDistanceGood && !ObjectManager.Target.IsTargetingMe)
            {
                WoWObject obj = ObjectManager.GetObjectByGuid(ObjectManager.Target.Target);
                if (obj.IsValid && obj.Type == WoWObjectType.Player && 
                    new WoWPlayer(obj.GetBaseAddress).HealthPercent < MySettings.UseTauntBelowToTPercentage)
                {
                    Taunt.Cast();
                }
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

            //Special Rotation against 4 or more Targets
            if (ObjectManager.Me.GetUnitInSpellRange(8f) >= 4)
            {
                //Cast Warbreaker
                if (MySettings.UseWarbreaker && Warbreaker.IsSpellUsable)
                {
                    Warbreaker.Cast();
                    return;
                }
                //Cast Bladestorm when
                if (MySettings.UseBladestorm && Bladestorm.IsSpellUsable &&
                    //Colossus Smash Dot is active
                    ColossusSmash.TargetHaveBuffFromMe)
                {
                    Bladestorm.Cast();
                    return;
                }
                //Cast Cleave
                if (MySettings.UseCleave && Cleave.IsSpellUsable && HaveRage(10))
                {
                    Cleave.Cast();
                    return;
                }
                //Cast Cleave
                if (MySettings.UseWhirlwind && Whirlwind.IsSpellUsable && HaveRage(25))
                {
                    Whirlwind.Cast();
                    return;
                }
            }
            else
            {
                //Maintain Rend when
                if (MySettings.UseRend && Rend.IsSpellUsable &&
                    HaveRage(15) && Rend.IsHostileDistanceGood &&
                    ObjectManager.Target.AuraTimeLeft(Rend.Id, true) <= 1000 * 15 / 3 &&
                    //Colossus Smash Dot is absent
                    !ColossusSmash.TargetHaveBuffFromMe)
                {
                    Rend.Cast();
                    return;
                }
                //Cast Colossus Smash when
                if (MySettings.UseColossusSmash && ColossusSmash.IsSpellUsable && ColossusSmash.IsHostileDistanceGood &&
                    //Colossus Smash Dot is absent and Shattered Defenses Buff is not active.
                    !ColossusSmash.TargetHaveBuffFromMe && !ShatteredDefensesBuff.HaveBuff)
                {
                    ColossusSmash.Cast();
                    return;
                }
                //Cast Warbreaker when
                if (MySettings.UseWarbreaker && Warbreaker.IsSpellUsable && Warbreaker.IsHostileDistanceGood &&
                    //Colossus Smash Dot is absent and Shattered Defenses Buff is not active.
                    !ColossusSmash.TargetHaveBuffFromMe && !ShatteredDefensesBuff.HaveBuff)
                {
                    Warbreaker.Cast();
                    return;
                }
                //Cast Overpower (talented) when available.
                if (MySettings.UseOverpower && Overpower.IsSpellUsable &&
                    HaveRage(10) && Overpower.IsHostileDistanceGood)
                {
                    Overpower.Cast();
                    return;
                }
                //Cast Mortal Strike when
                if (MySettings.UseMortalStrike && MortalStrike.IsSpellUsable &&
                    HaveRage(20) && MortalStrike.IsHostileDistanceGood &&
                    //you have 3 Focused Rage Stacks
                    FocusedRage.BuffStack >= 3)
                {
                    MortalStrike.Cast();
                    return;
                }
                //Spend all Energy on Execute if possible.
                if (MySettings.UseExecute && Execute.IsSpellUsable && Execute.IsHostileDistanceGood &&
                    (ObjectManager.Target.HealthPercent < 20 || StoneHeartBuff.HaveBuff))
                {
                    if (HaveRage(40))
                    {
                        Execute.Cast();
                        return;
                    }
                }
                else
                {
                    //Cast Mortal Strike
                    if (MySettings.UseMortalStrike && MortalStrike.IsSpellUsable &&
                        HaveRage(20) && MortalStrike.IsHostileDistanceGood)
                    {
                        MortalStrike.Cast();
                        return;
                    }
                    //Cast Focused Rage when
                    if (MySettings.UseFocusedRage && FocusedRage.IsSpellUsable &&
                        HaveRage(15) && FocusedRage.IsHostileDistanceGood &&
                        //you have less than 3 Focused Rage Stacks
                        FocusedRage.BuffStack < 3)
                    {
                        FocusedRage.Cast();
                        return;
                    }
                    //Cast Whirlwind when
                    if (MySettings.UseWhirlwind && Whirlwind.IsSpellUsable &&
                        HaveRage(25) && Whirlwind.IsHostileDistanceGood &&
                        //you have the Fervor of Battle Talent or it hits multiple Targets
                        (FervorofBattle.HaveBuff || ObjectManager.Me.GetUnitInSpellRange(8f) > 1))
                    {
                        Whirlwind.Cast();
                        return;
                    }
                    //Cast Slam when
                    if (MySettings.UseSlam && Slam.IsSpellUsable &&
                        HaveRage(20) && Slam.IsHostileDistanceGood &&
                        //you don't have the Fervor of Battle Talent
                        !FervorofBattle.HaveBuff)
                    {
                        Slam.Cast();
                        return;
                    }
                }
            }

            //Cast Charge
            if (MySettings.UseCharge && Charge.IsSpellUsable && Charge.IsHostileDistanceGood)
            {
                Charge.Cast();
                return;
            }
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    //Check Rage costs
    private bool HaveRage(int cost)
    {
        return ObjectManager.Me.Rage >= cost || (DeadlyCalm.HaveBuff && BattleCry.HaveBuff);
    }

    #region Nested type: WarriorArmsSettings

    [Serializable]
    public class WarriorArmsSettings : Settings
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
        public bool UseWarbreaker = true;

        /* Offensive Spells */
        public bool UseCharge = true;
        public bool UseCleave = true;
        public bool UseColossusSmash = true;
        public bool UseExecute = true;
        public bool UseFocusedRage = true;
        public bool UseMortalStrike = true;
        public bool UseOverpower = true;
        public bool UseRend = true;
        public bool UseSlam = true;
        public bool UseWhirlwind = true;

        /* Offensive Cooldowns */
        public bool UseAvatar = true;
        public bool UseBattleCry = true;
        public bool UseBladestorm = true;

        /* Defensive Spells */
        public int UseCommandingShoutBelowPercentage = 50;
        public int UseDefensiveStanceBelowPercentage = 50;
        public int UseDiebytheSwordBelowPercentage = 30;

        /* Utility Spells */
        public int UseTauntBelowToTPercentage = 20;

        /* Game Settings */
        public bool UseTrinketOne = true;
        public bool UseTrinketTwo = true;

        public WarriorArmsSettings()
        {
            ConfigWinForm("Warrior Arms Settings");
            /* Professions & Racials */
            //AddControlInWinForm("Use Arcane Torrent", "UseArcaneTorrent", "Professions & Racials");
            AddControlInWinForm("Use Berserking", "UseBerserking", "Professions & Racials");
            AddControlInWinForm("Use Blood Fury", "UseBloodFury", "Professions & Racials");
            AddControlInWinForm("Use Darkflight", "UseDarkflight", "Professions & Racials");
            AddControlInWinForm("Use Gift of the Naaru", "UseGiftoftheNaaruBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            AddControlInWinForm("Use Stone Form", "UseStoneformBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            AddControlInWinForm("Use War Stomp", "UseWarStompBelowPercentage", "Professions & Racials", "BelowPercentage", "Life");
            /* Artifact Spells */
            AddControlInWinForm("Use Warbreaker", "UseWarbreaker", "Artifact Spells");
            /* Offensive Spells */
            AddControlInWinForm("Use Charge", "UseCharge", "Offensive Spells");
            AddControlInWinForm("Use Cleave", "UseCleave", "Offensive Spells");
            AddControlInWinForm("Use Colossus Smash", "UseColossusSmash", "Offensive Spells");
            AddControlInWinForm("Use Execute", "UseExecute", "Offensive Spells");
            AddControlInWinForm("Use Focused Rage", "UseFocusedRage", "Offensive Spells");
            AddControlInWinForm("Use Mortal Strike", "UseMortalStrike", "Offensive Spells");
            AddControlInWinForm("Use Overpower", "UseOverpower", "Offensive Spells");
            AddControlInWinForm("Use Rend", "UseRend", "Offensive Spells");
            AddControlInWinForm("Use Slam", "UseSlam", "Offensive Spells");
            AddControlInWinForm("Use Whirlwind", "UseWhirlwind", "Offensive Spells");
            /* Offensive Cooldowns */
            AddControlInWinForm("Use Avatar", "UseAvatar", "Offensive Cooldowns");
            AddControlInWinForm("Use Battle Cry", "UseBattleCry", "Offensive Cooldowns");
            AddControlInWinForm("Use Bladestorm", "UseBladestorm", "Offensive Cooldowns");
            /* Defensive Spells */
            AddControlInWinForm("Use Commanding Shout", "UseCommandingShoutBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Defensive Stance", "UseDefensiveStanceBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            AddControlInWinForm("Use Die by the Sword", "UseDiebytheSwordBelowPercentage", "Defensive Spells", "BelowPercentage", "Life");
            /* Utility Spells */
            AddControlInWinForm("Use Taunt", "UseTauntBelowToTPercentage", "Utility Spells", "BelowPercentage", "Target of Target Life");
            /* Game Settings */
            AddControlInWinForm("Use Trinket One", "UseTrinketOne", "Game Settings");
            AddControlInWinForm("Use Trinket Two", "UseTrinketTwo", "Game Settings");
        }

        public static WarriorArmsSettings CurrentSetting { get; set; }

        public static WarriorArmsSettings GetSettings()
        {
            string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warrior_Arms.xml";
            if (File.Exists(currentSettingsFile))
            {
                return
                    CurrentSetting = Load<WarriorArmsSettings>(currentSettingsFile);
            }
            return new WarriorArmsSettings();
        }
    }

    #endregion
}

public class WarriorProtection
{
    private static WarriorProtectionSettings MySettings = WarriorProtectionSettings.GetSettings();

    #region General Timers & Variables

    private readonly WoWItem _firstTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET);
    private readonly WoWItem _secondTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET, 2);
    public int LC = 0;

    private Timer _onCd = new Timer(0);

    #endregion

    #region Professions & Racials

    public readonly Spell Alchemy = new Spell("Alchemy");
    public readonly Spell ArcaneTorrent = new Spell("Arcane Torrent");
    public readonly Spell Berserking = new Spell("Berserking");
    public readonly Spell BloodFury = new Spell("Blood Fury");

    public readonly Spell GiftoftheNaaru = new Spell("Gift of the Naaru");

    public readonly Spell Stoneform = new Spell("Stoneform");
    public readonly Spell WarStomp = new Spell("War Stomp");

    #endregion

    #region Warrior Buffs

    public readonly Spell BattleShout = new Spell("Battle Shout");
    public readonly Spell BattleStance = new Spell("Battle Stance");
    public readonly Spell BerserkerStance = new Spell("Berserker Stance");
    public readonly Spell CommandingShout = new Spell("Commanding Shout");
    public readonly Spell DefensiveStance = new Spell("Defensive Stance");

    #endregion

    #region Offensive Spell

    public readonly Spell Avatar = new Spell("Avatar");
    public readonly Spell Bladestorm = new Spell("Bladestorm");
    public readonly Spell Bloodbath = new Spell("Bloodbath");
    public readonly Spell Charge = new Spell("Charge");
    public readonly Spell Cleave = new Spell("Cleave");
    public readonly Spell Devastate = new Spell("Devastate");
    public readonly Spell DragonRoar = new Spell("Dragon Roar");
    public readonly Spell Execute = new Spell("Execute");
    public readonly Spell HeroicLeap = new Spell("Heroic Leap");
    public readonly Spell HeroicStrike = new Spell("Heroic Strike");
    public readonly Spell HeroicThrow = new Spell("Heroic Throw");
    public readonly Spell Revenge = new Spell("Revenge");
    public readonly Spell ShieldSlam = new Spell("Shield Slam");
    public readonly Spell Shockwave = new Spell("Shockwave");
    public readonly Spell StormBolt = new Spell("Storm Bolt");
    public readonly Spell SunderArmor = new Spell("Sunder Armor");
    public readonly Spell Taunt = new Spell("Taunt");
    public readonly Spell ThunderClap = new Spell("Thunder Clap");

    #endregion

    #region Offensive Cooldown

    public readonly Spell BerserkerRage = new Spell("Berserker Rage");
    public readonly Spell DeadlyCalm = new Spell("Deadly Calm");
    public readonly Spell Recklessness = new Spell("Recklessness");
    public readonly Spell ShatteringThrow = new Spell("Shattering Throw");
    public readonly Spell SkullBanner = new Spell("Skull Banner");
    public readonly Spell SweepingStrikes = new Spell("Sweeping Strikes");

    #endregion

    #region Defensive Cooldown

    public readonly Spell DemoralizingBanner = new Spell("Demoralizing Banner");
    public readonly Spell DemoralizingShout = new Spell("Demoralizing Shout");
    public readonly Spell Disarm = new Spell("Disarm");
    public readonly Spell DisruptingShout = new Spell("Disrupting Shout");
    public readonly Spell Hamstring = new Spell("Hamstring");
    public readonly Spell IntimidatingShout = new Spell("Intimidating Shout");
    public readonly Spell MassSpellReflection = new Spell("Mass Spell Reflection");
    public readonly Spell PiercingHowl = new Spell("Piercing Howl");
    public readonly Spell Pummel = new Spell("Pummel");
    public readonly Spell ShieldBarrier = new Spell("Shield Barrier");
    public readonly Spell ShieldBlock = new Spell("Shield Block");
    public readonly Spell ShieldWall = new Spell("Shield Wall");
    public readonly Spell SpellReflection = new Spell("Spell Reflection");
    public readonly Spell StaggeringShout = new Spell("Staggering Shout");
    private Timer _disarmTimer = new Timer(0);
    private Timer _shieldBarrierTimer = new Timer(0);

    #endregion

    #region Healing Spell

    public readonly Spell EnragedRegeneration = new Spell("Enraged Regeneration");
    public readonly Spell LastStand = new Spell("Last Stand");
    public readonly Spell RallyingCry = new Spell("Rallying Cry");
    public readonly Spell VictoryRush = new Spell("Victory Rush");

    #endregion

    public WarriorProtection()
    {
        Main.InternalRange = ObjectManager.Me.GetCombatReach;
        Main.InternalAggroRange = Main.InternalRange;
        Main.InternalLightHealingSpell = null;
        MySettings = WarriorProtectionSettings.GetSettings();
        Main.DumpCurrentSettings<WarriorProtectionSettings>(MySettings);
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
                            if (ObjectManager.Me.Target != lastTarget
                                && HeroicStrike.IsHostileDistanceGood)
                            {
                                Pull();
                                lastTarget = ObjectManager.Me.Target;
                            }

                            if (ObjectManager.Target.Level < 70 && ObjectManager.Me.Level > 84
                                && MySettings.UseLowCombat)
                            {
                                LC = 1;
                                if (ObjectManager.Target.GetDistance < 30)
                                    LowCombat();
                            }
                            else
                            {
                                LC = 0;
                                if (ObjectManager.Target.GetDistance < 30)
                                    Combat();
                            }
                        }
                        if (!ObjectManager.Me.IsCast)
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

    private void Pull()
    {
        if (HeroicLeap.IsHostileDistanceGood && HeroicLeap.KnownSpell && HeroicLeap.IsSpellUsable
            && MySettings.UseHeroicLeap)
        {
            SpellManager.CastSpellByIDAndPosition(6544, ObjectManager.Target.Position);
            Others.SafeSleep(200);
        }

        if (Taunt.IsHostileDistanceGood && Taunt.KnownSpell && Taunt.IsSpellUsable
            && MySettings.UseTaunt && ObjectManager.Target.GetDistance > 20)
        {
            Taunt.Cast();
        }
    }

    private void LowCombat()
    {
        Buff();
        if (MySettings.DoAvoidMelee)
            AvoidMelee();
        DefenseCycle();
        Heal();

        if (HeroicThrow.KnownSpell && HeroicThrow.IsSpellUsable && HeroicThrow.IsHostileDistanceGood
            && MySettings.UseHeroicThrow && !ObjectManager.Target.InCombat)
        {
            HeroicThrow.Cast();
            return;
        }

        if (Charge.KnownSpell && Charge.IsSpellUsable && Charge.IsHostileDistanceGood
            && MySettings.UseCharge && ObjectManager.Target.GetDistance > Main.InternalRange)
        {
            Charge.Cast();
            return;
        }

        if (ShieldSlam.KnownSpell && ShieldSlam.IsSpellUsable && ShieldSlam.IsHostileDistanceGood
            && ObjectManager.Me.RagePercentage < 95 && MySettings.UseShieldSlam)
        {
            ShieldSlam.Cast();
            return;
        }
        if (HeroicStrike.KnownSpell && HeroicStrike.IsSpellUsable && HeroicStrike.IsHostileDistanceGood && MySettings.UseHeroicStrike &&
            (ObjectManager.Me.RagePercentage > 80 || ObjectManager.Me.HaveBuff(122510)))
        {
            if (ObjectManager.Me.HealthPercent > 80)
            {
                if (DeadlyCalm.KnownSpell && DeadlyCalm.IsSpellUsable && MySettings.UseDeadlyCalm)
                {
                    DeadlyCalm.Cast();
                    Others.SafeSleep(200);
                }
                HeroicStrike.Cast();
                return;
            }
            return;
        }
        if (Revenge.KnownSpell && Revenge.IsHostileDistanceGood && Revenge.IsSpellUsable
            && ObjectManager.Me.RagePercentage < 95 && MySettings.UseRevenge)
        {
            Revenge.Cast();
            return;
        }
        if (Shockwave.KnownSpell && Shockwave.IsSpellUsable && Shockwave.IsHostileDistanceGood
            && MySettings.UseShockwave)
        {
            Shockwave.Cast();
            return;
        }
        if (DragonRoar.KnownSpell && DragonRoar.IsSpellUsable && DragonRoar.IsHostileDistanceGood
            && MySettings.UseDragonRoar)
        {
            DragonRoar.Cast();
            return;
        }
        if (Bladestorm.KnownSpell && Bladestorm.IsSpellUsable && Bladestorm.IsHostileDistanceGood
            && MySettings.UseBladestorm)
        {
            Bladestorm.Cast();
            return;
        }
        // Blizzard API Calls for Devastate using Sunder Armor Function
        if (SunderArmor.KnownSpell && SunderArmor.IsSpellUsable && SunderArmor.IsHostileDistanceGood
            && MySettings.UseDevastate)
        {
            SunderArmor.Cast();
            return;
        }
        if (ThunderClap.KnownSpell && ThunderClap.IsSpellUsable && ThunderClap.IsHostileDistanceGood
            && MySettings.UseThunderClap)
        {
            ThunderClap.Cast();
        }
    }

    private void Combat()
    {
        Buff();
        DPSBurst();
        if (MySettings.DoAvoidMelee)
            AvoidMelee();
        DPSCycle();
        Decast();
        if (_onCd.IsReady)
            DefenseCycle();
        Heal();
    }

    private void Buff()
    {
        if (ObjectManager.Me.IsMounted)
            return;

        if (MySettings.UseDefensiveStance && DefensiveStance.KnownSpell && DefensiveStance.IsSpellUsable
            && !DefensiveStance.HaveBuff && LC != 1)
        {
            DefensiveStance.Cast();
            return;
        }
        if (!BattleStance.HaveBuff && BattleStance.KnownSpell && BattleStance.IsSpellUsable
            && MySettings.UseBattleStance && LC == 1)
        {
            BattleStance.Cast();
            return;
        }
        if (!BerserkerStance.HaveBuff && BerserkerStance.KnownSpell && BerserkerStance.IsSpellUsable
            && MySettings.UseBerserkerStance && !MySettings.UseBattleStance && !MySettings.UseDefensiveStance)
        {
            BerserkerStance.Cast();
            return;
        }
        if (BattleShout.KnownSpell && BattleShout.IsSpellUsable && !BattleShout.HaveBuff
            && MySettings.UseBattleShout)
        {
            BattleShout.Cast();
            return;
        }
        if (CommandingShout.KnownSpell && CommandingShout.IsSpellUsable && !CommandingShout.HaveBuff
            && MySettings.UseCommandingShout && !MySettings.UseBattleShout)
        {
            CommandingShout.Cast();
            return;
        }
        if (MySettings.UseAlchFlask && !ObjectManager.Me.HaveBuff(79638) && !ObjectManager.Me.HaveBuff(79640) && !ObjectManager.Me.HaveBuff(79639)
            && !ItemsManager.IsItemOnCooldown(75525) && ItemsManager.GetItemCount(75525) > 0)
        {
            ItemsManager.UseItem(75525);
        }
    }

    private void AvoidMelee()
    {
        if (ObjectManager.Target.GetDistance < MySettings.DoAvoidMeleeDistance && ObjectManager.Target.InCombat)
        {
            Logging.WriteFight("Too Close. Moving Back");
            var maxTimeTimer = new Timer(1000 * 2);
            MovementsAction.MoveBackward(true);
            while (ObjectManager.Target.GetDistance < 2 && ObjectManager.Target.InCombat && !maxTimeTimer.IsReady)
                Others.SafeSleep(300);
            MovementsAction.MoveBackward(false);
            if (maxTimeTimer.IsReady && ObjectManager.Target.GetDistance < 2 && ObjectManager.Target.InCombat)
            {
                MovementsAction.MoveForward(true);
                Others.SafeSleep(1000);
                MovementsAction.MoveForward(false);
                MovementManager.Face(ObjectManager.Target.Position);
            }
        }
    }

    private void DefenseCycle()
    {
        if (ObjectManager.Me.HealthPercent < 95 && MySettings.UseDisarm && Disarm.IsHostileDistanceGood
            && Disarm.KnownSpell && Disarm.IsSpellUsable && _disarmTimer.IsReady)
        {
            Disarm.Cast();
            _disarmTimer = new Timer(1000 * 60);
            return;
        }
        if (ObjectManager.Me.HealthPercent < 20 && MySettings.UseIntimidatingShout
            && IntimidatingShout.KnownSpell && IntimidatingShout.IsSpellUsable &&
            ObjectManager.Target.GetDistance < 8)
        {
            IntimidatingShout.Cast();
            _onCd = new Timer(1000 * 8);
            return;
        }
        if (ObjectManager.Me.HealthPercent < 60 && ShieldWall.KnownSpell && ShieldWall.IsSpellUsable
            && MySettings.UseShieldWall)
        {
            ShieldWall.Cast();
            _onCd = new Timer(1000 * 12);
            return;
        }
        if (ObjectManager.Me.HealthPercent < 80 && MySettings.UseDemoralizingBanner
            && DemoralizingBanner.KnownSpell && DemoralizingBanner.IsSpellUsable &&
            ObjectManager.Target.GetDistance < 30)
        {
            SpellManager.CastSpellByIDAndPosition(114203, ObjectManager.Target.Position);
            _onCd = new Timer(1000 * 15);
            return;
        }
        if (ObjectManager.Me.HealthPercent < 90 && MySettings.UseDemoralizingShout
            && DemoralizingShout.KnownSpell && DemoralizingShout.IsSpellUsable &&
            ObjectManager.Target.GetDistance < 30)
        {
            DemoralizingShout.Cast();
            _onCd = new Timer(1000 * 10);
            return;
        }
        if (ObjectManager.Me.HealthPercent <= MySettings.UseWarStompAtPercentage && WarStomp.IsSpellUsable &&
            WarStomp.KnownSpell
            && MySettings.UseWarStomp)
        {
            WarStomp.Cast();
            _onCd = new Timer(1000 * 2);
            return;
        }
        if (ObjectManager.Me.HealthPercent <= MySettings.UseStoneformAtPercentage && Stoneform.IsSpellUsable &&
            Stoneform.KnownSpell
            && MySettings.UseStoneform)
        {
            Stoneform.Cast();
            _onCd = new Timer(1000 * 8);
            return;
        }
        if (ObjectManager.Me.HealthPercent < 80 && ShieldBlock.KnownSpell && ShieldBlock.IsSpellUsable
            && MySettings.UseShieldBlock)
        {
            ShieldBlock.Cast();
            _onCd = new Timer(1000 * 6);
        }
    }

    private void Heal()
    {
        if (ObjectManager.Me.IsMounted)
            return;

        if (VictoryRush.KnownSpell && VictoryRush.IsSpellUsable && VictoryRush.IsHostileDistanceGood
            && MySettings.UseVictoryRush && ObjectManager.Me.HealthPercent < 90)
        {
            VictoryRush.Cast();
            return;
        }
        if (ObjectManager.Me.HealthPercent < 30 && LastStand.IsSpellUsable && LastStand.KnownSpell
            && MySettings.UseLastStand && ObjectManager.Me.InCombat)
        {
            LastStand.Cast();
            return;
        }
        if (ObjectManager.Me.HealthPercent < 30 && RallyingCry.IsSpellUsable && RallyingCry.KnownSpell
            && MySettings.UseRallyingCry && ObjectManager.Me.InCombat && !LastStand.HaveBuff)
        {
            RallyingCry.Cast();
            return;
        }
        if (ObjectManager.Me.HealthPercent <= MySettings.UseGiftoftheNaaruAtPercentage &&
            GiftoftheNaaru.IsSpellUsable && GiftoftheNaaru.KnownSpell
            && MySettings.UseGiftoftheNaaru)
        {
            GiftoftheNaaru.Cast();
            return;
        }
        if (ObjectManager.Me.HealthPercent < 80 && EnragedRegeneration.IsSpellUsable &&
            EnragedRegeneration.KnownSpell
            && MySettings.UseEnragedRegeneration)
        {
            EnragedRegeneration.Cast();
        }
    }

    private void Decast()
    {
        if (ArcaneTorrent.IsSpellUsable && ArcaneTorrent.KnownSpell && ObjectManager.Target.GetDistance < 8
            && ObjectManager.Me.HealthPercent <= MySettings.UseArcaneTorrentForDecastAtPercentage
            && MySettings.UseArcaneTorrentForDecast && ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe)
        {
            ArcaneTorrent.Cast();
            return;
        }
        if (!Hamstring.TargetHaveBuff && MySettings.UseHamstring && Hamstring.KnownSpell
            && Hamstring.IsSpellUsable && Hamstring.IsHostileDistanceGood)
        {
            Hamstring.Cast();
            return;
        }
        if (ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe && Pummel.IsHostileDistanceGood && Pummel.KnownSpell && Pummel.IsSpellUsable &&
            MySettings.UsePummel)
        {
            Pummel.Cast();
            return;
        }
        if (ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe && ObjectManager.Target.GetDistance < 10 && DisruptingShout.KnownSpell &&
            DisruptingShout.IsSpellUsable && MySettings.UseDisruptingShout)
        {
            DisruptingShout.Cast();
            return;
        }
        if (ObjectManager.Target.GetMove && !PiercingHowl.TargetHaveBuff && MySettings.UsePiercingHowl && PiercingHowl.KnownSpell && PiercingHowl.IsSpellUsable &&
            ObjectManager.Target.GetDistance < 15)
        {
            PiercingHowl.Cast();
            return;
        }
        if (Hamstring.TargetHaveBuff && MySettings.UseStaggeringShout && StaggeringShout.KnownSpell && StaggeringShout.IsSpellUsable && ObjectManager.Target.GetDistance < 20)
        {
            StaggeringShout.Cast();
            return;
        }
        if (ObjectManager.Target.IsCast && SpellReflection.KnownSpell && SpellReflection.IsSpellUsable && MySettings.UseSpellReflection)
        {
            SpellReflection.Cast();
            return;
        }
        if (ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe && MySettings.UseMassSpellReflection && MassSpellReflection.KnownSpell &&
            MassSpellReflection.IsSpellUsable)
        {
            MassSpellReflection.Cast();
            return;
        }
        if (ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe && ObjectManager.Me.HealthPercent < 80 && ShieldBarrier.KnownSpell && ShieldBarrier.IsSpellUsable &&
            MySettings.UseShieldBarrier && _shieldBarrierTimer.IsReady)
        {
            ShieldBarrier.Cast();
            _shieldBarrierTimer = new Timer(1000 * 6);
        }
    }

    private void DPSBurst()
    {
        if (MySettings.UseTrinketOne && !ItemsManager.IsItemOnCooldown(_firstTrinket.Entry) && ItemsManager.IsItemUsable(_firstTrinket.Entry))
        {
            ItemsManager.UseItem(_firstTrinket.Name);
            Logging.WriteFight("Use First Trinket Slot");
        }

        if (MySettings.UseTrinketTwo && !ItemsManager.IsItemOnCooldown(_secondTrinket.Entry) && ItemsManager.IsItemUsable(_secondTrinket.Entry))
        {
            ItemsManager.UseItem(_secondTrinket.Name);
            Logging.WriteFight("Use Second Trinket Slot");
            return;
        }
        if (Berserking.IsSpellUsable && Berserking.KnownSpell && ObjectManager.Target.GetDistance < 30
            && MySettings.UseBerserking)
        {
            Berserking.Cast();
            return;
        }
        if (BloodFury.IsSpellUsable && BloodFury.KnownSpell && ObjectManager.Target.GetDistance < 30
            && MySettings.UseBloodFury)
        {
            BloodFury.Cast();
            return;
        }
        if (BerserkerRage.KnownSpell && BerserkerRage.IsSpellUsable && ObjectManager.Me.RagePercentage < 50
            && MySettings.UseBerserkerRage && ObjectManager.Target.GetDistance < 30)
        {
            BerserkerRage.Cast();
            return;
        }
        if (Recklessness.KnownSpell && Recklessness.IsSpellUsable && MySettings.UseRecklessness && ObjectManager.Target.GetDistance < 30)
        {
            Recklessness.Cast();
            return;
        }
        if (ShatteringThrow.KnownSpell && ShatteringThrow.IsSpellUsable && ShatteringThrow.IsHostileDistanceGood && MySettings.UseShatteringThrow)
        {
            ShatteringThrow.Cast();
            return;
        }
        if (SkullBanner.KnownSpell && SkullBanner.IsSpellUsable && MySettings.UseSkullBanner && ObjectManager.Target.GetDistance < 30)
        {
            SkullBanner.Cast();
            return;
        }
        if (Avatar.KnownSpell && Avatar.IsSpellUsable && MySettings.UseAvatar && ObjectManager.Target.GetDistance < 30)
        {
            Avatar.Cast();
            return;
        }
        if (Bloodbath.KnownSpell && Bloodbath.IsSpellUsable && MySettings.UseBloodbath && ObjectManager.Target.GetDistance < 30)
        {
            Bloodbath.Cast();
            return;
        }
        if (StormBolt.KnownSpell && StormBolt.IsSpellUsable && MySettings.UseStormBolt && StormBolt.IsHostileDistanceGood)
        {
            StormBolt.Cast();
        }
    }

    private void DPSCycle()
    {
        Usefuls.SleepGlobalCooldown();
        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (HeroicThrow.KnownSpell && HeroicThrow.IsSpellUsable && HeroicThrow.IsHostileDistanceGood
                && MySettings.UseHeroicThrow && !ObjectManager.Target.InCombat)
            {
                HeroicThrow.Cast();
                return;
            }

            if (Charge.KnownSpell && Charge.IsSpellUsable && Charge.IsHostileDistanceGood
                && MySettings.UseCharge && ObjectManager.Target.GetDistance > Main.InternalRange)
            {
                Charge.Cast();
                return;
            }

            if (VictoryRush.KnownSpell && VictoryRush.IsSpellUsable && VictoryRush.IsHostileDistanceGood
                && MySettings.UseVictoryRush && ObjectManager.Me.HealthPercent < 90)
            {
                VictoryRush.Cast();
                return;
            }

            if (ObjectManager.GetNumberAttackPlayer() > 2 && ThunderClap.KnownSpell && ThunderClap.IsSpellUsable
                && ThunderClap.IsHostileDistanceGood && MySettings.UseThunderClap)
            {
                ThunderClap.Cast();
                return;
            }

            if (Cleave.KnownSpell && Cleave.IsSpellUsable && Cleave.IsHostileDistanceGood &&
                ObjectManager.GetNumberAttackPlayer() > 2
                && MySettings.UseCleave && (ObjectManager.Me.RagePercentage > 80 || ObjectManager.Me.HaveBuff(122510)))
            {
                if (ObjectManager.Me.HealthPercent > 80)
                {
                    if (DeadlyCalm.KnownSpell && DeadlyCalm.IsSpellUsable && MySettings.UseDeadlyCalm)
                    {
                        DeadlyCalm.Cast();
                        Others.SafeSleep(200);
                    }
                    Cleave.Cast();
                    return;
                }
            }

            else
            {
                if (HeroicStrike.KnownSpell && HeroicStrike.IsSpellUsable && HeroicStrike.IsHostileDistanceGood
                    && MySettings.UseHeroicStrike &&
                    (ObjectManager.Me.RagePercentage > 80 || ObjectManager.Me.HaveBuff(122510)))
                {
                    if (ObjectManager.Me.HealthPercent > 80)
                    {
                        if (DeadlyCalm.KnownSpell && DeadlyCalm.IsSpellUsable && MySettings.UseDeadlyCalm)
                        {
                            DeadlyCalm.Cast();
                            Others.SafeSleep(200);
                        }
                        HeroicStrike.Cast();
                        return;
                    }
                }
            }

            if (ShieldSlam.KnownSpell && ShieldSlam.IsSpellUsable && ShieldSlam.IsHostileDistanceGood
                && MySettings.UseShieldSlam && ObjectManager.Me.RagePercentage < 95)
            {
                ShieldSlam.Cast();
                return;
            }
            if (Revenge.KnownSpell && Revenge.IsHostileDistanceGood && Revenge.IsSpellUsable
                && MySettings.UseRevenge && ObjectManager.Me.RagePercentage < 95)
            {
                Revenge.Cast();
                return;
            }
            if (Shockwave.KnownSpell && Shockwave.IsSpellUsable && Shockwave.IsHostileDistanceGood
                && MySettings.UseShockwave)
            {
                Shockwave.Cast();
                return;
            }
            if (DragonRoar.KnownSpell && DragonRoar.IsSpellUsable && DragonRoar.IsHostileDistanceGood
                && MySettings.UseDragonRoar)
            {
                Shockwave.Cast();
                return;
            }
            if (Bladestorm.KnownSpell && Bladestorm.IsSpellUsable && Bladestorm.IsHostileDistanceGood
                && MySettings.UseBladestorm)
            {
                Bladestorm.Cast();
                return;
            }
            if (ThunderClap.KnownSpell && ThunderClap.IsSpellUsable && ThunderClap.IsHostileDistanceGood
                && MySettings.UseThunderClap && !ObjectManager.Target.HaveBuff(115798))
            {
                ThunderClap.Cast();
                return;
            }
            if (SunderArmor.KnownSpell && SunderArmor.IsSpellUsable && SunderArmor.IsHostileDistanceGood
                && MySettings.UseDevastate)
            {
                SunderArmor.Cast();
                return;
            }
            if (ArcaneTorrent.IsSpellUsable && ArcaneTorrent.KnownSpell
                && MySettings.UseArcaneTorrentForResource)
            {
                ArcaneTorrent.Cast();
            }
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    private void Patrolling()
    {
        if (ObjectManager.Me.IsMounted)
            return;
        Buff();
        Heal();
    }

    #region Nested type: WarriorProtectionSettings

    [Serializable]
    public class WarriorProtectionSettings : Settings
    {
        public bool DoAvoidMelee = false;
        public int DoAvoidMeleeDistance = 0;
        public bool UseAlchFlask = true;
        public bool UseArcaneTorrentForDecast = true;
        public int UseArcaneTorrentForDecastAtPercentage = 100;
        public bool UseArcaneTorrentForResource = true;
        public bool UseAvatar = true;
        public bool UseBattleShout = true;
        public bool UseBattleStance = true;
        public bool UseBerserkerRage = true;
        public bool UseBerserkerStance = false;
        public bool UseBerserking = true;
        public bool UseBladestorm = true;
        public bool UseBloodFury = true;
        public bool UseBloodbath = true;
        public bool UseCharge = true;
        public bool UseCleave = true;
        public bool UseCommandingShout = false;
        public bool UseDeadlyCalm = true;
        public bool UseDefensiveStance = true;
        public bool UseDemoralizingBanner = true;
        public bool UseDemoralizingShout = true;
        public bool UseDevastate = true;
        public bool UseDisarm = true;
        public bool UseDisruptingShout = true;
        public bool UseDragonRoar = true;

        public bool UseEnragedRegeneration = true;
        public bool UseExecute = true;
        public bool UseGiftoftheNaaru = true;
        public int UseGiftoftheNaaruAtPercentage = 80;
        public bool UseHamstring = false;
        public bool UseHeroicLeap = true;
        public bool UseHeroicStrike = true;
        public bool UseHeroicThrow = true;
        public bool UseIntimidatingShout = true;
        public bool UseLastStand = true;

        public bool UseLowCombat = true;
        public bool UseMassSpellReflection = true;
        public bool UsePiercingHowl = false;
        public bool UsePummel = true;
        public bool UseRallyingCry = true;
        public bool UseRecklessness = true;
        public bool UseRevenge = true;
        public bool UseShatteringThrow = true;
        public bool UseShieldBarrier = true;
        public bool UseShieldBlock = true;
        public bool UseShieldSlam = true;
        public bool UseShieldWall = true;
        public bool UseShockwave = true;
        public bool UseSkullBanner = true;
        public bool UseSpellReflection = true;
        public bool UseStaggeringShout = true;
        public bool UseStoneform = true;
        public int UseStoneformAtPercentage = 80;
        public bool UseStormBolt = true;
        public bool UseSweepingStrikes = true;
        public bool UseTaunt = true;
        public bool UseThunderClap = true;
        public bool UseTrinketOne = true;
        public bool UseTrinketTwo = true;
        public bool UseVictoryRush = true;
        public bool UseWarStomp = true;
        public int UseWarStompAtPercentage = 80;

        public WarriorProtectionSettings()
        {
            ConfigWinForm("Warrior Protection Settings");
            /* Professions & Racials */
            AddControlInWinForm("Use Arcane Torrent for Interrupt", "UseArcaneTorrentForDecast", "Professions & Racials", "AtPercentage");
            AddControlInWinForm("Use Arcane Torrent for Resource", "UseArcaneTorrentForResource", "Professions & Racials");
            AddControlInWinForm("Use Berserking", "UseBerserking", "Professions & Racials");
            AddControlInWinForm("Use Blood Fury", "UseBloodFury", "Professions & Racials");
            AddControlInWinForm("Use Gift of the Naaru", "UseGiftoftheNaaru", "Professions & Racials");

            AddControlInWinForm("Use Stoneform", "UseStoneform", "Professions & Racials");
            AddControlInWinForm("Use War Stomp", "UseWarStomp", "Professions & Racials");
            /* Warrior Buffs */
            AddControlInWinForm("Use Battle Shout", "UseBattleShout", "Warrior Buffs");
            AddControlInWinForm("Use Battle Stance", "UseBattleStance", "Warrior Buffs");
            AddControlInWinForm("Use Berserker Stance", "UseBerserkerStance", "Warrior Buffs");
            AddControlInWinForm("Use Commanding Shout", "UseCommandingShout", "Warrior Buffs");
            AddControlInWinForm("Use Defensive Stance", "UseDefensiveStance", "Warrior Buffs");
            /* Offensive Spell */
            AddControlInWinForm("Use Avatar", "UseAvatar", "Offensive Spell");
            AddControlInWinForm("Use Bladestorm", "UseBladestorm", "Offensive Spell");
            AddControlInWinForm("Use Bloodbath", "UseBloodbath", "Offensive Spell");
            AddControlInWinForm("Use Charge", "UseCharge", "Offensive Spell");
            AddControlInWinForm("Use Cleave", "UseCleave", "Offensive Spell");
            AddControlInWinForm("Use Devastate", "UseDevastate", "Offensive Spell");
            AddControlInWinForm("Use Dragon Roar", "UseDragonRoar", "Offensive Spell");
            AddControlInWinForm("Use Exectue", "UseExecute", "Offensive Spell");
            AddControlInWinForm("Use Heroic Leap", "UseHeroicLeap", "Offensive Spell");
            AddControlInWinForm("Use Heroic Strike", "UseHeroicStrike", "Offensive Spell");
            AddControlInWinForm("Use Heroic Throw", "UseHeroicThrow", "Offensive Spell");
            AddControlInWinForm("Use Revenge", "UseRevenge", "Offensive Spell");
            AddControlInWinForm("Use Shield Slam", "UseShieldSlam", "Offensive Spell");
            AddControlInWinForm("Use Shockwave", "UseShockwave", "Offensive Spell");
            AddControlInWinForm("Use Storm Bolt", "UseStormBolt", "Offensive Spell");
            AddControlInWinForm("Use Taunt", "UseTaunt", "Offensive Spell");
            AddControlInWinForm("Use Thunder Clap", "UseThunderClap", "Offensive Spell");
            /* Offensive Cooldown */
            AddControlInWinForm("Use Berserker Rage", "UseBerserkerRage", "Offensive Cooldown");
            AddControlInWinForm("Use Deadly Calm", "UseDeadlyCalm", "Offensive Cooldown");
            AddControlInWinForm("Use Recklessness", "UseRecklessness", "Offensive Cooldown");
            AddControlInWinForm("Use Shattering Throw", "UseShatteringThrow", "Offensive Cooldown");
            AddControlInWinForm("Use Sweeping Strikes", "UseSweepingStrikes", "Offensive Cooldown");
            AddControlInWinForm("Use Skull Banner", "UseSkullBanner", "Offensive Cooldown");
            /* Defensive Cooldown */
            AddControlInWinForm("Use Demoralizing Banner", "UseDemoralizingBanner", "Defensive Cooldown");
            AddControlInWinForm("Use Demoralizing Shout", "UseDemoralizingShout", "Defensive Cooldown");
            AddControlInWinForm("Use Disarm", "UseDisarm", "Defensive Cooldown");
            AddControlInWinForm("Use Disrupting Shout", "UseDisruptingShout", "Defensive Cooldown");
            AddControlInWinForm("Use Hamstring", "UseHamstring", "Defensive Cooldown");
            AddControlInWinForm("Use Intimidating Shout", "UseIntimidatingShout", "Defensive Cooldown");
            AddControlInWinForm("Use Mass Spell Reflection", "UseMassSpellReflection", "Defensive Cooldown");
            AddControlInWinForm("Use Piercing Howl", "UsePiercingHowl", "Defensive Cooldown");
            AddControlInWinForm("Use Pummel", "UsePummel", "Defensive Cooldown");
            AddControlInWinForm("Use Shield Barrier", "UseShieldBarrier", "Defensive Cooldown");
            AddControlInWinForm("Use Shield Block", "UseShieldBlock", "Defensive Cooldown");
            AddControlInWinForm("Use Shield Wall", "UseShieldWall", "Defensive Cooldown");
            AddControlInWinForm("Use Spell Reflection", "UseSpellReflection", "Defensive Cooldown");
            AddControlInWinForm("Use Staggering Shout", "UseStaggeringShout", "Defensive Cooldown");
            /* Healing Spell */
            AddControlInWinForm("Use Enraged Regeneration", "UseEnragedRegeneration", "Healing Spell");
            AddControlInWinForm("Use Last Stand", "UseLastStand", "Healing Spell");
            AddControlInWinForm("Use Rallying Cry", "UseRallyingCry", "Healing Spell");
            AddControlInWinForm("Use Victory Rush", "UseVictoryRush", "Healing Spell");
            /* Game Settings */
            AddControlInWinForm("Use Low Combat Settings", "UseLowCombat", "Game Settings");
            AddControlInWinForm("Use Trinket One", "UseTrinketOne", "Game Settings");
            AddControlInWinForm("Use Trinket Two", "UseTrinketTwo", "Game Settings");

            AddControlInWinForm("Use Alchemist Flask", "UseAlchFlask", "Game Settings");
            AddControlInWinForm("Do avoid melee (Off Advised!!)", "DoAvoidMelee", "Game Settings");
            AddControlInWinForm("Avoid melee distance (1 to 4)", "DoAvoidMeleeDistance", "Game Settings");
        }

        public static WarriorProtectionSettings CurrentSetting { get; set; }

        public static WarriorProtectionSettings GetSettings()
        {
            string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warrior_Protection.xml";
            if (File.Exists(currentSettingsFile))
            {
                return
                    CurrentSetting = Load<WarriorProtectionSettings>(currentSettingsFile);
            }
            return new WarriorProtectionSettings();
        }
    }

    #endregion
}

public class WarriorFury
{
    private static WarriorFurySettings MySettings = WarriorFurySettings.GetSettings();

    #region General Timers & Variables

    private readonly WoWItem _firstTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET);
    private readonly WoWItem _secondTrinket = EquippedItems.GetEquippedItem(WoWInventorySlot.INVTYPE_TRINKET, 2);
    public int LC = 0;

    private Timer _onCd = new Timer(0);

    #endregion

    #region Professions & Racials

    public readonly Spell Alchemy = new Spell("Alchemy");
    public readonly Spell ArcaneTorrent = new Spell("Arcane Torrent");
    public readonly Spell Berserking = new Spell("Berserking");
    public readonly Spell BloodFury = new Spell("Blood Fury");

    public readonly Spell GiftoftheNaaru = new Spell("Gift of the Naaru");

    public readonly Spell Stoneform = new Spell("Stoneform");
    public readonly Spell WarStomp = new Spell("War Stomp");

    #endregion

    #region Warrior Buffs

    public readonly Spell BattleShout = new Spell("Battle Shout");
    public readonly Spell BattleStance = new Spell("Battle Stance");
    public readonly Spell BerserkerStance = new Spell("Berserker Stance");
    public readonly Spell CommandingShout = new Spell("Commanding Shout");
    public readonly Spell DefensiveStance = new Spell("Defensive Stance");
    public readonly Spell SuddenDeathTalent = new Spell("Sudden Death");
    public readonly Spell UnquenchableThirstTalent = new Spell("Unquenchable Thirst");
    public readonly uint EnrageBuffId = 13046;
    public readonly uint BloodsurgeBuffId = 46915;

    #endregion

    #region Offensive Spell

    public readonly Spell Avatar = new Spell("Avatar");
    public readonly Spell Bladestorm = new Spell("Bladestorm");
    public readonly Spell Bloodbath = new Spell("Bloodbath");
    public readonly Spell Bloodthirst = new Spell("Bloodthirst");
    public readonly Spell Charge = new Spell("Charge");
    public readonly Spell Cleave = new Spell("Cleave");
    public readonly Spell ColossusSmash = new Spell("Colossus Smash");
    public readonly Spell DragonRoar = new Spell("Dragon Roar");
    public readonly Spell Execute = new Spell("Execute");
    public readonly Spell HeroicLeap = new Spell("Heroic Leap");
    public readonly Spell HeroicStrike = new Spell("Heroic Strike");
    public readonly Spell HeroicThrow = new Spell("Heroic Throw");
    public readonly Spell ImpendingVictory = new Spell("Impending Victory");
    public readonly Spell RagingBlow = new Spell("Raging Blow");
    public readonly Spell Ravager = new Spell("Ravager");
    public readonly Spell Shockwave = new Spell("Shockwave");
    public readonly Spell StormBolt = new Spell("Storm Bolt");
    public readonly Spell Taunt = new Spell("Taunt");
    public readonly Spell ThunderClap = new Spell("Thunder Clap");
    public readonly Spell Whirlwind = new Spell("Whirlwind");
    public readonly Spell WildStrike = new Spell("Wild Strike");

    #endregion

    #region Offensive Cooldown

    public readonly Spell BerserkerRage = new Spell("Berserker Rage");
    public readonly Spell DeadlyCalm = new Spell("Deadly Calm");
    public readonly Spell Recklessness = new Spell("Recklessness");
    public readonly Spell ShatteringThrow = new Spell("Shattering Throw");
    public readonly Spell SkullBanner = new Spell("Skull Banner");
    public readonly Spell SweepingStrikes = new Spell("Sweeping Strikes");

    #endregion

    #region Defensive Cooldown

    public readonly Spell DemoralizingBanner = new Spell("Demoralizing Banner");
    public readonly Spell DiebytheSword = new Spell("Die by the Sword");
    public readonly Spell Disarm = new Spell("Disarm");
    public readonly Spell DisruptingShout = new Spell("Disrupting Shout");
    public readonly Spell Hamstring = new Spell("Hamstring");
    public readonly Spell IntimidatingShout = new Spell("Intimidating Shout");
    public readonly Spell MassSpellReflection = new Spell("Mass Spell Reflection");
    public readonly Spell PiercingHowl = new Spell("Piercing Howl");
    public readonly Spell Pummel = new Spell("Pummel");
    public readonly Spell StaggeringShout = new Spell("Staggering Shout");
    private Timer _disarmTimer = new Timer(0);

    #endregion

    #region Healing Spell

    public readonly Spell EnragedRegeneration = new Spell("Enraged Regeneration");
    public readonly Spell RallyingCry = new Spell("Rallying Cry");
    public readonly Spell VictoryRush = new Spell("Victory Rush");

    #endregion

    public WarriorFury()
    {
        Main.InternalRange = ObjectManager.Me.GetCombatReach;
        Main.InternalAggroRange = Main.InternalRange;
        Main.InternalLightHealingSpell = null;
        MySettings = WarriorFurySettings.GetSettings();
        Main.DumpCurrentSettings<WarriorFurySettings>(MySettings);
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
                            if (ObjectManager.Me.Target != lastTarget
                                && HeroicStrike.IsHostileDistanceGood)
                            {
                                Pull();
                                lastTarget = ObjectManager.Me.Target;
                            }

                            if (ObjectManager.Target.Level < 70 && ObjectManager.Me.Level > 84
                                && MySettings.UseLowCombat)
                            {
                                LC = 1;
                                if (ObjectManager.Target.GetDistance < 30)
                                    LowCombat();
                            }
                            else
                            {
                                LC = 0;
                                if (ObjectManager.Target.GetDistance < 30)
                                    Combat();
                            }
                        }
                        if (!ObjectManager.Me.IsCast)
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

    private void Pull()
    {
        if (MySettings.UseCharge && Charge.IsSpellUsable && Charge.IsHostileDistanceGood)
        {
            Charge.Cast();
            Thread.Sleep(100);
            Usefuls.SleepGlobalCooldown();
        }
        if (MySettings.UseRavager && Ravager.IsSpellUsable && Ravager.IsHostileDistanceGood)
        {
            Ravager.Cast();
            Thread.Sleep(100);
            Usefuls.SleepGlobalCooldown();
        }
        if (MySettings.UseRecklessness && Recklessness.IsSpellUsable && ObjectManager.Target.GetDistance < 10f)
        {
            Recklessness.Cast();
            Thread.Sleep(100);
            Usefuls.SleepGlobalCooldown();
        }
        if (MySettings.UseBloodthirst && Bloodthirst.IsSpellUsable && ObjectManager.Target.GetDistance < 10f)
        {
            Bloodthirst.Cast();
            return;
        }
        if (MySettings.UseHeroicLeap && HeroicLeap.IsSpellUsable && HeroicLeap.IsHostileDistanceGood)
        {
            SpellManager.CastSpellByIDAndPosition(6544, ObjectManager.Target.Position);
            Others.SafeSleep(200);
        }
        if (MySettings.UseTaunt && Taunt.IsSpellUsable && Taunt.IsHostileDistanceGood && ObjectManager.Target.GetDistance > 20)
        {
            Taunt.Cast();
            return;
        }
    }

    private void LowCombat()
    {
        Buff();
        if (MySettings.DoAvoidMelee)
            AvoidMelee();
        DefenseCycle();
        Heal();

        if (HeroicThrow.KnownSpell && HeroicThrow.IsSpellUsable && HeroicThrow.IsHostileDistanceGood
            && MySettings.UseHeroicThrow && !ObjectManager.Target.InCombat)
        {
            HeroicThrow.Cast();
            return;
        }

        if (Charge.KnownSpell && Charge.IsSpellUsable && Charge.IsHostileDistanceGood
            && MySettings.UseCharge && ObjectManager.Target.GetDistance > Main.InternalRange)
        {
            Charge.Cast();
            return;
        }

        if (Bloodthirst.KnownSpell && Bloodthirst.IsSpellUsable && Bloodthirst.IsHostileDistanceGood
            && MySettings.UseBloodthirst)
        {
            Bloodthirst.Cast();
            return;
        }
        if (ColossusSmash.KnownSpell && ColossusSmash.IsHostileDistanceGood && ColossusSmash.IsSpellUsable
            && MySettings.UseColossusSmash)
        {
            ColossusSmash.Cast();
            return;
        }
        if (HeroicStrike.KnownSpell && HeroicStrike.IsSpellUsable && HeroicStrike.IsHostileDistanceGood
            && MySettings.UseHeroicStrike && ObjectManager.GetNumberAttackPlayer() < 3
            && ObjectManager.Me.RagePercentage > 80)
        {
            if (DeadlyCalm.KnownSpell && DeadlyCalm.IsSpellUsable && MySettings.UseDeadlyCalm)
            {
                DeadlyCalm.Cast();
                Others.SafeSleep(200);
            }

            HeroicStrike.Cast();
            return;
        }
        if (Shockwave.KnownSpell && Shockwave.IsSpellUsable && ObjectManager.Target.GetDistance < 10
            && MySettings.UseShockwave)
        {
            Shockwave.Cast();
            return;
        }
        if (DragonRoar.KnownSpell && DragonRoar.IsSpellUsable && ObjectManager.Target.GetDistance < 8
            && MySettings.UseDragonRoar)
        {
            DragonRoar.Cast();
            return;
        }
        if (Bladestorm.KnownSpell && Bladestorm.IsSpellUsable && ObjectManager.Target.GetDistance < 8
            && MySettings.UseBladestorm)
        {
            Bladestorm.Cast();
            return;
        }
        if (ThunderClap.KnownSpell && ThunderClap.IsSpellUsable && ThunderClap.IsHostileDistanceGood
            && MySettings.UseThunderClap)
        {
            ThunderClap.Cast();
        }
    }

    private void Combat()
    {
        Buff();
        DPSBurst();
        if (MySettings.DoAvoidMelee)
            AvoidMelee();
        DPSCycle();
        Decast();
        if (_onCd.IsReady)
            DefenseCycle();
        Heal();
    }

    private void Buff()
    {
        if (ObjectManager.Me.IsMounted)
            return;

        if (ObjectManager.Me.HealthPercent < 30 && MySettings.UseDefensiveStance && DefensiveStance.KnownSpell && DefensiveStance.IsSpellUsable && !DefensiveStance.HaveBuff)
        {
            DefensiveStance.Cast();
            return;
        }
        if (!BattleStance.HaveBuff && BattleStance.KnownSpell && BattleStance.IsSpellUsable && MySettings.UseBattleStance && ObjectManager.Me.HealthPercent > 50)
        {
            BattleStance.Cast();
            return;
        }
        if (!BerserkerStance.HaveBuff && BerserkerStance.KnownSpell && BerserkerStance.IsSpellUsable && MySettings.UseBerserkerStance && !MySettings.UseBattleStance &&
            ObjectManager.Me.HealthPercent > 50)
        {
            BerserkerStance.Cast();
            return;
        }
        if (BattleShout.KnownSpell && BattleShout.IsSpellUsable && !BattleShout.HaveBuff && MySettings.UseBattleShout)
        {
            BattleShout.Cast();
            return;
        }
        if (CommandingShout.KnownSpell && CommandingShout.IsSpellUsable && !CommandingShout.HaveBuff && MySettings.UseCommandingShout && !MySettings.UseBattleShout)
        {
            CommandingShout.Cast();
            return;
        }
        if (MySettings.UseAlchFlask && !ObjectManager.Me.HaveBuff(79638) && !ObjectManager.Me.HaveBuff(79640) && !ObjectManager.Me.HaveBuff(79639) &&
            !ItemsManager.IsItemOnCooldown(75525) && ItemsManager.GetItemCount(75525) > 0)
        {
            ItemsManager.UseItem(75525);
        }
    }

    private void AvoidMelee()
    {
        if (ObjectManager.Target.GetDistance < MySettings.DoAvoidMeleeDistance && ObjectManager.Target.InCombat)
        {
            Logging.WriteFight("Too Close. Moving Back");
            var maxTimeTimer = new Timer(1000 * 2);
            MovementsAction.MoveBackward(true);
            while (ObjectManager.Target.GetDistance < 2 && ObjectManager.Target.InCombat && !maxTimeTimer.IsReady)
                Others.SafeSleep(300);
            MovementsAction.MoveBackward(false);
            if (maxTimeTimer.IsReady && ObjectManager.Target.GetDistance < 2 && ObjectManager.Target.InCombat)
            {
                MovementsAction.MoveForward(true);
                Others.SafeSleep(1000);
                MovementsAction.MoveForward(false);
                MovementManager.Face(ObjectManager.Target.Position);
            }
        }
    }

    private void DefenseCycle()
    {
        if (ObjectManager.Me.HealthPercent < 95 && MySettings.UseDisarm && Disarm.IsHostileDistanceGood
            && Disarm.KnownSpell && Disarm.IsSpellUsable && _disarmTimer.IsReady)
        {
            Disarm.Cast();
            _disarmTimer = new Timer(1000 * 60);
            return;
        }
        if (ObjectManager.Me.HealthPercent < 20 && MySettings.UseIntimidatingShout
            && IntimidatingShout.KnownSpell && IntimidatingShout.IsSpellUsable &&
            ObjectManager.Target.GetDistance < 8)
        {
            IntimidatingShout.Cast();
            _onCd = new Timer(1000 * 8);
            return;
        }
        if (ObjectManager.Me.HealthPercent < 80 && MySettings.UseDiebytheSword
            && DiebytheSword.KnownSpell && DiebytheSword.IsSpellUsable)
        {
            DiebytheSword.Cast();
            _onCd = new Timer(1000 * 8);
            return;
        }
        if (ObjectManager.Me.HealthPercent < 80 && MySettings.UseDemoralizingBanner
            && DemoralizingBanner.KnownSpell && DemoralizingBanner.IsSpellUsable &&
            ObjectManager.Target.GetDistance < 30)
        {
            SpellManager.CastSpellByIDAndPosition(114203, ObjectManager.Target.Position);
            _onCd = new Timer(1000 * 15);
            return;
        }
        if (ObjectManager.Me.HealthPercent <= MySettings.UseWarStompAtPercentage && WarStomp.IsSpellUsable &&
            WarStomp.KnownSpell
            && MySettings.UseWarStomp)
        {
            WarStomp.Cast();
            _onCd = new Timer(1000 * 2);
            return;
        }
        if (ObjectManager.Me.HealthPercent <= MySettings.UseStoneformAtPercentage && Stoneform.IsSpellUsable &&
            Stoneform.KnownSpell
            && MySettings.UseStoneform)
        {
            Stoneform.Cast();
            _onCd = new Timer(1000 * 8);
        }
    }

    private void Heal()
    {
        if (ObjectManager.Me.IsMounted)
            return;

        if (VictoryRush.KnownSpell && VictoryRush.IsSpellUsable && VictoryRush.IsHostileDistanceGood
            && MySettings.UseVictoryRush && ObjectManager.Me.HealthPercent < 90)
        {
            VictoryRush.Cast();
            return;
        }
        if (ObjectManager.Me.HealthPercent < 30 && RallyingCry.IsSpellUsable && RallyingCry.KnownSpell
            && MySettings.UseRallyingCry && ObjectManager.Me.InCombat)
        {
            RallyingCry.Cast();
            return;
        }
        if (ObjectManager.Me.HealthPercent <= MySettings.UseGiftoftheNaaruAtPercentage &&
            GiftoftheNaaru.IsSpellUsable && GiftoftheNaaru.KnownSpell
            && MySettings.UseGiftoftheNaaru)
        {
            GiftoftheNaaru.Cast();
            return;
        }
        if (ObjectManager.Me.HealthPercent < 80 && EnragedRegeneration.IsSpellUsable &&
            EnragedRegeneration.KnownSpell
            && MySettings.UseEnragedRegeneration)
        {
            EnragedRegeneration.Cast();
        }
    }

    private void Decast()
    {
        if (ArcaneTorrent.IsSpellUsable && ArcaneTorrent.KnownSpell && ObjectManager.Target.GetDistance < 8
            && ObjectManager.Me.HealthPercent <= MySettings.UseArcaneTorrentForDecastAtPercentage
            && MySettings.UseArcaneTorrentForDecast && ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe)
        {
            ArcaneTorrent.Cast();
            return;
        }
        if (!Hamstring.TargetHaveBuff && MySettings.UseHamstring && Hamstring.KnownSpell
            && Hamstring.IsSpellUsable && Hamstring.IsHostileDistanceGood)
        {
            Hamstring.Cast();
            return;
        }
        if (ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe && Pummel.IsHostileDistanceGood
            && Pummel.KnownSpell && Pummel.IsSpellUsable && MySettings.UsePummel)
        {
            Pummel.Cast();
            return;
        }
        if (ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe &&
            ObjectManager.Target.GetDistance < 10
            && DisruptingShout.KnownSpell && DisruptingShout.IsSpellUsable && MySettings.UseDisruptingShout)
        {
            DisruptingShout.Cast();
            return;
        }
        if (ObjectManager.Target.GetMove && !PiercingHowl.TargetHaveBuff && MySettings.UsePiercingHowl
            && PiercingHowl.KnownSpell && PiercingHowl.IsSpellUsable && ObjectManager.Target.GetDistance < 15)
        {
            PiercingHowl.Cast();
            return;
        }
        if (Hamstring.TargetHaveBuff && MySettings.UseStaggeringShout && StaggeringShout.KnownSpell
            && StaggeringShout.IsSpellUsable && ObjectManager.Target.GetDistance < 20)
        {
            StaggeringShout.Cast();
            return;
        }
        if (ObjectManager.Target.IsCast && ObjectManager.Target.IsTargetingMe &&
            MySettings.UseMassSpellReflection
            && MassSpellReflection.KnownSpell && MassSpellReflection.IsSpellUsable)
        {
            MassSpellReflection.Cast();
        }
    }

    private void DPSBurst()
    {
        if (MySettings.UseTrinketOne && !ItemsManager.IsItemOnCooldown(_firstTrinket.Entry) && ItemsManager.IsItemUsable(_firstTrinket.Entry))
        {
            ItemsManager.UseItem(_firstTrinket.Name);
            Logging.WriteFight("Use First Trinket Slot");
        }

        if (MySettings.UseTrinketTwo && !ItemsManager.IsItemOnCooldown(_secondTrinket.Entry) && ItemsManager.IsItemUsable(_secondTrinket.Entry))
        {
            ItemsManager.UseItem(_secondTrinket.Name);
            Logging.WriteFight("Use Second Trinket Slot");
            return;
        }
        if (Berserking.IsSpellUsable && Berserking.KnownSpell && ObjectManager.Target.GetDistance < 30 && MySettings.UseBerserking)
        {
            Berserking.Cast();
            return;
        }
        if (BloodFury.IsSpellUsable && BloodFury.KnownSpell && ObjectManager.Target.GetDistance < 30 && MySettings.UseBloodFury)
        {
            BloodFury.Cast();
            return;
        }
        if (BerserkerRage.KnownSpell && BerserkerRage.IsSpellUsable && ObjectManager.Me.RagePercentage < 50 && MySettings.UseBerserkerRage &&
            ObjectManager.Target.GetDistance < 30)
        {
            BerserkerRage.Cast();
            return;
        }
        if (Recklessness.KnownSpell && Recklessness.IsSpellUsable && MySettings.UseRecklessness && ObjectManager.Target.GetDistance < 30)
        {
            Recklessness.Cast();
            return;
        }
        if (ShatteringThrow.KnownSpell && ShatteringThrow.IsSpellUsable && ShatteringThrow.IsHostileDistanceGood && MySettings.UseShatteringThrow)
        {
            ShatteringThrow.Cast();
            return;
        }
        if (SkullBanner.KnownSpell && SkullBanner.IsSpellUsable && MySettings.UseSkullBanner && ObjectManager.Target.GetDistance < 30)
        {
            SkullBanner.Cast();
            return;
        }
        if (Avatar.KnownSpell && Avatar.IsSpellUsable && MySettings.UseAvatar && ObjectManager.Target.GetDistance < 30)
        {
            Avatar.Cast();
            return;
        }
        if (Bloodbath.KnownSpell && Bloodbath.IsSpellUsable && MySettings.UseBloodbath && ObjectManager.Target.GetDistance < 30)
        {
            Bloodbath.Cast();
            return;
        }
        if (StormBolt.KnownSpell && StormBolt.IsSpellUsable && MySettings.UseStormBolt && StormBolt.IsHostileDistanceGood)
        {
            StormBolt.Cast();
        }
    }

    private void ExecuteCycle()
    {
        if (MySettings.UseExecute && (SuddenDeathTalent.HaveBuff || ObjectManager.Me.Rage > 70) && Execute.IsSpellUsable && Execute.IsHostileDistanceGood)
        {
            Execute.Cast();
            return;
        }
        if (MySettings.UseBloodthirst && !ObjectManager.Me.HaveBuff(EnrageBuffId) && (UnquenchableThirstTalent.KnownSpell || ObjectManager.Me.Rage < 80) && Bloodthirst.IsSpellUsable &&
            Bloodthirst.IsHostileDistanceGood)
        {
            Bloodthirst.Cast();
            return;
        }
        if (MySettings.UseRavager && Ravager.IsSpellUsable && Ravager.IsHostileDistanceGood)
        {
            Ravager.Cast();
            return;
        }
        if (MySettings.UseDragonRoar && DragonRoar.IsSpellUsable && DragonRoar.IsHostileDistanceGood)
        {
            DragonRoar.Cast();
            return;
        }
        if (MySettings.UseStormBolt && StormBolt.IsSpellUsable && StormBolt.IsHostileDistanceGood)
        {
            StormBolt.Cast();
            return;
        }
        if (MySettings.UseExecute && ObjectManager.Me.HaveBuff(EnrageBuffId) && Execute.IsSpellUsable && Execute.IsHostileDistanceGood)
        {
            Execute.Cast();
            return;
        }
        if (MySettings.UseWildStrike && ObjectManager.Me.HaveBuff(BloodsurgeBuffId) && WildStrike.IsSpellUsable && WildStrike.IsHostileDistanceGood)
        {
            WildStrike.Cast();
            return;
        }
        if (MySettings.UseRagingBlow && RagingBlow.IsSpellUsable && RagingBlow.IsHostileDistanceGood)
        {
            RagingBlow.Cast();
            return;
        }
        if (MySettings.UseBloodthirst && UnquenchableThirstTalent.KnownSpell && Bloodthirst.IsSpellUsable && Bloodthirst.IsHostileDistanceGood)
        {
            Bloodthirst.Cast();
            return;
        }
    }

    private void DPSCycle()
    {
        Usefuls.SleepGlobalCooldown();
        try
        {
            Memory.WowMemory.GameFrameLock(); // !!! WARNING - DONT SLEEP WHILE LOCKED - DO FINALLY(GameFrameUnLock()) !!!

            if (MySettings.UseBerserkerRage && BerserkerRage.IsSpellUsable && !ObjectManager.Me.HaveBuff(EnrageBuffId) && ObjectManager.Target.GetDistance <= 25)
            {
                BerserkerRage.Cast();
                return;
            }
            if (MySettings.UseCharge && Charge.IsSpellUsable && Charge.IsHostileDistanceGood)
            {
                Charge.Cast();
                return;
            }
            if (ObjectManager.Target.HealthPercent <= 20)
            {
                ExecuteCycle();
                return;
            }
            if (MySettings.UseWildStrike && (ObjectManager.Me.HaveBuff(BloodsurgeBuffId) || ObjectManager.Me.Rage > 70) && WildStrike.IsSpellUsable && WildStrike.IsHostileDistanceGood)
            {
                WildStrike.Cast();
                return;
            }
            if (MySettings.UseExecute && SuddenDeathTalent.KnownSpell && SuddenDeathTalent.HaveBuff && Execute.IsSpellUsable && Execute.IsHostileDistanceGood)
            {
                Execute.Cast();
                return;
            }
            if (MySettings.UseRagingBlow && RagingBlow.BuffStack == 2 && RagingBlow.IsSpellUsable && RagingBlow.IsHostileDistanceGood)
            {
                RagingBlow.Cast();
                return;
            }
            if (MySettings.UseBloodthirst && !ObjectManager.Me.HaveBuff(EnrageBuffId) && (UnquenchableThirstTalent.KnownSpell || ObjectManager.Me.Rage < 80) && Bloodthirst.IsSpellUsable &&
                Bloodthirst.IsHostileDistanceGood)
            {
                Bloodthirst.Cast();
                return;
            }
            if (MySettings.UseRavager && Ravager.IsSpellUsable && Ravager.IsHostileDistanceGood)
            {
                Ravager.Cast();
                return;
            }
            if (MySettings.UseDragonRoar && DragonRoar.IsSpellUsable && DragonRoar.IsHostileDistanceGood)
            {
                DragonRoar.Cast();
                return;
            }
            if (MySettings.UseStormBolt && StormBolt.IsSpellUsable && StormBolt.IsHostileDistanceGood)
            {
                StormBolt.Cast();
                return;
            }
            if (MySettings.UseWildStrike && ObjectManager.Me.HaveBuff(BloodsurgeBuffId) && WildStrike.IsSpellUsable && WildStrike.IsHostileDistanceGood)
            {
                WildStrike.Cast();
                return;
            }
            if (MySettings.UseRagingBlow && RagingBlow.IsSpellUsable && RagingBlow.IsHostileDistanceGood)
            {
                RagingBlow.Cast();
                return;
            }
            if (MySettings.UseWildStrike && ObjectManager.Me.HaveBuff(EnrageBuffId) && WildStrike.IsSpellUsable && WildStrike.IsHostileDistanceGood)
            {
                WildStrike.Cast();
                return;
            }
            if (MySettings.UseBloodthirst && UnquenchableThirstTalent.KnownSpell && Bloodthirst.IsSpellUsable && Bloodthirst.IsHostileDistanceGood)
            {
                Bloodthirst.Cast();
                return;
            }
        }
        finally
        {
            Memory.WowMemory.GameFrameUnLock();
        }
    }

    private void Patrolling()
    {
        if (!ObjectManager.Me.IsMounted)
        {
            Buff();
            Heal();
        }
    }

    #region Nested type: WarriorFurySettings

    [Serializable]
    public class WarriorFurySettings : Settings
    {
        public bool DoAvoidMelee = false;
        public int DoAvoidMeleeDistance = 0;
        public bool UseAlchFlask = true;
        public bool UseArcaneTorrentForDecast = true;
        public int UseArcaneTorrentForDecastAtPercentage = 100;
        public bool UseArcaneTorrentForResource = true;
        public bool UseAvatar = true;
        public bool UseBattleShout = true;
        public bool UseBattleStance = true;
        public bool UseBerserkerRage = true;
        public bool UseBerserkerStance = false;
        public bool UseBerserking = true;
        public bool UseBladestorm = true;
        public bool UseBloodFury = true;
        public bool UseBloodbath = true;
        public bool UseBloodthirst = true;
        public bool UseCharge = true;
        public bool UseCleave = true;
        public bool UseColossusSmash = true;
        public bool UseCommandingShout = false;
        public bool UseDeadlyCalm = true;
        public bool UseDefensiveStance = true;
        public bool UseDemoralizingBanner = true;
        public bool UseDiebytheSword = true;
        public bool UseDisarm = true;
        public bool UseDisruptingShout = true;
        public bool UseDragonRoar = true;
        public bool UseRavager = true;
        public bool UseEnragedRegeneration = true;
        public bool UseExecute = true;
        public bool UseGiftoftheNaaru = true;
        public int UseGiftoftheNaaruAtPercentage = 80;
        public bool UseHamstring = false;
        public bool UseHeroicLeap = true;
        public bool UseHeroicStrike = true;
        public bool UseHeroicThrow = true;
        public bool UseIntimidatingShout = true;

        public bool UseLowCombat = true;
        public bool UseMassSpellReflection = true;
        public bool UsePiercingHowl = false;
        public bool UsePummel = true;
        public bool UseRagingBlow = true;
        public bool UseRallyingCry = true;
        public bool UseRecklessness = true;
        public bool UseShatteringThrow = true;
        public bool UseShockwave = true;
        public bool UseSkullBanner = true;
        public bool UseStaggeringShout = true;
        public bool UseStoneform = true;
        public int UseStoneformAtPercentage = 80;
        public bool UseStormBolt = true;
        public bool UseSweepingStrikes = true;
        public bool UseTaunt = true;
        public bool UseThunderClap = true;
        public bool UseTrinketOne = true;
        public bool UseTrinketTwo = true;
        public bool UseVictoryRush = true;
        public bool UseWarStomp = true;
        public int UseWarStompAtPercentage = 80;
        public bool UseWhirlwind = true;
        public bool UseWildStrike = true;

        public WarriorFurySettings()
        {
            ConfigWinForm("Warrior Fury Settings");
            /* Professions & Racials */
            AddControlInWinForm("Use Arcane Torrent for Interrupt", "UseArcaneTorrentForDecast", "Professions & Racials", "AtPercentage");
            AddControlInWinForm("Use Arcane Torrent for Resource", "UseArcaneTorrentForResource", "Professions & Racials");
            AddControlInWinForm("Use Berserking", "UseBerserking", "Professions & Racials");
            AddControlInWinForm("Use Blood Fury", "UseBloodFury", "Professions & Racials");
            AddControlInWinForm("Use Gift of the Naaru", "UseGiftoftheNaaru", "Professions & Racials");

            AddControlInWinForm("Use Stoneform", "UseStoneform", "Professions & Racials");
            AddControlInWinForm("Use War Stomp", "UseWarStomp", "Professions & Racials");
            /* Warrior Buffs */
            AddControlInWinForm("Use Battle Shout", "UseBattleShout", "Warrior Buffs");
            AddControlInWinForm("Use Battle Stance", "UseBattleStance", "Warrior Buffs");
            AddControlInWinForm("Use Berserker Stance", "UseBerserkerStance", "Warrior Buffs");
            AddControlInWinForm("Use Commanding Shout", "UseCommandingShout", "Warrior Buffs");
            AddControlInWinForm("Use Defensive Stance", "UseDefensiveStance", "Warrior Buffs");
            /* Offensive Spell */
            AddControlInWinForm("Use Avatar", "UseAvatar", "Offensive Spell");
            AddControlInWinForm("Use Bladestorm", "UseBladestorm", "Offensive Spell");
            AddControlInWinForm("Use Bloodbath", "UseBloodbath", "Offensive Spell");
            AddControlInWinForm("Use Bloodthirst", "UseBloodthirst", "Offensive Spell");
            AddControlInWinForm("Use Charge", "UseCharge", "Offensive Spell");
            AddControlInWinForm("Use Cleave", "UseCleave", "Offensive Spell");
            AddControlInWinForm("Use Colossus Smash", "UseColossusSmash", "Offensive Spell");
            AddControlInWinForm("Use Ravager", "UseRavager", "Offensive Spell");
            AddControlInWinForm("Use Dragon Roar", "UseDragonRoar", "Offensive Spell");
            AddControlInWinForm("Use Exectue", "UseExecute", "Offensive Spell");
            AddControlInWinForm("Use Heroic Leap", "UseHeroicLeap", "Offensive Spell");
            AddControlInWinForm("Use Heroic Strike", "UseHeroicStrike", "Offensive Spell");
            AddControlInWinForm("Use Heroic Throw", "UseHeroicThrow", "Offensive Spell");
            AddControlInWinForm("Use Raging Blow", "UseRagingBlow", "Offensive Spell");
            AddControlInWinForm("Use Shockwave", "UseShockwave", "Offensive Spell");
            AddControlInWinForm("Use Storm Bolt", "UseStormBolt", "Offensive Spell");
            AddControlInWinForm("Use Taunt", "UseTaunt", "Offensive Spell");
            AddControlInWinForm("Use Thunder Clap", "UseThunderClap", "Offensive Spell");
            AddControlInWinForm("Use Whirlwind", "UseWhirlwind", "Offensive Spell");
            AddControlInWinForm("Use Wild Strike", "UseWildStrike", "Offensive Spell");
            /* Offensive Cooldown */
            AddControlInWinForm("Use Berserker Rage", "UseBerserkerRage", "Offensive Cooldown");
            AddControlInWinForm("Use Deadly Calm", "UseDeadlyCalm", "Offensive Cooldown");
            AddControlInWinForm("Use Recklessness", "UseRecklessness", "Offensive Cooldown");
            AddControlInWinForm("Use Shattering Throw", "UseShatteringThrow", "Offensive Cooldown");
            AddControlInWinForm("Use Sweeping Strikes", "UseSweepingStrikes", "Offensive Cooldown");
            AddControlInWinForm("Use Skull Banner", "UseSkullBanner", "Offensive Cooldown");
            /* Defensive Cooldown */
            AddControlInWinForm("Use Demoralizing Banner", "UseDemoralizingBanner", "Defensive Cooldown");
            AddControlInWinForm("Use Die by the Sword", "UseDiebytheSword", "Defensive Cooldown");
            AddControlInWinForm("Use Disarm", "UseDisarm", "Defensive Cooldown");
            AddControlInWinForm("Use Disrupting Shout", "UseDisruptingShout", "Defensive Cooldown");
            AddControlInWinForm("Use Hamstring", "UseHamstring", "Defensive Cooldown");
            AddControlInWinForm("Use Intimidating Shout", "UseIntimidatingShout", "Defensive Cooldown");
            AddControlInWinForm("Use Mass Spell Reflection", "UseMassSpellReflection", "Defensive Cooldown");
            AddControlInWinForm("Use Piercing Howl", "UsePiercingHowl", "Defensive Cooldown");
            AddControlInWinForm("Use Pummel", "UsePummel", "Defensive Cooldown");
            AddControlInWinForm("Use Staggering Shout", "UseStaggeringShout", "Defensive Cooldown");
            /* Healing Spell */
            AddControlInWinForm("Use Enraged Regeneration", "UseEnragedRegeneration", "Healing Spell");
            AddControlInWinForm("Use Rallying Cry", "UseRallyingCry", "Healing Spell");
            AddControlInWinForm("Use Victory Rush", "UseVictoryRush", "Healing Spell");
            /* Game Settings */
            AddControlInWinForm("Use Low Combat Settings", "UseLowCombat", "Game Settings");
            AddControlInWinForm("Use Trinket One", "UseTrinketOne", "Game Settings");
            AddControlInWinForm("Use Trinket Two", "UseTrinketTwo", "Game Settings");

            AddControlInWinForm("Use Alchemist Flask", "UseAlchFlask", "Game Settings");
            AddControlInWinForm("Do avoid melee (Off Advised!!)", "DoAvoidMelee", "Game Settings");
            AddControlInWinForm("Avoid melee distance (1 to 4)", "DoAvoidMeleeDistance", "Game Settings");
        }

        public static WarriorFurySettings CurrentSetting { get; set; }

        public static WarriorFurySettings GetSettings()
        {
            string currentSettingsFile = Application.StartupPath + "\\CombatClasses\\Settings\\Warrior_Fury.xml";
            if (File.Exists(currentSettingsFile))
            {
                return
                    CurrentSetting = Load<WarriorFurySettings>(currentSettingsFile);
            }
            return new WarriorFurySettings();
        }
    }

    #endregion
}

#endregion

// ReSharper restore ObjectCreationAsStatement
// ReSharper restore EmptyGeneralCatchClause