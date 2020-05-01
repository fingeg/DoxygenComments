# Doxygen Comments
Automatic doxygen comments creation for functions, headers and all other members.
The comments formats can be completely [customized](#Customizing).

## Installation
Visual Studio Marketplace: [DoxygenComments](https://marketplace.visualstudio.com/items?itemName=FinnGegenmantel.doxygenComments).

Or in Visual Studio -> Extensions -> Doxygen Comments

# Formats
The following formats are the default formats, but the can be [customized](#Customizing).
## Header
```cpp
/*****************************************************************//**
 * \file   sampleClass.h
 * \brief 
 *
 * \author Finn 
 * \date   April 2020
***********************************************************************/
```

## Function
```cpp
/**
 * Set the sample class name.
 * 
 * \param name The name for the sample class
 * \return a sample return value
 */
```

# Getting Started
Type '/**' for single line comments. After the comment is created, press enter or tab to generate the doxygen comment.

To skip the single line format, use '/*!'.

Header can be created by writing '/**' in the first file line, and all other, directly before the wished member.

## Different comments
- Function: Automatic paramets and return values
- Header: Automatic file, date and author

# Customizing
The plugin settings are under `Tools -> Options -> Doxygen`.    

In the pages `Header`, `Function` and `Default` are the customizable formats.   
You can use all the listed variables. The `\param` will be multiplied to the number of parameters of the function 
and the `\return` attribute will only be shown without a void return type.

You can replace attributes like `\param` or `\return` with everything else (for example: `@param`), 
if you use the `$PARAMS` and `$RETURN` variables in the same line.

If you want you can add custom attributes like `\license` to always generate a licence in a header comment.

# Addtitional Info
If you want to contirbute to this plugin and add a nice feature or fix a bug, 
feel free to make a pull request in my [repository](https://github.com/fingeg/DoxygenComments).

## Todo
If I have time, or if you want to help, the following features would be nice to implement:
- IntelliSense Quick Info for doxygen style
- Share the customized formats in the git repo

---
This is a fork of [dragospop/CppDoxyComplete](https://github.com/dragospop/CppDoxyComplete), but is for VS2019 
and customizable.