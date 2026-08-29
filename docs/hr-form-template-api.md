# HR form-template storage and frontend contract

## Why the model has two tables

`app.HrFormTemplates` stores the stable identity and searchable metadata for a form. `app.HrFormTemplateVersions` stores immutable JSON layout definitions. The template points separately to its current draft and current published version.

This prevents a later designer edit from changing a form that HR already printed or issued. A designer saves a new version, previews it, then publishes that exact version. Consumers should locate a template by its stable `code`, not by its editable Arabic or English name.

Employee values are **not** stored in the template definition. The definition stores field bindings and placeholders such as `employee.fullNameAr`; the changing values remain in the form-generation request or, when issued forms are persisted, in a separate generated-document record that references the exact template-version ID.

## Persisted properties

### `app.HrFormTemplates`

| Property | Purpose |
| --- | --- |
| `Id` | UUIDv7 database identity. |
| `Code` | Stable unique frontend/API key, for example `CASH_ADVANCE_ACK`. |
| `NameAr`, `NameEn` | Editable display names. |
| `Category` | Filter/group value such as `finance`, `employment`, or `custody`. |
| `DescriptionAr`, `DescriptionEn` | Optional catalog help text. |
| `IsActive` | Controls whether users may select the template for new forms. |
| `CurrentDraftVersionId` | Latest saved designer version. |
| `CurrentPublishedVersionId` | Exact version available for operational use. |
| Audit/soft-delete/`RowVersion` fields | Creator, timestamps, archive trail, and optimistic concurrency. |

### `app.HrFormTemplateVersions`

| Property | Purpose |
| --- | --- |
| `HrFormTemplateId`, `VersionNumber` | Parent and sequential version identity. |
| `DefinitionSchemaVersion` | Contract version understood by the frontend/renderer. Currently `1`. |
| `DefinitionJson` | Complete page, header, body, footer, fields, blocks, and styles. |
| `DefinitionSha256` | Integrity and equality check for the normalized JSON. |
| `ChangeNote` | Optional designer explanation (maximum 500 characters). |
| `CreatedByUserId`, `CreatedAtUtc` | Immutable author and creation time. |

## Definition JSON version 1

The API validates the stable envelope and leaves block payloads extensible for future designer controls.

```json
{
  "schemaVersion": 1,
  "locale": "ar-SA",
  "direction": "rtl",
  "page": {
    "size": "A4",
    "orientation": "portrait",
    "widthMm": 210,
    "heightMm": 297,
    "marginsMm": { "top": 25.4, "right": 31.75, "bottom": 25.4, "left": 31.75 },
    "headerDistanceMm": 12.7,
    "footerDistanceMm": 12.7
  },
  "theme": {
    "fontFamily": "Noto Naskh Arabic",
    "fontSizePt": 12,
    "lineHeight": 1.5,
    "textColor": "#111827",
    "accentColor": "#0F5FC2"
  },
  "fields": [
    {
      "key": "employee.fullNameAr",
      "type": "text",
      "source": "employee",
      "labelAr": "اسم الموظف",
      "path": "fullNameAr",
      "required": true
    },
    {
      "key": "advance.amount",
      "type": "money",
      "source": "manual",
      "labelAr": "مبلغ السلفة",
      "currency": "SAR",
      "required": true,
      "validation": { "minimum": 0.01 }
    },
    {
      "key": "document.date",
      "type": "date",
      "source": "system",
      "labelAr": "التاريخ",
      "format": "yyyy/MM/dd"
    }
  ],
  "sections": {
    "header": {
      "repeat": true,
      "heightMm": 18,
      "blocks": [
        { "id": "company", "type": "binding", "fieldKey": "company.nameAr", "align": "start" },
        { "id": "department", "type": "text", "text": "إدارة الموارد البشرية", "align": "end" }
      ]
    },
    "body": {
      "blocks": [
        { "id": "title", "type": "text", "text": "إقرار سلفة نقدية", "style": { "fontSizePt": 18, "bold": true, "underline": true, "align": "center" } },
        { "id": "date", "type": "field", "fieldKey": "document.date" },
        { "id": "legal-copy", "type": "richText", "content": [] },
        { "id": "signatures", "type": "signatureGrid", "columns": 3, "items": ["employee", "finance", "hr"] },
        { "id": "fingerprint", "type": "fingerprint", "labelAr": "البصمة" }
      ]
    },
    "footer": {
      "repeat": true,
      "heightMm": 10,
      "blocks": [
        { "id": "page-number", "type": "pageNumber", "format": "{page} / {pages}", "align": "center" }
      ]
    }
  }
}
```

Required envelope rules:

- `schemaVersion` must be `1`.
- `direction` must be `rtl` or `ltr`.
- `page.size` supports `A4`, `A5`, `Letter`, or `Custom`; a custom page also requires dimensions between 50 and 1000 mm.
- `page.orientation` must be `portrait` or `landscape`.
- All four margins are required and must be between 0 and 100 mm.
- `sections.body` is required. Header and footer are optional.
- `fields` is optional, but when present it may contain at most 250 unique keys. Every field requires `key`, `type`, and `source`.
- The complete UTF-8 definition may not exceed 512 KB.

Recommended field `type` values are `text`, `multiline`, `number`, `money`, `date`, `checkbox`, `select`, `employee`, `signature`, and `fingerprint`. Recommended `source` values are `employee`, `company`, `manual`, `system`, and `computed`. They are intentionally not database enums so new designer controls can be introduced without a migration.

Recommended reusable block properties are `id`, `type`, `fieldKey`, `text`/`content`, `xMm`, `yMm`, `widthMm`, `heightMm`, `paddingMm`, `marginMm`, `border`, `background`, `style`, `visibilityRule`, `pageBreakBefore`, and `keepTogether`.

## API lifecycle

All endpoints require authentication and the shown permission.

| Method and route | Permission | Use |
| --- | --- | --- |
| `GET /api/hr-form-templates` | `hr_forms.templates.read` | Search/filter the catalog. |
| `GET /api/hr-form-templates/{id}` | `hr_forms.templates.read` | Load metadata plus current draft and published definitions. |
| `GET /api/hr-form-templates/by-code/{code}` | `hr_forms.templates.read` | Resolve a stable template key. |
| `POST /api/hr-form-templates` | `hr_forms.templates.manage` | Create metadata and immutable version 1 as the draft. |
| `PUT /api/hr-form-templates/{id}` | `hr_forms.templates.manage` | Update metadata/active state using `rowVersion`. The stable code does not change. |
| `POST /api/hr-form-templates/{id}/versions` | `hr_forms.templates.manage` | Save a new immutable designer version and make it the draft. |
| `GET /api/hr-form-templates/{id}/versions` | `hr_forms.templates.read` | Show version history. |
| `POST /api/hr-form-templates/{id}/versions/{versionId}/publish` | `hr_forms.templates.manage` | Point operational use to the selected exact version. |
| `PATCH /api/hr-form-templates/{id}/archive` | `hr_forms.templates.manage` | Soft-archive the template with a reason. |

The create-version, metadata-update, publish, and archive operations require the latest base64 `rowVersion`. A stale designer tab receives HTTP `409` instead of overwriting a colleague's work.

## Frontend builder guidance

Use three synchronized surfaces: a component palette, an A4 canvas preview, and a property inspector. Save the complete definition as a new version; do not PATCH individual blocks on the server. Keep local autosave in the browser, but create a database version only on an explicit save or a reasonable debounce/checkpoint boundary so dragging a block does not create hundreds of versions.

For the supplied Word samples, the measured page defaults are A4 portrait with 25.4 mm top/bottom margins, 31.75 mm left/right margins, and 12.7 mm header/footer distance. The samples contain Arabic legal paragraphs, employee identity fields, dates, monetary amounts, signatures, and fingerprint areas, all covered by the contract above.
