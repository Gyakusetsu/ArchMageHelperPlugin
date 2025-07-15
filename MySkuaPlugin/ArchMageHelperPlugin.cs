using System;
using System.Runtime.CompilerServices;
using Skua.Core.Interfaces;
using System.Timers;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

namespace RimuruPlugin
{
    public class ArchMageHelperPlugin : ISkuaPlugin
    {

        public IScriptInterface Bot => IScriptInterface.Instance;

        public string Name => "ArchMage Helper Plugin";

        public string Author => "Rimuru";

        public string Description => "ArchMage Helper Plugin";

        public List<IOption>? Options { get; } = new List<IOption>();

        private enum AscensionMode
        {
            Corporeal,
            Astral
        }

        private AscensionMode SelectedAscensionMode = AscensionMode.Corporeal;

        private const string CORPOREAL_AURA_NAME = "Corporeal Ascension";
        private const string ASTRAL_AURA_NAME = "Astral Ascension";
        // private string SelectedAuraName = CORPOREAL_AURA_NAME;

        private System.Timers.Timer? MainHelperTimer;
        private System.Timers.Timer? DamageBoostHelperTimer;
        private System.Timers.Timer? UnloadTimer;
        private System.Timers.Timer? OptionLockTimer;
        private System.Timers.Timer? SkillSpamTimer;

        private IPluginHelper? BotHelper;

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        public static void PressKey(Keys key, bool up) {
            const int KEYEVENTF_EXTENDEDKEY = 0x1;
            const int KEYEVENTF_KEYUP = 0x2;
            if (up) {
                keybd_event((byte) key, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, (UIntPtr) 0);
            }
            else {
                keybd_event((byte) key, 0x45, KEYEVENTF_EXTENDEDKEY, (UIntPtr) 0);
            }
        }

        /// <summary>Returns true if the current application has focus, false otherwise</summary>
        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero) {
                return false;       // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        private bool IsArchMageEquiped()
        {
            return Bot.Player.CurrentClass is not null && Bot.Player.CurrentClass.Name.Equals("ArchMage");
        }

        private void OptionLockListener(Object? source, ElapsedEventArgs e)
        {
            if (IsArchMageEquiped()) {
                Bot.Options.AttackWithoutTarget = true;
            }
        }

        private void SkillSpamListener(Object? source, ElapsedEventArgs e)
        {
            if (IsArchMageEquiped() && Bot.Target.Auras.Count > 0) {
                // PressKey(Keys.D1, false);
                // PressKey(Keys.D1, true);
                if (Bot.Player.Mana < 30)
                {
                    Bot.Skills.UseSkill(2);
                    // PressKey(Keys.D3, false);
                    // PressKey(Keys.D3, true);
                } 
                
                if (Bot.Player.Mana > 10) {
                    Bot.Skills.UseSkill(1);
                    // PressKey(Keys.D2, false);
                    // PressKey(Keys.D2, true);
                }

                if (Bot.Player.Mana > 20) {
                    Bot.Skills.UseSkill(3);
                    // PressKey(Keys.D4, false);
                    // PressKey(Keys.D4, true);
                }
            }
        }

        private void ArcaneSigilListener(Object? source, ElapsedEventArgs e)
        {
            if (IsArchMageEquiped()) {

                switch (SelectedAscensionMode)
                {
                    case AscensionMode.Corporeal:
                        if (Bot.Player.Stats?.CriticalChance < 1.0f && Bot.Self.HasActiveAura("Arcane Flux"))
                            //   || !Bot.Self.HasActiveAura(CORPOREAL_AURA_NAME))
                        {
                            ActivateArcaneSigil();
                        }
                        break;
                    case AscensionMode.Astral:
                        if (!Bot.Self.HasActiveAura(ASTRAL_AURA_NAME))
                        {
                            ActivateArcaneSigil();
                        }
                        break;
                }
            }
        }

        private void DamageBoostListener(Object? source, ElapsedEventArgs e)
        {
            if (IsArchMageEquiped() && Bot.Player.InCombat
                && (Bot.Flash.GetGameObject("world.myAvatar.dataLeaf.sta.$cao", 1.0f) < 1.3f // Damage boost
                    || !Bot.Self.HasActiveAura("Arcane Sigil")))
            {
                ActivateArcaneSigil();
            }
        }

        private void UnloadPluginListener(Object? source, ElapsedEventArgs e)
        {
            if (Bot.Player.LoggedIn && Bot.Flash.IsWorldLoaded && Bot.Map.Loaded && Bot.Player.Loaded) {
                if (Bot.Bank.Contains("ArchMage") || Bot.Inventory.Contains("ArchMage")) {
                    if (UnloadTimer is not null) {
                        UnloadTimer.Stop();
                        UnloadTimer.Dispose();
                        Logger("ArchMage owned");
                    }
                } else {
                    Logger("ArchMage not owned, Unloading ArchMage Helper Plugin", "UnloadPluginListener");
                    Unload();
                }
            }
        }

        private void ActivateArcaneSigil() {
            Bot.Skills.UseSkill(4);
        }

        public void Load(IServiceProvider provider, IPluginHelper helper)
        {
            BotHelper = helper;

            MainHelperTimer = new System.Timers.Timer(250);
            MainHelperTimer.Elapsed += ArcaneSigilListener;
            MainHelperTimer.AutoReset = true;
            MainHelperTimer.Enabled = true;

            DamageBoostHelperTimer = new System.Timers.Timer(250);
            DamageBoostHelperTimer.Elapsed += DamageBoostListener;
            DamageBoostHelperTimer.AutoReset = true;
            DamageBoostHelperTimer.Enabled = false;

            OptionLockTimer = new System.Timers.Timer(1000);
            OptionLockTimer.Elapsed += OptionLockListener;
            OptionLockTimer.AutoReset = true;
            OptionLockTimer.Enabled = true;

            UnloadTimer = new System.Timers.Timer(1000);
            UnloadTimer.Elapsed += UnloadPluginListener;
            UnloadTimer.AutoReset = true;
            UnloadTimer.Enabled = true;

            SkillSpamTimer = new System.Timers.Timer(250);
            SkillSpamTimer.Elapsed += SkillSpamListener;
            SkillSpamTimer.AutoReset = true;
            SkillSpamTimer.Enabled = true;

            helper.AddMenuButton("Set DamageBoost Helper", delegate
            {
                DamageBoostHelperTimer.Enabled = !DamageBoostHelperTimer.Enabled;
                Logger($"SetHelper: {DamageBoostHelperTimer.Enabled.ToString()}");
            });
            helper.AddMenuButton("Switch Ascension Mode", delegate
            {
                if (SelectedAscensionMode == AscensionMode.Corporeal) {
                    SelectedAscensionMode = AscensionMode.Astral;
                } else {
                    SelectedAscensionMode = AscensionMode.Corporeal;
                }
                Logger($"Switched to {nameof(SelectedAscensionMode)} Ascension");
            });
            helper.AddMenuButton("SkillSpam", delegate
            {
                SkillSpamTimer.Enabled = !SkillSpamTimer.Enabled;
                Logger($"SkillSpam: {SkillSpamTimer.Enabled.ToString()}");
            });

            Logger("ArchMage Helper Plugin Loaded");
        }

        public void Unload()
        {
            if (BotHelper is not null) {
                BotHelper.RemoveMenuButton("Set DamageBoost Helper");
                BotHelper.RemoveMenuButton("Switch Ascension Mode");
                BotHelper.RemoveMenuButton("SkillSpam");
            }
            if (MainHelperTimer is not null)
            {
                MainHelperTimer.Stop();
                MainHelperTimer.Dispose();
            }
            if (DamageBoostHelperTimer is not null)
            {
                DamageBoostHelperTimer.Stop();
                DamageBoostHelperTimer.Dispose();
            }
            if (OptionLockTimer is not null)
            {
                OptionLockTimer.Stop();
                OptionLockTimer.Dispose();
            }
            if (UnloadTimer is not null)
            {
                UnloadTimer.Stop();
                UnloadTimer.Dispose();
            }
            if (SkillSpamTimer is not null)
            {
                SkillSpamTimer.Stop();
                SkillSpamTimer.Dispose();
            }
            Logger("ArchMage Helper Plugin Unloaded");
        }

        /// <summary>
        /// CoreBots.cs
        /// Logs a line of text to the script log with time, method from where it's called and a message
        /// </summary>
        public void Logger(string message = "", [CallerMemberName] string caller = "", bool messageBox = false, bool stopBot = false)
        {
            Bot.Log($"[{DateTime.Now:HH:mm:ss}] ({caller})  {message}");
            if (Bot.Player.LoggedIn)
                Bot.Send.ClientModerator(message.Replace('[', '(').Replace(']', ')'), caller);
        }
    }
}
