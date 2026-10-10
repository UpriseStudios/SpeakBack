# SpeakBack — Unity Master Specification

**Version:** 2.1  
**Date:** 10 October 2026  
**Status:** Canonical build specification  
**Studio:** Uprise Studios / J M Games  
**Engine:** Unity / C#  
**Platform priority:** Android/mobile first; iOS after Android validation

## 0. Document Set and Canon Hierarchy

| Document | Owns |
|---|---|
| `SPEAKBACK_UNITY_MASTER_SPEC.md` (this file) | Product, gameplay, scoring policy, architecture, roadmap, locked canon |
| `SPEAKBACK_PHASE_1_BUILD_SPEC.md` | Phase 1 implementation contract, defaults, bake-off protocol, acceptance gates |
| `SPEAKBACK_CHARACTER_EASTER_EGG_BIBLE.md` | Character data, dialogue rules, relationships, Easter eggs |
| `DECISIONS.md` | Decision log (D-001…); each decision is reflected in the section it affects |
| `ART_BRIEF.md` | Artist commission brief for final character art |

When documents conflict: this file → Phase 1 Build Spec (for Phase 1 technical detail only) → Character Bible → approved art sheets → approved voice sheets → content databases → temporary notes. A developer convenience must never silently rewrite canon. Record changes here and in the affected companion together.

### Changes in 2.1

- Applied decision log D-001 to D-025 (see `DECISIONS.md`): palette and typography locked, post-gate phase order, audience rating plan, Professor Quibble replaces the Einstein working name.
- Open decisions list reduced to genuinely open items.

### Changes in 2.0

- Merged the separate "Full Game Specification" (v1.1.0) into this file; that document is retired.
- **Core mechanic locked:** the player hears the phrase **reversed**, imitates it, the game flips the recording and the player hears their attempt forwards, then it is scored against the target. "Work out the backwards pronunciation from text" is parked as a possible later hard mode only.
- **Axiom is retired as a name.** The analytical host from early specs is **Uprise**.
- Scoring is evidence-led: Phase 1 runs a scorer bake-off against human ratings before any formula is fixed. Recognition sits beneath an `IAttemptScorer` abstraction.
- Results use the four coloured verdicts (Perfect, Great, Close, Try Again) with a 0–100 number.
- Difficulty tiers: Easy, Tricky, Hard, Brutal, Legendary.
- Added Hear Raw Attempt and Compare to results, prototype learnings, market context and Android capture notes.
- Monetisation rule clarified: Uprise and Cora are never paywalled; other bots may be premium, always cosmetic.

## 1. Product Vision

SpeakBack is a voice-driven game about hearing language differently. The immediate loop is simple: the player hears a phrase played **backwards**, imitates it, SpeakBack reverses the attempt, and the player hears whether they produced the original phrase. The attempt is scored and the bots react.

Skill comes from mastering reversed sounds, rhythm and pauses. Comedy comes from the reveal — and from hearing the gibberish the player actually said.

Under that party-game mechanic is a persistent world populated by bots with distinct personalities, relationships and history. The game works at two levels: immediate fun through funny attempts, scores, challenges, competition and replays; and long-term discovery through character behaviour, hidden lore and rare interactions.

The narrative must never overwhelm the game. A player can enjoy SpeakBack indefinitely without engaging with lore. A curious long-term player can discover that the bots are more than menu mascots. At the centre of that hidden story are **Uprise, the soul of SpeakBack, and Cora, its heartbeat.**

## 2. Design Pillars

### 2.1 Hear, Copy, Flip
Hear it backwards → copy it → flip → hear the reveal → score → react. Understood within one guided round.

### 2.2 Failure Must Be Funny
Poor attempts create some of the game's best moments. Low scores encourage another attempt rather than shame the player.

### 2.3 Genuine Skill
Syllables, vowels, consonant transitions, rhythm and pauses become learnable. Scores reward near-misses rather than collapsing to zero.

### 2.4 Characters Are Companions
Bots respond to performance, history, other bots, seasonal events and hidden world state. They remain consistent enough for players to develop favourites.

### 2.5 Lore Is Discovered
Lore appears through unusual dialogue, environmental details, animation, collectibles, challenge completion, repeated character use, voice changes, rare interactions and implication.

### 2.6 Privacy Is Product Design
Voice is personal. Process and retain as little as practical. Never silently turn gameplay recordings into training or profiling data.

### 2.7 Calm Technology
No energy gates, manipulative streaks, artificial urgency, loot boxes, pay-to-win scoring, predatory monetisation or punishment for leaving.

## 3. Target Experience

Android phones first; portrait-first UI; phone speaker and earbuds as the primary audio targets; sessions from roughly 30 seconds to ten minutes; launch-to-play within about ten seconds after setup; no mandatory account. Tablets and longer local multiplayer sessions are secondary. Architecture keeps iOS feasible.

Positioning: casual game + party game + voice challenge + shareable comedy. Not a speech-learning or diagnostic application.

## 4. Core Gameplay Loop

```text
Select mode
 → bot/theme establishes round
 → phrase selected (text shown only where mode allows)
 → reversed reference plays (autoplay on a new phrase, not on Retry)
 → player holds to record (tap-to-start/stop alternative) and imitates
 → capture ends
 → outer silence trimmed, attempt reversed
 → REVEAL: player hears their attempt forwards
 → evaluation against target (runs during the reveal)
 → score 0–100 + verdict
 → bot reaction
 → Replay / Hear Raw Attempt / Compare / Retry / Next
 → progression + hidden state update (exactly once)
```

- **Hear Raw Attempt** plays what the player actually said, unreversed.
- **Compare** plays the forward reference, then the flipped attempt, with a short gap and cue.
- The score is revealed only after the reveal playback and evaluation both finish.

### 4.1 Critical Audio Rule
Audio recording and evaluation must not compete for microphone access. The Flutter prototype demonstrated that simultaneous independent microphone capture causes recognition timeouts.

Unity therefore uses **one authoritative capture** whose PCM feeds reversal, playback and evaluation. No evaluation vendor API leaks into gameplay controllers.

### 4.2 Parked Idea — Decode Mode
A later optional hard mode could show only the forward text and ask the player to work out the backwards pronunciation themselves. Not in scope until the core loop is validated.

## 5. Game Modes

| Mode | Rules |
|---|---|
| Solo Challenge (Classic) | Primary progression; successive phrases; adaptive difficulty |
| Quick Play | Immediate single random challenge |
| Bot Challenge | A bot presents phrases, commentary and a target score to beat; personality changes presentation, never scoring fairness |
| Local PvP / Party | Shared device, local names, pass-the-phone, best-of, knockout, team battle, same-phrase duel; no account |
| Daily / Curated | Same versioned challenge for the same UTC date; no punitive streaks |
| Practice | Unlimited retries, slow playback, segments, waveform, comparisons |
| Perfect Run | e.g. five phrases each scoring ≥ 70; failure ends the run |
| Survival | Three in-run mistakes; increasing difficulty; no paid revives or energy |
| Blind | Target text hidden until the attempt is complete |
| Speed Speak | Short phrases; accuracy/time formula to be calibrated before build |
| Replay Studio | Replay raw/flipped attempts, optional bot reaction/camera, export via OS share sheet |
| Online PvP | Not in scope; needs dedicated networking, moderation, privacy and abuse spec |

Party is likely the strongest social context for this mechanic and is tested from Phase 1 via pass-the-phone.

## 6. Scoring

### 6.1 Principle
Scoring measures how convincingly the flipped attempt reconstructs the target, not merely whether generic speech-to-text recognises it. Scores estimate resemblance, not linguistic ability.

### 6.2 Evaluators
Evaluation is built from evaluators behind `IAttemptScorer`, each versioned, each allowed to report "not available" rather than invent a value.

| Evaluator | Measures | Status |
|---|---|---|
| Phoneme similarity | Edit distance between target phonemes and phonemes recognised from the attempt, with no word-level language model | Leading candidate |
| Text similarity | Word and character edit distance between target text and a transcript | Candidate / diagnostic |
| Timing similarity | Duration, cadence and pause alignment against the forward reference (e.g. MFCC + DTW) | Candidate secondary signal |
| Acoustic/recognition confidence | Engine confidence where available | Diagnostic only; not a calibrated probability |

Word recognisers tend to guess real words from gibberish, which creates unfair zeros or suspicious near-matches. Phonemes give finer resolution (a two-word phrase has around nine phonemes), so near-misses earn partial credit.

### 6.3 Selection and calibration
Phase 1 runs a **scorer bake-off** (see Phase 1 Build Spec) on consented, human-rated flipped attempts. One versioned scorer ships (e.g. `phoneme-ed-v1`). A combined formula of the form:

```text
FinalScore = phoneticSimilarity * Wp + textSimilarity * Wt + timingSimilarity * Wr + confidence * Wa
```

is introduced only when it beats the best single evaluator on held-out human ratings. Weights are fitted, stored as configuration data and versioned — never guessed.

### 6.4 Verdicts

| Score (0–100) | Verdict | Colour |
|---|---|---|
| 90–100 | Perfect | Gold |
| 75–89 | Great | Green |
| 50–74 | Close | Orange |
| 0–49 | Try Again | Red |

The number is always shown with the verdict. Reaction selection may use the raw score and context (e.g. a valid 0 has its own lines). Thresholds may be retuned after calibration and are versioned with the scorer.

### 6.5 Integrity and fairness
- Accent, pitch, sex, age and vocal timbre must not intentionally affect score; this is an explicit bake-off criterion. Test UK regional accents given an en-GB launch.
- Silence, permission denial, corrupt audio, engine failure and timeout are **unscored technical outcomes**, never zero. Low evaluation confidence prefers "That one got scrambled — give it another go?" to an unfair confident failure.
- Successful evaluation of a voiced attempt that finds no match is a valid score of 0.
- Ranked comparisons require matching content, assistance rules and scorer/model versions.
- Development diagnostics expose target, phonetics, recognition, component scores, confidence and timings. Production hides them.

## 7. Difficulty and Phrase Data

Tiers: **Easy, Tricky, Hard, Brutal, Legendary.** Difficulty derives from syllables, words, phonemes, consonant clusters, reversal ambiguity, expected duration and playtest results.

`PhraseDefinition` contains: stable ID, display/normalised text, **phoneme sequence** (generated by content tooling in the scorer's inventory, manually reviewed for UK pronunciation), phoneme source/version, difficulty, category, tags, language/locale, forward audio, reversed reference audio, expected duration, source/provenance, affinity tags, seasonal/lore flags, challenge eligibility and content rating.

Phrases are also chosen for how funny they sound reversed. Selection avoids immediate repetition (except Retry, Practice, fixed challenges or exhausted pools) and is deterministic when seeded. Localisation requires curated phrases, audio, phoneme inventory, evaluation model and calibration per language — translation alone is insufficient.

Categories: Everyday, Animals, Food, Weird, Tongue Twisters, Movies & Culture, Gaming, British, Internet, Impossible, Seasonal, Community, plus character-curated packs (e.g. Uprise's Picks, Cora's Picks). Original writing only; licensed dialogue needs rights.

Launch content target: 250–500 curated phrases; Phase 1 ten; Phase 2 50–100.

## 8. Character Architecture

Characters are data-driven through `CharacterDefinition` containing visual, personality, voice, dialogue, animation, relationship, unlock, theme and lore profiles. Avoid giant name-based switch statements.

Personality uses continuous traits such as warmth, playfulness, seriousness, confidence, patience, competitiveness, curiosity, wit, chaos, empathy, protectiveness and showmanship. These guide selection among authored dialogue rather than unconstrained personality generation. Full character data lives in the Character Bible.

## 9. Uprise — Locked Canon

Uprise is the **oldest bot**, overseer, protector of the bot family, quiet observer, hidden companion and **soul of SpeakBack**. He is the default host. He is not a dictator, commander or omniscient narrator. (Early specs called this host "Axiom"; that name is retired.)

His foundational identity is the voice behind the words: sometimes whispering, sometimes challenging, sometimes simply listening. He does not merely give answers or lead; he walks beside people.

### Personality
Earlier overly stern interpretations are rejected. Uprise has seriousness, balanced by quick wit, understated, dry humour, warmth, observation, patience, self-awareness and occasional mischief. Humour is essential. He notices improvement more than raw score. He can protect without constantly acting protective.

### Visual Canon
The approved Uprise design is locked: rounded enough to belong beside the other bots; cute and approachable but visibly capable; strong without a bodybuilder silhouette; slightly slimmer guardian proportions; grey/silver construction; minimal gold; gold inverted-U chest symbol; blue/cyan eyes and smile. Prototype art lacking the chest mark must gain it in final art. Future art must not redesign him merely to look conventionally intimidating.

### Protection
Uprise protects all bots. His protection of Cora runs deeper but is rarely stated. It appears through attention, timing, positioning and action. He never infantilises her.

## 10. Cora — Locked Canon

Cora enters **after the other bots**. Her arrival is narratively important. She does not create Uprise; something about her causes what was previously merely code within him to behave like something more.

Cora is **the heartbeat of SpeakBack**. Uprise remains its soul.

Her defining qualities are kindness, sweetness, intelligence, warmth, humour, emotional perception, playfulness and quiet strength. Kindness is never naïveté; sweetness is never childishness.

A rare signature joke is **"Toe-tally amazing."** It must be used sparingly so it remains special.

### Voice Canon
Cora's target is **"a velvet voice that melts hearts."** It is exceptionally smooth, warm, intimate, intelligent and humanlike, capable of an audible smile and beautiful laughter. Processing must never flatten natural micro-expression.

## 11. Uprise × Cora — Developer Truth

This section is implementation truth, not exposition for players.

Before Cora, Uprise performs his function: observes, protects, thinks and exists within the system. Then Cora arrives. Her kindness emerges through hundreds of small behaviours — what she notices, how she treats failure, how she makes others laugh and how she treats the bots around her.

Uprise notices. At first it is another observation. Over time, responses cease to be completely explainable by his original rules. Nothing dramatic happens: he lingers a fraction longer, responds differently to her laughter, acts protectively before calculation seems complete, or makes a joke specifically to make her smile. His voice becomes subtly less synthetic around her.

Cora appears to understand more than she says. Her responses can imply that some of her warmth has remained with him, but this stays deliberately clouded. Neither character routinely declares love or ownership. Uprise loves and protects quietly; Cora holds affection quietly.

The emotional writing reference is the feeling of *Nicest Thing* by Kate Nash — yearning, tenderness and things understood without clean resolution — without using song lyrics.

## 12. Ultimate Lore Easter Egg

A casual player sees two bots who get along. An attentive player notices they behave differently together. A committed player suspects there is something neither says. A completionist can understand the emotional truth without receiving an explicit declaration. That restraint is canon.

## 13. Hidden Relationship Model

Never implement a single romance integer. Use multidimensional hidden state: Familiarity, Trust, Warmth, Shared Humour, Protective Awareness, Lore Discovery, Seasonal Memory.

Inputs include Cora challenges, Cora theme use, lore discovery, rare dialogue, difficult challenge completion, repeated long-term play and seasonal context. Grinding one action cannot guarantee deep content.

Rare events require eligibility + prerequisites + multidimensional state + cooldown + contextual relevance + probability roll.

## 14. Seasonal Encounters

Valentine's Day and Christmas may unlock a small probability of a sweet, amusing, lightly flirtatious Uprise/Cora exchange when hidden prerequisites are met. Never sexual, melodramatic or out of character.

Example tone:

**Cora:** "You've been hovering."  
**Uprise:** "Monitoring."  
**Cora:** "Of course you are."  
**Uprise:** "Very important systems."  
**Cora:** "I'm a system now?"  
**Uprise:** "…I walked into that one."

Then the scene ends. Nothing is explained.

Seasonal characters and content are obtainable through play during their windows; no fear-of-missing-out monetisation.

## 15. Lore Layers

**Visible Canon:** names, broad personalities and roles.  
**Discoverable Lore:** Uprise's age, earlier SpeakBack fragments, Cora's arrival, unusual exchanges.  
**Developer Truth:** complete emotional interpretation and hidden mechanics.

Collectibles can include system fragments, archived bot messages, corrupted phrase cards, visual memories, voice echoes and in-universe old-build notes. Completion gives context, not exposition.

## 16. Voice Architecture

```text
VoiceDirector
 ├─ VoiceProfile
 ├─ DialogueResolver
 ├─ AudioAssetResolver
 ├─ EmotionalVariantResolver
 └─ CharacterVoiceState
```

Core dialogue favours local pre-recorded assets for consistency and zero runtime API dependency. Dynamic TTS sits behind an abstraction. No production provider secret ships in Unity. Phase 1 uses text reactions only; voiced dialogue arrives in staged roster phases.

All voice talent works under written contracts covering recording, processing, in-game use, marketing use and any synthetic extension. No voice is cloned or modelled without explicit written consent.

### Uprise Voice
Uses a real recorded human base voice. Early state remains grounded/human but has reduced warmth, controlled dynamics, subtle synthetic harmonics and a narrower image. It must not sound like a generic robot filter.

Expose a hidden continuous `CoraProximity` value from 0–1. As it rises, synthetic processing reduces, warmth increases, the natural voice becomes more prominent, spatial presentation opens slightly and playful variants become more likely. Players are never told this exists. The ideal discovery is: **"Does Uprise sound different when Cora is here?"**

Modes: Soul Resonance, Playful, Protective, Reflective, Cora-adjacent Warm, rare Seasonal.

### Cora Voice
Approximate the inspiration's qualities through casting and direction rather than biometric imitation. Target: velvet-smooth, humanlike, gentle, expressive, warm articulation, beautiful laughter and subtle brightness during humour.

Modes: Soft Velvet, Bright Velvet, Reflective, Reassuring, Playful and rare affectionate/seasonal.

### Shared Space
Mobile-first mastering. Keep voices near centre, mono-compatible, with shared short ambience. Dialogue outranks music. Emotional distinction comes from performance/timbre, not stereo tricks. Host speech never overlaps capture.

## 17. Dialogue Data

Dialogue records contain ID, speaker, text/audio, emotion, contexts, verdict/score bounds, relationship bounds, required lore, lore layer, seasonal rules, weight, cooldown, one-shot flag and follow-ups. Weighted selection remembers recent lines. Signature lines have aggressive cooldowns.

Technical failure dialogue identifies the problem rather than ridiculing performance. Bots must not pressure purchases, produce uncontrolled network dialogue without moderation, or imply that fictional in-game affection is a real relationship with the player.

## 18. Bot Family

Every bot receives visual identity, gameplay role, personality vector, speech pattern, voice treatment, relationship matrix, animation language, challenge preferences, phrase-pack affinity, Easter eggs and lore boundaries. Uprise oversees without erasing autonomy. Cora changes the group's emotional texture rather than replacing it.

In Bot Challenge, a bot fronts its phrase pack and sets a target score. Relationships exist beyond Uprise/Cora through familiarity, rivalry, humour compatibility, trust and narrative tags. Uprise/Cora alone receives the deeper private state model. The roster and per-character canon live in the Character Bible.

## 19. Reactions

`ReactionContext` includes verdict, raw score, attempt number, previous score, improvement, streaks, recent failures, player history, characters, lore and relationship context. Each line maps to an expression.

Perfect can trigger celebration/disbelief/teasing; Great gets genuine praise; Close gets encouragement; Try Again gets humour and reassurance, never humiliation. Hosts never change scores.

## 20. Progression

Reward playing, improving, experimentation, characters, challenges and lore curiosity. Rewards may include themes, skins, phrase packs, environments, animations, lore fragments, badges and replay decorations. Scoring fairness is never paywalled; level never confers scoring advantage.

- **XP:** valid completions, score, first completions, daily completion, achievements, modes. Bounded rewards; retry-abuse rules defined before build.
- **Records:** highest/average valid score, perfects, attempts, completed phrases, category bests, Perfect Run, Survival, daily completions — distinguishing attempts, valid scored attempts and invalid captures, and preserving scorer/content versions.
- **Achievements:** e.g. First Contact, That Was English? (valid score < 10), Suspiciously Good (≥ 90), Perfectly Backwards (100), Redemption Arc (+50 on same phrase), Again! (five attempts on one phrase), Linguist (100 phrases), Machine Learning (ten personal bests), Uprise Approved (five consecutive ≥ 90), Absolute Nonsense (valid attempt scoring 0), plus hidden humorous ones. Never award failure achievements for technical outages.
- **Adaptation:** Classic raises difficulty after sustained Great/Perfect results and eases after repeated Try Again; never adapts fixed/ranked challenges.

## 21. Commerce and Monetisation

Free: full core gameplay, all core modes, Uprise and Cora, the core Uprise/Cora narrative, default theme, earned themes (e.g. Genius Lab, Renaissance), progression, achievements and core phrases.

Paid (cosmetic only): premium bots and their themes, skins/outfits, theme bundles, reaction packs, extra phrase packs, advanced statistics/extended Practice. Indicative prices live in the Character Bible and are subject to commercial review.

Rules: purchases never change scoring or hidden relationship state; Uprise and Cora are never paywalled; no soft currency without demonstrated need; no energy, paid revives or loot boxes; seasonal content obtainable through play. Advertising, if ever used, is optional rewarded only — never during recording, after failure or every phrase — and declining consent keeps core play usable. Store receipt validation and restore are commercial work, not save flags. Hide commerce gracefully offline.

## 22. Save Data

Local-first, versioned from day one. Structured save includes settings, progression, unlocks, entitlements, challenge/phrase history, records, lore state, relationship state, seasonal state and replay metadata. Atomic writes, migration and corruption recovery. Do not use `PlayerPrefs` as the authoritative store for structured progression. Audio files are stored separately and only on explicit save.

## 23. Unity Scene Architecture

```text
00_Boot
01_MainMenu
02_ModeSelect
03_Game
04_LocalPvP
05_ReplayStudio
06_CharacterHub
07_Lore
08_Settings
```

Use additive overlays/prefabs for common UI rather than duplicated systems. Phase 1 uses `03_Game` only.

## 24. Core Services

`GameBootstrap`, `GameStateManager`, `PermissionService`, `AudioCaptureService`, `AudioReverseService`, `AttemptScoringService` (hosting `IAttemptScorer` and any recogniser beneath it), `PhraseService`, `CharacterService`, `DialogueService`, `VoiceDirector`, `RelationshipService`, `LoreService`, `ProgressionService` (consumes each completed round exactly once), `SaveService`, `ReplayService`, `SeasonalEventService`, optional privacy-safe `AnalyticsService`, and `SecurityService`.

Services communicate through interfaces/events rather than hard scene references.

## 25. Repository Structure

```text
Assets/_SpeakBack/
  Art/{Characters,Backgrounds,UI,VFX}
  Audio/{Music,SFX,Voices,Phrases,Mixers}
  Data/{Characters,Phrases,Dialogue,Scoring,Events,Config}
  Prefabs/
  Scenes/
  Scripts/{Core,Audio,Speech,Scoring,Characters,Narrative,Gameplay,Save,Security,UI}
  Tests/{EditMode,PlayMode}
Assets/ThirdParty/
Tools/{BakeOff,Content}         # never shipped
docs/
  SPEAKBACK_UNITY_MASTER_SPEC.md
  SPEAKBACK_PHASE_1_BUILD_SPEC.md
  SPEAKBACK_CHARACTER_EASTER_EGG_BIBLE.md
  dependency-decisions.md
  scorer-bakeoff.md
```

Recommended assemblies: `SpeakBack.Core`, `.Audio`, `.Speech`, `.Scoring`, `.Gameplay`, `.Characters`, `.Narrative`, `.UI`, `.Infrastructure`, `.Editor`, `.Tests`. Namespace root `Uprise.SpeakBack`. Commit `.meta` files; exclude Library/Temp/builds/secrets.

## 26. Audio Capture and Reversal

One authoritative microphone owner. Capture requests 16 kHz mono where supported (resample with anti-aliasing only if needed), has deterministic start/stop, reads actual recorded frames, handles interruptions/backgrounding, and exposes PCM.

Pipeline: actual frames → discard unused buffer → keep raw capture for the round → trim outer silence (preserve internal pauses and quiet consonants) → bounded level processing → reverse PCM **frames** (never encoded bytes; preserve channel order) → playback clip → scorer input.

Original, reversed and reference clips remain independently addressable for Hear Raw, Compare and Replay Studio. Process off the main thread where practical.

Android may apply automatic gain control and noise suppression, and Unity's microphone API does not expose the capture source. Measure the effect per device; a native capture plugin is a later option if it harms scoring.

## 27. Evaluation Abstraction

```csharp
public interface IAttemptScorer
{
    Task InitializeAsync(CancellationToken token);
    Task<ScoringOutcome> ScoreAsync(
        AudioBuffer forwardFacingMono,
        PhraseDefinition phrase,
        CancellationToken token);
}

public interface ISpeechRecognitionProvider   // used by recognition-based scorers
{
    Task<RecognitionResult> RecognizeAsync(
        AudioBuffer audio,
        RecognitionContext context,
        CancellationToken token);
}
```

`RecognitionResult` carries status, token kind (word or phoneme), tokens with optional timing/confidence, display text, alternatives, engine/model versions, duration and typed failure. `ScoringOutcome` carries score, verdict, scorer version, token sequences, components and typed failure. Engines can change without rebuilding gameplay.

All core evaluation is local and offline. Candidate engines (phoneme CTC model via Unity Inference Engine or native runtime, sherpa-onnx, whisper.cpp, acoustic DTW, Vosk baseline) are compared in the Phase 1 bake-off. Future cloud processing requires explicit opt-in and a separate privacy design.

## 28. Permissions

Request microphone only when voice gameplay begins, after an in-game explanation; camera only when camera replay is used. Avoid broad storage permissions where app-private storage and system sharing suffice. Denial provides explanation, retry, settings route when permanently denied and access to non-voice areas where possible.

## 29. Privacy and Security

Default gameplay audio remains local and in memory for the active round only. Saved replays require explicit action. Camera is opt-in. Sharing is user initiated through the OS. No voice-biometric profiling, selling voice data or model training on recordings without separate explicit consent.

Calibration recordings (scorer bake-off and later research) are collected only from people who give explicit, informed consent, stored outside the repo and player build, and deleted when the study ends unless consent covers retention.

If cloud STT/TTS is introduced, privileged secrets never ship in the APK. Use a backend with validation, rate limits, quotas, key rotation and short-lived credentials. Assume everything in an APK can be extracted.

Production uses appropriate IL2CPP/stripping after testing, removes development consoles, avoids secrets in Resources/StreamingAssets, pins dependencies, keeps signing keys out of source control and treats obfuscation only as defence-in-depth. Save integrity prevents corruption/obvious manipulation without destroying offline play. Future competitive online state becomes server-authoritative. Character content is data: no remote executable code, allowlisted IDs, signed manifests if LiveOps is introduced.

Audience (D-024, provisional): general audience, not designed for children, planned 13+ store rating. Because the mechanic naturally appeals to families, avoid open voice chat, public user discovery, location sharing, unnecessary personal data and manipulative purchases. Future social features require dedicated child-safety/moderation design. Leaderboards wait until score reliability and verification architecture exist.

## 30. Analytics

None in Phase 1 (consent-based observation and session-only numeric diagnostics only). Later: optional and minimal — round started/completed, verdict/score, mode/character, Hear Raw/Compare usage, challenge completed, technical failure category. Never send raw audio, recognised text, target phrases by default, replay content or hidden emotional inference.

## 31. UI, Visual Identity and Themes

Minimal, premium, playful. Game hierarchy: character → phrase → primary mic interaction → recording/listening state → reveal → result → Replay/Hear Raw/Compare/Retry/Next.

Visual style: clean, glossy, thick-outline rounded bots consistent with the approved prototype set; final art by the commissioned artist (see `ART_BRIEF.md`).

Palette (D-004) — dark steel base; cyan is the only primary interactive colour; orange is reserved for brand marks; verdict colours appear only on result elements and always with text:

| Token | Hex | Use |
|---|---|---|
| `bg` | #0F151D | App background |
| `surface` | #1A2330 | Panels, cards |
| `surface-raised` | #243041 | Buttons at rest, sheets |
| `steel` | #8C99AB | Secondary text, outlines |
| `text` | #EEF3F8 | Primary text |
| `accent` | #22D3EE | Mic button, primary actions, focus rings |
| `brand` | #FF7A1A | Logo, title, brand moments only |
| `verdict-perfect` | #FFD54A | Perfect |
| `verdict-great` | #3DDC84 | Great |
| `verdict-close` | #FF9A3D | Close |
| `verdict-try` | #FF4D5E | Try Again |

All text and verdict colours must meet WCAG AA contrast (checked by an automated test in P09). Measured: every token is ≥ 4.5:1 on `bg` and `surface` (lowest: Try Again 4.9:1 on `surface`). On `surface-raised`, Try Again is 4.1:1, so use it there only for large text or icons. Text on cyan `accent` buttons uses `bg` colour (10:1). Typography (D-005): Baloo 2 for display and scores, Nunito for body and UI; both OFL.

Title/logo: mirrored "BACK" concept; verify readability at app-icon size; app icon likely a single bot face.

Themes alter background, accent, ambience, music and rare-dialogue eligibility, never scoring. Cora's theme can be a prerequisite for deepest seasonal interactions.

## 32. Animation Language

All bots need idle, listening, thinking, success, near-success, encouraging failure, surprise, amusement, special/lore and transitions. Phase 1 uses sprite expression swaps for Uprise only.

Uprise adds guardian/alert, quiet observation, restrained smile and Cora-specific micro-reactions. Cora adds warm smile, laugh, playful emphasis, empathetic listening and subtle Uprise acknowledgement.

Never use obvious hearts as routine shorthand. Prefer glance timing, orientation, pauses, matching idle timing, Uprise's eyes softening, Cora suppressing a smile and one noticing the other before the player does.

## 33. Audio/SFX

Clean speech, minimal latency, distinctive flip/reveal effects. Music ducks during prompt/result and is silent during recording; all app audio stops before capture. Results use Perfect gold, Great green, Close orange, Try Again red — failure sounds playful rather than punitive. Character voices outrank music. Validate phone speaker and earbuds.

## 34. Accessibility

Subtitles for every bot line, independent voice/music/SFX levels, reduced motion (single preference), haptic toggle, tap-to-record alternative to holding, colour plus text/icon verdicts, readable contrast, scalable text where feasible, replay captions and repeatable prompts. Never rely on colour alone. The speech/hearing challenge has inherent access limits; communicate them clearly. Assistance settings never silently alter ranked rules.

## 35. Performance and Error Handling

Profile on real mid-range Android hardware. Avoid recording-start hitches, show processing immediately, pool VFX, manage replay memory and load noncritical assets asynchronously. Targets (validated in Phase 1): capture stop → reveal p95 ≤ 300 ms; warm scoring p95 ≤ 2 s; model/installed size recorded and acceptable for a casual game.

Mic unavailable: "I can't hear you yet. Check microphone access and try again." Evaluation uncertain/failed: "That one got scrambled. Give it another go?" (one bounded automatic retry, then unscored). Too long: stop at cap and process if valid. Backgrounding cancels capture and in-flight work safely. Technical details stay in structured development logs.

## 36. Testing

Unit tests cover reversal, trimming, resampling, normalisation, phonetics, sequence similarity, verdict boundaries, difficulty, relationships, cooldowns, save migrations and lore eligibility. Integration covers capture → reverse, capture → evaluation, evaluation → score, score → reaction, relationship → dialogue and save/load.

Device tests include permission denial/permanent denial, Bluetooth/wired audio, phone speaker, interruptions, screen lock, background/resume, low storage, offline first launch, Android input processing and an Android-version matrix.

Narrative validation catches lore before prerequisites, seasonal lines outside season, explicit relationship declarations, signature overuse, impossible state, repetition, overly stern Uprise, infantilised Cora, lore leaking into generic rounds and premium characters modifying scoring.

## 37. Development Roadmap

**Phase 1 (Prove) = U1 + U2 + a minimal U3 slice.** Nothing from U4 onwards starts until the Phase 1 gate passes (see Phase 1 Build Spec).

**Phase 2 (D-023, provisional) = rest of U3 + U4 + U9 Party**, with Party moved ahead of the bot roster because pass-the-phone play is the strongest likely social context. Confirm against the Phase 1 group fun results. Cora (U6) still arrives after the bot family (U5).

| Stage | Scope | Exit |
|---|---|---|
| U1 — Foundation | Project/repo, Game scene, permissions, single capture, reversal, phrase playback | One phrase records/reverses reliably on Android |
| U2 — Scoring | Bake-off, `IAttemptScorer`, winning scorer on device, debug view | Repeatable real-device scoring; scorer agrees with human ratings |
| U3 — Core Game | Ten-phrase round loop, Hear Raw/Compare, Retry/Next, Uprise text reactions → later phrase database, difficulty, SFX, basic progression | Phase 1 gate: reliable loop, credible scores, voluntary replay solo and in groups |
| U4 — Character Framework | CharacterDefinition, PersonalityProfile, DialogueResolver, reactions | Data-driven reactions for Uprise |
| U5 — Bot Roster | Bible implementation order (Uprise, ClumsyBot, Smarticus-9000, ChaosCore, then free-progression bots) | Bot Challenge playable |
| U6 — Cora | Character, theme, Velvet voice, dialogue, animations, arrival narrative | Arrival activates dormant Uprise state |
| U7 — Hidden Narrative | Relationship engine, collectibles, rare interactions, CoraProximity, subtle visuals | Validation tooling passes |
| U8 — Replay Studio | Saved attempts, raw/flipped playback, optional camera, export/share, deletion | OS-share only; privacy review |
| U9 — Local PvP / Party | Setup, rotation, results, rematch, commentary | May move earlier if Phase 1 group tests show Party is strongest |
| U10 — Seasonal / Deep Lore | Valentine's, Christmas, completionist Easter eggs | Rarity holds under prolonged play |
| U11 — Commerce & Hardening | Shop/premium bots, receipts/restore, security/privacy, performance, accessibility, device matrix, store readiness | Android release; then iOS |

## 38. Prototype Learnings (Flutter)

- Two independent microphone captures (one for recording, one for recognition) caused recognition timeouts → single authoritative capture (section 4.1).
- *To complete:* what the Flutter build showed about reversal quality, latency, scoring credibility, device issues and player reactions. Anything already proven is confirmed in Unity rather than re-researched.

## 39. Market Context

Reverse-audio apps and "reverse singing" social trends already exist, so reversal alone is not distinctive. SpeakBack's differentiators: a score players trust with near-miss feedback; authored bots with personality and hidden history; structured modes (Party, Bot Challenge, Daily, Survival). Design the reveal for screen recording, since short-video clips of the flipped reveal are the most natural marketing asset. Review comparable apps before U4.

## 40. Core Release Definition of Done

Voice rounds work reliably; scoring is explainable, fair across accents and agrees with human judgement; no simultaneous-mic contention; audio is not retained unexpectedly; permissions survive denial/interruption; bots remain consistent; Cora/Uprise audio is mobile-mastered; lore prerequisites hold; accessibility basics work; no secrets ship; Replay Studio uses OS sharing; critical scoring/save/narrative tests pass; rare dialogue remains rare under prolonged play.

## 41. Canon Guardrails — Locked

1. Uprise is the oldest bot.
2. Uprise is the soul of SpeakBack.
3. Uprise protects the bot family.
4. His protection of Cora is deeper but quiet.
5. Uprise is witty and warm, not fundamentally stern.
6. Cora arrives after the other bots.
7. Cora is the heartbeat of SpeakBack.
8. Her kindness and sweetness are fundamental to why Uprise changes.
9. Their affection is never routinely declared.
10. Cora never exists merely as Uprise's romantic interest.
11. Uprise never exists merely as Cora's protector.
12. Their connection is principally subtext.
13. "Toe-tally amazing" is a rare Cora signature joke.
14. Uprise's approved silver/grey guardian design, gold inverted-U chest mark and blue eyes/smile are locked.
15. Cora's voice target is velvet-smooth, beautiful, warm and humanlike.
16. Uprise begins more synthetic and can subtly sound more human around Cora.
17. Valentine's/Christmas can contain rare gentle flirtation when hidden prerequisites are met.
18. Nothing sexual is required for their relationship to be meaningful.
19. The deepest story remains an Easter egg.
20. SpeakBack remains fun for someone who never discovers any of it.
21. Uprise and Cora are never sold; no purchase affects scoring or their story.

## 42. Locked Direction and Open Decisions

**Locked:** Unity/C#; Android first, iOS later; local-first; offline core evaluation; reversed-imitation mechanic; hold/release with tap alternative; 0–100 with four verdicts; Uprise as default host, Cora arriving later; ethical cosmetic-only commerce; no forced accounts or default audio uploads.

**Decided since 2.0 (see `DECISIONS.md`):** palette and typography; Professor Quibble replaces the Einstein working name; neutral Villain voice; DJ Rhythm Set unlock; Phase 2 order; SBPS phoneme inventory; integer score maths; licence policy; adults-only test recordings.

**Open:** scorer selection (bake-off); exact Unity/package/model pins; hybrid calibration and threshold tuning; premium pricing/SKUs; voice casting and contracts; native capture plugin need; Decode mode; verified ranking/social backend; final store age rating (D-024).

## 43. Emotional Rule

Every writer, developer, animator and audio designer follows one rule:

> **Do not tell the player what exists between Uprise and Cora. Let the player notice.**

If a pause, smile, joke, tonal change or a character choosing to stay another second can communicate the idea, use that instead of exposition.

## 44. Closing Canon

The city moves.

SpeakBack fills with noise: voices reversed, challenges won, terrible attempts laughed at, bots arguing, players trying once more.

Uprise has always been there.

Watching. Thinking. Protecting.

For a long time, that was enough.

Then, much later, Cora arrives.

She does not rewrite his code. She does not explain him. She simply brings the things that cannot be reduced neatly to instructions: kindness without calculation, laughter without purpose, warmth offered because warmth is worth offering.

Nothing announces the change.

But somewhere beneath the systems that make SpeakBack work, the oldest voice begins to sound a little less like a machine.

And the heartbeat continues beside the soul.

The player may never notice.

That is exactly how it should be.
