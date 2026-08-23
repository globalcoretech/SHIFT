Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms.Main
    ''' <summary>
    ''' Centralized theme constants for the Enterprise Desktop Shell UI system.
    ''' Enforces ultra-premium ₹50,000 modern SaaS design system styling.
    ''' </summary>
    Public Module ThemeConstants
        ' Header & Branding Palette
        Public ReadOnly HeaderBackground As Color = Color.FromArgb(15, 23, 42)          ' #0F172A Dark Slate
        Public ReadOnly HeaderForeground As Color = Color.FromArgb(255, 255, 255)      ' White
        Public ReadOnly HeaderSubText As Color = Color.FromArgb(148, 163, 184)         ' #94A3B8
        Public ReadOnly HeaderBadgeBg As Color = Color.FromArgb(30, 41, 59)           ' #1E293B

        ' Left Navigation Menu Palette
        Public ReadOnly NavigationBackground As Color = Color.FromArgb(15, 23, 42)      ' #0F172A Midnight Dark
        Public ReadOnly NavigationItemHover As Color = Color.FromArgb(30, 41, 59)        ' #1E293B
        Public ReadOnly NavigationItemSelected As Color = Color.FromArgb(79, 70, 229)     ' #4F46E5 Electric Indigo
        Public ReadOnly NavigationActiveIndicator As Color = Color.FromArgb(56, 189, 248) ' #38BDF8 High-Contrast Sky Cyan Indicator Bar
        Public ReadOnly NavigationText As Color = Color.FromArgb(226, 232, 240)        ' #E2E8F0
        Public ReadOnly NavigationMutedText As Color = Color.FromArgb(100, 116, 139)    ' #64748B

        ' Workspace & Cards Palette
        Public ReadOnly WorkspaceBackground As Color = Color.FromArgb(248, 250, 252)   ' #F8FAFC
        Public ReadOnly CardBackground As Color = Color.FromArgb(255, 255, 255)       ' White
        Public ReadOnly CardBorder As Color = Color.FromArgb(226, 232, 240)           ' #E2E8F0
        Public ReadOnly TextPrimary As Color = Color.FromArgb(15, 23, 42)             ' #0F172A
        Public ReadOnly TextSecondary As Color = Color.FromArgb(71, 85, 105)          ' #475569
        Public ReadOnly TextMuted As Color = Color.FromArgb(148, 163, 184)            ' #94A3B8

        ' Modern Accent System
        Public ReadOnly PrimaryAccent As Color = Color.FromArgb(79, 70, 229)           ' #4F46E5 Indigo
        Public ReadOnly SecondaryAccent As Color = Color.FromArgb(139, 92, 246)        ' #8B5CF6 Purple
        Public ReadOnly SuccessGreen As Color = Color.FromArgb(16, 185, 129)           ' #10B981 Emerald
        Public ReadOnly WarningOrange As Color = Color.FromArgb(245, 158, 11)          ' #F59E0B Amber
        Public ReadOnly DangerRed As Color = Color.FromArgb(239, 68, 68)               ' #EF4444 Crimson
        Public ReadOnly DangerRedHover As Color = Color.FromArgb(220, 38, 38)          ' #DC2626
        Public ReadOnly InfoBlue As Color = Color.FromArgb(59, 130, 246)               ' #3B82F6 Sky Blue

        Public ReadOnly StatusBackground As Color = Color.FromArgb(15, 23, 42)          ' #0F172A
        Public ReadOnly StatusForeground As Color = Color.FromArgb(226, 232, 240)      ' #E2E8F0

        Public Const FontNameDefault As String = "Segoe UI"

        ''' <summary>
        ''' Applies modern SaaS DataGridView styling across all grid tables.
        ''' </summary>
        Public Sub ApplyModernGridStyle(grid As DataGridView)
            If grid Is Nothing Then Return

            grid.BackgroundColor = Color.White
            grid.BorderStyle = BorderStyle.None
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            grid.GridColor = Color.FromArgb(241, 245, 249) ' Soft Slate-100 gridlines

            ' Column Headers Styling
            grid.EnableHeadersVisualStyles = False
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryAccent '#4F46E5 Indigo Header
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PrimaryAccent '#4F46E5 Uniform Selection
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White
            grid.ColumnHeadersDefaultCellStyle.Font = New Font(FontNameDefault, 9.5!, FontStyle.Bold)
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(4, 2, 4, 2)
            grid.ColumnHeadersHeight = 40

            ' Rows Styling
            grid.DefaultCellStyle.BackColor = Color.White
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59)
            grid.DefaultCellStyle.Font = New Font(FontNameDefault, 9.0!, FontStyle.Regular)
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(199, 210, 254) '#C7D2FE Indigo-200 Distinct Selection
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 27, 75)   '#1E1B4B Deep Indigo Text
            grid.DefaultCellStyle.Padding = New Padding(4, 2, 4, 2)

            ' Alternate Rows Zebra Striping & Selection
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252) '#F8FAFC
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(199, 210, 254) '#C7D2FE
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 27, 75)
            grid.RowTemplate.Height = 42
        End Sub

        ''' <summary>
        ''' Applies Primary Indigo styling to KryptonButton controls.
        ''' </summary>
        Public Sub ApplyKryptonPrimaryButton(btn As Krypton.Toolkit.KryptonButton)
            If btn Is Nothing Then Return
            btn.StateCommon.Back.Color1 = PrimaryAccent '#4F46E5
            btn.StateCommon.Back.Color2 = PrimaryAccent
            btn.StateCommon.Content.ShortText.Color1 = Color.White
            btn.StateCommon.Content.ShortText.Font = New Font(FontNameDefault, 9.0!, FontStyle.Bold)
            btn.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All
            btn.StateCommon.Border.Color1 = PrimaryAccent
            btn.StateCommon.Border.Rounding = 6
            btn.StateCommon.Border.Width = 1
            btn.StateTracking.Back.Color1 = Color.FromArgb(67, 56, 202) '#4338CA Hover
            btn.StateTracking.Back.Color2 = Color.FromArgb(67, 56, 202)
        End Sub

        ''' <summary>
        ''' Applies Secondary Outline Slate styling to KryptonButton controls.
        ''' </summary>
        Public Sub ApplyKryptonSecondaryButton(btn As Krypton.Toolkit.KryptonButton)
            If btn Is Nothing Then Return
            btn.StateCommon.Back.Color1 = Color.White
            btn.StateCommon.Back.Color2 = Color.White
            btn.StateCommon.Content.ShortText.Color1 = Color.FromArgb(30, 41, 59) '#1E293B
            btn.StateCommon.Content.ShortText.Font = New Font(FontNameDefault, 9.0!, FontStyle.Bold)
            btn.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All
            btn.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225) '#CBD5E1
            btn.StateCommon.Border.Rounding = 6
            btn.StateCommon.Border.Width = 1
            btn.StateTracking.Back.Color1 = Color.FromArgb(241, 245, 249) '#F1F5F9 Hover
            btn.StateTracking.Back.Color2 = Color.FromArgb(241, 245, 249)
            btn.StateTracking.Border.Color1 = Color.FromArgb(148, 163, 184) '#94A3B8
        End Sub

        ''' <summary>
        ''' Applies application-native Krypton styling to KryptonTextBox controls.
        ''' Eliminates classic Windows 3D borders and applies indigo active accent border.
        ''' </summary>
        Public Sub ApplyAppTextBoxStyle(txt As Krypton.Toolkit.KryptonTextBox)
            If txt Is Nothing Then Return
            txt.StateCommon.Back.Color1 = Color.White
            txt.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All
            txt.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225) '#CBD5E1 Slate-300
            txt.StateCommon.Border.Rounding = 6
            txt.StateCommon.Border.Width = 1
            txt.StateCommon.Content.Font = New Font(FontNameDefault, 9.0!, FontStyle.Regular)
            txt.StateCommon.Content.Color1 = TextPrimary
            txt.StateCommon.Content.Padding = New Padding(6, 4, 6, 4)
            txt.StateActive.Border.Color1 = PrimaryAccent
            txt.StateActive.Border.Color2 = PrimaryAccent
        End Sub

        ''' <summary>
        ''' Applies application-native Krypton styling to KryptonComboBox controls.
        ''' Integrated dropdown arrow styling and zero detached native arrow artifacts.
        ''' </summary>
        Public Sub ApplyAppComboBoxStyle(cbo As Krypton.Toolkit.KryptonComboBox)
            If cbo Is Nothing Then Return
            cbo.DropDownStyle = ComboBoxStyle.DropDownList
            cbo.StateCommon.ComboBox.Back.Color1 = Color.White
            cbo.StateCommon.ComboBox.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All
            cbo.StateCommon.ComboBox.Border.Color1 = Color.FromArgb(203, 213, 225) '#CBD5E1 Slate-300
            cbo.StateCommon.ComboBox.Border.Rounding = 6
            cbo.StateCommon.ComboBox.Border.Width = 1
            cbo.StateCommon.ComboBox.Content.Font = New Font(FontNameDefault, 9.0!, FontStyle.Regular)
            cbo.StateCommon.ComboBox.Content.Color1 = TextPrimary
            cbo.StateCommon.ComboBox.Content.Padding = New Padding(6, 4, 6, 4)
            cbo.StateActive.ComboBox.Border.Color1 = PrimaryAccent
            cbo.StateActive.ComboBox.Border.Color2 = PrimaryAccent
            cbo.StateCommon.Item.Content.ShortText.Font = New Font(FontNameDefault, 9.0!, FontStyle.Regular)
            cbo.StateCommon.Item.Content.ShortText.Color1 = TextPrimary
        End Sub

        ''' <summary>
        ''' Applies application-native Krypton styling to KryptonDateTimePicker controls.
        ''' Integrated calendar button boundary and zero floating classic button artifacts.
        ''' </summary>
        Public Sub ApplyAppDatePickerStyle(dtp As Krypton.Toolkit.KryptonDateTimePicker)
            If dtp Is Nothing Then Return
            dtp.StateCommon.Back.Color1 = Color.White
            dtp.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All
            dtp.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225) '#CBD5E1 Slate-300
            dtp.StateCommon.Border.Rounding = 6
            dtp.StateCommon.Border.Width = 1
            dtp.StateCommon.Content.Font = New Font(FontNameDefault, 9.0!, FontStyle.Regular)
            dtp.StateCommon.Content.Color1 = TextPrimary
            dtp.StateCommon.Content.Padding = New Padding(6, 4, 6, 4)
            dtp.StateActive.Border.Color1 = PrimaryAccent
            dtp.StateActive.Border.Color2 = PrimaryAccent
        End Sub

        ''' <summary>
        ''' Applies consistent flat SaaS input control styling to Krypton inputs.
        ''' </summary>
        Public Sub ApplyKryptonInputStyle(ctrl As Control)
            If ctrl Is Nothing Then Return

            If TypeOf ctrl Is Krypton.Toolkit.KryptonComboBox Then
                ApplyAppComboBoxStyle(CType(ctrl, Krypton.Toolkit.KryptonComboBox))
            ElseIf TypeOf ctrl Is Krypton.Toolkit.KryptonDateTimePicker Then
                ApplyAppDatePickerStyle(CType(ctrl, Krypton.Toolkit.KryptonDateTimePicker))
            ElseIf TypeOf ctrl Is Krypton.Toolkit.KryptonTextBox Then
                ApplyAppTextBoxStyle(CType(ctrl, Krypton.Toolkit.KryptonTextBox))
            End If
        End Sub

        ''' <summary>
        ''' Applies consistent hero page header styling across all module screens.
        ''' Fixes text layer overlap by enforcing 20px left margin, 14px top title padding, and 46px subtitle vertical placement.
        ''' </summary>
        Public Sub ApplyHeaderStyle(headerPanel As Panel, titleLabel As Label, subtitleLabel As Label)
            If headerPanel IsNot Nothing Then
                headerPanel.Dock = DockStyle.Top
                headerPanel.Height = 84
                headerPanel.BackColor = Color.FromArgb(30, 27, 75) '#1E1B4B Deep Indigo
                headerPanel.Padding = New Padding(20, 14, 20, 12)
                headerPanel.Margin = New Padding(0, 0, 0, 10)
            End If

            If titleLabel IsNot Nothing Then
                titleLabel.Font = New Font(FontNameDefault, 13.0!, FontStyle.Bold)
                titleLabel.ForeColor = Color.White
                titleLabel.AutoSize = True
                titleLabel.Location = New Point(20, 14)
            End If

            If subtitleLabel IsNot Nothing Then
                subtitleLabel.Font = New Font(FontNameDefault, 8.5!, FontStyle.Regular)
                subtitleLabel.ForeColor = Color.FromArgb(199, 210, 254) '#C7D2FE Light Indigo
                subtitleLabel.AutoSize = True
                subtitleLabel.Location = New Point(20, 46)
            End If
        End Sub

        ''' <summary>
        ''' Applies consistent top toolbar container styling across all modules.
        ''' </summary>
        Public Sub ApplyToolbarStyle(toolbarPanel As Panel)
            If toolbarPanel Is Nothing Then Return
            toolbarPanel.Dock = DockStyle.Top
            toolbarPanel.Height = 56
            toolbarPanel.BackColor = Color.White
            toolbarPanel.Padding = Padding.Empty
            toolbarPanel.Margin = New Padding(0, 0, 0, 12)
        End Sub

        ''' <summary>
        ''' Applies clean white rounded surface styling for content cards.
        ''' </summary>
        Public Sub ApplyCardSurfaceStyle(cardPanel As Panel)
            If cardPanel Is Nothing Then Return
            cardPanel.BackColor = Color.White
            cardPanel.Padding = New Padding(12)
        End Sub

        ''' <summary>
        ''' Applies consistent styling to standard WinForms input controls.
        ''' Ensures dropdown arrows belong visually to ComboBox controls without detached gaps.
        ''' </summary>
        Public Sub ApplyStandardInputStyle(ctrl As Control)
            If ctrl Is Nothing Then Return

            ctrl.Font = New Font(FontNameDefault, 9.0!, FontStyle.Regular)
            ctrl.BackColor = Color.White
            ctrl.ForeColor = TextPrimary

            If TypeOf ctrl Is ComboBox Then
                Dim cbo = CType(ctrl, ComboBox)
                cbo.FlatStyle = FlatStyle.Standard
            End If
        End Sub

        ''' <summary>
        ''' Applies hybrid DataGrid column sizing to prevent clipping of critical statutory columns
        ''' (e.g. GSTIN, PAN, Client Code, Mobile Phone) while allocating remaining space cleanly on 1366x768 displays.
        ''' </summary>
        Public Sub ApplyHybridGridColumnSizing(grid As DataGridView)
            If grid Is Nothing Then Return

            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None

            For Each col As DataGridViewColumn In grid.Columns
                Dim headerUpper = col.HeaderText.ToUpperInvariant()
                If headerUpper.Contains("GSTIN") Then
                    col.Width = 145
                    col.MinimumWidth = 135
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                ElseIf headerUpper.Contains("PAN") Then
                    col.Width = 95
                    col.MinimumWidth = 85
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                ElseIf headerUpper.Contains("CODE") Then
                    col.Width = 75
                    col.MinimumWidth = 65
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                ElseIf headerUpper.Contains("MOBILE") OrElse headerUpper.Contains("PHONE") Then
                    col.Width = 95
                    col.MinimumWidth = 85
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                ElseIf headerUpper.Contains("SCHEME") Then
                    col.Width = 110
                    col.MinimumWidth = 95
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                ElseIf headerUpper.Contains("ENTITY") OrElse (headerUpper.Contains("TYPE") AndAlso Not headerUpper.Contains("NAME")) Then
                    col.Width = 115
                    col.MinimumWidth = 100
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                ElseIf headerUpper.Contains("STATE") Then
                    col.Width = 105
                    col.MinimumWidth = 90
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                ElseIf headerUpper.Contains("DISTRICT") Then
                    col.Width = 85
                    col.MinimumWidth = 75
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                ElseIf headerUpper.Contains("CONTACT") Then
                    col.FillWeight = 110
                    col.MinimumWidth = 95
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                ElseIf headerUpper.Contains("NAME") OrElse headerUpper.Contains("TITLE") OrElse headerUpper.Contains("BUSINESS") Then
                    col.FillWeight = 160
                    col.MinimumWidth = 140
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                Else
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet
                End If
            Next
        End Sub

        ''' <summary>
        ''' Applies unified status badge pill styling across all modules.
        ''' </summary>
        Public Sub ApplyBadgeStyle(lbl As Label, badgeType As String)
            If lbl Is Nothing Then Return
            lbl.Font = New Font(FontNameDefault, 8.0!, FontStyle.Bold)
            lbl.TextAlign = ContentAlignment.MiddleCenter
            lbl.AutoSize = False

            Select Case If(badgeType, "").ToLowerInvariant()
                Case "overdue", "danger", "high"
                    lbl.BackColor = Color.FromArgb(253, 232, 232) '#FDE8E8 Soft Red Pill
                    lbl.ForeColor = Color.FromArgb(155, 28, 28)    '#9B1C1C Dark Crimson
                Case "duetoday", "warning", "medium"
                    lbl.BackColor = Color.FromArgb(254, 243, 199) '#FEF3C7 Soft Amber Pill
                    lbl.ForeColor = Color.FromArgb(146, 64, 14)    '#92400E Dark Amber
                Case "upcoming", "info", "low"
                    lbl.BackColor = Color.FromArgb(225, 239, 254) '#E1EFFE Soft Blue Pill
                    lbl.ForeColor = Color.FromArgb(30, 66, 159)    '#1E429F Dark Blue
                Case "completed", "success", "verified"
                    lbl.BackColor = Color.FromArgb(222, 247, 236) '#DEF7EC Soft Emerald Pill
                    lbl.ForeColor = Color.FromArgb(3, 84, 63)      '#03543F Dark Emerald
                Case Else
                    lbl.BackColor = Color.FromArgb(241, 245, 249) '#F1F5F9 Slate Pill
                    lbl.ForeColor = TextSecondary
            End Select
        End Sub

        ''' <summary>
        ''' Applies consistent tab navigation button styling.
        ''' </summary>
        Public Sub ApplyTabStyle(btn As Button, isActive As Boolean)
            If btn Is Nothing Then Return
            btn.Font = New Font(FontNameDefault, 9.0!, If(isActive, FontStyle.Bold, FontStyle.Regular))
            btn.ForeColor = If(isActive, PrimaryAccent, TextSecondary)
            btn.BackColor = Color.Transparent
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.Cursor = Cursors.Hand
        End Sub

        ''' <summary>
        ''' Applies consistent psychological empty state card styling.
        ''' </summary>
        Public Sub ApplyEmptyStateStyle(panel As Panel, iconHost As Control, titleLabel As Label, subtitleLabel As Label)
            If panel IsNot Nothing Then
                panel.BackColor = Color.White
            End If
            If titleLabel IsNot Nothing Then
                titleLabel.Font = New Font(FontNameDefault, 12.0!, FontStyle.Bold)
                titleLabel.ForeColor = TextPrimary
                titleLabel.TextAlign = ContentAlignment.MiddleCenter
            End If
            If subtitleLabel IsNot Nothing Then
                subtitleLabel.Font = New Font(FontNameDefault, 9.0!, FontStyle.Regular)
                subtitleLabel.ForeColor = TextSecondary
                subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            End If
        End Sub
    End Module
End Namespace
