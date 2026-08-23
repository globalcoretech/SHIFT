Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-High Precision GDI+ Anti-Aliased Vector Icon Renderer.
    ''' Produces crisp, resolution-independent corporate vector icons without unicode emojis or external image files.
    ''' </summary>
    Public Class VectorIconHelper
        ''' <summary>
        ''' Renders a crisp vector Magnifying Glass Search Icon
        ''' </summary>
        Public Shared Function CreateSearchIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.PixelOffsetMode = PixelOffsetMode.HighQuality
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.8!, size / 9.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    ' Circle glass
                    Dim glassSize As Single = size * 0.52!
                    g.DrawEllipse(pen, strokeWidth, strokeWidth, glassSize, glassSize)

                    ' Handle line
                    Dim x1 As Single = strokeWidth + glassSize * 0.85!
                    Dim y1 As Single = strokeWidth + glassSize * 0.85!
                    Dim x2 As Single = size - strokeWidth - 1
                    Dim y2 As Single = size - strokeWidth - 1
                    g.DrawLine(pen, x1, y1, x2, y2)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Plus (+) Action Icon
        ''' </summary>
        Public Shared Function CreatePlusIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(2.0!, size / 8.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    Dim margin As Single = size * 0.2!
                    ' Horizontal line
                    g.DrawLine(pen, margin, size / 2.0!, size - margin, size / 2.0!)
                    ' Vertical line
                    g.DrawLine(pen, size / 2.0!, margin, size / 2.0!, size - margin)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Import / Upload Arrow Icon
        ''' </summary>
        Public Shared Function CreateImportIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.8!, size / 9.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    ' Upward arrow shaft
                    g.DrawLine(pen, size / 2.0!, size * 0.75!, size / 2.0!, size * 0.2!)
                    ' Arrow head
                    g.DrawLine(pen, size / 2.0!, size * 0.2!, size * 0.25!, size * 0.45!)
                    g.DrawLine(pen, size / 2.0!, size * 0.2!, size * 0.75!, size * 0.45!)

                    ' Bottom tray
                    g.DrawLine(pen, size * 0.18!, size * 0.85!, size * 0.82!, size * 0.85!)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Export / Download Arrow Icon
        ''' </summary>
        Public Shared Function CreateExportIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.8!, size / 9.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    ' Downward arrow shaft
                    g.DrawLine(pen, size / 2.0!, size * 0.2!, size / 2.0!, size * 0.75!)
                    ' Arrow head
                    g.DrawLine(pen, size / 2.0!, size * 0.75!, size * 0.25!, size * 0.5!)
                    g.DrawLine(pen, size / 2.0!, size * 0.75!, size * 0.75!, size * 0.5!)

                    ' Bottom tray
                    g.DrawLine(pen, size * 0.18!, size * 0.85!, size * 0.82!, size * 0.85!)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Circular Refresh Icon
        ''' </summary>
        Public Shared Function CreateRefreshIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.8!, size / 9.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    Dim rect As New RectangleF(strokeWidth + 1, strokeWidth + 1, size - (strokeWidth * 2) - 2, size - (strokeWidth * 2) - 2)
                    g.DrawArc(pen, rect, 45, 270)

                    ' Arrow head at start of arc
                    Dim arrowX As Single = rect.Right - 1
                    Dim arrowY As Single = rect.Top + (rect.Height / 2.0!)
                    g.DrawLine(pen, arrowX, arrowY, arrowX - 4, arrowY - 4)
                    g.DrawLine(pen, arrowX, arrowY, arrowX - 4, arrowY + 4)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Filter Funnel Icon
        ''' </summary>
        Public Shared Function CreateFilterIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.6!, size / 10.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round
                    pen.LineJoin = LineJoin.Round

                    Dim points As PointF() = {
                        New PointF(size * 0.15!, size * 0.2!),
                        New PointF(size * 0.85!, size * 0.2!),
                        New PointF(size * 0.58!, size * 0.55!),
                        New PointF(size * 0.58!, size * 0.85!),
                        New PointF(size * 0.42!, size * 0.75!),
                        New PointF(size * 0.42!, size * 0.55!)
                    }
                    g.DrawPolygon(pen, points)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Edit Pencil Icon
        ''' </summary>
        Public Shared Function CreateEditIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.6!, size / 10.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    ' Pencil body diagonal line
                    g.DrawLine(pen, size * 0.2!, size * 0.8!, size * 0.75!, size * 0.25!)
                    ' Tip
                    g.DrawLine(pen, size * 0.2!, size * 0.8!, size * 0.15!, size * 0.85!)
                    g.DrawLine(pen, size * 0.15!, size * 0.85!, size * 0.35!, size * 0.8!)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Trash Can Icon
        ''' </summary>
        Public Shared Function CreateTrashIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.6!, size / 10.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    ' Lid handle
                    g.DrawLine(pen, size * 0.35!, size * 0.18!, size * 0.65!, size * 0.18!)
                    ' Top rim line
                    g.DrawLine(pen, size * 0.15!, size * 0.28!, size * 0.85!, size * 0.28!)

                    ' Bucket body
                    Dim pts As PointF() = {
                        New PointF(size * 0.22!, size * 0.32!),
                        New PointF(size * 0.28!, size * 0.85!),
                        New PointF(size * 0.72!, size * 0.85!),
                        New PointF(size * 0.78!, size * 0.32!)
                    }
                    g.DrawLines(pen, pts)

                    ' Vertical ribs
                    g.DrawLine(pen, size * 0.42!, size * 0.42!, size * 0.42!, size * 0.75!)
                    g.DrawLine(pen, size * 0.58!, size * 0.42!, size * 0.58!, size * 0.75!)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Eye View Details Icon
        ''' </summary>
        Public Shared Function CreateEyeIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.6!, size / 10.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    ' Eye outline curves
                    g.DrawArc(pen, size * 0.1!, size * 0.22!, size * 0.8!, size * 0.56!, 190, 160)
                    g.DrawArc(pen, size * 0.1!, size * 0.22!, size * 0.8!, size * 0.56!, 10, 160)

                    ' Pupil
                    Dim pupilR As Single = size * 0.18!
                    g.DrawEllipse(pen, (size / 2.0!) - pupilR, (size / 2.0!) - pupilR, pupilR * 2, pupilR * 2)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Close X Icon
        ''' </summary>
        Public Shared Function CreateCloseIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(2.0!, size / 8.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    Dim margin As Single = size * 0.25!
                    g.DrawLine(pen, margin, margin, size - margin, size - margin)
                    g.DrawLine(pen, size - margin, margin, margin, size - margin)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Excel / Spreadsheet Document Icon
        ''' </summary>
        Public Shared Function CreateExcelIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.6!, size / 10.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    ' Outer document border
                    g.DrawRectangle(pen, size * 0.18!, size * 0.12!, size * 0.64!, size * 0.76!)

                    ' Table grid lines
                    g.DrawLine(pen, size * 0.18!, size * 0.42!, size * 0.82!, size * 0.42!)
                    g.DrawLine(pen, size * 0.18!, size * 0.62!, size * 0.82!, size * 0.62!)
                    g.DrawLine(pen, size * 0.5!, size * 0.42!, size * 0.5!, size * 0.88!)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Checkmark Validation Icon
        ''' </summary>
        Public Shared Function CreateCheckIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(2.0!, size / 8.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round
                    pen.LineJoin = LineJoin.Round

                    Dim pts As PointF() = {
                        New PointF(size * 0.2!, size * 0.55!),
                        New PointF(size * 0.42!, size * 0.75!),
                        New PointF(size * 0.82!, size * 0.28!)
                    }
                    g.DrawLines(pen, pts)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Red Cross (X) Circle Icon
        ''' </summary>
        Public Shared Function CreateCrossIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(2.0!, size / 8.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    Dim margin As Single = size * 0.28!
                    g.DrawLine(pen, margin, margin, size - margin, size - margin)
                    g.DrawLine(pen, size - margin, margin, margin, size - margin)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Warning Triangle / Alert Icon
        ''' </summary>
        Public Shared Function CreateAlertIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(1.8!, size / 9.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round
                    pen.LineJoin = LineJoin.Round

                    Dim pts As PointF() = {
                        New PointF(size * 0.5!, size * 0.15!),
                        New PointF(size * 0.88!, size * 0.82!),
                        New PointF(size * 0.12!, size * 0.82!)
                    }
                    g.DrawPolygon(pen, pts)
                    g.DrawLine(pen, size * 0.5!, size * 0.38!, size * 0.5!, size * 0.58!)
                    g.DrawLine(pen, size * 0.5!, size * 0.7!, size * 0.5!, size * 0.73!)
                End Using
            End Using
            Return bmp
        End Function

        ''' <summary>
        ''' Renders a crisp vector Question Mark (?) Icon
        ''' </summary>
        Public Shared Function CreateQuestionIcon(color As Color, Optional size As Integer = 16) As Bitmap
            Dim bmp As New Bitmap(size, size)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.Clear(Color.Transparent)

                Dim strokeWidth As Single = Math.Max(2.0!, size / 8.0!)
                Using pen As New Pen(color, strokeWidth)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round

                    g.DrawArc(pen, size * 0.25!, size * 0.18!, size * 0.5!, size * 0.4!, 180, 180)
                    g.DrawLine(pen, size * 0.5!, size * 0.55!, size * 0.5!, size * 0.65!)
                    g.DrawLine(pen, size * 0.5!, size * 0.78!, size * 0.5!, size * 0.81!)
                End Using
            End Using
            Return bmp
        End Function
    End Class
End Namespace
