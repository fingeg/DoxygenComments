# Doxygen Comments
Automatic doxygen comments creation for functions, headers and all other members.

## Installation
Visual Studio Marketplace: [DoxygenComments](https://marketplace.visualstudio.com/items?itemName=FinnGegenmantel.doxygenComments).

Or in Visual Studio -> Extensions -> Doxygen Comments

# Format
## Header
```cpp
/*****************************************************************//**
 * \file sampleClass.h
 * \brief 
 *
 * \author Finn 
 * \date April 2020
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

# Info
Based on [dragospop/CppDoxyComplete](https://github.com/dragospop/CppDoxyComplete).
