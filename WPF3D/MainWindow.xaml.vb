Imports System.Windows.Media.Media3D
Imports ObjImport.ObjectHandler.Object3DHandler

Class MainWindow
    Private Sub Grid_Loaded(sender As Object, e As RoutedEventArgs)
        'files need to be accessed like this.
        'this code was optimised for using multiple threads to read many objects at once. Object files are cached in memory, so producing the same object many times should get faster.
        Dim obj = CreateMeshFromObj("./Example.obj")

        'get the object's bounds
        Dim thebounds = obj.Bounds
        Dim offx = -(thebounds.Location.X + (thebounds.SizeX / 2))
        Dim offy = -(thebounds.Location.Y + (thebounds.SizeY / 2))
        Dim offz = -(thebounds.Location.Z + (thebounds.SizeZ / 2))

        'move the object to where it's defined to be
        obj.Transform = New TranslateTransform3D(offx, offy, offz)

        'place the object into a modelvisual3d, so that it can be displayed
        Dim visual3d = New ModelVisual3D With {
                                                    .Content = obj
                                              }
        'add the child
        viewPort3D.Children.Add(visual3d)
    End Sub
End Class
