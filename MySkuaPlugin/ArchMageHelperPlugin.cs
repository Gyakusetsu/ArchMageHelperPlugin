using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Skua.Core.Interfaces;

namespace RimuruPlugin
{
    public class ArchMageHelperPlugin : ISkuaPlugin
    {

        public static IScriptInterface Bot => IScriptInterface.Instance;

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

        private Thread? HelperThread { get; set; } = null;

    private void ArcaneFluxListener()
        {
            while (true)
            {
                if (Bot.Player.CurrentClass?.Name == "ArchMage"
                    && Bot.Player.InCombat
                    && Bot.Self.HasActiveAura("Arcane Flux"))
                {
                    switch (SelectedAscensionMode)
                    {
                        case AscensionMode.Corporeal:
                            if (!Bot.Self.HasActiveAura("Corporeal Ascension"))
                            {
                                Logger("Activating Corporeal Ascension");
                                Bot.Skills.UseSkill(4);
                            }
                            break;
                        case AscensionMode.Astral:
                            if (!Bot.Self.HasActiveAura("Astral Ascension"))
                            {
                                Logger("Activating Astral Ascension");
                                Bot.Skills.UseSkill(4);
                            }
                            break;
                    }
                }
                Thread.Sleep(500);
            }
        }

        public void Load(IServiceProvider provider, IPluginHelper helper)
        {
            HelperThread = new Thread(new ThreadStart(ArcaneFluxListener)){ IsBackground = true };
            HelperThread.Start();

            Logger("ArchMage Helper Plugin Loaded");

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
        }

        public void Unload()
        {
            HelperThread.Abort();
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
