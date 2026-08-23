Imports System
Imports System.Security.Cryptography
Imports System.Text
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Exceptions

Namespace Security
    ''' <summary>
    ''' Implementation of salted PBKDF2/SHA-256 password hashing and verification.
    ''' Cryptographically secure implementation resistant to timing attacks.
    ''' </summary>
    Public Class PasswordHasher
        Implements IPasswordHasher, IPasswordVerifier

        Private Const SaltByteSize As Integer = 24
        Private Const HashByteSize As Integer = 24
        Private Const Pbkdf2Iterations As Integer = 10000

        Public Function HashPassword(password As String, ByRef outSalt As String) As String Implements IPasswordHasher.HashPassword
            If String.IsNullOrEmpty(password) Then
                Throw New ValidationException("Password cannot be null or empty for hashing.", "password")
            End If

            Dim saltBytes(SaltByteSize - 1) As Byte
            Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
                rng.GetBytes(saltBytes)
            End Using

            outSalt = Convert.ToBase64String(saltBytes)
            Return ComputeHash(password, saltBytes)
        End Function

        Public Function VerifyPassword(password As String, storedHash As String, storedSalt As String) As Boolean Implements IPasswordHasher.VerifyPassword
            If String.IsNullOrEmpty(password) OrElse String.IsNullOrEmpty(storedHash) OrElse String.IsNullOrEmpty(storedSalt) Then
                Return False
            End If

            Try
                Dim saltBytes As Byte() = Convert.FromBase64String(storedSalt)
                Dim computedHash As String = ComputeHash(password, saltBytes)
                Return SlowEquals(Encoding.UTF8.GetBytes(computedHash), Encoding.UTF8.GetBytes(storedHash))
            Catch
                Return False
            End Try
        End Function

        Public Function IsPasswordValid(password As String, ByRef outErrorMessage As String) As Boolean Implements IPasswordVerifier.IsPasswordValid
            If String.IsNullOrEmpty(password) OrElse password.Length < AppConstants.MinimumPasswordLength Then
                outErrorMessage = $"Password must be at least {AppConstants.MinimumPasswordLength} characters long."
                Return False
            End If

            outErrorMessage = String.Empty
            Return True
        End Function

        ''' <summary>
        ''' Generates a cryptographically secure random temporary password.
        ''' </summary>
        Public Function GenerateTemporaryPassword(Optional length As Integer = 12) As String Implements IPasswordHasher.GenerateTemporaryPassword
            If length < 8 Then length = 12
            Const validChars As String = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*"
            Dim bytes(length - 1) As Byte
            Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
                rng.GetBytes(bytes)
            End Using
            Dim result(length - 1) As Char
            For i As Integer = 0 To length - 1
                result(i) = validChars(bytes(i) Mod validChars.Length)
            Next
            Return New String(result)
        End Function

        Private Function ComputeHash(password As String, salt As Byte()) As String
            Using pbkdf2 As New Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256)
                Dim hashBytes As Byte() = pbkdf2.GetBytes(HashByteSize)
                Return Convert.ToBase64String(hashBytes)
            End Using
        End Function

        ''' <summary>
        ''' Constant-time comparison to prevent timing attacks.
        ''' </summary>
        Private Function SlowEquals(a As Byte(), b As Byte()) As Boolean
            Dim diff As UInteger = CUInt(a.Length) Xor CUInt(b.Length)
            Dim i As Integer = 0
            While i < a.Length AndAlso i < b.Length
                diff = diff Or CUInt(a(i) Xor b(i))
                i += 1
            End While
            Return diff = 0
        End Function
    End Class
End Namespace
