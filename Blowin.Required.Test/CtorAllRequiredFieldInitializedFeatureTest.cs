using System.Threading.Tasks;
using Blowin.Required.Features;
using Xunit;
using VerifyCS = Blowin.Required.Test.CSharpCodeFixVerifier<
    Blowin.Required.BlowinRequiredAnalyzer,
    Blowin.Required.BlowinRequiredCodeFixProvider>;

namespace Blowin.Required.Test
{
    public class CtorAllRequiredFieldInitializedFeatureTest
    {
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
                if(Age == 0)
                {
                    Age = 20;
                }
                else
                {
                    Name = ""ttt"";
                }
            {|#0:}|}
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
                if(Age == 0)
                {
                    Age = 20;
                }
                else
                {
                }
            {|#0:}|}
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
                if(Age == 0)
                {
                    Age = 20;
                }
            {|#0:}|}
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
                if(Age == 0)
                {
                    Age = 20;
                    
                    if(string.IsNullOrEmpty(Name)){
          
                    }
                    else
                    {
                        Age = 400;
                    }
                }
                else
                {
                    Age = 500;
                }
            {|#0:}|}
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
                if(Age == 0)
                {
                    Age = 20;
                    
                    if(string.IsNullOrEmpty(Name)){
                        Age = 400;
                    }
                    else
                    {
                        
                    }
                }
                else
                {
                    Age = 500;
                }
            {|#0:}|}
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
                if(Age == 0)
                {
                    Age = 20;
                    
                    if(string.IsNullOrEmpty(Name)){
                        Age = 400;
                    }
                }
                else
                {
                    Age = 500;
                }
            {|#0:}|}
        }", "Age")]
        public async Task Invalid(string test, string argument)
        {
            var expected = VerifyCS.Diagnostic(CtorAllRequiredFieldInitializedFeature.DiagnosticId)
                .WithLocation(0)
                .WithArguments(argument);
            
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
                if(Age == 0)
                {
                    Age = 20;
                }
                else
                {
                    Age = 22;
                }
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
                if(Age == 0)
                {
                    if(string.IsNullOrEmpty(Name)){
                        Age = 300;
                    }
                    else
                    {
                        Age = 400;
                    }
                    Age = 20;
                }
                else
                {
                    Age = 22;
                }
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
                if(Age == 0)
                {
                }
                else
                {
                }
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
                Age = 0;

                if(Age == 0)
                {
                }
                else
                {
                }
            }
        }")]
        public async Task Valid(string test)
        {
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}