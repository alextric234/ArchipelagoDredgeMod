using System.Collections.Generic;
using System.Linq;
using ArchipelagoDredge.Network;
using CommandTerminal;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

public class TerminalCommandManager
{
    public static void Initialize()
    {
        Terminal.Shell.AddCommand("apconnect", ConnectToArchipelago, 3, 4, "connect <ip> <port> <slotname> <password>");
    }

    private static void ConnectToArchipelago(CommandArg[] args)
    {
        WinchCore.Log.Info("Command received");
        args.ForEach(arg => WinchCore.Log.Info(arg.ToString()));
        var apHost = args[0].ToString();
        var apPort = args[1].ToString();
        var slotName = args[2].ToString();
        var passWord = args.ElementAtOrDefault(3).ToString() ?? "";
        ConnectionManager.TryConnect(apHost, apPort, slotName, passWord);
    }

}