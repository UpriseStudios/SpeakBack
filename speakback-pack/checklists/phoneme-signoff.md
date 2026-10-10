# Phoneme Sign-off (HUMAN STEP D)

Phrases are scored against their phoneme sequences, so a wrong phoneme means unfair scores for everyone. The ten Phase 1 phrases have **draft** British (RP-style) phonemes in `data/phrases/phase1_phrases.json`.

## How to check each phrase

1. Say the phrase naturally, the way your players would.
2. Read the phoneme list aloud slowly using the key below. Does it match a normal British pronunciation?
3. Watch for the usual traps:
   - Non-rhotic r: "never" ends in /ə/ with no r; "purple" has /ɜː/ with no r.
   - "the" before a consonant is /ð ə/.
   - Weak vowels: "banana" is /b ə n ɑː n ə/.
   - Words that vary: "sandwich" (-wɪtʃ / -wɪdʒ / -ndwɪtʃ). Put the common variants in `alternatePronunciations`.
   - Happy-tensing: "tiny" ends in /iː/ in SBPS.
4. Fix any token in the JSON. Use only SBPS symbols from `data/phonemes/sbps.json`.
5. Set `phonemeStatus` to `reviewed` and add `"reviewedBy": "Jamie", "reviewedOn": "YYYY-MM-DD"`.
6. Run `python -m content validate` (from P07).

## Quick key (SBPS)

| Symbol | As in | Symbol | As in | Symbol | As in |
|---|---|---|---|---|---|
| iː | fleece | ɪ | kit | e | dress |
| æ | trap | ʌ | strut | ɒ | lot |
| ɔː | thought | ʊ | foot | uː | goose |
| ɜː | nurse | ə | about | ɑː | palm, bath |
| eɪ | face | aɪ | price | ɔɪ | choice |
| əʊ | goat | aʊ | mouth | ɪə | near |
| eə | square | ʊə | cure | θ | thin |
| ð | this | ʃ | ship | ʒ | measure |
| tʃ | church | dʒ | judge | ŋ | sing |
| j | yes | r, w, l, h, m, n | as spelt | p b t d k ɡ f v s z | as spelt |

## Sign-off

| ID | Phrase | Changes made | Reviewed |
|---|---|---|---|
| sb_en_001 | Blue banana | | ☐ |
| sb_en_002 | Tiny dragon | | ☐ |
| sb_en_003 | Hello robot | | ☐ |
| sb_en_004 | Purple moon | | ☐ |
| sb_en_005 | My cat rules | | ☐ |
| sb_en_006 | Please pass the biscuits | | ☐ |
| sb_en_007 | The robot stole my sandwich | | ☐ |
| sb_en_008 | Never trust a talking penguin | | ☐ |
| sb_en_009 | I speak fluent nonsense | | ☐ |
| sb_en_010 | The tiny dragon ate my homework | | ☐ |
