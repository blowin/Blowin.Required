using System.Threading.Tasks;
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

            private Person() => Age = 10;

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
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            public Person(Person2 p)
            {
                p.Age = 200;
            }

            public static Person Create(Person2 p)
            {
                return {|#0:new Person(p)
                {
                }|};
            }
        }

class Person2
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }
        }", "Age")]
        [InlineData(@"using System;

class RequiredAttribute : Attribute { }

class Person
        {
            public string Name { get; set; }
            
            [Required]
            public int Age { get; set; }

            private Person()
            {
                return;
                Age = 200;
            }

            public static Person Create()
            {
                return {|#0:new Person()
                {
                    Name = """"
                }|};
            }
        }", "Age")]
        public async Task Invalid(string test, string argument)
        {
            var expected = VerifyCS.Diagnostic(RequiredInitializerFeature.DiagnosticId).WithLocation(0).WithArguments(argument);
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

            private Person()
            {
                if(string.IsNullOrEmpty(Name)){
                    Age = 200;
                }
                Name = ""ttt"";
            {|#0:}|}

            public static Person Create()
            {
                return {|#1:new Person()
                {
                    Name = """"
                }|};
            }
        }", "Age")]
        public async Task InvalidWithCtorFail(string test, string property)
        {
            var expectedCtor = VerifyCS.Diagnostic(CtorAllRequiredFieldInitializedFeature.DiagnosticId).WithLocation(0).WithArguments(property);
            var expectedInitializer = VerifyCS.Diagnostic(RequiredInitializerFeature.DiagnosticId).WithLocation(1).WithArguments(property);
            await VerifyCS.VerifyAnalyzerAsync(test, expectedCtor, expectedInitializer);
        }
    }
}
