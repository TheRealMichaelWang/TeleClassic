using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeleClassic.Networking;
using static TeleClassic.CommandProcessor;

namespace TeleClassic
{
    public sealed class CommandProcessor
    {
        public interface CommandObject
        {
            public void ToString(StringBuilder stringBuilder);
        }

        public interface CommandAction
        {
            public void Invoke(CommandProcessor commandProcessor);
            public int GetExpectedArgumentCount();
            public bool ReturnsValue();

            public string GetName();
            public string GetDescription();
        }

        public sealed class StringCommandObject : CommandObject
        {
            public string String { get; private set; }

            public StringCommandObject(string @string)
            {
                this.String = @string;
            }

            public void ToString(StringBuilder stringBuilder) => stringBuilder.Append(String + "\n");
        }

        public sealed class PlayerCommandObject : CommandObject
        {
            public List<PlayerSession> playerSessions;

            public PlayerCommandObject(List<PlayerSession> playerSessions)
            {
                this.playerSessions = playerSessions;
            }

            public void ToString(StringBuilder stringBuilder)
            {
                foreach (PlayerSession player in playerSessions)
                    stringBuilder.Append(player.Name + "\n");
            }
        }

        public sealed class WorldCommandObject : CommandObject
        {
            public List<MultiplayerWorld> worlds;

            public WorldCommandObject(List<MultiplayerWorld> worlds)
            {
                this.worlds = worlds;
            }

            public void ToString(StringBuilder stringBuilder)
            {
                foreach (MultiplayerWorld world in this.worlds)
                    stringBuilder.Append(world.Name + "\n");
            }
        }

        public sealed class HelpCommandAction : CommandAction
        {
            CommandParser commandParser;

            public int GetExpectedArgumentCount() => 0;
            public bool ReturnsValue() => false;

            public string GetName() => "help";
            public string GetDescription() => "Lists commands and their descriptions.";

            public HelpCommandAction(CommandParser commandParser)
            {
                this.commandParser = commandParser;
            }

            public void Invoke(CommandProcessor commandProcessor)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("There are " + commandParser.AvailibleCommands.Count + "availible command(s).");
                foreach (CommandAction availibleCommand in commandParser.AvailibleCommands.Values)
                    stringBuilder.AppendLine(availibleCommand.GetName() + " - " + availibleCommand.GetDescription());
                commandProcessor.Print(stringBuilder.ToString());
            }
        }

        public abstract class PrintCommandAction : CommandAction
        {
            public int GetExpectedArgumentCount() => 1;
            public bool ReturnsValue() => false;

            public string GetName() => "print";
            public string GetDescription() => "Prints data/output to you.";

            public void Invoke(CommandProcessor commandProcessor)
            {
                StringBuilder stringBuilder = new StringBuilder();
                commandProcessor.PopObject().ToString(stringBuilder);
                this.Print(stringBuilder.ToString().TrimEnd('\r','\n'));
            }

            public abstract void Print(string message);
        }

        public sealed class ConcatonateCommandAction : CommandAction
        {
            public int GetExpectedArgumentCount() => 2;
            public bool ReturnsValue() => true;

            public string GetName() => "add";
            public string GetDescription() => "combines two lists and returns a new list.";

            public void Invoke(CommandProcessor commandProcessor)
            {
                CommandObject a = commandProcessor.PopObject();
                CommandObject b = commandProcessor.PopObject(a.GetType());
                if (a.GetType() == typeof(PlayerCommandObject))
                {
                    PlayerCommandObject aPlayer = (PlayerCommandObject)a;
                    PlayerCommandObject bPlayer = (PlayerCommandObject)b;
                    List<PlayerSession> combinedPlayers = new List<PlayerSession>(aPlayer.playerSessions.Count + bPlayer.playerSessions.Count);
                    combinedPlayers.AddRange(aPlayer.playerSessions);
                    combinedPlayers.AddRange(bPlayer.playerSessions);
                    commandProcessor.PushObject(new PlayerCommandObject(combinedPlayers));
                }
                else if (a.GetType() == typeof(WorldCommandObject))
                {
                    WorldCommandObject aWorld = (WorldCommandObject)a;
                    WorldCommandObject bWorld = (WorldCommandObject)b;
                    List<MultiplayerWorld> combinedWorlds = new List<MultiplayerWorld>(aWorld.worlds.Count + bWorld.worlds.Count);
                    combinedWorlds.AddRange(aWorld.worlds);
                    combinedWorlds.AddRange(bWorld.worlds);
                    commandProcessor.PushObject(new WorldCommandObject(combinedWorlds));
                }
                else
                    throw new ArgumentException("Type error, expected player or world command object.");
            }
        }

        public sealed class FindPlayersCommandAction : CommandAction
        {
            public int GetExpectedArgumentCount() => 2;
            public bool ReturnsValue() => true;

            public string GetName() => "fp";
            public string GetDescription() => "Finds a players whos username in a selection of players";

            public void Invoke(CommandProcessor commandProcessor)
            {
                StringCommandObject query = (StringCommandObject)commandProcessor.PopObject(typeof(StringCommandObject));
                PlayerCommandObject players = (PlayerCommandObject)commandProcessor.PopObject(typeof(PlayerCommandObject));

                List<PlayerSession> matchingPlayers = new List<PlayerSession>();
                foreach (PlayerSession player in players.playerSessions)
                    if (player.Name.Contains(query.String))
                        matchingPlayers.Add(player);

                commandProcessor.PushObject(new PlayerCommandObject(matchingPlayers));
            }
        }

        public sealed class ExcludePlayersCommandAction : CommandAction
        {
            public int GetExpectedArgumentCount() => 2;
            public bool ReturnsValue() => true;

            public string GetName() => "ep";
            public string GetDescription() => "Excludes a players whos username in a selection of players.";

            public void Invoke(CommandProcessor commandProcessor)
            {
                StringCommandObject query = (StringCommandObject)commandProcessor.PopObject(typeof(StringCommandObject));
                PlayerCommandObject players = (PlayerCommandObject)commandProcessor.PopObject(typeof(PlayerCommandObject));

                List<PlayerSession> matchingPlayers = new List<PlayerSession>();
                foreach (PlayerSession player in players.playerSessions)
                    if (!player.Name.Contains(query.String))
                        matchingPlayers.Add(player);

                commandProcessor.PushObject(new PlayerCommandObject(matchingPlayers));
            }
        }

        public sealed class MessageCommandAction : CommandAction
        {
            public int GetExpectedArgumentCount() => 2;
            public bool ReturnsValue() => false;

            public string GetName() => "m";
            public string GetDescription() => "Sends a message to a selection of players.";

            public void Invoke(CommandProcessor commandProcessor)
            {
                StringCommandObject message = (StringCommandObject)commandProcessor.PopObject(typeof(StringCommandObject));
                PlayerCommandObject players = (PlayerCommandObject)commandProcessor.PopObject(typeof(PlayerCommandObject));

                if (commandProcessor.Permissions < Permission.Operator)
                    throw new ArgumentException("Insufficient permissions to send batch messages.");

                foreach (PlayerSession player in players.playerSessions)
                    player.Message(message.String);
            }
        }
        
        public class PushCommandObjectCommandAction : CommandAction
        {
            public int GetExpectedArgumentCount() => throw new NotImplementedException();
            public bool ReturnsValue() => throw new NotImplementedException();

            public string GetName() => throw new NotImplementedException();
            public string GetDescription() => throw new NotImplementedException();

            public CommandObject LiteralToPush;

            public PushCommandObjectCommandAction(CommandObject literalToPush)
            {
                this.LiteralToPush = literalToPush;
            }

            public void Invoke(CommandProcessor commandProcessor) => commandProcessor.PushObject(this.LiteralToPush);
        }

        public static ConcatonateCommandAction concatonateCommandAction = new ConcatonateCommandAction();
        public static FindPlayersCommandAction findPlayersCommandAction = new FindPlayersCommandAction();
        public static ExcludePlayersCommandAction excludePlayersCommandAction = new ExcludePlayersCommandAction();
        public static MessageCommandAction messageCommandAction = new MessageCommandAction();

        public Permission Permissions { get; private set; }

        private Stack<CommandObject> stack;
        private PrintCommandAction printCommandAction;
        
        public CommandProcessor(Permission permissions, PrintCommandAction printCommandAction)
        {
            this.Permissions = permissions;
            this.printCommandAction = printCommandAction;
            stack = new Stack<CommandObject>();
        }

        public CommandObject PopObject()
        {
            if (stack.Count == 0)
                throw new ArgumentException("Expected more operands on stack.");
            return stack.Pop();
        }

        public CommandObject PopObject(Type commandObjectType)
        {
            if (!commandObjectType.IsAssignableTo(typeof(CommandObject)))
                throw new ArgumentException("Cannot expect popped object to be of non-CommandObject type.");
            CommandObject commandObject = this.PopObject();
            if (!commandObject.GetType().IsAssignableTo(commandObjectType))
                throw new ArgumentException("Type error, expected " + commandObjectType.Name+ ".");
            return commandObject;
        }

        public void PushObject(CommandObject @object) => stack.Push(@object);

        public void ExecuteCommand(List<CommandAction> commands)
        {
            foreach (CommandAction command in commands)
            {
                try
                {
                    command.Invoke(this);
                }
                catch (ArgumentException e)
                {
                    this.Print("Runtime Error: " + e.Message + "\n while executing command "+command.GetName()+".");
                    return;
                }
            }
        }

        public void Print(string message) => printCommandAction.Print(message);
    }

    public sealed class CommandParser
    {
        private struct Token
        {
            public enum TokenType
            {
                Help,
                Print,
                Concatonate,
                Identifier,
                Semicolon,
                Comma,
                OpenParen,
                CloseParen,
                End
            }

            public TokenType Type;
            public string Identifier;

            public Token(TokenType type, string identifier)
            {
                this.Type = type;
                this.Identifier = identifier;
            }
        }

        private class Lexer
        {
            public readonly string Source;
            public int Position;

            public Lexer(string source)
            {
                this.Source = source;
                this.Position = 0;
            }

            public Token ScanTok()
            {
                string tokStr = string.Empty;
                while (Position != Source.Length && Source[Position] == ' ')
                    Position++;

                if (Position == Source.Length)
                    return new Token(Token.TokenType.End, string.Empty);

                if (char.IsLetter(Source[Position]))
                {
                    for (; Position < Source.Length && char.IsLetter(Source[Position]); Position++)
                        tokStr += Source[Position];
                    switch (tokStr)
                    {
                        case "h":
                            return new Token(Token.TokenType.Help, tokStr);
                        case "p":
                            return new Token(Token.TokenType.Print, tokStr);
                        case "+":
                            return new Token(Token.TokenType.Concatonate, tokStr);
                        default:
                            return new Token(Token.TokenType.Identifier, tokStr);
                    }
                }
                else
                {
                    char c = Source[Position];
                    Position++;
                    switch (c)
                    {
                        case ';':
                            return new Token(Token.TokenType.Semicolon, string.Empty);
                        case ',':
                            return new Token(Token.TokenType.Comma, string.Empty);
                        case '(':
                            return new Token(Token.TokenType.OpenParen, string.Empty);
                        case ')':
                            return new Token(Token.TokenType.CloseParen, string.Empty);
                        default:
                            throw new ArgumentException("Unrecognized token '" + c + "'.");
                    }
                }
            }
        }

        public Dictionary<string, CommandAction> AvailibleCommands;
        public readonly PrintCommandAction printCommandAction;
        private HelpCommandAction helpCommandAction;

        public void AddCommand(CommandAction commandAction) => AvailibleCommands.Add(commandAction.GetName(), commandAction);

        public CommandParser(PrintCommandAction printCommandAction)
        {
            this.printCommandAction = printCommandAction;
            AvailibleCommands = new Dictionary<string, CommandAction>();
            AvailibleCommands.Add("print", this.printCommandAction);
            AvailibleCommands.Add("add", concatonateCommandAction);

            AddCommand(MultiplayerWorld.getPlayerListCommandAction);
            AddCommand(findPlayersCommandAction);
            AddCommand(excludePlayersCommandAction);
            AddCommand(Server.getAllPlayersCommandAction);
            AddCommand(WorldManager.getWorldListCommandAction);
            AddCommand(WorldManager.generatePersonalWorldCommandAction);
            AddCommand(WorldManager.findWorldCommandAction);
            AddCommand(messageCommandAction);
            AddCommand(Blacklist.kickPlayerCommandAction);
            AddCommand(Blacklist.banPlayerCommandAction);
            AddCommand(Blacklist.temporaryBanPlayerCommandAction);
            AddCommand(helpCommandAction = new HelpCommandAction(this));
        }

        private void MatchNextTok(Lexer lexer, Token.TokenType tokenType)
        {
            Token scanned = lexer.ScanTok();
            if (scanned.Type != tokenType)
                throw new ArgumentException("Unexpected token " + scanned.Type + ".");
        }

        private void CompileValue(Lexer lexer, List<CommandAction> commands)
        {
            Token opTok = lexer.ScanTok();
            switch (opTok.Type)
            {
                case Token.TokenType.Concatonate:
                    CompileValue(lexer, commands);
                    MatchNextTok(lexer, Token.TokenType.Comma);
                    CompileValue(lexer, commands);
                    commands.Add(concatonateCommandAction);
                    break;
                case Token.TokenType.Identifier:
                    {
                        if (AvailibleCommands.ContainsKey(opTok.Identifier))
                        {
                            CommandAction command = AvailibleCommands[opTok.Identifier];
                            if (!command.ReturnsValue())
                                throw new ArgumentException("Command " + opTok.Identifier + " doesn't return a value.");
                            if (command.GetExpectedArgumentCount() > 0)
                            {
                                if (command.GetExpectedArgumentCount() > 1)
                                    MatchNextTok(lexer, Token.TokenType.OpenParen);
                                for (int i = 0; i < command.GetExpectedArgumentCount(); i++)
                                {
                                    if (i > 0)
                                        MatchNextTok(lexer, Token.TokenType.Comma);
                                    CompileValue(lexer, commands);
                                }
                                if (command.GetExpectedArgumentCount() > 1)
                                    MatchNextTok(lexer, Token.TokenType.CloseParen);
                            }
                            commands.Add(command);
                        }
                        else
                            commands.Add(new PushCommandObjectCommandAction(new StringCommandObject(opTok.Identifier)));
                        break;
                    }
                default:
                    throw new ArgumentException("Unexpected token " + opTok.Type + ".");
            }
        }

        private void CompileStatement(Lexer lexer, List<CommandAction> commands)
        {
            Token opTok = lexer.ScanTok();
            switch (opTok.Type)
            {
                case Token.TokenType.Help:
                    commands.Add(helpCommandAction);
                    break;
                case Token.TokenType.Print:
                    CompileValue(lexer, commands);
                    commands.Add(printCommandAction);
                    break;
                case Token.TokenType.Identifier:
                    if (!AvailibleCommands.ContainsKey(opTok.Identifier))
                        throw new ArgumentException("Unkown command " + opTok.Identifier + ".");
                    CommandAction command = AvailibleCommands[opTok.Identifier];
                    if (command.GetExpectedArgumentCount() > 0)
                    {
                        if(command.GetExpectedArgumentCount() > 1)
                            MatchNextTok(lexer, Token.TokenType.OpenParen);
                        for (int i = 0; i < command.GetExpectedArgumentCount(); i++)
                        {
                            if (i > 0)
                                MatchNextTok(lexer, Token.TokenType.Comma);
                            CompileValue(lexer, commands);
                        }
                        if (command.GetExpectedArgumentCount() > 1)
                            MatchNextTok(lexer, Token.TokenType.CloseParen);
                    }
                    commands.Add(command);
                    if (command.ReturnsValue())
                        commands.Add(printCommandAction);
                    break;
                default:
                    throw new ArgumentException("Unexpected token " + opTok.Type + ".");
            }

            Token finalTok = lexer.ScanTok();
            if (finalTok.Type == Token.TokenType.Semicolon)
                CompileStatement(lexer, commands);
            else if (finalTok.Type != Token.TokenType.End)
                throw new ArgumentException("Unexpected token " + finalTok.Type + ".");
        }

        public List<CommandAction> Compile(string source)
        {
            Lexer lexer = new Lexer(source);
            List<CommandAction> commands = new List<CommandAction>();
            CompileStatement(lexer, commands);
            return commands;
        }
    }
}
