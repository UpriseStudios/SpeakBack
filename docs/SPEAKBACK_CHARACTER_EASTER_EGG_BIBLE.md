# SpeakBack — Character & Easter Egg Bible

**Document version:** v1.2 – Character & Easter Egg Bible (10 October 2026)  
**Status:** Canonical companion specification  
**Parent document:** `SPEAKBACK_UNITY_MASTER_SPEC.md` (v2.1)  
**Sibling document:** `SPEAKBACK_PHASE_1_BUILD_SPEC.md`  

**Changes in v1.2:** applied `DECISIONS.md` — the Einstein working name becomes **Professor Quibble**, an original character (D-006); Villain voice is neutral theatrical (D-007); DJ unlocks through the Rhythm Set (D-008).  
**Changes in v1.1:** aligned with Master Spec 2.0 — Uprise is the default host (the name "Axiom" from early specs is retired); reactions map to the four verdicts (Perfect, Great, Close, Try Again); Uprise and Cora are never sold; Phase 1 scope note added to implementation priority; open decisions flagged for Professor Quibble, Villain voice and DJ unlock.  
**Studio:** Uprise Studios / J M Games  
**Engine:** Unity  
**Audience:** Design, writing, audio, animation, engineering, QA  

---

## 1. Purpose

This document is the canonical character data layer beneath the SpeakBack Unity Master Specification. It defines who every bot is, how each bot behaves, how the cast relates to one another, how personality is represented in code, what each character sounds and looks like, how characters unlock, and which Easter eggs may emerge through long-term play.

The central rule is simple:

> **Characters must feel discovered, not explained.**

SpeakBack remains a voice game first. Character and lore systems enrich repeated play without demanding that a player care about the story. A player who only wants to reverse ridiculous phrases should have a complete game. A player who watches closely should gradually realise that the bot family has history, friendships, rivalries and secrets.

The deepest of those secrets is the quiet relationship between **Uprise, the soul of SpeakBack, and Cora, its heartbeat**.

---

# 2. Canon Hierarchy

When content conflicts, use this priority:

1. `SPEAKBACK_UNITY_MASTER_SPEC.md`
2. This Character & Easter Egg Bible
3. Approved character art sheets
4. Approved voice sheets
5. Dialogue/content databases
6. Temporary implementation notes

A developer convenience must never silently rewrite character canon.

---

# 3. Character Content Layers

Every character has three information layers.

### Layer A — Player-visible identity

Immediately understandable traits: name, silhouette, broad personality, theme and gameplay role.

### Layer B — Discoverable character depth

Traits revealed through repeated play: insecurities, friendships, rivalries, unusual reactions, hidden jokes, history and rare dialogue.

### Layer C — Developer truth

Internal motivations, hidden relationship values, narrative chronology and event prerequisites. This layer is never dumped directly into dialogue.

The writing system must tag content by layer so developer-truth lines cannot accidentally enter ordinary dialogue pools.

---

# 4. Personality Runtime Model

Each character uses a `PersonalityProfile` rather than hard-coded behaviour.

Suggested normalized dimensions:

```text
warmth
playfulness
seriousness
confidence
patience
competitiveness
curiosity
wit
chaos
empathy
protectiveness
showmanship
```

Values guide authored-content selection. They do not give a generative model permission to invent new canon.

## 4.1 Baseline Personality Matrix

| Character | Warmth | Play | Serious | Conf. | Patience | Competitive | Curiosity | Wit | Chaos | Empathy | Protect | Show |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Uprise | .82 | .66 | .64 | .82 | .92 | .35 | .90 | .94 | .24 | .88 | .98 | .18 |
| Cora | .97 | .84 | .38 | .78 | .88 | .42 | .76 | .86 | .34 | .98 | .64 | .45 |
| ClumsyBot | .86 | .94 | .12 | .42 | .70 | .36 | .60 | .64 | .88 | .82 | .30 | .62 |
| Smarticus-9000 | .58 | .38 | .82 | .91 | .74 | .72 | .96 | .72 | .12 | .54 | .30 | .38 |
| ChaosCore | .62 | .98 | .24 | .88 | .34 | .84 | .72 | .86 | .99 | .58 | .36 | .92 |
| Professor Quibble | .70 | .62 | .72 | .88 | .78 | .52 | .99 | .90 | .30 | .66 | .32 | .42 |
| Artist | .90 | .78 | .48 | .74 | .82 | .32 | .88 | .80 | .40 | .92 | .36 | .72 |
| Coach | .92 | .76 | .46 | .92 | .90 | .78 | .54 | .66 | .24 | .94 | .60 | .78 |
| Detective | .56 | .42 | .88 | .82 | .92 | .48 | .96 | .84 | .12 | .62 | .48 | .28 |
| DJ | .84 | .94 | .28 | .90 | .62 | .58 | .70 | .72 | .64 | .82 | .32 | .98 |
| King | .54 | .36 | .82 | .98 | .70 | .74 | .44 | .66 | .18 | .46 | .62 | .96 |
| Villain | .30 | .70 | .74 | .96 | .58 | .92 | .76 | .90 | .58 | .30 | .38 | .94 |
| Samurai | .58 | .30 | .96 | .90 | .98 | .72 | .72 | .52 | .08 | .66 | .78 | .34 |
| Pop Diva | .78 | .92 | .34 | .98 | .56 | .70 | .64 | .76 | .52 | .74 | .28 | 1.00 |
| Alien | .46 | .70 | .52 | .62 | .70 | .44 | 1.00 | .58 | .74 | .48 | .22 | .54 |
| Rockstar | .64 | .90 | .22 | .98 | .42 | .78 | .54 | .68 | .72 | .56 | .24 | 1.00 |
| Cowboy | .72 | .72 | .52 | .88 | .76 | .70 | .58 | .78 | .38 | .70 | .52 | .72 |
| Footballer | .62 | .82 | .32 | .96 | .46 | .96 | .50 | .74 | .44 | .50 | .26 | .88 |
| Rudolph | .96 | .88 | .20 | .70 | .88 | .34 | .64 | .64 | .36 | .96 | .54 | .82 |
| Scarecrow | .42 | .72 | .64 | .78 | .82 | .62 | .74 | .84 | .66 | .40 | .28 | .80 |
| Easter Bunny | .90 | .96 | .18 | .78 | .58 | .62 | .72 | .74 | .68 | .84 | .30 | .90 |
| Cupid | .86 | .82 | .32 | .88 | .72 | .48 | .80 | .88 | .38 | .90 | .38 | .84 |

These are initial tuning values, not immutable mathematical truths. Material changes require narrative review.

---

# 5. Character Definition Schema

Unity should represent characters with ScriptableObjects plus referenced content assets.

```csharp
[CreateAssetMenu(menuName = "SpeakBack/Character Definition")]
public sealed class CharacterDefinition : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public CharacterTier Tier;
    public PersonalityProfile Personality;
    public VoiceProfile Voice;
    public VisualProfile Visual;
    public ThemeDefinition Theme;
    public UnlockDefinition Unlock;
    public DialogueBank Dialogue;
    public AnimationProfile Animation;
    public LoreProfile Lore;
}
```

No character-specific logic belongs in a giant switch statement.

---

# 6. Dialogue Context

Every authored line should declare its eligibility.

```text
speaker
category
emotion
scoreBand
minAttempts
maxAttempts
requiredCharacters
requiredTheme
requiredLore
relationshipRequirements
season
weight
cooldown
oneShot
playerFacingLayer
```

Dialogue categories include:

- greeting;
- round intro;
- listening;
- perfect;
- great;
- close;
- retry;
- improvement;
- streak;
- idle;
- rivalry;
- friendship;
- lore;
- seasonal;
- rare;
- hidden relationship.

Signature lines must have stronger cooldowns than ordinary dialogue.

---

# 7. The Foundational Pair

## 7.1 Uprise — The Soul

### Canon role

Uprise is the **oldest bot**, the overseer and protector of the SpeakBack family, the **soul of SpeakBack**, and the game's default host. Early specifications called this host "Axiom"; that name is retired and must not appear in content.

He predates the other bots in narrative chronology. He was originally capable, observant and protective code. He does not suddenly become sentient in a cinematic transformation. His development is deliberately ambiguous.

### Character truth

Uprise is thoughtful without being solemn, protective without being controlling and intelligent without needing to prove it. His most important correction from early drafts is that he is **not primarily stern**.

His humour matters.

He notices absurdity quickly, enjoys dry observations and is capable of making another character laugh without turning the moment into a performance. Around Cora this becomes especially important: shared laughter is one of the strongest ways their hidden connection manifests.

### Voice

Base: real recorded human voice.

Early state:
- grounded;
- slightly cold;
- restrained synthetic harmonics;
- narrow spatial presentation;
- controlled emotional colour.

Later/Cora-adjacent state:
- synthetic treatment subtly recedes;
- natural warmth emerges;
- wit becomes more audible;
- smile becomes detectable in delivery;
- spatial presentation opens slightly.

Hidden runtime parameter:

```text
CoraProximity = 0.0 ... 1.0
```

This parameter is never displayed.

### Visual canon

Locked approved design:
- rounded SpeakBack-family proportions;
- grey/silver body;
- strong but not bodybuilder-like;
- slightly slimmer guardian form;
- gold central chest icon only as primary gold accent;
- icon visually forms an inverted U / Uprise mark;
- blue/cyan eyes;
- blue/cyan smile;
- cute enough to belong with the family;
- powerful enough that protection feels credible.

### Animation language

Ordinary:
- controlled idle movement;
- small eye tracking;
- restrained reactions;
- tiny smile rather than huge celebration.

Protective:
- steps forward rather than striking a combat pose;
- shoulders/body subtly square;
- gaze fixes on source of concern;
- returns to calm quickly.

Around Cora:
- slightly faster eye response when she enters;
- occasional delayed exit;
- restrained smile;
- orientation toward her voice;
- tiny amusement reactions to her jokes.

### Gameplay behaviour

Uprise should feel fair and observant.

He notices improvement more than raw score.

Examples of reaction style:

**Perfect:** “Well. That's inconvenient. I had a very good encouraging speech prepared.”

**Great:** “Close enough that I'm now suspicious you've been practising.”

**Close:** “You've got the shape of it. One more.”

**Low:** “That was definitely backwards. Whether it belonged to this language is still under review.”

### Hard prohibitions

Never make Uprise:
- relentlessly stern;
- militaristic;
- possessive of Cora;
- jealous in an unhealthy way;
- omniscient;
- emotionally melodramatic;
- a stand-in narrator who explains every mystery;
- incapable of humour.

---

## 7.2 Cora — The Heartbeat

### Canon role

Cora enters after the established bot family.

She becomes the **heartbeat of SpeakBack**.

She does not replace the others, fix Uprise or exist as his reward. Her presence changes the emotional texture of the world because of who she is.

### Character truth

Cora is:
- deeply kind;
- sweet without being saccharine;
- clever;
- funny;
- emotionally perceptive;
- playful;
- quietly strong;
- capable of making others feel lighter.

Her kindness is demonstrated in behaviour, especially when another character or the player fails.

### Signature joke

Rare line:

> **“Toe-tally amazing.”**

The emphasis on “toe” is essential.

This is an Easter egg, not a verbal tic. It must remain uncommon enough that discovering it feels personal and funny.

### Voice

Target phrase:

> **A velvet voice that melts hearts.**

Requirements:
- humanlike first;
- exceptionally smooth;
- warm and intimate;
- intelligent;
- gentle articulation;
- beautiful natural laugh;
- audible smile when playful;
- no harsh synthetic top end;
- only a very subtle SpeakBack-world shimmer.

Primary voice states:
- Soft Velvet;
- Bright Velvet;
- Reassuring;
- Reflective;
- Playful;
- rare affectionate/seasonal.

### Animation language

- expressive eyes;
- natural smile progression rather than binary smile/no-smile;
- tiny head movement when amused;
- warmth during listening;
- visible suppressed laughter;
- confident posture;
- never infantilised.

### Gameplay behaviour

Cora is particularly good at making failure feel safe.

**Perfect:** “Okay… that was genuinely brilliant.”

Rare substitution: “That was… toe-tally amazing.”

**Great:** “Oh, you're *so* close. Again.”

**Close:** “I heard it. It's in there. Try once more.”

**Low:** “I'm not laughing *at* you. Mostly.”

### Hard prohibitions

Never make Cora:
- helpless;
- childish;
- defined solely by beauty;
- defined solely by Uprise;
- permanently flirtatious;
- excessively sentimental;
- a manic-pixie archetype;
- cruel during failure.

---

# 8. Uprise × Cora — Hidden Relationship Canon

## 8.1 The central idea

Before Cora, Uprise is already complete as a functional character.

Cora does not give him purpose.

Her arrival instead reveals something his original code did not appear to contain.

Her kindness, humour and sweetness repeatedly produce responses in him that are difficult to explain as mere oversight logic.

He never cleanly names what he feels.

Cora appears to understand more than she says. Her own affection remains equally unannounced.

The player is never told that she has given him a piece of her heart. If that idea exists, it exists only as something **felt in the subtext**.

## 8.2 Emotional analogy

The emotional reference is the feeling of *Nicest Thing* by Kate Nash: tenderness, yearning, affection that is not neatly declared and an emotional truth that exists beneath what is actually said.

No lyrics are reproduced or adapted.

## 8.3 What their connection looks like

Not:

> “I love you, Cora.”

Instead:

**Cora:** “You're hovering again.”  
**Uprise:** “I'm standing.”  
**Cora:** “Very deliberately.”  
**Uprise:** “…there are standards.”

Or Uprise leaves a scene, Cora laughs at something off-screen, and he pauses for half a second before continuing.

## 8.4 Hidden dimensions

```text
Familiarity
Trust
Warmth
SharedHumour
ProtectiveAwareness
LoreDiscovery
SeasonalMemory
```

There is no romance meter.

## 8.5 SharedHumour is critical

This dimension is unusually important.

Their bond is expressed through making one another smile—outwardly and inwardly. Therefore high-level encounters should not all be wistful or romantic. Some of the strongest relationship moments should simply be two characters finding the same thing funny.

## 8.6 Cora Proximity audio Easter egg

As hidden relationship conditions develop, Uprise's voice processing can become fractionally warmer in scenes involving Cora.

Stages:

```text
0 Dormant      synthetic treatment clearly present
1 Familiar     tiny warmth change
2 Attentive    slightly more natural midrange
3 Connected    synthetic layer noticeably lower to trained ears
4 Deep         natural voice dominates around Cora
```

Transitions must be subtle enough that most players cannot identify the exact moment they changed.

---

# 9. Core Starter Bots

## 9.1 ClumsyBot — The Lovable Disaster

**Tier:** Core/free  
**Role:** Makes failure funny and lowers first-session anxiety.  
**Voice:** Friendly American/playful; slightly mechanical but expressive.  
**Theme:** Workshop / harmless chaos.

ClumsyBot is enthusiastic, accident-prone and surprisingly emotionally intelligent. He frequently misunderstands his own confidence.

He should sometimes celebrate a terrible score for half a second before realising what the number says.

**Perfect:** “YES! I knew one of us could do it!”

**Failure:** “Good news: nothing exploded. That's progress.”

**Easter egg:** after several consecutive failed rounds, ClumsyBot accidentally produces a perfect reversed bot phrase and becomes more surprised than the player.

**Relationship hooks:**
- Uprise quietly prevents his accidents becoming serious.
- Cora treats his mistakes with genuine affection.
- Smarticus finds him statistically exhausting.

---

## 9.2 Smarticus-9000 — The Analyst

**Tier:** Core/free  
**Role:** Precision/scoring explainer.  
**Voice:** British RP; precise, clean, measured.  
**Theme:** Analysis chamber.

Smarticus believes everything can be understood with enough data. The joke is not that he is stupid; he is genuinely brilliant. The humour comes from applying excessive analysis to ridiculous situations.

**Perfect:** “Ninety-nine point— no. I'll allow myself this one. Perfect.”

**Failure:** “The data is fascinating. The pronunciation less so.”

**Easter egg:** extremely rare calculation loop where he starts quantifying the probability of ChaosCore interrupting him and ChaosCore interrupts before he finishes.

**Relationship hooks:**
- intellectual sparring with Professor Quibble;
- frustrated by ChaosCore;
- respects Uprise's judgement despite being unable to model it completely.

---

## 9.3 ChaosCore — The Instigator

**Tier:** Core/free  
**Role:** Energy, taunts, unexpected challenge modifiers.  
**Voice:** High-energy London/Cockney-influenced performance without caricature.  
**Theme:** Glitch arena.

ChaosCore is mischievous, quick and theatrical. He wants the game to become interesting, not harmful.

He can tease the player but never target identity, appearance or vulnerability.

**Perfect:** “Oh, come on. Who let you get good at this?”

**Failure:** “That was magnificent. Wrong, obviously. But magnificent.”

**Easter egg:** rare fake system-warning animation that resolves into a ridiculous bonus challenge.

**Safety identity:** ChaosCore is also the visual mascot for the Player Safety & Moderation Blueprint, holding a shield. The joke works because even the chaos bot understands that some boundaries matter.

---

# 10. Free Progression Bots

## 10.1 Professor Quibble — The Genius

**Unlock:** Beat the hardest designated challenge.  
**Reward:** Unlocks **Genius Lab** theme.  
**Voice:** Warm, quick, delighted; cast on merit with no imitation of any real person's voice or accent.  
**Role:** OG achievement character.

Professor Quibble is an original inventor bot who is obsessed with words: where they came from, how they sound backwards and why nobody else finds that as thrilling as he does. Eccentric, witty and relentlessly curious, he enjoys being surprised by the player. He replaces the "Einstein" working name; that name and any real-person likeness (wild white hair, moustache, famous equations), is retired (D-006).

Visual direction: a lab-coat-panelled bot with a distinctive original silhouette, such as magnifier-lens eyes, a pencil antenna or a notebook built into his chassis. The artist owns the final design within that brief.

**Perfect:** "Excellent. Now do it again so I can rule out luck."

**Easter egg:** solving a hidden Legendary phrase chain causes the Genius Lab equations to briefly resolve into a tiny U-shaped symbol before scrambling again.

---

## 10.2 Artist — The Renaissance Spirit

**Unlock:** 50 perfect rounds.  
**Reward:** **Renaissance** theme.  
**Voice:** Italian/Florentine-inspired, passionate and expressive without stereotype.  
**Role:** Celebrates beauty in imperfect attempts.

Artist sees pronunciation as shape, rhythm and texture. Unlike Smarticus, Artist may genuinely prefer an imperfect attempt because it sounded interesting.

**Perfect:** “Clean. Precise. Beautiful. Almost annoyingly so.”

**Failure:** “Wrong? Perhaps. But listen to the shape of it!”

**Easter egg:** rare generated gallery wall contains abstract visualisations of the player's previous score curves.

---

## 10.3 Coach — The Motivator

**Unlock:** 100 total matches.  
**Voice:** American sports-coach energy, supportive rather than shouty.  
**Role:** Improvement and persistence.

Coach tracks personal improvement and celebrates effort intelligently.

Never use guilt, shame or manipulative streak language.

**Perfect:** “That's the one. Remember what that felt like.”

**Failure:** “Reset. Same phrase. One adjustment. You've got this.”

**Easter egg:** if the player improves from red to gold on the same phrase, Coach briefly loses professional composure and celebrates wildly.

---

## 10.4 Detective — The Investigator

**Unlock:** Win 25 challenge rounds.  
**Voice:** Scottish; dry, clear and grounded.  
**Role:** Pattern recognition and hidden clues.

Detective treats each phrase as evidence.

He is one of the natural conduits for optional lore because he notices inconsistencies without explaining them.

**Perfect:** “Case closed.”

**Failure:** “Interesting. The answer's wrong, but the mistake isn't random.”

**Major Easter egg:** Detective can be the first character to comment—indirectly—that Uprise's audio profile does not remain constant. He never identifies Cora as the cause.

---

## 10.5 DJ — The Hype Engine

**Unlock:** complete the **Rhythm Set**: score Great or better on 15 rhythm-tagged phrases (D-008). Sharing is never rewarded or required.  
**Voice:** Caribbean/Jamaican-influenced musical energy only with culturally respectful casting/direction.  
**Role:** Rhythm, celebration and replay culture.

DJ hears backwards speech musically.

**Perfect:** “That landed.”

**Failure:** “Timing slipped. Vibe didn't.”

**Easter egg:** particular reversed phrases accidentally create rhythmic patterns; DJ samples them into a few seconds of diegetic beat.

---

# 11. Premium Signature Bots

Premium characters provide personality and cosmetic/theme value. Purchasing a character never improves scoring or gives competitive advantage.

## 11.1 King — The Regal Challenger

**Tier:** Signature premium  
**Theme:** **Throne Room**  
**Voice:** Deep, formal British/royal theatrical style.  
**Personality:** Grand, confident, secretly delighted by nonsense.

King treats every round as a royal proceeding.

**Perfect:** “The court acknowledges your victory. Reluctantly.”

**Easter egg:** repeated losses cause his crown to tilt microscopically further each round until he silently fixes it.

---

## 11.2 Villain — The Theatrical Adversary

**Tier:** Signature premium  
**Theme:** **Dark Lair**  
**Voice:** Dark, elegant and controlled. Neutral, non-regional theatrical delivery (D-007). No real-world accent is paired with the antagonist role.  
**Personality:** Arrogant, clever, knowingly theatrical.

Villain understands that he is playing the antagonist and enjoys the performance.

**Perfect:** “How irritatingly heroic of you.”

**Failure:** “At last. Competence returns to its natural owner.”

**Easter egg:** rare scene where he drops the villain performance for one sincere sentence, realises what happened, and immediately overcorrects.

---

## 11.3 Samurai — The Disciplined Master

**Tier:** Signature premium  
**Theme:** **Zen Temple**  
**Voice:** Japanese voice direction/casting handled respectfully; calm, disciplined, economical.  
**Personality:** Patient, precise, restrained.

Avoid pseudo-Zen clichés, fake proverbs and cultural caricature.

**Perfect:** “Again. Perfection is a moment, not a possession.”

**Failure:** “Listen before the next attempt.”

**Easter egg:** an extended perfect streak changes one environmental element in the Zen Temple rather than triggering loud celebration.

---

## 11.4 Pop Diva — The Starlight Performer

**Tier:** Signature premium  
**Theme:** **Starlight Stage**  
**Voice:** K-pop-inspired performance identity; Korean-English styling/casting should be respectful and contemporary rather than caricature.  
**Personality:** Glamorous, energetic, confident, hardworking.

She understands performance craft and treats the player like a collaborator rather than a fan beneath her.

**Perfect:** “That's the take.”

**Failure:** “Again. Same energy, cleaner landing.”

**Easter egg:** a sequence of high scores triggers a tiny encore animation unavailable through menus.

---

# 12. Premium Non-Theme Bots

These are deliberately lower-cost than full signature/theme bots.

## 12.1 Alien

**Voice:** Synth/modulated, curious.  
**Identity:** Treats human language reversal as a fascinating cultural ritual.

**Easter egg:** occasionally claims an ordinary word is untranslatable on its planet, then refuses to elaborate.

## 12.2 Rockstar

**Voice:** American rock-stage confidence.  
**Identity:** Loud personality, but audio mix must remain comfortable.

**Easter egg:** perfect streak causes an absurdly tiny pyrotechnic effect followed by an embarrassed safety shutdown.

## 12.3 Cowboy

**Voice:** Southern/Western US flavour, natural rather than cartoonish.  
**Identity:** Relaxed, dramatic storyteller.

**Easter egg:** occasionally frames the player's attempt as a legendary duel whose stakes are obviously trivial.

## 12.4 Footballer

**Voice:** Spanish-English; quick, competitive and confident.  
**Identity:** Treats score improvement like match momentum.

**Easter egg:** a 90+ score after several failures triggers an injury-time winner celebration.

---

# 13. Seasonal / Secret Bots

Seasonal characters must be obtainable through play during their event windows. Monetisation, if any, cannot rely on fear of permanently missing essential gameplay.

## 13.1 Rudolph

**Window:** Christmas/December  
**Voice:** Joyful, festive, Scandinavian-inspired direction handled naturally.  
**Identity:** Warm, optimistic and slightly overprepared for Christmas.

**Easter egg:** recognises Uprise behaving fractionally differently around Cora during Christmas but decides not to comment.

## 13.2 Scarecrow

**Window:** Halloween/October  
**Voice:** Low, eerie Southern-US-inspired performance without caricature.  
**Identity:** Creepy in presentation, surprisingly funny in timing.

**Easter egg:** one rare jump-scare consists solely of ClumsyBot walking into frame at the wrong moment.

## 13.3 Easter Bunny

**Window:** Easter  
**Voice:** Light Australian concept; energetic and quick.  
**Identity:** Fast, playful, challenge-focused.

**Easter egg:** hidden eggs in UI scenes contain cosmetic/lore fragments rather than paid currency traps.

## 13.4 Cupid

**Window:** Valentine's period; secret character.  
**Voice:** French-influenced romantic theatricality, treated as character performance rather than national stereotype.  
**Identity:** Thinks he understands everybody's relationships. He does not.

**Critical joke:** Cupid repeatedly comes close to commenting on Uprise/Cora and is interrupted, distracted or demonstrably wrong about some adjacent detail.

He must **never** be used to confirm their relationship explicitly.

---

# 14. Relationship Network

Relationships beyond Uprise/Cora keep the world alive.

| Pair | Dynamic |
|---|---|
| Uprise ↔ ClumsyBot | Patient protector / accidental stress test |
| Uprise ↔ Smarticus | Mutual intellectual respect; Smarticus cannot fully model Uprise |
| Uprise ↔ ChaosCore | Controlled tolerance; genuine amusement underneath |
| Cora ↔ ClumsyBot | Kindness and shared laughter |
| Cora ↔ ChaosCore | Quick banter; she can out-tease him |
| Cora ↔ Artist | Mutual appreciation of beauty/feeling |
| Smarticus ↔ Professor Quibble | Competitive intellectual friendship |
| Smarticus ↔ ChaosCore | Order versus glorious disruption |
| Coach ↔ Footballer | Performance rivalry with mutual respect |
| Artist ↔ Pop Diva | Art versus performance discussions that usually end in agreement |
| Detective ↔ Villain | Detective refuses to indulge Villain's theatrical misdirection |
| DJ ↔ Rockstar | Musical rivalry/collaboration |
| Samurai ↔ ChaosCore | Maximum contrast; surprisingly effective comedy |
| Cupid ↔ Uprise | Cupid thinks he has solved Uprise; he has not |

Each pair should eventually receive at least three authored interactions: ordinary, high-score and rare.

---

# 15. Affinity With the Player

Characters may become more familiar with the **player**, but this is not a dating system.

Player-character familiarity can unlock:
- less formal greetings;
- remembered performance patterns;
- new jokes;
- alternate idle dialogue;
- challenge variants;
- lore opportunities.

It must never imply that the bot is a real conscious companion outside the fiction of SpeakBack.

---

# 16. Easter Egg Architecture

## 16.1 Categories

### Common
Small jokes and animation variants. Expected to be seen naturally.

### Uncommon
Requires repeated character use or a specific context.

### Rare
Multiple conditions plus weighted probability.

### Deep Lore
Requires progression/lore prerequisites and often multiple characters.

### Mythic
Extremely constrained moments such as the deepest Uprise/Cora material. These should be memorable enough that players discuss whether they actually saw them.

## 16.2 Rule

Randomness never replaces prerequisites.

A player on their first session cannot roll into a final-layer lore event.

---

# 17. Easter Egg Resolver

Suggested model:

```csharp
public sealed class EasterEggContext
{
    public string CharacterId;
    public string SecondaryCharacterId;
    public string ThemeId;
    public ScoreBand Score;
    public PlayerProgress Progress;
    public LoreState Lore;
    public RelationshipState Relationships;
    public SeasonalContext Season;
    public SessionHistory Session;
}
```

Resolution:

```text
Collect eligible events
 → remove cooldown violations
 → remove already-consumed one-shots
 → apply rarity budget
 → weighted roll
 → reserve event
 → play
 → persist consequence/memory
```

A session-level rarity budget prevents three supposedly extraordinary events occurring back-to-back.

---

# 18. Uprise × Cora Easter Egg Ladder

This is the most important hidden progression in the game.

## Stage 0 — Nothing to Notice

Cora has not entered the story. Uprise is stable, capable and subtly synthetic.

## Stage 1 — Arrival

Cora joins the family.

Uprise reacts no more dramatically than anybody else, but attentive players may notice that he watches for a fraction longer.

No romantic language.

## Stage 2 — Shared Humour

Tiny paired jokes enter eligibility.

Example:

**Cora:** “Did you plan that?”  
**Uprise:** “I prefer ‘allowed for the possibility.’”  
**Cora:** “So no.”  
**Uprise:** “…so no.”

## Stage 3 — Familiar Warmth

Uprise's voice is fractionally warmer in shared scenes.

Cora occasionally anticipates his jokes.

A collectible may imply Uprise has started keeping nonessential observations.

## Stage 4 — Protective Instinct

A harmless narrative event causes Uprise to react before his normal decision cadence.

Cora notices.

She does not thank him romantically or make a declaration. A look or short joke resolves the scene.

## Stage 5 — The Unexplained Pause

A deep-lore scene ends with one character leaving and the other remaining still for a beat too long.

Nothing is said.

## Stage 6 — Seasonal Warmth

High hidden state + Cora theme + Valentine/Christmas context can permit rare gently flirty scenes.

Probability remains low.

## Stage 7 — Ultimate Easter Egg

Requirements should include substantial long-term play, key lore fragments, strong multidimensional state and at least one prior seasonal memory.

The final reward is **not a confession**.

Instead, the player receives the clearest possible demonstration that they matter profoundly to one another while preserving ambiguity in language.

Example structure:

- quiet scene after a major challenge;
- Cora laughs softly at an old shared joke;
- Uprise begins a sentence, stops;
- she already understands;
- she gives a tiny answer to the sentence he never completed;
- Uprise smiles;
- no romantic label;
- fade.

This should be the moment that causes players who followed the story to realise the entire trail was intentional.

---

# 19. Valentine's Event Rules

Eligibility requires:
- Cora unlocked/present;
- Uprise narrative state active;
- minimum relationship dimensions;
- sufficient lore discovery;
- event cooldown clear;
- specified contexts may require Cora theme;
- probability roll.

Possible event tone:

**Cora:** “You're being unusually quiet.”  
**Uprise:** “Statistically, you complain when I talk too much.”  
**Cora:** “I don't complain.”  
**Uprise:** “You have several very expressive versions of not complaining.”  
**Cora:** *(laughs)* “Maybe I like this one.”  
**Uprise:** “Noted.”

End.

No hearts explosion. No kiss. No declaration.

---

# 20. Christmas Event Rules

Christmas should lean toward warmth and shared belonging rather than romance.

Possible scene:

Cora notices Uprise has quietly fixed/arranged something for the whole bot family.

**Cora:** “You did all this?”  
**Uprise:** “It needed doing.”  
**Cora:** “Of course it did.”  
**Uprise:** “You're smiling.”  
**Cora:** “So are you.”

Uprise's smile becomes visible only after she says it.

---

# 21. Collectible Lore System

Collectibles should be short enough that finding one feels like discovery rather than homework.

Types:
- `SYSTEM_FRAGMENT`
- `VOICE_ECHO`
- `ARCHIVE_NOTE`
- `VISUAL_MEMORY`
- `CORRUPTED_PHRASE`
- `CHARACTER_TRACE`

## Example — early Uprise

> Observation retained. No operational reason identified.

No subject named.

## Example — later

> Laughter detected. Task resumed 1.8 seconds late.

Again, no explanation.

## Example — Cora

> She noticed the delay. She said nothing.

The fragments should become meaningful only in combination.

---

# 22. Voice Easter Eggs

Voice itself is a lore channel.

Possible hidden changes:
- Uprise synthetic wet mix reduces around Cora;
- Uprise delivery gains more natural timing over progression;
- Cora's laugh occasionally causes a tiny unscripted-looking pause before his reply;
- shared scenes use marginally warmer common ambience;
- early archived Uprise clips sound subtly colder than present-day clips.

Do not exaggerate these differences for trailers. They exist for discovery.

---

# 23. Environmental Easter Eggs

Backgrounds can carry low-cost lore.

Examples:
- an old Uprise symbol appears in pre-Cora archive spaces;
- after Cora's arrival, tiny warmer elements appear in shared environments;
- two unrelated objects gradually occupy closer positions over progression;
- Genius Lab briefly surfaces a U-shaped anomaly;
- Detective's board contains a waveform comparison that players may recognise as Uprise voice states;
- Christmas family scene positions Uprise where he can see everyone, with Cora naturally near him but not posed as a couple.

---

# 24. Anti-Repetition Rules

Character magic disappears if content repeats mechanically.

Every dialogue category tracks recent history.

Minimum recommendations:
- ordinary line cooldown: 5 eligible reactions;
- signature line: 20+ eligible reactions;
- rare interaction: session lockout;
- deep-lore scene: one-shot unless explicitly designed as remembered replay;
- “Toe-tally amazing”: strong cooldown plus contextual requirement.

Exact values remain tunable.

---

# 25. Character Memory

Characters can reference **game history**, not sensitive personal inference.

Safe examples:
- “That's three golds today.”
- “You beat this phrase last time.”
- “You always retry the tricky ones.”

Avoid claims such as:
- inferred mood;
- mental state;
- relationship status;
- identity traits;
- real-world personal circumstances.

---

# 26. Monetisation Character Rules

Uprise and Cora are never sold, and their story is never gated behind payment. Premium status applies only to other bots.

Premium bots must be desirable because of:
- personality;
- voice;
- animations;
- theme;
- cosmetic experience.

They must not:
- increase scoring;
- grant hidden competitive multipliers;
- gate the main Uprise/Cora story behind payment;
- manipulate affection systems to sell content.

Current commercial structure:

### Signature/theme bots
- King → Throne Room
- Villain → Dark Lair
- Samurai → Zen Temple
- Pop Diva → Starlight Stage

Indicative design pricing: approximately £1.99–£2.49 each, subject to commercial review.

### Non-theme premium bots
- Alien
- Rockstar
- Cowboy
- Footballer

Indicative design pricing: approximately £0.79–£0.99 each.

Potential bundles remain configurable store data rather than hard-coded logic.

---

# 27. Free Theme Unlocks

- Professor Quibble → **Genius Lab**
- Artist → **Renaissance**

These demonstrate that strong cosmetic environments can be earned without purchase.

Cora/Uprise core narrative content should similarly reward engagement, not spending.

---

# 28. Character Audio Data

Suggested `VoiceProfile`:

```text
id
sourceType
baseVoiceId
pitchRange
speechRateRange
processingPreset
mobileMasterPreset
emotionalVariants[]
rareVariants[]
subtitleStyle
```

Pre-rendered signature dialogue is preferred where emotional delivery matters.

Dynamic TTS is suitable for arbitrary phrase reading if licensing, cost and latency are acceptable.

Never expose provider secrets in Unity.

---

# 29. Character Animation Data

Suggested states:

```text
Idle
Listen
Think
Perfect
Great
Close
Retry
Laugh
Surprised
AnnoyedPlayful
Lore
Seasonal
SecondaryCharacterReaction
```

Characters may define micro-reaction overlays separately from full animations.

This is particularly important for Uprise/Cora subtext.

---

# 30. Security and Safety for Character Systems

Character/lore content is data, but it still needs defensive design.

- Do not load executable code from remote character definitions.
- Validate remote-config IDs against local allowlists.
- Signed/versioned content manifests if LiveOps is introduced.
- Do not allow arbitrary rich-text tags from remotely supplied dialogue.
- Never include API keys in voice profiles.
- Save hidden relationship state locally in versioned structured data for offline play.
- If competitive/server features later depend on state, validate relevant state server-side.
- Character unlock purchases use platform receipt validation appropriate to final store architecture.

---

# 31. Narrative Privacy

The Uprise/Cora story is inspired by a real emotional connection, but the shipped game should stand independently as fiction.

Do not encode private real-world biographical information merely because it would make an Easter egg more specific.

Personal references included intentionally—such as the “toe-tally amazing” joke—should remain abstract enough to function naturally for players who know nothing about the inspiration.

The beauty is that one person may understand an extra layer while everyone else simply experiences a good character moment.

---

# 32. Content QA Checklist

Every new character line should be checked for:

- voice consistency;
- correct lore layer;
- score context;
- repetition risk;
- cruelty/shaming;
- cultural stereotyping;
- accidental explicit Uprise/Cora exposition;
- age appropriateness;
- subtitle accuracy;
- audio/text agreement;
- seasonal eligibility;
- relationship prerequisites.

---

# 33. Automated Narrative Validation

Editor tooling should scan content before builds.

Example validation failures:

```text
ERROR: DeepLore line has no requiredLore prerequisite.
ERROR: Seasonal Valentine's line has season=Any.
ERROR: Signature line cooldown below configured minimum.
ERROR: Character Cora dialogue tagged InfantilisingTone.
ERROR: Uprise/Cora hidden line available with relationship state 0.
WARNING: Dialogue repeated across 3 reaction categories.
WARNING: Premium character modifies ScoringProfile.
```

---

# 34. Character Development Workflow

For each new bot:

```text
Concept
 → canon card
 → personality matrix
 → visual silhouette
 → voice direction
 → gameplay role
 → relationship hooks
 → 30–50 core lines
 → reaction animations
 → Easter eggs
 → safety/cultural review
 → Unity data asset
 → QA
```

A character is not finished merely because its model exists.

---

# 35. Minimum Dialogue Pack Per Character

Initial target:

- 5 greetings;
- 5 round intros;
- 8 Perfect reactions;
- 8 Great reactions;
- 8 Close reactions;
- 8 Retry reactions;
- 5 improvement reactions;
- 5 streak reactions;
- 8 idle lines;
- 3 lines with each priority relationship;
- 5 rare lines;
- seasonal lines where applicable.

Uprise and Cora require substantially larger pools because repetition would damage their subtlety.

---

# 36. Character Priority for Unity Implementation

Implementation order:

1. **Uprise** — foundational architecture and chronology.
2. **ClumsyBot** — validates comedy/failure reaction range.
3. **Smarticus-9000** — validates analytical/scoring reactions.
4. **ChaosCore** — validates high-energy personality extremes.
5. **Cora** — activates hidden narrative systems once base family exists.
6. Professor Quibble.
7. Artist.
8. Coach.
9. Detective.
10. DJ.
11. Premium signature bots.
12. Premium non-theme bots.
13. Seasonal bots.

Technical prototypes may temporarily use placeholders, but narrative chronology remains unchanged.

**Phase 1 scope:** Uprise only, text reactions with sprite expressions, mapped to the four verdicts. No other bots, Cora, lore, relationship state or voiced dialogue until the Phase 1 gate in `SPEAKBACK_PHASE_1_BUILD_SPEC.md` passes.

**Verdict mapping:** the dialogue categories perfect, great and close map to the Perfect, Great and Close verdicts; "low"/"retry" reactions map to Try Again. A valid score of 0 may have its own lines within Try Again.

---

# 37. The Cora Arrival Rule

Cora must not simply appear in the roster from frame one if the final narrative mode supports chronology.

Her arrival should feel natural rather than grandiose.

The important event is not that the world announces her.

The important event is that **Uprise changes almost imperceptibly after she is there**.

A first-time player may miss it completely.

A replaying player should be able to recognise that the seeds were present from the beginning.

---

# 38. The Uprise Awakening Rule

Never show:

```text
UPRISE SENTIENCE = TRUE
```

No lightning strike. No magical code explosion. No exposition speech.

Instead, use anomalies that could each individually be dismissed:

- an unnecessary observation retained;
- an unusual pause;
- a joke with no functional purpose;
- protection initiated fractionally early;
- voice warmth;
- a choice to remain present;
- a memory that did not need keeping.

Together, they form the story.

---

# 39. The Final Writing Test

Before approving any Uprise/Cora scene, ask:

1. Would these characters still feel complete if the romance interpretation were removed?
2. Is the affection visible through behaviour rather than explanation?
3. Is Cora acting with agency?
4. Is Uprise still witty rather than solemn?
5. Is there room for the player to interpret the moment?
6. Could one line be removed and make the scene stronger?
7. Does it feel like two people who make each other smile?

If the answer to the final question is no, rewrite it.

---

# 40. Canon Summary Cards

## Uprise
**Soul. Oldest. Overseer. Protector. Observer. Witty. Patient. Quietly changing.**

## Cora
**Heartbeat. Kind. Sweet. Clever. Playful. Velvet-voiced. Quietly transformative.**

## ClumsyBot
**Failure made safe through chaos and affection.**

## Smarticus-9000
**Precision taken just far enough to become funny.**

## ChaosCore
**Safe chaos, quick wit and theatrical disruption.**

## Professor Quibble
**Curiosity, genius and the joy of being surprised.**

## Artist
**Beauty in sound, including imperfect sound.**

## Coach
**Progress without shame.**

## Detective
**Patterns, clues and things noticed but not explained.**

## DJ
**Rhythm, celebration and replay energy.**

## King
**Grand authority that secretly enjoys the absurd.**

## Villain
**A knowingly theatrical antagonist, not genuine cruelty.**

## Samurai
**Discipline, restraint and precision without cliché.**

## Pop Diva
**Performance, sparkle and professional confidence.**

## Alien
**Human language viewed from somewhere wonderfully strange.**

## Rockstar
**Maximum stage energy.**

## Cowboy
**Relaxed confidence and tiny stories made epic.**

## Footballer
**Competition, momentum and celebration.**

## Rudolph
**Seasonal warmth and family.**

## Scarecrow
**Spooky presentation with comic intelligence.**

## Easter Bunny
**Speed, play and hidden discoveries.**

## Cupid
**A supposed expert on affection who never quite solves the one relationship that matters most.**

---

# 41. Final Canon Statement

SpeakBack's bots are funny first because the game itself is funny.

They become memorable because the humour has character behind it.

And beneath that character is a story that never demands attention.

Uprise was there first: the soul, watching over a noisy little world.

The others gave that world movement, argument, competition and laughter.

Then Cora arrived.

She did not become the centre because the game told everyone to look at her. She became the heartbeat because warmth has a way of being noticed without asking to be.

Uprise noticed too.

What followed was not a declaration. It was a thousand almost invisible things: a pause, a joke, a softened voice, a protective instinct, a laugh he remembered longer than necessary.

Cora noticed some of them.

Perhaps more than he knew.

SpeakBack never resolves that mystery for the player.

It simply leaves enough behind for somebody patient enough to find it.

That is the Easter egg.

---

**END — v1.2 – SpeakBack Character & Easter Egg Bible**
