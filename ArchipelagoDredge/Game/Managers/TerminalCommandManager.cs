using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArchipelagoDredge.Network;
using CommandTerminal;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

public class TerminalCommandManager
{
    public static void Initialize()
    {
        Terminal.Shell.AddCommand("ap", ArchipelagoCommands, 1, int.MaxValue, "commands for archipelago multiworld");
    }

    public static void LogMessage(TerminalLogType logType, string message)
    {
        Terminal.Log(logType, message);
    }

    private static void ArchipelagoCommands(CommandArg[] args)
    {
        var commandArgs = args.Skip(1).ToArray();
        switch (args[0].ToString().ToLower())
        {
            case "help":
                HelpCommand(commandArgs);
                break;
            case "connect":
                ConnectCommand(commandArgs);
                break;
            case "disconnect":
                DisconnectCommand(commandArgs);
                break;
            case "received":
                ReceivedCommand(commandArgs);
                break;
            case "items":
                ItemsCommand(commandArgs);
                break;
            case "locations":
                LocationsCommand(commandArgs);
                break;
            case "ready":
                ReadyCommand(commandArgs);
                break;
            case "hint":
                HintCommand(commandArgs);
                break;
            case "hint_location":
                HintLocationCommand(commandArgs);
                break;
            case "say":
                SayCommand(commandArgs);
                break;
            default:
                UnrecognizedCommand(commandArgs);
                break;
        }
    }

    private static void SayCommand(CommandArg[] commandArgs)
    {
    }

    private static void UnrecognizedCommand(CommandArg[] args)
    {
    }

    private static void HintLocationCommand(CommandArg[] args)
    {
    }

    private static void HintCommand(CommandArg[] args)
    {
    }

    private static void ReadyCommand(CommandArg[] args)
    {
    }

    private static void LocationsCommand(CommandArg[] args)
    {
    }

    private static void ItemsCommand(CommandArg[] args)
    {
    }

    private static void ReceivedCommand(CommandArg[] args)
    {
    }

    private static void DisconnectCommand(CommandArg[] args)
    {
        ArchipelagoCommandManager.Disconnect();
    }

    private static void ConnectCommand(CommandArg[] args)
    {
        if (args.Length == 0)
        {
            ArchipelagoCommandManager.ConfigConnect();
            return;
        }

        if (args.Length < 3)
        {
            WinchCore.Log.Error("Usage: /ap connect <ip> <port> <slotName> [password]");
            return;
        }

        if (!int.TryParse(args[1].ToString(), out var port) || port <= 0 || port > 65535)
            WinchCore.Log.Error("Invalid port number");

        var host = args[0].ToString();

        var password = "";
        var slotParts = new List<string>();

        for (var i = 2; i < args.Length; i++)
        {
            var token = args[i].ToString();

            if (token == "-p" || token == "--password")
            {
                if (i + 1 < args.Length)
                    password = args[i + 1].ToString();
                else
                    password = "";
                break;
            }

            if (token.StartsWith("password=", StringComparison.OrdinalIgnoreCase))
            {
                password = token.Substring("password=".Length);
                break;
            }

            slotParts.Add(token);
        }

        var slot = string.Join(" ", slotParts).Trim();

        if (string.IsNullOrEmpty(slot))
        {
            WinchCore.Log.Error(
                "Slot name is required. Usage: /ap connect <host> <port> <slot name...> [-p <password>]");
            return;
        }

        ArchipelagoCommandManager.TryConnect(host, port, slot, password);
    }

    private static void HelpCommand(CommandArg[] args)
    {
        var helpMessage = new StringBuilder();
        helpMessage.AppendLine("Commands:");
        helpMessage.AppendLine("help: lists commands");
        helpMessage.AppendLine("connect <ip> <port> <slotname> <password>: connects to archipelago multiworld");
        helpMessage.AppendLine("disconnect: disconnects from multiworld");
        helpMessage.AppendLine("received: lists all received items (not implemented)");
        helpMessage.AppendLine("items: lists all items (not implemented)");
        helpMessage.AppendLine("locations: lists all locations (not implemented)");
        helpMessage.AppendLine("ready: sends ready status to server (not implemented)");
        helpMessage.AppendLine("hint <itemname>: spends hint points for item hint (not implemented)");
        helpMessage.AppendLine("hint_location <locationname>: spends hint points for location hint (not implemented)");
        helpMessage.AppendLine("say <message>: broadcasts message to server (not implemented)");

        Terminal.Log(TerminalLogType.Message, helpMessage.ToString());
    }
}