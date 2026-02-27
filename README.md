# ANAF D100/D710 Form Filler

Aplicație C# pentru completarea automată a formularelor fiscale ANAF D100 (Declarație privind obligațiile de plată la bugetul de stat) și D710.

## Caracteristici

- ✅ Completează automat formularul XFA păstrând JavaScript-ul și validările
- ✅ Simulează butonul "Alege creanța fiscală" - calculează automat:
  - `COD_BUGETAR` - codul de încasare bugetară
  - `SCADENTA` - termenul de plată în funcție de tipul obligației
- ✅ Suportă 100+ tipuri de obligații fiscale
- ✅ Poate apăsa butonul VALIDARE prin UI Automation

## Cerințe

- .NET 8.0 SDK
- Adobe Acrobat Reader (pentru vizualizare și validare)
- Formularul `decl100.pdf` descărcat de pe [ANAF](https://www.anaf.ro/anaf/internet/ANAF/servicii_online/declaratii_electronice/descarcare_declaratii)

## Instalare

```bash
git clone https://github.com/YOUR_USERNAME/anaf-d100-filler.git
cd anaf-d100-filler
dotnet restore
```

## Configurare

Editează fișierul `date_declaratie.json`:

```json
{
  "sub1_tipD": "1",              // 1=D100, 2=D710
  "sub1_luna_r": "3",            // Luna de raportare
  "sub1_an_r": "2026",           // Anul de raportare
  "subA_cif": "12345678",        // CUI/CIF
  "subA_DENUMIRE": "SC FIRMA SRL",
  "subA_ADRESA": "Str. Exemplu Nr. 1",
  "subA_text_telefon": "0212345678",
  "subA_text_email": "contact@firma.ro",
  "subB_COD_IMP": "103--Impozit pe profit...",  // Codul obligației
  "subB_SUMA01I": "2500",        // Suma datorată
  "SUMA1": "2500",               // Total sume
  "SUMA2": "2500",
  "SUMA0": "0",
  "sub2_Nume": "POPESCU",        // Declarant
  "sub2_Prenume": "ION",
  "sub2_Functia": "Administrator"
}
```

## Utilizare

1. Descarcă formularul `decl100.pdf` de pe site-ul ANAF
2. Editează `date_declaratie.json` cu datele tale
3. Rulează:

```bash
dotnet run
```

4. PDF-ul completat va fi salvat ca `D100_VALIDAT_*.pdf`

## Coduri Obligații Suportate

| Cod | Descriere | Scadență |
|-----|-----------|----------|
| 103 | Impozit pe profit | Luna următoare + 25 zile |
| 121 | Impozit microîntreprinderi | Luna următoare + 25 zile |
| 102 | Impozit pe venit | Luna următoare + 25 zile |
| 130 | Impozit dividende | Luna următoare + 25 zile |
| ... | [vezi codul sursă pentru lista completă] | |

## Tehnologii

- **iText7** - manipulare PDF/XFA
- **UI Automation** - pentru apăsarea butonului VALIDARE
- **.NET 8.0**

## Limitări

- Formularul original `decl100.pdf` nu este inclus (descărcați de pe ANAF)
- Butonul VALIDARE necesită Adobe Reader deschis și interacțiune UI

## Licență

MIT License

## Disclaimer

Acest proiect este doar pentru uz educațional. Verificați întotdeauna declarațiile generate înainte de depunere. Autorul nu își asumă responsabilitatea pentru erori în completarea formularelor fiscale.
