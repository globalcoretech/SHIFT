Namespace DTOs
    Public Class UpdateCheckResult
        Public Property IsSuccess As Boolean
        Public Property IsUpdateAvailable As Boolean
        Public Property Manifest As UpdateManifestDto
        Public Property ErrorMessage As String
        Public Property DownloadedFilePath As String
    End Class
End Namespace
