using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libterminal.JobRunner.MacroParser
{
    public class PoParts : ParserObject
    {
        public List<PoPart> Parts = new List<PoPart>();
    }

    public class PoPart : ParserObject
    {

    }

    public class PoMacro : PoPart
    {
        public PoExpression Expression;
    }

    public class PoText : PoPart
    {

    }

    public class PoExpression : ParserObject
    {
        public PoValue Value { get; set; }
    }

    public class PoValue : ParserObject
    {
        public PoMember Member { get; set; }
        public PoValue Value { get; set; }
    }

    public class PoMember : ParserObject
    {
    }

    public class PoLiteralMember : PoMember
    {
        public PoLiteral Literal { get; set; }
    }

    public class PoFunctionCall : PoMember
    {
        public PoLiteralMember Literal { get; set; }
        public PoExpression Expression { get; set; }
    }

    public class PoLiteral : ParserObject
    {

    }

    public class Parser
    {
        public Parser()
        {

        }

        private void LogDebug(string message)
        {
            //System.Diagnostics.Debug.WriteLine(message);
        }

        private void LogDebug(string message, ParserState state)
        {
            //System.Diagnostics.Debug.WriteLine(message + "(index=" + state.Index + ", stack={" + string.Join(',', state.Stack.Select(x => x.ToString()).ToList()) + "})");
        }

        public PoParts ParseParts(ParserState state)
        {
            LogDebug("ParseParts - ", state);

            var start = state.Push();

            var part = ParsePart(state);
            if (part != null)
            {
                var parts = ParseParts(state);
                if (parts != null)
                {
                    parts.Length += part.Index - start;
                    parts.Index = start;

                    parts.Parts.Insert(0, part);

                    state.Release();
                    return parts;
                }

                state.Release();
                return new PoParts
                {
                    State = state,
                    Index = start,
                    Length = state.Index - start,
                    Parts = new List<PoPart>
                    {
                        part
                    }
                };
            }

            state.Pop();
            return null;
        }

        public PoPart ParsePart(ParserState state)
        {
            LogDebug("ParsePart - ", state);

            var macro = ParseMacro(state);
            if (macro != null)
                return macro;

            var text = ParseText(state);
            if (text != null)
                return text;

            return null;
        }

        public PoMacro ParseMacro(ParserState state)
        {
            LogDebug("ParseMacro - ", state);

            var start = state.Push();

            if (state.MatchText("{{") != null)
            {
                state.MatchText(@"\s*");

                var expression = ParseExpression(state);

                state.MatchText(@"\s*");
                if (state.MatchText("}}") != null)
                {
                    state.Release();
                    return new PoMacro
                    {
                        State = state,
                        Index = start,
                        Length = state.Index - start,
                        Expression = expression
                    };
                }
            }

            state.Pop();
            return null;
        }

        public PoText ParseText(ParserState state)
        {
            LogDebug("ParseText - ", state);

            var start = state.Push();

            var matchText = state.MatchText(@"(?:(?!\{\{).)+");
            if (matchText != null)
            {
                state.Release();
                return new PoText
                {
                    State = state,
                    Index = start,
                    Length = state.Index - start
                };
            }

            state.Pop();
            return null;
        }

        public PoExpression ParseExpression(ParserState state)
        {
            LogDebug("ParseExpression - ", state);

            var start = state.Push();

            var value = ParseValue(state);
            if (value != null)
            {
                state.Release();
                return new PoExpression
                {
                    State = state,
                    Index = start,
                    Length = state.Index - start,
                    Value = value
                };
            }

            state.Pop();
            return null;
        }

        public PoValue ParseValue(ParserState state)
        {
            LogDebug("ParseValue - ", state);

            var start = state.Push();

            var member = ParseMember(state);
            if(member != null)
            {
                state.Push();
                if(state.MatchText(@"\s*\.\s*") != null)
                {
                    var value = ParseValue(state);
                    if(value != null)
                    {
                        state.Release(2);
                        return new PoValue
                        {
                            State = state,
                            Index = start,
                            Length = state.Index - start,
                            Member = member,
                            Value = value
                        };
                    }
                }
                state.Pop();
                state.Release();
                return new PoValue
                {
                    State = state,
                    Index = start,
                    Length = state.Index - start,
                    Member = member,
                    Value = null
                };
            }

            state.Pop();
            return null;
        }

        public PoMember ParseMember(ParserState state)
        {
            LogDebug("ParseMember - ", state);

            var functionCall = ParseFunctionCall(state);
            if (functionCall != null)
                return functionCall;

            var literal = ParseLiteralMember(state);
            if (literal != null)
                return literal;

            return null;
        }

        public PoFunctionCall ParseFunctionCall(ParserState state)
        {
            LogDebug("ParseFunctionCall - ", state);

            var start = state.Push();

            var literalMember = ParseLiteralMember(state);
            if(literalMember != null)
            {
                if(state.MatchText(@"\s*\(\s*") != null)
                {
                    var expression = ParseExpression(state);    // Optional
                    if (expression != null)
                        state.MatchText(@"\s*");

                    if(state.MatchText(@"\)") != null)
                    {
                        state.Release();
                        return new PoFunctionCall
                        {
                            State = state,
                            Index = start,
                            Length = state.Index - start,
                            Literal = literalMember,
                            Expression = expression
                        };
                    }
                }
            }

            state.Pop();
            return null;
        }

        public PoLiteralMember ParseLiteralMember(ParserState state)
        {
            LogDebug("ParseLiteralMember - ", state);

            var start = state.Push();

            var literal = ParseLiteral(state);
            if(literal != null)
            {
                state.Release();
                return new PoLiteralMember
                {
                    State = state,
                    Index = start,
                    Length = state.Index - start,
                    Literal = literal
                };
            }

            state.Pop();
            return null;
        }

        public PoLiteral ParseLiteral(ParserState state)
        {
            LogDebug("ParseLiteral - ", state);

            var start = state.Push();

            var matchText = state.MatchText(@"[A-Za-z_][A-Za-z0-9_]*");
            if (matchText != null)
            {
                state.Release();
                return new PoLiteral
                {
                    State = state,
                    Index = start,
                    Length = state.Index - start
                };
            }

            state.Pop();
            return null;
        }
    }
}
