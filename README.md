# VS-CtxPackage
This is a plugin repository for Visual Studio 2012, 2013, 2015, which we use it to generate file by T4 template. 

Why we need this
T4 template is a very powerful functionality of Visual Studio, but we need to add the same T4 template file for each file if we want to use T4 template. This is not very convenient. 

This plugin provides a basic infrastructure to map the T4 template with extension, so that we are able to use the same template for different files, and we don't need to add the template into Visual Studio project.

How to build and install:

1.  Download the source code.
2.  Build CtxPackageAll.CodeGenerators project
3.  Build CtxPackageAll.Setup project, then it will generate an exe file.
4.  Run the exe file to install the plugin.

How to configure T4 templates:

1. Go to your plugin installation folder.
2. Open file: Settings.config
3. Add template mapping if you want.
  <Templates>
    <add key=".cus.res" value="CustomResourceTemplate.tt"/>
    <add key=".res" value="ResourceTemplate.tt"/>
    <add key=".jsres" value="JSResourceTemplate.tt"/>
    <add key=".ioc" value="IoCTemplate.tt"/>
    <add key=".less" value="DotLessTemplate.tt"/>
  </Templates>
4. Add file mapping, key is the original file extension, value is the generated file extension.
  <Extensions>
    <add key=".less" value=".css"/>
    <add key=".res" value=".cs"/>
    <add key=".jsres" value=".js"/>
  </Extensions>
5. After everything setup, restart Visual Studio.
6. Open a project, choose one file, change the properties of this file as below:
   Custom Tool -> T4CommonTextTemplatingFileGenerator
7. Then, if you make any change about the file, it will automatically update the generated file based on the T4 template.
