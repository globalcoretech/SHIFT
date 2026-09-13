Imports System

Namespace DTOs
    Public Class UpdateManifestDto
        Public Property LatestVersion As String
        Public Property ReleaseDate As DateTime
        Public Property DownloadUrl As String
        Public Property ReleaseNotes As String
        Public Property SHA256 As String
        Public Property RequiredDatabaseVersion As Integer
    End Class
End Namespace
