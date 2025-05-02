using ArchipelagoDredge.Network;
using CommandTerminal;
using System.Linq;
using System.Text;

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
        return;
    }

    private static void UnrecognizedCommand(CommandArg[] args)
    {

        return;
    }

    private static void HintLocationCommand(CommandArg[] args)
    {
        return;
    }

    private static void HintCommand(CommandArg[] args)
    {
        return;
    }

    private static void ReadyCommand(CommandArg[] args)
    {
        return;
    }

    private static void LocationsCommand(CommandArg[] args)
    {
        return;
    }

    private static void ItemsCommand(CommandArg[] args)
    {
        return;
    }

    private static void ReceivedCommand(CommandArg[] args)
    {
        return;
    }

    private static void DisconnectCommand(CommandArg[] args)
    {
        ArchipelagoCommandManager.Disconnect();
    }

    private static void ConnectCommand(CommandArg[] args)
    {
        var apHost = args[0].ToString();
        var apPort = args[1].ToString();
        var slotName = args[2].ToString();
        var password = args.ElementAtOrDefault(3).ToString() ?? "";
        ArchipelagoCommandManager.TryConnect(apHost, apPort, slotName, password);
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