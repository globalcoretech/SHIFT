Imports System
Imports StaffAutomation.Core.Security

Module Program
    Sub Main()
        Console.WriteLine("Main started")
        Console.WriteLine("================================================================================")
        Console.WriteLine("SEED DATA HASH GENERATOR OUTPUT (Powered by PasswordHasher.vb)")
        Console.WriteLine("================================================================================")

        Try
            Console.WriteLine("STEP 1")
            Dim hasher As New PasswordHasher()

            Console.WriteLine("STEP 2")
            Dim saltAdmin As String = ""

            Console.WriteLine("STEP 3")
            Dim hashAdmin As String = hasher.HashPassword("admin123", saltAdmin)

            Console.WriteLine("STEP 4")
            Console.WriteLine($"Username         : admin")
            Console.WriteLine($"Plaintext        : admin123")
            Console.WriteLine($"Generated Salt   : " & saltAdmin)
            Console.WriteLine($"Generated Hash   : " & hashAdmin)

            ' Account 2: owner / owner123
            Dim saltOwner As String = ""
            Dim hashOwner As String = hasher.HashPassword("owner123", saltOwner)

            ' Account 3: employee / employee123
            Dim saltEmployee As String = ""
            Dim hashEmployee As String = hasher.HashPassword("employee123", saltEmployee)

            Console.WriteLine("--------------------------------------------------------------------------------")
            Console.WriteLine($"Username         : owner")
            Console.WriteLine($"Plaintext        : owner123")
            Console.WriteLine($"Generated Salt   : " & saltOwner)
            Console.WriteLine($"Generated Hash   : " & hashOwner)
            Console.WriteLine("--------------------------------------------------------------------------------")
            Console.WriteLine($"Username         : employee")
            Console.WriteLine($"Plaintext        : employee123")
            Console.WriteLine($"Generated Salt   : " & saltEmployee)
            Console.WriteLine($"Generated Hash   : " & hashEmployee)
            Console.WriteLine("================================================================================")
        Catch ex As Exception
            Console.WriteLine("EXCEPTIONAL ERROR DURING HASH GENERATION:")
            Console.WriteLine(ex.ToString())
        End Try

        Console.Out.Flush()
    End Sub
End Module
