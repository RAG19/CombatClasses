using System;
using System.Linq;
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

    public void Dispose()
    {
        Logging.WriteFight("Saved TnB API.");
        InternalLoop = false;
    }
    #endregion

    public void Initialize(bool configOnly, bool resetSettings = false)
    {
        try
        {
            if (!InternalLoop)
                InternalLoop = true;
            Logging.WriteFight("Loading TnB API");
            Logging.NewFile();

            /**
            ShowMethods(typeof(WoWUnit));
            ShowMethods(typeof(WoWPlayer));
            ShowMethods(typeof(Spell));
            ShowMethods(typeof(nManager));
            ShowMethods(typeof(ObjectManager));
            ShowMethods(typeof(CombatClass));
            ShowMethods(typeof(HealerClass));
            ShowMethods(typeof(Fight));
            ShowMethods(typeof(MountTask));
            ShowMethods(typeof(Logging));
            ShowMethods(typeof(Timer));
            ShowMethods(typeof(Usefuls));
            /**/
            ShowMethods(typeof(Usefuls));

            Logging.NewFile();
            Dispose();
        }
        catch
        {
        }
    }

    private static void ShowMethods(Type type)
    {
        Logging.WriteDebug("");
        Logging.WriteDebug("[u][size=large]" + type.Name + "[/size][/u]");
        Logging.WriteDebug("");
        foreach (var method in type.GetMethods())
        {
            var parameters = method.GetParameters();
            var parameterDescriptions = string.Join
                (", ", method.GetParameters()
                    .Select(x => ShortenTypeName(x.ParameterType) + " " + x.Name)
                    .ToArray());

            if (method.Name.Substring(0, 4) == "get_")
                Logging.WriteDebug(method.Name.Substring(4) + " [color=#ffff00]returns[/color] " + ShortenTypeName(method.ReturnType));
            else if (method.Name.Substring(0, 4) == "set_")
                Logging.WriteDebug(method.Name.Substring(4) + " [color=#00ff00]can be set[/color] " + ShortenTypeName(method.ReturnType));
            else if (method.ReturnType == typeof(void))
                Logging.WriteDebug(method.Name + "(" + parameterDescriptions + ")");
            else
                Logging.WriteDebug(method.Name + "(" + parameterDescriptions + ") [color=#ffff00]returns[/color] " + ShortenTypeName(method.ReturnType));
        }
        Logging.WriteDebug("[hr]");
    }

    private static string ShortenTypeName(Type type)
    {
        string s = type.ToString();

        if (s.IndexOf('[') > 0)
            return "[i][" + s.Substring(s.LastIndexOf('.') + 1) + "[/i]";
        else
            return "[i]" + s.Substring(s.LastIndexOf('.') + 1) + "[/i]";
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