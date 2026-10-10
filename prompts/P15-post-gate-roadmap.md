# P15 — Post-gate roadmap (outline prompts)

Use these only after `docs/phase-1-gate.md` is accepted as **PASS** (D-027). Each item is a starting brief. At the start of Phase 2, ask Claude Code to expand one item at a time into a full prompt in this pack's format, informed by the gate report: "Expand P2-NN into a full prompt file using prompts/P08 as the format reference; do not build anything."

Order follows D-023 (provisional): finish U3, then U4, with U9 Party moved up, then the rest of the Master roadmap.

## Phase 2 — Identity and Party

**P2-01 Save system foundation** (Master §22). Versioned JSON save with atomic writes (write temp → fsync → rename), migrations, corruption recovery with a backup copy, settings merged in. No PlayerPrefs as truth. Unit tests for every migration and a fuzzed corruption test.

**P2-02 Phrase library to 50–100** (Master §7). Content pipeline at scale: categories, Easy–Brutal tiers, rhythm tags (needed for the DJ Rhythm Set), phoneme review queue, comedy-listen notes, provenance. Difficulty calibrated from Phase 1 data.

**P2-03 Classic mode and adaptive difficulty** (Master §5, §20). Successive phrases, adaptation rules, anti-repetition with seeded selection, records (personal bests per phrase).

**P2-04 Character framework (U4)** (Master §8, §17, §19; Bible §4–§6, §24, §33). CharacterDefinition, PersonalityProfile, DialogueBank with eligibility fields and lore layers, ReactionContext, cooldowns, and the narrative validation editor tool. Migrate Uprise from the Phase 1 JSON into it with no behaviour change, proven by tests.

**P2-05 Party mode (U9, moved up)** (Master §5). Local player names (no accounts, no saving of names beyond the session unless the player opts in), pass-the-phone rotation, best-of-5, knockout and same-phrase duel, results podium, rematch, Uprise commentary. Privacy: no audio kept after the round.

**P2-06 Progression basics** (Master §20). XP with bounded rewards and retry-abuse rules, levels, the first achievements (First Contact, That Was English?, Suspiciously Good, Perfectly Backwards, Redemption Arc, Absolute Nonsense). Never award for technical failures.

**P2-07 Scoring v2 research** (Master §6.3). Using a larger consented dataset: weighted phoneme substitution costs from the SBPS equivalence candidates, best-of-alternate-pronunciations, optional timing hybrid. Ships only if it beats v1 on held-out human ratings and fairness. Version bump; old scores are never compared to new ones.

**P2-08 Final art integration**. Import the artist's layered Uprise assets, expression sets, logo and icon; theme pass; check icon readability.

## Phase 3 — The bot family and Cora

**P3-01 Bot roster wave 1 (U5)**: ClumsyBot, Smarticus-9000, ChaosCore in Bible order; Bot Challenge mode with target scores; relationship hooks as authored lines only.
**P3-02 Free progression bots**: Professor Quibble (Genius Lab), Artist (Renaissance), Coach, Detective, DJ (Rhythm Set unlock).
**P3-03 Voice pipeline**: VoiceDirector, pre-recorded line assets, subtitles for every line, mobile mastering, contracts and consent recorded before any recording is imported.
**P3-04 Cora (U6)**: arrival chronology, theme, velvet voice direction, dialogue pools, animations; activates dormant Uprise state. Run the Bible §39 writing test on every line.
**P3-05 Hidden narrative (U7)**: multidimensional relationship state, Easter egg resolver with rarity budget, collectibles, CoraProximity voice processing, narrative validation in CI.

## Phase 4 — Sharing, events, commerce, launch

**P4-01 Replay Studio (U8)**: explicit saves only, raw/flipped playback, OS share sheet export, deletion; privacy review.
**P4-02 Daily challenge**: versioned manifest, UTC date, no punitive streaks.
**P4-03 Seasonal and deep lore (U10)**: Valentine's/Christmas rules, seasonal bots obtainable through play, rarity proven under simulated long-term play.
**P4-04 Commerce (U11a)**: shop for premium bots/themes/cosmetics only, platform receipts and restore, offline handling; Uprise and Cora never sold. Requires Jamie's pricing decisions.
**P4-05 Child-safety and store readiness (U11b)**: confirm the age rating (D-024), privacy policy, data safety form, accessibility audit, device matrix at scale, Android release, then the iOS port plan.
