using System.Text.Json;
using System.Xml.Linq;
using System.Text;
using System.IO;
using iText.Kernel.Pdf;
using iText.Forms;
using iText.Forms.Xfa;
using iText.Kernel.Pdf.Filespec;


// inputuri + output

string fisierPdf = Path.GetFullPath("decl100.pdf");
string fisierJson = "date_declaratie.json";
string fisierOutput = Path.GetFullPath($"D100_COMPLETAT_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");


// simulare buton creanta fiscala (toate variantele posbile)
// cod fiscal - cod bugetar - model scandenta(formula calcul scadenta)

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



//preiau datele din Json sau creez un set de date default 
//daca nu exista

Dictionary<string, object> date;

if (File.Exists(fisierJson))
{
    Console.WriteLine($"[1/5] Citesc datele din {fisierJson}...");
    var jsonText = File.ReadAllText(fisierJson);
    date = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText) ?? new();
}

//date default pt testare rapida
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
}



// extrag datele din json (cod impozit, luna, an, tip declaratie)

string codImpFull = GetVal(date, "subB_COD_IMP", "");
int luna_r = int.Parse(GetVal(date, "sub1_luna_r", "1")); 
int an_r = int.Parse(GetVal(date, "sub1_an_r", "2026"));
string tipD = GetVal(date, "sub1_tipD", "1");

string codImpNumeric = codImpFull.Split('-')[0].Trim();



string codBugetar = "";
string scadenta = "";
string model = "";


// simulare buton creanta fiscala
if (obligatii.TryGetValue(codImpNumeric, out var ob))
{
    codBugetar = ob.CodBugetar;
    model = ob.Model;

    //calc scadenta in functie de model, luna_r, an_r
    scadenta = CalculeazaScadenta(model, luna_r, an_r);
    
    Console.WriteLine($"      COD_BUGETAR: {codBugetar}");
    Console.WriteLine($"      SCADENTA: {scadenta}");
}
//generez XML cu datele din json

string xmlD100 = GenereazaXmlD100(date, codImpNumeric, codBugetar, scadenta, luna_r, an_r, tipD);
string xmlFileName = tipD == "1" ? "D100.xml" : "D710.xml";

//salvare XML local pt debug ('xmlD100')
string xmlPath = Path.Combine(Path.GetDirectoryName(fisierOutput)!, xmlFileName);
File.WriteAllText(xmlPath, xmlD100, Encoding.UTF8);

try
{
    //deschid PDF, actulizez datele in XFA, atasez XML si salvez ca PDF nou
    
    using var reader = new PdfReader(fisierPdf);
    using var writer = new PdfWriter(fisierOutput);
    
    var stampProps = new StampingProperties();
    stampProps.UseAppendMode(); //!!!!!!!!!fortez pastrarea structurii initiale(evita rescriere completa)
    //!!!!!!!!!!!!!!!!!!!!!!!!!
    
    using var pdfDoc = new PdfDocument(reader, writer, stampProps); //sursa , destinatie, reguli scriere
    
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
        string universalCode = tipD == "1" ? "D100_A300" : "D710_A300";
        updated += UpdateOrCreate(sub1, "universalCode", universalCode);
    }
    
    var subA = form1.Element("subA");
    if (subA != null)
    {
        updated += UpdateElement(subA, "cif", GetVal(date, "subA_cif", ""));
        updated += UpdateElement(subA, "DENUMIRE", GetVal(date, "subA_DENUMIRE", ""));
        updated += UpdateElement(subA, "ADRESA", GetVal(date, "subA_ADRESA", ""));
        updated += UpdateElement(subA, "text_telefon", GetVal(date, "subA_text_telefon", ""));
        updated += UpdateElement(subA, "text_email", GetVal(date, "subA_text_email", ""));
    }
    
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
    
    updated += UpdateElement(form1, "SUMA1", GetVal(date, "SUMA1", ""));
    updated += UpdateElement(form1, "SUMA2", GetVal(date, "SUMA2", ""));
    updated += UpdateElement(form1, "SUMA0", GetVal(date, "SUMA0", ""));
    
    var identImp = form1.Element("ident_IMP");
    if (identImp != null)
    {
        updated += UpdateElement(identImp, "DENUMIRE", GetVal(date, "subA_DENUMIRE", ""));
        updated += UpdateElement(identImp, "cifR", GetVal(date, "subA_cif", ""));
    }
    
    var sub2 = form1.Element("sub2");
    if (sub2 != null)
    {
        updated += UpdateElement(sub2, "Nume", GetVal(date, "sub2_Nume", ""));
        updated += UpdateElement(sub2, "Prenume", GetVal(date, "sub2_Prenume", ""));
        updated += UpdateElement(sub2, "Functia", GetVal(date, "sub2_Functia", ""));
    }
    
// adauga xml ca attachment in PDF (daca exista deja, se va suprascrie la salvare)    
    byte[] xmlBytes = Encoding.UTF8.GetBytes(xmlD100);
    var fileSpec = PdfFileSpec.CreateEmbeddedFileSpec(
        pdfDoc, 
        xmlBytes, 
        xmlFileName,   
        xmlFileName, 
        new PdfName("application/xml"), 
        null, 
        null);



    
    pdfDoc.AddFileAttachment(xmlFileName, fileSpec);
    Console.WriteLine($"      Atașat: {xmlFileName}");
    

    //scriu inapoi in PDF
    xfa.SetDomDocument(xDoc);
    xfa.Write(pdfDoc);
    pdfDoc.Close();
    
  
}
catch (Exception ex)
{
    Console.WriteLine($"\nEROARE: {ex.Message}\n{ex.StackTrace}");
}

//modifica un element 

int UpdateElement(XElement parent, string name, string value)
{
    var el = parent.Element(name);
    if (el != null && !string.IsNullOrEmpty(value)) { el.Value = value; return 1; }
    return 0;
}


//creeaza elementul daca nu exista

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

string EscapeXml(string s)
{
    if (string.IsNullOrEmpty(s)) return "";
    return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;").Replace("'", "&apos;");
}

string GenereazaXmlD100(Dictionary<string, object> date, string codOblig, string codBugetar, string scadenta, int luna, int an, string tipD)
{
    // construiesc XML manual
    var sb = new StringBuilder();
    
    string tagRoot = tipD == "1" ? "declaratie100" : "declaratie710";
    string xmlns = tipD == "1" 
        ? "mfp:anaf:dgti:d100:declaratie:v2" 
        : "mfp:anaf:dgti:d710:declaratie:v2";
    string xsd = tipD == "1" ? "D100.xsd" : "D710.xsd";
    
    sb.AppendLine("<?xml version=\"1.0\"?>");
    sb.Append($"<{tagRoot}");
    sb.Append($" luna=\"{luna:D2}\"");
    sb.Append($" an=\"{an}\"");
    sb.Append($" d_anulare=\"0\"");
    sb.Append($" d_succ=\"0\"");
    sb.Append($" d_dizolv=\"0\"");
    sb.Append($" d_modif=\"0\"");
    
    sb.Append($" nume_declar=\"{EscapeXml(GetVal(date, "sub2_Nume", ""))}\"");
    sb.Append($" prenume_declar=\"{EscapeXml(GetVal(date, "sub2_Prenume", ""))}\"");
    sb.Append($" functie_declar=\"{EscapeXml(GetVal(date, "sub2_Functia", ""))}\"");
    
    sb.Append($" cui=\"{EscapeXml(GetVal(date, "subA_cif", ""))}\"");
    sb.Append($" den=\"{EscapeXml(GetVal(date, "subA_DENUMIRE", ""))}\"");
    sb.Append($" adresa=\"{EscapeXml(GetVal(date, "subA_ADRESA", ""))}\"");
    sb.Append($" telefon=\"{EscapeXml(GetVal(date, "subA_text_telefon", ""))}\"");
    sb.Append($" mail=\"{EscapeXml(GetVal(date, "subA_text_email", ""))}\"");
    
    sb.Append($" totalPlata_A=\"{GetVal(date, "SUMA1", "0")}\"");
    
    sb.Append($" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
    sb.Append($" xsi:schemaLocation=\"{xmlns} {xsd}\"");
    sb.Append($" xmlns=\"{xmlns}\">");
    sb.AppendLine();
    
    string suma = GetVal(date, "subB_SUMA01I", "0");
    sb.Append("\t<obligatie");
    sb.Append($" cod_oblig=\"{codOblig}\"");
    sb.Append($" cod_bugetar=\"{codBugetar}\"");
    sb.Append($" scadenta=\"{scadenta}\"");
    
    if (tipD == "1")
    {
        sb.Append($" suma_dat=\"{suma}\"");
        sb.Append($" suma_ded=\"0\"");
        sb.Append($" suma_plata=\"{suma}\"");
        sb.Append($" suma_rest=\"0\"");
    }
    else
    {
        sb.Append($" suma_dat_I=\"{suma}\"");
        sb.Append($" suma_ded_I=\"0\"");
        sb.Append($" suma_plata_I=\"{suma}\"");
        sb.Append($" suma_rest_I=\"0\"");
    }
    
    sb.AppendLine(" />");
    
    sb.AppendLine($"</{tagRoot}>");
    
    return sb.ToString();
}


//calculez scadenta in functie de modelul obligatiei fiscale, luna si anul de referinta (luna_r, an_r)
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
