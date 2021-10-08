using System.Threading.Tasks;
using Xunit;
using VerifyCS = Blowin.Required.Test.CSharpCodeFixVerifier<
    Blowin.Required.BlowinRequiredAnalyzer,
    Blowin.Required.BlowinRequiredCodeFixProvider>;

namespace Blowin.Required.Test
{
    public class RequiredWithGenericTest
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
        public async Task Invalid(string test, string argument)
        {
            var expected = VerifyCS.Diagnostic(BlowinRequiredAnalyzer.DiagnosticObjectCreationRuleId).WithLocation(0).WithArguments(argument);
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
        public async Task Valid(string test)
        {
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}