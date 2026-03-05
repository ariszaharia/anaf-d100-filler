using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Xfa;

namespace ANAF.AzureFunction;

public class FillD100Function
{
    private readonly ILogger _logger;

    public FillD100Function(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FillD100Function>();
    }

    [Function("HealthCheck")]
    public HttpResponseData HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString("{\"status\":\"running\",\"service\":\"ANAF D100/D710 PDF Filler - Azure Functions + Syncfusion\"}");
        return response;
    }

    [Function("FillD100")]
    public async Task<HttpResponseData> FillD100(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fill-d100")] HttpRequestData req)
    {
        try
        {
            var (pdfBytes, jsonText, error) = await ParseMultipartRequest(req);
            if (error != null)
            {
                return await CreateErrorResponse(req, error);
            }

            var date = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText!) ?? new();
            var result = AnafPdfFiller.FillPdf(pdfBytes!, date);

            if (result.Error != null)
            {
                return await CreateErrorResponse(req, result.Error);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");
            response.Headers.Add("Content-Disposition", $"attachment; filename=\"{result.FileName}\"");
            await response.Body.WriteAsync(result.PdfBytes!);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FillD100");
            return await CreateErrorResponse(req, ex.Message);
        }
    }

    [Function("FillD100Base64")]
    public async Task<HttpResponseData> FillD100Base64(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fill-d100-base64")] HttpRequestData req)
    {
        try
        {
            var (pdfBytes, jsonText, error) = await ParseMultipartRequest(req);
            if (error != null)
            {
                return await CreateErrorResponse(req, error);
            }

            var date = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText!) ?? new();
            var result = AnafPdfFiller.FillPdf(pdfBytes!, date);

            if (result.Error != null)
            {
                return await CreateErrorResponse(req, result.Error);
            }

            var responseObj = new
            {
                success = true,
                fileName = result.FileName,
                contentType = "application/pdf",
                pdfBase64 = Convert.ToBase64String(result.PdfBytes!),
                xmlContent = result.XmlContent
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(responseObj));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FillD100Base64");
            return await CreateErrorResponse(req, ex.Message);
        }
    }

    private async Task<(byte[]? PdfBytes, string? JsonText, string? Error)> ParseMultipartRequest(HttpRequestData req)
    {
        var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault() ?? "";
        
        if (!contentType.Contains("multipart/form-data"))
        {
            return (null, null, "Content-Type must be multipart/form-data");
        }

        var boundary = GetBoundary(contentType);
        if (string.IsNullOrEmpty(boundary))
        {
            return (null, null, "Could not parse multipart boundary");
        }

        using var ms = new MemoryStream();
        await req.Body.CopyToAsync(ms);
        var bodyBytes = ms.ToArray();

        var parts = ParseMultipartBody(bodyBytes, boundary);

        byte[]? pdfBytes = null;
        string? jsonText = null;

        foreach (var part in parts)
        {
            if (part.Name == "pdf" && part.Data != null)
            {
                pdfBytes = part.Data;
            }
            else if (part.Name == "json")
            {
                jsonText = part.Data != null ? Encoding.UTF8.GetString(part.Data) : part.Value;
            }
        }

        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            return (null, null, "PDF file is required (field name: 'pdf')");
        }

        if (string.IsNullOrEmpty(jsonText))
        {
            return (null, null, "JSON data is required (field name: 'json')");
        }

        return (pdfBytes, jsonText, null);
    }

    private string? GetBoundary(string contentType)
    {
        var elements = contentType.Split(';');
        foreach (var element in elements)
        {
            var trimmed = element.Trim();
            if (trimmed.StartsWith("boundary="))
            {
                return trimmed.Substring(9).Trim('"');
            }
        }
        return null;
    }

    private List<MultipartPart> ParseMultipartBody(byte[] bodyBytes, string boundary)
    {
        var parts = new List<MultipartPart>();
        var boundaryMarker = "--" + boundary;
        var endMarker = "--" + boundary + "--";
        
        // Find all boundary positions
        var positions = new List<int>();
        var boundaryBytesPattern = Encoding.UTF8.GetBytes(boundaryMarker);
        
        for (int i = 0; i <= bodyBytes.Length - boundaryBytesPattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < boundaryBytesPattern.Length; j++)
            {
                if (bodyBytes[i + j] != boundaryBytesPattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                positions.Add(i);
            }
        }

        for (int p = 0; p < positions.Count - 1; p++)
        {
            int start = positions[p] + boundaryBytesPattern.Length;
            int end = positions[p + 1];
            
            // Skip CRLF after boundary
            if (start + 2 <= bodyBytes.Length && bodyBytes[start] == '\r' && bodyBytes[start + 1] == '\n')
                start += 2;
            
            // Remove trailing CRLF before next boundary
            if (end >= 2 && bodyBytes[end - 1] == '\n' && bodyBytes[end - 2] == '\r')
                end -= 2;

            if (start >= end) continue;

            var partBytes = new byte[end - start];
            Array.Copy(bodyBytes, start, partBytes, 0, partBytes.Length);

            // Find header/body separator
            int headerEnd = -1;
            for (int i = 0; i < partBytes.Length - 3; i++)
            {
                if (partBytes[i] == '\r' && partBytes[i + 1] == '\n' && 
                    partBytes[i + 2] == '\r' && partBytes[i + 3] == '\n')
                {
                    headerEnd = i;
                    break;
                }
            }

            if (headerEnd < 0) continue;

            var headerText = Encoding.UTF8.GetString(partBytes, 0, headerEnd);
            var bodyStart = headerEnd + 4;
            var bodyLength = partBytes.Length - bodyStart;

            var part = new MultipartPart();

            // Parse name from Content-Disposition
            var nameMatch = System.Text.RegularExpressions.Regex.Match(headerText, @"name=""([^""]+)""");
            if (nameMatch.Success)
            {
                part.Name = nameMatch.Groups[1].Value;
            }

            var filenameMatch = System.Text.RegularExpressions.Regex.Match(headerText, @"filename=""([^""]+)""");
            if (filenameMatch.Success)
            {
                part.FileName = filenameMatch.Groups[1].Value;
            }

            if (bodyLength > 0)
            {
                part.Data = new byte[bodyLength];
                Array.Copy(partBytes, bodyStart, part.Data, 0, bodyLength);
                part.Value = Encoding.UTF8.GetString(part.Data);
            }

            parts.Add(part);
        }

        return parts;
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string error)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { success = false, error }));
        return response;
    }

    private class MultipartPart
    {
        public string? Name { get; set; }
        public string? FileName { get; set; }
        public string? Value { get; set; }
        public byte[]? Data { get; set; }
    }
}

// ============================================================
// ANAF PDF FILLER SERVICE - SYNCFUSION VERSION
// ============================================================
public static class AnafPdfFiller
{
    private static readonly Dictionary<string, (string CodBugetar, string Model)> Obligatii = new()
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

    public static (byte[]? PdfBytes, string? FileName, string? XmlContent, string? Error) FillPdf(byte[] pdfTemplateBytes, Dictionary<string, object> date)
    {
        if (pdfTemplateBytes == null || pdfTemplateBytes.Length == 0)
        {
            return (null, null, null, "PDF template bytes cannot be empty");
        }

        // Extrag datele comune
        int luna_r = int.Parse(GetVal(date, "sub1_luna_r", "1"));
        int an_r = int.Parse(GetVal(date, "sub1_an_r", "2026"));
        string tipD = GetVal(date, "sub1_tipD", "1");

        // Extrag obligatiile
        var listaObligatii = new List<(string CodImpFull, string CodImpNumeric, string CodBugetar, string Scadenta, string NrJustificativ, decimal Suma)>();

        if (date.TryGetValue("obligatii", out var obligatiiObj) && obligatiiObj is JsonElement jsonArr && jsonArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonArr.EnumerateArray())
            {
                string codImpFull = item.TryGetProperty("COD_IMP", out var ci) ? ci.GetString() ?? "" : "";
                string sumaStr = item.TryGetProperty("SUMA01I", out var si) ? si.GetString() ?? "0" : "0";
                decimal suma = decimal.TryParse(sumaStr, out var s) ? s : 0;

                string codImpNumeric = codImpFull.Split('-')[0].Trim();
                string codBugetar = "";
                string scadenta = "";
                string nrJustificativ = "";

                if (Obligatii.TryGetValue(codImpNumeric, out var ob))
                {
                    codBugetar = ob.CodBugetar;
                    scadenta = CalculeazaScadenta(ob.Model, luna_r, an_r);
                    nrJustificativ = GenereazaNrJustificativ(codImpNumeric, luna_r, an_r, scadenta);
                }

                listaObligatii.Add((codImpFull, codImpNumeric, codBugetar, scadenta, nrJustificativ, suma));
            }
        }
        else
        {
            // Fallback pt format vechi
            string codImpFull = GetVal(date, "subB_COD_IMP", "");
            string sumaStr = GetVal(date, "subB_SUMA01I", "0");
            decimal suma = decimal.TryParse(sumaStr, out var s) ? s : 0;
            string codImpNumeric = codImpFull.Split('-')[0].Trim();

            if (Obligatii.TryGetValue(codImpNumeric, out var ob))
            {
                string scadenta = CalculeazaScadenta(ob.Model, luna_r, an_r);
                string nrJustificativ = GenereazaNrJustificativ(codImpNumeric, luna_r, an_r, scadenta);
                listaObligatii.Add((codImpFull, codImpNumeric, ob.CodBugetar, scadenta, nrJustificativ, suma));
            }
        }

        if (listaObligatii.Count == 0)
        {
            return (null, null, null, "No obligations found in JSON");
        }

        decimal totalPlata = listaObligatii.Sum(o => o.Suma);

        // Generez XML
        string xmlD100 = GenereazaXmlD100Multi(date, listaObligatii, luna_r, an_r, tipD, totalPlata);
        string xmlFileName = tipD == "1" ? "D100.xml" : "D710.xml";
        string pdfFileName = tipD == "1" ? $"D100_{DateTime.Now:yyyyMMdd_HHmmss}.pdf" : $"D710_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

        try
        {
            using var inputMs = new MemoryStream(pdfTemplateBytes);
            
            // Încărcare PDF XFA cu Syncfusion
            PdfLoadedXfaDocument loadedXfaDocument = new PdfLoadedXfaDocument(inputMs);
            PdfLoadedXfaForm xfaForm = loadedXfaDocument.XfaForm;
            
            // Creăm dicționarul cu valorile de completat - structură XFA
            var fieldValues = new Dictionary<string, string>
            {
                // sub1 - date declarație
                { "form1.sub1.tipD", tipD },
                { "form1.sub1.luna_r", luna_r.ToString() },
                { "form1.sub1.an_r", an_r.ToString() },
                { "form1.sub1.d_anulare", "0" },
                { "form1.sub1.d_succ", "0" },
                { "form1.sub1.d_dizolv", "0" },
                { "form1.sub1.d_modif", "0" },
                { "form1.sub1.universalCode", tipD == "1" ? "D100_A300" : "D710_A300" },
                
                // subA - date firmă
                { "form1.subA.cif", GetVal(date, "subA_cif", "") },
                { "form1.subA.DENUMIRE", GetVal(date, "subA_DENUMIRE", "") },
                { "form1.subA.ADRESA", GetVal(date, "subA_ADRESA", "") },
                { "form1.subA.text_telefon", GetVal(date, "subA_text_telefon", "") },
                { "form1.subA.text_email", GetVal(date, "subA_text_email", "") },
                
                // ident_IMP
                { "form1.ident_IMP.DENUMIRE", GetVal(date, "subA_DENUMIRE", "") },
                { "form1.ident_IMP.cifR", GetVal(date, "subA_cif", "") },
                
                // sub2 - semnătar
                { "form1.sub2.Nume", GetVal(date, "sub2_Nume", "") },
                { "form1.sub2.Prenume", GetVal(date, "sub2_Prenume", "") },
                { "form1.sub2.Functia", GetVal(date, "sub2_Functia", "") },
                
                // Totals
                { "form1.SUMA1", totalPlata.ToString("0") },
                { "form1.SUMA2", "0" },
                { "form1.SUMA0", "0" },
            };
            
            // subB - prima obligație
            var firstOb = listaObligatii[0];
            fieldValues["form1.subB.COD_IMP"] = firstOb.CodImpFull;
            fieldValues["form1.subB.COD_BUGETAR"] = firstOb.CodBugetar;
            fieldValues["form1.subB.SCADENTA"] = firstOb.Scadenta;
            fieldValues["form1.subB.SCADENTA2"] = firstOb.Scadenta;
            fieldValues["form1.subB.NR_JUSTIFICATIV"] = firstOb.NrJustificativ;
            fieldValues["form1.subB.SUMA01I"] = firstOb.Suma.ToString("0");
            fieldValues["form1.subB.SUMA02I"] = "0";
            fieldValues["form1.subB.SUMA03I"] = firstOb.Suma.ToString("0");
            fieldValues["form1.subB.SUMA04I"] = "0";

            // Export XFA data, modify it, and import back
            using var xfaDataStream = new MemoryStream();
            xfaForm.ExportXfaData(xfaDataStream);
            xfaDataStream.Position = 0;
            
            XmlDocument xfaXml = new XmlDocument();
            xfaXml.Load(xfaDataStream);
            
            // Completăm valorile în XML-ul XFA
            FillXfaXmlFields(xfaXml, fieldValues);
            
            // Import XFA data back
            using var modifiedXfaStream = new MemoryStream();
            xfaXml.Save(modifiedXfaStream);
            modifiedXfaStream.Position = 0;
            xfaForm.ImportXfaData(modifiedXfaStream);

            // Adăugăm XML ca attachment
            byte[] xmlBytes = Encoding.UTF8.GetBytes(xmlD100);
            
            // Salvăm documentul XFA
            using var outputMs = new MemoryStream();
            loadedXfaDocument.Save(outputMs);
            loadedXfaDocument.Close();

            // Re-deschid documentul pentru a adăuga attachment-ul
            outputMs.Position = 0;
            using var finalMs = new MemoryStream();
            using (var finalDoc = new PdfLoadedDocument(outputMs))
            {
                PdfAttachment attachment = new PdfAttachment(xmlFileName, xmlBytes);
                attachment.MimeType = "application/xml";
                finalDoc.Attachments.Add(attachment);
                finalDoc.Save(finalMs);
            }

            return (finalMs.ToArray(), pdfFileName, xmlD100, null);
        }
        catch (Exception ex)
        {
            return (null, null, null, $"Syncfusion XFA Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void FillXfaXmlFields(XmlDocument xfaXml, Dictionary<string, string> fieldValues)
    {
        // Parcurgem toate câmpurile și le setăm valorile
        foreach (var kvp in fieldValues)
        {
            if (string.IsNullOrEmpty(kvp.Value)) continue;
            
            string fieldPath = kvp.Key;
            string value = kvp.Value;
            
            // Căutăm elementul în XML folosind calea (ex: form1.subA.cif)
            var parts = fieldPath.Split('.');
            SetXfaFieldValue(xfaXml.DocumentElement, parts, 0, value);
        }
    }
    
    private static void SetXfaFieldValue(XmlNode? parentNode, string[] pathParts, int index, string value)
    {
        if (parentNode == null || index >= pathParts.Length) return;
        
        string currentPart = pathParts[index];
        
        // Căutăm elementul copil cu acest nume
        foreach (XmlNode child in parentNode.ChildNodes)
        {
            if (child.LocalName == currentPart || child.Name == currentPart)
            {
                if (index == pathParts.Length - 1)
                {
                    // Am ajuns la ultimul element - setăm valoarea
                    child.InnerText = value;
                    return;
                }
                else
                {
                    // Continuăm să căutăm în adâncime
                    SetXfaFieldValue(child, pathParts, index + 1, value);
                    return;
                }
            }
        }
        
        // Dacă nu am găsit, căutăm în toți copiii recursiv
        foreach (XmlNode child in parentNode.ChildNodes)
        {
            if (child.HasChildNodes)
            {
                SetXfaFieldValue(child, pathParts, index, value);
            }
        }
    }

    private static string GetVal(Dictionary<string, object> d, string k, string def)
    {
        return d.TryGetValue(k, out var v) && v != null ? v.ToString() ?? def : def;
    }

    private static string EscapeXml(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\"", "&quot;").Replace("'", "&apos;");
    }

    private static string GenereazaXmlD100Multi(Dictionary<string, object> date, List<(string CodImpFull, string CodImpNumeric, string CodBugetar, string Scadenta, string NrJustificativ, decimal Suma)> listaOb, int luna, int an, string tipD, decimal totalPlata)
    {
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

        sb.Append($" totalPlata_A=\"{totalPlata:0}\"");

        sb.Append($" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        sb.Append($" xsi:schemaLocation=\"{xmlns} {xsd}\"");
        sb.Append($" xmlns=\"{xmlns}\">");
        sb.AppendLine();

        foreach (var ob in listaOb)
        {
            sb.Append("\t<obligatie");
            sb.Append($" cod_oblig=\"{ob.CodImpNumeric}\"");
            sb.Append($" cod_bugetar=\"{ob.CodBugetar}\"");
            sb.Append($" scadenta=\"{ob.Scadenta}\"");
            if (!string.IsNullOrEmpty(ob.NrJustificativ))
                sb.Append($" nr_evid=\"{ob.NrJustificativ}\"");

            string sumaStr = ob.Suma.ToString("0");
            if (tipD == "1")
            {
                sb.Append($" suma_dat=\"{sumaStr}\"");
                sb.Append($" suma_ded=\"0\"");
                sb.Append($" suma_plata=\"{sumaStr}\"");
                sb.Append($" suma_rest=\"0\"");
            }
            else
            {
                sb.Append($" suma_dat_I=\"{sumaStr}\"");
                sb.Append($" suma_ded_I=\"0\"");
                sb.Append($" suma_plata_I=\"{sumaStr}\"");
                sb.Append($" suma_rest_I=\"0\"");
            }
            sb.AppendLine(" />");
        }

        sb.AppendLine($"</{tagRoot}>");
        return sb.ToString();
    }

    private static string CalculeazaScadenta(string model, int luna_r, int an_r)
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

    private static string GenereazaNrJustificativ(string codOblig, int luna, int an, string scadenta)
    {
        if (string.IsNullOrEmpty(scadenta) || string.IsNullOrEmpty(codOblig)) return "";

        var parts = scadenta.Split('.');
        if (parts.Length != 3) return "";

        string scadZi = parts[0].PadLeft(2, '0');
        string scadLuna = parts[1].PadLeft(2, '0');
        string scadAn = parts[2].Substring(2, 2);

        string lunaRef = luna.ToString().PadLeft(2, '0');
        string anRef = an.ToString().Substring(2, 2);
        string cod3 = codOblig.PadLeft(3, '0').Substring(0, 3);

        string nrev = $"10{cod3}01{lunaRef}{anRef}{scadZi}{scadLuna}{scadAn}0000";

        int suma = 0;
        foreach (char c in nrev)
            if (char.IsDigit(c)) suma += c - '0';

        string cifraControl = (suma % 100).ToString().PadLeft(2, '0');
        return nrev + cifraControl;
    }
}
