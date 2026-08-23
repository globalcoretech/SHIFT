Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports System.Text
Imports System.Xml.Linq

Namespace Forms.Main
    ''' <summary>
    ''' Ultra-Fast Native OpenXML Excel (.XLSX) Generator and Parser Engine.
    ''' Zero External Dependencies: Built directly on .NET System.IO.Compression.ZipArchive and System.Xml.Linq.
    ''' Produces Professionally Styled Excel Templates (Navy Header, Custom Column Widths, Cell Gridlines, Text Formatting)
    ''' and Natively Reads .XLSX Spreadsheets.
    ''' </summary>
    Public Class NativeExcelEngine
        Private Shared ReadOnly SheetNamespace As XNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"

        ''' <summary>
        ''' Generates a Professionally Styled .xlsx Excel Template File for Client Import.
        ''' </summary>
        Public Shared Sub GenerateStyledTemplateXlsx(outputPath As String)
            If File.Exists(outputPath) Then
                File.Delete(outputPath)
            End If

            Dim headers As String() = {"GSTIN", "ClientCode", "ClientName", "PanNumber", "EntityType", "GstType", "StateName", "District", "Pincode", "Address", "ContactPerson", "Phone", "Email"}

            Dim sampleData As New List(Of String()) From {
                New String() {"27AABCA1234F1Z5", "CLI-001", "Apex Global Logistics Pvt Ltd", "AABCA1234F", "Pvt Ltd", "Regular Monthly", "27 - Maharashtra", "Mumbai", "400001", "Plot 42 Commercial Complex", "Rajesh Sharma", "9820098200", "info@apexglobal.com"},
                New String() {"07AAAFR9876E1ZB", "CLI-002", "Royal Traders & Co", "AAAFR9876E", "Proprietorship", "Regular QRMP", "07 - Delhi", "Central Delhi", "110001", "12 Connaught Place", "Amit Roy", "9811198111", "contact@royaltraders.com"},
                New String() {"", "CLI-003", "Star Consultancy Services", "AABCS5432K", "Individual", "Unregistered", "27 - Maharashtra", "Pune", "411001", "88 MG Road", "Suresh Kumar", "9890098900", "suresh@starconsult.in"}
            }

            ' Build Shared Strings Dictionary
            Dim sharedStrings As New List(Of String)()
            Dim sharedStringMap As New Dictionary(Of String, Integer)(StringComparer.Ordinal)

            Dim getSharedStringIndex = Function(text As String) As Integer
                                           If text Is Nothing Then text = String.Empty
                                           If Not sharedStringMap.ContainsKey(text) Then
                                               sharedStringMap(text) = sharedStrings.Count
                                               sharedStrings.Add(text)
                                           End If
                                           Return sharedStringMap(text)
                                       End Function

            ' Pre-register headers and sample data in shared strings
            For Each h In headers
                getSharedStringIndex(h)
            Next
            For Each row In sampleData
                For Each cellVal In row
                    getSharedStringIndex(cellVal)
                Next
            Next

            Using archive As ZipArchive = ZipFile.Open(outputPath, ZipArchiveMode.Create)
                ' 1. [Content_Types].xml
                WriteArchiveFile(archive, "[Content_Types].xml",
                    "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
                    "<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">" &
                    "  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>" &
                    "  <Default Extension=""xml"" ContentType=""application/xml""/>" &
                    "  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>" &
                    "  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>" &
                    "  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>" &
                    "  <Override PartName=""/xl/sharedStrings.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>" &
                    "</Types>")

                ' 2. _rels/.rels
                WriteArchiveFile(archive, "_rels/.rels",
                    "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
                    "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
                    "  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>" &
                    "</Relationships>")

                ' 3. xl/_rels/workbook.xml.rels
                WriteArchiveFile(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
                    "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
                    "  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>" &
                    "  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>" &
                    "  <Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>" &
                    "</Relationships>")

                ' 4. xl/workbook.xml
                WriteArchiveFile(archive, "xl/workbook.xml",
                    "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
                    "<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">" &
                    "  <sheets>" &
                    "    <sheet name=""Client_Import_Template"" sheetId=""1"" r:id=""rId1""/>" &
                    "  </sheets>" &
                    "</workbook>")

                ' 5. xl/styles.xml (Styles: Navy Header Fill #1E1B4B, White Bold Font, Cell Borders, Text Format @)
                WriteArchiveFile(archive, "xl/styles.xml",
                    "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
                    "<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">" &
                    "  <numFmts count=""1"">" &
                    "    <numFmt numFmtId=""49"" formatCode=""@""/>" &
                    "  </numFmts>" &
                    "  <fonts count=""2"">" &
                    "    <font><sz val=""11""/><color theme=""1""/><name val=""Segoe UI""/></font>" &
                    "    <font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Segoe UI""/></font>" &
                    "  </fonts>" &
                    "  <fills count=""3"">" &
                    "    <fill><patternFill patternType=""none""/></fill>" &
                    "    <fill><patternFill patternType=""gray125""/></fill>" &
                    "    <fill><patternFill patternType=""solid""><fgColor rgb=""FF1E1B4B""/><bgColor indexed=""64""/></patternFill></fill>" &
                    "  </fills>" &
                    "  <borders count=""2"">" &
                    "    <border><left/><right/><top/><bottom/></border>" &
                    "    <border>" &
                    "      <left style=""thin""><color rgb=""FFD1D5DB""/></left>" &
                    "      <right style=""thin""><color rgb=""FFD1D5DB""/></right>" &
                    "      <top style=""thin""><color rgb=""FFD1D5DB""/></top>" &
                    "      <bottom style=""thin""><color rgb=""FFD1D5DB""/></bottom>" &
                    "    </border>" &
                    "  </borders>" &
                    "  <cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>" &
                    "  <cellXfs count=""3"">" &
                    "    <xf numFmtId=""49"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""/>" &
                    "    <xf numFmtId=""49"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyFont=""1"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>" &
                    "    <xf numFmtId=""49"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1"" applyAlignment=""1""><alignment horizontal=""left"" vertical=""center""/></xf>" &
                    "  </cellXfs>" &
                    "</styleSheet>")

                ' 6. xl/sharedStrings.xml
                Dim sbStrings As New StringBuilder()
                sbStrings.AppendLine("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
                sbStrings.AppendLine($"<sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" count=""{sharedStrings.Count}"" uniqueCount=""{sharedStrings.Count}"">")
                For Each itemStr In sharedStrings
                    sbStrings.AppendLine($"  <si><t>{System.Security.SecurityElement.Escape(itemStr)}</t></si>")
                Next
                sbStrings.AppendLine("</sst>")
                WriteArchiveFile(archive, "xl/sharedStrings.xml", sbStrings.ToString())

                ' 7. xl/worksheets/sheet1.xml (Custom Column Widths & Rows)
                Dim sbSheet As New StringBuilder()
                sbSheet.AppendLine("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
                sbSheet.AppendLine("<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">")

                ' Column Width Definitions
                sbSheet.AppendLine("  <cols>")
                sbSheet.AppendLine("    <col min=""1"" max=""1"" width=""22"" customWidth=""1""/>") ' GSTIN
                sbSheet.AppendLine("    <col min=""2"" max=""2"" width=""16"" customWidth=""1""/>") ' ClientCode
                sbSheet.AppendLine("    <col min=""3"" max=""3"" width=""34"" customWidth=""1""/>") ' ClientName
                sbSheet.AppendLine("    <col min=""4"" max=""4"" width=""16"" customWidth=""1""/>") ' PanNumber
                sbSheet.AppendLine("    <col min=""5"" max=""5"" width=""22"" customWidth=""1""/>") ' EntityType
                sbSheet.AppendLine("    <col min=""6"" max=""6"" width=""20"" customWidth=""1""/>") ' GstType
                sbSheet.AppendLine("    <col min=""7"" max=""7"" width=""22"" customWidth=""1""/>") ' StateName
                sbSheet.AppendLine("    <col min=""8"" max=""8"" width=""18"" customWidth=""1""/>") ' District
                sbSheet.AppendLine("    <col min=""9"" max=""9"" width=""14"" customWidth=""1""/>") ' Pincode
                sbSheet.AppendLine("    <col min=""10"" max=""10"" width=""36"" customWidth=""1""/>") ' Address
                sbSheet.AppendLine("    <col min=""11"" max=""11"" width=""22"" customWidth=""1""/>") ' ContactPerson
                sbSheet.AppendLine("    <col min=""12"" max=""12"" width=""16"" customWidth=""1""/>") ' Phone
                sbSheet.AppendLine("    <col min=""13"" max=""13"" width=""28"" customWidth=""1""/>") ' Email
                sbSheet.AppendLine("  </cols>")

                sbSheet.AppendLine("  <sheetData>")

                ' Header Row (Style 1 = Bold White Text, Navy Fill, Centered, Thin Borders)
                sbSheet.AppendLine("    <row r=""1"" ht=""28"" customHeight=""1"">")
                For colIdx As Integer = 0 To headers.Length - 1
                    Dim cellRef = GetCellReference(colIdx, 1)
                    Dim strIdx = getSharedStringIndex(headers(colIdx))
                    sbSheet.AppendLine($"      <c r=""{cellRef}"" t=""s"" s=""1""><v>{strIdx}</v></c>")
                Next
                sbSheet.AppendLine("    </row>")

                ' Data Rows (Style 2 = Standard Font, Left Aligned, Text Format @, Thin Borders)
                For rowIdx As Integer = 0 To sampleData.Count - 1
                    Dim excelRowNum = rowIdx + 2
                    sbSheet.AppendLine($"    <row r=""{excelRowNum}"" ht=""22"" customHeight=""1"">")
                    Dim rowVals = sampleData(rowIdx)
                    For colIdx As Integer = 0 To headers.Length - 1
                        Dim cellRef = GetCellReference(colIdx, excelRowNum)
                        Dim cellVal = If(colIdx < rowVals.Length, rowVals(colIdx), String.Empty)
                        Dim strIdx = getSharedStringIndex(cellVal)
                        sbSheet.AppendLine($"      <c r=""{cellRef}"" t=""s"" s=""2""><v>{strIdx}</v></c>")
                    Next
                    sbSheet.AppendLine("    </row>")
                Next

                sbSheet.AppendLine("  </sheetData>")
                sbSheet.AppendLine("</worksheet>")

                WriteArchiveFile(archive, "xl/worksheets/sheet1.xml", sbSheet.ToString())
            End Using
        End Sub

        ''' <summary>
        ''' Natively Reads an .xlsx Spreadsheet File without requiring Excel.
        ''' </summary>
        Public Shared Function ReadXlsxRows(filePath As String) As List(Of String())
            Dim rowsData As New List(Of String())()

            Using archive As ZipArchive = ZipFile.OpenRead(filePath)
                ' 1. Load Shared Strings Dictionary
                Dim sharedStrings As New List(Of String)()
                Dim sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml")

                If sharedStringsEntry IsNot Nothing Then
                    Using stream = sharedStringsEntry.Open()
                        Dim doc = XDocument.Load(stream)
                        For Each si In doc.Descendants(SheetNamespace + "si")
                            Dim textVal = String.Concat(si.Descendants(SheetNamespace + "t").Select(Function(t) t.Value))
                            sharedStrings.Add(textVal)
                        Next
                    End Using
                End If

                ' 2. Parse Sheet1.xml
                Dim sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                If sheetEntry Is Nothing Then
                    ' Fallback to first available worksheet
                    sheetEntry = archive.Entries.FirstOrDefault(Function(e) e.FullName.StartsWith("xl/worksheets/sheet") AndAlso e.FullName.EndsWith(".xml"))
                End If

                If sheetEntry IsNot Nothing Then
                    Using stream = sheetEntry.Open()
                        Dim doc = XDocument.Load(stream)

                        For Each rowNode In doc.Descendants(SheetNamespace + "row")
                            Dim rowCells As New Dictionary(Of Integer, String)()
                            Dim maxColIndex As Integer = -1

                            For Each cNode In rowNode.Elements(SheetNamespace + "c")
                                Dim cellRef = cNode.Attribute("r")?.Value
                                Dim colIndex As Integer = If(cellRef IsNot Nothing, GetColumnIndexFromCellRef(cellRef), rowCells.Count)

                                Dim cellType = cNode.Attribute("t")?.Value
                                Dim rawVal = cNode.Element(SheetNamespace + "v")?.Value

                                Dim cellText As String = String.Empty

                                If cellType = "s" AndAlso Not String.IsNullOrEmpty(rawVal) Then
                                    Dim sIdx As Integer
                                    If Integer.TryParse(rawVal, sIdx) AndAlso sIdx >= 0 AndAlso sIdx < sharedStrings.Count Then
                                        cellText = sharedStrings(sIdx)
                                    End If
                                ElseIf cellType = "inlineStr" Then
                                    cellText = String.Concat(cNode.Descendants(SheetNamespace + "t").Select(Function(t) t.Value))
                                Else
                                    cellText = If(rawVal, String.Empty)
                                End If

                                rowCells(colIndex) = cellText
                                If colIndex > maxColIndex Then maxColIndex = colIndex
                            Next

                            If maxColIndex >= 0 Then
                                Dim rowArray(maxColIndex) As String
                                For colIdx As Integer = 0 To maxColIndex
                                    If rowCells.ContainsKey(colIdx) Then
                                        rowArray(colIdx) = rowCells(colIdx)
                                    Else
                                        rowArray(colIdx) = String.Empty
                                    End If
                                Next
                                rowsData.Add(rowArray)
                            End If
                        Next
                    End Using
                End If
            End Using

            Return rowsData
        End Function

        Private Shared Sub WriteArchiveFile(archive As ZipArchive, entryPath As String, content As String)
            Dim entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal)
            Using writer As New StreamWriter(entry.Open(), Encoding.UTF8)
                writer.Write(content)
            End Using
        End Sub

        Private Shared Function GetCellReference(colIndex As Integer, rowIndex As Integer) As String
            Dim colName As String = String.Empty
            Dim div As Integer = colIndex + 1

            While div > 0
                Dim modVar As Integer = (div - 1) Mod 26
                colName = Chr(65 + modVar) & colName
                div = CInt(Math.Floor((div - modVar) / 26))
            End While

            Return $"{colName}{rowIndex}"
        End Function

        Private Shared Function GetColumnIndexFromCellRef(cellRef As String) As Integer
            Dim colName As String = String.Empty
            For Each ch In cellRef
                If Char.IsLetter(ch) Then
                    colName &= Char.ToUpper(ch)
                Else
                    Exit For
                End If
            Next

            Dim index As Integer = 0
            For Each ch In colName
                index = index * 26 + (Asc(ch) - Asc("A"c) + 1)
            Next

            Return index - 1
        End Function
    End Class
End Namespace
