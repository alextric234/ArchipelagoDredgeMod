using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDredge.Game.Helpers;
using ArchipelagoDredge.Network;
using ArchipelagoDredge.Utils;
using CommandTerminal;
using Winch.Core;

namespace ArchipelagoDredge.Game.Managers;

public static class TerminalCommandManager
{
    public static void Initialize()
    {
        Terminal.Shell.AddCommand("ap", ArchipelagoCommands, 1, int.MaxValue, "commands for archipelago multiworld");
    }

    public static void LogMessage(TerminalLogType logType, string message)
    {
        MainThreadDispatcher.RunOnMainThread(() => Print(logType, message));
    }

    // Terminal.Log treats its text as a format string, so text with braces (like server help) must go in as an argument.
    private static void Print(TerminalLogType logType, string text)
    {
        Terminal.Log(logType, "{0}", text);
    }

    private static void ArchipelagoCommands(CommandArg[] args)
    {
        var command = args[0].ToString();
        var commandArgs = args.Skip(1).ToArray();
        if (command.StartsWith("!"))
        {
            SendToServer(JoinArgs(args));
            return;
        }

        switch (command.ToLower())
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
                SendToServer($"!hint {JoinArgs(commandArgs)}".TrimEnd());
                break;
            case "hint_location":
                SendToServer($"!hint_location {JoinArgs(commandArgs)}".TrimEnd());
                break;
            case "say":
                SayCommand(commandArgs);
                break;
            default:
                Print(TerminalLogType.Error, $"Unknown command '{command}'. Type 'ap help' for the list.");
                break;
        }
    }

    private static string JoinArgs(CommandArg[] args)
    {
        return string.Join(" ", args.Select(arg => arg.ToString())).Trim();
    }

    // The server answers chat and !commands with a message, which shows up in the terminal like any other.
    private static void SendToServer(string text)
    {
        if (!RequireConnection())
        {
            return;
        }

        ArchipelagoClient.Session.Say(text);
    }

    private static bool RequireConnection()
    {
        if (ArchipelagoClient.IsConnected)
        {
            return true;
        }

        Print(TerminalLogType.Error, "Not connected to a multiworld. Use 'ap connect' first.");
        return false;
    }

    private static void SayCommand(CommandArg[] args)
    {
        var message = JoinArgs(args);
        if (message.Length == 0)
        {
            Print(TerminalLogType.Error, "Usage: ap say <message>");
            return;
        }

        SendToServer(message);
    }

    private static void ReadyCommand(CommandArg[] args)
    {
        if (!RequireConnection())
        {
            return;
        }

        ArchipelagoClient.Session.SetClientState(ArchipelagoClientState.ClientReady);
        Print(TerminalLogType.Message, "Sent ready status.");
    }

    private static void LocationsCommand(CommandArg[] args)
    {
        if (!RequireConnection())
        {
            return;
        }

        var locations = ArchipelagoClient.Session.Locations;
        var names = locations.AllMissingLocations
            .Select(id => locations.GetLocationNameFromId(id, ArchipelagoClient.GameName) ?? $"Location {id}");
        LogList("Unchecked locations", names, JoinArgs(args));
    }

    private static void ItemsCommand(CommandArg[] args)
    {
        var names = ItemNames.ItemsByName.Keys
            .Where(name => !name.EndsWith(ItemNames.ResearchedSuffix) && !name.Contains(ItemNames.StartingGearTag));
        LogList("DREDGE items", names, JoinArgs(args));
    }

    private static void ReceivedCommand(CommandArg[] args)
    {
        if (!RequireConnection())
        {
            return;
        }

        var lines = ArchipelagoClient.Session.Items.AllItemsReceived
            .Where(item => !item.ItemDisplayName.EndsWith(ItemNames.ResearchedSuffix))
            .Select(item => $"{item.ItemDisplayName} (from {item.Player.Alias})");
        LogList("Received items", lines, JoinArgs(args), sort: false);
    }

    private static void LogList(string title, IEnumerable<string> lines, string filter, bool sort = true)
    {
        var matching = lines.Where(line => line.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        var list = (sort ? matching.OrderBy(line => line) : matching).ToList();

        var heading = filter.Length == 0 ? $"{title} ({list.Count}):" : $"{title} matching '{filter}' ({list.Count}):";
        Print(TerminalLogType.Message, heading + Environment.NewLine + string.Join(Environment.NewLine, list));
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
        {
            WinchCore.Log.Error("Invalid port number");
            return;
        }

        var host = args[0].ToString();

        var password = "";
        var slotParts = new List<string>();

        for (var i = 2; i < args.Length; i++)
        {
            var token = args[i].ToString();

            if (token == "-p" || token == "--password")
            {
                if (i + 1 < args.Length)
                {
                    password = args[i + 1].ToString();
                }
                else
                {
                    password = "";
                }

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

        var deathLink = ApConfigHelper.ReadDeathLink();

        _ = ArchipelagoCommandManager.TryConnect(host, port, slot, password, deathLink);
    }

    private static void HelpCommand(CommandArg[] args)
    {
        var helpMessage = new StringBuilder();
        helpMessage.AppendLine("Commands:");
        helpMessage.AppendLine("help: lists commands");
        helpMessage.AppendLine("connect <ip> <port> <slotname> <password>: connects to archipelago multiworld");
        helpMessage.AppendLine("disconnect: disconnects from multiworld");
        helpMessage.AppendLine("received [filter]: lists items you've received");
        helpMessage.AppendLine("items [filter]: lists DREDGE item names, for hints");
        helpMessage.AppendLine("locations [filter]: lists locations you haven't checked yet");
        helpMessage.AppendLine("ready: sends ready status to server");
        helpMessage.AppendLine("hint [itemname]: hints an item, or lists your hints with no name");
        helpMessage.AppendLine("hint_location <locationname>: hints what's at a location");
        helpMessage.AppendLine("say <message>: sends a chat message to the multiworld");
        helpMessage.AppendLine("!<command>: runs any server command, e.g. ap !missing or ap !remaining");

        Print(TerminalLogType.Message, helpMessage.ToString());
    }
}