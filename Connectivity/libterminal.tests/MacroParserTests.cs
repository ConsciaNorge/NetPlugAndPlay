using libterminal.JobRunner.MacroParser;
using System;
using Xunit;

namespace libterminal.tests
{
    public class MacroParserTests
    {
        [Fact]
        public void SimpleString()
        {
            var testString = "This is a test";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });

            Assert.Equal(1, parts.Parts.Count);
            Assert.Equal(testString, parts.Parts[0].Text);
            Assert.IsType(typeof(PoText), parts.Parts[0]);
        }

        [Fact]
        public void SimpleMacro()
        {
            var testString = "{{myValue}}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });

            Assert.Equal(1, parts.Parts.Count);
            Assert.Equal(testString, parts.Parts[0].Text);
            Assert.IsType(typeof(PoMacro), parts.Parts[0]);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.IsType(typeof(PoLiteralMember), (parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.Equal("myValue", ((parts.Parts[0] as PoMacro).Expression.Value.Member as PoLiteralMember).Text);
        }

        [Fact]
        public void SimpleFunction()
        {
            var testString = "{{MyLittleFunction()}}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });
            Assert.Equal(testString, parts.Parts[0].Text);
            Assert.IsType(typeof(PoMacro), parts.Parts[0]);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.IsType(typeof(PoFunctionCall), (parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.NotNull(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Literal);
            Assert.Equal("MyLittleFunction", ((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Literal.Text);
            Assert.Null(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression);
            Assert.Null((parts.Parts[0] as PoMacro).Expression.Value.Value);
        }

        [Fact]
        public void SimpleFunctionTrailingSpace()
        {
            var testString = "{{MyLittleFunction() }}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });
            Assert.Equal(testString, parts.Parts[0].Text);
            Assert.IsType(typeof(PoMacro), parts.Parts[0]);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.IsType(typeof(PoFunctionCall), (parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.NotNull(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Literal);
            Assert.Equal("MyLittleFunction", ((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Literal.Text);
            Assert.Null(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression);
            Assert.Null((parts.Parts[0] as PoMacro).Expression.Value.Value);
        }

        [Fact]
        public void FunctionWithLiteralParameter()
        {
            var testString = "{{MyLittleFunction(parameterValue)}}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });
            Assert.Equal(testString, parts.Parts[0].Text);
            Assert.IsType(typeof(PoMacro), parts.Parts[0]);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.IsType(typeof(PoFunctionCall), (parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.NotNull(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Literal);
            Assert.Equal("MyLittleFunction", ((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Literal.Text);
            Assert.NotNull(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression);
            Assert.NotNull(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression.Value);
            Assert.NotNull(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression);
            Assert.IsType(typeof(PoLiteralMember), ((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression.Value.Member);
            Assert.NotNull((((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression.Value.Member as PoLiteralMember).Literal);
            Assert.Equal("parameterValue", (((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression.Value.Member as PoLiteralMember).Literal.Text);
        }

        [Fact]
        public void PropertyOfFunctionResult()
        {
            var testString = "{{MyLittleFunction().BobsPizza}}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });
            Assert.Equal(testString, parts.Parts[0].Text);
            Assert.IsType(typeof(PoMacro), parts.Parts[0]);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.IsType(typeof(PoFunctionCall), (parts.Parts[0] as PoMacro).Expression.Value.Member);
            Assert.NotNull(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Literal);
            Assert.Equal("MyLittleFunction", ((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Literal.Text);
            Assert.Null(((parts.Parts[0] as PoMacro).Expression.Value.Member as PoFunctionCall).Expression);
            Assert.NotNull((parts.Parts[0] as PoMacro).Expression.Value.Value);
            Assert.IsType(typeof(PoValue), (parts.Parts[0] as PoMacro).Expression.Value.Value);
            Assert.NotNull(((parts.Parts[0] as PoMacro).Expression.Value.Value as PoValue).Member);
            Assert.IsType(typeof(PoLiteralMember), ((parts.Parts[0] as PoMacro).Expression.Value.Value as PoValue).Member);
            Assert.NotNull((((parts.Parts[0] as PoMacro).Expression.Value.Value as PoValue).Member as PoLiteralMember));
            Assert.NotNull((((parts.Parts[0] as PoMacro).Expression.Value.Value as PoValue).Member as PoLiteralMember).Literal);
            Assert.Equal("BobsPizza", (((parts.Parts[0] as PoMacro).Expression.Value.Value as PoValue).Member as PoLiteralMember).Literal.Text);
        }
    }
}
