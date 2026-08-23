Namespace Security
    ''' <summary>
    ''' Contract for validating password strength and verification.
    ''' </summary>
    Public Interface IPasswordVerifier
        Function IsPasswordValid(password As String, ByRef outErrorMessage As String) As Boolean
    End Interface
End Namespace
