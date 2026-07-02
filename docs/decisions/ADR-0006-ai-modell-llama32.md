# ADR-0006 — Alapértelmezett lokális AI-modell: llama3.2:3b (MVP)

- Státusz: Elfogadva (a megvalósult kód rögzítése)
- Dátum: 2026-07-02
- Döntéshozó: kmarko.net@gmail.com (dokumentum-audit nyomán rögzítve)

## Kontextus

A tervező dokumentumok (product-vision, architecture, ai-pipeline,
implementation-plan) végig `gpt-oss:20b`-t rögzítettek alapértelmezett
lokális modellként. A megvalósítás (`OllamaOptions.DefaultModel`) viszont
`llama3.2:3b`-re váltott. A 20b modell ~13–16 GB RAM-ot igényel, ami az
átlagos otthoni PC-n (és a Raspberry Pi célhardveren) nem áll
rendelkezésre; a 3b modell ~3–4 GB-ból elfut.

## Döntés

- **MVP default: `llama3.2:3b`** (Ollama), embedding: `nomic-embed-text`
  (768 dim, változatlan).
- A modell **konfigurálható** (`Ai:Providers:ollama:Model` /
  `Ollama__DefaultModel` env): erős hardveren (16+ GB RAM, GPU) a
  `gpt-oss:20b` ajánlott a jobb magyar minőségért.
- A golden-sample regressziós készletet (ai-pipeline.md 9.1) a mindenkori
  default modellel kell futtatni; modellváltásnál kötelező újrafuttatás.

## Indoklás

- A 4–8 GB RAM-os célhardver (DELIVERY.md előfeltételek, Raspberry Pi
  opció) a 20b modellel nem működne — a telepítési élmény megtörne.
- A 3b modell magyar összefoglaló/kinyerés minősége gyengébb, de a
  strukturált JSON-feladatokra (classify, deadline-extract) elfogadható;
  a Q&A minőségi célt (8/10) erős hardveren a 20b hozza.
- A provider-absztrakció miatt a váltás konfiguráció, nem kód.

## Következmények

- `product-vision.md`, `architecture.md` 5.2, `ai-pipeline.md` 8.,
  `implementation-plan.md`, `DELIVERY.md`, `deploy-raspberry-pi.md`
  egységesen: default `llama3.2:3b`, opció `gpt-oss:20b`.
- A `scripts/pull-models.sh` / DELIVERY-útmutató a default modellt húzza;
  a 20b csak explicit felhasználói döntésre.
- A sikermetrika (Q&A 8/10) kiértékelésekor jelezni kell, melyik
  modellel mérünk; a 3b-vel mért gyengébb eredmény nem bukás, hanem a
  hardver-trade-off dokumentált következménye.
