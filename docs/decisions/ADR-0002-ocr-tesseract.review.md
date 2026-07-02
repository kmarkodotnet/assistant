# Review — ADR-0002 (Tesseract OCR)

> Review dátuma: 2026-07-02 · Reviewer: Claude (dokumentum-audit)

## Összegzés

Rendben lévő ADR: az alternatívák (PaddleOCR, EasyOCR, cloud-OCR privacy
okból kizárva) felsorolva, a kétlépcsős stratégia (PdfPig text layer →
Tesseract fallback) az ai-pipeline.md 3.2-vel konzisztens, a kézírás
non-goal a product-vision #12-vel egyezik.

## Észrevételek

1. **NuGet-csomag kettőssége** — „a `TesseractOCR` vagy `Tesseract`
   NuGet csomaggal” — a kettő különböző wrapper, eltérő karbantartottsággal;
   az implementáció megkezdése előtt egy sorban rögzítendő, melyik lett
   (a kódban ellenőrizhető, és az ADR frissítendő rá).
2. **Pontossági küszöb hiányzik** — az ai-pipeline.md 3.2 konkrét
   heurisztikát ad (min. 100 char + 80% nyomtatható a text-layer
   elfogadásához); az ADR „üres vagy túl rövid” megfogalmazása lazább.
   Elég egy hivatkozás a pipeline-doksira.
3. A ~200 MB image-növekedés reális becslés; a `--oem 3 --psm 6`
   paraméterezés (ai-pipeline.md) ide nem kell — jó, hogy nem duplikálja.

## Verdikt

Elfogadva státusz indokolt, változtatás nem szükséges a döntés szintjén.
