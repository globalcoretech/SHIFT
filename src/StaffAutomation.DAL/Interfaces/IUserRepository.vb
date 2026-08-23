Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports StaffAutomation.Core.Entities

Namespace Interfaces
    ''' <summary>
    ''' Data Access Repository contract for User entity persistence.
    ''' </summary>
    Public Interface IUserRepository
        Function GetByIdAsync(userId As Integer) As Task(Of UserEntity)
        Function GetByUsernameAsync(username As String) As Task(Of UserEntity)
        Function GetAllAsync() As Task(Of List(Of UserEntity))
        Function AddAsync(user As UserEntity) As Task(Of Integer)
        Function UpdateAsync(user As UserEntity) As Task(Of Boolean)
        Function SoftDeleteAsync(userId As Integer, modifiedBy As Integer) As Task(Of Boolean)
    End Interface
End Namespace
