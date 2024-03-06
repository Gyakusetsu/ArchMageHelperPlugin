using System;
using System.Runtime.CompilerServices;
using Skua.Core.Interfaces;
using System.Timers;
using System.Threading;

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
            MainHelperTimer = new System.Timers.Timer(250);
            MainHelperTimer.Elapsed += ArcaneSigilListener;
            MainHelperTimer.AutoReset = true;
            MainHelperTimer.Enabled = true;

            DamageBoostHelperTimer = new System.Timers.Timer(250);
            DamageBoostHelperTimer.Elapsed += DamageBoostListener;
            DamageBoostHelperTimer.AutoReset = true;
            DamageBoostHelperTimer.Enabled = true;

            OptionLockTimer = new System.Timers.Timer(1000);
            OptionLockTimer.Elapsed += OptionLockListener;
            OptionLockTimer.AutoReset = true;
            OptionLockTimer.Enabled = true;

            UnloadTimer = new System.Timers.Timer(1000);
            UnloadTimer.Elapsed += UnloadPluginListener;
            UnloadTimer.AutoReset = true;
            UnloadTimer.Enabled = true;

            helper.AddMenuButton("Use Corporeal Ascension Mode", delegate
            {
                SelectedAscensionMode = AscensionMode.Corporeal;
                Logger("Switched to Corporeal Ascension");
            });
            helper.AddMenuButton("Use Astral Ascension Mode", delegate
            {
                SelectedAscensionMode = AscensionMode.Astral;
                Logger("Switched to Astral Ascension");
            });

            Logger("ArchMage Helper Plugin Loaded");
        }

        public void Unload()
        {
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
