# Blowin.Required

![Build](https://github.com/blowin/Blowin.Required/actions/workflows/dotnet.yml/badge.svg)

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

### Success cases:


![initializer](https://github.com/blowin/Blowin.Required/blob/master/images/initializer_ok.jpg)
![initializer from constructor non required](https://github.com/blowin/Blowin.Required/blob/master/images/ctor_ok.PNG)
![constructor & initializer](https://github.com/blowin/Blowin.Required/blob/master/images/ctor_initializer_ok.PNG)
![initializer from constructor required](https://github.com/blowin/Blowin.Required/blob/master/images/ctor_required_initialization_ok.PNG)

### Fail cases:

![Initializer](https://github.com/blowin/Blowin.Required/blob/master/images/initializer.jpg)
![Ctor fail](https://github.com/blowin/Blowin.Required/blob/master/images/ctor_fail.jpg)
![Initialization required property from ctor](https://github.com/blowin/Blowin.Required/blob/master/images/ctor_required_initialization_fail.jpg)
![Generic class restriction](https://github.com/blowin/Blowin.Required/blob/master/images/generic_class_generic_restriction_fail.jpg)
![Generic method restriction](https://github.com/blowin/Blowin.Required/blob/master/images/generic_method_generic_restriction_fail.jpg)
![Generic method, restriction for implicit parameter](https://github.com/blowin/Blowin.Required/blob/master/images/generic_method_implicit_parameter_generic_restriction_fail.jpg)
