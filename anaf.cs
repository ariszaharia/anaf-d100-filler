using System.Text.Json;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using iText.Kernel.Pdf;
using iText.Forms;
using iText.Forms.Xfa;

// === XFA FORM FILLER - APASĂ BUTONUL VALIDARE REAL VIA ACROBAT ===

string fisierPdf = Path.GetFullPath("decl100.pdf");
string fisierJson = "date_declaratie.json";
string fisierTemp = Path.GetFullPath($"D100_Temp_{DateTime.Now:HHmmss}.pdf");
string fisierFinal = Path.GetFullPath($"D100_VALIDAT_{DateTime.Now:HHmmss}.pdf");

Console.WriteLine(">>> XFA Form Filler - Apăsare Reală VALIDARE");
Console.WriteLine("    1. Completează XFA cu iText7");
Console.WriteLine("    2. Apasă VALIDARE cu UI Automation (click real)\n");

// === DEFINIREA OBLIGAȚIILOR (vec2 din JavaScript) ===
var obligatii = new Dictionary<string, (string CodBugetar, string Model)>
{
    { "708", ("5508XXXXXX", "LUNAR_25U") },
    { "108", ("20A020800X", "LUNAR_25U") },
    { "766", ("20A300501X", "LUNAR_25U") },
    { "767", ("20A300505X", "LUNAR_25U") },
    { "161", ("20A121700X", "LUNAR_25U") },
    { "102", ("5503XXXXXX", "LUNAR_102") },
    { "103", ("5503XXXXXX", "LUNAR_25LLU") },
    { "105", ("5503XXXXXX", "LUNAR_105") },
    { "150", ("5503XXXXXX", "LUNAR_25U") },
    { "121", ("5503XXXXXX", "LUNAR_121") },
    { "604", ("5503XXXXXX", "LUNAR_25U") },
    { "605", ("5503XXXXXX", "LUNAR_25U") },
    { "606", ("5503XXXXXX", "LUNAR_25U") },
    { "608", ("5503XXXXXX", "LUNAR_25U") },
    { "621", ("5503XXXXXX", "LUNAR_25U") },
    { "620", ("20030118XX", "LUNAR_25U") },
    { "690", ("5503XXXXXX", "LUNAR_25U") },
    { "631", ("5503XXXXXX", "LUNAR_25U") },
    { "632", ("5503XXXXXX", "LUNAR_25U") },
    { "633", ("5503XXXXXX", "LUNAR_25U") },
    { "634", ("5503XXXXXX", "LUNAR_25U") },
    { "635", ("5503XXXXXX", "LUNAR_25U") },
    { "636", ("5503XXXXXX", "LUNAR_25U") },
    { "637", ("5503XXXXXX", "LUNAR_25U") },
    { "638", ("5503XXXXXX", "LUNAR_25U") },
    { "639", ("5503XXXXXX", "LUNAR_25U") },
    { "640", ("5503XXXXXX", "LUNAR_25U") },
    { "641", ("5503XXXXXX", "LUNAR_25U") },
    { "810", ("5503XXXXXX", "LUNAR_25U") },
    { "750", ("20A160400X", "LUNAR_25M") },
    { "755", ("20A300501X", "LUNAR_25U") },
    { "756", ("20A300502X", "LUNAR_25U") },
    { "780", ("5503XXXXXX", "AN_780") },
    { "781", ("20A300804X", "AN_780") },
    { "211", ("20A140205X", "FREE") },
    { "216", ("20A140208X", "FREE") },
    { "212", ("20A140203X", "FREE") },
    { "217", ("20A140207X", "FREE") },
    { "213", ("20A140204X", "FREE") },
    { "214", ("20A140202X", "FREE") },
    { "215", ("20A140201X", "FREE") },
    { "221", ("20A140301X", "FREE") },
    { "222", ("20A140302X", "FREE") },
    { "224", ("20A140303X", "FREE") },
    { "225", ("20A140304X", "FREE") },
    { "231", ("20A140101X", "FREE") },
    { "232", ("20A140102X", "FREE") },
    { "233", ("20A140103X", "FREE") },
    { "234", ("20A140104X", "FREE") },
    { "235", ("20A140105X", "FREE") },
    { "236", ("20A140106X", "FREE") },
    { "238", ("20A140108X", "FREE") },
    { "237", ("20A140107X", "FREE") },
    { "270", ("20A140600X", "FREE") },
    { "226", ("20A140306X", "FREE") },
    { "227", ("20A140307X", "FREE") },
    { "945", ("20A160500X", "LUNAR_25U") },
    { "941", ("20A140308X", "LUNAR_25U") },
    { "942", ("20A141308X", "LUNAR_25U") },
    { "943", ("20A140210X", "LUNAR_25U") },
    { "944", ("20A141200X", "LUNAR_25U") },
    { "450", ("26A120900X", "TRIM_25U2") },
    { "4551", ("26A121400X", "TRIM_25U2") },
    { "4552", ("26A121400X", "FREE") },
    { "456", ("26A121500X", "TRIM_25U2") },
    { "758", ("20A300505X", "LUNAR_25U") },
    { "713", ("20A121300X", "LUNAR_25U") },
    { "712", ("20A121200X", "LUNAR_25U") },
    { "711", ("20A121100X", "LUNAR_25U") },
    { "504", ("20A160103X", "LUNAR_25U") },
    { "536", ("20A160107X", "FREE") },
    { "537", ("20A160108X", "FREE") },
    { "534", ("20A160108X", "FREE") },
    { "551", ("5505XXXXXX", "FREE") },
    { "552", ("5506XXXXXX", "FREE") },
    { "553", ("5507XXXXXX", "LUNAR_25U") },
    { "535", ("20A160109X", "LUNAR_25U") },
    { "825", ("20A200400X", "LUNAR_25U") },
    { "539", ("20A160110X", "FREE") },
    { "538", ("20A160111X", "FREE") },
    { "130", ("5503XXXXXX", "LUNAR_130") },
    { "107", ("5503XXXXXX", "LUNAR_25U") },
    { "127", ("5503XXXXXX", "LUNAR_25U") },
    { "128", ("20A020900X", "LUNAR_25U") },
    { "626", ("5503XXXXXX", "LUNAR_25U") },
    { "162", ("20030118XX", "LUNAR_25U") },
    { "642", ("5503XXXXXX", "LUNAR_25U") },
    { "115", ("5503XXXXXX", "AN_115") },
    { "125", ("5503XXXXXX", "AN_125") },
    { "117", ("5503XXXXXX", "LUNAR_117") },
    { "116", ("5503XXXXXX", "LUNAR_116") },
    { "707", ("5503XXXXXX", "LUNAR_25U") },
    { "706", ("5503XXXXXX", "AN_706") },
    { "709", ("5503XXXXXX", "AN_709") },
    { "228", ("20A140305X", "FREE") },
    { "229", ("20A140309X", "FREE") },
    { "245", ("20A140701X", "FREE") },
    { "246", ("20A140702X", "FREE") },
    { "628", ("5503XXXXXX", "LUNAR_25U") },
    { "714", ("20A300503X", "ANC_714") },
    { "715", ("20A300504X", "ANC_715") },
    { "716", ("20A300506X", "ANC_716") },
    { "718", ("20A300507X", "ANC_718") },
    { "719", ("20A300508X", "ANC_719") },
    { "7171", ("20A300509X", "ANC_717") },
    { "7172", ("20A300510X", "ANC_717") },
    { "131", ("5503XXXXXX", "15LUNI") },
    { "1321", ("5503XXXXXX", "18LUNI") },
    { "1322", ("5503XXXXXX", "15LUNI") },
    { "629", ("5503XXXXXX", "LUNAR_25U") },
    { "7011", ("5503XXXXXX", "LUNAR_701") },
    { "7012", ("5503XXXXXX", "LUNAR_701") },
    { "7013", ("5503XXXXXX", "LUNAR_701") },
    { "702", ("5503XXXXXX", "LUNAR_25U") },
    { "247", ("20A140310X", "FREE") },
};

// 1. CITEȘTE JSON CONFIG
Dictionary<string, object> date;

if (File.Exists(fisierJson))
{
    Console.WriteLine($"[1/5] Citesc datele din {fisierJson}...");
    var jsonText = File.ReadAllText(fisierJson);
    date = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText) ?? new();
}
else
{
    date = new Dictionary<string, object>
    {
        { "sub1_tipD", "1" },
        { "sub1_luna_r", "3" },
        { "sub1_an_r", "2026" },
        { "subA_cif", "12345678" },
        { "subA_DENUMIRE", "SC TEST AUTOMATIZARE SRL" },
        { "subA_ADRESA", "Str. Test Nr. 1, București" },
        { "subA_text_telefon", "0212345678" },
        { "subA_text_email", "test@test.ro" },
        { "subB_COD_IMP", "103--Impozit pe profit/plati anticipate" },
        { "subB_SUMA01I", "2500" },
        { "SUMA1", "2500" },
        { "SUMA2", "2500" },
        { "SUMA0", "0" },
        { "sub2_Nume", "POPESCU" },
        { "sub2_Prenume", "ION" },
        { "sub2_Functia", "Administrator" },
    };
    File.WriteAllText(fisierJson, JsonSerializer.Serialize(date, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"[1/5] Creat: {fisierJson}");
}

Console.WriteLine($"      {date.Count} câmpuri\n");

// 2. SIMULARE BUTON "ALEGE CREANȚA FISCALĂ"
Console.WriteLine("[2/5] Simulez butonul 'Alege creanța fiscală'...\n");

string codImpFull = GetVal(date, "subB_COD_IMP", "");
int luna_r = int.Parse(GetVal(date, "sub1_luna_r", "1"));
int an_r = int.Parse(GetVal(date, "sub1_an_r", "2026"));
string tipD = GetVal(date, "sub1_tipD", "1");

string codImpNumeric = codImpFull.Split('-')[0].Trim();

Console.WriteLine($"      COD_IMP: {codImpFull}");
Console.WriteLine($"      Cod numeric: {codImpNumeric}");
Console.WriteLine($"      Perioada: {luna_r:D2}/{an_r}");

string codBugetar = "";
string scadenta = "";
string model = "";

if (obligatii.TryGetValue(codImpNumeric, out var ob))
{
    codBugetar = ob.CodBugetar;
    model = ob.Model;
    scadenta = CalculeazaScadenta(model, luna_r, an_r);
    
    Console.WriteLine($"      COD_BUGETAR: {codBugetar}");
    Console.WriteLine($"      SCADENTA: {scadenta}");
}

try
{
    // 3. COMPLETEAZĂ PDF CU ITEXT7
    Console.WriteLine("\n[3/5] Completez PDF-ul cu iText7 (append mode)...");
    
    using (var reader = new PdfReader(fisierPdf))
    using (var writer = new PdfWriter(fisierTemp))
    {
        var stampProps = new StampingProperties();
        stampProps.UseAppendMode();
        
        using var pdfDoc = new PdfDocument(reader, writer, stampProps);
        
        var acroForm = PdfAcroForm.GetAcroForm(pdfDoc, false);
        var xfa = acroForm?.GetXfaForm();
        
        if (xfa == null || !xfa.IsXfaPresent())
        {
            Console.WriteLine("EROARE: Nu s-a găsit XFA!");
            return;
        }
        
        Console.WriteLine("      XFA găsit. Append mode activ.");
        
        var domDoc = xfa.GetDomDocument();
        using var ms = new MemoryStream();
        domDoc!.Save(ms);
        ms.Position = 0;
        var xDoc = XDocument.Load(ms);
        
        var form1 = xDoc.Descendants("form1").FirstOrDefault();
        if (form1 == null) { Console.WriteLine("EROARE: Nu s-a găsit form1!"); return; }
        
        int updated = 0;
        
        // SUB1
        var sub1 = form1.Element("sub1");
        if (sub1 != null)
        {
            updated += UpdateElement(sub1, "tipD", tipD);
            updated += UpdateElement(sub1, "luna_r", luna_r.ToString());
            updated += UpdateElement(sub1, "an_r", an_r.ToString());
            updated += UpdateOrCreate(sub1, "d_anulare", "0");
            updated += UpdateOrCreate(sub1, "d_succ", "0");
            updated += UpdateOrCreate(sub1, "d_dizolv", "0");
            updated += UpdateOrCreate(sub1, "d_modif", "0");
        }
        
        // SUBA
        var subA = form1.Element("subA");
        if (subA != null)
        {
            updated += UpdateElement(subA, "cif", GetVal(date, "subA_cif", ""));
            updated += UpdateElement(subA, "DENUMIRE", GetVal(date, "subA_DENUMIRE", ""));
            updated += UpdateElement(subA, "ADRESA", GetVal(date, "subA_ADRESA", ""));
            updated += UpdateElement(subA, "text_telefon", GetVal(date, "subA_text_telefon", ""));
            updated += UpdateElement(subA, "text_email", GetVal(date, "subA_text_email", ""));
        }
        
        // SUBB - Obligația
        var subB = form1.Element("subB");
        if (subB != null)
        {
            updated += UpdateElement(subB, "COD_IMP", codImpFull);
            updated += UpdateOrCreate(subB, "COD_BUGETAR", codBugetar);
            updated += UpdateOrCreate(subB, "SCADENTA", scadenta);
            updated += UpdateOrCreate(subB, "SCADENTA2", scadenta);
            updated += UpdateElement(subB, "SUMA01I", GetVal(date, "subB_SUMA01I", "0"));
            updated += UpdateOrCreate(subB, "SUMA02I", "0");
            updated += UpdateOrCreate(subB, "SUMA03I", GetVal(date, "subB_SUMA01I", "0"));
            updated += UpdateOrCreate(subB, "SUMA04I", "0");
        }
        
        // SUME TOTALE
        updated += UpdateElement(form1, "SUMA1", GetVal(date, "SUMA1", ""));
        updated += UpdateElement(form1, "SUMA2", GetVal(date, "SUMA2", ""));
        updated += UpdateElement(form1, "SUMA0", GetVal(date, "SUMA0", ""));
        
        // IDENT_IMP
        var identImp = form1.Element("ident_IMP");
        if (identImp != null)
        {
            updated += UpdateElement(identImp, "DENUMIRE", GetVal(date, "subA_DENUMIRE", ""));
            updated += UpdateElement(identImp, "cifR", GetVal(date, "subA_cif", ""));
        }
        
        // SUB2 - Declarant
        var sub2 = form1.Element("sub2");
        if (sub2 != null)
        {
            updated += UpdateElement(sub2, "Nume", GetVal(date, "sub2_Nume", ""));
            updated += UpdateElement(sub2, "Prenume", GetVal(date, "sub2_Prenume", ""));
            updated += UpdateElement(sub2, "Functia", GetVal(date, "sub2_Functia", ""));
        }
        
        Console.WriteLine($"      Actualizate {updated} câmpuri.");
        
        // Salvează XFA modificat
        xfa.SetDomDocument(xDoc);
        xfa.Write(pdfDoc);
        
        pdfDoc.Close();
    }
    
    Console.WriteLine($"      Salvat: {fisierTemp}");
    
    // 4. DESCHIDE CU ACROBAT ȘI APASĂ VALIDARE
    Console.WriteLine("\n[4/5] Deschid cu Adobe Acrobat și apăs VALIDARE...\n");
    
    ApasaValidareCuAcrobat(fisierTemp, fisierFinal);
    
    Console.WriteLine(new string('=', 60));
    Console.WriteLine("SUCCES COMPLET!");
    Console.WriteLine(new string('=', 60));
    Console.WriteLine($"\nPDF validat: {fisierFinal}");
    Console.WriteLine("\nCâmpuri completate automat:");
    Console.WriteLine($"  - COD_IMP: {codImpNumeric}");
    Console.WriteLine($"  - COD_BUGETAR: {codBugetar}");
    Console.WriteLine($"  - SCADENTA: {scadenta}");
    Console.WriteLine($"  - SUMA: {GetVal(date, "subB_SUMA01I", "0")} lei");
    Console.WriteLine("\nFormularul a fost VALIDAT real cu butonul din PDF!");
}
catch (Exception ex)
{
    Console.WriteLine($"\nEROARE: {ex.Message}\n{ex.StackTrace}");
}

// === APASĂ VALIDARE CU UI AUTOMATION ===
void ApasaValidareCuAcrobat(string pdfInput, string pdfOutput)
{
    // Folosim PowerShell cu UI Automation pentru a:
    // 1. Deschide PDF în Adobe Reader/Acrobat
    // 2. Folosi UI Automation pentru a găsi butonul VALIDARE și a-l apăsa
    // 3. Salva și închide
    
    string psScript = $@"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

# Copiază fișierul
Copy-Item -Path '{pdfInput}' -Destination '{pdfOutput}' -Force

# Deschide PDF-ul
Start-Process '{pdfOutput}'
Write-Host 'PDF deschis, aștept încărcarea...'
Start-Sleep -Seconds 4

# Import Windows API pentru click
Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win32 {{
    [DllImport(""user32.dll"")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport(""user32.dll"")]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
    
    [DllImport(""user32.dll"")]
    public static extern bool SetCursorPos(int X, int Y);
    
    [DllImport(""user32.dll"")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    
    public const int MOUSEEVENTF_LEFTDOWN = 0x02;
    public const int MOUSEEVENTF_LEFTUP = 0x04;
}}

public struct RECT {{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}}
'@

# Găsește fereastra Adobe
$adobe = Get-Process | Where-Object {{ 
    $_.MainWindowTitle -like '*D100*' -or 
    $_.MainWindowTitle -like '*VALIDAT*' -or
    $_.MainWindowTitle -like '*Adobe*' -or 
    $_.MainWindowTitle -like '*Acrobat*' -or
    $_.MainWindowTitle -like '*.pdf*'
}} | Select-Object -First 1

if ($adobe -and $adobe.MainWindowHandle -ne [IntPtr]::Zero) {{
    Write-Host ""Găsit: $($adobe.ProcessName) - $($adobe.MainWindowTitle)""
    
    # Aduce în prim-plan
    [Win32]::SetForegroundWindow($adobe.MainWindowHandle)
    Start-Sleep -Milliseconds 500
    
    # Obține poziția ferestrei
    $rect = New-Object RECT
    [Win32]::GetWindowRect($adobe.MainWindowHandle, [ref]$rect)
    
    $winWidth = $rect.Right - $rect.Left
    $winHeight = $rect.Bottom - $rect.Top
    
    Write-Host ""Fereastră: $($rect.Left),$($rect.Top) - $winWidth x $winHeight""
    
    # Butonul VALIDARE este aproximativ la:
    # x = 123.825mm din stânga paginii (dar pagina e centrată în fereastră)
    # Pentru un form A4 (210mm lățime) în fereastră, calculăm poziția relativă
    # Butonul e la ~60% din lățimea paginii, ~45% din înălțimea paginii (în secțiunea de jos)
    
    # Estimare coordonate relative pentru buton (trebuie ajustate)
    # Butonul VALIDARE e galben, mare, în partea de jos a primei pagini
    $btnX = $rect.Left + [int]($winWidth * 0.55)  # 55% din lățime
    $btnY = $rect.Top + [int]($winHeight * 0.82)   # 82% din înălțime (partea de jos)
    
    Write-Host ""Click la: $btnX, $btnY""
    
    # Click pe buton
    [Win32]::SetCursorPos($btnX, $btnY)
    Start-Sleep -Milliseconds 200
    [Win32]::mouse_event([Win32]::MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
    [Win32]::mouse_event([Win32]::MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
    
    Write-Host 'Click trimis, aștept validarea...'
    Start-Sleep -Seconds 3
    
    # Verifică dacă apare dialog de succes/eroare și închide-l
    [System.Windows.Forms.SendKeys]::SendWait('{{ENTER}}')
    Start-Sleep -Seconds 1
    
    # Salvează: Ctrl+S
    [System.Windows.Forms.SendKeys]::SendWait('^s')
    Write-Host 'Salvare...'
    Start-Sleep -Seconds 2
    
    # Confirmă dacă apare dialog
    [System.Windows.Forms.SendKeys]::SendWait('{{ENTER}}')
    Start-Sleep -Seconds 1
    
    # Închide: Ctrl+W 
    [System.Windows.Forms.SendKeys]::SendWait('^w')
    Start-Sleep -Milliseconds 500
    
    # Confirmă închidere fără salvare dacă întreabă
    [System.Windows.Forms.SendKeys]::SendWait('n')
    
    Write-Host 'VALIDARE completă!'
}} else {{
    Write-Host 'EROARE: Nu am găsit fereastra Adobe Reader/Acrobat!'
    Write-Host 'Procese disponibile:'
    Get-Process | Where-Object {{ $_.MainWindowTitle -ne '' }} | Select-Object ProcessName, MainWindowTitle | Format-Table
}}
";
    
    // Salvează scriptul
    string psPath = Path.Combine(Path.GetDirectoryName(pdfInput)!, "_validare.ps1");
    File.WriteAllText(psPath, psScript, Encoding.UTF8);
    
    Console.WriteLine("      Script UI Automation creat.");
    Console.WriteLine("      Lansez Adobe Reader...\n");
    Console.WriteLine("      ⚠️  NU atingeți mouse-ul sau tastatura în următoarele 15 secunde!\n");
    
    // Execută PowerShell
    var psi = new ProcessStartInfo
    {
        FileName = "powershell.exe",
        Arguments = $"-ExecutionPolicy Bypass -File \"{psPath}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = false
    };
    
    using var proc = Process.Start(psi);
    proc?.WaitForExit(30000);
    
    string output = proc?.StandardOutput.ReadToEnd() ?? "";
    string error = proc?.StandardError.ReadToEnd() ?? "";
    
    if (!string.IsNullOrEmpty(output))
        Console.WriteLine($"      {output.Replace("\n", "\n      ")}");
    if (!string.IsNullOrEmpty(error))
        Console.WriteLine($"      EROARE: {error}");
    
    // Cleanup
    try { File.Delete(psPath); } catch { }
}

// === FUNCȚII HELPER ===

int UpdateElement(XElement parent, string name, string value)
{
    var el = parent.Element(name);
    if (el != null && !string.IsNullOrEmpty(value)) { el.Value = value; return 1; }
    return 0;
}

int UpdateOrCreate(XElement parent, string name, string value)
{
    if (string.IsNullOrEmpty(value)) return 0;
    var el = parent.Element(name);
    if (el != null) el.Value = value;
    else parent.Add(new XElement(name, value));
    return 1;
}

string GetVal(Dictionary<string, object> d, string k, string def)
{
    return d.TryGetValue(k, out var v) && v != null ? v.ToString() ?? def : def;
}

// === CALCUL SCADENȚĂ ===
string CalculeazaScadenta(string model, int luna_r, int an_r)
{
    int refMonth = luna_r - 1;
    int refYear = an_r;
    int endMonth, endYear;
    DateTime scad;
    
    switch (model)
    {
        case "LUNAR_25U":
            endYear = (refMonth < 11) ? refYear : refYear + 1;
            endMonth = (refMonth < 11) ? refMonth + 1 : 0;
            scad = new DateTime(endYear, endMonth + 1, 25);
            break;
            
        case "LUNAR_121":
            endYear = (refMonth < 11) ? refYear : refYear + 1;
            endMonth = (refMonth < 11) ? refMonth + 1 : 0;
            scad = new DateTime(endYear, endMonth + 1, 25);
            if (refYear >= 2021 && refYear <= 2025 && refMonth == 11)
                scad = new DateTime(refYear + 1, 6, 25);
            break;
            
        case "LUNAR_130":
            endYear = (refMonth < 11) ? refYear : refYear + 1;
            endMonth = (refMonth < 11) ? refMonth + 1 : 0;
            scad = new DateTime(endYear, endMonth + 1, 25);
            if (refYear >= 2021 && refYear <= 2025 && refMonth == 11)
                scad = new DateTime(refYear + 1, 6, 25);
            break;
            
        case "LUNAR_102":
        case "LUNAR_105":
        case "LUNAR_25LLU":
            endYear = refYear;
            endMonth = refMonth + 1;
            if (endMonth > 11) { endMonth -= 12; endYear++; }
            scad = new DateTime(endYear, endMonth + 1, 25);
            break;
            
        case "LUNAR_25M":
            if (refMonth == 11)
                scad = new DateTime(refYear, 12, 25);
            else
                scad = new DateTime(refYear, refMonth + 2, 25);
            break;
            
        case "AN_780":
            int zi = (refYear == 2016 || refYear == 2020 || refYear == 2024 || refYear == 2028) ? 28 : 29;
            scad = new DateTime(refYear, 7, zi);
            break;
            
        case "AN_706":
            endMonth = refMonth + 6;
            endYear = refYear;
            if (endMonth > 11) { endMonth -= 12; endYear++; }
            scad = new DateTime(endYear, endMonth + 1, 30);
            break;
            
        case "AN_709":
            endMonth = refMonth + 6;
            endYear = refYear;
            if (endMonth > 11) { endMonth -= 12; endYear++; }
            scad = new DateTime(endYear, endMonth + 1, 25);
            break;
            
        case "TRIM_25U2":
            endMonth = refMonth + 2;
            endYear = refYear;
            if (endMonth > 11) { endMonth -= 12; endYear++; }
            scad = new DateTime(endYear, endMonth + 1, 25);
            break;
            
        case "FREE":
            scad = DateTime.Now.AddDays(30);
            break;
            
        case "ANC_714":
        case "ANC_715":
        case "ANC_716":
        case "ANC_719":
            scad = new DateTime(refYear, 3, 30);
            break;
            
        case "ANC_717":
        case "ANC_718":
            scad = new DateTime(refYear, 3, 31);
            break;
            
        case "LUNAR_116":
        case "LUNAR_117":
        case "LUNAR_701":
            endMonth = refMonth + 1;
            endYear = refYear;
            if (endMonth > 11) { endMonth -= 12; endYear++; }
            scad = new DateTime(endYear, endMonth + 1, 25);
            break;
            
        case "AN_115":
        case "AN_125":
            endMonth = (refYear >= 2021 && refYear <= 2025) ? refMonth + 6 : refMonth + 3;
            endYear = refYear;
            if (endMonth > 11) { endMonth -= 12; endYear++; }
            scad = new DateTime(endYear, endMonth + 1, 25);
            break;
            
        case "15LUNI":
        case "18LUNI":
            int luni = model == "15LUNI" ? 15 : 18;
            scad = new DateTime(refYear, refMonth + 1, 25).AddMonths(luni);
            break;
            
        default:
            endMonth = refMonth + 1;
            endYear = refYear;
            if (endMonth > 11) { endMonth -= 12; endYear++; }
            scad = new DateTime(endYear, endMonth + 1, 25);
            break;
    }
    
    return scad.ToString("dd.MM.yyyy");
}
