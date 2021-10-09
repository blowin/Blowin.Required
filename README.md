# Blowin.Required

| Source      | Link |
| ----------- | ----------- |
| VSIX        | [![VSIX](https://img.shields.io/visual-studio-marketplace/v/Blowin.requiredproperty)](https://marketplace.visualstudio.com/items?itemName=Blowin.requiredproperty)       |
| Nuget       | [![NUGET package](https://img.shields.io/nuget/v/Blowin.Required.svg)](https://www.nuget.org/packages/Blowin.Required/)        |

Implementation of proposal 'Required Properties'
https://github.com/dotnet/csharplang/issues/3630

Add required attribute to property and enjoy :)

```c#
public class Person
{
    public string Name { get; set; }
    
    [Required]
    public int Age { get; set; }
}
```

You can use DataAnnotation, or any attribute with name Required

Support diagnostics:
1. Required property must be initialized (initializer)
2. Type can't be used as generic parameter with new() restriction
3. If constructor initialization of required property, it should be initialized in any execution path

