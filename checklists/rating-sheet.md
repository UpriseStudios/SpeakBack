# Blind Rating Sheet (HUMAN STEP B)

**Raters:** 3 or more adults. Ideally not the people who were recorded, and not shown any scores.  
**What they rate:** the **flipped** takes only (`*.flipped16k.wav`), the version that should sound like the phrase.

## Instructions for raters

For each clip you'll see the target phrase. Play the clip (as many times as you like) and answer:

1. **Overall:** did that sound like the target phrase?
   - `2` **Good**: clearly recognisable; you'd accept it.
   - `1` **Partial**: some words or sounds are recognisable.
   - `0` **Unintelligible**: you can't hear the phrase in it.
2. **Closeness (optional, 0–10):** how close was it overall? 10 = indistinguishable from the phrase said normally.

Rate how close the sound is to the phrase, not the speaker's accent or voice. An accented but correct attempt is Good.

Shuffle the clip order for each rater (P05's harness can generate a shuffled list: `python -m bakeoff rating-list --rater R1`).

## CSV format (`ratings.csv`)

```csv
take_id,rater_id,label,closeness
S01_sb_en_001_t1,R1,2,8
S01_sb_en_001_t1,R2,1,6
S01_sb_en_001_t1,R3,2,7
```

- `take_id` = the file name without `.flipped16k.wav`.
- `rater_id` = R1, R2… (no names).
- `label` = 0, 1 or 2.
- `closeness` = 0–10, or blank.

Every take needs at least 3 ratings.
