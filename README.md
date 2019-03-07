# WPF3DObjSupport
Provides a basic (reasonably thread safe) interface to use in order to import 3D objects into a WPF 3D viewport at runtime.

It also supports a feature not in the obj specification, while maintaining file compatibility:
<code><pre>#aph</pre></code>
which provides a different way to set the transparancy of a set of colours in a \*.mtl file. For example:
<code><pre>
#aph 0.5
newmtl Green_0
Kd 0.129412 0.733333 0.298039
#aph 1
</pre></code>
will define a material called Green_0, with 0.5 opacity, then set the opacity back to 1 for the next material described. Because this starts with a #, it should not cause compatibility issues with any existing 3D modelling application.

## Usage
Simply import the built DLL (ObjImport/bin/ObjImport.dll), and use the function "CreateMeshFromObj(filename)", where filename is the name of the obj file you would like to import.

A sample of the use of this library may be found in <code>WPF3D/MainWindow.xaml.vb</pre>.

Textures mapped to the object will likely perform poorly, due to implementation, but otherwise, any object should load reasonably quickly, and be optimised for the ViewPort3D.
