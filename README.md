# ANAF D100/D710 PDF Filler - Azure Functions (Syncfusion)

Cloud-based API pentru completarea formularelor ANAF D100/D710.

## Cerințe

- .NET 8.0 SDK
- Azure Functions Core Tools v4 (pentru testare locală)
- Licență Syncfusion (sau community license)

## Configurare Licență Syncfusion

1. Obține licența de la https://www.syncfusion.com/account/claim-license-key
2. Adaugă în `local.settings.json`:
```json
{
  "Values": {
    "SyncfusionLicenseKey": "LICENTA_TA_AICI"
  }
}
```

## Testare Locală

```powershell
# Instalează Azure Functions Core Tools
npm install -g azure-functions-core-tools@4

# Rulează local
func start
```

API-ul va porni pe `http://localhost:7071`

## Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/health` | GET | Health check |
| `/api/fill-d100` | POST | Returnează PDF completat |
| `/api/fill-d100-base64` | POST | Returnează PDF ca Base64 în JSON |

## Request Format

`multipart/form-data` cu:
- `pdf` - fișierul PDF gol (decl100.pdf)
- `json` - datele în format text

## Deploy pe Azure

### 1. Crează Function App în Azure Portal

```powershell
# Login Azure
az login

# Crează resource group
az group create --name anaf-rg --location westeurope

# Crează storage account
az storage account create --name anafstorage --location westeurope --resource-group anaf-rg --sku Standard_LRS

# Crează Function App
az functionapp create --resource-group anaf-rg --consumption-plan-location westeurope --runtime dotnet-isolated --runtime-version 8 --functions-version 4 --name anaf-d100-filler --storage-account anafstorage
```

### 2. Configurează Syncfusion License Key

```powershell
az functionapp config appsettings set --name anaf-d100-filler --resource-group anaf-rg --settings "SyncfusionLicenseKey=LICENTA_TA"
```

### 3. Deploy

```powershell
# Din folderul proiectului
func azure functionapp publish anaf-d100-filler
```

### 4. URL Final

După deploy, URL-ul va fi:
```
https://anaf-d100-filler.azurewebsites.net/api/fill-d100-base64
```

## Power Apps Custom Connector

1. În Power Apps, mergi la **Data** → **Custom connectors** → **New custom connector**
2. **Host**: `anaf-d100-filler.azurewebsites.net`
3. **Base URL**: `/api`
4. Definește acțiunea:
   - **Name**: FillD100
   - **Request**:
     - URL: `/fill-d100-base64`
     - Method: POST
     - Content-Type: multipart/form-data
   - **Response**:
     ```json
     {
       "success": true,
       "fileName": "D100_xxx.pdf",
       "pdfBase64": "...",
       "xmlContent": "..."
     }
     ```

## Costuri Estimate (Azure Functions Consumption Plan)

- **Primul milion de execuții/lună**: GRATUIT
- **După**: ~$0.20 per milion de execuții
- **Memorie**: ~$0.000016 per GB-s

Pentru utilizare tipică (câteva sute de declarații/lună), costul va fi **$0/lună**.
