# Doxygen Comments
Automatic doxygen comments creation for functions, headers and all other members.
The comments can be [customized](#Customizing) and you can [share ](#Shared-Formats) them to force the same style for all contirbuters with this plugin.

## Installation
Visual Studio Marketplace: [DoxygenComments](https://marketplace.visualstudio.com/items?itemName=FinnGegenmantel.doxygenComments).

Or in Visual Studio -> Extensions -> Doxygen Comments

# Format
The following formats are the default formats, but the can be completly [customized](#Customizing).
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
- Class: Automatic template parameter
- Header: Automatic file, date and author

# Customizing
The plugin settings are under `Tools -> Options -> Doxygen`.    

In the pages `Header` and `Functions` are the customizable formats.   
You can use all the listed variables. The `\param` will be multiplied to the number of parameters of the function 
and the `\return` attribute will only be shown without a void return type.

You can replace attributes like `\param` or `\return` with everything else, 
if you use the `$PARAMS` and `$RETURN` variables in the same line.

If you want you can add custom attributes like `\license` to always generate a licence in a header comment.

# Shared Formats
If you work in a team and wants to share the same format for every contributer in the project you can activate the 
`share formats` attribute in `Tools -> Options -> Doxygen -> General`. A `.doxygencomments` file will be created in the
top of the working directory and everyone who has this plugin installed will have the same format.

With shared formats you can force a consistently documentation in your whole project and use for example in each file the 
same licence attribute.

# Addtitional Info
If you want to contirbute to this plugin and add a nice feature or fix a bug, 
feel free to make a pull request in my [repository](https://github.com/fingeg/DoxygenComments).

## Todo
If I have time, or if you want to help, the following features would be nice to implement:
- IntelliSense Quick Info for doxygen style

---
This is a fork of [dragospop/CppDoxyComplete](https://github.com/dragospop/CppDoxyComplete), but is for VS2019, 
customizable and has the option to share the same style in a project.