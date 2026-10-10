# Bake-off Recording Session Script (HUMAN STEP A)

**Goal:** 40–60 kept flipped attempts across all ten phrases from 5+ consenting adults, giving the scorer bake-off honest data.

## Before the sessions

- [ ] Dev APK from P04 installed on 2+ phones (different makes if possible).
- [ ] References recorded in the app's "Record references" mode (you, quiet room, normal speech).
- [ ] Printed consent forms (`consent-form.md`), a pen and a participant log sheet (consent ref ↔ speaker code; keep it with the forms, not with the audio).
- [ ] A plan for variety. Across all participants, aim for:
  - at least 3 different UK accent regions, plus anyone non-UK who's willing;
  - a spread of higher, middle and lower voices;
  - at least one naturally quiet speaker;
  - one session in a moderately noisy room (TV or kitchen noise, not shouting-loud);
  - both phones used.

## Per participant (about 10–15 minutes)

1. Explain the study using the consent form. Confirm they're 18+. Get both signatures. Assign the next speaker code (S01, S02…) and consent ref.
2. In the app's setup screen, enter the speaker code, consent ref and the accent/voice labels **they choose**, then tick the confirmation box.
3. Give one practice go on any phrase, and delete the take.
4. For each phrase: play the reversed reference (twice if they ask), they hold and copy, you both hear the flip. Choose **Keep** for any genuine attempt, good or bad, since bad attempts are valuable data. Use **Redo** only for technical problems (released too early, a cough). Aim for 1–2 kept takes per phrase.
5. Don't coach them on how to say it. Don't show them scores, because there are none in this build.
6. Thank them. Remind them how to ask for deletion.

## After each session

- [ ] `adb pull /sdcard/Android/data/com.uprisestudios.speakback/files/bakeoff ~/speakback-bakeoff/data/` (the exact path is shown on the app's export screen).
- [ ] Check the files arrived, then use **Delete session** on the phone.
- [ ] Keep `~/speakback-bakeoff/` outside the repo, on an encrypted disk.

## Mix check before rating

- [ ] Every phrase has 4+ kept takes.
- [ ] No single person provides more than about 25% of takes.
- [ ] Group labels are present on every take.
