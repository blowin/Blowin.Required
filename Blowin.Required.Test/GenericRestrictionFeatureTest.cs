using System.Threading.Tasks;
using Blowin.Required.Features;
using Xunit;
using VerifyCS = Blowin.Required.Test.CSharpCodeFixVerifier<
    Blowin.Required.BlowinRequiredAnalyzer,
    Blowin.Required.BlowinRequiredCodeFixProvider>;

namespace Blowin.Required.Test
{
    public class GenericRestrictionFeatureTest
    {
        [Theory]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public static void Fail()
            {
                var tt = Access<{|#0:Person|}>.Test;
            }
        }

class Access<T> where T : new() 
{
    public static int Test = 20;
}", "Person")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public static void Fail()
            {
                Access<{|#0:Person|}>();
            }

            private static void Access<T>() where T : new(){}
        }
", "Person")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            static void Fail(IAccess<{|#0:Person|}> tt)
            {
            }
        }

interface IAccess<T> where T : new() 
{
}", "Person")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            static void Fail(IAccess tt)
            {
                tt.Test<{|#0:Person|}>();
            }
        }

interface IAccess
{
    void Test<T>() where T : new(); 
}", "Person")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            static void Fail()
            {
                var _ = typeof(Holder<{|#0:Person|}>);
            }
        }

class Holder<T> where T : new(){}", "Person")]
        public async Task Invalid(string test, string argument)
        {
            var expected = VerifyCS.Diagnostic(GenericRestrictionFeature.DiagnosticId).WithLocation(0).WithArguments(argument);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
        
        [Theory]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public static void Fail()
            {
                {|#0:TestGenericCall.Access(new Person
                {
                    Age = 20,
                })|};
            }
        }

class TestGenericCall{
    public static void Access<T>(T o) where T : new(){}
}
", "Person")]
        public async Task InvalidImplicit(string test, string argument)
        {
            var expected = VerifyCS.Diagnostic(GenericRestrictionFeature.DiagnosticId).WithLocation(0).WithArguments(argument);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
        
        [Theory]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public static void Fail()
            {
                var tt = Access<Person>.Test;
            }
        }

class Access<T>
{
    public static int Test = 20;
}")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public static void Fail()
            {
                Access<Person>();
            }

            private static void Access<T>(){}
        }
")]
        
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            public int Age { get; set; }

            public static void Fail()
            {
                var tt = Access<Person>.Test;
            }
        }

class Access<T>
{
    public static int Test = 20;
}")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            public int Age { get; set; }

            public static void Fail()
            {
                Access<Person>();
            }

            private static void Access<T>(){}
        }
")]
        
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            public int Age { get; set; }

            public static void Fail()
            {
                var tt = Access<Person>.Test;
            }
        }

class Access<T> where T : new() 
{
    public static int Test = 20;
}")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            public int Age { get; set; }

            public static void Fail()
            {
                Access<Person>();
            }

            private static void Access<T>() where T : new() {}
        }
")]
        [InlineData(@"using System;
using System.Collections.Generic;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            static void Fail()
            {
                var _ = typeof(Dictionary<Person, string>);
            }
        }")]
        public async Task Valid(string test)
        {
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}