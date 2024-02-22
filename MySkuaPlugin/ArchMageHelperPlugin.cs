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
        private string SelectedAuraName = CORPOREAL_AURA_NAME;

        private System.Timers.Timer? MainHelperTimer;
        private System.Timers.Timer? DamageBoostHelperTimer;

        private void ArcaneFluxListener(Object? source, ElapsedEventArgs e)
        {
            if (Bot.Self.HasActiveAura("Arcane Flux"))
            {
                switch (SelectedAscensionMode)
                {
                    case AscensionMode.Corporeal:
                        if (Bot.Player.Stats?.CriticalChance < 1.0f)
                         //   || !Bot.Self.HasActiveAura(CORPOREAL_AURA_NAME))
                        {
                            Bot.Skills.UseSkill(4);
                        }
                        break;
                    case AscensionMode.Astral:
                        if (!Bot.Self.HasActiveAura(ASTRAL_AURA_NAME))
                        {
                            Bot.Skills.UseSkill(4);
                        }
                        break;
                }
            }
        }

        private void DamageBoostListener(Object? source, ElapsedEventArgs e)
        {
            if (Bot.Self.HasActiveAura(SelectedAuraName)
                && !Bot.Self.HasActiveAura("Arcane Sigil"))
            {
                Bot.Skills.UseSkill(4);
            }
        }

        public void Load(IServiceProvider provider, IPluginHelper helper)
        {
            MainHelperTimer = new System.Timers.Timer(100);
            MainHelperTimer.Elapsed += ArcaneFluxListener;
            MainHelperTimer.AutoReset = true;
            MainHelperTimer.Enabled = true;

            DamageBoostHelperTimer = new System.Timers.Timer(150);
            DamageBoostHelperTimer.Elapsed += DamageBoostListener;
            DamageBoostHelperTimer.AutoReset = true;
            DamageBoostHelperTimer.Enabled = true;

            helper.AddMenuButton("Use Corporeal Ascension Mode", delegate
            {
                SelectedAscensionMode = AscensionMode.Corporeal;
                SelectedAuraName = CORPOREAL_AURA_NAME;
                Logger("Switched to Corporeal Ascension");
            });
            helper.AddMenuButton("Use Astral Ascension Mode", delegate
            {
                SelectedAscensionMode = AscensionMode.Astral;
                SelectedAuraName = ASTRAL_AURA_NAME;
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
