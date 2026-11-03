# Asset and licence log

Every third-party file the game ships must be listed here with its source and licence before it is committed. A commercial release depends on this log being complete.

## Shipped third-party assets

| Asset                             | Used for                                      | Source                                                      | Licence                   | Notes                                                    |
| --------------------------------- | --------------------------------------------- | ----------------------------------------------------------- | ------------------------- | -------------------------------------------------------- |
| IM Fell English (regular, italic) | Printed text in the UI and on in-world labels | `@fontsource/im-fell-english` 5.3.0 (fonts by Igino Marini) | SIL Open Font License 1.1 | Bundled through the npm package; no network font loading |
| IM Fell English SC                | Small-caps headings, labels, the shop sign    | `@fontsource/im-fell-english-sc` 5.3.0 (Igino Marini)       | SIL Open Font License 1.1 | As above                                                 |

## Original assets (made for this project)

| Asset                                                                            | Where                                                   |
| -------------------------------------------------------------------------------- | ------------------------------------------------------- |
| All 3D geometry (room, shelves, bottles, bench, still, counter, customers, desk) | Built in code: `src/scene/*.tsx`                        |
| Labels, gauge dial, calendar, shop sign, ledger page                             | Drawn at runtime on canvas: `src/scene/textures.ts`     |
| The ink-engraving renderer                                                       | `src/render/`                                           |
| Liquid colours                                                                   | `packages/content/data/ingredients.json` (`appearance`) |

## Code dependencies

Open-source libraries (React, Three.js, React Three Fiber, Zod, Vite) are tracked in `pnpm-lock.yaml`. All are MIT-licensed. A full third-party notices file will be generated for the desktop build before release.

## Rules for adding assets

1. Prefer CC0 sources (Poly Haven, Kenney, ambientCG) or commissioned work with a written transfer of rights.
2. Record the asset here in the same commit that adds it: file, source URL, author, licence, any attribution required.
3. No assets with non-commercial, share-alike-on-the-game, or unclear licences.
4. AI-generated assets: record the tool and its terms, and have them reviewed before release.
5. Budgets: 3D models as glTF/GLB, compressed textures, at most 2 MB per prop and 8 MB per hero asset.
