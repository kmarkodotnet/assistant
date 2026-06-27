# ADR-0002 — OCR motor: Tesseract

- Státusz: Elfogadva
- Dátum: 2026-06-26
- Döntéshozó: kmarko.net@gmail.com

## Kontextus
Feltöltött dokumentumok jelentős része szkennelt PDF vagy fénykép, amelyből
szöveget kell kinyerni az osztályozás, összefoglalás, entitás-kivonás és
keresés előtt. Lokális, ingyenes OCR motor kell, mert a privacy-first elv
szerint az adat nem hagyhatja el a háztartást.

Vizsgált alternatívák: Tesseract, PaddleOCR, EasyOCR, fizetős cloud OCR
(Google Vision, AWS Textract — privacy okból kizárva).

## Döntés
**Tesseract** OCR motor, **magyar (`hun`) + angol (`eng`)** nyelvi csomaggal.

## Indoklás
- Lokálisan futtatható, nyílt forráskódú, jól dokumentált.
- A digitális (text layer-rel rendelkező) PDF-eknél nincs is rá szükség —
  csak a kép-alapú dokumentumokhoz.
- Magyar nyelvi modellje érett.
- Docker image-be könnyen csomagolható (`apt-get install tesseract-ocr
  tesseract-ocr-hun tesseract-ocr-eng`), .NET-ből a `TesseractOCR` vagy
  `Tesseract` NuGet csomaggal hívható.
- A pontosság MVP-re elegendő. Speciális esetekre (kézírás, alacsony minőségű
  scan) a felhasználó manuálisan korrigálhatja a kinyert szöveget.

## Következmények
- `IDocumentTextExtractor` absztrakció a backenden, hogy később PaddleOCR-re
  vagy más motorra váltás ne legyen invazív.
- A pipeline két lépésben dolgozik: (1) digitális szöveg-réteg kinyerés
  (pl. `PdfPig`), (2) ha üres vagy túl rövid, akkor Tesseract fallback.
- A Docker image mérete nő (~200 MB) a nyelvi csomagokkal — vállalható.
- Kézírás-felismerés **nem cél** (lásd Product Vision non-goal #12).
