# L2 Studio context

This glossary names the stable concepts shared by Studio's authoring, import,
publication, and inspection workflows. It deliberately does not prescribe a
database schema, HTTP route, or implementation technique.

| Term | Meaning |
| --- | --- |
| **Game version** | A named lineage of source material and the authored and generated work derived from it. It is the isolation boundary for Studio work. |
| **Authored content** | Editable Studio-owned game definitions and lookup values. It is not authoritative live-game state. |
| **Original resource** | A read-only-to-conversion source file supplied from a game client. Studio may manage its private storage but never republishes it as authored content. |
| **Import job** | Durable, observable work that reconciles authored content or discovers and converts original resources. |
| **Artifact** | An immutable generated result of processing one source identity with a particular build fingerprint, including its files and dependencies. |
| **Catalog** | The mutable, version-scoped index that selects the current artifact for each source identity and exposes import diagnostics. |
| **Release** | A coherent, immutable selection of healthy generated artifacts and client entry points which may be activated for one game version. |
| **Generated asset** | Browser-consumable output produced from an original resource. It is read-only outside the import and release workflow. |
| **Inspection surface** | A Studio-only interactive view of authored or generated material, used to understand and validate it without becoming a player runtime or authority. |
