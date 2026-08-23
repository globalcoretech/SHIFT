Namespace Security
    ''' <summary>
    ''' Contract for salted cryptographic password hashing and verification.
    ''' </summary>
    Public Interface IPasswordHasher
        Function HashPassword(password As String, ByRef outSalt As String) As String
        Function VerifyPassword(password As String, storedHash As String, storedSalt As String) As Boolean
        Function GenerateTemporaryPassword(Optional length As Integer = 12) As String
    End Interface
End Namespace
