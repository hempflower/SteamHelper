using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteamHelper
{
    class Program
    {
        public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);

        [Option(Description = "Steam account",ShortName = "u")]
        [Required]
        public string Username { get; } = "";

        [Option(Description = "Steam password", ShortName = "p")]
        [Required]
        public string Password { get; } = "";

        [Option(Description = "Url", ShortName = "ur")]
        public string SteamUrl { get; } = "";

        private void OnExecute()
        {
            var steamExe = Utils.FindSteamExePath();
            if (steamExe == "")
            {
                Environment.Exit(-1);
            }
            Console.WriteLine("Kill Steam Client");
            Utils.KillSteamClient();
            List<string> argsList = new List<string>();

            // Login args
            argsList.Add("-u");
            argsList.Add(Username);
            argsList.Add(Password);
            argsList.Add("-vgui");
            argsList.Add("-nofriendsui");
            argsList.Add("-noverifyfiles");
            argsList.Add("-nodircheck");
            argsList.Add("-skipinitialbootstrap");
            argsList.Add("-nobootstrapperupdate");
            if (SteamUrl != "")
            {
                argsList.Add("--");
                argsList.Add(SteamUrl);
            }
            Console.WriteLine("Launch Steam Client");
            Utils.LaunchSteam(string.Join(" ", argsList));
            var cts = new CancellationTokenSource();
            Utils.SteamAutoLogin(Username,Password,true,30,cts.Token);
        }
    }
}
