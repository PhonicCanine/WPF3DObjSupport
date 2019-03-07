Imports System.Windows.Media
Imports System.Windows.Media.Media3D
Imports System.Windows

Namespace ObjectHandler

    'Joseph Fergusson, 2018

    ''' <summary>
    ''' Manages 3D objects. Most importantly, creating them from *.obj files.
    ''' </summary>
    Public Class Object3DHandler

        ''' <summary>
        ''' Creates a Model3DGroup from a given file as a .obj
        ''' </summary>
        ''' <param name="objname">Name of the object file.</param>
        ''' <returns>A new Model3DGroup based on the file.</returns>
        Public Shared Function CreateMeshFromObj(objname As String) As Model3DGroup

            'First off, here's the specification for a .obj file: https://en.wikipedia.org/wiki/Wavefront_.obj_file
            'This implements all features, except lines and parameter verticies, as Microsoft 3D Builder doesn't use these things (At least not in my testing).


            'A lot of variables to store data on the object as we go through and create it.
            Dim theobj As New MeshGeometry3D()
            Dim directory = objname.Replace(objname.Split("/").Last(), "")

            Dim theString = SharedFileData.GetDataFromFile(objname)
            Dim splitString = theString

            Dim vertecies = New List(Of Point3D)
            Dim faces = New Dictionary(Of String, List(Of List(Of Integer)))
            Dim currentFaces = New List(Of Integer)
            Dim textureFaces = New Dictionary(Of Integer, List(Of Integer))

            Dim faceTextureDictionary = New Dictionary(Of Point3D, Point3D)

            Dim currentMaterial As String = ""
            Dim materialLibList = New List(Of String)
            Dim texturePoints = New List(Of Point)

            'Loop through all the lines in the file
            For i = 0 To splitString.Count - 1

                If splitString(i).StartsWith("#") Then

                    'pass, the line is a comment

                    'If the line is refering to a vertex
                ElseIf splitString(i).Contains("v") And Not splitString(i).Contains("n") And Not splitString(i).Contains("t") Then
                    'split the line up
                    Dim splitTwo = splitString(i).Split(" ")
                    Dim thepoint = New Point3D()
                    Dim correctCount = 0

                    'loop through the X, Y and Z coordinates of the vertex, setting the points in a new point3d
                    'This implementation actually reads the file as X, Z, Y.
                    For x = 0 To splitTwo.Length - 1
                        Dim op As Decimal
                        If Double.TryParse(splitTwo(x), op) Then
                            If correctCount = 0 Then
                                thepoint.X = op
                                correctCount += 1
                            ElseIf correctCount = 1 Then
                                thepoint.Z = op
                                correctCount += 1
                            ElseIf correctCount = 2 Then
                                thepoint.Y = op
                                correctCount += 1
                            End If
                        End If
                    Next
                    'add this vertex to the list of verticies
                    vertecies.Add(thepoint)

                    'the line is a material library
                ElseIf splitString(i).Contains("mtllib") Then
                    'Add the material library name to the list of material libraries, to be accessed later.
                    materialLibList.Add(splitString(i).Replace("mtllib ", "").TrimStart(" "))

                    'The line is defining which material to use for the following faces
                ElseIf splitString(i).Contains("usemtl") Then

                    'If faces contains the current material, otherwise, add the current material to the list of current materials
                    If faces.ContainsKey(currentMaterial) Then
                        faces(currentMaterial).Add(currentFaces)
                    ElseIf currentMaterial <> "" Then
                        faces(currentMaterial) = New List(Of List(Of Integer)) From {
                        currentFaces
                    }
                    End If

                    'Start on a new material to display.
                    currentFaces = New List(Of Integer)
                    currentMaterial = splitString(i).Replace("usemtl ", "")

                    'the line is trying to make a face
                ElseIf splitString(i).Contains("f") Then
                    Dim splitTwo = splitString(i).Split(" ")
                    Dim thepoint = New Point3D()
                    Dim correctCount = 0

                    Dim facepoint = New Point3D
                    Dim texturePoint = New Point3D

                    'faces can actually be in more than one form
                    'point, or point/texture, and finally point/texture/normal, but here that isn't implemented.
                    'Loop through point a, b and c
                    For x = 0 To splitTwo.Count - 1
                        Dim PointNumber As Integer

                        Dim firstPart As String
                        Dim textureInfo As String = ""

                        'Split the point reference if need be
                        If splitTwo(x).Contains("/") Then
                            Dim twoParts = splitTwo(x).Split("/")
                            firstPart = twoParts(0)
                            textureInfo = twoParts(1)
                        Else
                            firstPart = splitTwo(x)
                        End If

                        'If the point is a legitimate number, add it to the face
                        If Integer.TryParse(firstPart, PointNumber) Then
                            If correctCount = 0 Then

                                currentFaces.Add(PointNumber - 1)
                                correctCount += 1
                                facepoint.X = PointNumber - 1

                            ElseIf correctCount = 1 Then

                                currentFaces.Add(PointNumber - 1)
                                correctCount += 1
                                facepoint.Y = PointNumber - 1

                            ElseIf correctCount = 2 Then

                                correctCount += 1
                                currentFaces.Add(PointNumber - 1)
                                facepoint.Z = PointNumber - 1

                            End If
                        End If

                        'Loop through the texture points, and allocate texture faces
                        Dim TextureNumber As Integer
                        If Integer.TryParse(textureInfo, TextureNumber) Then


                            If textureFaces.ContainsKey(PointNumber - 1) = False Then
                                textureFaces(PointNumber - 1) = New List(Of Integer)
                            End If

                            If correctCount = 1 Then
                                texturePoint.X = TextureNumber - 1
                            ElseIf correctCount = 2 Then
                                texturePoint.Y = TextureNumber - 1
                            ElseIf correctCount = 3 Then
                                texturePoint.Z = TextureNumber - 1
                            End If

                            textureFaces(PointNumber - 1).Add(TextureNumber - 1)

                        End If

                    Next

                    'Adds the texture point to the texture dictionary
                    faceTextureDictionary(facepoint) = texturePoint

                    'this is a dodgy solution. It means that textures can properly be displayed, but ruins performance. Hence here it is commented out.

                    'If faces.ContainsKey(currentMaterial) Then
                    '    faces(currentMaterial).Add(currentFaces)
                    'ElseIf currentMaterial <> "" Then
                    '    faces(currentMaterial) = New List(Of List(Of Integer))
                    '    faces(currentMaterial).Add(currentFaces)
                    'End If
                    'currentFaces = New List(Of Integer)
                    'currentMaterial = currentMaterial

                ElseIf splitString(i).Contains("vt") Then

                    'Chooses texture points that get referenced by points on the object.

                    Dim splitTwo = splitString(i).Replace("vt ", "").Split(" ")
                    Dim thepoint = New Point()
                    Dim correctCount = 0
                    For x = 0 To splitTwo.Length - 1
                        Dim op As Decimal
                        If Double.TryParse(splitTwo(x), op) Then
                            If correctCount = 0 Then
                                thepoint.X = op
                                correctCount += 1
                            ElseIf correctCount = 1 Then
                                thepoint.Y = op
                                correctCount += 1
                            End If
                        End If
                    Next
                    texturePoints.Add(thepoint)
                End If
            Next

            'Add the faces that were remaining to the list of faces for the specific material.
            If faces.ContainsKey(currentMaterial) Then
                faces(currentMaterial).Add(currentFaces)
            ElseIf currentMaterial <> "" Then
                faces(currentMaterial) = New List(Of List(Of Integer)) From {
                currentFaces
            }
            End If

            'holds all the materials that will be used in the object
            Dim materialDictionary = New Dictionary(Of String, Material)

            Dim returnObject = New Model3DGroup()

            'Loops through each material library, getting data and writing it into a material
            For Each materialLibrary In materialLibList
                Dim materialData = SharedFileData.GetDataFromFile(directory & "/" & materialLibrary)
                Dim theCurrentMaterial = New MaterialGroup()
                Dim materialName = ""
                Dim specularPower = 0D

                'Not part of the specification. Allows clear things.
                Dim alpha As Byte = 255
                For i = 0 To materialData.Count - 1
                    Dim line = materialData(i)

                    'Custom property. Starts with a # because that means it is commented out in any real obj reader.
                    If line.StartsWith("#aph") Then
                        'this defines the alpha to use for colors. Custom addition to the mtl format.
                        Dim restOfLine = line.Replace("#aph ", "")
                        Dim colors = restOfLine.Split(" ")
                        Dim aph = Double.Parse(colors(0))
                        Dim aphbyte As Byte = aph * 255
                        alpha = aphbyte
                    ElseIf line.StartsWith("#") Then
                        'pass, actual comment

                        'Defines a new material
                    ElseIf line.Contains("newmtl") Then

                        'saves the current material
                        If theCurrentMaterial.Children.Count > 0 Then
                            materialDictionary(materialName) = theCurrentMaterial
                        End If

                        'resets things (specularPower, and materialName) and creates a new material group
                        theCurrentMaterial = New MaterialGroup()
                        specularPower = 0D
                        materialName = line.Replace("newmtl ", "")

                        'Defines an ambient material (Gives off light). Reads the values of R,G,B from the file, and makes them into a New Emissive material.
                    ElseIf line.Contains("Ka") Then
                        Dim restOfLine = line.Replace("Ka ", "")
                        Dim colors = restOfLine.Split(" ")
                        Dim r = Double.Parse(colors(0))
                        Dim rbyte As Byte = r * 255
                        Dim g = Double.Parse(colors(1))
                        Dim gbyte As Byte = g * 255
                        Dim b = Double.Parse(colors(2))
                        Dim bbyte As Byte = b * 255
                        Dim newMaterial = New EmissiveMaterial(New SolidColorBrush(Color.FromArgb(alpha, rbyte, gbyte, bbyte)))
                        theCurrentMaterial.Children.Add(newMaterial)

                        'Defines a new diffuse material (Gets hit by light). Once again, reads values from the file and makes them into a new DiffuseMaterial.
                    ElseIf line.Contains("Kd") And line.Contains("map_Kd") = False Then
                        Dim restOfLine = line.Replace("Kd ", "")
                        Dim colors = restOfLine.Split(" ")
                        Dim r = Double.Parse(colors(0))
                        Dim rbyte As Byte = r * 255
                        Dim g = Double.Parse(colors(1))
                        Dim gbyte As Byte = g * 255
                        Dim b = Double.Parse(colors(2))
                        Dim bbyte As Byte = b * 255
                        Dim newMaterial = New DiffuseMaterial(New SolidColorBrush(Color.FromArgb(alpha, rbyte, gbyte, bbyte)))
                        theCurrentMaterial.Children.Add(newMaterial)

                        'Defines a new specular material. Very similar to the above two, but this time it makes a SpecularMaterial, and uses specularPower.
                    ElseIf line.Contains("Ks") Then
                        Dim restOfLine = line.Replace("Ks ", "")
                        Dim colors = restOfLine.Split(" ")
                        Dim r = Double.Parse(colors(0))
                        Dim rbyte As Byte = r * 255
                        Dim g = Double.Parse(colors(1))
                        Dim gbyte As Byte = g * 255
                        Dim b = Double.Parse(colors(2))
                        Dim bbyte As Byte = b * 255
                        Dim newMaterial = New SpecularMaterial(New SolidColorBrush(Color.FromArgb(alpha, rbyte, gbyte, bbyte)), specularPower)
                        theCurrentMaterial.Children.Add(newMaterial)

                        'Sets specular power for the material
                    ElseIf line.Contains("Ns") Then
                        Dim restOfLine = line.Replace("Ns ", "")
                        Dim colors = restOfLine.Split(" ")
                        specularPower = Double.Parse(colors(0))

                        'Maps an image as a diffuse material.
                    ElseIf line.Contains("map_Kd ") Then
                        Dim restOfLine = line.Replace("map_Kd ", "")
                        Dim image = New Imaging.BitmapImage(New Uri(restOfLine, UriKind.Relative))
                        Dim height = image.Height
                        Dim ImageBrush = New ImageBrush(image)
                        Dim newMaterial = New DiffuseMaterial(ImageBrush)
                        theCurrentMaterial.Children.Add(newMaterial)
                    End If
                Next

                'Add the last material to the dictionary
                If theCurrentMaterial.Children.Count > 0 Then
                    materialDictionary(materialName) = theCurrentMaterial
                End If
            Next

            Dim encounterCounter = New Dictionary(Of Integer, Integer)
            'Can be heavily optimised for runtime performance or initial load performance



            'Loops through the faces finally putting them together
            For Each keyval In faces
                Dim materialName = keyval.Key
                Dim materialUseList = keyval.Value
                For Each materialUse In materialUseList
                    Dim alreadyAdded = New Dictionary(Of Integer, Point3D)
                    Dim object3d = New MeshGeometry3D()


                    'What actually needs to happen here:
                    'Need to go through the list of face parts (materialUse), and map each number to a new vert

                    'maps old indexes to new ones assigned to broken down parts of object
                    Dim mappingDict = New Dictionary(Of Integer, Integer)

                    Dim trueCount = 0
                    For i = 0 To materialUse.Count - 1
                        'If the material has not been added already, add the verticies that use it into the object, the added list, and set the mapping dictionaries value for the material to be the material number that it is
                        If alreadyAdded.ContainsKey(materialUse(i)) = False Then
                            object3d.Positions.Add(vertecies(materialUse(i)))
                            alreadyAdded(materialUse(i)) = vertecies(materialUse(i))
                            mappingDict(materialUse(i)) = trueCount
                            trueCount += 1
                        End If
                    Next


                    'The texture points need to match the original triangle indicies.

                    'Goes through all the vertexes used by a specific material, and forms an object from the face they're a part of.
                    'This was a really big issue. The loop was going the wrong way (forwards) so the objects had the right geometry but where always inside out. Didn't notice until I tried to render a Go board. 27/04/18
                    For i = materialUse.Count - 3 To 0 Step -3
                        Dim x = materialUse(i + 2)
                        Dim y = materialUse(i + 1)
                        Dim z = materialUse(i)
                        object3d.TriangleIndices.Add(mappingDict(x))
                        object3d.TriangleIndices.Add(mappingDict(y))
                        object3d.TriangleIndices.Add(mappingDict(z))
                        Dim faceDetails = faceTextureDictionary(New Point3D(z, y, x))


                        'loops through the points implicated in the current texture.
                        'FIRST TRIANGLE X,Y,Z
                        'SECOND TRIANGLE Z,Y,X
                        If faceDetails.X <> Nothing Then

                            'Switching the order of the triangles seemed to make the textures display a little better, but this still doesn't work great.

                            If i = 0 Then
                                object3d.TextureCoordinates.Add(texturePoints(faceDetails.X))
                                object3d.TextureCoordinates.Add(texturePoints(faceDetails.Y))
                                object3d.TextureCoordinates.Add(texturePoints(faceDetails.Z))
                            Else

                                Dim coordList = New List(Of Point) From {
                                texturePoints(faceDetails.X),
                                texturePoints(faceDetails.Y),
                                texturePoints(faceDetails.Z)
                            }
                                For z = 0 To 2
                                    If (object3d.TextureCoordinates.Contains(coordList(z))) Then
                                    Else
                                        object3d.TextureCoordinates.Add(coordList(z))
                                    End If
                                Next

                            End If


                        End If

                    Next

                    '0 1 2
                    '1,0 0,1 0,0

                    '0 1 2
                    '0,1 1,0 1,1

                    'Finally adds the face to a 3D model, then adds the 3D model to the group of models that will be returned
                    Dim model3D = New GeometryModel3D(object3d, materialDictionary(materialName))
                    returnObject.Children.Add(model3D)
                Next
            Next

            'Returns the model.
            Return returnObject
        End Function

    End Class

    Class SharedFileData
        'Stores the file content of every single file accessed by the safe GetDataFromFile function.
        Shared files As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))

        'All the files that have been accessed by the safe GetDataFromFile function.
        Shared filesAccessed As Dictionary(Of String, Boolean) = New Dictionary(Of String, Boolean)

        'All the file numbers currently in use by the safe GetDataFromFile function.
        Private Shared currentFileNumbers As List(Of Integer) = New List(Of Integer)

        ''' <summary>
        ''' Gets data from a file in a thread-safe way. This means it only reads an individual file once, then stores the file in memory for the rest of the application's lifetime. Threadsafe, so accessing from any thread, at any time, is okay. If you would not like this, simply replace the function with a more simple version with the same signature.
        ''' </summary>
        ''' <param name="fileName">The name of the file to be accessed.</param>
        ''' <returns>A list containing all of the file data.</returns>
        Public Shared Function GetDataFromFile(fileName As String) As List(Of String)

            Dim realName = ""

            'Have to strip everything that comes before the last ../ otherwise, it can have threading issues. Also stops duplicate files from appearing in the files dictionary.
            If fileName.Contains("../") Then
                realName = fileName.Substring(fileName.LastIndexOf("../") + 3)
            Else
                realName = fileName
            End If

            'replaces any double backslashes, before setting the filename to be the one relative to the application's launch point.
            realName = realName.Replace("//", "/")
            fileName = realName

            If filesAccessed.ContainsKey(fileName) Then
                While files.ContainsKey(fileName) = False
                    'wait until the other thread finishes reading the file. The fileAccessed dictionary is set before the file is read, so this is needed.
                End While

                'return a copy of the file as stored in the file dictionary
                Return files(fileName).Select(Function(innerString) innerString).ToList
            End If

            'sets the current file to have been accessed
            filesAccessed(fileName) = True

            'Checks which file numbers are in use and adds a new one to the list
            If currentFileNumbers.Count = 0 Then
                currentFileNumbers.Add(1)
            Else
                currentFileNumbers.Add(currentFileNumbers.Last + 1)
            End If

            'claims the last filenumber on the list
            Dim fileNumber = currentFileNumbers.Last

            Try
                'opens the file for input
                FileSystem.FileOpen(fileNumber, fileName, Mode:=OpenMode.Input)

                'makes a new list to store the fileData in
                Dim fileData = New List(Of String)

                'Loops while there's still data in the file
                While Not EOF(fileNumber)

                    'Gets the next line
                    Dim theString = FileSystem.LineInput(fileNumber)

                    'only add to the script array if the string is not a comment, and the line contains stuff
                    While (theString = "" OrElse theString.Replace(" ", "") = "") AndAlso EOF(fileNumber) = False
                        theString = FileSystem.LineInput(fileNumber)
                    End While

                    'adds the line to the list of file data
                    fileData.Add(theString)

                End While

                'closes the file
                FileSystem.FileClose(fileNumber)

                'removes the current filenumber from the list
                currentFileNumbers.Remove(fileNumber)

                'sets the file as stored in memory to the file data
                files(fileName) = fileData

                'returns a copy of the file data (if it returned the filedata, it would be by reference and that causes issues, since each file is stored in memory, not in the actual file)
                Return fileData.Select(Function(innerString) innerString).ToList

                'Catches if somehow two filenumbers that are the same are added to the list of file numbers (in parallel), or if the next instance of the same file is opened before the fileAccessed is set for that file.
            Catch ex As System.IO.FileNotFoundException

                Throw New System.IO.FileNotFoundException

            Catch ex As System.IO.IOException

                'Calls this function again, in the hopes that the file will now be in fileAccessed, or it can claim a new fileNumber
                Return GetDataFromFile(fileName)

            End Try


        End Function
    End Class

End Namespace