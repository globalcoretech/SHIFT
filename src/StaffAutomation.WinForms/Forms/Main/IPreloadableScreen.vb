Option Strict On
Option Explicit On

Imports System.Threading.Tasks

Namespace Forms.Main
    ''' <summary>
    ''' Defines a screen capable of preloading its data and preparing layout asynchronously
    ''' before being atomically swapped into the primary workspace container.
    ''' Supports navigation version tokens to discard stale navigation callbacks.
    ''' </summary>
    Public Interface IPreloadableScreen
        ''' <summary>
        ''' Preloads required data, populates DataGridView sources, and prepares layout.
        ''' </summary>
        ''' <param name="navigationToken">Unique sequence token for the navigation request.</param>
        Function PreloadDataAsync(navigationToken As Long) As Task
    End Interface
End Namespace
