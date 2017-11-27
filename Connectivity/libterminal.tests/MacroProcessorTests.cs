using libterminal.JobRunner;
using libterminal.JobRunner.MacroParser;
using libterminal.JobRunner.MacroProcessor;
using System;
using Xunit;

namespace libterminal.tests
{
    public class MacroProcessorTests
    {
        [Fact]
        public void SimpleString()
        {
            var testString = "This is a test";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });

            var job = new Job
            {
                Parameters = new System.Collections.Generic.List<JobValue>
                {
                }
            };
            var processor = new JobProcessor(job);
            var processorResult = processor.ProcessParts(parts);


            Assert.Equal(testString, processorResult);
        }

        [Fact]
        public void SimpleMacro()
        {
            var testString = "{{myValue}}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });

            var job = new Job
            {
                Parameters = new System.Collections.Generic.List<JobValue>
                {
                    new JobValue { Name = "myValue", Value="This is a test" }
                }
            };
            var processor = new JobProcessor(job);
            var processorResult = processor.ProcessParts(parts);


            Assert.Equal("This is a test", processorResult);
        }

        [Fact]
        public void BasicUrlFunction()
        {
            var testString = "{{Url(destination)}}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });

            var job = new Job
            {
                Parameters = new System.Collections.Generic.List<JobValue>
                {
                    new JobValue { Name = "destination", Value="http://www.meet.the.king.of.pizza/hello" }
                }
            };
            var processor = new JobProcessor(job);
            var processorResult = processor.ProcessParts(parts);


            Assert.Equal("http://www.meet.the.king.of.pizza/hello", processorResult);
        }

        [Fact]
        public void PropertyOfUrl()
        {
            var testString = "{{Url(destination).Host}}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });

            var job = new Job
            {
                Parameters = new System.Collections.Generic.List<JobValue>
                {
                    new JobValue { Name = "destination", Value="http://www.meet.the.king.of.pizza/hello" }
                }
            };
            var processor = new JobProcessor(job);
            var processorResult = processor.ProcessParts(parts);


            Assert.Equal("www.meet.the.king.of.pizza", processorResult);
        }

        [Fact]
        public void StripUserInfoFromUrl()
        {
            var testString = "{{Url(destination).StripUserInfo}}";

            var p = new Parser();
            var parts = p.ParseParts(new ParserState { Text = testString });

            var job = new Job
            {
                Parameters = new System.Collections.Generic.List<JobValue>
                {
                    new JobValue { Name = "destination", Value="http://bob:minion@www.meet.the.king.of.pizza/hello" }
                }
            };
            var processor = new JobProcessor(job);
            var processorResult = processor.ProcessParts(parts);


            Assert.Equal("http://www.meet.the.king.of.pizza/hello", processorResult);
        }
    }
}
