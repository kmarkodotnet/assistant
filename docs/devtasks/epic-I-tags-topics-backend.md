# Epic I — Tag + Topic — Backend dev taskok

> **Felolvasott tervezési dokumentumok (mátrix szerint):**
> - `coding-standards.md` (FULL — baseline)
> - `domain-model.md` §1.7 (Tag), §1.8 (Topic)
> - `database-schema.md` §4.9 (tag), §4.10 (topic), §4.11 (join), §5.1 (seed topic-fa)
> - `api-design.md` §9 (Tags), §10 (Topics)
> - `ai-pipeline.md` §3.4 (osztályozó — AI Tag-et hozhat, Topic-ot NEM)
> - `search-strategy.md` §2.1 (filter by tag/topic)
>
> **Story-k:** I1, I2
> **Fázis:** Fázis 11

---

## Áttekintés

Tag (lapos, free-form) és Topic (hierarchikus, admin-kurált) entitások.
Az AI **újat hozhat létre Tag-ből**, **NEM Topic-ból** — a Topic-fa
kontroll alatt marad.

## Taskok

### T-IBE-01 — `Tag` entity + CRUD
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Tag.cs` (már létezhet az Epic D-ből;
    finomítás).
  - `src/FamilyOs.Application/Tags/*.cs` (Create/List/Get/Patch/Delete).
  - `src/FamilyOs.Api/Endpoints/TagsModule.cs`.
- **AC:**
  - [ ] `?q=` autocomplete < 50 ms; `?sort=usageCount:desc`.
  - [ ] Lowercase normalization a nevekre + UNIQUE.
  - [ ] Soft delete, `?force=true` admin-only ha `usage_count > 0`.

### T-IBE-02 — Tag color + UI mező validáció
- **Fájlok:**
  - `src/FamilyOs.Application/Tags/CreateTag/CreateTagValidator.cs`
- **AC:**
  - [ ] Color hex format (`#rrggbb`).
  - [ ] Tag név regex CHECK (`database-schema.md` §4.9).

### T-IBE-03 — Tag usage_count maintenance
- **Cél:** denormalizált számláló, document_tag insert/delete-re módosul.
- **Fájlok:**
  - `src/FamilyOs.Application/Common/Behaviors/TagUsageCountBehavior.cs`
    vagy DB trigger.
- **AC:**
  - [ ] document_tag / note_tag insert → +1, delete → -1.
  - [ ] Eventual consistency: napi szintű recompute job opcionális.

### T-IBE-04 — `Topic` entity + CRUD
- **Fájlok:**
  - `src/FamilyOs.Domain/Entities/Topic.cs`
  - `src/FamilyOs.Application/Topics/*.cs`
  - `src/FamilyOs.Api/Endpoints/TopicsModule.cs`
- **AC:**
  - [ ] CRUD admin-only (`RequireAdmin`).
  - [ ] Tree-build endpoint: `?flat=false` → nested.
  - [ ] Slug validáció `^[a-z0-9-]+$`.

### T-IBE-05 — Topic mélység-korlát + körvédelem
- **Cél:** alkalmazás-szintű invariáns.
- **Fájlok:**
  - `src/FamilyOs.Domain/Services/TopicHierarchyValidator.cs`
- **AC:**
  - [ ] Max 3 szint mélység.
  - [ ] Önreferencia tilos (`parentTopicId != id`).
  - [ ] Kör-detekt: parent-lánc nem tartalmazhatja az aktuális topic-ot.

### T-IBE-06 — Topic delete + cascade
- **Cél:** topic törlés csak ha nincs gyermek vagy dokumentum-kapcsolat.
- **Fájlok:**
  - `src/FamilyOs.Application/Topics/DeleteTopicCommand.cs`.
- **AC:**
  - [ ] Ha gyermek topic vagy aktív kapcsolódás van → 409 magyar üzenet.
  - [ ] Soft delete a topic-on.

### T-IBE-07 — Topic seed bővítés
- **Cél:** ha új topic kerül a `database-schema.md` §5.1-ből, idempotensen
  betöltődik.
- **Fájlok:**
  - kiegészítés `DbSeedRunner`-ben.
- **AC:**
  - [ ] Új seed nem írja felül a manuálisan szerkesztett mezőket
        (`UPDATE` csak ha hiányzik).

### T-IBE-08 — Integration tesztek
- **Fájlok:**
  - `tests/FamilyOs.Api.IntegrationTests/Tags/TagAutocompleteTests.cs`
  - `tests/FamilyOs.Api.IntegrationTests/Topics/TopicHierarchyTests.cs`
- **AC:**
  - [ ] Tag autocomplete `?q=garan` → „garancia" találat.
  - [ ] Topic mélység > 3 → 422.
  - [ ] Topic kör-attempt → 422.

---

## Megvalósítási sorrend

```
T-IBE-01 → 02 → 03                  (Tag)
       → 04 → 05 → 06 → 07          (Topic)
       → 08                           (tesztek)
```

## Epic-DoD

- [ ] Tag CRUD + autocomplete működik gyorsan.
- [ ] Topic-fa admin-kezelhető a hierarchia-invariánsok betartásával.
- [ ] Seed nem törli a manuális Topic-módosításokat.
- [ ] Integration tesztek zöldek.
