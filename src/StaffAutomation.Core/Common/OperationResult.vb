Imports System

Namespace Common
    ''' <summary>
    ''' Generic Operation Result wrapper model encapsulating execution outcome and data payload.
    ''' </summary>
    ''' <typeparam name="T">Type of result payload</typeparam>
    Public Class OperationResult(Of T)
        Public Property IsSuccess As Boolean
        Public Property Data As T
        Public Property Message As String = String.Empty
        Public Property Exception As Exception

        Public Shared Function Success(dataPayload As T, Optional successMessage As String = "") As OperationResult(Of T)
            Return New OperationResult(Of T)() With {
                .IsSuccess = True,
                .Data = dataPayload,
                .Message = successMessage
            }
        End Function

        Public Shared Function Failure(errorMessage As String, Optional ex As Exception = Nothing) As OperationResult(Of T)
            Return New OperationResult(Of T)() With {
                .IsSuccess = False,
                .Message = errorMessage,
                .Exception = ex
            }
        End Function
    End Class
End Namespace
