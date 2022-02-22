using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic
{
    public sealed class CommandProcessor
    {
        private struct Token
        {
            public enum TokenType
            {
                Print,
                Message,
                Kick,
                TeleportPos,
                TeleportWorld,
                ListPlayers,
                ListWorlds,
                FindPlayer,
                FindWorld,
                Identifier,
                From,
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
                if (Position == Source.Length)
                    return new Token(Token.TokenType.End, string.Empty);

                string tokStr = string.Empty;
                for(; Position < Source.Length && Source[Position] != ' '; Position++)
                    tokStr += Source[Position];

                switch (tokStr)
                {
                    case "p":
                        return new Token(Token.TokenType.Print, tokStr);
                    case "m":
                        return new Token(Token.TokenType.Message, tokStr);
                    case "k":
                        return new Token(Token.TokenType.Kick, tokStr);
                    case "tp":
                        return new Token(Token.TokenType.TeleportPos, tokStr);
                    case "tpw":
                        return new Token(Token.TokenType.TeleportWorld, tokStr);
                    case "lp":
                        return new Token(Token.TokenType.ListPlayers, tokStr);
                    case "lw":
                        return new Token(Token.TokenType.ListWorlds, tokStr);
                    case "fp":
                        return new Token(Token.TokenType.FindPlayer, tokStr);
                    case "fw":
                        return new Token(Token.TokenType.FindWorld, tokStr);
                    case "<|":
                        return new Token(Token.TokenType.From, tokStr);
                    default:
                        return new Token(Token.TokenType.Identifier, tokStr);
                }
            }
        }

        public CommandProcessor()
        {
            
        }


    }
}
