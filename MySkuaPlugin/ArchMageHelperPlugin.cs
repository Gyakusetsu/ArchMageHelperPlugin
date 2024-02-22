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

        private void ArcaneFluxListener(Object? source, ElapsedEventArgs e)
        {
            if (Bot.Player.CurrentClass?.Name == "ArchMage"
                    && Bot.Self.HasActiveAura("Arcane Flux"))
            {
                switch (SelectedAscensionMode)
                {
                    case AscensionMode.Corporeal:
                        if (Bot.Player.Stats?.CriticalChance < 1.0f)
                         //   || !Bot.Self.HasActiveAura(CORPOREAL_AURA_NAME))
                        {
                            Bot.Skills.UseSkill(4);
                            Bot.Player.Stats?.CriticalChance.ToString();
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
            
            if (!Bot.Self.Auras.Any(a => a.Name is not null 
                && (a.Name.Equals("Arcane Flux", StringComparison.OrdinalIgnoreCase)
                    || a.Name.Equals("Arcane Sigil", StringComparison.OrdinalIgnoreCase)))
                && Bot.Self.HasActiveAura(SelectedAuraName))
            {
                /*
                    * 30% Damage boost
                */
                Bot.Skills.UseSkill(4);
            }
        }

        public void Load(IServiceProvider provider, IPluginHelper helper)
        {
            MainHelperTimer = new System.Timers.Timer(100);
            MainHelperTimer.Elapsed += ArcaneFluxListener;
            MainHelperTimer.Elapsed += DamageBoostListener;
            MainHelperTimer.AutoReset = true;
            MainHelperTimer.Enabled = true;

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
