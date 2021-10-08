﻿using System.Threading.Tasks;
using Blowin.Required.Features;
using Xunit;
using VerifyCS = Blowin.Required.Test.CSharpCodeFixVerifier<
    Blowin.Required.BlowinRequiredAnalyzer,
    Blowin.Required.BlowinRequiredCodeFixProvider>;

namespace Blowin.Required.Test
{
    public class RequiredInitializerFeatureTest
    {
        [Theory]
        [InlineData(@"class Person
        {
        public string Name { get; set; }
        public int Age { get; set; }
    }")]
        [InlineData(@"class Person
        {
        public string Name { get; set; }
        public int Age { get; set; }

        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }
    }")]
        [InlineData(@"
using System;

class RequiredAttribute : Attribute { }
class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }
        }")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            private Person()
            {
                Age = 10;
            }

            public static Person Create()
            {
                return new Person()
                {
                    Name = """"
                };
            }
        }")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public static Person Create()
            {
                return new Person()
                {
                    Age = 200
                };
            }
        }")]
        public async Task Valid(string test)
        {
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        
        [Theory]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            private Person()
            {
                Name = ""ttt"";
            }

            public static Person Create()
            {
                return {|#0:new Person()
                {
                    Name = """"
                }|};
            }
        }", "Age")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public static Person Create()
            {
                return {|#0:new Person()|};
            }
        }", "Age")]
        public async Task Invalid(string test, string argument)
        {
            var expected = VerifyCS.Diagnostic(RequiredInitializerFeature.DiagnosticId).WithLocation(0).WithArguments(argument);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}